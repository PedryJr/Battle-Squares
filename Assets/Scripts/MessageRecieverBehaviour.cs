using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static PlayerSynchronizer;
using static Unity.VisualScripting.Member;

public class MessageRecieverBehaviour : MonoBehaviour
{

    [SerializeField]
    ChatBubbleBehaviour bubbleBehaviour;

    PlayerSynchronizer playerSynchronizer;

    List<ChatBubbleBehaviour> tabs;

    Inputs inputs;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        tabs = new List<ChatBubbleBehaviour>();

        inputs = new Inputs();

        inputs.GameUI.Tab.performed += (context) => 
        { 
        
            foreach (PlayerData player in playerSynchronizer.playerIdentities)
            {

                ChatBubbleBehaviour newChatBubble = Instantiate(bubbleBehaviour, transform);
                newChatBubble.ApplyMessage(player.square.playerName, player.square);
                tabs.Add(newChatBubble);

            }

        };

        inputs.GameUI.Tab.canceled += (context) => 
        { 
        
            foreach(ChatBubbleBehaviour chatBubble in tabs)
            {

                if(chatBubble) chatBubble.state = ChatBubbleBehaviour.AnimationState.Shrinking;

            }

            tabs.Clear();

        };

        inputs.Enable();


    }

    public void CreateNewMessage(string message, PlayerBehaviour source)
    {

        ChatBubbleBehaviour newChatBubble = Instantiate(bubbleBehaviour, transform);
        newChatBubble.ApplyMessage(message, source);

    }

    private void OnDisable()
    {

        inputs.Disable();

    }

    private void OnDestroy()
    {

        inputs.Disable();
        inputs.Dispose();

    }

}
