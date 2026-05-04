using Netcode.Transports.Facepunch;
using UnityEngine;

public class AppIDSelector : MonoBehaviour
{

    FacepunchTransport transport;

    [SerializeField] AppIDMode appIdMode;

    private void OnValidate()
    {
        transport = GetComponent<FacepunchTransport>();
        transport.SetSteamID((uint)appIdMode);
    }

    enum AppIDMode : uint
    {
        BattleSquares1 = 3180450,
        BattleSquares2 = 4148410,
        T480 = 480,
    }

}