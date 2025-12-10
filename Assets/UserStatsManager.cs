using System.IO;
using UnityEngine;

public class UserStatsManager
{

    public static int wins = 0;
    public static int maxWinStreak = 0;
    public static int maxLobbyKills = 0;
    public static int kills = 0;
    public static int deaths = 0;

    private static string statsFilePath;

    public static void Init()
    {
        statsFilePath = Path.Combine(SaveManager.saveFolderPath, "stats.json");
        LoadStats();
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

    public class UserStatsData
    {
        public int wins = 0;
        public int maxWinStreak = 0;
        public int maxLobbyKills = 0;
        public int kills;
        public int deaths;
    }
}