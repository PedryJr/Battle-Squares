using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
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

    private void Awake()
    {
        
        scores = new List<ScoreBoard>();
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
        if (scoreboard.playerData.square.score >= 10) SceneManager.LoadSceneAsync("GameOver");
    }

    public void LoadGameScene(Scene arg0, LoadSceneMode arg1)
    {

        timer = 0.9f;

        if (arg0.name.Equals("GameScene"))
        {

            inGame = true;
            UpdateScoreBoardFunc();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            foreach (PlayerData player in playerSynchronizer.playerIdentities) player.square.score = 0;
            UpdateScoreBoardFunc();

        }
    }

    public void UnloadGameScene(Scene arg0)
    {

        if (arg0.name.Equals("GameScene"))
        {
            inGame = false;
            Cursor.visible = true;
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

            if(!scoreBoard.IsDestroyed())
                if(!scoreBoard.gameObject.IsDestroyed())
                    Destroy(scoreboard.gameObject);

        }
        scores.Clear();

        if(playerSynchronizer.playerIdentities != null)
        {
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

        Debug.Log($"Mode Updated! ({gameMode.ToString()})");

    }

    public enum Mode
    {

        DM,
        TDM,
        CTF

    }

}
