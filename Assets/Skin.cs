using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Unity.Netcode;

public class Skin : NetworkBehaviour
{
    string skinsPath;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        skinsPath = Path.Combine(Application.persistentDataPath, "skins.json");

        // Load skin data from file
        if (File.Exists(skinsPath))
        {
            string json = File.ReadAllText(skinsPath);
            bool[] data = JsonConvert.DeserializeObject<bool[]>(json);

            // Ensure we don't exceed the skin array length
            if (data.Length == 116)
            {
                playerSynchronizer.skin = (bool[])data.Clone();
            }
            else
            {
                Debug.LogWarning("Loaded skin data length does not match expected length. Initializing with default.");
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
        playerSynchronizer.skin = new bool[116];
        for (int i = 0; i < playerSynchronizer.skin.Length; i++)
        {
            playerSynchronizer.skin[i] = true;
        }

        // Save the default skin data
        SaveSkinData(playerSynchronizer.skin);
    }

    public void SaveSkinData(bool[] data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(skinsPath, json);
    }

    public void SaveSingleSkin()
    {
        SaveSkinData(playerSynchronizer.skin);
    }

    private void OnApplicationQuit()
    {
        SaveSingleSkin();
    }
}
