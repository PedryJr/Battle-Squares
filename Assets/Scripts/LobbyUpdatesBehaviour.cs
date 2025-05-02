using Unity.Netcode;
using UnityEngine;
using static PlayerSynchronizer;

public sealed class LobbyUpdatesBehaviour : MonoBehaviour
{

    PlayerSynchronizer playerSynchronizer;

    float timer;

    float[] oldMods;

    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        oldMods = new float[Mods.at.Length];
    }

    void Update()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        if (NetworkManager.Singleton.IsHost)
        {

            bool shouldUpdate = false;

            for (int i = 0; i < Mods.at.Length; i++)
            {

                if (oldMods[i] != Mods.at[i])
                {
                    shouldUpdate = true;
                    break;
                }

            }

            if (shouldUpdate)
            {

                for (int i = 0; i < Mods.at.Length; i++) oldMods[i] = Mods.at[i];

                playerSynchronizer.SendModsDataClientRpc(Mods.at);

            }

            playerSynchronizer.localSquare.ready = true;

        }

        timer += Time.deltaTime * 5;

        if(timer > 1)
        {

            playerSynchronizer.UpdatePlayerReady(playerSynchronizer.localSquare.ready);
            timer = 0;

        }

        foreach(PlayerData player in playerSynchronizer.playerIdentities)
        {

            if(player.square.rb.gravityScale != (3f * Mods.at[0]))
            {
                player.square.rb.gravityScale = 3f * Mods.at[0];
            }

            if(player.square.maxHealthPoints != Mods.at[10])
            {

                player.square.healthPoints = player.square.healthPoints / player.square.maxHealthPoints * Mods.at[10];
                player.square.maxHealthPoints = Mods.at[10];

            }

            if(player.square.rb.sharedMaterial.bounciness != Mods.at[14])
            {

                foreach (PlayerData player2 in playerSynchronizer.playerIdentities)
                {

                    player2.square.physMat.bounciness = Mods.at[14];
                    player2.square.rb.sharedMaterial = player.square.physMat;
                    player2.square.col.sharedMaterial = player.square.physMat;

                }

            }

            if (player.square.rb.sharedMaterial.friction != Mods.at[15])
            {

                foreach (PlayerData player2 in playerSynchronizer.playerIdentities)
                {
                    player2.square.physMat.friction = Mods.at[15];
                    player2.square.rb.sharedMaterial = player.square.physMat;
                    player2.square.col.sharedMaterial = player.square.physMat;

                }

            }

            player.square.newMods = true;

        }

    }
}
