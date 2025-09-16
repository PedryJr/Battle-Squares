using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using static PlayerSynchronizer;
using static UnityEngine.ParticleSystem;
using Color = UnityEngine.Color;


[BurstCompile]
public sealed class ProjectileBehaviour : MonoBehaviour
{

    const Int32 ENVIRONTMENT_MASK = 0b00000000000000000000001000000000;

    public float initDamage;

    [SerializeField]
    Transform boom;

    public float damageScaleOverTime;

    public float damage;
    public float aoeDamage;

    public float syncTimer;

    public float speedModifier;

    public uint projectileID;

    public bool IsLocalProjectile;

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

    public ulong ownerId;

    public bool melee;

    public bool hit;
    public bool sync;
    public bool flipFlop;

    const string paramNameCameraPositionX = "CameraPositionX";

    bool stuck;
    GameObject stuckTo;

    [SerializeField]
    GameObject impactParticle;

    [SerializeField]
    public HitMarkBehaviour hitMark;

    ParticleSystemRenderer trailParticles;
    ParticleSystem trailParticleSystem;
    MainModule trailMainModule;

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

    static Dictionary<ulong, Material> trailMaterials = new Dictionary<ulong, Material>();

    [BurstCompile]
    private void Awake()
    {

        projectileCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailParticles = GetComponentInChildren<ParticleSystemRenderer>();
        trailParticleSystem = trailParticles.GetComponent<ParticleSystem>();
        if(trailParticleSystem) trailMainModule = trailParticleSystem.main;
        cameraAnimator = Camera.main.GetComponent<CameraAnimator>();
        playersHit = new List<PlayerBehaviour>();
        flagsHit = new List<FlagBehaviour>();
        playersCollidingWith = new List<PlayerBehaviour>();
    }
    [BurstCompile]
    private void Start()
    {

        float pitch = 1f + UnityEngine.Random.Range(-0.08f, 0.08f);

        if (math.abs(data.burst) != 0) return;

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

    [BurstCompile]
    public void InitializeBullet(ref ProjectileInitData data)
    {

        initDamage = data.baseDamage;
        aoeDamage = data.aoeDamage * Mods.at[7];
        damageScaleOverTime = data.damageTimeScale;
        skipAoeOnHit = data.skipAoeOnTargetHit;
        returnToSender = data.retornToSender;
        stickToSender = data.stickToSender;

        endMorph = data.targetMorph;
        startMorph = transform.localScale;
        owningPlayer = data.owningPlayer;

        sync = data.sync;

        data.id++;

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

        if (trailParticles)
        {

            Material trailParticleMaterial;
            if (trailMaterials.ContainsKey(data.owningPlayer.id)) trailParticleMaterial = trailMaterials[data.owningPlayer.id];
            else
            {
                trailParticleMaterial = Instantiate(trailParticles.material);
                trailMaterials.Add(data.owningPlayer.id, trailParticleMaterial);
            }
            Destroy(trailParticles.material);
            trailParticles.material = trailParticleMaterial;
            trailParticles.material.color = generalParticleColor;

        }

        damage = initDamage * Mods.at[4];

        speedModifier = data.acceleration;

        melee = data.melee;

        if (flipFlop)
        {


            meleeStartDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation + (data.swingDegrees / 2));
            meleeEndDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation - (data.swingDegrees / 2));

            meleeStartRot = data.meleeRotation / 2;
            meleeEndRot = -data.meleeRotation / 2;

        }
        else
        {

            meleeStartDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation - (data.swingDegrees / 2));
            meleeEndDirection = MyExtentions.AngleToNormalizedCoordinate(rb.rotation + (data.swingDegrees / 2));

            meleeStartRot = -data.meleeRotation / 2;
            meleeEndRot = data.meleeRotation / 2;

        }

        initRot = rb.rotation;

        this.data = data;

        if (math.abs(data.burst) > 0)
        {
            data.id++;
            int burst = math.abs(data.burst);

            Vector2 offset = new Vector2(data.burstData[burst * 2 -1] * -data.burst, data.burstData[(burst * 2) -2] * data.burst);
            if(rb.linearVelocity.x < 0) offset.x *= -1;
            if(rb.linearVelocity.y < 0) offset.y *= -1;
            if (math.abs(rb.linearVelocity.x) < 0.001f || math.abs(rb.linearVelocity.x) < 0.001f) offset *= 2;
            burst--;

            rb.linearVelocity = (rb.linearVelocity + offset).normalized * data.speed;
            rb.angularVelocity = UnityEngine.Random.Range(15f, -15f);
            data.position += new Vector2(UnityEngine.Random.Range(-0, 0), UnityEngine.Random.Range(-0, 0));
            data.burst = (data.burst > 0 ? burst : -burst) * -1;

            GameObject newBurst = Instantiate(gameObject, transform.parent);
            newBurst.GetComponent<ProjectileBehaviour>().InitializeBullet(ref data);

        }

        if (!IsLocalProjectile) gameObject.layer = LayerMask.NameToLayer("RemoteProjectile");
        projectileManager.projectiles.Add(this);
        lastPos = rb.position;

        rb.linearVelocity *= Mods.at[3];
        this.data.knockback *= Mods.at[12];

    }
    [BurstCompile]
    private void Update()
    {

        if (IsLocalProjectile) LocalUpdate();

        GlobalUpdate();

    }

    float audioTimer;

    [BurstCompile]
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
                if(multiplySpawnrateByLifetime) deltaTime = Time.deltaTime * math.lerp(1, 0, math.clamp(timeAlive/data.lifeTime, 0, 1)) * lifeTimeMultiplier;
                else deltaTime = Time.deltaTime;

                externalTrailSpawnTimer += deltaTime;

                while (externalTrailSpawnTimer > externalTrailSpawnRate)
                {

                    ExternalTrailBehaviour externalTrail = Instantiate(externalTrailRef, transform.position, transform.rotation, null);
                    if (externalTrail) externalTrail.Play(generalParticleColor, owningPlayer.id);

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
    [BurstCompile]
    private void LateUpdate()
    {

        if (stickToSender)
        {
            owningPlayer.transform.localScale = Vector3.Lerp(Vector3.one, data.targetMorph/5, math.clamp((timeAlive / data.timeToMorph) * 2, 0, 1));
            transform.position = owningPlayer.transform.position;
            owningPlayer.rb.rotation = rb.rotation;
        }

    }

    float rotate;
    float startRotate;
    bool lastSticky;

    [BurstCompile]
    private void FixedUpdate()
    {

        Vector2 vel, pos;
        float ang, rot, oldRot;

        damage += Time.deltaTime * (damageScaleOverTime * Mods.at[11]);
        timeAlive += Time.deltaTime;
        vel = rb.linearVelocity;
        pos = rb.position;
        ang = rb.angularVelocity;
        rot = rb.rotation;
        oldRot = rb.rotation;

        if(lastSticky != hasStuckToPoint)
        {
            rb.gravityScale = 0;
            lastSticky = hasStuckToPoint;
        }

        if(homingDirection != Vector2.zero)
        {
            rb.AddForce(homingDirection * data.homingStrength * Time.deltaTime * 50);
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
                morphLerp = data.morhpAnimation.Evaluate(math.clamp(timeAlive / data.timeToMorph, 0, 1));
                transform.localScale = Vector3.Lerp(startMorph, endMorph, morphLerp);
            }

            rb.linearVelocity = vel;
            rb.position = pos;
            rb.angularVelocity = ang;
            rb.rotation = rot;
            return;
        }

        if(data.spinSpeed > 0)
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
            morphLerp = data.morhpAnimation.Evaluate(math.clamp(timeAlive / data.timeToMorph, 0, 1));
            transform.localScale = Vector3.Lerp(startMorph, endMorph, morphLerp);
        }
        vel += vel * (speedModifier * Time.deltaTime);

        travelDistance += (pos - lastPos).magnitude;
        lastPos = pos;

        vel = Vector2.ClampMagnitude(vel, data.speedLimit);
        if(vel.magnitude < data.minSpeed)
        {
            vel = vel.normalized * data.minSpeed;
        }

        (float posX, float posY) = (pos.x, pos.y);
        pos = new Vector2(math.clamp(posX, -64, 64), math.clamp(posY, -64, 64));

        if (rot > 360) rot -= 360;
        if (rot < 0) rot += 360;

        ang = math.clamp(ang, -1000, 1000);

        if (data.alignDirection)
        {
            rot = math.degrees(math.atan2(vel.y, vel.x));
            ang = (rot - rb.rotation) * Time.deltaTime;
            if (projectileTrailBehaviour)
            {
                projectileTrailBehaviour.transform.rotation = transform.rotation;
            }
        }

        rb.linearVelocity = vel;
        rb.position = hasStuckToPoint ? rb.position : pos;
        rb.angularVelocity = ang;
        rb.rotation = hasStuckToPoint ? stickyNormalAngle : rot;
        if (hasStuckToPoint) transform.localPosition = pointStuckAt;


    }

    float lingeringTimer;

    [BurstCompile]
    void LocalUpdate()
    {

        syncTimer += Time.deltaTime;

        if(!destroyed && timeAlive > data.lifeTime) destroyed = true;

        if (destroyed)
        {

            if (!instaDestroy)
            {

                foreach (PlayerData player in projectileManager.playerSynchronizer.playerIdentities)
                {
                    if (player.id == ownerId)
                    {

                        if (Vector2.Distance(rb.position, player.square.rb.position) > data.aoe) continue;
                        if (Physics2D.Linecast(rb.position, player.square.rb.position, LayerMask.GetMask("Environment")).collider != null) continue;
                        if (playerHit) if (skipAoeOnHit && player.square == playerHit) continue;

                        Vector2 direction = (player.square.rb.position - rb.position).normalized;

                        player.square.timeSinceHit = 0.25f;
                        projectileManager.playerSynchronizer.UpdatePlayerHealth((byte)player.square.id, 0, 0, (byte)ownerId, direction * data.knockback);

                    }
                    else
                    {

                        if (Vector2.Distance(rb.position, player.square.rb.position) > data.aoe) continue;
                        if (Physics2D.Linecast(rb.position, player.square.rb.position, LayerMask.GetMask("Environment")).collider != null) continue;
                        if (playerHit) if (skipAoeOnHit && player.square == playerHit) continue;

                        Vector2 direction = (player.square.rb.position - rb.position).normalized;

                        player.square.timeSinceHit = 0.25f;
                        projectileManager.playerSynchronizer.UpdatePlayerHealth((byte)player.square.id, aoeDamage, data.slowDownAmount, (byte)ownerId, direction * data.knockback);

                    }

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

            projectileManager.DespawnProjectile(projectileID, hit);

        }
        else if (sync && syncTimer > data.syncSpeed)
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

    [BurstCompile]
    private void OnTriggerEnter2D(Collider2D collider)
    {

        CollisionCheck(collider.gameObject);

    }
    [BurstCompile]
    private void OnCollisionEnter2D(Collision2D collision)
    {

        CollisionCheck(collision.gameObject);

    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        CollisionCancell(collision.gameObject);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        CollisionCancell(collision.gameObject);
    }

    [BurstCompile]
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

    [BurstCompile]
    void CollisionCancell(GameObject collidedWith)
    {

        PlayerBehaviour playerBehaviour = collidedWith.GetComponent<PlayerBehaviour>();
        if (playerBehaviour) playersCollidingWith.Remove(playerBehaviour);

    }

    [BurstCompile]
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

                }else if (!data.oneTimeHit)
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
    [BurstCompile]
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
    [BurstCompile]
    void FlagCollisionCheck(FlagBehaviour flag)
    {

        bool skipHit = false;

        if (flag.activityState == FlagActivityState.Idle)
        {
            if(flag.ownerId == ownerId) skipHit = true;
        }
        else if (flag.activityState == FlagActivityState.FollowTarget)
        {
            if(flag.playerBehaviour.id == ownerId) skipHit = true;
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

    [BurstCompile]
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
            destroyed = true;
            spriteRenderer.enabled = false;
            hit = true;

        }

        if (data.rotationFlipOnImpact)
        {

            if (flipRotation) rb.angularVelocity = -data.spinSpeed;
            else rb.angularVelocity = data.spinSpeed;
            flipRotation = !flipRotation;

        }

    }
    [BurstCompile]
    public void OnDespawn(bool hit)
    {

        DestroyThisProjectile(hit);

    }

    static Dictionary<ulong, Material> impactMaterials = new Dictionary<ulong, Material>();

    [BurstCompile]
    void DestroyThisProjectile(bool hit)
    {

        if(data.senderSpeedOnDeath > 0)
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

            GameObject impactParticles = Instantiate(impactParticle, boom.transform.position, Quaternion.Euler(0, 0, angle), null);

            Material imapctMaterial;

            ParticleSystemRenderer particleSystemRenderer = impactParticles.GetComponent<ParticleSystemRenderer>();

            if (impactMaterials.ContainsKey(owningPlayer.id)) imapctMaterial = impactMaterials[owningPlayer.id];
            else
            {

                imapctMaterial = Instantiate(particleSystemRenderer.material);
                impactMaterials.Add(owningPlayer.id, imapctMaterial);

            }
            Destroy(particleSystemRenderer.material);
            particleSystemRenderer.material = imapctMaterial;
            particleSystemRenderer.material.color = generalParticleColor;

            if (timeAlive >= data.lifeTime)
            {
                impactParticles.transform.localScale *= 0.65f;
            }

            SpawnHitMark(aoe);

        }

        Destroy(gameObject);

    }
    [BurstCompile]
    public void SpawnHitMark(bool aoe)
    {

        if (!owningPlayer) return;
        if (!hitMark) return;

        RaycastHit2D point = GetClosestEnvironmentPoint(rb.position, out Transform toParent);
        Vector3 hitMarkPos = new Vector3(point.point.x, point.point.y, transform.position.z);

        if(aoe) hitMarkPos = new Vector3(boom.transform.position.x, boom.transform.position.y, transform.position.z);

        float angle = math.degrees(math.atan2(point.normal.y, point.normal.x));

        HitMarkBehaviour newHitMark = Instantiate(hitMark, hitMarkPos, Quaternion.Euler(0, 0, angle), toParent);

        newHitMark.ownerId = (byte)ownerId;
        newHitMark.owner = owningPlayer;
        StencilInfectorBehaviour stencilInfectorBehaviour;
        if (toParent.TryGetComponent(out stencilInfectorBehaviour)) newHitMark.AssignStencil(stencilInfectorBehaviour.GetStencil());
        else if (toParent.parent && toParent.parent.TryGetComponent(out stencilInfectorBehaviour)) newHitMark.AssignStencil(stencilInfectorBehaviour.GetStencil());
        else if (toParent.parent && toParent.parent.parent && toParent.parent.parent.TryGetComponent(out stencilInfectorBehaviour)) newHitMark.AssignStencil(stencilInfectorBehaviour.GetStencil());

        Color color = owningPlayer.PlayerColor.HitMarkColor;
        newHitMark.spawnColor = color;
        newHitMark.fadeColor = new UnityEngine.Color(color.r, color.g, color.b, 0f);

/*        foreach (SpawnStageBehaviour spawnStage in newHitMark.spawnStages)
        {

            foreach (SpriteRenderer spriteRenderer in spawnStage.sprites)
            {

                spriteRenderer.color = newHitMark.spawnColor;

            }

        }*/


    }
    [BurstCompile]
    RaycastHit2D GetClosestEnvironmentPoint(Vector2 origin)
    {

        int rayCount = 4;

        float angleStep = 360f / rayCount;
        RaycastHit2D shortestHit = default;
        float shortestDistance = math.INFINITY;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i * angleStep) + rb.rotation;
            Vector2 direction = new Vector2(math.cos(math.radians(angle)), math.sin(math.radians(angle)));

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, 100f, ENVIRONTMENT_MASK);

            if (hit.collider != null && hit.distance < shortestDistance)
            {

                shortestDistance = hit.distance;
                shortestHit = hit;

            }
        }

        return shortestHit;

    }

    RaycastHit2D GetClosestEnvironmentPoint(Vector2 origin, out Transform objectHit)
    {
        objectHit = null;
        int rayCount = 4;

        float angleStep = 360f / rayCount;
        RaycastHit2D shortestHit = default;
        float shortestDistance = math.INFINITY;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i * angleStep) + rb.rotation;
            Vector2 direction = new Vector2(math.cos(math.radians(angle)), math.sin(math.radians(angle)));

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, 100f, ENVIRONTMENT_MASK);

            if (hit.collider != null && hit.distance < shortestDistance)
            {

                shortestDistance = hit.distance;
                shortestHit = hit;
                objectHit = hit.transform;

            }
        }

        return shortestHit;

    }

    [BurstCompile]
    public void HitReg()
    {

        EventInstance eventInstance = RuntimeManager.CreateInstance(hitSoundReference);
        eventInstance.setParameterByName("CameraPositionX", transform.position.x - Camera.main.transform.position.x);
        eventInstance.setVolume(MySettings.volume);
        eventInstance.start();

        GameObject impactParticles = Instantiate(impactParticle, boom.transform.position, transform.rotation, null);

        Material imapctMaterial;

        ParticleSystemRenderer particleSystemRenderer = impactParticles.GetComponent<ParticleSystemRenderer>();

        if (impactMaterials.ContainsKey(owningPlayer.id)) imapctMaterial = impactMaterials[owningPlayer.id];
        else
        {

            imapctMaterial = Instantiate(particleSystemRenderer.material);
            impactMaterials.Add(owningPlayer.id, imapctMaterial);

        }

        impactParticles.GetComponent<ParticleSystemRenderer>().material = imapctMaterial;
        impactParticles.GetComponent<ParticleSystemRenderer>().material.color = generalParticleColor;

    }

    [BurstCompile]
    private void OnDestroy()
    {

        shotInstance.release();

        for (int i = 0; i < spriteRenderer.materials.Length; i++) Destroy(spriteRenderer.materials[i]);

        owningPlayer.transform.localScale = Vector3.one;
        owningPlayer.nozzleBehaviour.transform.localScale = Vector3.one * 0.4f;

        if (aliveSound)
        {

            aliveInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            aliveInstance.release();

        }

    }

}
[BurstCompile]
[Serializable]
public struct ProjectileInitData
{

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
    public int burst;
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
    public bool retornToSender;
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


}