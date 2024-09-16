using System;
using UnityEngine;
using static ScoreManager;

public class MapInitiator : MonoBehaviour
{

    [SerializeField]
    MapTypes[] mapTypes;

    public MapBehaviour activeMap;

    public void InitPresetMap(int index, Mode activeMode)
    {

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
        return Instantiate(mapTypes[0].maps[index]);
    }

    MapBehaviour InitDT(int index)
    {
        return Instantiate(mapTypes[1].maps[index]);
    }

    MapBehaviour InitCTF(int index)
    {
        return Instantiate(mapTypes[2].maps[index]);
    }

}

[Serializable]
public struct MapTypes
{

    public MapBehaviour[] maps;

}
