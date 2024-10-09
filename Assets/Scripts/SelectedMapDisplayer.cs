using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SelectedMapDisplayer : MonoBehaviour
{

    ScoreManager scoreManager;
    MapSynchronizer mapSynchronizer;
    PlayerSynchronizer playerSynchronizer;

    [SerializeField]
    Image image;

    [SerializeField]
    TMP_Text arenaName;

    private void Awake()
    {
        
        scoreManager = FindAnyObjectByType<ScoreManager>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Update()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;

        int mapType = (int)scoreManager.gameMode;
        ShowCurrentMap(mapType);

    }

    void ShowCurrentMap(int mapType)
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

        image.sprite = sprite;

    }

    void LoadMapName(string name)
    {
        arenaName.text = name;
    }

}
