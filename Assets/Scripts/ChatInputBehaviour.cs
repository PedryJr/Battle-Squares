using Steamworks;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public sealed class ChatInputBehaviour : MonoBehaviour
{

    [SerializeField]
    TextMeshProUGUI inputContext;

    Inputs inputs;

    private void Awake()
    {

        inputs = new Inputs();

        inputs.ChatMaps.Submit.performed += (context) => { Submit(); };

        inputs.ChatMaps.Enable();

    }

    void Submit()
    {

        if(GetComponent<TMP_InputField>().text != "")
        {

            string context = SanitizeMessage(inputContext.text);

            ChatManager chatManager = GameObject.FindGameObjectWithTag("Sync").GetComponent<ChatManager>();
            chatManager.SpreadMessage(context, SteamClient.SteamId);

            GetComponent<TMP_InputField>().text = "";
            inputContext.text = "";
            GetComponent<TMP_InputField>().DeactivateInputField(true);

            GetComponent<TMP_InputField>().enabled = false;  // Temporarily disable the input field to ensure it doesn't get reactivated

            // Re-enable the input field after a short delay, if necessary
            StartCoroutine(ReenableInputField(GetComponent<TMP_InputField>()));

        }

    }
    
    private IEnumerator ReenableInputField(TMP_InputField inputField)
    {
        yield return new WaitForEndOfFrame(); // Wait for the end of the current frame
        inputField.enabled = true;            // Re-enable the input field
    }

    string SanitizeMessage(string message)
    {
        message = message.Length > 120 ? message.Substring(0, 120) : message;

        message = Regex.Replace(message, @"[^a-zA-Z0-9\s.,!?;:'""\-\(\)]", string.Empty);

        return message;
    }

    private void OnDestroy()
    {
        
        inputs.Dispose();

    }

}
