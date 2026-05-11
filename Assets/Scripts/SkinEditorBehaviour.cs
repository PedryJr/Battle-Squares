using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkinEditorBehaviour : MonoBehaviour
{

    bool animate = false;

    [SerializeField]
    PaintAreaBehaviour noAnimPainter;

    [SerializeField]
    PaintAreaBehaviour animPainter;

    [SerializeField]
    TMP_Text buttonText;
    [SerializeField]
    int AnimOnID, AnimOffID;

    PaintAreaBehaviour activePainter;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        animate = playerSynchronizer.skinData.animate;
        if (animate)
        {
            if (buttonText) buttonText.text = Translation_Manager.GetTranslation(AnimOnID);
        }
        else
        {
            if (buttonText) buttonText.text = Translation_Manager.GetTranslation(AnimOffID);
        }

    }

    public void TOGGLEANIMATE(TMP_Text buttonText)
    {

        animate = !animate;

        if (animate)
        {
            if(buttonText) buttonText.text = Translation_Manager.GetTranslation(AnimOnID);
            EnableAnimationEditor();
        }
        else
        {
            if (buttonText) buttonText.text = Translation_Manager.GetTranslation(AnimOffID);
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
