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

        if(SteamNetwork.currentLobby != null)
        {

            if (preview.ownerId == SteamNetwork.currentLobby.Value.Owner.Id)
            {
                textField.text = "Create Lobby";
            }
            else
            {
                textField.text = "Join Lobby";
            }

        }
        else
        {
            textField.text = "Loading...";
        }
    }
}
