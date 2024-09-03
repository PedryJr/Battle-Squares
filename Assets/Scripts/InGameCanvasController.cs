using UnityEngine;

public sealed class InGameCanvasController : MonoBehaviour
{

    Inputs inputs;

    [SerializeField]
    GameObject gameCanvas;

    bool canvasOn = false;

    private void Awake()
    {

        inputs = new Inputs();

        inputs.GameUI.ToggleUI.performed += (context) => 
        {

            canvasOn = !canvasOn;

            gameCanvas.SetActive(canvasOn);
            Cursor.visible = canvasOn;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;

        };

        inputs.GameUI.ToggleUI.Enable();

    }

    private void OnDestroy()
    {
        
        inputs.Dispose();

    }

}
