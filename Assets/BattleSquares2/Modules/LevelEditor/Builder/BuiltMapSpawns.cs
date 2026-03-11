using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class BuiltMapSpawns : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    public static BuiltMapSpawns instance;

    private void Awake()
    {
        instance = this;
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    private void OnDestroy()
    {
        instance = null;
    }

    private void Start()
    {
        transform.position = GetSpawn(playerSynchronizer.localSquare.GetGameID());
    }

    float spawnCycle = 0f;

    Transform[] spawns;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InitializeSpawns()
    {
        
        spawns = GetComponentsInChildren<Transform>().Where(t => t != transform).ToArray();
        foreach (var item in spawns) item.SetParent(transform.parent, true);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 GetSpawn(byte playerId) => spawns[(int)((spawnCycle / 4) + PlayerIdToListIndex(playerId)) % spawns.Length].position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        spawnCycle = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++)
        {
            PlayerBehaviour player = playerSynchronizer.playerIdentities[i].square;
            if (!player.isLocalPlayer) continue;
            if (!(!player.isDead && !player.spawnBuffer)) continue;
            transform.position = GetSpawn(player.GetGameID());
        }
    }

    int PlayerIdToListIndex(byte playerId)
    {
        for (int i = 0; i < playerSynchronizer.playerIdentities.Count; i++) if (playerSynchronizer.playerIdentities[i].square.GetGameID() == playerId) return i;
        return playerId;
    }

}
