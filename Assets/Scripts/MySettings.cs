using System.IO;
using UnityEngine;

public sealed class MySettings : MonoBehaviour
{

    public static float volume = 0.7f;
    public static int vsync = 0;
    public static int fps = 0;
    public static int fullscreen = 0;
    public static bool postProcessing = true;

    private static string settingsFilePath;

    public static void Init()
    {
        settingsFilePath = Path.Combine(Application.persistentDataPath, "settings.json");
        LoadSettings();
    }

    public static void SaveSettings()
    {
        SettingsData data = new SettingsData
        {
            volume = volume,
            vsync = vsync,
            fps = fps,
            fullscreen = fullscreen,
            postProcessing = postProcessing
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(settingsFilePath, json);
    }

    public static void LoadSettings()
    {
        if (File.Exists(settingsFilePath))
        {
            string json = File.ReadAllText(settingsFilePath);
            SettingsData data = JsonUtility.FromJson<SettingsData>(json);

            volume = data.volume;
            vsync = data.vsync;
            fps = data.fps;
            fullscreen = data.fullscreen;
            postProcessing = data.postProcessing;
        }

        ApplySettings();

    }

    public static void ApplySettings()
    {

        RefreshRate refreshRate = Screen.mainWindowDisplayInfo.refreshRate;

        int x = Screen.mainWindowDisplayInfo.width;
        int y = Screen.mainWindowDisplayInfo.height;

        switch (fullscreen)
        {
            case 0: Screen.SetResolution(x, y, FullScreenMode.ExclusiveFullScreen, refreshRate); break;
            case 1: Screen.SetResolution(x, y, FullScreenMode.FullScreenWindow, refreshRate); break;
            case 2: Screen.SetResolution(x, y, FullScreenMode.Windowed, refreshRate); break;
        }

        QualitySettings.vSyncCount = vsync;
        switch (fps)
        {
            case 0: Application.targetFrameRate = -1; break;
            case 1: Application.targetFrameRate = 30; break;
            case 2: Application.targetFrameRate = 60; break;
            case 3: Application.targetFrameRate = 144; break;
            case 4: Application.targetFrameRate = 240; break;
        }

        SaveSettings();

    }

}


public class SettingsData
{
    public float volume = 1;
    public int vsync = 0;
    public int fps = 0;
    public int fullscreen = 0;
    public bool postProcessing = true;
}
