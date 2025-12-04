using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

public class AssetResources : MonoBehaviour
{

    [SerializeField] Sprite smallCornerOctagon;
    public static Sprite GetSmallCornerOctagon => Instance.smallCornerOctagon;

    [SerializeField] PowerDotBehaviour powerDot;
    public static PowerDotBehaviour PowerDot => Instance.powerDot;

    [SerializeField] SpawnEventHandle spawnEventHandle;
    public static SpawnEventHandle SpawnEventHandle => Instance.spawnEventHandle;

    public static AssetResources Instance 
    { 
        get; 
        private set; 
    }

    private void Awake()
    {
        Instance = this;

        //LoadAllMods();
    }

    async void LoadAllMods()
    {

        ProjectileManager projectileManager = FindAnyObjectByType<ProjectileManager>();

        string modPath = Path.Combine(Application.dataPath, "../Mods");
        if (!Directory.Exists(modPath)) Directory.CreateDirectory(modPath);

        string[] mods = Directory.GetFiles(modPath, "*.bsm");

        foreach (string modFile in mods)
        {
            WeaponBuilder builder = LoadWeaponBuilderFromBundle(modFile);
            projectileManager.weapons[builder.typeID] = builder;
        }
    }

    private WeaponBuilder LoadWeaponBuilderFromBundle(string bundlePath)
    {
        if (!File.Exists(bundlePath))
        {
            Debug.LogError($"Bundle not found: {bundlePath}");
            return null;
        }

        AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            Debug.LogError("Failed to load AssetBundle.");
            return null;
        }

        WeaponBuilder loadedAsset = bundle.LoadAllAssets<WeaponBuilder>().FirstOrDefault();
        bundle.Unload(false);
        return loadedAsset;
    }
}
