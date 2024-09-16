using Steamworks;
using Steamworks.Data;
using System;
using Unity.Netcode;
using UnityEngine;

public sealed class SteamNetwork : MonoBehaviour, IConnectionManager
{

    LocalSteamData localSteamData;

    public static string playerName;
    public static SteamId playerSteamID;

    public static Lobby? currentLobby;
    public ulong? lastLobbyId;

    private void Awake()
    {
        localSteamData = GetComponent<LocalSteamData>();
        SetupSteamClient();
        CreateNewLobby();
        SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberDisconnected;
    }

    private void Update()
    {

        SteamClient.RunCallbacks();

        if(lastLobbyId != currentLobby?.Id.Value)
        {

            lastLobbyId = currentLobby?.Id.Value;

        }

    }

    private void SteamMatchmaking_OnLobbyMemberDisconnected(Lobby arg1, Friend arg2)
    {

        if( ulong.Parse(currentLobby.Value.GetData("OwnerId")) == arg2.Id)
        {

            SteamNetwork.currentLobby?.Leave();

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

        localSteamData.Init(playerSteamID);

        try
        {
            SteamClient.Init(3180450, false);
        }
        catch { }

    }

    public static async void CreateNewLobby()
    {

        currentLobby?.Leave();
        currentLobby?.Refresh();
        currentLobby = null;

        currentLobby = await SteamMatchmaking.CreateLobbyAsync(4);

        currentLobby?.SetPublic();
        currentLobby?.SetJoinable(true);
        currentLobby?.SetData("Avalible", "false");
        currentLobby?.SetData("Name", SteamClient.Name);
        currentLobby?.SetData("OwnerId", SteamClient.SteamId.Value.ToString());
        currentLobby?.SetData("Variant", "BattleSquares");

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
