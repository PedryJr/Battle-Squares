using System;
using UnityEngine;

public sealed class SelectMapButtonBehaviour : MonoBehaviour
{
    #region BS2
    public LevelPrep levelPrep = null;
    public bool legacy = true;
    #endregion

    [NonSerialized]
    public int mapId;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    public void SELECT()
    {

        playerSynchronizer.localSquare.selectedLegacyMap = legacy;

        MapStreamSynchronizer.SetGlobalLevelPrep(levelPrep);
        playerSynchronizer.UpdateSelectedMap(mapId, legacy);
/*        if (legacy)
        {
            playerSynchronizer.UpdateSelectedMap(mapId, legacy);
            MapStreamSynchronizer.SetGlobalLevelPrep(null);
        }
        else MapStreamSynchronizer.SetGlobalLevelPrep(levelPrep);*/
    }

}
