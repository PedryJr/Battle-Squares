using Steamworks;
using TMPro;
using UnityEngine;

public sealed class ChatBehaviour : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI context;

    public void SubmitChat()
    {
        GameObject.FindGameObjectWithTag("Sync").GetComponent<ChatManager>().SpreadMessage(context.text, SteamClient.SteamId);
    }
}