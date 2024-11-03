using ProximityChat;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public class VoiceHandle : NetworkBehaviour
{

    [SerializeField]
    VoiceNetworker voiceNetworker;

    PlayerSynchronizer playerSynchronizer = null;
    PlayerBehaviour attatchedPlayer = null;

    Transform cameraTransform;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Start()
    {
        cameraTransform = Camera.main.transform;
        voiceNetworker.StartRecording();

        DontDestroyOnLoad(this);

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;

    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

        cameraTransform = Camera.main.transform;

        if (arg0.name.Equals("MenuScene")) Destroy(gameObject);

    }

    private void Update()
    {

        if (!attatchedPlayer)
        {

            foreach (PlayerData player in playerSynchronizer.playerIdentities)
            {
                if (player.id == OwnerClientId) attatchedPlayer = player.square;
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

    }

}
