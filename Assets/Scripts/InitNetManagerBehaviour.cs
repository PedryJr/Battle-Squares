using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed class InitNetManagerBehaviour : MonoBehaviour
{

    NetworkManager networkManager;

    private void Awake()
    {
        
        networkManager = GetComponent<NetworkManager>();

        networkManager.SetSingleton();

    }

}
