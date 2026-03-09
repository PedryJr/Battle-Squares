using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public sealed class ClientBehaviour : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        SteamMatchmaking.OnLobbyDataChanged += SteamMatchmaking_OnLobbyDataChanged;
        playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();
    }

    private void OnDestroy()
    {
        SteamMatchmaking.OnLobbyDataChanged -= SteamMatchmaking_OnLobbyDataChanged;
    }

    private void SteamMatchmaking_OnLobbyDataChanged(Lobby obj)
    {
        if (SteamNetwork.currentLobby.Value.Id != obj.Id) return;
        if (!NetworkManager.Singleton.IsHost) return;
        bool SessionNoMore = false;
        bool sucess = bool.TryParse(SteamNetwork.currentLobby?.GetData("SessionNoMore"), out SessionNoMore);
        if (sucess && SessionNoMore) EndOnlineSession();
    }

    public void DisconnectClientEvent()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            SteamNetwork.currentLobby?.SetData("SessionNoMore", "true");
            KickAllRemotePlayers();
        }
        else
        {
            
            playerSynchronizer.DisconnectPlayerLocally();
        }
    }

    void KickAllRemotePlayers()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            foreach (PlayerData item in playerSynchronizer.playerIdentities)
            {
                if (item.square.GetNetworkID() == playerSynchronizer.localSquare.GetNetworkID()) continue;
                playerSynchronizer.KickPlayerClientRpc(item.square.GetNetworkID());
            }
        }
    }

    void EndOnlineSession()
    {
        SteamNetwork.currentLobby?.Leave();

        SteamNetwork.CreateNewLobby();


        if (playerSynchronizer.IsHost)
        {

            playerSynchronizer.hostShutdown = true;
            playerSynchronizer.DisconnectPlayerLocally();

        }
        else
        {

            playerSynchronizer.DisconnectPlayerLocally();

        }

        NetworkManager.Singleton.Shutdown(true);

        playerSynchronizer.hostShutdown = false;
        LobbyStateBehaviour.pauseAccessUpdate = false;
    }

    public void ReturnPlayersToLobby()
    {

        SceneManager.LoadSceneAsync("LobbyScene");

    }

}
