using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static ProximityPixelationSystem;

public sealed unsafe class ProximityPixelationSystem : MonoBehaviour
{
    public static ProximityPixelationSystem Singleton;

    [SerializeField]
    Material sharedSpriteMaterial;
    Mesh spriteAsMesh = null;
    List<GridSpaceTileData> tiles;

    public List<ProximityPixelSenssor> sensorObjects;

    Camera mainCamera;

    JobHandle positionCompute;
    JobHandle meshCompute;

    NativeList<GridSpaceForceField> nativeProximitySensors;
    NativeList<ForceFieldBlockerData> nativeForceFieldBlockers;
    NativeList<GridSpaceForceField> nativeProximitySensorsSwap;
    NativeList<ForceFieldBlockerData> nativeForceFieldBlockersSwap;

    NativeArray<GridSpaceForceField> fixedProximitySensors;
    NativeArray<ForceFieldBlockerData> fixedforceFieldBlockers;
    NativeArray<GridSpaceTileData> nativeProximityPixels;
    NativeArray<GameStateData> gameState;
    NativeArray<float4x4> models;
    NativeArray<int> visibleIds;

    NativeArray<VertexData> vertexDatas;
    NativeArray<VertexData> vertexDatasSwap;

    NativeArray<float3> vertices;
    NativeArray<int> triangles;

    RenderParams renderParams;

    int tilesSqrt = 128;
    int iterations;
    int batchSize;
    int visibleTiles;

    int gridSpaceForceFieldSize;
    int forceFieldBlockerSize;

    [MethodImpl(512)]
    private void Awake()
    {
        sensorObjects = new List<ProximityPixelSenssor>();
        gridSpaceForceFieldSize = UnsafeUtility.SizeOf<GridSpaceForceField>();
        forceFieldBlockerSize = UnsafeUtility.SizeOf<ForceFieldBlockerData>();

        Singleton = this;

        InitializeProximityTiles();

        InitializeRenderer();

        InitializeNativeArrays();

        ActivateTiles();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void GeneraateLocation(int x, int y)
    {

        GridSpaceTileData tile = new GridSpaceTileData()
        {
            originalTilePosition = new float2(x + 0.5f, y + 0.5f),
            calculatedTilePosition = new float2(x + 0.5f, y + 0.5f),
            tileZPosition = 0,
            originalTileZRotation = 0,
            calculatedTileZRotation = 0,
            originalTileScale = 1.001f,
            calculatedTileScale = 1.2f,
        };

        tiles.Add(tile);

    }

    [MethodImpl(512)]
    private void ActivateTiles()
    {

        GridSpaceTileData* nativeCArray = (GridSpaceTileData*)nativeProximityPixels.GetUnsafePtr();
        transform.localScale = new float3(1f);

    }
    [MethodImpl(512)]
    private void InitializeNativeArrays()
    {
        visibleTiles = 0;

        vertexDatas = new NativeArray<VertexData>(iterations * 4, Allocator.Persistent);
        vertexDatasSwap = new NativeArray<VertexData>(iterations * 4, Allocator.Persistent);

        vertices = new NativeArray<float3>(iterations * 4, Allocator.Persistent);
        triangles = new NativeArray<int>(iterations * 6, Allocator.Persistent);

        visibleIds = new NativeArray<int>(iterations, Allocator.Persistent);
        for (int i = 0; i < iterations; i++)
        {
            visibleIds[i] = visibleTiles;
            visibleTiles++;

            int triangleIndex = i * 6;
            int vertOffset = i * 4;

            triangles[triangleIndex + 0] = vertOffset + 0;
            triangles[triangleIndex + 1] = vertOffset + 1;
            triangles[triangleIndex + 2] = vertOffset + 2;

            triangles[triangleIndex + 3] = vertOffset + 2;
            triangles[triangleIndex + 4] = vertOffset + 3;
            triangles[triangleIndex + 5] = vertOffset + 0;

        }


        nativeProximityPixels = new NativeArray<GridSpaceTileData>(tiles.ToArray(), Allocator.Persistent);
        models = new NativeArray<float4x4>(iterations, Allocator.Persistent);
        nativeProximitySensors = new NativeList<GridSpaceForceField>(Allocator.Persistent);
        nativeProximitySensorsSwap = new NativeList<GridSpaceForceField>(Allocator.Persistent);
        nativeForceFieldBlockers = new NativeList<ForceFieldBlockerData>(Allocator.Persistent);
        nativeForceFieldBlockersSwap = new NativeList<ForceFieldBlockerData>(Allocator.Persistent);
        gameState = new NativeArray<GameStateData>(new GameStateData[]
        { new()
            {
                time = Time.time,
                deltaTime = Time.deltaTime,
                cameraHeight = mainCamera.orthographicSize * 2f,
                cameraWidth = mainCamera.orthographicSize * 2f * mainCamera.aspect,
                cameraPosition = new float2(mainCamera.transform.position.x, mainCamera.transform.position.y),
            }
        }, Allocator.TempJob);
    }
    [MethodImpl(512)]
    private void InitializeProximityTiles()
    {
        tiles = new List<GridSpaceTileData>();
        int dim = tilesSqrt;
        for (int i = -dim; i < dim; i++) for (int j = -dim; j < dim; j++) GeneraateLocation(i, j);

        iterations = tiles.Count;
        batchSize = iterations / (Environment.ProcessorCount * 4);
    }
    [MethodImpl(512)]
    public void InitializeRenderer()
    {

        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;

        spriteAsMesh = new Mesh();

        renderParams = new RenderParams(sharedSpriteMaterial);
        renderParams.instanceID = 0;
        renderParams.layer = 10;
        renderParams.camera = Camera.main;
        renderParams.lightProbeProxyVolume = null;
        renderParams.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderParams.motionVectorMode = MotionVectorGenerationMode.Camera;

        mainCamera = Camera.main;
    }

    NativeArray<VertexAttributeDescriptor> layout;

    private void Start()
    {
        layout = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        layout[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 2);
        layout[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float16, 2);
        layout[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float16, 2);

        CalculateProximityPixels();
        spriteAsMesh.indexFormat = IndexFormat.UInt32;
        spriteAsMesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
        spriteAsMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2048);
        spriteAsMesh.MarkDynamic();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    NativeArray<GameStateData> GetNativeGameState()
    {
        NativeArray<GameStateData> gameStateData = new NativeArray<GameStateData>(1, Allocator.Persistent);

        gameStateData[0] = new()
        {
            time = Time.time,
            deltaTime = registerDT,
            cameraHeight = mainCamera.orthographicSize * 2f,
            cameraWidth = mainCamera.orthographicSize * 2f * mainCamera.aspect,
            cameraPosition = new float2(mainCamera.transform.position.x, mainCamera.transform.position.y),
        };
        return gameStateData;
    }

    float registerDT = 0f;
    float frameRate = 60f;
    float frameTimer = 0f;

    private void Update()
    {
        frameTimer += Time.deltaTime;
        if(frameTimer > 1f / frameRate)
        {
            registerDT = Mathf.Max(frameTimer, Time.deltaTime);
            foreach (var item in sensorObjects) if (item) item.CustomUpdate();
            CalculateProximityPixels();
            frameTimer = 0f;
        }
    }

    private void OnDestroy()
    {

        positionCompute.Complete();
        meshCompute.Complete();

        SafeComplete(meshCompute);
        SafeComplete(positionCompute);

        SafeDispose(nativeProximitySensors);
        SafeDispose(nativeForceFieldBlockers);
        SafeDispose(nativeProximityPixels);
        SafeDispose(models);
        SafeDispose(fixedProximitySensors);
        SafeDispose(fixedforceFieldBlockers);
        SafeDispose(visibleIds);
        SafeDispose(vertices);
        SafeDispose(triangles);
        SafeDispose(gameState);

        Singleton = null;
    }
    
    void SafeDispose<T>(in NativeArray<T> arr) where T : unmanaged
    {
        try
        {
            if(arr.IsCreated) arr.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
    
    void SafeDispose<T>(in NativeList<T> arr) where T : unmanaged
    {
        try
        {
            if (arr.IsCreated) arr.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
    
    void SafeComplete(in JobHandle jobHandle)
    {
        if (jobHandle.IsCompleted) return;
        jobHandle.Complete();
    }

    public void AddForceFieldBlocker(ForceFieldBlockerData forceFieldBlockerData) => nativeForceFieldBlockers.Add(forceFieldBlockerData);
    public void AddProximitySensor(ref GridSpaceForceField newData) => nativeProximitySensors.Add(newData);
    public void AssertMainCamera(Camera newMainCamera) => mainCamera = newMainCamera;

    void CalculateProximityPixels()
    {

        positionCompute.Complete();
        
        gameState.Dispose();
        gameState = GetNativeGameState();

        (vertexDatas, vertexDatasSwap) = (vertexDatasSwap, vertexDatas);
        (nativeForceFieldBlockers, nativeForceFieldBlockersSwap) = (nativeForceFieldBlockersSwap, nativeForceFieldBlockers);
        (nativeProximitySensors, nativeProximitySensorsSwap) = (nativeProximitySensorsSwap, nativeProximitySensors);

        ProximitySensorCalculation proximitySensorCalculation = new ProximitySensorCalculation()
        {
            vertexDatas = vertexDatasSwap,
            forceFieldBlockers = nativeForceFieldBlockersSwap.AsDeferredJobArray(),
            nativeProximityPixels = nativeProximityPixels,
            gridSpaceForceFields = nativeProximitySensorsSwap.AsDeferredJobArray(),
            gameState = gameState,
        };

        positionCompute = proximitySensorCalculation.Schedule(iterations, 64);

        nativeProximitySensors.Clear();
        nativeForceFieldBlockers.Clear();

        spriteAsMesh.SetVertexBufferParams(vertexDatas.Length, layout);
        spriteAsMesh.SetVertexBufferData(vertexDatas, 0, 0, vertexDatas.Length);
    }

    private void LateUpdate()
    {
        Graphics.RenderMesh(renderParams, spriteAsMesh, 0, transform.localToWorldMatrix);
    }

    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    public struct ForceFieldBlockerData
    {

        public float2 pointALocal, pointBLocal;
        public float2 pointAWorld, pointBWorld;

        public float2 worldMin;
        public float2 worldMax;

    }

    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    public struct GameStateData
    {
        public float2 cameraPosition;
        public float time;
        public float deltaTime;
        public float cameraHeight;
        public float cameraWidth;
    }

    [Serializable]
    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    public struct GridSpaceForceField
    {
        [HideInInspector]
        public float3 colorValue;
        [HideInInspector]
        public float colorRadius;
        [HideInInspector]
        public float2 origin;
        [HideInInspector]
        public float rotation;

        [Header("Global settings")]
        [Tooltip("Global time multiplier for reaching the desired warping result")]
        public float warpSpeed;
        [Tooltip("Tile color strength")]
        public float chroma;

        [Header("Blockers")]
        public bool enableBlockerInfluence;
        public float blockerFalloff;

        [Header("Position Warp")]
        public bool enablePositionWarp;
        public float positionWarpStrength;
        public float positionWarpFallof;
        public float positionWarpRadius;

        [Header("Scale Warp")]
        public bool enableScaleWarp;
        public float scaleWarpStrength;
        public float scaleWarpFalloff;
        public float scaleWarpRadius;

        [Header("Rotation Warp")]
        public bool enableRotationWarp;
        public float rotationWarpStrength;
        public float rotationWarpFalloff;
        public float rotationWarpRadius;

        [Header("Swirl/Twist Warp")]
        public bool enableSwirl;
        public float swirlStrength;
        public float swirlFalloff;
        public float swirlRadius;

        [Header("Radial Pulsation")]
        public bool enablePulsation;
        public float pulsationFrequency;
        public float pulsationAmplitude;
        public float pulsationFallof;
        public float pulsationRadius;

    }

    public struct VertexData
    {
        public float2 pos;
        public half2 miscData1;
        public half2 miscData2;
    }

    [BurstCompile(FloatPrecision = FloatPrecision.Low, FloatMode = FloatMode.Fast, DisableDirectCall = false, DisableSafetyChecks = false, OptimizeFor = OptimizeFor.Performance)]
    public struct GridSpaceTileData
    {

        // --- GPU DATA ---
        public half2 miscData1;
        public half2 miscData2;

        // --- Tile Position ---
        public float2 originalTilePosition;     // Original position in world space
        public float2 calculatedTilePosition;   // Calculated position after applying warps
        public float tileZPosition;             // Z position of the tile

        // --- Tile Rotation ---
        public float originalTileZRotation;     // Original Z rotation in radians
        public float calculatedTileZRotation;   // Calculated Z rotation after applying warps

        // --- Tile Scale ---
        public float originalTileScale;         // Original scale of the tile
        public float calculatedTileScale;       // Calculated scale after applying warps

        // --- Tile Visibility ---
        public bool culled;                     // Whether the tile is culled by the camera frustum (Orthographic)
        public int visibleId;

        public void SetId(int visibleId) => this.visibleId = visibleId;

    }
}

[BurstCompile(FloatPrecision = floatPrecision, FloatMode = floatMode, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, OptimizeFor = optimizeFor)]
unsafe struct ProximitySensorCalculation : IJobParallelFor
{
    const FloatPrecision floatPrecision = FloatPrecision.Low;
    const FloatMode floatMode = FloatMode.Fast;
    const OptimizeFor optimizeFor = OptimizeFor.Performance;
    const bool disableDirectCall = false;
    const bool disableSafetyChecks = true;

    [NativeDisableParallelForRestriction] public NativeArray<ForceFieldBlockerData> forceFieldBlockers;
    [NativeDisableParallelForRestriction] public NativeArray<GameStateData> gameState;
    [NativeDisableParallelForRestriction] public NativeArray<GridSpaceTileData> nativeProximityPixels;
    [NativeDisableParallelForRestriction] public NativeArray<GridSpaceForceField> gridSpaceForceFields;
    [NativeDisableParallelForRestriction] public NativeArray<VertexData> vertexDatas;
    const float RETURN_BLEND_STRENGTH = 5f;

    [BurstCompile(FloatPrecision = floatPrecision, FloatMode = floatMode, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, OptimizeFor = optimizeFor)]
    public void Execute(int index)
    {
        GridSpaceTileData* tile = &((GridSpaceTileData*)nativeProximityPixels.GetUnsafePtr())[index];
        GameStateData* gameStateData = &((GameStateData*)gameState.GetUnsafePtr())[0];

        float chromaAccum = 0;

        half pixelScale = (half)2.5f;
        half2 center = new half2(gameStateData->cameraPosition.xy);
        half2 halfSize = new half2((half)(gameStateData->cameraWidth * 0.5f + pixelScale), (half)(gameStateData->cameraHeight * 0.5f + pixelScale));
        half2 min = new half2((half)(center.x - halfSize.x), (half)(center.y - halfSize.y));
        half2 max = new half2((half)(center.x + halfSize.x), (half)(center.y + halfSize.y));
        bool4 result = new bool4 (
            tile->originalTilePosition.x >= min.x,
            tile->originalTilePosition.x <= max.x,
            tile->originalTilePosition.y >= min.y,
            tile->originalTilePosition.y <= max.y);

        if (!math.all(result)) return;

        float time = gameStateData->time;
        float deltaTime = gameStateData->deltaTime;

        float3 colorAccum = float3.zero;
        float2 positionOffset = float2.zero;
        float rotationOffset = 0f;
        float scaleFactor = 1f;
        bool hasInfluence = false;
        float maxBlendStrength = 0f;

        for (int i = 0; i < gridSpaceForceFields.Length; i++)
        {

            GridSpaceForceField* field = &((GridSpaceForceField*)gridSpaceForceFields.GetUnsafePtr())[i];
            float2 toTile = tile->originalTilePosition - field->origin;
            float distance = math.length(toTile);

            bool4x2 isWithinRadiusB = new bool4x2
            (
                distance < field->positionWarpRadius,
                distance < field->swirlRadius,
                distance < field->pulsationRadius,
                distance < field->rotationWarpRadius,
                distance < field->scaleWarpRadius,
                false, false, false
            );

            if (!math.any(isWithinRadiusB.c0 | isWithinRadiusB.c1)) continue;

            float2 dir = math.normalizesafe(toTile);

            bool hasBlockerInfluence = field->enableBlockerInfluence;

            if (hasBlockerInfluence)
            {

                float2 intersection = float2.zero;
                float2 q2 = field->origin + toTile;
                float2 q1 = field->origin;
                float2 minB = math.min(q1, q2);
                float2 maxB = math.max(q1, q2);

                bool isBlocked = false;

                for (int j = 0; j < forceFieldBlockers.Length; j++)
                {
                    ForceFieldBlockerData* blocker = &((ForceFieldBlockerData*)forceFieldBlockers.GetUnsafeReadOnlyPtr())[j];

                    float2 p1 = blocker->pointAWorld;
                    float2 p2 = blocker->pointBWorld;

                    if (field->warpSpeed == 0f) continue;

                    bool2 tooSmall = blocker->worldMax < minB;
                    bool2 tooLarge = blocker->worldMin > maxB;
                    bool2x2 eval = new bool2x2(tooLarge, tooSmall);

                    if (math.any(eval.c0 | eval.c1)) continue;

                    float2x3 dsub1, dsub2;
                    dsub1 = new float2x3(p2, q2, q1);
                    dsub2 = new float2x3(p1, q1, p1);

                    float2x3 dres = dsub1 - dsub2;


                    float3 mul1, mul2, mul3, mul4;
                    mul1 = new float3(dres.c0.x, dres.c2.x, dres.c2.x);
                    mul2 = new float3(dres.c1.y, dres.c1.y, dres.c0.y);
                    mul3 = new float3(dres.c0.y, dres.c2.y, dres.c2.y);
                    mul4 = new float3(dres.c1.x, dres.c1.x, dres.c0.x);

                    float3 sub1, sub2;
                    sub1 = math.mul(mul1, mul2);
                    sub2 = math.mul(mul3, mul4);

                    float3 res = sub1 - sub2;

                    float2 div1, div2;
                    div1 = new float2(res.y, res.z);
                    div2 = new float2(res.x, res.x);

                    float2 divres = div1 / div2;

                    bool4 edge = new bool4(divres.x >= 0f, divres.x <= 1f, divres.y >= 0f, divres.y <= 1f);

                    if (math.all(edge))
                    {
                        intersection = p1 + divres.x * dres.c0;
                        isBlocked = true;
                        break;
                    }

                }

                if (isBlocked) distance = math.pow(distance, field->blockerFalloff);

            }

            colorAccum += field->colorValue * math.saturate(1 - distance / field->colorRadius);

            float influence = 0f;
            if (field->enablePositionWarp && distance < field->positionWarpRadius)
                influence = math.max(influence, math.saturate(1 - distance / field->positionWarpRadius));
            if (field->enableSwirl && distance < field->swirlRadius)
                influence = math.max(influence, math.saturate(1 - distance / field->swirlRadius));
            if (field->enablePulsation && distance < field->pulsationRadius)
                influence = math.max(influence, math.saturate(1 - distance / field->pulsationRadius));
            if (field->enableRotationWarp && distance < field->rotationWarpRadius)
                influence = math.max(influence, math.saturate(1 - distance / field->rotationWarpRadius));
            if (field->enableScaleWarp && distance < field->scaleWarpRadius)
                influence = math.max(influence, math.saturate(1 - distance / field->scaleWarpRadius));

            if (influence <= 0) continue;
            hasInfluence = true;

            maxBlendStrength = math.max(maxBlendStrength, field->warpSpeed * influence);
            if (field->enablePositionWarp)
            {
                float effect = influence * field->positionWarpStrength;
                effect *= math.pow(math.saturate(1 - distance / field->positionWarpRadius), field->positionWarpFallof);
                positionOffset += dir * effect;
            }
            if (field->enableSwirl)
            {
                float effect = influence * field->swirlStrength;
                effect *= math.pow(math.saturate(1 - distance / field->swirlRadius), field->swirlFalloff);
                positionOffset += new float2(-dir.y, dir.x) * effect;
            }
            if (field->enablePulsation)
            {
                float effect = math.sin(time * field->pulsationFrequency) * field->pulsationAmplitude;
                effect *= influence;
                effect *= math.pow(math.saturate(1 - distance / field->pulsationRadius), field->pulsationFallof);
                positionOffset += dir * effect;
            }

            if (field->enableRotationWarp)
            {
                float effect = influence * field->rotationWarpStrength;
                effect *= math.pow(math.saturate(1 - distance / field->rotationWarpRadius), field->rotationWarpFalloff);
                rotationOffset += effect;
            }
            if (field->enableSwirl)
            {
                rotationOffset += influence * field->swirlStrength * 0.1f *
                                 math.pow(math.saturate(1 - distance / field->swirlRadius), field->swirlFalloff);
            }

            if (field->enableScaleWarp)
            {
                scaleFactor *= 1 + influence * field->scaleWarpStrength *
                              math.pow(math.saturate(1 - distance / field->scaleWarpRadius), field->scaleWarpFalloff);
            }
            if (field->enablePulsation)
            {
                scaleFactor *= 1 + math.sin(time * field->pulsationFrequency) * field->pulsationAmplitude * 0.1f *
                               influence * math.pow(math.saturate(1 - distance / field->pulsationRadius), field->pulsationFallof);
            }

            chromaAccum += field->chroma * influence;

        }

        float2 warpedPosition = tile->originalTilePosition + positionOffset;
        float warpedRotation = tile->originalTileZRotation + rotationOffset;
        float warpedScale = tile->originalTileScale * scaleFactor;

        float blendFactor;
        if (hasInfluence)
        {
            blendFactor = math.saturate(maxBlendStrength * deltaTime);
            tile->calculatedTilePosition = math.lerp(tile->calculatedTilePosition, warpedPosition, blendFactor);
            tile->calculatedTileZRotation = math.lerp(tile->calculatedTileZRotation, warpedRotation, blendFactor);
            tile->calculatedTileScale = math.lerp(tile->calculatedTileScale, warpedScale, blendFactor);
        }
        else
        {
            blendFactor = math.saturate(RETURN_BLEND_STRENGTH * deltaTime);
            tile->calculatedTilePosition = math.lerp(tile->calculatedTilePosition, tile->originalTilePosition, blendFactor);
            tile->calculatedTileZRotation = math.lerp(tile->calculatedTileZRotation, tile->originalTileZRotation, blendFactor);
            tile->calculatedTileScale = math.lerp(tile->calculatedTileScale, tile->originalTileScale, blendFactor);
        }

        tile->tileZPosition = -math.distance(tile->calculatedTilePosition, tile->originalTilePosition);

        float colorStrength = math.length(colorAccum);
        float normalizedH = RGBtoHSV(math.normalize(colorAccum)).x;
        tile->miscData1.x = new half(normalizedH);
        tile->miscData1.y = new half(colorStrength);
        tile->miscData2.x = new half(chromaAccum);

        float2 tileScaleXY = tile->calculatedTileScale;
        float4x4 model = new float4x4(new float3x3(quaternion.Euler(0f, 0f, tile->calculatedTileZRotation)) * tile->calculatedTileScale, new float3(tile->calculatedTilePosition, 0f));

        int vertOffset = index * 4;
        int4 vertexOffsets = new int4( vertOffset + 0, vertOffset + 1, vertOffset + 2, vertOffset + 3);

        //GPU DATA

        VertexData vertexData1 = vertexDatas[vertexOffsets.x];
        VertexData vertexData2 = vertexDatas[vertexOffsets.y];
        VertexData vertexData3 = vertexDatas[vertexOffsets.z];
        VertexData vertexData4 = vertexDatas[vertexOffsets.w];

        float3 temp;

        temp = math.transform(model, new float3(-0.5f, -0.5f, 0f));
        vertexData1.pos = temp.xy;
        temp = math.transform(model, new float3(0.5f, -0.5f, 0f));
        vertexData2.pos = temp.xy;
        temp = math.transform(model, new float3(0.5f, 0.5f, 0f));
        vertexData3.pos = temp.xy;
        temp = math.transform(model, new float3(-0.5f, 0.5f, 0f));
        vertexData4.pos = temp.xy;

        half2 miscData1 = tile->miscData1, miscData2 = tile->miscData2;

        vertexData1.miscData1 = miscData2;
        vertexData2.miscData1 = miscData2;
        vertexData3.miscData1 = miscData2;
        vertexData4.miscData1 = miscData2;

        vertexData1.miscData2 = miscData1;
        vertexData2.miscData2 = miscData1;
        vertexData3.miscData2 = miscData1;
        vertexData4.miscData2 = miscData1;

        vertexDatas[vertexOffsets.x] = vertexData1;
        vertexDatas[vertexOffsets.y] = vertexData2;
        vertexDatas[vertexOffsets.z] = vertexData3;
        vertexDatas[vertexOffsets.w] = vertexData4;

    }

    float3 RGBtoHCV(in float3 RGB)
    {
        float4 P = (RGB.y < RGB.z) ? new float4(RGB.zy, -1.0f, 2.0f / 3.0f) : new float4(RGB.yz, 0.0f, -1.0f / 3.0f);
        float4 Q = (RGB.x < P.x) ? new float4(P.xyw, RGB.x) : new float4(RGB.x, P.yzx);
        float C = Q.x - math.min(Q.w, Q.y);
        float H = math.abs((Q.w - Q.y) / (6 * C + math.EPSILON) + Q.z);
        return new float3(H, C, Q.x);
    }

    float3 RGBtoHSV(in float3 RGB)
    {
        float3 HCV = RGBtoHCV(RGB);
        float S = HCV.y / (HCV.z + math.EPSILON);
        return new float3(HCV.x, S, HCV.z);
    }

}