using Steamworks;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public sealed class Hunter : NetworkBehaviour
{

    public int lobbyKills;
    public float gameKills;

    public string lobbySceneName = "LobbyScene";
    public string gameSceneName = "GameScene";
    public string gameOverSceneName = "GameOver";

    public float dieTime;

    public int winStreak;

    public int wins;

    PlayerSynchronizer playerSynchronizer;
    ScoreManager scoreManager;

    List<float> fourSecondKillList;

    void Start()
    {

        fourSecondKillList = new List<float>();

        SteamUserStats.RequestCurrentStats();

        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;

/*        MySettings.Init();*/
        wins = UserStatsManager.wins;

    }

    private void SceneManager_sceneUnloaded(Scene arg0)
    {


        if (arg0.name.Equals(lobbySceneName))
        {

            if(lobbyKills > UserStatsManager.maxLobbyKills)
            {
                UserStatsManager.maxLobbyKills = lobbyKills;
                SteamUserStats.SetStat("cws", Mathf.Clamp(lobbyKills, 0, 40));
            }


            lobbyKills = 0;

        }

        if (arg0.name.Equals(gameSceneName)) gameKills = 0;

        if (playerSynchronizer.localSquare && playerSynchronizer.playerIdentities != null)
        {

            foreach (PlayerData player in playerSynchronizer.playerIdentities)
            {

                player.square.killStreak = 0;

            }

            UserStatsManager.SaveStats();

        }

        if (arg0.name.Equals(gameOverSceneName))
        {

            SteamUserStats.StoreStats();
            UserStatsManager.SaveStats();

        }

    }

    void Update()
    {
        dieTime += Time.deltaTime;

        for(int i = 0; i < fourSecondKillList.Count; i++)
        {
            fourSecondKillList[i] += Time.deltaTime;
        }

        RefreshKillList();

    }

    void RefreshKillList()
    {

        bool continueRefresh = false;

        int indexToRemove = 0;

        for (int i = 0; i < fourSecondKillList.Count; i++)
        {
            if (fourSecondKillList[i] >= 4)
            {
                
                indexToRemove = i;
                continueRefresh = true;
            
            }
        }

        if (continueRefresh)
        {
            fourSecondKillList.RemoveAt(indexToRemove);
            continueRefresh = true;
            RefreshKillList();
        }

    }

    public void Kill(byte deadId, byte killerId)
    {

        if ((byte)playerSynchronizer.localSquare.GetGameID() != killerId) return;

        UserStatsManager.kills++;

        if (scoreManager.inGame)
        {

            fourSecondKillList.Add(0f);

            if (fourSecondKillList.Count >= 4)
            {
                SteamUserStats.SetStat("epi", 1);
            }

        }

        if (SceneManager.GetActiveScene().name.Equals(lobbySceneName))
        {
            lobbyKills++;
        }
        else
        {
            gameKills++;

            PlayerBehaviour deadPlayer = null;
            PlayerBehaviour killerPlayer = null;

            foreach(PlayerData player in playerSynchronizer.playerIdentities)
            {

                if ((byte)player.square.GetGameID() == deadId) deadPlayer = player.square;
                if ((byte)player.square.GetGameID() == killerId) killerPlayer = player.square;

            }

            if(deadPlayer && killerPlayer)
            {

                if (deadPlayer.killStreak >= 10)
                {

                    SteamUserStats.SetStat("dae", 1);

                }

            }

        }

    }

    public void Die(byte deadId)
    {

        if ((byte)playerSynchronizer.localSquare.GetGameID() != deadId) return;

        UserStatsManager.deaths++;

        if (dieTime < 0.5f)
        {

            SteamUserStats.SetStat("dws", 1);

        }

    }

    public void Spawn(byte spawnId)
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        if ((byte)playerSynchronizer.localSquare.GetGameID() != spawnId) return;

        dieTime = 0;

    }

    public void GameEnd()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        bool givePDM = true;

        if(playerSynchronizer.localSquare.score >= 10)
        {

            Win();

            foreach (PlayerData player in playerSynchronizer.playerIdentities)
            {

                if (player.square.isLocalPlayer) continue;

                if(player.square.score != 0 && scoreManager.gameMode == ScoreManager.Mode.DM) givePDM = false;

            }

        }
        else
        {

            givePDM = false;
            Lose();

        }

        if (givePDM)
        {

            SteamUserStats.SetStat("pdm", 1);

        }

        SteamUserStats.SetStat("wmi", Mathf.Clamp(UserStatsManager.maxWinStreak, 0, 10));

    }

    public void Win()
    {

        UserStatsManager.wins++;


        SteamUserStats.SetStat("Wins", UserStatsManager.wins);

        this.wins = UserStatsManager.wins;


        winStreak++;
        UserStatsManager.maxWinStreak = winStreak > UserStatsManager.maxWinStreak ? winStreak : UserStatsManager.maxWinStreak;

        if(gameKills == 0)
        {

            SteamUserStats.SetStat("puw", 1);

        }
    }

    public void Lose()
    {

        UserStatsManager.maxWinStreak = winStreak > UserStatsManager.maxWinStreak ? winStreak : UserStatsManager.maxWinStreak;
        winStreak = 0;

    }

    struct KillP4M
    {
        public float timer;
    }

}
