using UnityEngine;
using UnityEngine.UI;
using static DragAndScrollMod;

public sealed class EditorLevelListing : MonoBehaviour
{

    [SerializeField]
    public Image icon;

    DragAndScrollMod _dragMod;

    ListPersistendLevels lister;

    private void Awake() => _dragMod = FindAnyObjectByType<DragAndScrollMod>();

    public string levelName;
    public void LoadListing(string listing, ListPersistendLevels lister)
    {
        this.lister = lister;
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

    public void DELETE_LEVEL()
    {
        this.lister.Delist(this);
        LevelFilePaths.DeleteLevel(levelName);
    }

}