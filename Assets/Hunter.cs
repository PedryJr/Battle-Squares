using NUnit.Framework;
using Steamworks;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public class Hunter : NetworkBehaviour
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

        MySettings.Init();
        wins = MySettings.wins;

    }

    private void SceneManager_sceneUnloaded(Scene arg0)
    {


        if (arg0.name.Equals(lobbySceneName))
        {

            if(lobbyKills > MySettings.maxLobbyKills)
            {
                MySettings.maxLobbyKills = lobbyKills;
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

            MySettings.SaveStats();

        }

        if (arg0.name.Equals(gameOverSceneName))
        {

            SteamUserStats.StoreStats();
            MySettings.SaveStats();

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

        if ((byte)playerSynchronizer.localSquare.id != killerId) return;

        MySettings.kills++;

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

                if ((byte)player.id == deadId) deadPlayer = player.square;
                if ((byte)player.id == killerId) killerPlayer = player.square;

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

        if ((byte)playerSynchronizer.localSquare.id != deadId) return;

        MySettings.deaths++;

        if (dieTime < 0.5f)
        {

            SteamUserStats.SetStat("dws", 1);

        }

    }

    public void Spawn(byte spawnId)
    {

        if ((byte)playerSynchronizer.localSquare.id != spawnId) return;

        dieTime = 0;

    }

    public void GameEnd()
    {

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

        SteamUserStats.SetStat("wmi", Mathf.Clamp(MySettings.maxWinStreak, 0, 10));

    }

    public void Win()
    {

        MySettings.wins++;


        SteamUserStats.SetStat("Wins", MySettings.wins);

        this.wins = MySettings.wins;


        winStreak++;
        MySettings.maxWinStreak = winStreak > MySettings.maxWinStreak ? winStreak : MySettings.maxWinStreak;

        if(gameKills == 0)
        {

            SteamUserStats.SetStat("puw", 1);

        }
    }

    public void Lose()
    {

        MySettings.maxWinStreak = winStreak > MySettings.maxWinStreak ? winStreak : MySettings.maxWinStreak;
        winStreak = 0;

    }

    struct KillP4M
    {
        public float timer;
    }

}
