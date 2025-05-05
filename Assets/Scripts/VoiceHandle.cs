using ProximityChat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public sealed class VoiceHandle : NetworkBehaviour
{

    [SerializeField]
    VoiceNetworker voiceNetworker;
    VoiceEmitter voiceEmitter;
    VoiceRecorder voiceRecorder;

    PlayerSynchronizer playerSynchronizer = null;
    PlayerBehaviour attatchedPlayer = null;

    Transform cameraTransform;

    float lastSettingsVolume = -1;
    float settingsVolume;

    float lastVolume = -1;
    float volume;

    bool lastMuted = true;
    bool muted;

    bool lastSelfMute;
    bool selfMute;

    private void Awake()
    {
        
        voiceEmitter = GetComponent<VoiceEmitter>();
        voiceRecorder = GetComponent<VoiceRecorder>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        voiceNetworker.StartRecording();

        if (IsOwner)
        {

            selfMute = MySettings.muted;

            ApplySelfMute();

        }

        DontDestroyOnLoad(this);

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

        cameraTransform = Camera.main.transform;

        if (arg0.name.Equals("MenuScene")) if(this) if(gameObject) Destroy(gameObject);
            
    }

    private void Update()
    {

        if (!attatchedPlayer)
        {

            if (playerSynchronizer)
            {

                PlayerBehaviour player = null;
                player = playerSynchronizer.GetPlayerById(OwnerClientId);
                if (player)
                {
                    attatchedPlayer = player;
                    volume = attatchedPlayer.voiceVolume;
                    muted = attatchedPlayer.voiceMute;
                    ApplyVoiceChatVolume();
                }

            }

        }
        else
        {

            if (!attatchedPlayer.isDead)
            {

                Vector3 newPos = attatchedPlayer.transform.position;
                newPos.z = cameraTransform.position.z - 5;

                transform.position = newPos;

            }

        }

        if (attatchedPlayer)
        {

            settingsVolume = MySettings.volume;
            volume = attatchedPlayer.voiceVolume;
            muted = attatchedPlayer.voiceMute;

            bool change = false;

            if (volume != lastVolume) change = true;

            if (muted != lastMuted) change = true;

            if (settingsVolume != lastSettingsVolume) change = true;

            if (change) ApplyVoiceChatVolume();

            lastSettingsVolume = settingsVolume;
            lastVolume = volume;
            lastMuted = muted;

            if (IsOwner)
            {
                selfMute = MySettings.muted;

                if(selfMute != lastSelfMute) ApplySelfMute();

                lastSelfMute = muted;

            }

        }

    }

    void ApplyVoiceChatVolume()
    {

        float muteMultiplier = muted ? 0 : 1;
        voiceEmitter.SetVolume(volume * settingsVolume * muteMultiplier);

    }

    void ApplySelfMute()
    {

        if (selfMute)
        {
            voiceNetworker.StopRecording();
            voiceRecorder.StopRecording();
        }
        else
        {
            voiceNetworker.StartRecording();
            voiceRecorder.StartRecording();
        }

    }

}
