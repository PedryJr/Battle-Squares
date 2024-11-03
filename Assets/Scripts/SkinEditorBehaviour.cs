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
    TMP_Text buttonText;

    PaintAreaBehaviour activePainter;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        animate = playerSynchronizer.skinData.animate;
        if (animate)
        {
            if (buttonText) buttonText.text = "Animation - ON";
        }
        else
        {
            if (buttonText) buttonText.text = "Animation - Off";
        }

    }

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

        playerSynchronizer.skinData.animate = animate;

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

    private void OnEnable()
    {
        
        if(playerSynchronizer.skinData.animate) activePainter = Instantiate(animPainter, transform);
        else activePainter = Instantiate(noAnimPainter, transform);

    }

    private void OnDisable()
    {

        Destroy(activePainter.gameObject);

    }

}
