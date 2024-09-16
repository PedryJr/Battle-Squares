using UnityEngine;

public sealed class InGameCanvasController : MonoBehaviour
{

    Inputs inputs;

    [SerializeField]
    GameObject gameCanvas;

    public static bool canvasOn = false;

    private void Awake()
    {
        Cursor.visible = false;
        inputs = new Inputs();

        inputs.GameUI.ToggleUI.performed += (context) => 
        {

            canvasOn = !canvasOn;

            gameCanvas.SetActive(canvasOn);
            /*Cursor.visible = canvasOn;*/
            CursorBehaviour.SetEnabled(canvasOn);
            Cursor.lockState = canvasOn ? CursorLockMode.None : CursorLockMode.Locked;
            PlayerController.uiRegs = canvasOn ? PlayerController.uiRegs : 0;

        };

        inputs.GameUI.ToggleUI.Enable();

    }

    private void OnDestroy()
    {
        
        inputs.Dispose();

    }

}
