using UnityEngine;

public sealed class ChatSquare : MonoBehaviour
{

    [SerializeField]
    public GameObject container;

    private void Start()
    {
        
        GameObject.FindGameObjectWithTag("Sync").GetComponent<ChatManager>().SetChatSquare(this);

    }

}