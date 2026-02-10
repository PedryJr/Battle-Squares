using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Runtime.CompilerServices;

public sealed class PlayerMLAgent : Agent
{
    #region Enums

    [Serializable]
    public enum TrainingMode
    {
        FullControl,
        MovementOnly,
        AbilitiesOnly,
        MovementAndJump,
        CombatOnly,
        Custom
    }

    [Flags]
    public enum DebugVisualization
    {
        None = 0,
        TargetDirection = 1 << 0,
        TargetRadius = 1 << 1,       
        RaycastRange = 1 << 2,      
        AimDirection = 1 << 3,       
        PredictedShot = 1 << 4,     
        ShotAccuracy = 1 << 5,      
        PunishZone = 1 << 6,          
        NavMeshPath = 1 << 7,      
        All = ~0
    }

    private enum TrainingCategory
    {
        HorizontalMovement,
        VerticalMovement,
        Jump,
        PrimaryAttack,
        SecondaryAttack
    }

    #endregion

    #region Structs

    public struct WeaponStats
    {
        public int remainingAmmo;
        public int maxAmmo;
        public bool isMelee;
        public float projectileSpeed;
        public float projectileAcceleration;
        public float projectileGravity;
    }

    #endregion

    #region Serialized Fields

    [Header("Core References")]
    [SerializeField] public PlayerController playerController;
    public MLTrainingManager mLTrainingManager;
    public bool isTraining;

    [Header("Training Mode")]
    [SerializeField] public TrainingMode currentTrainingMode = TrainingMode.FullControl;

    [Header("Custom Training Toggles (Only active when mode is Custom)")]
    [SerializeField] private bool trainHorizontalMovement = true;
    [SerializeField] private bool trainVerticalMovement = true;
    [SerializeField] private bool trainJump = true;
    [SerializeField] private bool trainPrimaryAttack = true;
    [SerializeField] private bool trainSecondaryAttack = true;

    // Add to Reward Settings section
    [Header("Consistency Reward Settings")]
    [SerializeField] private float actionConsistencyReward = 0.01f;
    [SerializeField] private float actionChangeSpamPenalty = 0.02f;
    [SerializeField] private float minActionHoldReward = 0.005f;
    [SerializeField] private int maxActionChangesPerSecond = 5;
    [SerializeField] private float MIN_ACTION_HOLD_TIME = 0.2f;

    [Header("Observation Settings")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private int numRaycastsEnvironment = 12;

    [Header("Target Selection Settings")]
    [SerializeField] private int openAreaRaycastCount = 16;
    [SerializeField] private float openAreaSearchRadius = 15f;
    [SerializeField] private float minSafeDistanceFromWall = 2f;

    [Header("Reward Settings")]
    [SerializeField] private float alignmentRewardMultiplier = 0.01f;
    [SerializeField] private float positioningMultiplier = 0.01f;
    [SerializeField] private float punishZoneMultiplier = 0.01f;
    [SerializeField] private float speedOfThatPunishZone = 10f;
    [SerializeField] private float weaponLeadingMultiplier = 0.1f;
    [SerializeField] private float weaponBlindFirePenalty = 0.05f;
    [SerializeField] private float diagonalBiasMultiplier = 2f;
    [SerializeField] private float deathPenalty = 1f;
    [SerializeField] private float healthLossPenalty = 0.1f;
    [SerializeField] private float goalReward = 10f;
    [SerializeField] private float combatHitReward = 0.5f;
    [SerializeField] private float combatDamageReward = 0.1f;

    [Header("New Reward Settings")]
    [SerializeField] private float environmentProximityReward = 0.005f;
    [SerializeField] private float environmentProximityThreshold = 5f;
    [SerializeField] private float positioningWeightWhenVisible = 2f;
    [SerializeField] private float navigationWeightWhenOccluded = 2f;
    [SerializeField] private float penaltyForFallingIntoVoid = 0.01f;
    [SerializeField] private float penaltyForSavingAmmo = 0.01f;

    [Header("Projectile Avoidance Settings")]
    [SerializeField] private float projectileAvoidanceReward = 0.02f;
    [SerializeField] private float projectileDetectionRadius = 10f;
    [SerializeField] private int numRaycastsProjectiles = 8;

    [Header("Episode Settings")]
    [SerializeField] private float maxEpisodeTime = 60f;
    [SerializeField] private bool resetOnDeath = true;


    [Header("Training Statistics (Debug)")]
    [SerializeField] private int episodeCount = 0;
    [SerializeField] private int goalsReached = 0;
    [SerializeField] private int deaths = 0;

    #endregion

    #region Private Fields

    private PlayerSynchronizer playerSynchronizer;
    private Transform targetTransform;
    private Vector2 cachedTargetPosition = Vector2.zero;
    private bool hasPlayerTarget;
    private bool isTargetVisible = false;
    private Vector2 punishZone;
    private Vector2[] navigationPath;
    private int[] previousActions = new int[5];
    private int[] actionChangeCount = new int[5];
    private float[] actionHeldDuration = new float[5];
    private Vector2 targetDirectionCache = Vector2.right;
    private NavMeshPath navMeshPath;
    private Vector3 toNextTarget = Vector3.zero;
    private float episodeTimer;
    private float jumpExpense = 1f;
    private float shotDecay = 1f;

    // Debug draw cache
    private Vector2 lastAimDirection;
    private float lastAimRaycastDistance;
    private bool lastAimHitEnvironment;
    private Vector2 lastPredictedShotPosition;
    private bool lastShotWasAccurate;
    private float lastShotDistance;
    private bool shouldDrawShotDebug;
    private DebugVisualization debugFlags = DebugVisualization.None;
    private const float ActionFreq = 10f;

    #endregion

    #region Properties

    public Vector2 AimingDirection => playerController.playerBehaviour.aimDirection.normalized;
    public Vector2 AgentPosition
    {
        get
        {
            if (!playerController.playerBehaviour) Debug.Log("PlayerBehaviour is null wtf?!?");
            return playerController.playerBehaviour.transform.position;
        }
    }
    public Vector2 TargetPosition => cachedTargetPosition;
    public Vector2 TargetDirection => targetDirectionCache;
    public Vector2 AgentToTargetVector => TargetPosition - AgentPosition;
    public Vector2[] NavigationPath => navigationPath;

    public bool UpdateNavigationPath()
    {
        UpdateTargetSelection();

        Vector2 directDirection = TargetPosition - AgentPosition;

        bool pathSuccess = NavMesh.CalculatePath(AgentPosition, TargetPosition, 1 << 0, navMeshPath);

        if (!pathSuccess || navMeshPath.status == NavMeshPathStatus.PathInvalid || navMeshPath.corners == null || navMeshPath.corners.Length == 0)
        {
            targetDirectionCache = directDirection.sqrMagnitude > 0.0001f ? directDirection.normalized : Vector2.zero;
            return false;
        }
        if (navigationPath == null || navigationPath.Length != navMeshPath.corners.Length) navigationPath = new Vector2[navMeshPath.corners.Length];
        for (int i = 0; i < navMeshPath.corners.Length; i++) navigationPath[i] = navMeshPath.corners[i];
        if (navigationPath.Length >= 2) targetDirectionCache = (navigationPath[1] - AgentPosition).normalized;
        else targetDirectionCache = directDirection.sqrMagnitude > 0.0001f ? directDirection.normalized : Vector2.zero;
        return true;
    }

    private void UpdateNearestVisibleTarget()
    {
        if (playerSynchronizer == null || playerSynchronizer.playerIdentities == null || playerSynchronizer.playerIdentities.Count == 0)
        {
            targetTransform = null;
            return;
        }
        Physics2D.queriesStartInColliders = false;
        Vector2 playerPos = AgentPosition;
        float closestDistance = raycastDistance;
        targetTransform = null;
        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++)
        {
            PlayerSynchronizer.PlayerData item = playerSynchronizer.playerIdentities[i];
            if (item.square == null) continue;
            PlayerBehaviour otherPlayer = item.square;
            if (otherPlayer == playerController.playerBehaviour || otherPlayer.isDead) continue;
            Vector2 targetPos = otherPlayer.rb.position;
            Vector2 direction = targetPos - playerPos;
            float distance = direction.magnitude;
            if (distance <= raycastDistance)
            {
                RaycastHit2D hit = Physics2D.Raycast(playerPos, direction.normalized, distance, PhysicsMasks.ENVIRONTMENT_MASK);
                if (hit.collider == null && distance < closestDistance)
                {
                    closestDistance = distance;
                    targetTransform = otherPlayer.transform;
                }
            }
        }
        Physics2D.queriesStartInColliders = true;
    }


    #endregion

    #region Unity ML-Agents Lifecycle

    public void InitializeExtern()
    {
        navMeshPath = new NavMeshPath();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        playerController = GetComponent<PlayerController>();
        cachedTargetPosition = AgentPosition;
        UpdateTargetSelection();
    }

    public override void OnEpisodeBegin()
    {
        episodeCount++;
        episodeTimer = 0f;
        shouldDrawShotDebug = false;
        targetTransform = null;
        hasPlayerTarget = false;
        isTargetVisible = false;

        for (int i = 0; i < 5; i++)
        {
            actionChangeCount[i] = 0;
            actionHeldDuration[i] = 0f;
            previousActions[i] = 0;
        }

        if (playerController?.playerBehaviour != null)
            playerController.playerBehaviour.RespawnPlayer();

        punishZone = AgentPosition;
        cachedTargetPosition = AgentPosition;

        if (playerController != null)
        {
            playerController.HandleLeft(false);
            playerController.HandleRight(false);
            playerController.HandleUp(false);
            playerController.HandleDown(false);
        }

        UpdateTargetSelection();
    }

    const int MOVE_HORIZONTAL_T = 0;
    const int MOVE_VERTICAL_T = 1;
    const int MOVE_JUMP_B = 2;
    const int PERFORM_PRIMARY_B = 3;
    const int PERFORM_SECONDARY_B = 4;

    public override void OnActionReceived(ActionBuffers actions)
    {
        Span<int> currentActions = stackalloc int[5];
        currentActions[MOVE_HORIZONTAL_T] =

       currentActions[MOVE_HORIZONTAL_T] = GetFilteredAction(actions.DiscreteActions[0], TrainingCategory.HorizontalMovement);
        currentActions[MOVE_VERTICAL_T] = GetFilteredAction(actions.DiscreteActions[1], TrainingCategory.VerticalMovement);
        currentActions[MOVE_JUMP_B] = GetFilteredAction(actions.DiscreteActions[2], TrainingCategory.Jump);
        currentActions[PERFORM_PRIMARY_B] = GetFilteredAction(actions.DiscreteActions[3], TrainingCategory.PrimaryAttack);
        currentActions[PERFORM_SECONDARY_B] = GetFilteredAction(actions.DiscreteActions[4], TrainingCategory.SecondaryAttack);
        ApplyMovementActions(currentActions[MOVE_HORIZONTAL_T], currentActions[MOVE_VERTICAL_T]);
        ApplyAbilityActions(currentActions[MOVE_JUMP_B], currentActions[PERFORM_PRIMARY_B], currentActions[PERFORM_SECONDARY_B]);
        CalculateMovementRewards();
        CalculateScenarioRewards(currentActions);
        CheckEpisodeEnd();

        for (int i = 0; i < 5; i++) previousActions[i] = currentActions[i];
    }

    #endregion

    #region Target Selection

    private void UpdateTargetSelection()
    {
        // Only update if we don't have a valid target
        if (targetTransform != null)
        {
            var targetPb = targetTransform.GetComponent<PlayerBehaviour>();
            // Keep current target if it's still alive
            if (targetPb != null && !targetPb.isDead)
            {
                cachedTargetPosition = targetTransform.position;
                hasPlayerTarget = true;
                return;
            }
        }

        // Target is dead/null, find a new one
        Transform livePlayer = FindNearestLivePlayer();
        if (livePlayer != null)
        {
            targetTransform = livePlayer;
            cachedTargetPosition = livePlayer.position;
            hasPlayerTarget = true;
        }
        else
        {
            targetTransform = null;
            cachedTargetPosition = FindOpenAreaTarget();
            hasPlayerTarget = false;
        }
    }

    private Transform FindNearestLivePlayer()
    {
        if (playerSynchronizer == null || playerSynchronizer.playerIdentities == null || playerSynchronizer.playerIdentities.Count == 0) return null;
        Vector2 playerPos = AgentPosition;
        PlayerBehaviour playerBehaviour = playerSynchronizer.GetFurthestPlayer(AgentPosition, playerController.playerBehaviour.GetGameID(), false);
        if (playerBehaviour) return playerBehaviour.transform;
        return null;
    }

    private Vector2 FindOpenAreaTarget()
    {
        Vector2 playerPos = AgentPosition;
        float bestScore = 0f;
        Vector2 bestDirection = Vector2.right;
        float angleStep = 360f / openAreaRaycastCount;
        for (int i = 0; i < openAreaRaycastCount; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            RaycastHit2D hit = Physics2D.CircleCast(playerPos, 0.3f, direction, openAreaSearchRadius, PhysicsMasks.ENVIRONTMENT_MASK);
            float distanceToWall = hit.collider != null ? hit.distance : openAreaSearchRadius;
            if (distanceToWall >= minSafeDistanceFromWall)
            {
                float score = distanceToWall;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }
        }
        float targetDistance = Mathf.Min(bestScore * 0.7f, openAreaSearchRadius * 0.5f);
        return playerPos + bestDirection * targetDistance;
    }

    private void UpdateTargetVisibility()
    {
        if (!hasPlayerTarget || targetTransform == null)
        {
            isTargetVisible = false;
            return;
        }
        Physics2D.queriesStartInColliders = false;
        Vector2 playerPos = AgentPosition;
        Vector2 targetPos = targetTransform.position;
        Vector2 direction = targetPos - playerPos;
        float distance = direction.magnitude;
        RaycastHit2D hit = Physics2D.Raycast(playerPos, direction.normalized, distance, PhysicsMasks.ENVIRONTMENT_MASK);
        isTargetVisible = hit.collider == null;
        Physics2D.queriesStartInColliders = true;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        UpdateNavigationPath();
        UpdateTargetVisibility();

        CollectSelfKinematicObservations(sensor);
        CollectSelfStatisticsObservations(sensor);
        CollectArenaObservations(sensor);
        CollectTargetObservations(sensor);
        CollectAimingObservations(sensor);
        CollectOpponentObservations(sensor);
        CollectVisibilityObservation(sensor);
        CollectProjectileObservations(sensor);
    }

    #endregion

    #region Observation Collection

    private void CollectSelfKinematicObservations(VectorSensor sensor)
    {
        var rb = playerController.playerBehaviour.rb;
        var pb = playerController.playerBehaviour;

        float angleRad = rb.rotation * Mathf.Deg2Rad;
        AddObservationSafe(sensor, -1f, 1f, Mathf.Sin(angleRad));
        AddObservationSafe(sensor, -1f, 1f, Mathf.Cos(angleRad));
        AddObservationSafe(sensor, -720f, 720f, rb.angularVelocity);
        AddObservationSafe(sensor, -pb.maxSpeed, pb.maxSpeed, rb.linearVelocity);
        AddObservationSafe(sensor, 0f, 5f, jumpExpense);
        AddObservationSafe(sensor, 0f, 1f, pb.hasJump ? 1f : 0f);
    }

    private void CollectSelfStatisticsObservations(VectorSensor sensor)
    {
        var pb = playerController.playerBehaviour;
        WeaponStats primaryStats = pb.GetWeaponStats(true);
        WeaponStats secondaryStats = pb.GetWeaponStats(false);

        AddObservationSafe(sensor, 0f, 1f, primaryStats.isMelee ? 1f : 0f);
        AddObservationSafe(sensor, 0f, primaryStats.maxAmmo, primaryStats.remainingAmmo, 1f);
        AddObservationSafe(sensor, 0f, 100f, primaryStats.projectileSpeed);
        AddObservationSafe(sensor, 0f, 20f, primaryStats.projectileAcceleration);
        AddObservationSafe(sensor, -10f, 10f, primaryStats.projectileGravity);

        AddObservationSafe(sensor, 0f, 1f, secondaryStats.isMelee ? 1f : 0f);
        AddObservationSafe(sensor, 0f, secondaryStats.maxAmmo, secondaryStats.remainingAmmo, 1f);
        AddObservationSafe(sensor, 0f, ActionFreq, secondaryStats.projectileSpeed);
        AddObservationSafe(sensor, 0f, 20f, secondaryStats.projectileAcceleration);
        AddObservationSafe(sensor, -10f, 10f, secondaryStats.projectileGravity);

        AddObservationSafe(sensor, 0, 3, previousActions[MOVE_HORIZONTAL_T]);
        AddObservationSafe(sensor, 0, 3, previousActions[MOVE_VERTICAL_T]);
        AddObservationSafe(sensor, 0, 2, previousActions[MOVE_JUMP_B]);
        AddObservationSafe(sensor, 0, 2, previousActions[PERFORM_PRIMARY_B]);
        AddObservationSafe(sensor, 0, 2, previousActions[PERFORM_SECONDARY_B]);
    }

    private void CollectArenaObservations(VectorSensor sensor)
    {
        Vector3 playerPos = AgentPosition;
        float angleStep = 360f / numRaycastsEnvironment;
        float radius = 0.3f;

        for (int i = 0; i < numRaycastsEnvironment; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            RaycastHit2D hit = Physics2D.CircleCast(playerPos, radius, direction, raycastDistance, PhysicsMasks.ENVIRONTMENT_MASK);

            float distance = hit.collider != null ? hit.distance : raycastDistance;
            AddObservationSafe(sensor, 0f, raycastDistance, distance);
        }
    }

    private void CollectTargetObservations(VectorSensor sensor)
    {
        toNextTarget = TargetDirection;
        Vector2 normalized = toNextTarget.normalized;
        AddObservationSafe(sensor, -1f, 1f, normalized);
    }

    private void CollectAimingObservations(VectorSensor sensor)
    {
        Vector2 aimDir = AimingDirection;
        float radius = 0.3f;

        AddObservationSafe(sensor, -1f, 1f, aimDir);

        RaycastHit2D hit = Physics2D.CircleCast(AgentPosition, radius, aimDir, raycastDistance, PhysicsMasks.ENVIRONTMENT_MASK);
        float distance = hit.collider != null ? hit.distance : raycastDistance;
        AddObservationSafe(sensor, 0f, raycastDistance, distance);

        lastAimDirection = aimDir;
        lastAimHitEnvironment = hit.collider != null;
        lastAimRaycastDistance = distance;
    }

    private void CollectOpponentObservations(VectorSensor sensor)
    {
        UpdateNearestVisibleTarget();

        if (targetTransform != null)
        {
            Vector2 relativePos = (Vector2)targetTransform.position - AgentPosition;
            AddObservationSafe(sensor, -raycastDistance, raycastDistance, relativePos);

            Vector2 targetVelocity = targetTransform.GetComponent<Rigidbody2D>().linearVelocity;
            AddObservationSafe(sensor, -23f, 23f, targetVelocity);
        }
        else
        {
            AddObservationSafe(sensor, -raycastDistance, raycastDistance, Vector2.zero);
            AddObservationSafe(sensor, -23f, 23f, Vector2.zero);
        }
    }

    private void CollectVisibilityObservation(VectorSensor sensor)
    {
        AddObservationSafe(sensor, 0f, 1f, isTargetVisible ? 1f : 0f);
    }

    private void CollectProjectileObservations(VectorSensor sensor)
    {
        Vector2 playerPos = AgentPosition;
        float angleStep = 360f / numRaycastsProjectiles;
        float radius = 0.5f;

        for (int i = 0; i < numRaycastsProjectiles; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;

            RaycastHit2D hit = Physics2D.CircleCast(playerPos, radius, direction, projectileDetectionRadius, PhysicsMasks.PROJECTILE_MASK);

            if (hit.collider != null)
            {
                ProjectileBehaviour projectile = hit.collider.GetComponent<ProjectileBehaviour>();

                if (projectile != null && projectile.owningPlayer != null)
                {
                    int ownerId = projectile.owningPlayer.GetGameID();
                    int agentId = playerController.playerBehaviour.GetGameID();

                    if (ownerId == agentId)
                    {
                        AddObservationSafe(sensor, 0f, projectileDetectionRadius, projectileDetectionRadius);
                    }
                    else
                    {
                        AddObservationSafe(sensor, 0f, projectileDetectionRadius, hit.distance);
                    }
                }
                else
                {
                    AddObservationSafe(sensor, 0f, projectileDetectionRadius, hit.distance);
                }
            }
            else
            {
                AddObservationSafe(sensor, 0f, projectileDetectionRadius, projectileDetectionRadius);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddObservationSafe(VectorSensor sensor, float min, float max, Vector2 value, float defaultValue = 0f)
    {
        AddObservationSafe(sensor, min, max, value.x, defaultValue);
        AddObservationSafe(sensor, min, max, value.y, defaultValue);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddObservationSafe(VectorSensor sensor, float min, float max, float value, float defaultValue = 0f)
    {
        float normalizedValue = 2f * Mathf.Clamp01(Mathf.InverseLerp(min, max, value)) - 1f;
        if (float.IsNaN(normalizedValue) || float.IsInfinity(normalizedValue)) normalizedValue = defaultValue;
        sensor.AddObservation(normalizedValue);
    }

    #endregion

    #region Action Processing

    private int GetFilteredAction(int actionValue, TrainingCategory category)
    {
        bool isEnabled = currentTrainingMode switch
        {
            TrainingMode.FullControl => true,
            TrainingMode.MovementOnly =>
                category == TrainingCategory.HorizontalMovement ||
                category == TrainingCategory.VerticalMovement,
            TrainingMode.AbilitiesOnly =>
                category == TrainingCategory.Jump ||
                category == TrainingCategory.PrimaryAttack ||
                category == TrainingCategory.SecondaryAttack,
            TrainingMode.MovementAndJump =>
                category == TrainingCategory.HorizontalMovement ||
                category == TrainingCategory.VerticalMovement ||
                category == TrainingCategory.Jump,
            TrainingMode.CombatOnly =>
                category == TrainingCategory.PrimaryAttack ||
                category == TrainingCategory.SecondaryAttack,
            TrainingMode.Custom => category switch
            {
                TrainingCategory.HorizontalMovement => trainHorizontalMovement,
                TrainingCategory.VerticalMovement => trainVerticalMovement,
                TrainingCategory.Jump => trainJump,
                TrainingCategory.PrimaryAttack => trainPrimaryAttack,
                TrainingCategory.SecondaryAttack => trainSecondaryAttack,
                _ => false
            },
            _ => false
        };
        return isEnabled ? actionValue : 0;
    }

    private void ApplyMovementActions(int horizontal, int vertical)
    {
        playerController.HandleLeft(false);
        playerController.HandleRight(false);
        playerController.HandleDown(false);
        playerController.HandleUp(false);
        switch (horizontal)
        {
            case 1: playerController.HandleLeft(true); break;
            case 2: playerController.HandleRight(true); break;
        }

        switch (vertical)
        {
            case 1: playerController.HandleDown(true); break;
            case 2: playerController.HandleUp(true); break;
        }
    }

    private void ApplyAbilityActions(int jump, int primary, int secondary)
    {
        jumpExpense = playerController.OnJumpPerformed(jump == 1) ? 5f : jumpExpense;
        bool shotPrimary = primary == 1;
        bool shotSecondary = secondary == 1;
        if (shotPrimary || shotSecondary) EvaluateShot(shotPrimary);
        playerController.OnPrimaryPerformed(shotPrimary);
        playerController.OnSecondaryPerformed(shotSecondary);
    }

    #endregion

    #region Reward Calculation

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GiveReward(float reward)
    {
        if(isTraining) AddReward(reward);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CalculateFallingIntoVoidPenalty()
    {
        PlayerBehaviour pb = playerController.playerBehaviour;
        Rigidbody2D rb = pb.rb;

        if (Vector2.Angle(rb.linearVelocity.normalized, Vector2.down) <= 45f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(AgentPosition, 0.3f, Vector2.down, 5f, PhysicsMasks.ENVIRONTMENT_MASK);
            if (hit.collider == null) GiveReward(-penaltyForFallingIntoVoid);
        }
    }

    private void CalculateAmmoNotUsedInCorrectSituationReward()
    {
        PlayerBehaviour pb = playerController.playerBehaviour;
        Rigidbody2D rb = pb.rb;

        if (isTargetVisible && targetTransform != null)
        {
            WeaponStats primary, secondary;
            primary = pb.GetWeaponStats(true);
            secondary = pb.GetWeaponStats(false);

            float decayVal = -penaltyForSavingAmmo * shotDecay;

            GiveReward(decayVal * (primary.remainingAmmo / primary.maxAmmo));
            GiveReward(decayVal * (secondary.remainingAmmo / secondary.maxAmmo));
        }
    }

    private void CalculatePenaltyForBeingNearProjectiles()
    {
        Vector2 playerPos = AgentPosition;
        Vector2 velocity = playerController.playerBehaviour.rb.linearVelocity;

        if (velocity.sqrMagnitude < 0.01f) return;

        float angleStep = 360f / numRaycastsProjectiles;
        float radius = 0.5f;

        float totalReward = 0f;
        int threateningProjectiles = 0;

        for (int i = 0; i < numRaycastsProjectiles; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;

            RaycastHit2D hit = Physics2D.CircleCast(playerPos, radius, direction, projectileDetectionRadius, PhysicsMasks.PROJECTILE_MASK);

            if (hit.collider != null)
            {
                ProjectileBehaviour projectile = hit.collider.GetComponent<ProjectileBehaviour>();

                if (projectile != null && projectile.owningPlayer != null)
                {
                    int ownerId = projectile.owningPlayer.GetGameID();
                    int agentId = playerController.playerBehaviour.GetGameID();
                    if (ownerId == agentId) continue;
                    Vector2 projectilePos = hit.point;
                    Vector2 toProjectile = (projectilePos - playerPos).normalized;
                    float movementAlignment = Vector2.Dot(velocity.normalized, toProjectile);
                    float proximityWeight = 1f - (hit.distance / projectileDetectionRadius);
                    totalReward -= movementAlignment * proximityWeight;
                    threateningProjectiles++;
                }
            }
        }

        if (threateningProjectiles > 0) GiveReward((totalReward / threateningProjectiles) * projectileAvoidanceReward * jumpExpense);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CalculateScenarioRewards(Span<int> currentActions)
    {

        //Situation 1. Void beneath and falling! AVOID IT...
        CalculateFallingIntoVoidPenalty();

        //Situation 2. We have ammo! USE IT...
        CalculateAmmoNotUsedInCorrectSituationReward();

        //Situation 3. Enemy projectiles nearby! DODGE THEM...
        CalculatePenaltyForBeingNearProjectiles();

        TrackActionConsistency(currentActions, 1f / ActionFreq);

    }

    private void TrackActionConsistency(Span<int> currentActions, float deltaTime)
    {
        for (int i = 0; i < 5; i++)
        {
            if (currentActions[i] != previousActions[i])
            {
                actionChangeCount[i]++;

                // Penalize if action wasn't held long enough
                if (actionHeldDuration[i] < MIN_ACTION_HOLD_TIME && previousActions[i] != 0)
                {
                    GiveReward(-actionChangeSpamPenalty);
                }

                actionHeldDuration[i] = 0f;
            }
            else
            {
                // Reward for holding action consistently
                actionHeldDuration[i] += deltaTime;

                if (actionHeldDuration[i] >= MIN_ACTION_HOLD_TIME && currentActions[i] != 0)
                {
                    GiveReward(actionConsistencyReward * deltaTime);
                }
            }
        }
    }

    // Add new method
    private void PenalizeActionSpam(float deltaTime)
    {
        // Calculate changes per second
        float totalChanges = 0;
        for (int i = 0; i < actionChangeCount.Length; i++)
        {
            totalChanges += actionChangeCount[i];
        }

        float changesPerSecond = totalChanges / Mathf.Max(episodeTimer, 0.1f);

        if (changesPerSecond > maxActionChangesPerSecond)
        {
            float excessChanges = changesPerSecond - maxActionChangesPerSecond;
            GiveReward(-actionChangeSpamPenalty * excessChanges * deltaTime);
        }
    }


    private void CalculateMovementRewards()
    {
        Vector2 velocity = playerController.playerBehaviour.rb.linearVelocity;
        Vector2 currentAgentPos = AgentPosition;

        float distToZone = Vector2.Distance(currentAgentPos, punishZone);
        if (distToZone < 2f)
        {
            GiveReward(-punishZoneMultiplier * (1f - (distToZone / 2f)));
        }

        CalculateEnvironmentProximityReward();

        float navigationAlignment = Vector2.Dot(velocity.normalized, toNextTarget.normalized);
        float positioningScore = CalculatePositioningScore();

        if (isTargetVisible)
        {
            GiveReward(navigationAlignment * alignmentRewardMultiplier);
            GiveReward(positioningScore * positioningMultiplier * positioningWeightWhenVisible);
        }
        else
        {
            GiveReward(navigationAlignment * alignmentRewardMultiplier * navigationWeightWhenOccluded);
            GiveReward(positioningScore * positioningMultiplier);
        }

        if (!hasPlayerTarget && Vector2.Distance(currentAgentPos, TargetPosition) < 2f)
            OnGoalReached();
    }

    private void CalculateEnvironmentProximityReward()
    {
        Vector2 playerPos = AgentPosition;
        float closestEnvironmentDistance = float.MaxValue;
        float angleStep = 360f / numRaycastsEnvironment;
        float radius = 0.3f;

        for (int i = 0; i < numRaycastsEnvironment; i++)
        {
            float angle = i * angleStep;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            RaycastHit2D hit = Physics2D.CircleCast(playerPos, radius, direction, environmentProximityThreshold, PhysicsMasks.ENVIRONTMENT_MASK);

            if (hit.collider != null && hit.distance < closestEnvironmentDistance)
            {
                closestEnvironmentDistance = hit.distance;
            }
        }

        if (closestEnvironmentDistance <= environmentProximityThreshold)
        {
            GiveReward(environmentProximityReward);
        }
        else
        {

            float distanceOverThreshold = closestEnvironmentDistance - environmentProximityThreshold;
            float penalty = -environmentProximityReward * Mathf.Min(distanceOverThreshold / environmentProximityThreshold, 1f);
            GiveReward(penalty);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculatePositioningScore()
    {
        float angleDiff = Vector2.Angle(Vector2.right, TargetDirection.normalized);
        angleDiff = (angleDiff + 180f) % 45f / 45f;
        return Mathf.Abs(Mathf.Lerp(-1f, 1f, angleDiff)) * 2f - 1f;
    }

    private void EvaluateShot(bool isPrimary)
    {
        shotDecay = 0f;
        if (targetTransform == null)
        {
            GiveReward(-weaponBlindFirePenalty);
            lastShotWasAccurate = false;
            return;
        }

        var targetPb = targetTransform.GetComponent<PlayerBehaviour>();
        if (targetPb == null || targetPb.isDead)
        {
            targetTransform = null;
            return;
        }


        Vector2 playerPos = AgentPosition;
        Vector2 targetPos = targetTransform.position;
        Vector2 targetVel = targetPb.rb.linearVelocity;
        float projectileSpeed = playerController.playerBehaviour.GetWeaponStats(isPrimary).projectileSpeed;
        float dist = Vector2.Distance(playerPos, targetPos);
        float travelTime = dist / Mathf.Max(projectileSpeed, 10f);
        Vector2 predictedPos = targetPos + (targetVel * travelTime);
        Vector2 dirToPrediction = (predictedPos - playerPos).normalized;
        Vector2 currentAimDir = AimingDirection.normalized;


        float diagonalFactor = 1f - Mathf.Abs(Mathf.Abs(currentAimDir.x) - Mathf.Abs(currentAimDir.y));
        float currentBias = Mathf.Lerp(1f, diagonalBiasMultiplier, diagonalFactor);


        float angleDiff = Vector2.Angle(currentAimDir, dirToPrediction);
        float zeroPoint = 15f;


        float scaledAccuracy = 1f - (angleDiff / zeroPoint);


        float finalAccuracyScore = Mathf.Clamp(scaledAccuracy, -1f, 1f);

        if (finalAccuracyScore > 0)
        {

            float totalReward = finalAccuracyScore * weaponLeadingMultiplier * currentBias;
            GiveReward(totalReward);
            lastShotWasAccurate = true;
        }
        else
        {

            GiveReward(Mathf.Abs(finalAccuracyScore) * -weaponBlindFirePenalty);
            lastShotWasAccurate = false;
        }

        lastPredictedShotPosition = predictedPos;
        lastShotDistance = dist;
        shouldDrawShotDebug = true;
    }

    #endregion

    #region Episode Management
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckEpisodeEnd()
    {
        if (episodeTimer >= maxEpisodeTime && isTraining)
        {
            EndEpisode();
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnPlayerDeath()
    {
        deaths++;
        GiveReward(-deathPenalty);

        if (resetOnDeath)
        {
            EndEpisode();
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnGoalReached()
    {
        goalsReached++;
        GiveReward(goalReward);
        EndEpisode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCombatHit(bool hitPlayer)
    {
        GiveReward(hitPlayer ? combatHitReward : -combatHitReward);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnDamageDealt(float damage)
    {
        GiveReward(damage * combatDamageReward);
    }

    #endregion

    #region External Updates
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateExtern(float elapsed, float elapsedScaled)
    {
        if(isTraining) episodeTimer += elapsedScaled;
        jumpExpense = Mathf.Lerp(jumpExpense, 1f, elapsedScaled * 10f);
        shotDecay = Mathf.Lerp(shotDecay, 1f, elapsedScaled);
        UpdatePunishZone(elapsedScaled);
        DrawDebugVisualizations(elapsed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdatePunishZone(float elapsed)
    {
        Vector2 toAgent = AgentPosition - punishZone;
        float distance = toAgent.magnitude;

        if (distance > 0.001f)
        {
            punishZone += toAgent.normalized * (speedOfThatPunishZone * elapsed);
        }
    }

    private void DrawDebugVisualizations(float elapsed)
    {
        Vector2 playerPos = AgentPosition;

        // Draw target direction
        if (HasDebugFlag(DebugVisualization.TargetDirection))
        {
            Debug.DrawRay(playerPos, toNextTarget, Color.yellow, elapsed);
        }

        // Draw aim direction
        if (HasDebugFlag(DebugVisualization.AimDirection))
        {
            Color color = lastAimHitEnvironment ? Color.red : Color.cyan;
            Debug.DrawRay(playerPos, lastAimDirection * lastAimRaycastDistance, color, elapsed);
        }

        // Draw shot evaluation
        if (shouldDrawShotDebug)
        {
            if (HasDebugFlag(DebugVisualization.ShotAccuracy))
            {
                Color shotColor = lastShotWasAccurate ? Color.green : Color.red;
                Debug.DrawRay(playerPos, lastAimDirection * lastShotDistance, shotColor, elapsed);
            }

            if (HasDebugFlag(DebugVisualization.PredictedShot))
            {
                Debug.DrawLine(playerPos, lastPredictedShotPosition, Color.cyan, elapsed);
                DrawCross(lastPredictedShotPosition, 0.5f, Color.green, elapsed);
            }
        }

        // Draw punish zone
        if (HasDebugFlag(DebugVisualization.PunishZone))
        {
            DrawCircle(punishZone, 2f, Color.red, elapsed);
        }

        // Draw NavMesh path
        if (HasDebugFlag(DebugVisualization.NavMeshPath))
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(AgentPosition, TargetPosition, 1 << 0, path))
            {
                for (int i = 0; i < path.corners.Length - 1; i++) Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.magenta, elapsed);
            }
        }

        if (HasDebugFlag(DebugVisualization.TargetDirection))
        {
            Debug.DrawLine(playerPos, TargetPosition, Color.yellow, elapsed);
        }

        if (HasDebugFlag(DebugVisualization.TargetRadius))
        {
            DrawCircle(TargetPosition, 2f, Color.green, elapsed);
        }

        if (HasDebugFlag(DebugVisualization.RaycastRange))
        {
            DrawCircle(playerPos, raycastDistance, Color.blue, elapsed);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RequestDecisionExtern()
    {
        RequestDecision();
    }

    #endregion

    #region Debug Helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDebugFlag(DebugVisualization flag) => debugFlags = flag;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool HasDebugFlag(DebugVisualization flag)
    {
        return (debugFlags & flag) == flag;
    }

    private void DrawCircle(Vector2 center, float radius, Color color, float duration)
    {
        const int segments = 32;
        float angleStep = Mathf.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            Vector2 p1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
            Vector2 p2 = center + new Vector2(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
            Debug.DrawLine(p1, p2, color, duration);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawCross(Vector2 center, float size, Color color, float duration)
    {
        Debug.DrawLine(center + Vector2.up * size, center + Vector2.down * size, color, duration);
        Debug.DrawLine(center + Vector2.left * size, center + Vector2.right * size, color, duration);
    }

    internal void OnHpLoss(float delta)
    {
        AddReward(delta * healthLossPenalty);
    }

    #endregion
}