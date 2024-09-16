using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public class StartButtonBehaviourt : MonoBehaviour
{

    [SerializeField]
    TMP_Text textField;

    PlayerSynchronizer playerSynchronizer;


    bool everyOneReady;
    int players;
    int readyPlayers;

    private void Awake()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
            GetComponent<ButtonHoverAnimation>().enabled = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {

        players = playerSynchronizer.playerIdentities.Count;
        int i = 0;
        foreach (PlayerData player in playerSynchronizer.playerIdentities)
        {

            if (player.square.ready) i++;

        }
        readyPlayers = i;

        if (players == readyPlayers)
        {
            everyOneReady = true;
        }
        else
        {
            everyOneReady = false;
        }

        if (everyOneReady)
        {
            textField.text = "Start";
        }
        else
        {
            textField.text = $"{readyPlayers} / {players}";
        }

    }

    public void StartGameEvent()
    {
        if(everyOneReady)
        SceneManager.LoadSceneAsync("GameScene");

    }

}
