using FMOD.Studio;
using FMODUnity;
using MathNet.Numerics;
using Steamworks;
using System;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
[BurstCompile]
public sealed class PlayerBehaviour : MonoBehaviour
{
    public int selectedMap;
    public ulong id;

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

    public float healthPoints = 20;
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
    Transform deathChamber;
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

    Transform spawn;

    public Sprite[] bodyFrames;
    public Sprite[] nozzleFrames;

    [SerializeField]
    EventReference deathSoundReference;
    public EventInstance deathSoundInstance;

    public delegate void ColorChange();
    ColorChange colorChange = () => { };

    Vector2 nozzlePositionOffset;
    Vector2 nozzleInputDirection;

    float newNozzlePositionTime;
    Vector2 nozzleReferencePosition;

    public Vector3 spawnPosition;

    public Color playerColor;
    public Color playerDarkerColor;

    public string playerName;

    public float h;
    float s, v;
    public bool isDead = false;

    public int kills;
    public int killStreak;

    public bool scoreDeducted = false;

    public bool steamDataAvalible = false;
    public bool steamDataApplied = false;
    ulong steamId;

    public float newNozzleLerp;
    public Vector2 fromPos;
    public Vector2 toPos;
    public Vector2 nozzlePosOffset;
    float nozzlePositionSpeed = 13;
    bool controlled;
    bool flipFlop;
    Vector2 movementDirection = Vector2.zero;

    bool isSpawning;

    Transform playerTransform;
    Hunter hunter;

    [BurstCompile]
    private void Awake()
    {

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
        Color.RGBToHSV(new Color(0.639804f, 0.2080392f, 0.2080392f, 1f), out h, out s, out v);
        h = UnityEngine.Random.Range(0f, 1f);
        playerColor = Color.HSVToRGB(h, s * 1.15f, v * 0.83f);
        playerDarkerColor = Color.HSVToRGB(h, s * 0.95f, v * 0.95f);

        healthbar.color = playerColor;
        spriteRenderer.color = playerDarkerColor;

    }
    [BurstCompile]
    private void SceneManager_OnLoad(Scene arg0, LoadSceneMode arg1)
    {

        if (!this.IsDestroyed())
        {

            if (isLocalPlayer)
            {

                if (!NetworkManager.Singleton.IsHost) ready = false;

                if (arg0.name == "GameScene")
                {
                    score = scoreManager.startScore;
                    
                }

                spawnPosition = new Vector3(UnityEngine.Random.Range(0.0001f, 0.2001f), UnityEngine.Random.Range(0.0001f, 0.2001f), UnityEngine.Random.Range(0.0001f, 0.2001f));
                playerSynchronizer.UpdateHealth();
                CancelInvoke("RevivePlayer");

            }
            else
            {

                if (!NetworkManager.Singleton.IsHost) ready = false;

                if (arg0.name == "GameScene")
                {
                    score = scoreManager.startScore;
                }

            }

            deathChamber = GameObject.FindGameObjectWithTag("Death").transform;

            if(isLocalPlayer) spawn = GameObject.FindGameObjectWithTag("Spawn").transform;

            RevivePlayer();

            if (isLocalPlayer)
            {

                if (arg0.name == "GameScene")
                {

                    spawnBuffer = true;

                }

            }

        }

    }
    [BurstCompile]
    public void AssertSteamDataAvalible(ulong steamId)
    {

        steamDataAvalible = true;
        this.steamId = steamId;

    }

    [BurstCompile]
    private void Start()
    {

        try
        {

            DontDestroyOnLoad(gameObject);
            deathSoundInstance = RuntimeManager.CreateInstance(deathSoundReference);
            spawn = GameObject.FindGameObjectWithTag("Spawn").transform;
            deathChamber = GameObject.FindGameObjectWithTag("Death").transform;

            if (isLocalPlayer)
            {

                playerController = FindAnyObjectByType<PlayerController>();
                GetComponentInChildren<NozzleBehaviour>().SetPlayerController(playerController, this);
                playerTransform.position = GameObject.FindGameObjectWithTag("Spawn").transform.position;
                playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();

            }

            ApplyColors();

            playerSlapSound = RuntimeManager.CreateInstance(playerSlap);

        }
        catch
        {

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

    float speedParticleTimer;

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

    [SerializeField]
    EventReference playerSlap;
    EventInstance playerSlapSound;

    [BurstCompile]
    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment") ||
            collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {

            if (!hasJump) slapTimer = 0f;

            if(slapTimer == 0)
            {
                if (!isLocalPlayer || SceneManager.GetActiveScene().name.Equals("LobbyScene"))
                {

                    Vector2 toCam = Camera.main.transform.position - transform.position;
                    float soundDirection = ConvertVector2ToAngle(toCam.normalized);
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
                playerSlapSound.setVolume(MySettings.volume);
                playerSlapSound.start();
                slapTimer = 0.27f;

            }

            hasJump = true;
            slapIntensity = 0;
        }

    }

    public float ConvertVector2ToAngle(Vector2 direction)
    {
        float angleInRadians = Mathf.Atan2(direction.x, -direction.y);

        float angleInDegrees = -(angleInRadians * Mathf.Rad2Deg);

        if (angleInDegrees > 180)
        {
            angleInDegrees -= 360;
        }

        return angleInDegrees;
    }

    public void CreateTextureFromBoolArray10BY10(bool[] boolArray, byte frameIndex)
    {

        Span<bool> rotatedArray = stackalloc bool[100];

        for (int i = 0; i < 100; i++)
        {
            rotatedArray[i] = boolArray[99 - i];
        }

        Texture2D texture = new Texture2D(10, 10, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
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
        texture.Compress(true);
        if (frameIndex == 0) spriteRenderer.sprite = bodyFrames[frameIndex];

    }

    public void CreateTextureFromBoolArray4BY4(bool[] boolArray, byte frameIndex)
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

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
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

    float animationTimer;
    public float frameRate = 10;
    int animationIndex;
    int lastAnimationIndex;

    public void AnimatePlayer()
    {

        animationTimer = 1;

    }

    void ApplyPlayerAnimation()
    {

        if (animationTimer > 0) animationTimer -= Time.deltaTime * (frameRate / nozzleFrames.Length);
        if (animationTimer < 0) animationTimer = 0;
        if (animationTimer == 0)
        {
            animationIndex = 0;
        }
        else
        {
            animationIndex = Mathf.FloorToInt((1 - animationTimer) * bodyFrames.Length);
        }

        if(animationIndex != lastAnimationIndex)
        {
            nozzleBehaviour.spriteRenderer.sprite = nozzleFrames[animationIndex];
            spriteRenderer.sprite = bodyFrames[animationIndex];
            lastAnimationIndex = animationIndex;
        }

    }

    [BurstCompile]
    void ApplyColors()
    {

        Color.RGBToHSV(playerColor, out h, out s, out v);
        playerDarkerColor = Color.HSVToRGB(h, s * 0.96f, v * 0.80f);
        nozzleBehaviour.owningPlayerColor = playerColor;
        nozzleBehaviour.owningPlayerDarkerColor = playerDarkerColor;
        spriteRenderer.color = playerDarkerColor;
        healthbar.color = playerColor;
        newColor = false;

    }

    public Friend friend;

    [BurstCompile]
    void ApplySteamData()
    {

        GetImageData(steamId);
        friend = new Friend(steamId);
        playerName = friend.Name;
        steamDataApplied = true;

    }
    [BurstCompile]
    public async void GetImageData(SteamId steamId)
    {

        Steamworks.Data.Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);

        byte[] imageData = image.Value.Data;
        uint imageWidth = image.Value.Width;
        uint imageHeight = image.Value.Height;
        Vector2 imageDimentions = new Vector2(image.Value.Width, image.Value.Height);

        Texture2D spriteTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
        Rect spriteRect = new Rect(new Vector2(0, 0), imageDimentions);
        Vector2 spritePivot = imageDimentions / 2;

        spriteTexture.LoadRawTextureData(imageData);
        spriteTexture.Apply();

        pfp = Sprite.Create(spriteTexture, spriteRect, spritePivot);

    }

    [SerializeField]
    ParticleColorApplicant[] speedParticles;

    int speedParticleSwitcher;

    [BurstCompile]
    private void Update()
    {

        if(!steamDataApplied && steamDataAvalible) ApplySteamData();

        if(newColor) ApplyColors();

        oneSecondTimer += Time.deltaTime * 10;
        dataUpdateHighSpeedTimer += Time.deltaTime * 2;
        hpBarScale = Vector3.one * (healthPoints / maxHealthPoints);
        nozzlePosition = nozzleTransform.position;
        healthbar.transform.localScale = hpBarScale;
        ApplyPlayerAnimation();

/*        speedParticleTimer += Time.deltaTime * Mathf.Clamp((rb.linearVelocity.magnitude - 15) * 2f, 0, 20);
        if(speedParticleTimer > 1)
        {

            Vector2 speedParticleDirection = rb.linearVelocity.normalized;
            float speedParticleAngle = Mathf.Rad2Deg * Mathf.Atan2(speedParticleDirection.y, speedParticleDirection.x);
            int particleIndex = speedParticleSwitcher++ % speedParticles.Length;
            Instantiate(speedParticles[particleIndex], transform.position, Quaternion.Euler(0, 0, speedParticleAngle), null).Applycolor(this);

            speedParticleTimer = 0;
        }*/

        if (timeSinceHit < 1) timeSinceHit += Time.deltaTime * 3.5f;
        else if (timeSinceHit > 1) timeSinceHit = 1;

        if (oneSecondTimer >= 1f)
        {

            fps = fpsCapture;
            oneFifthFps = fps / 5f;

            fpsCapture = 0;
            oneSecondTimer = 0;

        }
        else
        {
            fpsCapture += 10;
        }

        if (isLocalPlayer)
        {

            if (climax > 0) climax -= Time.deltaTime * 0.3f;
            else if (climax < 0) climax = 0;

            if (rb.position.y < -60)
            {
                RespawnPlayer();
            }

        }
        else
        {

            ApplyRecievedData();
            CalculateNozzleMovementFromData();

        }

        if (rb.linearDamping > 0.1f) rb.linearDamping -= Time.deltaTime * 80;
        if (rb.angularDamping > 0.1f) rb.angularDamping -= Time.deltaTime * 80;
        if (rb.linearDamping < 0.1f) rb.linearDamping = 0.1f;
        if (rb.angularDamping < 0.1f) rb.angularDamping = 0.1f;

        slapIntensity = Mathf.Lerp(slapIntensity, rb.linearVelocity.magnitude + Mathf.Abs(rb.angularVelocity / 40f), Time.deltaTime * 10);
        slapTimer -= Time.deltaTime;
        if(slapTimer < 0) slapTimer = 0;

    }

    float slapTimer;
    bool lastDeathState = false;
    float deathTimer;
    public bool spawnBuffer = false;
    [BurstCompile]
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

        nozzleTransform.position = playerTransform.position + (Vector3) nozzlePosOffset;
        nozzleTransform.rotation = Quaternion.Euler(
                0,
                0,
                math.degrees(math.atan2(
                        (nozzleTransform.position - playerTransform.position).y,
                        (nozzleTransform.position - playerTransform.position).x)));

        nozzleTransform.localPosition = Vector2.ClampMagnitude(nozzleTransform.localPosition, 1.133f);

        controlled = playerController;

        if (isDead)
        {
            deathTimer += Time.deltaTime;
            rb.position = deathChamber.position;
            transform.position = deathChamber.position;
            if (deathTimer > 2) RevivePlayer();
        }
        else
        {

            deathTimer = 0;

        }

        if (spawnBuffer)
        {

            transform.position = spawn.position;
            rb.position = spawn.position;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            rb.rotation = spawn.eulerAngles.z;
            return;
        }

        Color playerFrozenBright = Color.Lerp(playerColor, frozenColor, 0.35f);
        Color playerFrozenDark = Color.Lerp(playerDarkerColor, frozenColor, 0.3f);

        healthbar.color = Color.Lerp(playerColor, playerFrozenBright, Mathf.Clamp01((rb.linearDamping / 70f) - 0.00142f));
        spriteRenderer.color = Color.Lerp(playerDarkerColor, playerFrozenDark, Mathf.Clamp01((rb.linearDamping / 70f) - 0.00142f));
        nozzleBehaviour.spriteRenderer.color = Color.Lerp(playerDarkerColor, playerFrozenDark, Mathf.Clamp01((rb.linearDamping / 70f) - 0.00142f));

        if (isDead != lastDeathState)
        {

            lastDeathState = isDead;

            if (isDead)
            {

                spriteRenderer.enabled = false;
                healthbar.enabled = false;
                col.enabled = false;
                nozzleBehaviour.spriteRenderer.enabled = false;
                rb.simulated = false;

            }

        }

        if (controlled)
        {

            SetMovementParameters(newMods);

        }

    }

    Color frozenColor = Color.white;

    public bool newMods;

    [BurstCompile]
    public void KillPlayer()
    {

        killStreak = 0;
        hunter.Die((byte)id);

        if (isLocalPlayer)
        {
            SetStats();
            playerController.CancellAllInputs();
        }
        else SetStats();

        void SetStats()
        {

            if (isLocalPlayer) mapSynchronizer.SpawnDogTag((byte) id, rb.position, rb.rotation, rb.linearVelocity / 2);
            if (isLocalPlayer) rb.position = deathChamber.position;
            if (isLocalPlayer) playerTransform.position = new Vector3(deathChamber.position.x, deathChamber.position.y, transform.position.z);
            healthPoints = maxHealthPoints;
            climax = 1;
            isDead = true;

        }

    }
    [BurstCompile]
    public void RevivePlayer()
    {

        spawnBuffer = false;

        hunter.Spawn((byte)id);

        if (!spawn)
        {

            isSpawning = false;
            spriteRenderer.enabled = true;
            healthbar.enabled = true;
            nozzleBehaviour.spriteRenderer.enabled = true;
            col.enabled = true;
            rb.simulated = true;
            isDead = false;
            scoreDeducted = false;
            transform.position = deathChamber.position;
            rb.position = deathChamber.position;
            if (playerSpawnEffectBehaviour)
            {
                Destroy(playerSpawnEffectBehaviour.gameObject);
                playerSpawnEffectBehaviour = null;
            }

        }
        else
        {

            CancelInvoke("RevivePlayer");

            isSpawning = false;
            spriteRenderer.enabled = true;
            healthbar.enabled = true;
            nozzleBehaviour.spriteRenderer.enabled = true;
            col.enabled = true;
            rb.simulated = true;
            isDead = false;
            scoreDeducted = false;

            if (playerSpawnEffectBehaviour)
            {
                Destroy(playerSpawnEffectBehaviour.gameObject);
                playerSpawnEffectBehaviour = null;
            }

            if (isLocalPlayer)
            {
                rb.position = spawn.transform.position;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = 0;
                rb.rotation = 0;
                playerTransform.position = spawn.transform.position;
                rb.simulated = true;
                healthPoints = maxHealthPoints;
                playerController.CancellAllInputs();
                playerSynchronizer.UpdateHealth();

            }

        }


    }
    [BurstCompile]
    public void RespawnPlayer()
    {

        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (!spawn) return;

        rb.position = spawn.transform.position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = 0;
        rb.rotation = 0;
        playerTransform.position = spawn.transform.position;

    }
    [BurstCompile]
    void UpdateDataToSend()
    {

        position = rb.position;
        rotation = rb.rotation;
        velocity = rb.linearVelocity;
        angularVelocity = rb.angularVelocity;
        localNozzlePosition = nozzleTransform.position - transform.position;
        nozzleRotation = nozzleTransform.rotation.eulerAngles.z;

    }
    [BurstCompile]
    void ApplyRecievedData()
    {

        healthbar.color = playerColor;
        spriteRenderer.color = playerDarkerColor;

        float lerp = GetFrameRateLerp();

    }
    [BurstCompile]
    public float GetFrameRateLerp()
    {


        return math.smoothstep(0, 1, timeSinceHit);

    }
    [BurstCompile]
    void CalculateNozzleMovementFromInput()
    {

        bool shouldSync = false;


        newNozzlePositionTime = math.clamp(newNozzlePositionTime + (Time.deltaTime * nozzlePositionSpeed), 0, 1);

        if (newNozzleLerp < 1) newNozzleLerp += Time.deltaTime * nozzlePositionSpeed;
        if (newNozzleLerp > 1) newNozzleLerp = 1;

        if (nozzleInputDirection != playerController.aimingDirection && playerController.aimingDirection.magnitude > 0)
        {

            nozzleInputDirection = playerController.aimingDirection;
            float distance = (rb.position - (Vector2)nozzleBehaviour.transform.position).magnitude;
            Vector2 direction = (Vector2)nozzleBehaviour.transform.position - rb.position;

            toPos = Vector2.ClampMagnitude(playerController.aimingDirection, 1.128f);
            fromPos = direction * distance;

            newNozzleLerp = 0;

            shouldSync = true;

        }

        nozzlePosOffset = Vector2.Lerp(
                fromPos,
                toPos, math.smoothstep(0, 1, newNozzleLerp));

        if (shouldSync) playerSynchronizer.UpdateNozzle();

    }
    [BurstCompile]
    void CalculateNozzleMovementFromData()
    {

        if (newNozzleLerp < 1) newNozzleLerp += Time.deltaTime * nozzlePositionSpeed;
        if (newNozzleLerp > 1) newNozzleLerp = 1;

        nozzlePosOffset = Vector2.Lerp(
            fromPos,
            toPos,
            math.smoothstep(0, 1, newNozzleLerp));
    }
    [BurstCompile]
    public void ApplyRecoil()
    {

        fromPos = Vector2.zero;
        newNozzleLerp = 0;
        nozzlePosOffset = Vector2.Lerp(
            fromPos,
            toPos,
            math.smoothstep(0, 1, newNozzleLerp));
        playerSynchronizer.UpdateNozzle();

    }
    [BurstCompile]
    void CalculateNozzleRotation()
    {
        nozzleTransform.rotation = Quaternion.Euler(
        0,
        0,
        math.degrees(math.atan2(
                (nozzleTransform.position - playerTransform.position).y,
                (nozzleTransform.position - playerTransform.position).x)));
    }
    [BurstCompile]
    public void AnimateNozzle(Vector3 from, Vector3 to)
    {

        nozzleReferencePosition = from - to;
        newNozzlePositionTime = 0;
        nozzleInputDirection = playerController.GetDirection();

    }
    [BurstCompile]
    private void FixedUpdate()
    {

        if (isLocalPlayer)
        {

            CalculateNozzleMovementFromInput();

            UpdateDataToSend();

        }

        flipFlop = !flipFlop;
        if (flipFlop) return;

        if (controlled)
        {

            ApplyTargetMovement();

            ReAdjustMovementValues();

        }

    }

    float acceleration;
    float maxSpeed;

    Vector2 velParam;
    float xLimiter;
    float yLimiter;
    Vector2 forceLimiter;
    Vector2 jumpVelocity;
    Vector2 jumpDirection;
    float jumpLimiter;
    void SetMovementParameters(bool newMod)
    {

        if (newMod)
        {

            acceleration = 130f * Mods.at[8];
            maxSpeed = 23.5f * Mods.at[1];
            newMods = false;

        }

        movementDirection = Vector2.Lerp(movementDirection, playerController.finalDirection, math.clamp(Time.deltaTime * 100, 0, 1));

        (velParam.x, velParam.y) = (math.clamp(rb.linearVelocityX, -maxSpeed, maxSpeed), math.clamp(rb.linearVelocityY, -maxSpeed, maxSpeed));
        (xLimiter, yLimiter) = (math.clamp(math.abs(movementDirection.x - (velParam.x / maxSpeed)), 0, 1), math.clamp(math.abs(movementDirection.y - (velParam.y / maxSpeed)), 0, 1));
        forceLimiter = new Vector2(xLimiter, yLimiter);

        jumpLimiter = 17.5f - math.clamp(rb.linearVelocityY / 2, -5, 10);
        jumpDirection = (Vector2.up + (playerController.finalDirection * 0.2f)).normalized;
        jumpVelocity = (jumpDirection * jumpLimiter) * Mods.at[2];

        if (playerController.inputJump)
        {

            rb.linearVelocity = rb.linearVelocity + jumpVelocity;
            playerController.inputJump = false;

        }

    }

    [BurstCompile]
    void ApplyTargetMovement()
    {

        rb.AddForce(movementDirection * acceleration * forceLimiter, ForceMode2D.Force);

        if (math.abs(rb.angularVelocity / 360) < 1f)
            rb.AddTorque(-movementDirection.x / 0.85f, ForceMode2D.Force);

    }

    [BurstCompile]
    void ReAdjustMovementValues()
    {

        (float posX, float posY) = (rb.position.x, rb.position.y);
        rb.position = new Vector2(math.clamp(posX, -64, 64), math.clamp(posY, -64, 64));

        if (rb.rotation > 360) rb.rotation -= 360;
        if (rb.rotation < 0) rb.rotation += 360;

        rb.angularVelocity = math.clamp(rb.angularVelocity, -1000, 1000);

        (float nPosX, float nPosY) = (nozzleTransform.localPosition.x, nozzleTransform.localPosition.y);
        nozzleTransform.localPosition = new Vector2(math.clamp(nPosX, -1, 1), math.clamp(nPosY, -1, 1));

    }

}
