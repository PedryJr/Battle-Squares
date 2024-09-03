using Steamworks;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ChatManager : NetworkBehaviour
{

    List<Chat> chatList;

    ChatSquare chatSquare;

    [SerializeField]
    ChatContainer chatContainer;

    void Awake()
    {
        chatList = new List<Chat>();
    }

    public void SpreadMessage(string context, SteamId steamId)
    {

        ChatContainer chatBubble = Instantiate(chatContainer, chatSquare.container.transform);
        ulong ignoreId = NetworkManager.LocalClientId;
        chatBubble.Initialize(context, steamId, ignoreId);
        if (IsHost)
        {

            SpreadMessageClientRpc(context, steamId.Value, ignoreId);

        }
        if (!IsHost)
        {

            SpreadMessageServerRpc(context, steamId.Value, ignoreId);

        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void SpreadMessageServerRpc(string context, ulong id, ulong ignoreId)
    {
        if (NetworkManager.LocalClientId == ignoreId) return;
        SteamId steamId = new SteamId() { Value = id };

        SpreadMessageClientRpc(context, steamId.Value, ignoreId);

        ChatContainer chatBubble = Instantiate(chatContainer, chatSquare.container.transform);
        chatBubble.Initialize(context, steamId, ignoreId);

    }

    [ClientRpc]
    public void SpreadMessageClientRpc(string context, ulong id, ulong ignoreId)
    {
        if (NetworkManager.LocalClientId == ignoreId) return;
        if (IsHost) return;

        SteamId steamId = new SteamId() { Value = id };

        ChatContainer chatBubble = Instantiate(chatContainer, chatSquare.container.transform);
        chatBubble.Initialize(context, steamId, ignoreId);

    }

    public void SetChatSquare(ChatSquare chatSquare)
    {

        this.chatSquare = chatSquare;

    }

    struct Chat
    {

        public SteamId steamId;
        public string context;

    }
}
