using UnityEngine;
using System.IO;

public sealed class SaveManager : MonoBehaviour
{
    [SerializeField] private string gameVersion = "1.6.0";

    public static SaveManager Instance { get; private set; }

    public static string saveFolderPath { get; private set; }
    public static string smallValuesPath { get; private set; }
    public static string skinsPath { get; private set; }
    public static string levelsPath { get; private set; }
    public static string modsPath { get; private set; }

    private Skin skin;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializePaths();

        skin = GetComponent<Skin>();
        skin.Init();
    }

    private void Start()
    {
        MySettings.Init();
        UserStatsManager.Init();
    }

    public static void PrematureInit()
    {
        saveFolderPath = Path.Combine(
            Application.persistentDataPath,
            "Saves",
            "0.0.0"
        );

        Directory.CreateDirectory(saveFolderPath);

        smallValuesPath = CreateSubFolder("SmallValues");
        skinsPath = CreateSubFolder("Skins");
        levelsPath = CreateSubFolder("Levels");
        modsPath = CreateSubFolder("Mods");
    }

    private void InitializePaths()
    {
        saveFolderPath = Path.Combine(
            Application.persistentDataPath,
            "Saves",
            gameVersion
        );

        Directory.CreateDirectory(saveFolderPath);

        smallValuesPath = CreateSubFolder("SmallValues");
        skinsPath = CreateSubFolder("Skins");
        levelsPath = CreateSubFolder("Levels");
        modsPath = CreateSubFolder("Mods");
    }

    private static string CreateSubFolder(string name)
    {
        string path = Path.Combine(saveFolderPath, name);
        Directory.CreateDirectory(path);
        return path;
    }
}
