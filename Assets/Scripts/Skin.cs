using System.IO;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

public sealed class Skin : NetworkBehaviour
{

    string skinsPath;

    PlayerSynchronizer playerSynchronizer;

    public void Init()
    {

        playerSynchronizer = GetComponent<PlayerSynchronizer>();
        skinsPath = Path.Combine(SaveManager.skinsPath, "skins.json");

        if (!File.Exists(skinsPath))
        {
            Debug.LogWarning($"Skins file not found at '{skinsPath}'. Reverting to default skin.");
            InitializeDefaultSkin();
            return;
        }

        try
        {

            string json = File.ReadAllText(skinsPath);
            SkinData skinData = JsonConvert.DeserializeObject<SkinData>(json);

            if (skinData == null)
            {
                Debug.LogWarning($"Failed to deserialize '{skinsPath}' — JSON returned null. Reverting to default skin.");
                InitializeDefaultSkin();
                return;
            }

            if (skinData.skinFrames == null || skinData.skinFrames.Length == 0)
            {
                Debug.LogWarning($"Skin data invalid: 'skinFrames' is null or empty in '{skinsPath}'. Reverting to default skin.");
                InitializeDefaultSkin();
                return;
            }

            var firstFrame = skinData.skinFrames[0];

            if (firstFrame.frame == null)
            {
                Debug.LogWarning($"Skin data invalid: first skin frame 'frame' is null in '{skinsPath}'. Reverting to default skin.");
                InitializeDefaultSkin();
                return;
            }

            if (firstFrame.frame.Length != 116)
            {
                Debug.LogWarning($"Skin frame size mismatch in '{skinsPath}': expected 116 but got {firstFrame.frame.Length}. Reverting to default skin.");
                InitializeDefaultSkin();
                return;
            }

            playerSynchronizer.skinData = skinData;
            Debug.Log($"Loaded skin from '{skinsPath}'.");

        }
        catch (JsonException jex)
        {
            Debug.LogWarning($"Error parsing skins file '{skinsPath}': {jex.Message}. Reverting to default skin.");
            InitializeDefaultSkin();
        }
        catch (IOException ioex)
        {
            Debug.LogWarning($"I/O error reading skins file '{skinsPath}': {ioex.Message}. Reverting to default skin.");
            InitializeDefaultSkin();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Unexpected error loading skins file '{skinsPath}': {ex.Message}. Reverting to default skin.");
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

        Debug.Log("Initialized default skin.");
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