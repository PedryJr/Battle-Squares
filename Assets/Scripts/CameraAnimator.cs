using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
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

    public float soundUpdateTimer;

    bool resize = true;

    [SerializeField]
    Volume processVolume;
    public float aberration;

    Vector2 targetPosition;
    Vector3 startPosition;

    float shakeTimer;

    [SerializeField]
    EventReference battleThemeReference;
    EventInstance battleThemeInstance;

    PlayerSynchronizer playerSynchronizer;
    Camera aCamera;

    ScoreManager scoreManager;

    List<Vector3> shakes;

    Transform spawn;

    int lastI;
    float transitionTimer;

    void Start()
    {
        shakes = new List<Vector3>();
        playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();
        startPosition = transform.position;
        targetPosition = new Vector2();
        aCamera = GetComponent<Camera>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        spawn = GameObject.FindGameObjectWithTag("Spawn").transform;

        battleThemeInstance = RuntimeManager.CreateInstance(battleThemeReference);
        battleThemeInstance.setVolume(initCameraTimer * MySettings.volume);
        battleThemeInstance.start();

        initCameraTimer = 0;
        oneSecondTimer = 0;
        soundUpdateTimer = 0;
        resize = true;

    }

    private void OnDisable()
    {

        battleThemeInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

    }

    void Update()
    {

        CalculateFrames();

        if (!scoreManager.inGame) return;

        Effects();

        if (initCameraTimer < 1) initCameraTimer += Time.deltaTime * 0.53f;
        else if (initCameraTimer > 1) initCameraTimer = 1;

        if (introTimer < 1) introTimer += Time.deltaTime;
        else if (introTimer > 1) introTimer = 1;

        targetPosition = Vector2.zero;

        int i = 0;
        if(playerSynchronizer.playerIdentities != null)
        {

            foreach (PlayerData playerData in playerSynchronizer.playerIdentities)
            {
                float xDif = Mathf.Abs(playerData.square.rb.position.x - playerSynchronizer.localSquare.rb.position.x);
                float yDif = Mathf.Abs(playerData.square.rb.position.y - playerSynchronizer.localSquare.rb.position.y);
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

        if (transitionTimer < 1.5f)
            transitionTimer += Time.deltaTime * 2f;
        else
            transitionTimer = 1.5f;

        if (lastI != i)
        {
            lastI = i;
            transitionTimer = 0;
        }


        Vector3 toPos = new Vector3(0, 0, -10f);
        if (i != 0) toPos = new Vector3(targetPosition.x / i, targetPosition.y / i, -10f);
        transform.position = Vector3.Lerp(transform.position, toPos, Time.deltaTime * (1.5f + transitionTimer) * introTimer);

        if (resize)
        {

            battleThemeInstance.setVolume(initCameraTimer * MySettings.volume);
            aCamera.orthographicSize = Mathf.Lerp(26, 12, Mathf.SmoothStep(0, 1, initCameraTimer));

        }

        if (initCameraTimer == 1) resize = false;


    }

    void Effects()
    {

        shakeTimer += Time.deltaTime;

        if(shakeTimer > 0.011f)
        {
            shakeTimer = 0;
            if (shakes.Count > 0)
            {
                transform.position += shakes[0] * (shakes.Count / 4f);
                shakes.RemoveAt(0);
            }
        }

        if(playerSynchronizer.localSquare) processVolume.weight = playerSynchronizer.localSquare.climax;

        soundUpdateTimer += Time.deltaTime * 5;
        if (soundUpdateTimer > 1f) SoundUpdates();

    }

    void SoundUpdates()
    {

        soundUpdateTimer = 0;
        if (!playerSynchronizer.localSquare) return;

        if (playerSynchronizer.localSquare.transform.position.magnitude < 50) battleThemeInstance.setParameterByName("CameraPositionX", playerSynchronizer.localSquare.transform.position.x);
        else battleThemeInstance.setParameterByName("CameraPositionX", 0);
        battleThemeInstance.setParameterByName("Climax", playerSynchronizer.localSquare.climax);
        battleThemeInstance.setParameterByName("Intensity", Mathf.Clamp01(playerSynchronizer.localSquare.nozzleBehaviour.intensity));

    }

    void CalculateFrames()
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

    public void Shake() 
    {

        for (int i = 0; i < 10; i++)
        {
            if (shakes.Count >= 25) return;
            shakes.Add(new Vector3(Random.Range(-0.014f, 0.014f), Random.Range(-0.014f, 0.014f), 0));
        }

    }

}
