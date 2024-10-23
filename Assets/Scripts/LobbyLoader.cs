using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyLoader : MonoBehaviour
{

    [SerializeField]
    LobbyBehaviour lobbyTemplate;

    [SerializeField]
    public GameObject lobbyPreview;

    [SerializeField]
    VerticalLayoutGroup layoutGroup;

    List<LobbyBehaviour> Lobbies;
    public List<LobbyBehaviour> failedLobbies;
    float lobbyUpdateTime = 0;

    async void Awake()
    {

        Lobbies = new List<LobbyBehaviour>();
        failedLobbies = new List<LobbyBehaviour>();
        await LoadLobbies();
        LoadFirstLobby();

    }

    // Update is called once per frame
    void Update()
    {

        UpdateLobbies();

    }

    public async void UpdateLobbies()
    {

        lobbyUpdateTime += Time.deltaTime;

        if (lobbyUpdateTime > 2)
        {

            lobbyUpdateTime = 0;
            await LoadLobbies();

        }

    }

    async Task LoadLobbies()
    {

        List<LobbyBehaviour> lobbiesToRemove = new List<LobbyBehaviour>();

        LobbyQuery lobbyQuery1 = SteamMatchmaking.LobbyList;
        lobbyQuery1.FilterDistanceWorldwide();
        lobbyQuery1.WithKeyValue("Variant", "BattleSquares");

        Lobby[] fetchedGreenLobbies = await lobbyQuery1.RequestAsync();

        if (this != null)
        {
            if (fetchedGreenLobbies != null)
            {
                foreach (Lobby lobby in fetchedGreenLobbies)
                {
                    bool exist = false;
                    foreach (LobbyBehaviour lb in Lobbies)
                    {
                        if (lb.lobby.Id == lobby.Id) exist = true;
                    }
                    if (exist) continue;
                    Lobbies.Add(Instantiate(lobbyTemplate, transform).Initialize(lobby, this));
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            bool exist = false;
            foreach (Lobby lb in fetchedGreenLobbies)
            {
                if (lobby.lobby.Id == lb.Id) exist = true;
            }
            if (exist) continue;
            if (!lobby.IsDestroyed())
            {
                lobbiesToRemove.Add(lobby);
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                lobby.transform.SetParent(transform.parent, true);
            }
        }

        List<LobbyBehaviour> sortedLobbies = new List<LobbyBehaviour>();

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                if (lobby.ownerId == SteamClient.SteamId.Value)
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                if (lobby.lobby.GetData("Avalible").Equals("true"))
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                if (lobby.lobby.GetData("Avalible").Equals("false"))
                {
                    if (lobby.lobby.GetData("OwnerId").Equals(SteamClient.SteamId.Value.ToString()))
                    {
                        sortedLobbies.Add(lobby);
                    }
                    else
                    {
                        lobbiesToRemove.Add(lobby);
                    }
                }
            }
        }

        foreach (LobbyBehaviour lobby in sortedLobbies)
        {
            if (!lobby.IsDestroyed())
            {
                lobby.transform.SetParent(transform, true);
            }
        }

        foreach (LobbyBehaviour lobby in lobbiesToRemove)
        {

            Lobbies.Remove(lobby);
            try
            {
                Destroy(lobby.gameObject);
            } catch { }

        }

    }

    async Task LoadOwn()
    {

        LobbyQuery lobbyQuery1 = SteamMatchmaking.LobbyList;
        lobbyQuery1.WithKeyValue("Variant", "BattleSquares");
        lobbyQuery1.WithKeyValue("OwnerId", SteamClient.SteamId.Value.ToString());

        Lobby[] fetchedGreenLobbies = await lobbyQuery1.RequestAsync();

        if (this != null)
        {
            if (fetchedGreenLobbies != null)
            {
                foreach (Lobby lobby in fetchedGreenLobbies)
                {
                    bool exist = false;
                    foreach (LobbyBehaviour lb in Lobbies)
                    {
                        if (lb.lobby.Id == lobby.Id) exist = true;
                    }
                    if (exist) continue;
                    Lobbies.Add(Instantiate(lobbyTemplate, transform).Initialize(lobby, this));
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            bool exist = false;
            foreach (Lobby lb in fetchedGreenLobbies)
            {
                if (lobby.lobby.Id == lb.Id) exist = true;
            }
            if (exist) continue;
            if (!lobby.IsDestroyed())
            {
                Lobbies.Remove(lobby);
                Destroy(lobby.gameObject);
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                lobby.transform.SetParent(transform.parent, true);
            }
        }

        List<LobbyBehaviour> sortedLobbies = new List<LobbyBehaviour>();

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                if (lobby.ownerId == SteamClient.SteamId.Value)
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                if (lobby.lobby.GetData("Avalible").Equals("true"))
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (!lobby.IsDestroyed())
            {
                if (lobby.lobby.GetData("Avalible").Equals("false"))
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in sortedLobbies)
        {
            if (!lobby.IsDestroyed())
            {
                lobby.transform.SetParent(transform, true);
            }
        }

    }

    void LoadFirstLobby()
    {

        foreach (LobbyBehaviour lobby in Lobbies)
        {

            if (lobby.lobbyId == SteamNetwork.currentLobby?.Id.Value)
            {

                lobby.OnClicked();

            }

        }

    }

}
