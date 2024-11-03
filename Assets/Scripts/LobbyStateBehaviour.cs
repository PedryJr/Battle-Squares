using TMPro;
using UnityEngine;

public class LobbyStateBehaviour : MonoBehaviour
{

    [SerializeField]
    TMP_Text accessField;

    public static bool access = false;

    private void Start()
    {

        if (access)
        {
            SteamNetwork.currentLobby?.SetJoinable(true);
            SteamNetwork.currentLobby?.SetData("Avalible", "true");
            accessField.text = "Public";
        }
        else
        {
            SteamNetwork.currentLobby?.SetJoinable(true);
            SteamNetwork.currentLobby?.SetData("Avalible", "false");
            accessField.text = "Private";
        }

    }

    public void ACESS()
    {

        access = !access;

        if (access)
        {
            SteamNetwork.currentLobby?.SetJoinable(true);
            SteamNetwork.currentLobby?.SetData("Avalible", "true");
            accessField.text = "Public";
        }
        else
        {
            SteamNetwork.currentLobby?.SetJoinable(true);
            SteamNetwork.currentLobby?.SetData("Avalible", "false");
            accessField.text = "Private";
        }

    }


}
