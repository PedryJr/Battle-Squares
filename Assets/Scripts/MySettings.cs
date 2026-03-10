using System.IO;
using UnityEngine;

public sealed class MySettings
{ 
    public static float Volume { get; private set; } = 0.7f;
    public static int Vsync { get; private set; } = 0;
    public static int Fps { get; private set; } = 0;
    public static int FullscreenMode { get; private set; } = 0;
    public static bool PostProcessing { get; private set; } = true;
    public static bool Muted { get; private set; } = true;
     
    private static string settingsFilePath;
    private static bool initialized = false;

    public static void Init()
    {
        if (initialized) return;
        initialized = true;

        settingsFilePath = Path.Combine(SaveManager.saveFolderPath, "settings.json");
        LoadSettings();
        ApplySettings(true);
    }
     

    public static void SetVolume(float v)
    {
        Volume = Mathf.Clamp01(v);
        OnChanged();
    }

    public static void SetVsync(int v)
    {
        Vsync = Mathf.Clamp(v, 0, 4);
        OnChanged();
    }

    public static void SetFps(int v)
    {
        Fps = Mathf.Clamp(v, 0, 4);
        OnChanged();
    }

    public static void SetFullscreen(int mode)
    {
        FullscreenMode = Mathf.Clamp(mode, 0, 1);
        OnChanged();
    }

    public static void SetPostProcessing(bool enabled)
    {
        PostProcessing = enabled;
        OnChanged();
    }

    public static void SetMuted(bool enabled)
    {
        Muted = enabled;
        OnChanged();
    }
     
    private static void OnChanged()
    {
        ApplySettings();
        SaveSettings();
    }

    public static void ApplyHighestResolutionAndRefreshRate()
    {
        /*        Resolution bestResolution = new Resolution();
                RefreshRate highestRefreshRate = new RefreshRate();

                // Get all available resolutions
                Resolution[] resolutions = Screen.resolutions;

                // Find the resolution with the highest width, height, and refresh rate
                foreach (Resolution resolution in resolutions)
                {
                    // Calculate actual refresh rate values for comparison
                    double currentRefreshRate = (double)resolution.refreshRateRatio.numerator / resolution.refreshRateRatio.denominator;
                    double bestRefreshRate = highestRefreshRate.denominator > 0
                        ? (double)highestRefreshRate.numerator / highestRefreshRate.denominator
                        : 0;

                    // Check if this resolution is better than our current best
                    if (resolution.width > bestResolution.width ||
                        (resolution.width == bestResolution.width && resolution.height > bestResolution.height) ||
                        (resolution.width == bestResolution.width && resolution.height == bestResolution.height && currentRefreshRate > bestRefreshRate))
                    {
                        bestResolution = resolution;
                        highestRefreshRate = resolution.refreshRateRatio;
                    }
                }*/

        /*        // Apply the best resolution found
                Screen.SetResolution(bestResolution.width, bestResolution.height, FullScreenMode.FullScreenWindow);

                double finalRefreshRate = (double)highestRefreshRate.numerator / highestRefreshRate.denominator;
                Debug.Log($"Applied resolution: {bestResolution.width}x{bestResolution.height} @ {finalRefreshRate:F2}Hz");*/

        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }

    private static void ApplySettings(bool forceApply = false)
    { 
        switch (FullscreenMode)
        {
            case 0:
            {
                    ApplyHighestResolutionAndRefreshRate();
                    break;
            }
            case 1:
            {
                    Screen.fullScreenMode = FullScreenMode.Windowed; 
                    break;
            }
        }
         
        QualitySettings.vSyncCount = Vsync;
         
        switch (Fps)
        {
            case 0: Application.targetFrameRate = -1; break;
            case 1: Application.targetFrameRate = 30; break;
            case 2: Application.targetFrameRate = 60; break;
            case 3: Application.targetFrameRate = 144; break;
            case 4: Application.targetFrameRate = 240; break;
        }
    }
     
    private static void SaveSettings()
    {
        SettingsData data = new SettingsData
        {
            volume = Volume,
            vsync = Vsync,
            fps = Fps,
            fullscreen = FullscreenMode,
            postProcessing = PostProcessing,
            muted = Muted
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(settingsFilePath, json);
    }

    private static void LoadSettings()
    {
        if (!File.Exists(settingsFilePath))
        {
            ApplySettings();
            SaveSettings();
            return;
        }

        string json = File.ReadAllText(settingsFilePath);
        SettingsData data = JsonUtility.FromJson<SettingsData>(json);

        Volume = data.volume;
        Vsync = data.vsync;
        Fps = data.fps;
        FullscreenMode = data.fullscreen;
        PostProcessing = data.postProcessing;
        Muted = data.muted;

        ApplySettings();
    }
}

public class SettingsData
{
    public float volume = 1;
    public int vsync = 0;
    public int fps = 0;
    public int fullscreen = 0;
    public bool postProcessing = true;
    public bool muted = false;
}
