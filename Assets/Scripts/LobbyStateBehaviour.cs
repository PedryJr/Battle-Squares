using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public sealed class LobbyStateBehaviour : MonoBehaviour
{

    [SerializeField]
    TMP_Text accessField;

    public static bool access = true;

    float timer = 0;

    private void Start()
    {
        UpdateAccessField();

    }

    private void Update()
    {
        timer += Time.deltaTime;
        if(timer > 2)
        {
            timer = 0;
            UpdateAccessField();
        }
    }

    public void ACESS()
    {

        access = !access;

        UpdateAccessField();

    }

    public void FORCEACESS(bool value)
    {

        access = value;

        UpdateAccessField();

    }

    void UpdateAccessField()
    {

        if (NetworkManager.Singleton.IsHost)
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

    }


}
