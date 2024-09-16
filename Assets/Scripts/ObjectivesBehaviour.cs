using UnityEngine;
using static PlayerSynchronizer;

public class ObjectivesBehaviour : MonoBehaviour
{

    public FlagBehaviour[] flags;

    public ISync[] syncs;

    IObjective[] objectives;

    ScoreManager scoreManager;
    MapSynchronizer mapSynchronizer;
    PlayerSynchronizer playerSynchronizer;

    float syncsPerSecond = 20;
    float syncTimer;
    bool inGame;

    int syncIndex;

    private void Awake()
    {
        
        syncs = GetComponentsInChildren<ISync>();
        flags = GetComponentsInChildren<FlagBehaviour>();
        objectives = GetComponentsInChildren<IObjective>();

        scoreManager = FindFirstObjectByType<ScoreManager>();
        mapSynchronizer = FindFirstObjectByType<MapSynchronizer>();
        mapSynchronizer.objectives = this;
        playerSynchronizer = FindFirstObjectByType<PlayerSynchronizer>();

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

            objectives[i].EnableWithOwner(sortedPlayers[i]);
        }

    }

    private void Update()
    {
        
        inGame = scoreManager.inGame;

    }

    private void FixedUpdate()
    {

        if(!inGame) return;
        syncTimer += Time.deltaTime * syncsPerSecond * syncs.Length;

        if (!(syncTimer >= 1f)) return;

        while (syncTimer >= 1f)
        {

            if (syncIndex >= syncs.Length) syncIndex = 0;
            if (syncs[syncIndex].ShouldSync()) syncs[syncIndex].DoSync();
            syncIndex++;
            syncTimer--;

        }

    }

}
