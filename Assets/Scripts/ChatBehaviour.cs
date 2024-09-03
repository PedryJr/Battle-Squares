using Steamworks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class ChatBehaviour : MonoBehaviour
{

    [SerializeField]
    TextMeshProUGUI context;

    public void SubmitChat()
    {

        GameObject.FindGameObjectWithTag("Sync").GetComponent<ChatManager>().SpreadMessage(context.text, SteamClient.SteamId);

    }

}
