using Netcode.Transports.Facepunch;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Image = UnityEngine.UI.Image;

public sealed class HostBehaviour : MonoBehaviour
{


    [SerializeField]
    bool hostOnly;

    [SerializeField]
    LobbyBehaviour selectedLobby;

    [SerializeField]
    LobbyBehaviour defaultLobby;

    private void Awake()
    {

        if (hostOnly && NetworkManager.Singleton.IsHost)
        {

            GetComponent<Image>().enabled = true;
            GetComponent<ButtonHoverAnimation>().enabled = true;
            GetComponentInChildren<TextMeshProUGUI>().enabled = true;

        }

    }

    public async void InitializeServerEvent()
    {
        selectedLobby.UpdateAvalible();

        if (!selectedLobby.activated) return;

        NetworkManager.Singleton.Shutdown(true);
        GameObject.FindGameObjectWithTag("Net").GetComponent<FacepunchTransport>().Shutdown();

        FindAnyObjectByType<PlayerSynchronizer>().ForceReset();

        if (selectedLobby.lobbyId.Value == SteamNetwork.currentLobby.Value.Id)
        {

            await SceneManager.LoadSceneAsync("LobbyScene", LoadSceneMode.Single);
            SteamNetwork.currentLobby?.SetData("Avalible", "false");
            SteamNetwork.currentLobby?.SetPrivate();
            SteamNetwork.currentLobby?.SetInvisible();
            SteamNetwork.currentLobby?.SetJoinable(false);
            NetworkManager.Singleton.StartHost();
            GameObject.FindGameObjectWithTag("Net").GetComponent<FacepunchTransport>().StartServer();

        }
        else
        {
/*
            if (selectedLobby.lobby.GetData("Avalible").Equals("false")) return;*/

            RoomEnter status = await selectedLobby.lobby.Join();

            if (status != RoomEnter.Success)
            {
                selectedLobby?.lobby.Leave();
                ApplyDefaultLobby();
                return;
            }

            SteamNetwork.currentLobby?.Leave();

            GameObject.FindGameObjectWithTag("Net").GetComponent<FacepunchTransport>().targetSteamId = selectedLobby.lobby.Owner.Id;

            NetworkManager.Singleton.StartClient();
            GameObject.FindGameObjectWithTag("Net").GetComponent<FacepunchTransport>().StartClient();
            

            SteamNetwork.currentLobby = selectedLobby.lobby;

        }

    }

    public void ApplyDefaultLobby()
    {
        selectedLobby.lobbyId = defaultLobby.lobbyId;
        selectedLobby.ownerId = defaultLobby.ownerId;

        selectedLobby.lobbyCapacity = defaultLobby.lobbyCapacity;
        selectedLobby.lobbyPopulation = defaultLobby.lobbyPopulation;

        selectedLobby.lobbyName.text = defaultLobby.lobbyName.text;

        selectedLobby.lobbyIcon.sprite = defaultLobby.lobbyIcon.sprite;

        selectedLobby.lobby = defaultLobby.lobby;

        selectedLobby.activated = defaultLobby.activated;
    }


}
