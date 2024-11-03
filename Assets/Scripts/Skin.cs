using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;

public class Skin : NetworkBehaviour
{

    string skinsPath;

    PlayerSynchronizer playerSynchronizer;

    public void Init()
    {

        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        skinsPath = Path.Combine(SaveManager.saveFolderPath, "skins.json");

        if (File.Exists(skinsPath))
        {

            string json = File.ReadAllText(skinsPath);
            SkinData skinData = JsonConvert.DeserializeObject<SkinData>(json);

            if (skinData != null)
            {

                if (skinData.skinFrames[0].frame.Length == 116)
                {

                    playerSynchronizer.skinData = skinData;

                }
                else
                {
                    InitializeDefaultSkin();
                }

            }
            else
            {
                InitializeDefaultSkin();
            }

        }
        else
        {
            InitializeDefaultSkin();
        }

    }

    private void InitializeDefaultSkin()
    {

        playerSynchronizer.skinData = new SkinData();

        for (int i = 0; i < playerSynchronizer.skinData.skinFrames[0].frame.Length; i++)
        {
            playerSynchronizer.skinData.skinFrames[0].frame[i] = true;
        }

        playerSynchronizer.skinData.animate = false;
        playerSynchronizer.skinData.frames = 1;
        playerSynchronizer.skinData.skinFrames[0].valid = true;

        SaveSkinData(playerSynchronizer.skinData);

    }

    public void SaveSkinData(SkinData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(skinsPath, json);
    }

    public void SaveSingleSkin()
    {
        SaveSkinData(playerSynchronizer.skinData);
    }

    private void OnApplicationQuit()
    {
        SaveSingleSkin();
    }
}

public class SkinData
{

    public bool animate;
    public float frameRate = 10;
    public SkinFrame[] skinFrames;
    public int frames;

    public SkinData()
    {
        skinFrames = new SkinFrame[1];
        skinFrames[0].frame = new bool[116];
    }

    public struct SkinFrame
    {
        public bool valid;
        public bool[] frame;
    }

}