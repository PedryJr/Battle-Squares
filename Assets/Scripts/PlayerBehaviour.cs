using FMOD.Studio;
using FMODUnity;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public int score;

    public FlagBehaviour flag;

    public Rigidbody2D rb;

    Transform nozzleTransform;
    Transform deathChamber;
    public NozzleBehaviour nozzleBehaviour;
    public PlayerController playerController;
    PlayerSynchronizer playerSynchronizer;
    SpriteRenderer spriteRenderer;
    ScoreManager scoreManager;

    [SerializeField]
    SpriteRenderer healthbar;

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

    public float h;
    float s, v;
    public bool isDead;

    public bool scoreDeducted = false;

    private void Awake()
    {

        scoreManager = FindAnyObjectByType<ScoreManager>();
        rb = GetComponent<Rigidbody2D>();
        nozzleBehaviour = GetComponentInChildren<NozzleBehaviour>();
        nozzleTransform = nozzleBehaviour.transform;
        SceneManager.sceneLoaded += SceneManager_OnLoad;
        hpBarScale = Vector3.one;
        DontDestroyOnLoad(this);
        spriteRenderer = GetComponent<SpriteRenderer>();
        Color.RGBToHSV(new Color(0.639804f, 0.2080392f, 0.2080392f, 1f), out h, out s, out v);
        h = Random.Range(0f, 1f);
        playerColor = Color.HSVToRGB(h, s * 1.15f, v * 0.83f);
        playerDarkerColor = Color.HSVToRGB(h, s * 0.95f, v * 0.95f);

        healthbar.color = playerColor;
        spriteRenderer.color = playerDarkerColor;


    }

    private void SceneManager_OnLoad(Scene arg0, LoadSceneMode arg1)
    {

        if (!this.IsDestroyed())
        {

            if(!NetworkManager.Singleton.IsHost) ready = false;

            if (arg0.name == "GameScene")
            {
                score = scoreManager.startScore;
            }

            SceneManager.MoveGameObjectToScene(gameObject, arg0);
            DontDestroyOnLoad(gameObject);

            if (!isLocalPlayer) return;

            if (arg0.name == "GameScene")
            {
                FindAnyObjectByType<MapInitiator>().InitPresetMap(selectedMap, scoreManager.gameMode);
            }

            spawnPosition = new Vector3(Random.Range(0.0001f, 0.2001f), Random.Range(0.0001f, 0.2001f), Random.Range(0.0001f, 0.2001f));
            spawnPosition += GameObject.FindGameObjectWithTag("Spawn").transform.position;
            deathChamber = GameObject.FindGameObjectWithTag("Death").transform;
            playerSynchronizer.UpdateHealth();
            CancelInvoke("RevivePlayer");

            gameObject.GetComponent<Rigidbody2D>().position = spawnPosition;
            healthPoints = 20;
            rb.simulated = true;
            RevivePlayer();

        }

    }

    private void Start()
    {
        deathSoundInstance = RuntimeManager.CreateInstance(deathSoundReference);
        DontDestroyOnLoad(gameObject);

        if (isLocalPlayer)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            GetComponentInChildren<NozzleBehaviour>().SetPlayerController(playerController, this);
            transform.position = GameObject.FindGameObjectWithTag("Spawn").transform.position;
            playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();
            deathChamber = GameObject.FindGameObjectWithTag("Death").transform;
        }

        ApplyColors();

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            hasJump = true;
        }

    }

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

    private void Update()
    {

        if(newColor) ApplyColors();

        oneSecondTimer += Time.deltaTime * 10;
        dataUpdateHighSpeedTimer += Time.deltaTime * 2;
        hpBarScale = Vector3.one * (healthPoints / maxHealthPoints);
        nozzlePosition = nozzleTransform.position;
        healthbar.transform.localScale = hpBarScale;

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

            if (rb.position.y < -40)
            {
                KillPlayer();
            }

            CalculateNozzleMovementFromInput();

            UpdateDataToSend();

        }
        else
        {

            ApplyRecievedData();
            CalculateNozzleRotation();

        }

    }

    private void LateUpdate()
    {
        if(!isLocalPlayer)
        {
            nozzleTransform.position = transform.position + new Vector3(localNozzlePosition.x, localNozzlePosition.y, 0);
        }

        controlled = playerController;

    }

    public void KillPlayer()
    {

        if (playerSynchronizer)
        {
            playerSynchronizer.PlayPlayerDeath(transform.position, playerColor);
            SetStats();
            playerController.CancellAllInputs();

            if (isLocalPlayer) rb.simulated = false;

        }
        else SetStats();

        Invoke("RevivePlayer", 2f);

        void SetStats()
        {

            if (isLocalPlayer) rb.position = deathChamber.position;
            if (isLocalPlayer) transform.position = new Vector3(deathChamber.position.x, deathChamber.position.y, transform.position.z);
            if (isLocalPlayer) playerController.quedInput = true;
            healthPoints = 20;
            climax = 1;
            isDead = true;

        }

    }

    void RevivePlayer()
    {

        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (!spawn) return;

        if (isLocalPlayer) rb.position = spawn.transform.position;
        if (isLocalPlayer) rb.linearVelocity = Vector3.zero;
        if (isLocalPlayer) rb.angularVelocity = 0;
        if (isLocalPlayer) rb.rotation = 0;
        if (isLocalPlayer) transform.position = spawn.transform.position;
        if (isLocalPlayer) rb.simulated = true;
        if (isLocalPlayer) playerController.quedInput = true;
        isDead = false;
        scoreDeducted = false;
        if (isLocalPlayer) healthPoints = 20;
        if (playerController) playerController.CancellAllInputs();
        playerSynchronizer.UpdateHealth();

    }

    void RespawnPlayer()
    {

        GameObject spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (!spawn) return;

        rb.position = spawn.transform.position;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = 0;
        rb.rotation = 0;
        transform.position = spawn.transform.position;
        if (playerController) playerController.CancellAllInputs();

    }

    void UpdateDataToSend()
    {

        position = rb.position;
        rotation = rb.rotation;
        velocity = rb.linearVelocity;
        angularVelocity = rb.angularVelocity;
        localNozzlePosition = nozzleTransform.position - transform.position;
        nozzleRotation = nozzleTransform.rotation.eulerAngles.z;

    }

    void ApplyRecievedData()
    {

        healthbar.color = playerColor;
        spriteRenderer.color = playerDarkerColor;

        float lerp = GetFrameRateLerp();

    }

    public float GetFrameRateLerp()
    {

        return Mathf.SmoothStep(0, 1, timeSinceHit);

    }

    void CalculateNozzleMovementFromInput()
    {

        if (playerController.aimingDirectionSimple != Vector2.zero)
        {
            nozzlePositionOffset = playerController.aimingDirectionSimple;

            nozzleTransform.position = Vector3.Lerp(
                    nozzleTransform.position,
                    rb.position + nozzlePositionOffset, Time.deltaTime * 50);

            nozzleTransform.rotation = Quaternion.Euler(
                    0,
                    0,
                    Mathf.Rad2Deg * Mathf.Atan2(
                            (nozzleTransform.position - transform.position).y,
                            (nozzleTransform.position - transform.position).x));

        }
        else
        {

            newNozzlePositionTime = Mathf.Clamp01(newNozzlePositionTime + (Time.deltaTime * 15));

            float nozzleLerp = Mathf.SmoothStep(0, 1, newNozzlePositionTime);

            if (nozzleInputDirection != playerController.finalDirection && playerController.finalDirection.magnitude > 0)
            {

                AnimateNozzle(nozzleTransform.transform.position, transform.position);

                if (playerController.finalDirection.magnitude > 1)
                {
                    nozzlePositionOffset
                    = new Vector2(playerController.finalDirection.x, playerController.finalDirection.y * 3.333f) * 0.8f;
                }
                else
                {
                    nozzlePositionOffset
                    = new Vector2(playerController.finalDirection.x, playerController.finalDirection.y * 3.333f);
                }

            }

            nozzleTransform.position = Vector3.Lerp(
                    rb.position + nozzleReferencePosition,
                    rb.position + nozzlePositionOffset, nozzleLerp);

            nozzleTransform.rotation = Quaternion.Euler(
                    0,
                    0,
                    Mathf.Rad2Deg * Mathf.Atan2(
                            (nozzleTransform.position - transform.position).y,
                            (nozzleTransform.position - transform.position).x));

        }

    }

    void CalculateNozzleRotation()
    {
        nozzleTransform.rotation = Quaternion.Euler(
        0,
        0,
        Mathf.Rad2Deg * Mathf.Atan2(
                (nozzleTransform.position - transform.position).y,
                (nozzleTransform.position - transform.position).x));
    }

    public void AnimateNozzle(Vector3 from, Vector3 to)
    {

        nozzleReferencePosition = from - to;
        newNozzlePositionTime = 0;
        nozzleInputDirection = playerController.GetDirection();

    }

    bool controlled;
    bool flipFlop;
    private void FixedUpdate()
    {
        flipFlop = !flipFlop;
        if (flipFlop) return;

        if (!controlled) return;

        SetTargetMovement();

        ReAdjustMovementValues();

    }
    Vector2 movementDirection = Vector2.zero;
    void SetTargetMovement()
    {

        movementDirection = Vector2.Lerp(movementDirection, playerController.finalDirection, Time.deltaTime * 100);

        float jumpLimiter = Mathf.Clamp(rb.linearVelocityY/2, -5, 10);

        Vector2 jumpDirection = (Vector2.up + (playerController.finalDirection * 0.1f)).normalized;

        if (playerController.inputJump)
            rb.AddForce(jumpDirection * (17.5f - jumpLimiter), ForceMode2D.Impulse);

        float maxSpeed = 23.5f;

        Vector2 velParam = new Vector2(Mathf.Clamp(rb.linearVelocityX, -maxSpeed, maxSpeed), Mathf.Clamp(rb.linearVelocityY, -maxSpeed, maxSpeed));
        float xLimiter = Mathf.Clamp01(Mathf.Abs(movementDirection.x - (velParam.x / maxSpeed)));
        float yLimiter = Mathf.Clamp01(Mathf.Abs(movementDirection.y - (velParam.y / maxSpeed)));
        Vector2 forceLimiter = new Vector2(xLimiter, yLimiter);

        float acceleration = 130f;
        rb.AddForce(movementDirection * acceleration * forceLimiter, ForceMode2D.Force);



        if (Mathf.Abs(rb.angularVelocity / 360) < 1f)
            rb.AddTorque(-movementDirection.x / 0.85f, ForceMode2D.Force);

        playerController.inputJump = false;

    }

    void ReAdjustMovementValues()
    {

        (float posX, float posY) = (rb.position.x, rb.position.y);
        rb.position = new Vector2(Mathf.Clamp(posX, -64, 64), Mathf.Clamp(posY, -64, 64));

        if (rb.rotation > 360) rb.rotation -= 360;
        if (rb.rotation < 0) rb.rotation += 360;

        rb.angularVelocity = Mathf.Clamp(rb.angularVelocity, -1000, 1000);

        (float nPosX, float nPosY) = (nozzleTransform.localPosition.x, nozzleTransform.localPosition.y);
        nozzleTransform.localPosition = new Vector2(Mathf.Clamp(nPosX, -1, 1), Mathf.Clamp(nPosY, -1, 1));

    }

}
