using BattleSquaresSDK;
using FMOD.Studio;
using FMODUnity;
using Steamworks;
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

[Preserve]
public sealed partial class PlayerBehaviour : MonoBehaviour, IPlayerHandle
{
    [SerializeField] double overrideMMR = 0;
    [ContextMenu("Override my mmr")]
    void OverrideMyMMR()
    {
        new EncryptedDouble(MMRlocation, 1000.0).Value = overrideMMR;
        LogCurrentMMRP();
    }

    [ContextMenu("Log my mmr")]
    void LogCurrentMMRP() => Debug.Log(new EncryptedDouble(MMRlocation, 1000.0).Value);

    public PlayerNeighbours neighbours;

    [SerializeField]
    Light2D playerLight;
    public const string MMRlocation = "SkillIssue";
    EncryptedDouble localMMR;
    double remoteMMR;
    public double previousMMR { get; private set; }
    public double MMR
    {
        get
        {
            if (GetNetworkID() == playerSynchronizer.localSquare.GetNetworkID())
            {
                if (localMMR == null) localMMR = new EncryptedDouble(MMRlocation, 1000.0);
                return localMMR.Value;
            }
            else return remoteMMR;
        }
        set
        {
            
            if (GetNetworkID() == playerSynchronizer.localSquare.GetNetworkID())
            {
                if (localMMR == null) localMMR = new EncryptedDouble(MMRlocation, value);
                else localMMR.Value = value;
            }
            else
            {
                remoteMMR = value;
            }
        }
    }
    public void StorePreviousMMR() => previousMMR = MMR;

    [SerializeField]
    ProximityPixelSenssor sensor;

    [SerializeField]
    PlayerColoringBehaviour coloringComponent;

    public PlayerColoringBehaviour PlayerColor => coloringComponent;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignMaterialToProjectile(in SpriteRenderer projectileRenderer) => PlayerColor.AssignMaterialToProjectile(projectileRenderer);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignMaterialToPlayer(in SpriteRenderer playerRenderer) => PlayerColor.AssignMaterialToPlayer(playerRenderer);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignMaterialToParticleRenderer(in ParticleSystemRenderer particleRenderer, in ParticleSystem particleSystem) => PlayerColor.AssignMaterialToParticleRenderer(particleRenderer, particleSystem);


    public bool selectedLegacyMap = true;
    public int selectedMap;

    public bool hasJump;
    public float fps = 6000;
    public float oneFifthFps;
    public float fpsCapture;
    public float oneSecondTimer;
    public float recievedDataInterval;
    public float dataUpdateHighSpeedTimer;
    public float timeSinceHit;

    public float climax;

    public bool isLocalPlayer = false;
    public bool newDataAvalible = false;
    public bool newColor = true;
    public bool ready = false;

    public Vector2 position;
    public float rotation;

    public Vector2 nozzlePosition;
    public Vector2 localNozzlePosition;
    public float nozzleRotation;

    public Vector2 velocity;
    public float angularVelocity;

    [SerializeField]
    private float hp = 20f;

    public float healthPoints
    {
        get
        {
            return hp;
        }
        set
        {

            float before = hp;
            float after = value;
            float delta = after - before;

            hp = value;
        }
    }

    public float maxHealthPoints = 20;
    public Vector3 hpBarScale;

    [NonSerialized]
    public float voiceVolume = 0.1f;
    [NonSerialized]
    public bool voiceMute = false;

    float slapIntensity;

    public int score;

    public FlagBehaviour flag;

    public Rigidbody2D rb;

    Transform nozzleTransform;
    public NozzleBehaviour nozzleBehaviour;
    public PlayerController playerController;
    PlayerSynchronizer playerSynchronizer;
    SpriteRenderer spriteRenderer;
    public Sprite pfp;
    ScoreManager scoreManager;
    MapSynchronizer mapSynchronizer;
    public ChatBubbleBehaviour chatBubbleBehaviour = null;

    [SerializeField]
    PlayerSpawnEffectBehaviour playerSpawnEffectBehaviourRef;
    PlayerSpawnEffectBehaviour playerSpawnEffectBehaviour = null;

    [SerializeField]
    public PhysicsMaterial2D physMat;

    public Collider2D col;

    [SerializeField]
    public SpriteRenderer healthbar;

    [SerializeField]
    DogTagBehaviour dogTag;

    public Sprite[] bodyFrames;
    public Sprite[] nozzleFrames;

    [SerializeField]
    EventReference deathSoundReference;
    public EventInstance deathSoundInstance;

    public delegate void ColorChange();
    ColorChange colorChange = () => { };

    Vector2 nozzlePositionOffset;
    Vector2 nozzleInputDirection;

    Vector2 lastRbVelocity;

    float newNozzlePositionTime;
    Vector2 nozzleReferencePosition;

    public Vector3 spawnPosition;
    public Vector3 deathPosition;


    public string playerName;

    public bool isDead = false;

    public int kills;
    public int killStreak;

    public bool scoreDeducted = false;

    public bool steamDataAvalible = false;
    public bool steamDataApplied = false;
    ulong steamId;

    public Vector2 toPos;
    public float newNozzleLerp;
    public Vector2 fromPos;
    public Vector2 nozzlePosOffset;
    float nozzlePositionSpeed = 13;
    bool controlled;
    bool flipFlop;
    Vector2 movementDirection = Vector2.zero;

    bool isSpawning;

    Transform playerTransform;
    Hunter hunter;

    float speedParticleTimer;

    [SerializeField]
    ParticleColorApplicant[] speedParticles;

    int speedParticleSwitcher;


    [SerializeField]
    EventReference playerSlap;
    EventInstance playerSlapSound;

    float slapTimer;
    public Friend friend;
    bool lastDeathState = false;
    float deathTimer;
    public bool spawnBuffer = false;
    Color frozenColor = Color.white;
    public bool newMods;

    private void Awake()
    {
        lastRbVelocity = new Vector2();
        playerTransform = transform;
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        hunter = FindAnyObjectByType<Hunter>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        pfp = null;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        nozzleBehaviour = GetComponentInChildren<NozzleBehaviour>();
        nozzleTransform = nozzleBehaviour.transform;
        fromPos = Vector2.up;
        toPos = Vector2.up;
        SceneManager.sceneLoaded += SceneManager_OnLoad;
        hpBarScale = Vector3.one;
        spriteRenderer = GetComponent<SpriteRenderer>();

        ApplyColors();
    }
    private void Start()
    {
        PlayerColor.SetColorHue(UnityEngine.Random.Range(0f, 1f));
        PlayerColor.AssignMaterialToPlayer(spriteRenderer);
        PlayerColor.AssignMaterialToPlayer(healthbar);
        PlayerColor.AssignMaterialToPlayer(nozzleBehaviour.spriteRenderer);
        try
        {

            DontDestroyOnLoad(gameObject);
            deathSoundInstance = RuntimeManager.CreateInstance(deathSoundReference);
            SetSpawnAndDeathPositions();

            if (isLocalPlayer)
            {

                GetComponentInChildren<NozzleBehaviour>().SetPlayerController(playerController, this);
                playerTransform.position = GameObject.FindGameObjectWithTag("Spawn").transform.position;
                playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();

            }

            ApplyColors();

            playerSlapSound = RuntimeManager.CreateInstance(playerSlap);

        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            SteamNetwork.currentLobby?.Leave();

            SteamNetwork.CreateNewLobby();

            PlayerSynchronizer playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();

            if (playerSynchronizer.IsHost)
            {

                playerSynchronizer.hostShutdown = true;
                playerSynchronizer.DisconnectPlayerLocally();

            }

            NetworkManager.Singleton.Shutdown(true);
            playerSynchronizer.DisconnectPlayerLocally();

            playerSynchronizer.hostShutdown = false;

        }

    }
    private void Update()
    {

        if (!steamDataApplied && steamDataAvalible) ApplySteamData();

#if UNITY_EDITOR
        newColor = true;
#endif

        if (newColor) ApplyColors();

        oneSecondTimer += Time.deltaTime * 10;
        dataUpdateHighSpeedTimer += Time.deltaTime * 2;
        hpBarScale = Vector3.one * (healthPoints / maxHealthPoints);
        nozzlePosition = nozzleTransform.position;
        healthbar.transform.localScale = hpBarScale;
        sensor.transform.localPosition = Vector3.zero;
        playerLight.transform.localPosition = Vector3.zero;
        ApplyPlayerAnimation();

        if (timeSinceHit < 1) timeSinceHit += Time.deltaTime * 3.5f;
        else if (timeSinceHit > 1) timeSinceHit = 1;

        if (oneSecondTimer >= 1f)
        {

            fps = fpsCapture;
            oneFifthFps = fps / 5f;

            fpsCapture = 0;
            oneSecondTimer = 0;

        }
        else fpsCapture += 10;

        if (isLocalPlayer)
        { 
            if (climax > 0) climax -= Time.deltaTime * 0.3f;
            else if (climax < 0) climax = 0; 
            if (rb.position.y < -60) RespawnPlayer(); 
        }

        if (rb.linearDamping > 0.1f) rb.linearDamping -= Time.deltaTime * 80;
        if (rb.angularDamping > 0.1f) rb.angularDamping -= Time.deltaTime * 80;
        if (rb.linearDamping < 0.1f) rb.linearDamping = 0.1f;
        if (rb.angularDamping < 0.1f) rb.angularDamping = 0.1f;

        slapIntensity = Mathf.Lerp(slapIntensity, rb.linearVelocity.magnitude + Mathf.Abs(rb.angularVelocity / 40f), Time.deltaTime * 10);
        slapTimer -= Time.deltaTime;
        if (slapTimer < 0) slapTimer = 0;

    }

    private void LateUpdate()
    {

        if (isSpawning)
        {
            if (playerSpawnEffectBehaviour)
            {
                playerTransform.position = playerSpawnEffectBehaviour.transform.position;
                rb.position = playerSpawnEffectBehaviour.transform.position;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0;
                rb.rotation = 0f;
                return;
            }
        }

        controlled = playerController;

        UpdateLifeState();

        if (spawnBuffer)
        {
            playerTransform.position = spawnPosition;
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            rb.rotation = 0;
            return;
        }

        if (controlled) SetMovementParameters(newMods);

        AnimateNozzleToAimDirection();

    }

    private void SceneManager_OnLoad(Scene arg0, LoadSceneMode arg1)
    {
        if (this)
        {
            if (isLocalPlayer)
            {
                if (!NetworkManager.Singleton.IsHost) ready = false;

                if (arg0.name == "GameScene")
                {
                    score = scoreManager.startScore;
                }

                playerSynchronizer.UpdateHealth();
                CancelInvoke("RevivePlayer");
            }
            else
            {
                if (!NetworkManager.Singleton.IsHost) ready = false;

                if (arg0.name == "GameScene") score = scoreManager.startScore;
            }

            SetSpawnAndDeathPositions();

            RevivePlayer();
        }
    }

    public void SpawnEffect()
    {

        isSpawning = true;

        spriteRenderer.enabled = false;
        healthbar.enabled = false;
        nozzleBehaviour.spriteRenderer.enabled = false;
        col.enabled = false;
        rb.simulated = false;

        playerSpawnEffectBehaviour = Instantiate(playerSpawnEffectBehaviourRef, playerTransform);
        playerSpawnEffectBehaviour.Init(this);

    }

    public void SetSpawnAndDeathPositions()
    {
        GameObject deathObj = GameObject.FindGameObjectWithTag("Death");
        deathPosition = deathObj.transform.position;

        if (BuiltMapSpawns.instance) spawnPosition = new float3((float2)BuiltMapSpawns.instance.GetSpawn(GetGameID()), playerTransform.position.z);
        else
        {
            GameObject spawnObj = GameObject.FindGameObjectWithTag("Spawn");
            if (spawnObj) spawnPosition = GameObject.FindGameObjectWithTag("Spawn").transform.position;
        }
        deathPosition.z = transform.position.z;
        spawnPosition.z = transform.position.z;
    }

    public void KillPlayer()
    {

        killStreak = 0;
        hunter.Die((byte)gameID);

        if (isLocalPlayer)
        {
            SetStats();
            playerController.CancelAllInputs();
        }
        else SetStats();

        void SetStats()
        {

            if (isLocalPlayer) mapSynchronizer.SpawnDogTag((byte) gameID, rb.position, rb.rotation, rb.linearVelocity / 2);
            healthPoints = maxHealthPoints;
            climax = 1;
            isDead = true;
        }
    }

    public void UpdateLifeState()
    {
        if(lastDeathState != isDead)
        {
            //Transitions States
            if (isDead)
            {
                playerTransform.position = deathPosition;
                rb.position = deathPosition;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = 0;
                rb.rotation = 0;
            }
            else
            {
                playerTransform.position = spawnPosition;
                rb.position = spawnPosition;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = 0;
                rb.rotation = 0;
                deathTimer = 0;
                isSpawning = false;
            }
        }
        //Always Applied States
        if (isDead)
        {
            deathTimer += Time.deltaTime;
            if (deathTimer > 2) RevivePlayer();
            if (playerLight.enabled) playerLight.enabled = false;
            if (sensor.enabled) sensor.enabled = false;
            if (spriteRenderer.enabled) spriteRenderer.enabled = false;
            if (healthbar.enabled) healthbar.enabled = false;
            if (nozzleBehaviour.spriteRenderer.enabled) nozzleBehaviour.spriteRenderer.enabled = false;
            if (col.enabled) col.enabled = false;
            if (rb.simulated) rb.simulated = false;
        }
        else
        {
            if (!playerLight.enabled) playerLight.enabled = true;
            if (!sensor.enabled) sensor.enabled = true;
            if (!spriteRenderer.enabled) spriteRenderer.enabled = true;
            if (!healthbar.enabled) healthbar.enabled = true;
            if (!nozzleBehaviour.spriteRenderer.enabled) nozzleBehaviour.spriteRenderer.enabled = true;
            if (!col.enabled) col.enabled = true;
            if (!rb.simulated)  rb.simulated = true;
        }

        lastDeathState = isDead;
    }

    public void RevivePlayer()
    {

        spawnBuffer = false;
        hunter.Spawn((byte)gameID);
        SetSpawnAndDeathPositions();

        isDead = false;
        scoreDeducted = false;

        UpdateLifeState();

        if (playerSpawnEffectBehaviour)
        {
            Destroy(playerSpawnEffectBehaviour.gameObject);
            playerSpawnEffectBehaviour = null; 
        } 

        CancelInvoke("RevivePlayer");

        if (isLocalPlayer)
        { 
            healthPoints = maxHealthPoints;
            playerController.CancelAllInputs();
            playerSynchronizer.UpdateHealth();
        }
    }

    public void RespawnPlayer()
    {
        SetSpawnAndDeathPositions();
        rb.position = spawnPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = 0;
        rb.rotation = 0;
        playerTransform.position = spawnPosition;
    }
}

//Identity shit
public partial class PlayerBehaviour : MonoBehaviour, IPlayerHandle
{
    public void AssertSteamDataAvalible(ulong steamId)
    {
        steamDataAvalible = true;
        this.steamId = steamId;
    }
    public void CreateTextureFromBoolArray10BY10(bool[] boolArray, int frameIndex)
    {

        Span<bool> rotatedArray = stackalloc bool[100];
        for (int i = 0; i < 100; i++) rotatedArray[i] = boolArray[99 - i];
        Texture2D texture = new Texture2D(10, 10, UnityEngine.TextureFormat.RGBA32, false);
        texture.filterMode = UnityEngine.FilterMode.Point;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                int index = i * 10 + j;
                Color color = rotatedArray[index] ? Color.white : Color.clear;
                texture.SetPixel(j, i, color);
            }
        }

        texture.Apply();
        bodyFrames[frameIndex] = Sprite.Create(texture, new Rect(0, 0, 10, 10), new Vector2(0.5f, 0.5f), 10);
        if (frameIndex == 0) spriteRenderer.sprite = bodyFrames[frameIndex];
    }

    public void CreateTextureFromBoolArray4BY4(bool[] boolArray, int frameIndex)
    {

        Span<bool> rotatedArray = stackalloc bool[16];

        rotatedArray[0] = boolArray[3];
        rotatedArray[1] = boolArray[7];
        rotatedArray[2] = boolArray[11];
        rotatedArray[3] = boolArray[15];
        rotatedArray[4] = boolArray[2];
        rotatedArray[5] = boolArray[6];
        rotatedArray[6] = boolArray[10];
        rotatedArray[7] = boolArray[14];
        rotatedArray[8] = boolArray[1];
        rotatedArray[9] = boolArray[5];
        rotatedArray[10] = boolArray[9];
        rotatedArray[11] = boolArray[13];
        rotatedArray[12] = boolArray[0];
        rotatedArray[13] = boolArray[4];
        rotatedArray[14] = boolArray[8];
        rotatedArray[15] = boolArray[12];

        Texture2D texture = new Texture2D(4, 4, UnityEngine.TextureFormat.RGBA32, false);
        texture.filterMode = UnityEngine.FilterMode.Point;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int index = i * 4 + j;

                Color color = rotatedArray[index] ? Color.white : Color.clear;
                texture.SetPixel(j, i, color);
            }
        }
        texture.Apply();
        nozzleFrames[frameIndex] = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        if (frameIndex == 0) nozzleBehaviour.spriteRenderer.sprite = nozzleFrames[frameIndex];
    }

    public void ApplyColors()
    {
        PlayerColor.RefreshColorComponents();
        spriteRenderer.color = PlayerColor.ExposedHealthColor;
        healthbar.color = PlayerColor.PrimaryColor;
        nozzleBehaviour.spriteRenderer.color = PlayerColor.NozzleColor;
        sensor.gridSpaceColor.color = PlayerColor.PrimaryColor;
        playerLight.color = PlayerColor.LightColor;
        newColor = false;
    }

    void ApplySteamData()
    {
        GetImageData(steamId);
        friend = new Friend(steamId);
        playerName = friend.Name;
        steamDataApplied = true;
    }

    public async void GetImageData(SteamId steamId)
    {
        Steamworks.Data.Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);

        byte[] imageData = image.Value.Data;
        uint imageWidth = image.Value.Width;
        uint imageHeight = image.Value.Height;
        Vector2 imageDimentions = new Vector2(image.Value.Width, image.Value.Height);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, UnityEngine.TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        pfp = Sprite.Create(spriteTexture, spriteRect, spritePivot);

    }
}

//Movement shit
public partial class PlayerBehaviour : MonoBehaviour, IPlayerHandle
{

    float animationTimer;
    public float frameRate = 10;
    int animationIndex;
    int lastAnimationIndex;

    float acceleration;
    float maxSpeed;

    Vector2 velParam;
    float xLimiter;
    float yLimiter;
    Vector2 forceLimiter;
    Vector2 jumpVelocity;
    Vector2 jumpDirection;
    float jumpLimiter;

    public AimDirection aimDirectionEnum = AimDirection.North;
    public Vector2 aimDirection
    {
        get => AimDirectionToVector(aimDirectionEnum);
        set
        {
            aimDirectionEnum = VectorToAimDirection(value);
            if (isLocalPlayer) playerSynchronizer.UpdateNozzle(GetGameID());
        }
    }
    public Vector2 moveDirection;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 AimDirectionToVector(AimDirection dir)
    {
        return dir switch
        {
            AimDirection.East => Vector2.right,
            AimDirection.NorthEast => new Vector2(1, 1).normalized * 1.145f,
            AimDirection.North => Vector2.up,
            AimDirection.NorthWest => new Vector2(-1, 1).normalized * 1.145f,
            AimDirection.West => Vector2.left,
            AimDirection.SouthWest => new Vector2(-1, -1).normalized * 1.145f,
            AimDirection.South => Vector2.down,
            AimDirection.SouthEast => new Vector2(1, -1).normalized * 1.145f,
            _ => Vector2.zero
        };
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AimDirection VectorToAimDirection(Vector2 v)
    {
        if (Mathf.Approximately(v.magnitude, 0)) return aimDirectionEnum;
        float angle = Mathf.Atan2(v.y, v.x);
        if (angle < 0) angle += Mathf.PI * 2f;
        int index = Mathf.RoundToInt(angle / (Mathf.PI / 4f)) % 8;
        return (AimDirection)index;
    }


    public enum AimDirection : byte
    {
        East = 0,
        NorthEast = 1,
        North = 2,
        NorthWest = 3,
        West = 4, 
        SouthWest = 5,
        South = 6,
        SouthEast = 7
    }


    [SerializeField]
    public ParticleBehaviour jumpParticleRef;

    private void FixedUpdate()
    { 

        if(!Mathf.Approximately(lastRbVelocity.sqrMagnitude, rb.linearVelocity.sqrMagnitude))
        {
            if(isLocalPlayer) playerSynchronizer.UpdateRigidBody(GetGameID());
        }
        flipFlop = !flipFlop;
        if (flipFlop) return; 
        if (controlled)
        {
            ApplyTargetMovement();
            ReAdjustMovementValues();
        } 
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        { 
            if (!hasJump) slapTimer = 0f; 
            if (slapTimer == 0)
            {
                if (!isLocalPlayer || SceneManager.GetActiveScene().name.Equals("LobbyScene"))
                { 
                    Vector2 toCam = Camera.main.transform.position - playerTransform.position;
                    float soundDirection = MyExtentions.ConvertVector2ToAngle(toCam.normalized);
                    float distance = toCam.magnitude; 
                    playerSlapSound.setParameterByName("Direction", soundDirection);
                    playerSlapSound.setParameterByName("Distance", distance); 
                }
                else
                { 
                    playerSlapSound.setParameterByName("Direction", 0);
                    playerSlapSound.setParameterByName("Distance", 0); 
                } 
                playerSlapSound.setParameterByName("Player Speed", slapIntensity);
                playerSlapSound.setVolume(MySettings.Volume);
                playerSlapSound.start();
                slapTimer = 0.27f; 
            } 
            hasJump = true;
            slapIntensity = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PlayNozzleRecoilAnimation() => nozzleBehaviour.transform.localPosition = Vector3.Lerp(nozzleBehaviour.transform.localPosition, Vector3.zero, 0.99f);

    private void AnimateNozzleToAimDirection()
    {
        float rot = 0;
        Vector3 pos = nozzleBehaviour.transform.position;
        pos = Vector3.Lerp(pos, transform.position + (Vector3)aimDirection, Time.deltaTime * 25);
        nozzleBehaviour.transform.position = pos; 
        Vector2 delta = transform.position - nozzleBehaviour.transform.position; 
        rot = MyExtentions.ConvertVector2ToAngle(delta);
        nozzleBehaviour.transform.rotation = Quaternion.Euler(0f, 0f, rot - 180);
    }

    void SetMovementParameters(bool newMod)
    {

        if (newMod)
        {
            acceleration = 130f * Mods.at[8];
            maxSpeed = 23.5f * Mods.at[1];
            newMods = false;
        }

        movementDirection = Vector2.Lerp(movementDirection, moveDirection, math.clamp(Time.deltaTime * 100, 0, 1));

        (velParam.x, velParam.y) = (math.clamp(rb.linearVelocityX, -maxSpeed, maxSpeed), math.clamp(rb.linearVelocityY, -maxSpeed, maxSpeed));
        (xLimiter, yLimiter) = (math.clamp(math.abs(movementDirection.x - (velParam.x / maxSpeed)), 0, 1), math.clamp(math.abs(movementDirection.y - (velParam.y / maxSpeed)), 0, 1));
        forceLimiter = new Vector2(xLimiter, yLimiter);

        jumpLimiter = 17.5f - math.clamp(rb.linearVelocityY / 2, -5, 10);
        jumpDirection = (Vector2.up + (movementDirection * 0.2f)).normalized;
        jumpVelocity = (jumpDirection * jumpLimiter) * Mods.at[2];

        MyExtentions.GetClosestEnvironmentPoint(rb.position);
        if (playerController.inputJump)
        {
            Vector2 calculatedVelocity = rb.linearVelocity + jumpVelocity;
            if (calculatedVelocity.y < 10f) calculatedVelocity.y = 10f;
            rb.linearVelocity = calculatedVelocity;
            Vector2 normalizedDirection = rb.linearVelocity.normalized;
            playerSynchronizer.SpawnJumpParticles(rb.position, Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg, GetGameID());
            playerController.inputJump = false;
        }
    }

    void ApplyTargetMovement()
    {
        rb.AddForce(movementDirection * acceleration * forceLimiter, ForceMode2D.Force);
        if (math.abs(rb.angularVelocity / 360) < 1f) rb.AddTorque(-movementDirection.x / 0.85f, ForceMode2D.Force);
    }

    void ReAdjustMovementValues()
    {
        (float posX, float posY) = (rb.position.x, rb.position.y);
        rb.position = new Vector2(math.clamp(posX, -64, 64), math.clamp(posY, -64, 64));
        if (rb.rotation > 360) rb.rotation -= 360;
        if (rb.rotation < 0) rb.rotation += 360;
        rb.angularVelocity = math.clamp(rb.angularVelocity, -1000, 1000);
    }

    public void AnimatePlayer() => animationTimer = 1;

    void ApplyPlayerAnimation()
    {

        if (animationTimer > 0) animationTimer -= Time.deltaTime * (frameRate / nozzleFrames.Length);
        if (animationTimer < 0) animationTimer = 0;
        if (animationTimer == 0) animationIndex = 0;
        else animationIndex = Mathf.FloorToInt((1 - animationTimer) * bodyFrames.Length);

        if (animationIndex != lastAnimationIndex)
        {
            nozzleBehaviour.spriteRenderer.sprite = nozzleFrames[animationIndex];
            spriteRenderer.sprite = bodyFrames[animationIndex];
            lastAnimationIndex = animationIndex;
        }
    }
}
//Id shit
public partial class PlayerBehaviour : MonoBehaviour, IPlayerHandle
{
    public byte GetGameID() => gameID;
    public byte GetNetworkID() => networkID;

    public void SetGameID(byte gameID) => this.gameID = gameID;
    public void SetNetworkID(byte networkID) => this.networkID = networkID;

    private byte gameID;
    private byte networkID;
}

//interface shit
public partial class PlayerBehaviour : MonoBehaviour, IPlayerHandle
{
    public event Action<IPlayerHandle> OnDestroyed;
    private void OnDestroy()
    {
        OnDestroyed?.Invoke(this);
    }

    public string Name => playerName;

    public ulong NetworkID => gameID;

    public ulong SteamID => steamId;

    public bool IsLocal => isLocalPlayer;

    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void SetPosition(System.Numerics.Vector2 position)
    {
        rb.position = new Vector2(position.X, position.Y);
        transform.position = new Vector3(position.X, position.Y, transform.position.z);
        playerSynchronizer.UpdateRigidBody(GetGameID());
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public System.Numerics.Vector2 GetPosition()
    {
        Vector2 pos = rb.position;
        return new System.Numerics.Vector2(pos.x, pos.y);
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void SetVelocity(System.Numerics.Vector2 position)
    {
        rb.linearVelocity = new Vector2(position.X, position.Y);
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public System.Numerics.Vector2 GetVelocity()
    {
        Vector2 vel = rb.linearVelocity;
        return new System.Numerics.Vector2(vel.x, vel.y);
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void SetRotation(float rotation)
    {
        rb.rotation = rotation;
        transform.rotation = Quaternion.Euler(0, 0, rotation);
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public float GetRotation()
    {
        return rb.rotation;
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void SetAngularVelocity(float rotation)
    {
        rb.angularVelocity = rotation;
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public float GetAngularVelocity()
    {
        return rb.angularVelocity;
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public float GetHealth()
    {
        return healthPoints;
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void SetHealth(float health)
    {
        healthPoints = health;
        playerSynchronizer.UpdateHealth();
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public float GetHealthCap()
    {
        return maxHealthPoints;
    }
    [Preserve]
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void SetHealthCap(float cap)
    {
        maxHealthPoints = cap;
    }
}
