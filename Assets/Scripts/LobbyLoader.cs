using Steamworks;
using Steamworks.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
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
    public Dictionary<ulong, LobbyBehaviour> LobbiesV2;
    public List<ulong> failedLobbies;
    float lobbyUpdateTime = 0;

    async void Awake()
    {

        Lobbies = new List<LobbyBehaviour>();
        LobbiesV2 = new Dictionary<ulong, LobbyBehaviour>();
        failedLobbies = new List<ulong>();
        await LoadLobbiesV2();

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
            await LoadLobbiesV2();
            RefreshLobbies();

        }

    }

    void RefreshLobbies()
    {
        foreach (var item in LobbiesV2)
        {
            LobbyBehaviour lobby = item.Value;

            lobby.lobby.TryRefresh();
        }
    }

    public LobbyBehaviour GetOwnLobby()
    {
        if (SteamNetwork.currentLobby == null) return null;
        ulong key = SteamNetwork.currentLobby.Value.Id.Value;
        if (!LobbiesV2.ContainsKey(SteamNetwork.currentLobby.Value.Id.Value)) return null;
        return LobbiesV2[key];
    }

    async Task LoadLobbiesV2()
    {

        LobbyQuery lobbyQuery1 = SteamMatchmaking.LobbyList;
        lobbyQuery1.WithMaxResults(50);
        lobbyQuery1.FilterDistanceWorldwide();
        lobbyQuery1.WithKeyValue("Variant", "BattleSquares");
        Lobby[] fetchedLobbies = await lobbyQuery1.RequestAsync();



        List<Lobby> manuallyFiltered = new List<Lobby>();
        List<ulong> keysToRemove = new List<ulong>();
        if (fetchedLobbies == null) return;

        //manuallyFiltered.AddRange(fetchedLobbies);

        //RemoveOfflineLobbies(ref fetchedLobbies, ref keysToRemove);

        for (int i = LobbiesV2.Count - 1; i >= 0; i--)
        {
            LobbyBehaviour listedLobby = LobbiesV2.ElementAt(i).Value;
            bool exists = false;
            foreach (Lobby lobby in fetchedLobbies)
            {
                if(lobby.Id == listedLobby.GetLobby.Id) exists = true;
                if (exists) break;
            }
            if(!exists)
            {
                LobbiesV2.Remove(listedLobby.GetLobby.Id);
                Destroy(listedLobby.gameObject);
            }
        }

        foreach (Lobby lobby in fetchedLobbies)
        {

            if (!LobbiesV2.ContainsKey(lobby.Id))
            {
                AddOnlineLobby(lobby);
            }

        }

        /*
                if (this != null)
                {

                    if (fetchedLobbies != null)
                    {

                        //Filter lobbies
                        for (int i = 0; i < fetchedLobbies.Length; i++)
                        {

                            bool filterFlag = true;
                            Lobby aLobby = fetchedLobbies[i];

                            if(!aLobby.GetData("Avalible").Equals("true")) filterFlag = false;

                            if (aLobby.GetData("OwnerId").Equals(SteamClient.SteamId.Value.ToString())) filterFlag = true;

                            if (filterFlag) manuallyFiltered.Add(aLobby);

                        }

                        //Add new lobbies
                        for (int i = 0; i < manuallyFiltered.Count; i++)
                        {

                            Lobby aLobby = manuallyFiltered[i];

                            if (!LobbiesV2.ContainsKey(aLobby.Id))
                            {
                                LobbiesV2.Add(aLobby.Id, Instantiate(lobbyTemplate, transform).Initialize(aLobby, this));
                            }

                        }

                        //Remove lobbies that no longer exist
                        List<ulong> lobbiesToRemove = new List<ulong>();
                        for (int i = 0; i < LobbiesV2.Count; i++)
                        {

                            bool matchFound = false;

                            ulong oldId = LobbiesV2.Keys.ElementAt(i);
                            ulong matchId = 0;

                            for (int j = 0; j < manuallyFiltered.Count; j++)
                            {
                                if (matchFound) break;
                                matchId = manuallyFiltered.ElementAt(j).Id;
                                matchFound = matchId == oldId;
                            }

                            if (!matchFound)
                            {
                                lobbiesToRemove.Add(matchId);
                                break;
                            }

                        }
                        foreach (ulong id in lobbiesToRemove)
                        {
                            LobbyBehaviour lobbyBehaviour = LobbiesV2[id];
                            LobbiesV2.Remove(id);
                            Destroy(lobbyBehaviour.gameObject);
                        }

                    }

                }*/

    }

    void AddOnlineLobby(Lobby lobbyToAdd)
    {

        if(IsLobbyValid(lobbyToAdd))
        {
            CreateNewLobby(lobbyToAdd);
        }

    }

    void CreateNewLobby(Lobby source)
    {
        LobbyBehaviour newLobby = Instantiate(lobbyTemplate, transform).Initialize(source);
        //if(!failedLobbies.Contains(newLobby.lobbyId.Value)) LobbiesV2.Add(source.Id, newLobby);
        LobbiesV2.Add(source.Id, newLobby);
    }

    bool IsLobbyValid(Lobby lobbyToCheck)
    {
        bool lobbyIsValid = false;

        if (Filter(lobbyToCheck, "Avalible", "true"))
        {
            lobbyIsValid = true;
        }
        else if (Filter(lobbyToCheck, "OwnerId", SteamClient.SteamId.Value))
        {
            lobbyIsValid = SafeFilterNewOwnedLobbyOnly(lobbyToCheck);
        }
        return lobbyIsValid;
    }

    bool Filter(Lobby toFilter, string key, string value)
    {
        return toFilter.GetData(key) == value;
    }

    bool Filter(Lobby toFilter, string key, ulong value)
    {
        return ulong.Parse(toFilter.GetData(key)) == value;
    }

    bool SafeFilterNewOwnedLobbyOnly(Lobby lobbyToCheck)
    {
        if(SteamNetwork.currentLobby != null) return lobbyToCheck.Id == SteamNetwork.currentLobby?.Id;
        else return false;
    }

    void RemoveOfflineLobbies(ref Lobby[] manuallyFiltered, ref List<ulong> keysToRemove)
    {
        foreach (KeyValuePair<ulong, LobbyBehaviour> listedLobby in LobbiesV2)
        {

            bool listingOnline = false;

            foreach (Lobby fetchedLobby in manuallyFiltered)
            {


                if (listedLobby.Key == fetchedLobby.Owner.Id.Value)
                {

                    if (IsLobbyValid(fetchedLobby))
                    {
                        listingOnline = true;
                    }

                }

            }

            if (!listingOnline)
            {
                keysToRemove.Add(listedLobby.Key);
            }

        }

        foreach (ulong key in keysToRemove)
        {
            LobbyBehaviour oldListing = LobbiesV2[key];
            LobbiesV2.Remove(key);
            if(oldListing) if(oldListing.gameObject) Destroy(oldListing.gameObject);
        }
    }

/*    async Task LoadLobbies()
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
            if (lobby)
            {
                lobbiesToRemove.Add(lobby);
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
            {
                lobby.transform.SetParent(transform.parent, true);
            }
        }

        List<LobbyBehaviour> sortedLobbies = new List<LobbyBehaviour>();

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
            {
                if (lobby.ownerId == SteamClient.SteamId.Value)
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
            {
                if (lobby.lobby.GetData("Avalible").Equals("true"))
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
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
            if (lobby)
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

    }*/
/*
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
            if (lobby)
            {
                Lobbies.Remove(lobby);
                Destroy(lobby.gameObject);
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
            {
                lobby.transform.SetParent(transform.parent, true);
            }
        }

        List<LobbyBehaviour> sortedLobbies = new List<LobbyBehaviour>();

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
            {
                if (lobby.ownerId == SteamClient.SteamId.Value)
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
            {
                if (lobby.lobby.GetData("Avalible").Equals("true"))
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in Lobbies)
        {
            if (lobby)
            {
                if (lobby.lobby.GetData("Avalible").Equals("false"))
                {
                    sortedLobbies.Add(lobby);
                }
            }
        }

        foreach (LobbyBehaviour lobby in sortedLobbies)
        {
            if (lobby)
            {
                lobby.transform.SetParent(transform, true);
            }
        }

    }*/

/*    void LoadFirstLobbyV2()
    {

        foreach (LobbyBehaviour lobby in LobbiesV2.Values)
        {

            if (lobby.lobby.Id == SteamNetwork.currentLobby?.Id.Value)
            {

                lobby.OnClicked();

            }

        }

    }*/

/*    void LoadFirstLobby()
    {

        foreach (LobbyBehaviour lobby in Lobbies)
        {

            if (lobby.lobbyId == SteamNetwork.currentLobby?.Id.Value)
            {

                lobby.OnClicked();

            }

        }

    }*/

}
