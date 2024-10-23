using System;
using UnityEngine;
using static ScoreManager;

public sealed class MapInitiator : MonoBehaviour
{

    MapTypes[] mapTypes;

    MapSynchronizer mapSynchronizer;

    public MapBehaviour activeMap;

    public void InitPresetMap(int index, Mode activeMode)
    {

        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();
        mapTypes = mapSynchronizer.mapTypes;

        switch (activeMode)
        {
            case Mode.DM: break;
            case Mode.DT: break;
            case Mode.CTF: break;
        }

        activeMap = activeMode == Mode.DM ? InitDM(index)
            : activeMode == Mode.DT ? InitDT(index)
            : activeMode == Mode.CTF ? InitCTF(index)
            : null;
        activeMap.gameObject.SetActive(true);

    }

    MapBehaviour InitDM(int index)
    {

        FindAnyObjectByType<CameraAnimator>().PlayTheme(mapTypes[0].maps[index].battleThemeReference);
        return Instantiate(mapTypes[0].maps[index]);

    }

    MapBehaviour InitDT(int index)
    {

        FindAnyObjectByType<CameraAnimator>().PlayTheme(mapTypes[1].maps[index].battleThemeReference);
        return Instantiate(mapTypes[1].maps[index]);

    }

    MapBehaviour InitCTF(int index)
    {

        FindAnyObjectByType<CameraAnimator>().PlayTheme(mapTypes[2].maps[index].battleThemeReference);
        return Instantiate(mapTypes[2].maps[index]);

    }

}

[Serializable]
public struct MapTypes
{

    public MapBehaviour[] maps;

}
