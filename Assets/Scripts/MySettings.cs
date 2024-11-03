using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public sealed class MySettings : MonoBehaviour
{

    public static float volume = 0.7f;
    public static int vsync = 0;
    public static int fps = 0;
    public static int fullscreen = 0;
    public static bool postProcessing = true;

    public static int wins = 0;
    public static int maxWinStreak = 0;
    public static int maxLobbyKills = 0;
    public static int kills = 0;
    public static int deaths = 0;

    private static string settingsFilePath;
    private static string statsFilePath;

    public static void Init()
    {
        settingsFilePath = Path.Combine(SaveManager.saveFolderPath, "settings.json");
        statsFilePath = Path.Combine(SaveManager.saveFolderPath, "stats.json");
        LoadSettings();
        LoadStats();
    }

    public static void SaveSettings()
    {
        SettingsData data = new SettingsData
        {
            volume = volume,
            vsync = vsync,
            fps = fps,
            fullscreen = fullscreen,
            postProcessing = postProcessing,
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(settingsFilePath, json);
    }

    public static void SaveStats()
    {

        UserStatsData data = new UserStatsData
        {
            wins = wins,
            maxWinStreak = maxWinStreak,
            maxLobbyKills = maxLobbyKills,
            kills = kills,
            deaths = deaths,
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(statsFilePath, json);

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

    public static void LoadStats()
    {

        if (File.Exists(statsFilePath))
        {

            string json = File.ReadAllText(statsFilePath);
            UserStatsData data = JsonUtility.FromJson<UserStatsData>(json);

            wins = data.wins;
            maxWinStreak = data.maxWinStreak;
            maxLobbyKills = data.maxLobbyKills;
            kills = data.kills;
            deaths = data.deaths;

        }

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

public class UserStatsData
{

    public int wins = 0;
    public int maxWinStreak = 0;
    public int maxLobbyKills = 0;
    public int kills;
    public int deaths;

}
