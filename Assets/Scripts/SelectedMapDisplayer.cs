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
    Image image;

    [SerializeField]
    TMP_Text arenaName;
     
    private void Awake()
    {
        scoreManager = FindAnyObjectByType<ScoreManager>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        streamSynchronizer = FindAnyObjectByType<MapStreamSynchronizer>();
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
        if(playerSynchronizer.IsHost)
        {
            LoadMapImage(streamSynchronizer.levelPrep.RasterizeLevel());
            LoadMapName(streamSynchronizer.levelPrep.levelName);
        }
        else
        {
            LoadMapImage(streamSynchronizer.levelReciever.RasterizeLevel());
            LoadMapName(streamSynchronizer.levelReciever.levelName);
        }
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
    }

    void LoadMapName(string name)
    {
        arenaName.text = name;
    }

}
