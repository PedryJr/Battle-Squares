using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SelectedMapDisplayer : MonoBehaviour
{

    ScoreManager scoreManager;
    MapSynchronizer mapSynchronizer;
    PlayerSynchronizer playerSynchronizer;
    MapStreamSynchronizer streamSynchronizer;

    [SerializeField]
    Sprite loadingSprite;

    [SerializeField]
    Image image;

    [SerializeField]
    TMP_Text arenaName;

    Color imageColor;
     
    private void Awake()
    {
        scoreManager = FindAnyObjectByType<ScoreManager>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        streamSynchronizer = FindAnyObjectByType<MapStreamSynchronizer>();
        imageColor = image.color;
    }

    private void Update()
    {
        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        int mapType = (int)scoreManager.gameMode;

        if (playerSynchronizer.localSquare.selectedLegacyMap) ShowCurrentMap_LEGACY(mapType);
        else ShowCurrentMap();
    }

    void ShowCurrentMap()
    {

        Sprite mapSprite;
        string mapName;

        if (playerSynchronizer.IsHost)
        {
            mapSprite = streamSynchronizer.levelPrep.RasterizeLevel();
            mapName = streamSynchronizer.levelPrep.levelName;
        }
        else
        {

            if (!streamSynchronizer.levelReciever.loadingCompleted)
            {

                mapSprite = loadingSprite;
                mapName = "Loading...";
            }
            else
            {
                mapSprite = streamSynchronizer.levelReciever.RasterizeLevel();
                mapName = streamSynchronizer.levelReciever.levelName;
            }
        }

        if (mapSprite == null)
        {
            mapSprite = loadingSprite;
            mapName = "Loading failed!";
        }

        LoadMapImage(mapSprite);
        LoadMapName(mapName);
    }

    void ShowCurrentMap_LEGACY(int mapType)
    {

        for (int mapId = 0; mapId < mapSynchronizer.mapTypes[mapType].maps.Length; mapId++)
        {

            if(mapId == playerSynchronizer.localSquare.selectedMap)
            {

                LoadMapImage(mapSynchronizer.mapTypes[mapType].maps[mapId].icon);
                LoadMapName(mapSynchronizer.mapTypes[mapType].maps[mapId].arenaName);

            }

        }

    }

    void LoadMapImage(Sprite sprite)
    {
        if(sprite) image.sprite = sprite;
        image.color = imageColor;
    }

    void LoadMapName(string name)
    {
        arenaName.text = name;
    }

}
