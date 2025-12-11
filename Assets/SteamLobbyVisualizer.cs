using Steamworks;
using TMPro;
using UnityEngine;

public class SteamLobbyVisualizer : MonoBehaviour
{

    TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        DontDestroyOnLoad(this.transform.parent.gameObject);
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        string outPut = string.Empty;
        if(SteamNetwork.currentLobby != null)
        {

                outPut += $"Current Lobby ID: {SteamNetwork.currentLobby.Value.Id}\n";
            GameObject lobbyPreviewOBJ = GameObject.FindGameObjectWithTag("LobbyPreview");
            LobbyBehaviour selectedLobby = null;
            if (lobbyPreviewOBJ) selectedLobby = GameObject.FindGameObjectWithTag("LobbyPreview").GetComponent<LobbyBehaviour>();
            if (selectedLobby)
            {

                //if(selectedLobby.lobby.Id != SteamNetwork.currentLobby.Value.Id) selectedLobby.lobby.Refresh();

                foreach (var item in selectedLobby.GetLobby.Data) outPut += item.Key + ": " + item.Value + "\n";


                outPut += $"Selected Lobby ID: {selectedLobby.lobby.Id}";
                outPut += $"Selected Lobby Validity: {selectedLobby.lobby.IsValid}";
            }
            else outPut += "Selected Lobby ID: NONE";


        }
        else
        {
            outPut = "NoLobbyFound";
        }

        text.text = outPut;

    }
}
