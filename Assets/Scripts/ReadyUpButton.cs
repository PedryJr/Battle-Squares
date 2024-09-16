using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static PlayerSynchronizer;

public class ReadyUpButton : MonoBehaviour
{

    [SerializeField]
    TMP_Text textField;

    int players;
    int playersReady;
    bool ready;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {

        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Start()
    {

        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {

            GetComponent<Image>().enabled = true;
            GetComponent<ButtonHoverAnimation>().enabled = true;
            GetComponentInChildren<TextMeshProUGUI>().enabled = true;

        }
        else
        {

            playerSynchronizer.localSquare.ready = true;
            Destroy(gameObject);

        }

    }

    private void Update()
    {
        
        players = playerSynchronizer.playerIdentities.Count;

        int i = 0;
        foreach(PlayerData player in playerSynchronizer.playerIdentities)
        {

            if(player.square.ready) i++;

        }

        playersReady = i;

        if (ready)
        {
            textField.text = $"{playersReady} / {players}";
        }
        else
        {
            textField.text = $"Ready";
        }

    }

    public void READY()
    {

        ready = !ready;
        playerSynchronizer.UpdatePlayerReady(ready);

    }

}
