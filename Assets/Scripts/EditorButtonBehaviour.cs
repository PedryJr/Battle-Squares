using UnityEngine;

public class EditorButtonBehaviour : MonoBehaviour
{
    [SerializeField]
    EditorSwitchBehaviour.Variant variant;
    EditorSwitchBehaviour switchBehaviour;

    private void Awake() => switchBehaviour = FindAnyObjectByType<EditorSwitchBehaviour>();

    public void ENTER_EDITOR()
    {
        switchBehaviour.TOGGLEEDITOR(variant);
    }
}
