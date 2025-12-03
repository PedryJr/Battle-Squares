using System.IO;
using System.IO.Compression;
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

        LoadAllMods();
    }

    async void LoadAllMods()
    {

        ProjectileManager projectileManager = FindAnyObjectByType<ProjectileManager>();

        string modPath = Path.Combine(Application.dataPath, "../Mods");
        if (!Directory.Exists(modPath))
            Directory.CreateDirectory(modPath);

        string[] mods = Directory.GetFiles(modPath, "*.bsm");

        foreach (string modFile in mods)
        {
            Debug.Log("Loading mod: " + modFile);

            // Extract into temp folder
            string tempFolder = Path.Combine(Application.temporaryCachePath,
                                             "Mod_" + Path.GetFileNameWithoutExtension(modFile));

            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);

            Directory.CreateDirectory(tempFolder);

            ZipFile.ExtractToDirectory(modFile, tempFolder);

            // Find catalog
            string catalogJson = Path.Combine(tempFolder, "catalog.json");

            if (!File.Exists(catalogJson))
            {
                Debug.LogError("Mod missing catalog.json: " + modFile);
                continue;
            }

            // Load catalog
            IResourceLocator locator =
                await Addressables.LoadContentCatalogAsync(catalogJson).Task;

            Debug.Log("Loaded mod catalog: " + locator.LocatorId);

            // Load the weapon using address = ScriptableObject.name
            string weaponAddress = Path.GetFileNameWithoutExtension(modFile);

            var handle = Addressables.LoadAssetAsync<WeaponBuilder>(weaponAddress);
            WeaponBuilder weapon = await handle.Task;

            Debug.Log("Loaded mod weapon: " + weapon.WeaponName);

            // Add to global weapon dictionary
            projectileManager.weapons[weapon.typeID] = weapon;
        }
    }
}
