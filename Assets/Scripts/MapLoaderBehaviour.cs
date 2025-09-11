using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using static ListPersistendLevels;

public sealed class MapLoaderBehaviour : MonoBehaviour
{

    ScoreManager.Mode lastMode;
    ScoreManager scoreManager;
    MapSynchronizer mapSynchronizer;

    List<GameObject> loadedMaps;

    [SerializeField]
    GameObject mapIconTemplate;

    private void Awake()
    {
        loadedMaps = new List<GameObject>();
        scoreManager = FindAnyObjectByType<ScoreManager>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        lastMode = scoreManager.gameMode;
        int mapType = (int)lastMode;
        LoadNewMaps(mapType);
    }

    private void Update()
    {
        
        if(scoreManager.gameMode != lastMode)
        {
            lastMode = scoreManager.gameMode;
            int mapType = (int) lastMode;
            LoadNewMaps(mapType);
        }

    }

    void LoadNewMaps(int mapType)
    {

        while(loadedMaps.Count > 0)
        {
            Destroy(loadedMaps[0]);
            loadedMaps.RemoveAt(0);
        }

        for(int mapId = 0; mapId < mapSynchronizer.mapTypes[mapType].maps.Length; mapId++)
        {

            LoadMapImage_LEGACY(mapSynchronizer.mapTypes[mapType].maps[mapId].icon, mapId);

        }

        LevelPathPointer levelPathPointer = new LevelPathPointer();
        levelPathPointer.LoadPaths();
        foreach (var item in levelPathPointer.indexes) LoadMapImage(item);

    }

    void LoadMapImage_LEGACY(Sprite sprite, int mapId)
    {
        GameObject newIcon = Instantiate(mapIconTemplate, transform);
        Image icon = newIcon.GetComponent<Image>();
        SelectMapButtonBehaviour mapButton = newIcon.GetComponent<SelectMapButtonBehaviour>();
        mapButton.legacy = true;

        icon.sprite = sprite;
        mapButton.mapId = mapId;

        loadedMaps.Add(newIcon);
    }

    void LoadMapImage(string levelName)
    {
        LevelPrep levelPrep = new LevelPrep();
        GameObject newIcon = Instantiate(mapIconTemplate, transform);
        Image icon = newIcon.GetComponent<Image>();
        SelectMapButtonBehaviour mapButton = newIcon.GetComponent<SelectMapButtonBehaviour>();
        mapButton.levelPrep = levelPrep;

        mapButton.levelPrep.levelName = levelName;
        mapButton.legacy = false;
        mapButton.mapId = -1;

        mapButton.levelPrep.LoadCompiledLeved(levelName);

        icon.preserveAspect = true;
        icon.sprite = mapButton.levelPrep.RasterizeLevel();

        loadedMaps.Add(newIcon);
    }

}
