using UnityEngine;
using UnityEngine.UI;
using static DragAndScrollMod;

public sealed class EditorLevelListing : MonoBehaviour
{

    [SerializeField]
    Image icon;

    DragAndScrollMod _dragMod;

    private void Awake() => _dragMod = FindAnyObjectByType<DragAndScrollMod>();

    public string levelName;
    public void LoadListing(string listing)
    {
        levelName = listing;
        Debug.Log($"Listing level: {levelName}");
        icon.sprite = LevelFilePaths.LoadLevelIcon(levelName);
    }

    public void LOAD_LEVEL()
    {

        Debug.Log($"Loading level: {levelName}");
        MapStorage shapeStorage = new MapStorage(levelName);
        shapeStorage.UseShapeStorage(_dragMod);
        _dragMod.activeLevelName = levelName;

    }

}