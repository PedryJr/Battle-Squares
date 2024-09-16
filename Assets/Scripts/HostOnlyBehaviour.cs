using Unity.Netcode;
using UnityEngine;

public sealed class HostOnlyBehaviour : MonoBehaviour
{

    private void OnEnable()
    {

        bool host = NetworkManager.Singleton.IsHost;

        if (!host)
        {
            gameObject.SetActive(false);
        }

    }

}
