using UnityEngine;
using static PlayerSynchronizer;

public class DMSpawnOrganizer : MonoBehaviour
{

    [SerializeField]
    Transform[] spawns;

    [SerializeField]
    Transform spawn;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Start()
    {

        PlayerData[] sortedPlayers = new PlayerData[playerSynchronizer.playerIdentities.Count];
        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++)
        {
            sortedPlayers[i] = playerSynchronizer.playerIdentities[i];
        }

        for (int i = 0; i < sortedPlayers.Length - 1; i++)
        {
            for (int j = 0; j < sortedPlayers.Length - 1 - i; j++)
            {
                if (sortedPlayers[j].id > sortedPlayers[j + 1].id)
                {
                    PlayerData temp = sortedPlayers[j];
                    sortedPlayers[j] = sortedPlayers[j + 1];
                    sortedPlayers[j + 1] = temp;
                }
            }
        }


        for (int i = 0; i < sortedPlayers.Length; i++)
        {

            if(playerSynchronizer.localSquare.id == sortedPlayers[i].square.id)
            {

                spawn.position = spawns[i].position;
                sortedPlayers[i].square.transform.position = spawn.position;

            }
            
        }

    }

}
