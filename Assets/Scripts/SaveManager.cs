using UnityEngine;
using System.IO;

public sealed class SaveManager : MonoBehaviour
{

    [SerializeField]
    GameVersion gameVersion;
    Skin skin;

    public static string saveFolderPath;

    private void Awake()
    {

        skin = GetComponent<Skin>();
        CreateVersionedSaveFolder();
        MySettings.Init();
        skin.Init();

    }

    private void CreateVersionedSaveFolder()
    {
        string saveRoot = Path.Combine(Application.persistentDataPath, "Saves");

        saveFolderPath = Path.Combine(saveRoot, gameVersion.version);

        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
    }
}
