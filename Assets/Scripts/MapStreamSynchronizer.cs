using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static AnimationAnchor;
using static LevelFilePaths;
using static ShapeMimicBehaviour;

public class MapStreamSynchronizer : NetworkBehaviour
{

    public static MapStreamSynchronizer Instance;

    public static void SetGlobalLevelPrep(LevelPrep value)
    {
        if (globalLevelPrep != value) Instance.LevelChangedCallback(value);
        globalLevelPrep = value;
    }

    public static LevelPrep GetGlobalLevelPrep()
    {
        return globalLevelPrep;
    }

    private static LevelPrep globalLevelPrep;

    [SerializeField]
    public LevelPrep levelPrep;
    [SerializeField]
    public LevelReciever levelReciever;

    //public static LevelReciever globalLevelReciever;
    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        Instance = this;
        globalLevelPrep = null;
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
    }


    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void OnJoinMapRequestServerRpc()
    {
        if (!IsHost) return;
        /*if (playerSynchronizer.localSquare.selectedLegacyMap) */LevelChangedCallback(levelPrep);
    }

    //ON HOST
    public void LevelChangedCallback(LevelPrep levelPrep)
    {
        if (!playerSynchronizer.IsHost) return;
        if (levelPrep == null) return;
        this.levelPrep = levelPrep;
        NotifyMapChangeClientRpc(levelPrep.levelExpectation, levelPrep.levelName);
        ApplyMapOnHost();
    }

    void ApplyMapOnHost()
    {
        LevelBuilderStuff.loadedSimplifiedShapeData = new SimplifiedShapeData[levelPrep.simplifiedShapeDataArray.Length];
        for (int i = 0; i < LevelBuilderStuff.loadedSimplifiedShapeData.Length; i++)
        {
            LevelBuilderStuff.loadedSimplifiedShapeData[i] = ConvertFromUSimplifiedShapeData(levelPrep.simplifiedShapeDataArray[i]);
        }
        LevelBuilderStuff.simplifiedAnimationDatas = levelPrep.simplifiedAnimationDataArray;
        LevelBuilderStuff.simplifiedLightData = levelPrep.lightPositions;
        LevelBuilderStuff.simplifiedSpawnData = levelPrep.spawnPositions;
    }


    //ON CLIENT
    [ClientRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable, AllowTargetOverride = true)]
    public void NotifyMapChangeClientRpc(LevelExpectation levelExpectation, string levelName)
    {
        if(levelReciever != null) if (levelReciever.levelExpectation.levelHashCode == levelExpectation.levelHashCode) return;
        if (IsHost) return;
        levelReciever = new LevelReciever(levelExpectation);
        levelReciever.levelName = levelName;
        RequestData();
    }

    public void RequestData()
    {
        if (levelReciever.loadingCompleted) return;
        if (!playerSynchronizer.localSquare) return;
        FetchChunkServerRpc(levelReciever.recievedChunks, playerSynchronizer.localSquare.GetID());
    }

    //ON CLIENT
    [ClientRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable, AllowTargetOverride = true)]
    public void GiveDataClientRpc(LevelDataRange dataRange, USimplifiedShapeData[] simplifiedShapeDatas, ClientRpcParams clientRpcParams = default)
    {
        if (!levelReciever.IsHashcodeValid(dataRange))
        {
            Debug.Log("Hashcode is not valid!");
            return;
        }
        for (int i = 0; i < dataRange.Length; i++)
        {
            levelReciever.simplifiedShapeDataArray[dataRange.start + i] = simplifiedShapeDatas[i];
        }
        levelReciever.recievedChunks.shapeCount += dataRange.Length;
        RequestData();
    }

    //ON CLIENT
    [ClientRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable, AllowTargetOverride = true)]
    public void GiveDataClientRpc(LevelDataRange dataRange, SimplifiedAnimationData[] simplifiedAnimationDatas, ClientRpcParams clientRpcParams = default)
    {
        if (!levelReciever.IsHashcodeValid(dataRange))
        {
            Debug.Log("Hashcode is not valid!");
            return;
        }
        for (int i = 0; i < dataRange.Length; i++)
        {
            levelReciever.simplifiedAnimationDataArray[dataRange.start + i] = simplifiedAnimationDatas[i];
        }
        levelReciever.recievedChunks.animationCount += dataRange.Length;
        RequestData();
    }

    //ON CLIENT
    [ClientRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable, AllowTargetOverride = true)]
    public void GiveDataClientRpc(LevelDataRange dataRange, ByteCoord[] byteCoords, ClientRpcParams clientRpcParams = default)
    {
        if (!levelReciever.IsHashcodeValid(dataRange))
        {
            Debug.Log("Hashcode is not valid!");
            return;
        }
        if (dataRange.levelDataType == LevelDataType.Light)
        {
            for (int i = 0; i < dataRange.Length; i++) levelReciever.lightPositions[dataRange.start + i] = byteCoords[i];
            levelReciever.recievedChunks.lightCount += dataRange.Length;
        }
        else if (dataRange.levelDataType == LevelDataType.Spawn)
        {
            for (int i = 0; i < dataRange.Length; i++) levelReciever.spawnPositions[dataRange.start + i] = byteCoords[i];
            levelReciever.recievedChunks.spawnCount += dataRange.Length;
        }
        RequestData();
    }

    //ON CLIENT
    [ClientRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void AknowledgeClientRpc(LevelDataRange dataRange, ClientRpcParams clientRpcParams = default)
    {
        if (!levelReciever.IsHashcodeValid(dataRange))
        {
            Debug.Log("Hashcode is not valid!");
            return;
        }
        Debug.Log("Loading finished");
        Debug.Log($"{levelReciever.recievedChunks.shapeCount}");
        Debug.Log($"{levelReciever.recievedChunks.animationCount}");
        Debug.Log($"{levelReciever.recievedChunks.lightCount}");
        Debug.Log($"{levelReciever.recievedChunks.spawnCount}");
        levelReciever.loadingCompleted = true;
        LevelBuilderStuff.loadedSimplifiedShapeData = new SimplifiedShapeData[levelReciever.simplifiedShapeDataArray.Length];
        for (int i = 0; i < LevelBuilderStuff.loadedSimplifiedShapeData.Length; i++)
        {
            LevelBuilderStuff.loadedSimplifiedShapeData[i] = ConvertFromUSimplifiedShapeData(levelReciever.simplifiedShapeDataArray[i]);
        }
        LevelBuilderStuff.simplifiedAnimationDatas = levelReciever.simplifiedAnimationDataArray;
        LevelBuilderStuff.simplifiedLightData = levelReciever.lightPositions;
        LevelBuilderStuff.simplifiedSpawnData = levelReciever.spawnPositions;
    }

    //ON HOST
    [ServerRpc(RequireOwnership = false)]
    public void FetchChunkServerRpc(LevelExpectation recievedChunks, byte requester)
    {

        ClientRpcParams clientRpcParams = default;
        clientRpcParams.Send.TargetClientIdsNativeArray = new NativeArray<ulong>(new ulong[] { requester }, Allocator.TempJob);
        clientRpcParams.Receive = default;

        Debug.Log("Fetching chunk");
        LevelDataRange dataRange = new LevelDataRange(levelPrep.levelExpectation);
        if (recievedChunks.shapeCount < levelPrep.levelExpectation.shapeCount)
        {
            Debug.Log("Fetching shapes.");
            Debug.Log($"{levelPrep.levelExpectation.shapeCount - recievedChunks.shapeCount} Shapes remaining.");
            dataRange.start = recievedChunks.shapeCount;
            dataRange.end = (ushort)(Math.Min((ushort)(levelPrep.levelExpectation.shapeCount - recievedChunks.shapeCount), (ushort)50) + recievedChunks.shapeCount);
            dataRange.levelDataType = LevelDataType.Shape;
            GiveDataClientRpc(dataRange, levelPrep.simplifiedShapeDataArray.AsSpan().Slice(dataRange.start, dataRange.Length).ToArray(), clientRpcParams);
        }
        else if (recievedChunks.animationCount < levelPrep.levelExpectation.animationCount)
        {
            Debug.Log("Fetching animations.");
            Debug.Log($"{levelPrep.levelExpectation.animationCount - recievedChunks.animationCount} Animations remaining.");
            dataRange.start = recievedChunks.animationCount;
            dataRange.end = (ushort)(Math.Min((ushort)(levelPrep.levelExpectation.animationCount - recievedChunks.animationCount), (ushort)50) + recievedChunks.animationCount);
            dataRange.levelDataType = LevelDataType.Animation;
            GiveDataClientRpc(dataRange, levelPrep.simplifiedAnimationDataArray.AsSpan().Slice(dataRange.start, dataRange.Length).ToArray(), clientRpcParams);
        }
        else if (recievedChunks.lightCount < levelPrep.levelExpectation.lightCount)
        {
            Debug.Log("Fetching lights.");
            Debug.Log($"{levelPrep.levelExpectation.lightCount - recievedChunks.lightCount} Animations remaining.");
            dataRange.start = recievedChunks.lightCount;
            dataRange.end = (ushort)(Math.Min((ushort)(levelPrep.levelExpectation.lightCount - recievedChunks.lightCount), (ushort)50) + recievedChunks.lightCount);
            dataRange.levelDataType = LevelDataType.Light;
            GiveDataClientRpc(dataRange, levelPrep.lightPositions.AsSpan().Slice(dataRange.start, dataRange.Length).ToArray(), clientRpcParams);
        }
        else if (recievedChunks.spawnCount < levelPrep.levelExpectation.spawnCount)
        {
            Debug.Log("Fetching lights.");
            Debug.Log($"{levelPrep.levelExpectation.spawnCount - recievedChunks.spawnCount} Animations remaining.");
            dataRange.start = recievedChunks.spawnCount;
            dataRange.end = (ushort)(Math.Min((ushort)(levelPrep.levelExpectation.spawnCount - recievedChunks.spawnCount), (ushort)50) + recievedChunks.spawnCount);
            dataRange.levelDataType = LevelDataType.Spawn;
            GiveDataClientRpc(dataRange, levelPrep.spawnPositions.AsSpan().Slice(dataRange.start, dataRange.Length).ToArray(), clientRpcParams);
        }
        else AknowledgeClientRpc(dataRange, clientRpcParams);

    }

}

[Serializable]
public class LevelReciever
{

    public bool loadingCompleted;

    public string levelName = string.Empty;

    [SerializeField]
    public LevelExpectation levelExpectation;
    [SerializeField]
    public LevelExpectation recievedChunks;

    public LevelReciever(LevelExpectation levelExpectation)
    {
        recievedChunks = new LevelExpectation(levelExpectation);
        this.levelExpectation = levelExpectation;
        simplifiedShapeDataArray = new USimplifiedShapeData[this.levelExpectation.shapeCount];
        simplifiedAnimationDataArray = new SimplifiedAnimationData[this.levelExpectation.animationCount];
        lightPositions = new ByteCoord[this.levelExpectation.lightCount];
        spawnPositions = new ByteCoord[this.levelExpectation.spawnCount];
        loadingCompleted = false;
    }

    [SerializeField]
    public USimplifiedShapeData[] simplifiedShapeDataArray = null;
    [SerializeField]
    public SimplifiedAnimationData[] simplifiedAnimationDataArray = null;
    [SerializeField]
    public ByteCoord[] lightPositions = null;
    [SerializeField]
    public ByteCoord[] spawnPositions = null;

    public bool IsHashcodeValid(LevelDataRange levelDataRange) => (levelDataRange.levelHashCode != 0 && levelDataRange.levelHashCode == recievedChunks.levelHashCode);

    private Sprite rasterizedSprite = null;
    public Sprite RasterizeLevel()
    {
        if (!loadingCompleted) return null;
        if(rasterizedSprite) return rasterizedSprite;

        CombineInstance[] shapes = new CombineInstance[simplifiedShapeDataArray.Length];

        Mesh[] meshesToCleanUp = new Mesh[simplifiedShapeDataArray.Length + 1];

        for (int i = 0; i < simplifiedShapeDataArray.Length; i++)
        {
            shapes[i] = new CombineInstance()
            {
                mesh = simplifiedShapeDataArray[i].GenerateWorldspaceMesh(),
                transform = Matrix4x4.identity,
                subMeshIndex = 0,
                lightmapScaleOffset = Vector4.zero,
                realtimeLightmapScaleOffset = Vector4.zero,
            };
            meshesToCleanUp[i] = shapes[i].mesh;
        }

        Mesh combinedShapes = new Mesh();
        combinedShapes.CombineMeshes(shapes, true, true);
        meshesToCleanUp[simplifiedShapeDataArray.Length] = combinedShapes;

        Vector3[] combinedMesh3DVertices = combinedShapes.vertices;
        float size = 0;
        Vector2[] combinedMesh2DVertices = new Vector2[combinedShapes.vertices.Length];
        for (int i = 0; i < combinedMesh2DVertices.Length; i++)
        {
            combinedMesh2DVertices[i] = combinedMesh3DVertices[i];
            if (combinedMesh2DVertices[i].magnitude > size) size = combinedMesh2DVertices[i].magnitude;
        }

        float pixelDensity = 2f;

        byte[] data = PolygonTriangulator.RasterizeMeshDATA(combinedMesh2DVertices, combinedShapes.triangles, out bool v, out int width, out int height, out float ppu, pixelDensity * (256f / size));

        PixelMetaData pixelMetaData = new PixelMetaData();
        pixelMetaData.h = height;
        pixelMetaData.w = width;
        pixelMetaData.ppu = ppu;
        pixelMetaData.v = v;

        Texture2D tex = new Texture2D(pixelMetaData.w, pixelMetaData.h, TextureFormat.Alpha8, false);
        tex.filterMode = FilterMode.Point;

        tex.SetPixelData(data, 0, 0);
        tex.Apply();

        rasterizedSprite = Sprite.Create(tex, new Rect(0, 0, pixelMetaData.w, pixelMetaData.h), new Vector2(0.5f, 0.5f), pixelMetaData.ppu);

        for (int i = meshesToCleanUp.Length - 1; i >= 0; i--) Mesh.Destroy(meshesToCleanUp[i]);

        return rasterizedSprite;
    }
}

[Serializable]
public class LevelPrep
{

    public static JsonSerializerSettings GetJsonSettings()
    {
        JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();

        jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        jsonSerializerSettings.Formatting = Formatting.Indented;
        jsonSerializerSettings.FloatFormatHandling = FloatFormatHandling.String;
        jsonSerializerSettings.FloatParseHandling = FloatParseHandling.Decimal;

        return jsonSerializerSettings;
    }

    [JsonConstructor]
    public LevelPrep() { }

    [SerializeField]
    public string levelName = string.Empty;

    [SerializeField]
    public int byteSize = 0;

    public LevelPrep(LevelExpectation levelExpectation, string levelName)
    {
        byteSize = 0;
        this.levelExpectation = levelExpectation;
        simplifiedShapeDataArray = new USimplifiedShapeData[this.levelExpectation.shapeCount];
        simplifiedAnimationDataArray = new SimplifiedAnimationData[this.levelExpectation.animationCount];
        lightPositions = new ByteCoord[this.levelExpectation.lightCount];
        spawnPositions = new ByteCoord[this.levelExpectation.spawnCount];
        this.levelName = levelName;
    }

    public LevelPrep(LevelPrep cloned)
    {
        this.levelExpectation = cloned.levelExpectation;
        simplifiedShapeDataArray = cloned.simplifiedShapeDataArray;
        simplifiedAnimationDataArray = cloned.simplifiedAnimationDataArray;
        lightPositions = cloned.lightPositions;
        byteSize = cloned.byteSize;
        levelName = string.Copy(cloned.levelName);
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public int[] StoreFromMimics(ShapeMimicBehaviour[] mimics)
    {
        int[] mimicIndexToMimicIDArray = new int[mimics.Length];
        for (int i = 0; i < simplifiedShapeDataArray.Length; i++)
        {
            simplifiedShapeDataArray[i] = ConvertFromSimplifiedShapeData(mimics[i].GetSimplifiedShapeData());
            mimicIndexToMimicIDArray[i] = mimics[i].ShapeID;
            byteSize += simplifiedShapeDataArray[i].GetSize();
        }
        return mimicIndexToMimicIDArray;
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public void StoreFromAnchors(AnimationAnchor[] anchors, int[] mimicIDs)
    {
        // Build lookup: global mimic ID ? local index
        Dictionary<int, int> idToIndex = new Dictionary<int, int>(mimicIDs.Length);
        for (int i = 0; i < mimicIDs.Length; i++)
            idToIndex[mimicIDs[i]] = i;

        // Fill array
        for (int i = 0; i < simplifiedAnimationDataArray.Length; i++)
        {
            simplifiedAnimationDataArray[i] = anchors[i].GetSimplifiedAnimationData();

            // Replace each linkedShape's global ID with local index
            for (int j = 0; j < simplifiedAnimationDataArray[i].linkedShapes.Length; j++)
            {
                int globalID = simplifiedAnimationDataArray[i].linkedShapes[j];
                if (idToIndex.TryGetValue(globalID, out int localIndex))
                {
                    simplifiedAnimationDataArray[i].linkedShapes[j] = localIndex;
                }
                else
                {
                    // optional: warn or mark invalid
                    simplifiedAnimationDataArray[i].linkedShapes[j] = -1;
                }
            }

            byteSize += simplifiedAnimationDataArray[i].GetSize();
        }
    }


    public void StoreFromWorldLights(WorldLight[] lights)
    {
        for (int i = 0; i < lightPositions.Length; i++)
        {
            ByteCoord byteCoord = new ByteCoord();
            byteCoord.SetPosition(lights[i].transform.position);
            lightPositions[i] = byteCoord;
            byteSize += 2;
        }
    }

    public void StoreFromWorldSpawns(EditorSquareSpawn[] spawns)
    {
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            ByteCoord byteCoord = new ByteCoord();
            byteCoord.SetPosition(spawns[i].transform.position);
            spawnPositions[i] = byteCoord;
            byteSize += 2;
        }
    }

    public void StoreFromPermaLight(PermaLightBehvaiour permaLightBehvaiour)
    {
        ByteCoord byteCoord = new ByteCoord();
        byteCoord.SetPosition(permaLightBehvaiour.transform.position);
        lightPositions[0] = byteCoord;
        byteSize += 2;
    }

    public void StoreCompiledLeved()
    {
        StoreCompiledLevel(this, GetJsonSettings());
    }

    public void LoadCompiledLeved(string levelName)
    {
        LevelPrep levelPrep = LevelFilePaths.LoadCompiledLevel(levelName, GetJsonSettings());
        this.levelName = levelPrep.levelName;
        this.levelExpectation = levelPrep.levelExpectation;
        this.simplifiedShapeDataArray = levelPrep.simplifiedShapeDataArray;
        this.simplifiedAnimationDataArray = levelPrep.simplifiedAnimationDataArray;
        this.lightPositions = levelPrep.lightPositions;
        this.spawnPositions = levelPrep.spawnPositions;
        this.byteSize = levelPrep.byteSize;
    }


    [SerializeField]
    public LevelExpectation levelExpectation;
    [SerializeField]
    public USimplifiedShapeData[] simplifiedShapeDataArray = null;
    [SerializeField]
    public SimplifiedAnimationData[] simplifiedAnimationDataArray = null;
    [SerializeField]
    public ByteCoord[] lightPositions = null;
    [SerializeField]
    public ByteCoord[] spawnPositions = null;

    Sprite cachedSprite = null;
    public Sprite RasterizeLevel()
    {
        if(!cachedSprite) cachedSprite = LoadLevelIcon(levelName);
        return cachedSprite;
    }
}

[Serializable]
public struct LevelExpectation : INetworkSerializable
{

    public LevelExpectation(LevelExpectation fromFreshExpectation)
    {
        this.levelHashCode = fromFreshExpectation.levelHashCode;

        shapeCount = 0;
        animationCount = 0;
        lightCount = 0;
        spawnCount = 0;
    }

    public int levelHashCode;
    public ushort shapeCount;
    public ushort animationCount;
    public ushort lightCount;
    public ushort spawnCount;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref levelHashCode);
        serializer.SerializeValue(ref shapeCount);
        serializer.SerializeValue(ref animationCount);
        serializer.SerializeValue(ref lightCount);
        serializer.SerializeValue(ref spawnCount);
    }
}

[Serializable]
public struct LevelDataRange : INetworkSerializable
{
    public LevelDataRange(LevelExpectation fromExpectation)
    {
        this.levelHashCode = fromExpectation.levelHashCode;
        start = 0;
        end = 0;
        levelDataType = LevelDataType.Spawn;
    }
    public int levelHashCode;
    public ushort start;
    public ushort end;
    public LevelDataType levelDataType;
    public ushort Length => (ushort)(end - start);

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref levelHashCode);
        serializer.SerializeValue(ref start);
        serializer.SerializeValue(ref end);
        serializer.SerializeValue(ref levelDataType);
    }
}

[Serializable]
public enum LevelDataType : byte
{
    Shape, Animation, Light, Spawn
}