using TMPro;
using UnityEngine;

public class JoinOrCreateBehaviour : MonoBehaviour
{

    [SerializeField]
    TMP_Text textField;

    [SerializeField]
    LobbyBehaviour preview;

    void Update()
    {
        if(preview.lobbyId == SteamNetwork.currentLobby.Value.Id)
        {
            textField.text = "Create Lobby";
        }
        else
        {
            textField.text = "Join Lobby";
        }
    }
}
