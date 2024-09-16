using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapLoaderBehaviour : MonoBehaviour
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

            LoadMapImage(mapSynchronizer.mapTypes[mapType].maps[mapId].icon, mapId);

        }

    }

    void LoadMapImage(Sprite sprite, int mapId)
    {
        GameObject newIcon = Instantiate(mapIconTemplate, transform);
        Image icon = newIcon.GetComponent<Image>();
        SelectMapButtonBehaviour mapButton = newIcon.GetComponent<SelectMapButtonBehaviour>();

        icon.sprite = sprite;
        mapButton.mapId = mapId;

        loadedMaps.Add(newIcon);
    }

}
