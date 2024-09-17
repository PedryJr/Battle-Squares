using UnityEngine;
using UnityEngine.UI;

public class SelectedMapDisplayer : MonoBehaviour
{

    ScoreManager scoreManager;
    MapSynchronizer mapSynchronizer;
    PlayerSynchronizer playerSynchronizer;

    [SerializeField]
    Image image;

    private void Awake()
    {
        
        scoreManager = FindAnyObjectByType<ScoreManager>();
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Update()
    {

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

            }

        }

    }

    void LoadMapImage(Sprite sprite)
    {

        image.sprite = sprite;

    }

}
