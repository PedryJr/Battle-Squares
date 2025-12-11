using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public sealed class ClientBehaviour : MonoBehaviour
{

    float timeToEnd = 0.25f;
    float timer = 0;

    bool beginSessionDestruction = false;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();
    }

    private void FixedUpdate()
    {
        if (beginSessionDestruction) timer += Time.deltaTime / timeToEnd;
        if (beginSessionDestruction && timer > 1) EndOnlineSession();
    }

    public void DisconnectClientEvent()
    {
        if (beginSessionDestruction) return;
        beginSessionDestruction = true;
        KickAllRemotePlayers();
    }

    void KickAllRemotePlayers()
    {

        if (NetworkManager.Singleton.IsHost)
        {
            SteamNetwork.currentLobby?.SetJoinable(false);
            SteamNetwork.currentLobby?.SetData("Avalible", "false");

            foreach (PlayerData item in playerSynchronizer.playerIdentities)
            {
                if (item.square.GetID() == playerSynchronizer.localSquare.GetID()) continue;
                playerSynchronizer.KickPlayerClientRpc(item.square.GetID());
            }
        }
    }

    void EndOnlineSession()
    {
        beginSessionDestruction = false;
        timer = 0;
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
    }

    public void ReturnPlayersToLobby()
    {

        SceneManager.LoadSceneAsync("LobbyScene");

    }

}
