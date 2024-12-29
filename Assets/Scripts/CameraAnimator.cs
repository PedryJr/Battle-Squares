using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static PlayerSynchronizer;

public sealed class CameraAnimator : MonoBehaviour
{
    public float fps = 6000;
    public float fpsCapture;
    public float oneSecondTimer = 0;
    public float initCameraTimer = 0;
    public float introTimer = 0;
    private float z = -20;

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
        battleThemeInstance = RuntimeManager.CreateInstance(battleThemeReference);
        battleThemeInstance.setVolume(initCameraTimer * MySettings.volume);
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

    private void Update()
    {
        CalculateFrames();

        if (!scoreManager.inGame) return;

        Effects();

        if (initCameraTimer < 1) initCameraTimer += Time.deltaTime * 0.53f;
        if (initCameraTimer > 1) initCameraTimer = 1;

        if (introTimer < 1) introTimer += Time.deltaTime;
        if (introTimer > 1) introTimer = 1;

        targetPosition = Vector2.zero;

        i = 0;
        if (playerSynchronizer.playerIdentities != null)
        {
            foreach (PlayerData playerData in playerSynchronizer.playerIdentities)
            {
                xDif = Mathf.Abs(playerData.square.rb.position.x - playerSynchronizer.localSquare.rb.position.x);
                yDif = Mathf.Abs(playerData.square.rb.position.y - playerSynchronizer.localSquare.rb.position.y);
                if (xDif > 31) continue;
                if (yDif > 19) continue;
                if (playerData.square.isDead) continue;

                targetPosition += playerData.square.rb.position;
                i++;
            }
        }

        if (playerSynchronizer.localSquare.isDead)
        {
            targetPosition = spawn.position;
            i = 1;
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
            toOrthoSize = 12.5f + Mathf.Clamp((i - 1) * 2f, 0, 2.8f);
            multiplier1 = 0.1f;
        }

        cameraLerp = cameraAnimation.Evaluate(transitionTimer);

        multiplier1 = Mathf.Lerp(multiplier1, Mathf.SmoothStep(offset, 1f, Mathf.Clamp01(Mathf.Clamp(playerSynchronizer.localSquare.rb.linearVelocity.magnitude / 55f, 0, 1f)) + offset), Time.deltaTime * 1.75f);

        if (playerSynchronizer.localSquare.rb.linearVelocityY < 0) multiplier2 = Mathf.Lerp(multiplier2, -Mathf.Abs(playerSynchronizer.localSquare.rb.linearVelocityY / 10.5f), Time.deltaTime * 2);
        else multiplier2 = Mathf.Lerp(multiplier2, 0, Time.deltaTime * 2);

        if (i == 1)
        {
            targetPosition = Vector2.Lerp(targetPosition + new Vector2(0, 3.5f), targetPosition, Mathf.Abs(multiplier2));
        }

        if (i != 0) (toPos.x, toPos.y, toPos.z) = (targetPosition.x / i, targetPosition.y / i, z);

        cameraTransform.position = Vector3.Lerp(cameraTransform.position, toPos, Time.deltaTime * 6.5f * multiplier1);
        aCamera.orthographicSize = math.lerp(aCamera.orthographicSize, Mathf.Lerp(fromOrthoSize, toOrthoSize, cameraLerp), Time.deltaTime * 10);
    }

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

        if (playerSynchronizer.localSquare) processVolume.weight = playerSynchronizer.localSquare.climax;

        soundUpdateTimer += Time.deltaTime * 5;
        if (soundUpdateTimer > 1f) SoundUpdates();
    }

    private void SoundUpdates()
    {
        battleThemeInstance.setVolume(initCameraTimer * MySettings.volume);

        soundUpdateTimer = 0;
        if (!playerSynchronizer.localSquare) return;

        if (playerSynchronizer.localSquare.transform.position.magnitude < 50) battleThemeInstance.setParameterByName("CameraPositionX", playerSynchronizer.localSquare.transform.position.x);
        else battleThemeInstance.setParameterByName("CameraPositionX", 0);
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