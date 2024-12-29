using System.Collections.Generic;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class LoadPlayerImagesBehaviour : MonoBehaviour
{

    [SerializeField]
    public PlayerSettingsBehaviour playerSettingsBehaviour;

    [SerializeField]
    LobbyPlayerDisplayBehaviour playerDisplayBehaviour;

    PlayerSynchronizer playerSynchronizer;

    List<LobbyPlayerDisplayBehaviour> displays;

    int lastPlayerCount;

    private void Start()
    {
        displays = new List<LobbyPlayerDisplayBehaviour>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    private void Update()
    {
        
        if(playerSynchronizer.playerIdentities != null)
        {
            UberUpdate();
        }

    }

    void UberUpdate()
    {

        int playerCount = playerSynchronizer.playerIdentities.Count;

        if(lastPlayerCount != playerCount)
        {

            lastPlayerCount = playerCount;

            List<LobbyPlayerDisplayBehaviour> displaysToDelete = new List<LobbyPlayerDisplayBehaviour>();

            foreach (LobbyPlayerDisplayBehaviour display in displays)
            {

                bool exist = false;

                foreach (PlayerData player in playerSynchronizer.playerIdentities)
                {

                    if (display.assignedPlayer) if (display.assignedPlayer == player.square) exist = true;

                }

                if (!exist) displaysToDelete.Add(display);
            }

            foreach (LobbyPlayerDisplayBehaviour display in displaysToDelete)
            {
                Destroy(display.gameObject);
                displays.Remove(display);
            }

            foreach (PlayerData player in playerSynchronizer.playerIdentities)
            {

                bool exists = false;

                foreach (LobbyPlayerDisplayBehaviour display in displays)
                {
                    if(display.assignedPlayer == player.square) exists = true;
                }

                if (exists) continue;

                LobbyPlayerDisplayBehaviour newDisplay = Instantiate(playerDisplayBehaviour, transform);
                newDisplay.Init(player.square);

                displays.Add(newDisplay);

            }

        }

    }

}
