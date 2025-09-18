using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using UnityEngine;

public sealed class ListPersistendLevels : MonoBehaviour
{

    [SerializeField]
    float destroyListedElementsInTime;

    TabModeObject meAsTabMode;

    [SerializeField]
    Transform levelListingParent;

    List<EditorLevelListing> listings;

    [SerializeField]
    EditorLevelListing prefabInstance;

    public static LevelPathPointer levelPathPointer;


    private void Awake()
    {
        meAsTabMode = GetComponent<TabModeObject>();
        meAsTabMode.tabModeEnabled = CustomEnable;
        meAsTabMode.tabModeDisabled = CustomDisable;
        listings = new List<EditorLevelListing>();
        levelPathPointer = new LevelPathPointer();
        levelPathPointer.LoadPaths();
    }

    public void CustomEnable()
    {
        Delist();
        Debug.Log("Custom enable running enlist!");
        levelPathPointer.LoadPaths();
        Enlist();
    }

    public void CustomDisable()
    {
        Debug.Log("Custom enable running delist!");
        Delist();
    }

    private void Delist()
    {
        for (int i = 0; i < listings.Count; i++)
        {
            ShrinkAndDestroy instance = listings[i].gameObject.AddComponent<ShrinkAndDestroy>();
            instance.timeToDestroy = destroyListedElementsInTime;
        }
        listings.Clear();
    }

    void Enlist()
    {

        levelPathPointer.LoadPaths();
        if (levelPathPointer.indexes != null) foreach (string index in levelPathPointer.indexes) HandlePreviewIndex(index);
    }

    void HandlePreviewIndex(string index)
    {
        EditorLevelListing levelListing = Instantiate(prefabInstance, levelListingParent);
        listings.Add(levelListing);
        levelListing.LoadListing(index);
    }

    public class LevelPathPointer
    {
        public string[] indexes = null;

        public void SavePaths()
        {
            string indexContent = JsonConvert.SerializeObject(this);
            LevelFilePaths.StoreIndex(indexContent);
        }

        public void LoadPaths()
        {
            string indexContentAsJson = LevelFilePaths.LoadIndex();
            if(indexContentAsJson == string.Empty) indexes = null;
            else indexes = JsonConvert.DeserializeObject<LevelPathPointer>(indexContentAsJson).indexes;
        }

        public void EnsurePath(string additionalPath)
        {

            if (indexes == null) CreatePathsPointer(additionalPath);
            else
            {
                List<string> pathsAsList = indexes.ToList();
                if (pathsAsList.Contains(additionalPath)) return;
                pathsAsList.Add(additionalPath);
                indexes = pathsAsList.ToArray();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CreatePathsPointer(string firstPath) => indexes = new string[1] { firstPath };
    }
}

public static class LevelFilePaths
{
    public static string LoadIndex()
    {
        EnsureIndexDirectory();
        if(!File.Exists(GetLevelsPointerFilePath())) return string.Empty;
        else return File.ReadAllText(GetLevelsPointerFilePath());
    }
    public static void StoreIndex(string indexContent)
    {
        EnsureIndexDirectory();
        File.WriteAllText(GetLevelsPointerFilePath(), indexContent);
    }

    public static string LoadLevel(string levelName)
    {
        EnsureIndexDirectory();
        EnsureLevelDirectory(levelName);
        if (!File.Exists(GetLevelsPointerFilePath())) return string.Empty;
        else return File.ReadAllText(GetLevelJsonFilePath(levelName));
    }

    public static void StoreLevel(string levelName, string levelAsJson)
    {
        ListPersistendLevels.levelPathPointer.EnsurePath(levelName);
        EnsureIndexDirectory();
        EnsureLevelDirectory(levelName);
        File.WriteAllText(GetLevelJsonFilePath(levelName), levelAsJson);
        StoreLevelIcon(levelName);
        ListPersistendLevels.levelPathPointer.SavePaths();
    }

    public static LevelPrep LoadCompiledLevel(string levelName, JsonSerializerSettings settings)
    {
        EnsureIndexDirectory();
        EnsureLevelDirectory(levelName);
        if (!File.Exists(GetLevelsPointerFilePath())) return null;
        else return JsonConvert.DeserializeObject<LevelPrep>(File.ReadAllText(GetLevelCompiledJsonFilePath(levelName)), settings);
    }

    public static void StoreCompiledLevel(LevelPrep level, JsonSerializerSettings settings)
    {
        string levelName = level.levelName;
        ListPersistendLevels.levelPathPointer.EnsurePath(levelName);
        EnsureIndexDirectory();
        EnsureLevelDirectory(levelName);
        File.WriteAllText(GetLevelCompiledJsonFilePath(levelName), JsonConvert.SerializeObject(level, settings));
        ListPersistendLevels.levelPathPointer.SavePaths();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureIndexDirectory()
    {
        if (!Directory.Exists(GetLevelsFolderPath())) Directory.CreateDirectory(GetLevelsFolderPath());
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureLevelDirectory(string levelName)
    {
        if (!Directory.Exists(GetLevelNamedFolderPath(levelName))) Directory.CreateDirectory(GetLevelNamedFolderPath(levelName));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelsFolderPath() => Application.dataPath + "/Levels";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelNamedFolderPath(string levelName) => Application.dataPath + "/Levels/" + levelName;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelsPointerFilePath() => GetLevelsFolderPath() + "/Index.bsl";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelJsonFilePath(string levelName) => GetLevelsFolderPath() + $"/{levelName}/data.bsl";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelIconFilePath(string levelName) => GetLevelsFolderPath() + $"/{levelName}/ico.bsl";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelMetadataFilePath(string levelName) => GetLevelsFolderPath() + $"/{levelName}/meta.bsl";
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLevelCompiledJsonFilePath(string levelName) => GetLevelsFolderPath() + $"/{levelName}/compiled.bsl";

    //Store icon function
    //Load icon function

    public static void StoreLevelIcon(string levelName)
    {

        EnsureIndexDirectory();
        EnsureLevelDirectory(levelName);
        ShapeMimicBehaviour[] shapeMimics = GameObject.FindObjectsByType<ShapeMimicBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CombineInstance[] shapes = new CombineInstance[shapeMimics.Length];

        Mesh[] meshesToCleanUp = new Mesh[shapeMimics.Length + 1];
        for (int i = 0; i < shapes.Length; i++)
        {
            shapes[i] = new CombineInstance()
            {
                mesh = shapeMimics[i].GenerateWorldspaceMesh(),
                transform = Matrix4x4.identity,
                subMeshIndex = 0,
                lightmapScaleOffset = Vector4.zero,
                realtimeLightmapScaleOffset = Vector4.zero,
            };
            meshesToCleanUp[i] = shapes[i].mesh;
        }

        Mesh combinedShapes = new Mesh();
        combinedShapes.CombineMeshes(shapes, true, true);
        meshesToCleanUp[shapeMimics.Length] = combinedShapes;

        Vector3[] combinedMesh3DVertices = combinedShapes.vertices;
        float size = 0;
        Vector2[] combinedMesh2DVertices = new Vector2[combinedShapes.vertices.Length];
        for (int i = 0; i < combinedMesh2DVertices.Length; i++)
        {
            combinedMesh2DVertices[i] = combinedMesh3DVertices[i];
            if (combinedMesh2DVertices[i].magnitude > size) size = combinedMesh2DVertices[i].magnitude;
        }

        float pixelDensity = 2f;

        byte[] pixelData = PolygonTriangulator.RasterizeMeshDATA(combinedMesh2DVertices, combinedShapes.triangles, out bool v, out int width, out int height, out float ppu, pixelDensity * (256f / size));

        PixelMetaData pixelMetaData = new PixelMetaData();
        pixelMetaData.h = height;
        pixelMetaData.w = width;
        pixelMetaData.ppu = ppu;
        pixelMetaData.v = v;

        string metadata = JsonConvert.SerializeObject(pixelMetaData);

        File.WriteAllText(GetLevelMetadataFilePath(levelName), metadata);
        File.WriteAllBytes(GetLevelIconFilePath(levelName), pixelData);

        for (int i = meshesToCleanUp.Length - 1; i >= 0; i--) Mesh.Destroy(meshesToCleanUp[i]);
    }

    public static Sprite LoadLevelIcon(string levelName)
    {
        EnsureIndexDirectory();
        EnsureLevelDirectory(levelName);

        PixelMetaData pixelMetaData;
        if (File.Exists(GetLevelMetadataFilePath(levelName))) pixelMetaData = JsonConvert.DeserializeObject<PixelMetaData>(File.ReadAllText(GetLevelMetadataFilePath(levelName)));
        else pixelMetaData = new PixelMetaData() { v = false };

        byte[] data;
        if (pixelMetaData.v) data = File.ReadAllBytes(GetLevelIconFilePath(levelName));
        else
        {
            data = new byte[1] { 0 };
            pixelMetaData.w = 1;
            pixelMetaData.h = 1;
            pixelMetaData.ppu = 1;
        }

        Texture2D tex = new Texture2D(pixelMetaData.w, pixelMetaData.h, TextureFormat.Alpha8, false);
        tex.filterMode = FilterMode.Point;

        tex.SetPixelData(data, 0, 0);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, pixelMetaData.w, pixelMetaData.h), new Vector2(0.5f, 0.5f), pixelMetaData.ppu);
    }

    public class PixelMetaData
    {
        public int w;
        public int h;
        public float ppu;
        public bool v;
    }

}