using System;
using UnityEngine;

public class SelectMapButtonBehaviour : MonoBehaviour
{

    [NonSerialized]
    public int mapId;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    public void SELECT()
    {

        playerSynchronizer.UpdateSelectedMap(mapId);

    }

}
