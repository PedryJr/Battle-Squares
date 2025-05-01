using Steamworks;
using Steamworks.Data;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SteamNetwork : MonoBehaviour, IConnectionManager
{

    ScoreManager scoreManager;

    LocalSteamData localSteamData;

    public static string playerName;
    public static SteamId playerSteamID;

    public static Lobby? currentLobby;
    public ulong? lastLobbyId;

    int playerCount = -2;

    float activePlayersTimer;
    private void Awake()
    {
        localSteamData = GetComponent<LocalSteamData>();
        SetupSteamClient();
        CreateNewLobby();
        SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberDisconnected;
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

        scoreManager = GetComponent<ScoreManager>();
    }

    private void SceneManager_sceneUnloaded(Scene arg0)
    {

        if (arg0.name == "GameScene" && NetworkManager.Singleton.IsHost)
        {

            if (LobbyStateBehaviour.access) currentLobby?.SetData("Avalible", "true");
            else currentLobby?.SetData("Avalible", "false");

        }

    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

        if (arg0.name == "GameScene" && NetworkManager.Singleton.IsHost)
        {

            currentLobby?.SetData("Avalible", "false");

        }

    }

    private void Update()
    {

        if(!scoreManager.inGame) activePlayersTimer += Time.deltaTime;

        SteamClient.RunCallbacks();

        if(lastLobbyId != currentLobby?.Id.Value)
        {

            lastLobbyId = currentLobby?.Id.Value;

        }

        if (activePlayersTimer > 5) UpdatePlayerCount();
        if (activePlayersTimer > 5) activePlayersTimer = 0;

    }

    async void UpdatePlayerCount()
    {

        playerCount = await SteamUserStats.PlayerCountAsync();

    }

    private void SteamMatchmaking_OnLobbyMemberDisconnected(Lobby arg1, Friend arg2)
    {

        if( ulong.Parse(currentLobby.Value.GetData("OwnerId")) == arg2.Id)
        {

            currentLobby?.Leave();

            CreateNewLobby();

            PlayerSynchronizer playerSynchronizer = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>();

            if (playerSynchronizer.IsHost)
            {

                playerSynchronizer.hostShutdown = true;
                playerSynchronizer.DisconnectPlayerLocally();

            }

            NetworkManager.Singleton.Shutdown(true);
            playerSynchronizer.DisconnectPlayerLocally();

            playerSynchronizer.hostShutdown = false;

        }

    }

    void SetupSteamClient()
    {

/*        SteamServerStats.SetAchievement();
        SteamServerStats.GetAchievement();
        SteamServerStats.ClearAchievement();*/

        playerName = SteamClient.Name;
        playerSteamID = SteamClient.SteamId;

        try
        {
            SteamClient.Init(3180450, false);
        }
        catch { }

    }

    public static async void CreateNewLobby()
    {

        currentLobby?.Leave();
        //currentLobby?.Refresh();
        currentLobby = null;

        currentLobby = await SteamMatchmaking.CreateLobbyAsync(4);

        currentLobby?.SetPublic();
        currentLobby?.SetJoinable(true);
        currentLobby?.SetData("Avalible", "false");
        currentLobby?.SetData("Name", SteamClient.Name);
        currentLobby?.SetData("OwnerId", SteamClient.SteamId.Value.ToString());
        currentLobby?.SetData("Variant", "BattleSquares");
        currentLobby?.SetData("Code", "INVALID");

    }

    private void OnApplicationQuit()
    {
        try
        {

            if (NetworkManager.Singleton.IsHost)
            {
                currentLobby?.SetPrivate();
                currentLobby?.SetJoinable(false);
                currentLobby?.SetInvisible();
            }

        } catch { }

        currentLobby?.Leave();
        SteamClient.Shutdown();

    }

    void OnLobbyTerminated()
    {



    }

    private void OnDisable()
    {

        SteamClient.Shutdown();

    }

    public void OnConnecting(ConnectionInfo info)
    {

        Debug.Log("Connecting...");

    }

    public void OnConnected(ConnectionInfo info)
    {
        Debug.Log("Connected!");
    }

    public void OnDisconnected(ConnectionInfo info)
    {
        Debug.Log("Disconnected!");
    }

    public void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        Debug.Log("Message Recieved!");
    }
}
