using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static PlayerSynchronizer;

public sealed class CameraAnimator : MonoBehaviour
{

    [SerializeField] float vDist;
    [SerializeField] float hDist;

    [SerializeField] float StencilEffectRenderScale;
    [SerializeField] float ThermalEffectRenderScale;

    [SerializeField] Camera effectRenderer;

    private static Camera _effectRenderer;
    public static Camera EtencilRenderer
    {
        get
        {
            if (!_effectRenderer) _effectRenderer = FindAnyObjectByType<CameraAnimator>(FindObjectsInactive.Exclude).effectRenderer;
            return _effectRenderer;
        }
    }

    public float fps = 6000;
    public float fpsCapture;
    public float oneSecondTimer = 0;
    public float initCameraTimer = 0;
    public float introTimer = 0;
    public float z = -20;
    public float cameraYOffset = 3.5f;

    public float soundUpdateTimer;

    [SerializeField]
    private Volume processVolume;

    public float aberration;

    private Vector2 targetPosition;
    private Vector3 startPosition;

    private float shakeTimer;

    private EventInstance battleThemeInstance;

    private PlayerSynchronizer playerSynchronizer;
    private Camera aCamera;
    private Transform cameraTransform;

    [SerializeField]
    private AnimationCurve cameraAnimation;

    private ScoreManager scoreManager;

    private List<Vector3> shakes;

    private Transform spawn;

    private int lastI;
    private float transitionTimer;
    private float fromOrthoSize;
    private float toOrthoSize;

    // Bounds tracking for local players
    private Vector2 minBounds;
    private Vector2 maxBounds;

    private void Start()
    {
        cameraTransform = transform;
        shakes = new List<Vector3>();
        playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();
        startPosition = transform.position;
        targetPosition = new Vector2();
        aCamera = GetComponent<Camera>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        spawn = GameObject.FindGameObjectWithTag("Spawn").transform;

        initCameraTimer = 0;
        oneSecondTimer = 0;
        soundUpdateTimer = 0;
    }

    public void PlayTheme(EventReference battleThemeReference)
    {
        if (battleThemeInstance.isValid()) battleThemeInstance.release();
        battleThemeInstance = RuntimeManager.CreateInstance(battleThemeReference);
        battleThemeInstance.setVolume(initCameraTimer * MySettings.Volume);
        battleThemeInstance.start();
    }

    private void OnDisable()
    {
        battleThemeInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    private float xDif;
    private float yDif;
    private int i;
    private float offset;
    private float cameraLerp;
    private Vector3 toPos;

    [MethodImpl(512)]
    private void Update()
    {

        BS_Screen.ResolutionScaleStencil = StencilEffectRenderScale;
        BS_Screen.ResolutionScaleThermal = ThermalEffectRenderScale;

        CalculateFrames();

        if (!scoreManager.inGame) return;
        if (!playerSynchronizer) return;

        Effects();

        if (initCameraTimer < 1) initCameraTimer += Time.deltaTime * 0.53f;
        if (initCameraTimer > 1) initCameraTimer = 1;

        if (introTimer < 1) introTimer += Time.deltaTime;
        if (introTimer > 1) introTimer = 1;

        targetPosition = Vector2.zero;
        minBounds = new Vector2(float.MaxValue, float.MaxValue);
        maxBounds = new Vector2(float.MinValue, float.MinValue);

        i = 0;
        int localPlayerCount = 0;
        bool anyLocalPlayerAlive = false;
        List<PlayerData> localPlayers = new List<PlayerData>();

        if (playerSynchronizer.playerIdentities != null)
        {
            // First pass: collect all local players and check if any are alive
            foreach (PlayerData playerData in playerSynchronizer.playerIdentities)
            {
                byte networkId = playerData.square.GetNetworkID();

                // Check if this is a local player (same network ID as localSquare)
                if (playerSynchronizer.localSquare != null && networkId == playerSynchronizer.localSquare.GetNetworkID())
                {
                    localPlayers.Add(playerData);
                    if (!playerData.square.isDead)
                    {
                        anyLocalPlayerAlive = true;
                    }
                }
            }

            // Second pass: calculate camera target
            foreach (PlayerData playerData in playerSynchronizer.playerIdentities)
            {
                Vector2 playerPos = playerData.square.rb.position;

                // Always include all local players in camera tracking
                bool isLocalPlayer = localPlayers.Contains(playerData);

                if (isLocalPlayer)
                {
                    if (!playerData.square.isDead)
                    {
                        // Track bounds for local players
                        minBounds.x = Mathf.Min(minBounds.x, playerPos.x);
                        minBounds.y = Mathf.Min(minBounds.y, playerPos.y);
                        maxBounds.x = Mathf.Max(maxBounds.x, playerPos.x);
                        maxBounds.y = Mathf.Max(maxBounds.y, playerPos.y);

                        targetPosition += playerPos;
                        i++;
                        localPlayerCount++;
                    }
                }
                else
                {
                    // For non-local players, only include if they're close to the camera center
                    Vector2 cameraCenter = localPlayerCount > 0 ? targetPosition / localPlayerCount : (Vector2)cameraTransform.position;

                    xDif = Mathf.Abs(playerPos.x - cameraCenter.x);
                    yDif = Mathf.Abs(playerPos.y - cameraCenter.y);

                    if (xDif > 20) continue;
                    if (yDif > 20 / 1.777778f) continue;
                    if (playerData.square.isDead) continue;

                    targetPosition += playerPos;
                    i++;
                }
            }
        }

        // If all local players are dead, focus on spawn
        if (!anyLocalPlayerAlive && localPlayers.Count > 0)
        {
            targetPosition = spawn.position;
            i = 1;
            minBounds = maxBounds = spawn.position;
        }

        if (transitionTimer < 1)
            transitionTimer += Time.deltaTime * 1.5f;
        if (transitionTimer > 1) transitionTimer = 1;

        offset = 0.2f;

        if (lastI != i)
        {
            lastI = i;
            transitionTimer = 0;
            fromOrthoSize = aCamera.orthographicSize;

            // Calculate required orthographic size to fit all local players
            float requiredSize = CalculateRequiredOrthoSize(localPlayerCount);
            toOrthoSize = requiredSize;

            multiplier1 = 0.1f;
        }

        cameraLerp = cameraAnimation.Evaluate(transitionTimer);

        // Average velocity of local players for camera smoothing
        float avgVelocity = 0f;
        float avgVerticalVelocity = 0f;

        if (localPlayers.Count > 0)
        {
            foreach (PlayerData localPlayer in localPlayers)
            {
                if (!localPlayer.square.isDead)
                {
                    avgVelocity += localPlayer.square.rb.linearVelocity.magnitude;
                    avgVerticalVelocity += localPlayer.square.rb.linearVelocityY;
                }
            }
            avgVelocity /= Mathf.Max(1, localPlayerCount);
            avgVerticalVelocity /= Mathf.Max(1, localPlayerCount);
        }

        multiplier1 = Mathf.Lerp(multiplier1, Mathf.SmoothStep(offset, 1f, Mathf.Clamp01(Mathf.Clamp(avgVelocity / 55f, 0, 1f)) + offset), Time.deltaTime * 1.75f);

        if (avgVerticalVelocity < 0)
            multiplier2 = Mathf.Lerp(multiplier2, -Mathf.Abs(avgVerticalVelocity / 10.5f), Time.deltaTime * 2);
        else
            multiplier2 = Mathf.Lerp(multiplier2, 0, Time.deltaTime * 2);

        if (i == 1) targetPosition = Vector2.Lerp(targetPosition + new Vector2(0, cameraYOffset), targetPosition, Mathf.Abs(multiplier2));

        if (i != 0) (toPos.x, toPos.y, toPos.z) = (targetPosition.x / i, targetPosition.y / i, z);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, toPos, Time.deltaTime * 6.5f * multiplier1);
        aCamera.orthographicSize = math.lerp(aCamera.orthographicSize, Mathf.Lerp(fromOrthoSize, toOrthoSize, cameraLerp), Time.deltaTime * 10);

        effectRenderer.orthographicSize = aCamera.orthographicSize;

    }

    private float CalculateRequiredOrthoSize(int localPlayerCount)
    {
        // Base size
        float baseSize = 14.5f;

        if (localPlayerCount <= 1)
        {
            // Single player or none - use default behavior
            return baseSize + Mathf.Clamp((i - 1) * 2f, 0, 2.8f);
        }

        // Calculate bounds size
        float boundsWidth = maxBounds.x - minBounds.x;
        float boundsHeight = maxBounds.y - minBounds.y;

        // Add padding (50% extra space around players)
        float paddingMultiplier = 1.5f;
        boundsWidth *= paddingMultiplier;
        boundsHeight *= paddingMultiplier;

        // Calculate required orthographic size to fit the bounds
        // Ortho size is half-height of the view
        float requiredHeightSize = boundsHeight / 2f;

        // Account for aspect ratio (width constraint)
        float aspectRatio = 1.777778f; // 16:9
        float requiredWidthSize = boundsWidth / (2f * aspectRatio);

        // Take the larger of the two
        float requiredSize = Mathf.Max(requiredHeightSize, requiredWidthSize);

        // Clamp to reasonable values
        requiredSize = Mathf.Max(requiredSize, baseSize);
        requiredSize = Mathf.Min(requiredSize, baseSize + 8f); // Max zoom out

        // Also consider nearby players for additional zoom
        float nearbyBonus = Mathf.Clamp((i - localPlayerCount) * 1.5f, 0, 2.8f);

        return requiredSize + nearbyBonus;
    }


    public int lastXSize;
    public int lastYSize;

    private float multiplier1;
    private float multiplier2;

    private void Effects()
    {
        shakeTimer += Time.deltaTime;

        if (shakeTimer > 0.035f)
        {
            shakeTimer = 0;
            if (shakes.Count > 0)
            {
                cameraTransform.position += shakes[0] * (shakes.Count / 2f);
                shakes.RemoveAt(0);
            }
        }

        // Average climax across all local players for effects
        if (playerSynchronizer.playerIdentities != null)
        {
            float totalClimax = 0f;
            int localCount = 0;

            foreach (PlayerData playerData in playerSynchronizer.playerIdentities)
            {
                if (playerSynchronizer.localSquare != null &&
                    playerData.square.GetNetworkID() == playerSynchronizer.localSquare.GetNetworkID())
                {
                    totalClimax += playerData.square.climax;
                    localCount++;
                }
            }

            if (localCount > 0)
            {
                processVolume.weight = totalClimax / localCount;
            }
        }

        soundUpdateTimer += Time.deltaTime * 5;
        if (soundUpdateTimer > 1f) SoundUpdates();
    }

    private void SoundUpdates()
    {
        battleThemeInstance.setVolume(initCameraTimer * MySettings.Volume);

        soundUpdateTimer = 0;
        if (!playerSynchronizer.localSquare) return;

        // Use the primary local player (localSquare) for sound parameters
        if (playerSynchronizer.localSquare.transform.position.magnitude < 50)
            battleThemeInstance.setParameterByName("CameraPositionX", playerSynchronizer.localSquare.transform.position.x);
        else
            battleThemeInstance.setParameterByName("CameraPositionX", 0);

        battleThemeInstance.setParameterByName("Climax", playerSynchronizer.localSquare.climax);
        battleThemeInstance.setParameterByName("Intensity", Mathf.Clamp01(playerSynchronizer.localSquare.nozzleBehaviour.intensity));
    }

    private void CalculateFrames()
    {
        oneSecondTimer += Time.deltaTime * 10;

        if (oneSecondTimer >= 1f)
        {
            fps = fpsCapture;

            fpsCapture = 0;
            oneSecondTimer = 0;
        }
        else
        {
            fpsCapture += 10;
        }
    }

    private Vector3 randomShake = new Vector3();

    public void Shake()
    {
        for (int i = 0; i < 4; i++)
        {
            if (shakes.Count >= 8) return;
            (randomShake.x, randomShake.y, randomShake.z) = (UnityEngine.Random.Range(-0.05f, 0.05f), UnityEngine.Random.Range(-0.05f, 0.05f), 0);
            shakes.Add(randomShake);
        }
    }
}