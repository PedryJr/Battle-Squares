using Unity.Netcode;
using UnityEngine;

public sealed class VoiceSynchronizer : NetworkBehaviour
{

    [SerializeField]
    NetworkObject voiceNetworker;

    private void Awake()
    {

        NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;

    }

    private void NetworkManager_OnClientConnectedCallback(ulong id)
    {

        if (!IsHost) return;

        NetworkManager.SpawnManager.InstantiateAndSpawn(voiceNetworker, id);

    }
}
