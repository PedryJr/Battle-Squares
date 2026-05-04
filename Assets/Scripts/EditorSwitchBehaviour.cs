using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EditorSwitchBehaviour : MonoBehaviour
{
    [SerializeField]
    string LevelEditorSceneName;


    [SerializeField]
    Canvas mainCanvas;

    [SerializeField]
    Canvas editorCanvas;
    bool editorCanvasOn = false;

    [SerializeField]
    Canvas workshopBrowseCanvas;
    bool workshopBrowseCanvasOn = false;

    [SerializeField]
    Canvas workshopUploadCanvas;
    bool workshopUploadCanvasOn = false;

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

    public void TOGGLEWORKSHOP_BROWSE()
    {
        workshopBrowseCanvasOn = !workshopBrowseCanvasOn;

        if (workshopBrowseCanvasOn)
        {
            workshopBrowseCanvas.gameObject.SetActive(true);
            mainCanvas.gameObject.SetActive(false);
        }
        else
        {
            workshopBrowseCanvas.GetComponentInChildren<WorkshopLoader>().DelistItems();
            workshopBrowseCanvas.gameObject.SetActive(false);
            mainCanvas.gameObject.SetActive(true);
        }
    }

    public void TOGGLEWORKSHOP_UPLOAD()
    {
        workshopUploadCanvasOn = !workshopUploadCanvasOn;

        if (workshopUploadCanvasOn)
        {
            workshopUploadCanvas.gameObject.SetActive(true);
            mainCanvas.gameObject.SetActive(false);
        }
        else
        {
            workshopUploadCanvas.gameObject.SetActive(false);
            mainCanvas.gameObject.SetActive(true);
        }
    }

    [Serializable]
    public enum Variant : byte
    {
        Skin,
        Level
    }

}
