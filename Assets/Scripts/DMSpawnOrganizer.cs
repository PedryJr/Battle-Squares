using UnityEngine;
using UnityEngine.PlayerLoop;
using static PlayerSynchronizer;

public sealed class DMSpawnOrganizer : MonoBehaviour
{

    [SerializeField]
    Transform[] spawns;

    [SerializeField]
    Transform spawn;

    PlayerSynchronizer playerSynchronizer;

    int spawnIndex;

    float spawnRefreshTimer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Start()
    {

        SetFirstSpawn();

    }

    private void Update()
    {

        if(SpawnTimer()) MoveSpawns();

    }

    void SetFirstSpawn()
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

            if (playerSynchronizer.localSquare.id == sortedPlayers[i].square.id)
            {

                spawn.position = spawns[i].position;
                spawn.parent = spawns[i].parent;
                sortedPlayers[i].square.transform.position = spawn.position;
                spawnIndex = i;

            }

        }

    }

    bool SpawnTimer()
    {

        spawnRefreshTimer += Time.deltaTime * 7;

        if(spawnRefreshTimer > 1)
        {
            spawnRefreshTimer -= 1;
            return true;
        }
        else return false;

    }

    void MoveSpawns()
    {

        int newIndex = spawnIndex++ % spawns.Length;
        if(!playerSynchronizer.localSquare.isDead && !playerSynchronizer.localSquare.spawnBuffer) spawn.SetPositionAndRotation(spawns[newIndex].position, spawns[newIndex].rotation);

    }

}
