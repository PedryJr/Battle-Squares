using ProximityChat;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VoiceSynchronizer : NetworkBehaviour
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
