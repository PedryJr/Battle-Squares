using Unity.Netcode;
using UnityEngine;
using static PlayerSynchronizer;

public class PassiveQuitDetection : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;
    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                foreach (PlayerData item in playerSynchronizer.playerIdentities)
                {
                    if (item.square.GetNetworkID() == playerSynchronizer.localSquare.GetNetworkID()) continue;
                    playerSynchronizer.KickPlayerClientRpc(item.square.GetNetworkID());
                }
            }
            SteamNetwork.currentLobby?.SetData("SessionNoMore", "true");
        }
    }
}
