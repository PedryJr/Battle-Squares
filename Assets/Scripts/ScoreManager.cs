using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PlayerSynchronizer;

public sealed class ScoreManager : NetworkBehaviour
{

    public bool inGame;

    float timer = 0;

    [SerializeField]
    ScoreBoard scoreBoard;

    List<ScoreBoard> scores;

    PlayerSynchronizer playerSynchronizer;

    [SerializeField]
    public Mode gameMode;

    public int startScore = 0;
    public int endScore = 10;

    Hunter hunter;

    private void Awake()
    {
        
        scores = new List<ScoreBoard>();
        hunter = GetComponent<Hunter>();
        SceneManager.sceneLoaded += LoadGameScene;
        SceneManager.sceneUnloaded += UnloadGameScene;
        playerSynchronizer = GetComponent<PlayerSynchronizer>();

    }

    private void Update()
    {

        AutoUpdate();

    }

    void AutoUpdate()
    {
        if (inGame)
        {

            timer += Time.deltaTime;

            if (timer > 1)
            {

                UpdateScoreBoardFunc();
                timer = 0;

            }

            if (scores.Count > 0)
            {
                foreach (ScoreBoard scoreboard in scores) CheckScore(scoreboard);
            }

        }
        else
        {

            timer = 0;

        }
    }

    void CheckScore(ScoreBoard scoreboard)
    {
        if (!IsHost) return;
        if (scoreboard.playerData.square.score >= endScore)
        {
            SceneManager.LoadSceneAsync("GameOver");
        }
    }

    public void LoadGameScene(Scene arg0, LoadSceneMode arg1)
    {

        timer = 0.9f;

        if (arg0.name.Equals("GameScene"))
        {

            try
            {

                inGame = true;
                UpdateScoreBoardFunc();

                CursorBehaviour.SetEnabled(false);
                Cursor.lockState = CursorLockMode.Locked;
                foreach (PlayerData player in playerSynchronizer.playerIdentities) player.square.score = 0;
                UpdateScoreBoardFunc();

            }
            catch (NullReferenceException)
            {

                SteamNetwork.currentLobby?.Leave();

                SteamNetwork.CreateNewLobby();

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

    }

    public void UnloadGameScene(Scene arg0)
    {

        if (arg0.name.Equals("GameScene"))
        {
            hunter.GameEnd();
            inGame = false;
            CursorBehaviour.SetEnabled(true);
            Cursor.lockState = CursorLockMode.None;
            scores.Clear();
        }
    }

    public void UpdateScoreBoard()
    {

        if (IsHost)
        {
            UpdateScoreBoardClientRpc();
        }

        if(!IsHost) UpdateScoreBoardServerRpc();

    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateScoreBoardServerRpc()
    {
        UpdateScoreBoardClientRpc();
    }

    [ClientRpc]
    public void UpdateScoreBoardClientRpc()
    {
        UpdateScoreBoardFunc();
    }


    void UpdateScoreBoardFunc()
    {

        if (!inGame) return;

        GameObject scorePanel = GameObject.FindGameObjectWithTag("Score");

        foreach (ScoreBoard scoreboard in scores)
        {

            if(scoreBoard != null)
                if(scoreBoard.gameObject)
                    Destroy(scoreboard.gameObject);

        }
        scores.Clear();

        if(playerSynchronizer.playerIdentities != null)
        {

            if (playerSynchronizer.playerIdentities.Count < 2) return;

            foreach (PlayerData player in playerSynchronizer.playerIdentities)
            {

                ScoreBoard newBoard = Instantiate(scoreBoard, scorePanel.transform);
                scores.Add(newBoard);
                newBoard.SetScore(player);

            }
        }

    }

    public void ConnectClient()
    {

    }

    public void UpdateModeAsHost(Mode gameMode)
    {

        UpdateGameModeServerRpc(gameMode);

    }

    [ServerRpc(RequireOwnership = false)]
    void UpdateGameModeServerRpc(Mode gameMode)
    {

        UpdateGameModeClientRpc(gameMode);

    }

    [ClientRpc]
    void UpdateGameModeClientRpc(Mode gameMode)
    {

        this.gameMode = gameMode;

        GameModeDisplayBehaviour modeDisplay = FindAnyObjectByType<GameModeDisplayBehaviour>();
        if(modeDisplay) modeDisplay.DisplayGameMode(gameMode);

    }

    public enum Mode
    {

        DM,
        DT,
        CTF

    }

}
