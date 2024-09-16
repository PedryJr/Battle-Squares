using FMOD.Studio;
using FMODUnity;
using System;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class ProjectileBehaviour : MonoBehaviour
{

    public float initDamage;

    [SerializeField]
    Transform boom;

    public float damageScaleOverTime;

    public float damage;
    public float aoeDamage;

    public float acceleration;

    public uint projectileID;

    public bool IsLocalProjectile;

    public Rigidbody2D rb;

    public float timeAlive;
    public float fullTimeAlive;

    public ProjectileManager projectileManager;

    public bool holdable;

    public float travelDistance;
    Vector2 lastPos;

    [SerializeField]
    public float recoil;

    public bool destroyed;

    public bool instaDestroy = false;

    public bool skipAoeOnHit;

    public ulong ownerId;

    public bool hit;

    bool stuck;
    GameObject stuckTo;

    [SerializeField]
    GameObject impactParticle;

    ParticleSystemRenderer trailParticles;

    SpriteRenderer spriteRenderer;

    CameraAnimator cameraAnimator;

    Color generalParticleColor;

    [SerializeField]
    EventReference shotReference;

    [SerializeField]
    EventReference hitSoundReference;

    EventInstance shotInstance;

    PlayerBehaviour playerHit;
    FlagBehaviour flagHit;

    Collider2D projectileCollider;

    [SerializeField]
    public ProjectileInitData data;

    float morphLerp;
    Vector3 startMorph;
    Vector3 endMorph;

    private void Awake()
    {
        projectileCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        trailParticles = GetComponentInChildren<ParticleSystemRenderer>();
        cameraAnimator = Camera.main.GetComponent<CameraAnimator>();
    }

    private void Start()
    {

        if (Mathf.Abs(data.burst) != 0) return;

        shotInstance = RuntimeManager.CreateInstance(shotReference);
        shotInstance.setParameterByName("CameraPositionX", transform.position.x - Camera.main.transform.position.x);
        shotInstance.setParameterByName("Power", data.speed / 65f);
        shotInstance.setVolume(MySettings.volume);
        shotInstance.setPitch(1f + UnityEngine.Random.Range(-0.05f, 0.05f));
        shotInstance.start();

        if (cameraAnimator && IsLocalProjectile) cameraAnimator.Shake();

    }

    public void InitializeBullet(ProjectileInitData data)
    {
        
        initDamage = data.baseDamage;
        aoeDamage = data.aoeDamage;
        damageScaleOverTime = data.damageTimeScale;
        skipAoeOnHit = data.skipAoeOnTargetHit;

        endMorph = data.targetMorph;
        startMorph = transform.localScale;

        data.id++;

        Vector2 initialOffset = new Vector2(data.fluctuation[0], data.fluctuation[1]);
        data.direction = (data.direction + initialOffset).normalized;

        float rotation = Mathf.Rad2Deg * Mathf.Atan2(data.direction.y, data.direction.x);
        Quaternion rotationQ = Quaternion.Euler(0, 0, rotation);
        Vector2 velocity = data.direction * data.speed;
        Vector2 position = data.position;

        projectileID = data.id;

        projectileManager = data.projectileManager;
        IsLocalProjectile = data.IsLocalProjectile;

        rb.linearVelocity = velocity;
        rb.position = position;
        rb.rotation = rotation;
        transform.position = position;
        transform.rotation = rotationQ;

        if (data.noGravity) rb.gravityScale = 0f;

        spriteRenderer.color = data.projectileColor;
        generalParticleColor = data.projectileDarkerColor;

        Material trailParticleMaterial = Instantiate(trailParticles.material);
        trailParticles.material = trailParticleMaterial;
        trailParticles.material.color = generalParticleColor;

        damage = initDamage;

        acceleration = data.acceleration;

        this.data = data;

        if (Mathf.Abs(data.burst) > 0)
        {
            data.id++;
            int burst = Mathf.Abs(data.burst);

            Vector2 offset = new Vector2(data.burstData[burst * 2 -1] * -data.burst, data.burstData[(burst * 2) -2] * data.burst);
            if(rb.linearVelocity.x < 0) offset.x *= -1;
            if(rb.linearVelocity.y < 0) offset.y *= -1;
            if (Mathf.Abs(rb.linearVelocity.x) < 0.001f || Mathf.Abs(rb.linearVelocity.x) < 0.001f) offset *= 2;
            burst--;

            rb.linearVelocity = (rb.linearVelocity + offset).normalized * data.speed;
            rb.angularVelocity = UnityEngine.Random.Range(15f, -15f);
            data.position += new Vector2(UnityEngine.Random.Range(-0, 0), UnityEngine.Random.Range(-0, 0));
            data.burst = (data.burst > 0 ? burst : -burst) * -1;

            GameObject newBurst = Instantiate(gameObject, transform.parent);
            newBurst.GetComponent<ProjectileBehaviour>().InitializeBullet(data);

        }

        if (!IsLocalProjectile) gameObject.layer = LayerMask.NameToLayer("RemoteProjectile");
        projectileManager.projectiles.Add(this);
        lastPos = rb.position;

    }

    private void Update()
    {

        if (IsLocalProjectile) LocalUpdate();

    }

    private void FixedUpdate()
    {

        damage += (Time.deltaTime * damageScaleOverTime);
        timeAlive += Time.deltaTime;

        if (data.enableMorph)
        {
            morphLerp = Mathf.SmoothStep(0, 1, Mathf.Clamp01(timeAlive / data.timeToMorph));
            transform.localScale = Vector3.Lerp(startMorph, endMorph, morphLerp);
        }
        rb.linearVelocity += rb.linearVelocity * (acceleration * Time.deltaTime);

        travelDistance += (rb.position - lastPos).magnitude;
        lastPos = rb.position;

        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, data.speedLimit);
        if(rb.linearVelocity.magnitude < data.minSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * data.minSpeed;
        }

    }

    void LocalUpdate()
    {

        if(!destroyed && timeAlive > data.lifeTime) destroyed = true;

        if (destroyed)
        {

            if (!instaDestroy)
            {

                

                foreach (PlayerData player in projectileManager.playerSynchronizer.playerIdentities)
                {
                    if (player.id == ownerId) continue;
                    if (Vector2.Distance(rb.position, player.square.rb.position) > data.aoe) continue;
                    if (Physics2D.Linecast(rb.position, player.square.rb.position, LayerMask.GetMask("Environment")).collider != null) continue;
                    if (playerHit) if (skipAoeOnHit && player.square == playerHit) continue;

                    Vector2 direction = (player.square.rb.position - rb.position).normalized;

                    player.square.timeSinceHit = 0.25f;
                    projectileManager.playerSynchronizer.UpdatePlayerHealth(player.square.id, aoeDamage, ownerId, direction * data.knockback);

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

    }

    private void OnTriggerEnter2D(Collider2D collider)
    {

        CollisionCheck(collider.gameObject);

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        CollisionCheck(collision.gameObject);

    }

    void CollisionCheck(GameObject collidedWith)
    {

        PlayerBehaviour playerBehaviour = collidedWith.gameObject.GetComponent<PlayerBehaviour>();
        FlagBehaviour flagBehaviour = collidedWith.GetComponent<FlagBehaviour>();
        bool environment = collidedWith.layer == LayerMask.NameToLayer("Environment");

        if (destroyed) return;
        if (!IsLocalProjectile) return;


        if (playerBehaviour) PlayerCollisionCheck(playerBehaviour);
        if (flagBehaviour) FlagCollisionCheck(flagBehaviour);
        if (environment) EnvironmentCollisionCheck();

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
                Vector2 direction = (playerBehaviour.rb.position - rb.position).normalized;
                projectileManager.playerSynchronizer.UpdatePlayerHealth(playerBehaviour.id, damage, ownerId, direction * data.knockback);
                playerBehaviour.timeSinceHit = 0.25f;

            }

        }

    }

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

    void EnvironmentCollisionCheck()
    {
        if (data.dieOnImpact)
        {
            destroyed = true;
            spriteRenderer.enabled = false;
            hit = true;
        }
    }

    public void OnDespawn(bool hit)
    {

        DestroyThisProjectile(hit);

    }

    void DestroyThisProjectile(bool hit)
    {

        if (hit)
        {
            EventInstance eventInstance = RuntimeManager.CreateInstance(hitSoundReference);
            eventInstance.setParameterByName("CameraPositionX", transform.position.x - Camera.main.transform.position.x);
            eventInstance.start();
        }
        GameObject impactParticles = Instantiate(impactParticle, boom.transform.position, transform.rotation, null);
        Material particleMaterial = Instantiate(impactParticles.GetComponent<ParticleSystemRenderer>().material);
        impactParticles.GetComponent<ParticleSystemRenderer>().material = particleMaterial;
        impactParticles.GetComponent<ParticleSystemRenderer>().material.color = generalParticleColor;

        if (timeAlive >= data.lifeTime)
        {
            impactParticles.transform.localScale *= 0.65f;
        }

        Destroy(gameObject);

    }

}

[Serializable]
public struct ProjectileInitData
{

    public ProjectileManager projectileManager;
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

    public bool enableMorph;
    public Vector3 targetMorph;
    public float timeToMorph;

}