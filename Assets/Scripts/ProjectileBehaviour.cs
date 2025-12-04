using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using static PlayerSynchronizer;
using static UnityEngine.ParticleSystem;
using Color = UnityEngine.Color;

public sealed class ProjectileBehaviour : MonoBehaviour
{

    public const Int32 ENVIRONTMENT_MASK = 0b00000000000000000000001000000000;

    public float initDamage;

    [SerializeField] Transform boom;

    public float damageScaleOverTime;

    public float damage;
    public float aoeDamage;

    public float syncTimer;

    public float speedModifier;

    public uint projectileID;

    public bool IsLocalProjectile;

    [SerializeField]
    [HideInInspector]
    public Rigidbody2D rb;

    public PlayerBehaviour owningPlayer;

    public float timeAlive;
    public float fullTimeAlive;

    public ProjectileManager projectileManager;

    public bool holdable;

    public float travelDistance;
    Vector2 lastPos;

    [SerializeField]
    public float recoil;

    public bool destroyed;

    public bool returnToSender;
    public bool stickToSender;

    public bool instaDestroy = false;

    public bool skipAoeOnHit;

    public byte ownerId;

    public bool melee;

    public bool hit;
    public bool sync;
    public bool flipFlop;

    const string paramNameCameraPositionX = "CameraPositionX";

    bool stuck;
    GameObject stuckTo;

    [SerializeField]
    public HitMarkBehaviour hitMark;

    [SerializeField]
    [HideInInspector]
    ParticleSystemRenderer trailParticles;
    [SerializeField]
    [HideInInspector]
    ParticleSystem trailParticleSystem;
    MainModule trailMainModule;

    [SerializeField]
    [HideInInspector]
    SpriteRenderer spriteRenderer;

    CameraAnimator cameraAnimator;

    UnityEngine.Color generalParticleColor;

    [SerializeField]
    EventReference shotReference;

    [SerializeField] EventReference aliveReference;

    [SerializeField] bool aliveSound;

    [SerializeField]
    EventReference hitSoundReference;

    EventInstance shotInstance;
    EventInstance aliveInstance;

    PlayerBehaviour playerHit;
    PlayerBehaviour closestPlayer = null;
    FlagBehaviour flagHit;

    List<PlayerBehaviour> playersHit;
    List<FlagBehaviour> flagsHit;

    [SerializeField]
    [HideInInspector]
    Collider2D projectileCollider;

    [SerializeField]
    public ProjectileInitData data;

    [SerializeField]
    ProjectileTrailBehaviour projectileTrailBehaviour;

    [SerializeField]
    ExternalTrailBehaviour externalTrailRef;

    [SerializeField]
    bool multiplySpawnrateByLifetime;

    [SerializeField]
    float lifeTimeMultiplier;

    [SerializeField]
    float externalTrailSpawnRate;
    float externalTrailSpawnTimer;

    float morphLerp;
    Vector3 startMorph;
    Vector3 endMorph;

    Vector2 meleeStartDirection;
    Vector2 meleeEndDirection;
    float meleeStartRot;
    float meleeEndRot;
    float initRot;
    public bool playShootSound;

    private void OnValidate()
    {
        projectileCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailParticles = GetComponentInChildren<ParticleSystemRenderer>();
        trailParticleSystem = trailParticles.GetComponent<ParticleSystem>();
    }

    private void Awake()
    {
        if (trailParticleSystem) trailMainModule = trailParticleSystem.main;
        cameraAnimator = Camera.main.GetComponent<CameraAnimator>();
        playersHit = new List<PlayerBehaviour>(4);
        flagsHit = new List<FlagBehaviour>(4);
        playersCollidingWith = new List<PlayerBehaviour>(4);
        spriteRenderer.sprite = AssetResources.GetSmallCornerOctagon;
    }

    private void Start()
    {
        if (!playShootSound) return;
        float pitch = 1f + UnityEngine.Random.Range(-0.08f, 0.08f);

        shotInstance = RuntimeManager.CreateInstance(shotReference);
        shotInstance.setParameterByName(paramNameCameraPositionX, transform.position.x - Camera.main.transform.position.x);
        shotInstance.setParameterByName("Power", data.speed / 65f);
        shotInstance.setVolume(MySettings.volume);
        shotInstance.setPitch(pitch);
        shotInstance.start();

        if (aliveSound)
        {
            aliveInstance = RuntimeManager.CreateInstance(aliveReference);
            aliveInstance.setParameterByName(paramNameCameraPositionX, transform.position.x - Camera.main.transform.position.x);
            aliveInstance.setParameterByName("Power", data.speed / 65f);
            aliveInstance.setVolume(MySettings.volume);
            aliveInstance.setPitch(pitch);
            aliveInstance.start();
        }

        if (cameraAnimator && IsLocalProjectile) cameraAnimator.Shake();
    }


    public void InitializeBullet(ref ProjectileInitData data)
    {

        initDamage = data.baseDamage;
        aoeDamage = data.aoeDamage * Mods.at[7];
        damageScaleOverTime = data.damageTimeScale;
        skipAoeOnHit = data.skipAoeOnTargetHit;
        stickToSender = data.stickToSender;

        endMorph = data.targetMorph;
        startMorph = transform.localScale;
        owningPlayer = data.owningPlayer;

        sync = data.sync;

        data.id++;


        owningPlayer.PlayerColor.AssignMaterialToParticleRenderer(trailParticles, trailParticleSystem);
        owningPlayer.PlayerColor.AssignMaterialToProjectile(spriteRenderer);
        

        Vector2 initialOffset = new Vector2(data.fluctuation[0], data.fluctuation[1]);
        data.direction = (data.direction + initialOffset).normalized;

        float rotation = math.degrees(math.atan2(data.direction.y, data.direction.x));
        Quaternion rotationQ = Quaternion.Euler(0, 0, rotation);
        Vector2 velocity = data.direction * data.speed;
        Vector2 position = data.position;

        projectileID = data.id;

        projectileManager = data.projectileManager;
        IsLocalProjectile = data.IsLocalProjectile;

        chargePlayerEndScale = transform.localScale;

        rb.linearVelocity = velocity;
        rb.angularVelocity = data.spinSpeed;
        rb.position = position;
        rb.rotation = rotation;

        if (stickToSender)
        {
            owningPlayer.rb.linearVelocity = velocity;
            rb.position = owningPlayer.rb.position;
        }

        startRotate = rb.rotation;
        rotate = startRotate + 1000;

        transform.position = position;
        transform.rotation = rotationQ;

        if (data.noGravity) rb.gravityScale = 0f;
        else rb.gravityScale *= Mods.at[5];

        if (stickToSender)
        {
            spriteRenderer.color = owningPlayer.PlayerColor.ProjectileColor;
            generalParticleColor = owningPlayer.PlayerColor.ParticleColor;
            owningPlayer.nozzleBehaviour.transform.localScale = Vector3.zero;
        }
        else
        {
            spriteRenderer.color = owningPlayer.PlayerColor.ProjectileColor;
            generalParticleColor = owningPlayer.PlayerColor.ParticleColor;
        }

        if (trailParticles) owningPlayer.PlayerColor.AssignMaterialToParticleRenderer(trailParticles, trailParticles.GetComponent<ParticleSystem>());

        damage = initDamage * Mods.at[4];
        speedModifier = data.acceleration;
        melee = data.melee;

        if (flipFlop)
        {
            meleeStartDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation + (data.swingDegrees / 2f));
            meleeEndDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation - (data.swingDegrees / 2f));

            meleeStartRot = data.meleeRotation / 2f;
            meleeEndRot = -data.meleeRotation / 2f;
        }
        else
        {
            meleeStartDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation - (data.swingDegrees / 2f));
            meleeEndDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation + (data.swingDegrees / 2f));

            meleeStartRot = -data.meleeRotation / 2f;
            meleeEndRot = data.meleeRotation / 2f;
        }

        initRot = rb.rotation;
        this.data = data;
        if (!IsLocalProjectile) gameObject.layer = LayerMask.NameToLayer("RemoteProjectile");
        projectileManager.projectiles.Add(this);
        lastPos = rb.position;
        rb.linearVelocity *= Mods.at[3];
        this.data.knockback *= Mods.at[12];
        SetupAllProxySpawns(ProjectileSpawnEvent.EventType.Birth);
    }

    void SetupAllProxySpawns(ProjectileSpawnEvent.EventType filterEventType)
    {
        for (int i = 0; i < data.projectileSpawnEvents.Length; i++) SetupProxySpawn(data.projectileSpawnEvents[i], filterEventType);
    }

    void SetupProxySpawn(ProjectileSpawnEvent spawnEvent, ProjectileSpawnEvent.EventType filterEventType)
    {
        ProjectileSpawnEvent.EventType eventType = spawnEvent.eventType;
        ProjectileSpawnEvent.EventDirection eventDirection = spawnEvent.eventDirection;

        if (eventType != filterEventType) return;

        spawnEvent.Ensure(ref spawnEvent);
        spawnEvent.SetManager(data.projectileManager);
        spawnEvent.SetShootingPlayer(owningPlayer);

        ProjectileSpawnEvent.GetSetVec2Stream directionStream = (_, __) => new Vector2();
        ProjectileSpawnEvent.GetSetVec2Stream positionStream = (_, __) => new Vector2();

        if (eventDirection == ProjectileSpawnEvent.EventDirection.ClosestPlayer)
        {
            directionStream = GetSetStreamClosestPlayer;
        }
        if (eventDirection == ProjectileSpawnEvent.EventDirection.ClosestGround)
        {
            directionStream = GetSetStreamClosestGround;
        }
        if (eventDirection == ProjectileSpawnEvent.EventDirection.Velocity)
        {
            directionStream = GetSetStreamClosestVelocity;
        }

        spawnEvent.spawnPosition = rb.position;
        positionStream = (oldDirection, oldPosition) =>
        {
            if (this) if (rb) return rb.position;
            return oldPosition;
        };

        spawnEvent.SetGetSpawnDirection(directionStream);
        spawnEvent.SetGetSpawnPosition(positionStream);
        spawnEvent.Poll(ref spawnEvent);

        SpawnEventHandle spawnEventHandle = Instantiate(AssetResources.SpawnEventHandle);
        spawnEventHandle.Initialize(ref spawnEvent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector2 GetSetStreamClosestPlayer(Vector2 oldDirection, Vector2 oldPosition)
    {
        if (this && rb) return (projectileManager.playerSynchronizer.GetClosestPlayer(rb.position).position - rb.position).normalized;
        else return (projectileManager.playerSynchronizer.GetClosestPlayer(oldPosition).position - oldPosition).normalized;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector2 GetSetStreamClosestGround(Vector2 oldDirection, Vector2 oldPosition)
    {
        if (this && rb) return (GetClosestEnvironmentPoint(rb.position).point - rb.position).normalized;
        else return (GetClosestEnvironmentPoint(oldPosition).point - oldPosition).normalized;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector2 GetSetStreamClosestVelocity(Vector2 oldDirection, Vector2 oldPosition)
    {
        if (this && rb) return rb.linearVelocity.normalized;
        else return oldDirection;
    }

    private void Update()
    {
        if (IsLocalProjectile) LocalUpdate();
        GlobalUpdate();
    }

    float audioTimer;

    void GlobalUpdate()
    {
        if (audioTimer < 1) audioTimer += Time.deltaTime * 20;
        else
        {
            PLAYBACK_STATE playbackState;
            shotInstance.getPlaybackState(out playbackState);
            if (playbackState == PLAYBACK_STATE.PLAYING) shotInstance.setParameterByName(paramNameCameraPositionX, transform.position.x - Camera.main.transform.position.x);
            if (aliveSound)
            {
                aliveInstance.getPlaybackState(out playbackState);
                if (playbackState == PLAYBACK_STATE.PLAYING) aliveInstance.setParameterByName(paramNameCameraPositionX, transform.position.x - Camera.main.transform.position.x);
            }
            audioTimer = 0;
        }

        if (externalTrailRef)
        {
            if (owningPlayer)
            {
                float deltaTime = 0;
                if (multiplySpawnrateByLifetime) deltaTime = Time.deltaTime * math.lerp(1, 0, math.clamp(timeAlive / data.lifeTime, 0, 1)) * lifeTimeMultiplier;
                else deltaTime = Time.deltaTime;

                externalTrailSpawnTimer += deltaTime;

                while (externalTrailSpawnTimer > externalTrailSpawnRate)
                {
                    ExternalTrailBehaviour externalTrail = Instantiate(externalTrailRef, transform.position, transform.rotation, null);
                    if (externalTrail) externalTrail.Play(owningPlayer.PlayerColor.ParticleColor, owningPlayer.id, owningPlayer);
                    externalTrailSpawnTimer -= externalTrailSpawnRate;
                }
            }
        }

        if (data.homing)
        {

            closestPlayer = null;

            foreach (PlayerData playerData in projectileManager.playerSynchronizer.playerIdentities)
            {

                if (closestPlayer)
                {

                    if (Vector2.Distance(rb.position, playerData.square.rb.position) < Vector2.Distance(rb.position, closestPlayer.rb.position))
                    {

                        closestPlayer = playerData.square;

                    }

                }
                else
                {

                    if (Vector2.Distance(rb.position, playerData.square.rb.position) < data.homingDistance)
                    {

                        closestPlayer = playerData.square;

                    }

                }

            }

            if (closestPlayer)
            {

                homingDirection = (closestPlayer.rb.position - rb.position).normalized;

                if (closestPlayer == owningPlayer) homingDirection /= 2;

            }
            else homingDirection = Vector2.zero;

        }
        else homingDirection = Vector2.zero;

    }

    Vector3 chargePlayerEndScale;
    Vector2 homingDirection = Vector2.zero;

    private void LateUpdate()
    {

        if (stickToSender)
        {
            owningPlayer.transform.localScale = Vector3.Lerp(Vector3.one, data.targetMorph / 5, math.clamp((timeAlive / data.timeToMorph) * 2, 0, 1));
            transform.position = owningPlayer.transform.position;
            owningPlayer.rb.rotation = rb.rotation;
        }

    }

    float rotate;
    float startRotate;
    bool lastSticky;

    private void FixedUpdate()
    {

        Vector2 vel, pos;
        float ang, rot, oldRot;

        damage += Time.deltaTime * (damageScaleOverTime * Mods.at[11]);
        damage = Mathf.Abs(damage);
        timeAlive += Time.deltaTime;
        vel = rb.linearVelocity;
        pos = rb.position;
        ang = rb.angularVelocity;
        rot = rb.rotation;
        oldRot = rb.rotation;

        if (lastSticky != hasStuckToPoint)
        {
            rb.gravityScale = 0;
            lastSticky = hasStuckToPoint;
        }

        if (homingDirection != Vector2.zero)
        {
            rb.AddForce(homingDirection * data.homingStrength * Time.deltaTime * 50);
        }

        if (data.hover)
        {
            RaycastHit2D hitPoint = GetClosestEnvironmentPointDown(rb.position, data.hoverDistance, data.hoverFloorRadius);
            if (hitPoint.transform)
            {
                float distance = Vector2.Distance(hitPoint.point, rb.position);
                float totalStength = data.hoverStrength * (data.hoverDistanceAttenuation > 0 ? (distance / data.hoverDistanceAttenuation) : 1);
                Vector2 pointToRb = (rb.position - hitPoint.point).normalized;
                rb.AddForce(pointToRb * totalStength * Time.deltaTime * (Mathf.Clamp01(timeAlive / data.timeForFullHoverEffect)));
            }
        }

        if (melee)
        {
            float meleePosLerp = data.meleePosAnimation.Evaluate(math.clamp(timeAlive / data.lifeTime, 0, 1));
            Vector2 meleeDirection = Vector2.Lerp(meleeStartDirection, meleeEndDirection, meleePosLerp);
            Vector2 meleeLocalPos = meleeDirection.normalized * data.meleeRange;
            Vector2 meleeGlobalPos = meleeLocalPos + owningPlayer.rb.position;
            pos = meleeGlobalPos;

            float meleeRotLerp = data.meleeRotAnimation.Evaluate(math.clamp(timeAlive / data.lifeTime, 0, 1));
            rot = initRot + math.lerp(meleeStartRot, meleeEndRot, meleeRotLerp);

            if (trailParticleSystem)
            {
                MainModule main = trailParticleSystem.main;

                Vector3 spriteSize = spriteRenderer.bounds.size;
                main.startSizeX = spriteSize.x;
                main.startSizeY = spriteSize.y;
                main.startSizeZ = 1;

                main.startRotation = math.radians(spriteRenderer.transform.eulerAngles.z);
            }

            vel = owningPlayer.rb.linearVelocity;

            if (data.enableMorph)
            {
                morphLerp = data.morhpAnimation.Evaluate(timeAlive / data.timeToMorph);
                if (data.clampMorph) transform.localScale = Vector3.Lerp(startMorph, endMorph, morphLerp);
                else transform.localScale = Vector3.LerpUnclamped(startMorph, endMorph, morphLerp);
            }

            rb.linearVelocity = vel;
            rb.position = pos;
            rb.angularVelocity = ang;
            rb.rotation = rot;
            return;
        }

        if (data.spinSpeed > 0)
        {
            ang = ang / math.abs(ang) * data.spinSpeed;
        }

        if (stickToSender)
        {
            owningPlayer.rb.linearVelocity = vel;
            rot = math.lerp(startRotate, rotate, timeAlive / data.lifeTime);
            if (rot > 360f) rot -= 360f;
            if (rot < 360f) rot += 360f;
        }

        if (data.enableMorph)
        {
            morphLerp = data.morhpAnimation.Evaluate(timeAlive / data.timeToMorph);
            if (data.clampMorph) transform.localScale = Vector3.Lerp(startMorph, endMorph, morphLerp);
            else transform.localScale = Vector3.LerpUnclamped(startMorph, endMorph, morphLerp);
        }
        vel += vel * (speedModifier * Time.deltaTime);

        travelDistance += (pos - lastPos).magnitude;
        lastPos = pos;

        vel = Vector2.ClampMagnitude(vel, data.speedLimit);
        if (vel.magnitude < data.minSpeed)
        {
            vel = vel.normalized * data.minSpeed;
        }

        (float posX, float posY) = (pos.x, pos.y);

        bool borderDeath = false;
        borderDeath |= Mathf.Abs(posX) > 64;
        borderDeath |= Mathf.Abs(posY) > 64;
        if (borderDeath && !destroyed)
        {
            destroyed = true;
            return;
        }

        pos = new Vector2(math.clamp(posX, -64, 64), math.clamp(posY, -64, 64));

        if (rot > 360) rot -= 360;
        if (rot < 0) rot += 360;

        ang = math.clamp(ang, -1000, 1000);

        if (data.alignDirection)
        {
            rot = math.degrees(math.atan2(vel.y, vel.x));
            ang = (rot - rb.rotation) * Time.deltaTime;
            if (projectileTrailBehaviour) projectileTrailBehaviour.transform.rotation = transform.rotation;
        }


        rb.linearVelocity = vel;
        rb.angularVelocity = ang;
        rb.rotation = hasStuckToPoint ? stickyNormalAngle : rot;
        if (hasStuckToPoint)
        {
            transform.localPosition = pointStuckAt;
            rb.rotation = stickyNormalAngle;
        }
    }

    float lingeringTimer;


    void LocalUpdate()
    {

        syncTimer += Time.deltaTime * data.syncSpeed;

        if (!destroyed && timeAlive > data.lifeTime) destroyed = true;

        if (destroyed)
        {

            if (!instaDestroy)
            {

                foreach (PlayerData player in projectileManager.playerSynchronizer.playerIdentities)
                {
                    if (Vector2.Distance(rb.position, player.square.rb.position) > data.aoe) continue;
                    if (Physics2D.Linecast(rb.position, player.square.rb.position, ENVIRONTMENT_MASK).collider) continue;
                    if (playerHit && skipAoeOnHit && player.square == playerHit) continue;

                    Vector2 direction = (player.square.rb.position - rb.position).normalized;

                    player.square.timeSinceHit = 0.25f;

                    // Determine damage/slow based on whether this is the owner
                    float damage = (player.id == ownerId) ? 0 : aoeDamage;
                    float slow = (player.id == ownerId) ? 0 : data.slowDownAmount;

                    projectileManager.playerSynchronizer.UpdatePlayerHealth(
                        player.square.GetID(),
                        damage,
                        slow,
                        (byte)ownerId,
                        direction * data.knockback
                    );


                }

                foreach (FlagBehaviour flag in FindObjectsByType<FlagBehaviour>(FindObjectsSortMode.None))
                {

                    if (Vector2.Distance(rb.position, flag.rb.position) > data.aoe) continue;
                    if (Physics2D.Linecast(rb.position, flag.rb.position, LayerMask.GetMask("Environment")).collider != null) continue;
                    if (flagHit) if (skipAoeOnHit && flag == flagHit) continue;

                    bool skipHit = false;

                    if (flag.activityState == FlagActivityState.Idle)
                    {
                        if (flag.ownerId == ownerId) skipHit = true;
                    }
                    else if (flag.activityState == FlagActivityState.FollowTarget)
                    {
                        if (flag.playerBehaviour.id == ownerId) skipHit = true;
                    }
                    else skipHit = true;

                    if (skipHit) continue;

                    flag.RegisterHit(this);

                }

            }

            instaDestroy = true;
            SetupAllProxySpawns(ProjectileSpawnEvent.EventType.Death);
            projectileManager.DespawnProjectile(projectileID, hit);

        }
        else if (sync && syncTimer > 1)
        {
            projectileManager.UpdateProjectile(this);
            syncTimer = 0;
        }

        lingeringTimer += Time.deltaTime;

    }

    private void OnTriggerStay2D(Collider2D collision)
    {

        if (!IsLocalProjectile) return;

        PlayerBehaviour playerBehaviour = collision.gameObject.GetComponent<PlayerBehaviour>();

        if (playerBehaviour)
        {

            if (playerBehaviour.id == owningPlayer.id) return;

            if (data.lingeringDamage > 0)
            {

                if (lingeringTimer * data.lingeringFrequency > 1)
                {

                    lingeringTimer = 0;


                    Vector2 direction = (playerBehaviour.rb.position - rb.position).normalized;
                    projectileManager.playerSynchronizer.UpdatePlayerHealth((byte)playerBehaviour.id, data.lingeringDamage, data.slowDownAmount, (byte)ownerId, direction * data.knockback);


                }

            }

        }

    }

    private void OnTriggerEnter2D(Collider2D collider) => CollisionCheck(collider.gameObject);
    private void OnCollisionEnter2D(Collision2D collision) => CollisionCheck(collision.gameObject);
    private void OnCollisionExit2D(Collision2D collision) => CollisionCancell(collision.gameObject);
    private void OnTriggerExit2D(Collider2D collision) => CollisionCancell(collision.gameObject);


    void CollisionCheck(GameObject collidedWith)
    {
        if (destroyed) return;
        if (!IsLocalProjectile) return;

        PlayerBehaviour playerBehaviour = collidedWith.gameObject.GetComponent<PlayerBehaviour>();
        FlagBehaviour flagBehaviour = collidedWith.GetComponent<FlagBehaviour>();
        ProjectileBehaviour projectileBehaviour = collidedWith.GetComponent<ProjectileBehaviour>();
        bool environment = collidedWith.layer == LayerMask.NameToLayer("Environment");

        if (playerBehaviour) PlayerCollisionCheck(playerBehaviour);
        if (flagBehaviour) FlagCollisionCheck(flagBehaviour);
        if (environment && !stickToSender && !melee) EnvironmentCollisionCheck();
        if (projectileBehaviour) ProjectileCollisionCheck(projectileBehaviour);
    }

    List<PlayerBehaviour> playersCollidingWith;
    void CollisionCancell(GameObject collidedWith)
    {

        PlayerBehaviour playerBehaviour = collidedWith.GetComponent<PlayerBehaviour>();
        if (playerBehaviour) playersCollidingWith.Remove(playerBehaviour);

    }

    void PlayerCollisionCheck(PlayerBehaviour playerBehaviour)
    {
        if (playerBehaviour.isLocalPlayer) return;
        playerHit = playerBehaviour;

        if (data.dieOnImpact)
        {
            destroyed = true;
            spriteRenderer.enabled = false;
            hit = true;
        }

        if (data.damageOnImpact)
        {
            if (playerBehaviour)
            {

                if (data.oneTimeHit && !playersHit.Contains(playerBehaviour))
                {

                    if (data.melee || stickToSender) damage *= Mods.at[6];

                    Vector2 direction = (playerBehaviour.rb.position - rb.position).normalized;
                    projectileManager.playerSynchronizer.UpdatePlayerHealth((byte)playerBehaviour.id, damage, data.slowDownAmount, (byte)ownerId, direction * data.knockback);
                    playerBehaviour.timeSinceHit = 0.25f;
                    projectileManager.HitRegProjectile(projectileID);

                }
                else if (!data.oneTimeHit)
                {

                    if (data.melee || stickToSender) damage *= Mods.at[6];

                    Vector2 direction = (playerBehaviour.rb.position - rb.position).normalized;
                    projectileManager.playerSynchronizer.UpdatePlayerHealth((byte)playerBehaviour.id, damage, data.slowDownAmount, (byte)ownerId, direction * data.knockback);
                    playerBehaviour.timeSinceHit = 0.25f;
                    projectileManager.HitRegProjectile(projectileID);

                }

            }

        }

        if (playerBehaviour)
        {
            playersCollidingWith.Add(playerBehaviour);
            playersHit.Add(playerBehaviour);

            if (data.bounceOfPlayers)
            {

                rb.linearVelocity = (rb.position - playerBehaviour.rb.position).normalized * data.speed;
                projectileManager.UpdateProjectile(this);

            }

        }

    }

    void ProjectileCollisionCheck(ProjectileBehaviour projectileBehaviour)
    {

        if (projectileBehaviour.data.dontBlockProjectiles) return;

        if (data.dieFromProjectiles)
        {
            destroyed = true;
            spriteRenderer.enabled = false;
            hit = true;
        }

    }

    void FlagCollisionCheck(FlagBehaviour flag)
    {

        bool skipHit = false;

        if (flag.activityState == FlagActivityState.Idle)
        {
            if (flag.ownerId == ownerId) skipHit = true;
        }
        else if (flag.activityState == FlagActivityState.FollowTarget)
        {
            if (flag.playerBehaviour.id == ownerId) skipHit = true;
        }

        if (data.oneTimeHit)
        {
            if (flagsHit.Contains(flag)) skipHit = true;
            flagsHit.Add(flag);
        }

        if (skipHit) return;

        flagHit = flag;

        if (data.dieOnImpact)
        {
            destroyed = true;
            spriteRenderer.enabled = false;
            hit = true;
        }

        if (data.damageOnImpact)
        {

            flag.RegisterHit(this);

        }

    }

    bool flipRotation = true;
    Vector3 pointStuckAt;
    Vector2 stickySurfaceNormal;
    float stickyNormalAngle;
    bool hasStuckToPoint;


    void EnvironmentCollisionCheck()
    {

        if (data.sticky)
        {

            RaycastHit2D closesPoint = GetClosestEnvironmentPoint(rb.position);

            if (closesPoint.transform)
            {
                transform.SetParent(closesPoint.transform, true);
                stickySurfaceNormal = closesPoint.normal;
                transform.position = closesPoint.point;
                pointStuckAt = transform.localPosition;
                hasStuckToPoint = true;
                stickyNormalAngle = Mathf.Atan2(stickySurfaceNormal.y, stickySurfaceNormal.x) * Mathf.Rad2Deg;
                rb.position = pointStuckAt;
                rb.rotation = stickyNormalAngle;
                if (IsLocalProjectile) projectileManager.UpdateProjectile(this);
            }

        }

        if (data.dieOnImpact)
        {
            if (data.bounces > 0)
            {

                Vector2 incomingDirection = rb.linearVelocity.normalized;
                RaycastHit2D hitpoint = GetClosestEnvironmentPoint(rb.position);
                Vector2 bounceDir = Vector2.Reflect(incomingDirection, hitpoint.normal);
                Vector2 normal = hitpoint.normal.normalized;
                Vector2 slidingDirection = new Vector2();

                float incomingAngle = Vector2.Angle(-incomingDirection, Vector2.right);
                float normalAngle = Vector2.Angle(normal, Vector2.right);

                Vector2 crossA = MyExtentions.AngleToNormalizedCoordinate(normalAngle + 90);
                Vector2 crossB = MyExtentions.AngleToNormalizedCoordinate(normalAngle - 90);

                if (Vector2.Distance(crossA, -incomingDirection) > Vector2.Distance(crossB, -incomingDirection)) slidingDirection = crossA;
                else slidingDirection = crossB;

                if(data.bounceAngleTilt > 0) bounceDir = Vector2.Lerp(bounceDir, slidingDirection, data.bounceAngleTilt);
                else if(data.bounceAngleTilt < 0) bounceDir = Vector2.Lerp(bounceDir, -incomingDirection, -data.bounceAngleTilt);

                bounceDir = bounceDir.normalized;

                rb.linearVelocity = bounceDir.normalized * rb.linearVelocity.magnitude * (1 - Mathf.Clamp01(data.bounceSpeedLoss));
                data.bounces--;
                if (IsLocalProjectile) projectileManager.UpdateProjectile(this);
                if (data.bounceParticle)
                {
                    float angle = Vector2.SignedAngle(Vector2.right, hitpoint.normal);
                    ParticleBehaviour bounceParticles = ParticlePool.Spawn(data.bounceParticle, hitpoint.point, Quaternion.Euler(0, 0, angle));
                    for (int i = 0; i < bounceParticles.ParticleSystems.Length; i++)
                    {
                        owningPlayer.PlayerColor.AssignMaterialToParticleRenderer(bounceParticles.ParticleSystemRenderers[i], bounceParticles.ParticleSystems[i]);
                    }
                }
            }
            else
            {
                destroyed = true;
                spriteRenderer.enabled = false;
                hit = true;
            }
        }

        if (data.rotationFlipOnImpact)
        {

            if (flipRotation) rb.angularVelocity = -data.spinSpeed;
            else rb.angularVelocity = data.spinSpeed;
            flipRotation = !flipRotation;

        }

    }

    public void OnDespawn(bool hit) => DestroyThisProjectile(hit);


    void DestroyThisProjectile(bool hit)
    {

        if (data.senderSpeedOnDeath > 0)
        {
            owningPlayer.rb.linearVelocity = rb.linearVelocity.normalized * data.senderSpeedOnDeath;
        }

        bool aoe = data.aoe > 0;

        if (hit || aoe)
        {

            EventInstance eventInstance = RuntimeManager.CreateInstance(hitSoundReference);
            eventInstance.setParameterByName("CameraPositionX", transform.position.x - Camera.main.transform.position.x);
            eventInstance.setVolume(MySettings.volume);
            eventInstance.start();

            RaycastHit2D point = GetClosestEnvironmentPoint(boom.position);
            float angle = math.degrees(math.atan2(-point.normal.y, -point.normal.x));

            ParticleBehaviour impactParticles = ParticlePool.Spawn(data.impactParticle, boom.transform.position, Quaternion.Euler(0, 0, angle), null);

            for (int i = 0; i < impactParticles.ParticleSystems.Length; i++)
            {
                owningPlayer.PlayerColor.AssignMaterialToParticleRenderer(impactParticles.ParticleSystemRenderers[i], impactParticles.ParticleSystems[i]);
            }

            SpawnHitMark(aoe);

        }

        Destroy(gameObject);

    }

    public void SpawnHitMark(bool aoe)
    {

        if (!owningPlayer) return;
        if (!hitMark) return;
        RaycastHit2D point;
        Transform toParent = null;
        if (boom) point = GetClosestEnvironmentPoint(boom.position, out toParent);
        else point = GetClosestEnvironmentPoint(rb.position, out toParent);
        if (!toParent) return;
        
        Vector3 hitMarkPos;
        
        if (aoe) hitMarkPos = new Vector3(boom.transform.position.x, boom.transform.position.y, transform.position.z);
        else hitMarkPos = new Vector3(point.point.x, point.point.y, transform.position.z);

        float angle = math.degrees(math.atan2(point.normal.y, point.normal.x));
        
        HitMarkBehaviour newHitMark = Instantiate(hitMark, toParent, true);
        newHitMark.transform.position = hitMarkPos;
        newHitMark.transform.rotation = Quaternion.Euler(0, 0, angle);

        newHitMark.ownerId = (byte)ownerId;
        newHitMark.owner = owningPlayer;
        StencilInfectorBehaviour stencilInfectorBehaviour;
        if (toParent.TryGetComponent(out stencilInfectorBehaviour)) newHitMark.AssignStencil(stencilInfectorBehaviour.GetStencil());
        else if (toParent.parent && toParent.parent.TryGetComponent(out stencilInfectorBehaviour)) newHitMark.AssignStencil(stencilInfectorBehaviour.GetStencil());
        else if (toParent.parent && toParent.parent.parent && toParent.parent.parent.TryGetComponent(out stencilInfectorBehaviour)) newHitMark.AssignStencil(stencilInfectorBehaviour.GetStencil());

        Color color = owningPlayer.PlayerColor.HitMarkColor;
        newHitMark.spawnColor = color;
        newHitMark.fadeColor = new UnityEngine.Color(color.r, color.g, color.b, 0f);

        Instantiate(AssetResources.PowerDot, hitMarkPos, transform.rotation, transform.parent);

    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetDirSpanContents(ref Span<Vector2> span)
    {
        span[0] = new Vector2(1f, 0f);
        span[1] = new Vector2(0.7071f, 0.7071f);
        span[2] = new Vector2(0f, 1f);
        span[3] = new Vector2(-0.7071f, 0.7071f);
        span[4] = new Vector2(-1f, 0f);
        span[5] = new Vector2(-0.7071f, -0.7071f);
        span[6] = new Vector2(0f, -1f);
        span[7] = new Vector2(0.7071f, -0.7071f);
    }
    const int DIRS_COUNT = 8;
    readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[1];



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    RaycastHit2D GetClosestEnvironmentPointDown(Vector2 origin, float maxDistance = 100f, float floorRadius = 0.5f)
    {
        return Physics2D.CircleCast(origin, floorRadius, new Vector2(0f, -1f), maxDistance, ENVIRONTMENT_MASK);
    }

    RaycastHit2D GetClosestEnvironmentPoint(Vector2 origin, float maxDistance = 100f)
    {
        Span<Vector2> DIRS_8 = stackalloc Vector2[DIRS_COUNT];
        SetDirSpanContents(ref DIRS_8);

        float shortestDistance = float.PositiveInfinity;
        RaycastHit2D closestHit = default;

        for (int i = 0; i < DIRS_COUNT; i++)
        {
            int hitCount = Physics2D.RaycastNonAlloc(origin, DIRS_8[i], hitBuffer, maxDistance, ENVIRONTMENT_MASK);
            if (hitCount <= 0) continue;
            float dist = hitBuffer[0].distance;
            if (dist >= shortestDistance) continue;
            shortestDistance = dist;
            closestHit = hitBuffer[0];
        }
        return closestHit;
    }


    RaycastHit2D GetClosestEnvironmentPoint(Vector2 origin, out Transform objectHit, float maxDistance = 100f)
    {
        objectHit = null;

        Span<Vector2> DIRS_8 = stackalloc Vector2[DIRS_COUNT];
        SetDirSpanContents(ref DIRS_8);

        float shortestDistance = float.PositiveInfinity;
        RaycastHit2D closestHit = default;

        for (int i = 0; i < DIRS_COUNT; i++)
        {
            int hitCount = Physics2D.RaycastNonAlloc(origin, DIRS_8[i], hitBuffer, maxDistance, ENVIRONTMENT_MASK);
            if (hitCount <= 0) continue;
            float dist = hitBuffer[0].distance;
            if (dist >= shortestDistance) continue;
            shortestDistance = dist;
            closestHit = hitBuffer[0];
        }

        if (closestHit.collider != null) objectHit = closestHit.collider.transform;
        return closestHit;
    }




    public void HitReg()
    {

        EventInstance eventInstance = RuntimeManager.CreateInstance(hitSoundReference);
        eventInstance.setParameterByName("CameraPositionX", transform.position.x - Camera.main.transform.position.x);
        eventInstance.setVolume(MySettings.volume);
        eventInstance.start();

        ParticleBehaviour impactParticles = ParticlePool.Spawn(data.impactParticle, boom.transform.position, transform.rotation, null);

        for (int i = 0; i < impactParticles.ParticleSystems.Length; i++)
        {
            owningPlayer.PlayerColor.AssignMaterialToParticleRenderer(impactParticles.ParticleSystemRenderers[i], impactParticles.ParticleSystems[i]);
        }
    }


    private void OnDestroy()
    {

        shotInstance.release();

        for (int i = 0; i < spriteRenderer.materials.Length; i++) Destroy(spriteRenderer.materials[i]);

        if (owningPlayer)
        {
            owningPlayer.transform.localScale = Vector3.one;
            owningPlayer.nozzleBehaviour.transform.localScale = Vector3.one * 0.4f;
        }

        if (aliveSound)
        {

            aliveInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            aliveInstance.release();

        }
    }
}

[Serializable]
public struct ProjectileInitData
{

    public ParticleBehaviour impactParticle;
    public ParticleBehaviour bounceParticle;
    public ProjectileManager projectileManager;
    public PlayerBehaviour owningPlayer;
    public Vector2 position;
    public Vector2 direction;
    public Color projectileColor;
    public Color projectileDarkerColor;
    public uint id;
    public bool IsLocalProjectile;
    public float acceleration;
    public float speed;
    public float lifeTime;
    public float[] fluctuation;
    public bool noGravity;
    public bool dieOnImpact;
    public bool damageOnImpact;
    public bool sticky;
    public bool skipAoeOnTargetHit;
    public float aoe;
    public float knockback;
    public float speedLimit;
    public float minSpeed;
    public float aoeDamage;
    public float baseDamage;
    public float damageTimeScale;
    public float[] burstData;
    public bool stickToSender;
    public bool melee;
    public float meleeRange;
    public float swingDegrees;
    public AnimationCurve meleePosAnimation;
    public bool oneTimeHit;
    public float meleeRotation;
    public AnimationCurve meleeRotAnimation;

    public bool enableMorph;
    public Vector3 targetMorph;
    public float timeToMorph;
    public AnimationCurve morhpAnimation;
    public bool homing;
    public float homingStrength;
    public float homingDistance;
    public float spinSpeed;
    public bool rotationFlipOnImpact;
    public bool dieFromProjectiles;
    public bool dontBlockProjectiles;
    public bool bounceOfPlayers;

    public bool sync;
    public float syncSpeed;

    public float slowDownAmount;
    public float senderSpeedOnDeath;

    public float lingeringDamage;
    public float lingeringFrequency;
    public bool alignDirection;
    public byte bounces;
    public bool clampMorph;
    public float bounceSpeedLoss;
    public float bounceAngleTilt;

    public bool hover;
    public float hoverDistance;
    public float hoverStrength;
    public float hoverFloorRadius;
    public float hoverDistanceAttenuation;
    public float timeForFullHoverEffect;

    public ProjectileSpawnEvent[] projectileSpawnEvents;
}