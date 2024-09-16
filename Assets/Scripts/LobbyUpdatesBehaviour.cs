using UnityEngine;

public class LobbyUpdatesBehaviour : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    float timer;

    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    void Update()
    {

        timer += Time.deltaTime * 5;

        if(timer > 1)
        {
            playerSynchronizer.UpdatePlayerReady(playerSynchronizer.localSquare.ready);
            timer = 0;
        }
    }
}
