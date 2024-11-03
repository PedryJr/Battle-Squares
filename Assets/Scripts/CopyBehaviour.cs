using System.Linq;
using TMPro;
using UnityEngine;
using System;
using Unity.Netcode;

public class CopyBehaviour : MonoBehaviour
{

    [SerializeField]
    TMP_Text codeField;

    string code;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsHost) return;
        code = GetRandomString(8);
        codeField.text = code;
    }

    private void Update()
    {

        if (!NetworkManager.Singleton.IsListening) return;

        if (SteamNetwork.currentLobby != null)
        {

            if (NetworkManager.Singleton.IsHost)
            {
                SteamNetwork.currentLobby?.SetData("Code", code);
            }
            else
            {

                string data = SteamNetwork.currentLobby?.GetData("Code");

                if (!$"{data}".Equals($"{code}"))
                {

                    code = data;
                    codeField.text = code;

                }

            }

        }

    }

    private System.Random random = new System.Random();

    public string GetRandomString(int length)
    {
        const string chars = "QWERTYUIOPASDFGHJKLZXCVBNM";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public void COPY()
    {

        GUIUtility.systemCopyBuffer = code;

    }

}
