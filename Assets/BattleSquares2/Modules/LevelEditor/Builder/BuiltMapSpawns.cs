using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class BuiltMapSpawns : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    private void Start()
    {
        transform.position = GetSpawn(playerSynchronizer.localSquare.GetID());
    }

    float spawnCycle = 0f;

    Transform[] spawns;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializeSpawns()
    {
        
        spawns = GetComponentsInChildren<Transform>().Where(t => t != transform).ToArray();
        foreach (var item in spawns) item.SetParent(transform.parent, true);
        /*        foreach (PlayerData player in playerSynchronizer.playerIdentities)
                {
                    Debug.Log($"Player ID: {player.square.GetID()} assigned to spawn at position {GetSpawn(player.square.GetID())}");
                }*/

/*        Debug.Log($"Player ID: {0} assigned to spawn at position {GetSpawn(0)}");
        Debug.Log($"Player ID: {1} assigned to spawn at position {GetSpawn(1)}");*/

    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetSpawn(byte playerId) => spawns[(int)((spawnCycle * 2) + PlayerIdToListIndex(playerId)) % spawns.Length].position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        spawnCycle += Time.deltaTime;
        if (!playerSynchronizer.localSquare.isDead && !playerSynchronizer.localSquare.spawnBuffer)
        {
            transform.position = GetSpawn(playerSynchronizer.localSquare.GetID());
        }
    }

    int PlayerIdToListIndex(byte playerId)
    {
        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++) if (playerSynchronizer.playerIdentities[i].square.GetID() == playerId) return i;
        return playerId;
    }

}
