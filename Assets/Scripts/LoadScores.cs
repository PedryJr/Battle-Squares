using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LoadScores : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    [SerializeField]
    ScoreContent scoreContent;

    float timer;
    bool load = true;
    private void Awake()
    {

        timer = 0;

        playerSynchronizer = FindFirstObjectByType<PlayerSynchronizer>();

        playerSynchronizer.CalculateMMR();

        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++)
        {

            string playerName = playerSynchronizer.playerIdentities[i].square.playerName;
            Sprite playerSprite = playerSynchronizer.playerIdentities[i].square.pfp;
            int playerScore = playerSynchronizer.playerIdentities[i].square.score;
            double oldPlayerMMR = playerSynchronizer.playerIdentities[i].square.previousMMR;
            double newPlayerMMR = playerSynchronizer.playerIdentities[i].square.MMR;

            ScoreContent scoreContent = Instantiate(this.scoreContent, transform);
            scoreContent.Init(playerSprite, playerName, playerScore, oldPlayerMMR, newPlayerMMR, playerSynchronizer.playerIdentities[i].square);

        }

    }

    private void Update()
    {
        if (!load) return;

        timer += Time.deltaTime;

        if(timer >= 4.65f)
        {

            if (NetworkManager.Singleton.IsHost) SceneManager.LoadSceneAsync("LobbyScene", LoadSceneMode.Single);
            load = false;

        }


    }

}
