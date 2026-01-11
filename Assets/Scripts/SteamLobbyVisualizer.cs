using Steamworks;
using System.Linq;
using TMPro;
using UnityEngine;

public class SteamLobbyVisualizer : MonoBehaviour
{
    PlayerSynchronizer playerSynchronizer;
    TMP_Text text;

    private void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        text = GetComponent<TMP_Text>();
        DontDestroyOnLoad(this.transform.parent.gameObject);
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    //void Update() => UpdateFunc();

    void UpdateFunc()
    {
        string outPut = string.Empty;
        if (SteamNetwork.currentLobby != null)
        {

            GameObject lobbyPreviewOBJ = GameObject.FindGameObjectWithTag("LobbyPreview");
            LobbyBehaviour selectedLobby = null;
            if (lobbyPreviewOBJ) selectedLobby = GameObject.FindGameObjectWithTag("LobbyPreview").GetComponent<LobbyBehaviour>();
            if (selectedLobby)
            {
                foreach (var item in selectedLobby.GetLobby.Data)
                {
                    outPut += item.Key + ": " + item.Value + "\n";
                }

                /*                outPut += $"Selected Lobby ID: {selectedLobby.lobby.Id}\n";
                                outPut += $"Selected Lobby Validity: {selectedLobby.lobby.IsValid}\n";
                                outPut += $"Selected Lobby Avalible: {selectedLobby.lobby.IsAvalible}\n";
                                outPut += $"Selected Lobby Name: {selectedLobby.lobby.OwnerName}\n";
                                outPut += $"Selected Lobby OwnerId: {selectedLobby.lobby.OwnerId}\n";*/
            }
            else
            {
                if (playerSynchronizer)
                {
                    outPut += $"Previous MMR: {playerSynchronizer.localSquare.previousMMR}\n";
                    outPut += $"Current MMR: {playerSynchronizer.localSquare.MMR}";
                }
            }


        }
        else
        {
            if (playerSynchronizer)
            {
                outPut += $"Previous MMR: {playerSynchronizer.localSquare.previousMMR}\n";
                outPut += $"Current MMR: {playerSynchronizer.localSquare.MMR}";
            }
        }

        text.text = outPut;
    }
}
