using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BuiltMapSpawns : MonoBehaviour
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
        /*
                foreach (var item in playerSynchronizer.playerIdentities)
                {
                    Vector3 spawnPos = GetSpawn(item.square.GetID());
                    spawnPos.z = item.square.spawnPosition.z;
                    item.square.spawnPosition = spawnPos;

                    spawnPos.z = item.square.transform.position.z;
                    item.square.transform.position = spawnPos;
                }*/
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetSpawn(byte playerId) => spawns[(int)(spawnCycle * 2 + playerId) % spawns.Length].position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        spawnCycle += Time.deltaTime;
        if (!playerSynchronizer.localSquare.isDead && !playerSynchronizer.localSquare.spawnBuffer)
        {
            transform.position = GetSpawn(playerSynchronizer.localSquare.GetID());
        }
    }

}
