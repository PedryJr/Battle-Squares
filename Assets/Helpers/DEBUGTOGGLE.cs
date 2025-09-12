using UnityEngine;
using UnityEngine.Windows;

public class DEBUGTOGGLE : MonoBehaviour
{
    
    DebugToggleInputs inputs;

    private void Awake()
    {
        inputs = new DebugToggleInputs();
        inputs.Main.Toggle.performed += (e) => gameObject.SetActive(!gameObject.activeSelf);
        inputs.Enable();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        inputs.Dispose();
    }

    private void Update()
    {
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        Cursor.visible = true;
    }

}
