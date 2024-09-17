using UnityEngine;
using static PlayerSynchronizer;

public sealed class FlagBehaviour : MonoBehaviour, ISync
{

    public ulong ownerId = 20000;

    [SerializeField]
    Vector3 flyingSize;

    [SerializeField]
    Vector3 idleSize;

    Vector3 fromSize;
    Vector3 toSize;

    SizeState sizeState = SizeState.idle;
    SizeState lastSizeState = SizeState.idle;

    float sizeStateTimer = 1;
    float sizeStateMultiplier = 1;

    public Color darkColor;
    public Color color;

    FlagPost[] flagPosts;

    public ParticleSystem.MainModule mainParticleModule;

    [SerializeField]
    float force = 1;

    public int id;

    Transform spawn;

    public SpriteRenderer spriteRenderer;
    MapSynchronizer mapSync;
    public PlayerBehaviour playerBehaviour;
    PlayerSynchronizer playerSynchronizer;
    public new Collider2D collider;
    Rigidbody2D following;
    Transform post;
    Transform child;
    public new ParticleSystem particleSystem;
    public FlagBehaviour follower;

    float normalMultiplier = 2;

    [SerializeField]
    float offset;

    bool score;
    public bool collected;
    bool appliedColor;

    float rotation;
    float emissionDelta = 1;

    public Rigidbody2D rb;

    float timer;

    public FlagActivityState activityState = FlagActivityState.Idle;

    public ObjectiveType objectiveType = ObjectiveType.flag;

    private void Awake()
    {

        spawn = new GameObject("Spawn - " + name).GetComponent<Transform>();
        id = ((transform.parent.GetSiblingIndex() + 1) * transform.parent.childCount) - (transform.GetSiblingIndex() + 1);
        child = transform.GetChild(0);
        particleSystem = child.GetComponent<ParticleSystem>();
        mainParticleModule = particleSystem.main;
        post = transform.parent;
        collider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mapSync = GameObject.FindGameObjectWithTag("Sync").GetComponent<MapSynchronizer>();
        playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();
        rb = GetComponent<Rigidbody2D>();
        flagPosts = FindObjectsByType<FlagPost>(FindObjectsSortMode.None);
        fromSize = idleSize;
        toSize = idleSize;

    }

    void Start()
    {
        spawn.SetParent(transform.parent, true);
        spawn.transform.localPosition = transform.localPosition;
    }

    private void Update()
    {

        if (!spriteRenderer.enabled) return;

        if(sizeState != lastSizeState)
        {
            if (sizeState == SizeState.idle)
            {
                toSize = idleSize;
                sizeStateMultiplier = 5;
            }
            else
            {
                toSize = flyingSize;
                sizeStateMultiplier = 3;
            }

            fromSize = transform.localScale;
            lastSizeState = sizeState;
            sizeStateTimer = 0;
        }

        if (sizeStateTimer < 1)
        {
            sizeStateTimer += Time.deltaTime * sizeStateMultiplier;
            LerpSize();
        }
        else
        {
            sizeStateTimer = 1;
            LerpSize();
        }

        void LerpSize()
        {
            transform.localScale = Vector3.Lerp(fromSize, toSize, Mathf.SmoothStep(0, 1, sizeStateTimer));
        }

        if (!appliedColor)
        {
            appliedColor = true;
            spriteRenderer.color = color;
            mainParticleModule.startColor = darkColor;
        }

        if (activityState == FlagActivityState.Idle)
        {
            rotation = (Mathf.SmoothStep(0, 1, mapSync.pingPong2S) - 0.5f) * 30;
            float rotationToPost = Mathf.Rad2Deg * Mathf.Atan2((transform.position - post.position).y, (transform.position - post.position).x);
            transform.rotation = Quaternion.Euler(0, 0, rotationToPost + rotation - 90);

        }
        else
        {

            if (timer > emissionDelta)
            {
                timer = 0;
                particleSystem.Emit(1);
            }

            float rotationToTarget = Mathf.Rad2Deg * Mathf.Atan2((rb.linearVelocity.normalized).y, (rb.linearVelocity.normalized).x);
            child.rotation = Quaternion.Euler(0, 0, rotationToTarget + 90);

        }

        if (!collected) return;
        if (!playerBehaviour) return;
        if (!playerBehaviour.isLocalPlayer) return;

        foreach (FlagPost post in flagPosts)
        {
            if (post.ownerId == playerBehaviour.id)
            {

                Vector3 toPost = post.transform.position - transform.position;

                if (toPost.magnitude < 3.5f)
                {
                    playerBehaviour.score++;
                    rb.AddForce(toPost.normalized * 8, ForceMode2D.Impulse);
                    SetToReturnToSpawnChain(false, false);
                    rb.angularVelocity = UnityEngine.Random.Range(0, 1) == 0 ? -127 : 127;
                    playerSynchronizer.UpdateScore();
                    return;
                }

            }

        }

    }
    private void FixedUpdate()
    {

        if (!spriteRenderer.enabled) return;

        UpdateGeneralParameters();

        UpdateForState();

        SetLimiters();

    }

    void SetLimiters()
    {

        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -127, 127);
        rb.linearVelocityX = Mathf.Clamp(rb.linearVelocityX, -60, 60);
        rb.linearVelocityY = Mathf.Clamp(rb.linearVelocityY, -60, 60);
        rb.rotation = Mathf.Repeat(rb.rotation, 360);

    }

    void UpdateGeneralParameters()
    {
        timer += Mathf.Clamp(Time.deltaTime * rb.linearVelocity.magnitude * 0.6f, 0, 0.055f);
    }

    void UpdateForState()
    {
        switch (activityState)
        {
            case FlagActivityState.Idle: break;
            case FlagActivityState.FollowTarget: UpdateFollowTarget(); break;
            case FlagActivityState.ReturnToSpawn: UpdateReturnToSpawn(); break;
        }
    }

    void UpdateFollowTarget()
    {

        if (!playerBehaviour)
        {
            SetToReturnToSpawnChain(true, false);
            return;
        }

        if((playerBehaviour.isLocalPlayer && playerBehaviour.isDead) || score) SetToReturnToSpawnChain(true, false);
        else
        {
            Vector2 toTarget = following.position - rb.position;
            Vector2 normal = toTarget.normalized * normalMultiplier;
            rb.AddForce((toTarget - normal) * force, ForceMode2D.Force);

        }

    }

    void UpdateReturnToSpawn()
    {

        Vector2 toSpawn = (Vector2) spawn.transform.position - rb.position;
        if (toSpawn.magnitude < 2) toSpawn = toSpawn.normalized * 1.5f;
        rb.AddForce(Vector2.ClampMagnitude(toSpawn, 3f) * force * 1.3f, ForceMode2D.Force);

        if (((Vector2) spawn.transform.position - rb.position).magnitude < 0.5f) SetToIdle();

    }

    public void SetToIdle()
    {
        activityState = FlagActivityState.Idle;
        transform.SetParent(post, true);
        rb.rotation = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        transform.position = spawn.position;

        transform.rotation = Quaternion.Euler(0, 0, 0);
        particleSystem.transform.rotation = Quaternion.Euler(0, 0, 0);

        rotation = (Mathf.SmoothStep(0, 1, mapSync.pingPong2S) - 0.5f) * 30;
        float rotationToPost = Mathf.Rad2Deg * Mathf.Atan2((transform.position - post.position).y, (transform.position - post.position).x);
        transform.rotation = Quaternion.Euler(0, 0, rotationToPost + rotation - 90);

        sizeState = SizeState.idle;
        score = false;
        normalMultiplier = 2f;
        particleSystem.Play();
        collected = false;
        spriteRenderer.color = color;
        mainParticleModule.startColor = darkColor;
    }

    public void SetToFollowTarget(PlayerBehaviour playerBehaviour, bool remote)
    {

        if (playerBehaviour.flag != null)
        {
            following = playerBehaviour.flag.GetAndSetFollower(this).rb;
            normalMultiplier = 1f;
        }
        else
        {
            normalMultiplier = 2f;
            following = playerBehaviour.rb;
        }

        rb.constraints = RigidbodyConstraints2D.None;
        emissionDelta = 1f;
        playerBehaviour.flag = this;
        sizeState = SizeState.flying;
        particleSystem.Stop();
        transform.SetParent(transform.parent.parent, true);
        this.playerBehaviour = playerBehaviour;
        activityState = FlagActivityState.FollowTarget;

        if (remote) return;
        mapSync.FlagStateChange(FlagActivityState.FollowTarget, id, playerBehaviour.id, false);

    }

    public void SetToReturnToSpawnChain(bool chain, bool remote)
    {
        spriteRenderer.color = Color.white;
        mainParticleModule.startColor = Color.gray;
        emissionDelta = 0.5f;
        activityState = FlagActivityState.ReturnToSpawn;
        collected = false;

        if (chain) Chain();
        else Single();

        void Chain()
        {
            if (playerBehaviour)
            {
                if (playerBehaviour.flag) if (playerBehaviour.flag == this) playerBehaviour.flag = null;
            }
            follower = null;
            following = null;
        }

        void Single()
        {
            if (follower)
            {
                follower.following = following;
                if (playerBehaviour.flag) if (playerBehaviour.flag == this) playerBehaviour.flag = follower;
            }
            else
            {
                if (playerBehaviour.flag) if (playerBehaviour.flag == this) playerBehaviour.flag = null;
            }
            following = null;
            follower = null;
        }

        playerBehaviour = null;

        if (remote) return;
        mapSync.FlagStateChange(FlagActivityState.ReturnToSpawn, id, 0, chain);


    }

    public FlagBehaviour GetAndSetFollower(FlagBehaviour newFollower)
    {
        if(follower != null)
        {
            return follower.GetAndSetFollower(newFollower);
        }
        else
        {
            follower = newFollower;
            return this;
        }
    }

    public void RegisterHit(ProjectileBehaviour projectileBehaviour)
    {

        if (projectileBehaviour.travelDistance > 25) return;

        if (projectileBehaviour.ownerId != ownerId)
        {

            if (activityState == FlagActivityState.Idle)
            {
                CollectFlag(projectileBehaviour);
            }
            else if (activityState == FlagActivityState.FollowTarget)
            {
                SetToReturnToSpawnChain(false, false);
            }

        }
        else
        {

            if (activityState == FlagActivityState.FollowTarget)
            {
                SetToReturnToSpawnChain(false, false);
            }

        }

    }

    void CollectFlag(ProjectileBehaviour projectileBehaviour)
    {

        collected = true;

        foreach (PlayerData playerData in mapSync.GetComponent<PlayerSynchronizer>().playerIdentities)
        {

            if (playerData.id == projectileBehaviour.ownerId) SetToFollowTarget(playerData.square, false);

        }
    }

    public void SyncRigidBody(SyncData updatedPosition)
    {

        Vector2 pos, vel;
        float ang, angVel;
        pos.x = updatedPosition.posX;
        pos.y = updatedPosition.posY;
        vel.x = updatedPosition.velX;
        vel.y = updatedPosition.velY;
        angVel = updatedPosition.angVel;
        ang = updatedPosition.ang;


        rb.position = pos;
        rb.linearVelocity = vel;
        rb.rotation = ang;
        rb.angularVelocity = angVel;

    }

    public SyncData FetchRigidBody()
    {

        SyncData syncData = new SyncData();
        syncData.posX = rb.position.x;
        syncData.posY = rb.position.y;
        syncData.velX = rb.linearVelocity.x;
        syncData.velY = rb.linearVelocity.y;
        syncData.angVel = rb.angularVelocity;
        syncData.ang = rb.rotation;

        return syncData;

    }

    public int GetId()
    {

        return id;

    }

    public bool ShouldSync()
    {

        if (!playerBehaviour) return false;
        if (playerBehaviour.isLocalPlayer && collected) return true;
        return false;

    }

    public void DoSync()
    {
        mapSync.FlagPositionUpdate(this, id);
    }

    public enum SizeState
    {
        flying,
        idle
    }

}

public enum FlagActivityState
{
    FollowTarget,
    ReturnToSpawn,
    Idle
}

public struct SyncData
{
    public float posX, posY, velX, velY, angVel, ang;
}
