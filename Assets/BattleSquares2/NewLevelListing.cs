using UnityEngine;
using static DragAndScrollMod;

public sealed class NewLevelListing : MonoBehaviour
{

    static NewLevelCreator currentLevelCreator = null;

    [SerializeField]
    Transform newLevelCreatorParent;

    [SerializeField]
    NewLevelCreator newLevelNamerAndCreator;

    DragAndScrollMod _dragMod;

    private void Awake()
    {
        currentLevelCreator = null;
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();
    }

    public void NEW_LEVEL()
    {
        if (currentLevelCreator) return;
        currentLevelCreator = Instantiate(newLevelNamerAndCreator, newLevelCreatorParent);

        if (_dragMod.activeLevelName == "New Level")
        {
            currentLevelCreator.BeforeClearFunc(BeforeClearFunc);
            currentLevelCreator.AfterClearFunc(AfterClearund);
        }

    }

    void BeforeClearFunc(string levelName)
    {
        MapStorage shapeStorage = new MapStorage(levelName, _dragMod);
    }

    void AfterClearund(string levelName)
    {
        MapStorage shapeStorage = new MapStorage(levelName);
        shapeStorage.UseShapeStorage(_dragMod);
    }

    public static void AllowNewCration() => currentLevelCreator = null;

}
