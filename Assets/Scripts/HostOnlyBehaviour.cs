using Unity.Netcode;
using UnityEngine;

public sealed class HostOnlyBehaviour : MonoBehaviour
{

    [SerializeField]
    bool destroy;

    private void Start()
    {

        bool host = NetworkManager.Singleton.IsHost;

        if (!host)
        {
            if (destroy)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }

        }

    }

}
