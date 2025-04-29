using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ClientBehaviour : MonoBehaviour
{

    public void DisconnectClientEvent()
    {

        SteamNetwork.currentLobby?.Leave();

        SteamNetwork.CreateNewLobby();

        PlayerSynchronizer playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();

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
