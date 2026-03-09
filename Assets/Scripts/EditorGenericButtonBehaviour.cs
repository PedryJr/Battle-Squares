using UnityEngine;

public class EditorGenericButtonBehaviour : MonoBehaviour
{
    DragAndScrollMod _dragMod;
    Swapper _swapper;
    void Start()
    {
        _swapper = FindAnyObjectByType<Swapper>();
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();
    }
    
    public void SAVE()
    {
        _dragMod.SaveCommand();
    }

    public void EXIT()
    {
        _swapper.RunSwapper();
    }

}
