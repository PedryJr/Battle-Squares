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

        playerSynchronizer = FindFirstObjectByType<PlayerSynchronizer>();

        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++)
        {

            string playerName = playerSynchronizer.playerIdentities[i].name;
            Sprite playerSprite = playerSynchronizer.playerIdentities[i].pfp;
            int playerScore = playerSynchronizer.playerIdentities[i].square.score;

            ScoreContent scoreContent = Instantiate(this.scoreContent, transform);
            scoreContent.Init(playerSprite, playerName, playerScore);

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
