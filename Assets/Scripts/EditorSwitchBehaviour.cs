using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EditorSwitchBehaviour : MonoBehaviour
{
    [SerializeField]
    string LevelEditorSceneName;

    bool editorCanvasOn = false;

    [SerializeField]
    Canvas mainCanvas;

    [SerializeField]
    Canvas editorCanvas;

    [SerializeField]

    public void TOGGLEEDITOR(Variant variant)
    {
        if(variant == Variant.Skin)
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
        else SceneManager.LoadScene(LevelEditorSceneName);

    }

    [Serializable]
    public enum Variant : byte
    {
        Skin,
        Level
    }

}
