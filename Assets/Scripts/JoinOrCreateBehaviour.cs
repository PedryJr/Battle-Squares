using TMPro;
using UnityEngine;

public sealed class JoinOrCreateBehaviour : MonoBehaviour
{

    [SerializeField]
    TMP_Text textField;

    [SerializeField]
    LobbyBehaviour preview;

    float timer = 0;
    string loadingString = "Loading...";
    const string loadingAnimation1 = "Loading";
    const string loadingAnimation2 = "Loading.";
    const string loadingAnimation3 = "Loading..";
    const string loadingAnimation4 = "Loading...";


    void Update()
    {

        timer += Time.deltaTime * 4;
        timer = Mathf.Repeat(timer, 4);

        if (timer >= 0 && timer < 1)
        {
            loadingString = loadingAnimation1;
        }
        else if(timer >= 1 && timer < 2)
        {
            loadingString = loadingAnimation2;
        }
        else if(timer >= 2 && timer < 3)
        {
            loadingString = loadingAnimation3;
        }
        else if(timer >= 3)
        {
            loadingString = loadingAnimation4;
        }

        if (SteamNetwork.currentLobby != null)
        {

            if (preview.ownerId == SteamNetwork.currentLobby.Value.Owner.Id)
            {
                textField.text = "Create Lobby";
            }
            else
            {

                if (preview.GetComponentInChildren<TMP_Text>().text.Equals("Select a lobby"))
                {

                    textField.text = loadingString;

                }
                else
                {

                    textField.text = "Join Lobby";

                }

            }

        }
        else
        {
            textField.text = loadingString;
        }
    }
}
