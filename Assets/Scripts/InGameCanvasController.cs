using TMPro;
using UnityEngine;

public sealed class InGameCanvasController : MonoBehaviour
{

    [SerializeField]
    bool inLobby;

    Inputs inputs;

    [SerializeField]
    GameObject pauseCanvas;

    [SerializeField]
    GameObject chatCanvas;

    [SerializeField]
    TMP_InputField inputField;

    [SerializeField]
    MessageRecieverBehaviour reciever;

    PlayerController playerController;
    PlayerSynchronizer playerSynchronizer;

    public static bool pauseCanvasOn = false;
    public bool chatCanvasOn = false;

    private void Awake()
    {

        playerController = FindAnyObjectByType<PlayerController>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

        pauseCanvasOn = false;
        Cursor.visible = false;
        inputs = new Inputs();

        if (inLobby)
        {

            if (chatCanvasOn)
            {
                ToggleChatVisible();
                return;
            }

            inputs.GameUI.ToggleChat.performed += (context) =>
            {

                if (chatCanvasOn) if (inputField.text != "") playerSynchronizer.SpreadInGameMessage(inputField.text);
                ToggleChatVisible();

            };

        }
        else
        {
            inputs.GameUI.ToggleUI.performed += (context) =>
            {

                if (chatCanvasOn)
                {
                    ToggleChatVisible();
                    return;
                }

                TogglePauseVisible();

            };

            inputs.GameUI.ToggleChat.performed += (context) =>
            {

                if (chatCanvasOn) if (inputField.text != "") playerSynchronizer.SpreadInGameMessage(inputField.text);
                ToggleChatVisible();

            };
        }

        inputs.GameUI.Enable();

    }

    void ToggleChatVisible()
    {

        if (pauseCanvasOn && !inLobby) return;

        chatCanvasOn = !chatCanvasOn;
        chatCanvas.SetActive(chatCanvasOn);
        if (chatCanvasOn)
        {
            inputField.ActivateInputField();
            TMP_Text[] fields = inputField.GetComponentsInChildren<TMP_Text>();

            foreach ( TMP_Text field in fields)
            {

                field.color = playerSynchronizer.localSquare.playerDarkerColor;

            }

        }
        else
        {
            inputField.DeactivateInputField();
        }

        SetPlayerControllable();

    }

    void TogglePauseVisible()
    {

        pauseCanvasOn = !pauseCanvasOn;

        pauseCanvas.SetActive(pauseCanvasOn);
        CursorBehaviour.SetEnabled(pauseCanvasOn);
        Cursor.lockState = pauseCanvasOn ? CursorLockMode.None : CursorLockMode.Locked;
        PlayerController.uiRegs = pauseCanvasOn ? PlayerController.uiRegs : 0;

        SetPlayerControllable();

    }

    void SetPlayerControllable()
    {

        if(pauseCanvasOn || chatCanvasOn) playerController.DisableController();
        else playerController.EnableController();

    }

    private void OnDestroy()
    {
        
        inputs.Dispose();

    }

}
