using Unity.Netcode;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class LobbyUpdatesBehaviour : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    float timer;
    [SerializeField] float syncRate = 1f;

    int modSyncIndex = 0;

    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    void FixedUpdate()
    {
        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        timer += Time.deltaTime * syncRate;
        if(timer >= 1)
        {
            timer = 0;
            if (NetworkManager.Singleton.IsHost) HostSyncs();
        }

        ApplyRBSpecificModsToAllClients();
    }

    void HostSyncs()
    {
        modSyncIndex = (modSyncIndex + 1) % Mods.at.Length;
        SyncMods();
        SyncMMR();
    }
    void SyncMMR()
    {
        playerSynchronizer.SyncMMR();
    }

    void SyncMods()
    {
        playerSynchronizer.SyncMods(modSyncIndex, Mods.at[modSyncIndex]);
        playerSynchronizer.localSquare.ready = true;
    }


    void ApplyRBSpecificModsToAllClients()
    {
        foreach (PlayerData player in playerSynchronizer.playerIdentities)
        {
            if (player.square.rb.gravityScale != (3f * Mods.PlayerGravity)) player.square.rb.gravityScale = 3f * Mods.PlayerGravity;


            if (player.square.maxHealthPoints != Mods.PlayerHealth)
            {
                player.square.healthPoints = player.square.healthPoints / player.square.maxHealthPoints * Mods.PlayerHealth;
                player.square.maxHealthPoints = Mods.PlayerHealth;
            }

            if (player.square.rb.sharedMaterial.bounciness != Mods.Bounce)
            {
                foreach (PlayerData player2 in playerSynchronizer.playerIdentities)
                {
                    player2.square.physMat.bounciness = Mods.Bounce;
                    player2.square.rb.sharedMaterial = player.square.physMat;
                    player2.square.col.sharedMaterial = player.square.physMat;
                }
            }

            if (player.square.rb.sharedMaterial.friction != Mods.Friction)
            {
                foreach (PlayerData player2 in playerSynchronizer.playerIdentities)
                {
                    player2.square.physMat.friction = Mods.Friction;
                    player2.square.rb.sharedMaterial = player.square.physMat;
                    player2.square.col.sharedMaterial = player.square.physMat;
                }
            }

            player.square.newMods = true;
        }
    }
}
