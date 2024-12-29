using UnityEngine;

public sealed class EditorSwitchBehaviour : MonoBehaviour
{

    bool editorCanvasOn = false;

    [SerializeField]
    Canvas mainCanvas;

    [SerializeField]
    Canvas editorCanvas;

    [SerializeField]

    public void TOGGLEEDITOR()
    {

        editorCanvasOn = !editorCanvasOn;

        if (editorCanvasOn)
        {
            editorCanvas.gameObject.SetActive(true);
            mainCanvas.gameObject.SetActive(false);
        }
        else
        {
            editorCanvas.gameObject.SetActive(false);
            mainCanvas.gameObject.SetActive(true);
        }

    }

}
