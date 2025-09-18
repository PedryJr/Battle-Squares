using UnityEngine;

public sealed class ModeChanger : MonoBehaviour
{
    [SerializeField]
    EditorMode mode;

    [SerializeField]
    GameObject popupMessage;

    DragAndScrollMod _dragMod;
    LevelEditorInitializer editorInitializer;
    private void Awake()
    {
        editorInitializer = FindAnyObjectByType<LevelEditorInitializer>();
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();
    }
    public void CHANGE_MODE()
    {
        editorInitializer.SetMode(mode);
        _dragMod.SetTabFlag(false);
        Instantiate(popupMessage, null);
    }

}
