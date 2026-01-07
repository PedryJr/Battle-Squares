using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static PlayerSynchronizer;

public sealed class ReadyUpButton : MonoBehaviour
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

        if (!NetworkManager.Singleton.IsHost)
        {
            GetComponent<Image>().enabled = true;
            GetComponent<ButtonHoverAnimation>().enabled = true;
            GetComponentInChildren<TextMeshProUGUI>().enabled = true;
            playerSynchronizer.UpdatePlayerReady(false);

        }
        else
        {
            playerSynchronizer.UpdatePlayerReady(true);
            Destroy(gameObject);

        }

    }

    private void Update()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        players = playerSynchronizer.playerIdentities.Count;

        int i = 0;
        foreach(PlayerData player in playerSynchronizer.playerIdentities)
        {

            if(player.square.ready) i++;

        }

        playersReady = i;
        ready = playerSynchronizer.localSquare.ready;
        if (ready)
        {
            textField.text = $"{playersReady} / {players}";
        }
        else
        {
            textField.text = $"Ready";
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void READY(bool forceful)
    {
        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;
        if (playerSynchronizer.localSquare.ready != forceful) playerSynchronizer.UpdatePlayerReady(forceful);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void READY() => playerSynchronizer.UpdatePlayerReady(!ready);

}
