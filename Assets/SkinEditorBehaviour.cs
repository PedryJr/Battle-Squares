using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class SkinEditorBehaviour : MonoBehaviour
{

    bool animate = false;
    bool editorCanvasOn = false;

    [SerializeField]
    PaintAreaBehaviour noAnimPainter;

    [SerializeField]
    PaintAreaBehaviour animPainter;

    [SerializeField]
    PaintAreaBehaviour activePainter;

    public void TOGGLEANIMATE(TMP_Text buttonText)
    {

        animate = !animate;

        if (animate)
        {
            if(buttonText) buttonText.text = "Animation - ON";
            EnableAnimationEditor();
        }
        else
        {
            if (buttonText) buttonText.text = "Animation - Off";
            DisableAnimationEditor();
        }

    }

    void EnableAnimationEditor()
    {
        Destroy(activePainter.gameObject);
        activePainter = null;
        activePainter = Instantiate(animPainter, transform);
    }

    void DisableAnimationEditor()
    {
        Destroy(activePainter.gameObject);
        activePainter = null;
        activePainter = Instantiate(noAnimPainter, transform);
    }

}
