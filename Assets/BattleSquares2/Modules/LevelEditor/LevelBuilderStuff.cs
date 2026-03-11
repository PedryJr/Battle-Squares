using NavMeshPlus.Components;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using static AnimationAnchor;
using static ShapeMimicBehaviour;

public sealed class LevelBuilderStuff : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float lightStrength = 0.37f;
    [SerializeField] private Transform levelOutput;

    [Header("Prefabs")]
    [SerializeField] private Transform boxObstacle;
    [SerializeField] private BuiltShapeBehaviour shapeRendererPrefab;
    [SerializeField] private GameObject shadowedColliderPrefab;
    [SerializeField] private Light2D lightPrefab;
    [SerializeField] private Transform spawnPointPrefab;
    [SerializeField] private BuiltMapSpawns mapSpawnsPrefab;
    [SerializeField] private LevelAnimationGroup animationGroupPrefab;

    [Header("Static Island Settings")]
    [SerializeField] private int maxStaticIslandsPerCluster = 8;

    [Header("Collision Settings")]
    [SerializeField] private bool useEdgeCollidersOnly = true;
    [SerializeField] private float edgeRadius = 0.01f;

    public static SimplifiedShapeData[] loadedSimplifiedShapeData { get; set; }
    public static SimplifiedAnimationData[] simplifiedAnimationDatas { get; set; }
    public static ByteCoord[] simplifiedLightData { get; set; }
    public static ByteCoord[] simplifiedSpawnData { get; set; }

    private Transform staticParent;
    private Transform animatedParent;
    private Transform lightsParent;

    private readonly List<ShapeIsland> staticIslands = new List<ShapeIsland>(128);
    private readonly Dictionary<int, AnimationGroup> animatedGroups = new Dictionary<int, AnimationGroup>(32);

    private int currentStencilId = 1;
    public static float STENCIL_OFFSET = 0f;

    private void Awake()
    {
        STENCIL_OFFSET = 0.1f;
        Initialize();
        BuildLevel();
        FindAnyObjectByType<NavMeshBaker>().BakeArena();
    }

    private void OnDestroy()
    {
        STENCIL_OFFSET = 0f;
    }

    private void Initialize()
    {
        staticParent = new GameObject("Static Shapes").transform;
        animatedParent = new GameObject("Animated Groups").transform;
        lightsParent = new GameObject("Lights").transform;

        staticParent.SetParent(levelOutput);
        animatedParent.SetParent(levelOutput);
        lightsParent.SetParent(levelOutput);
    }

    private void BuildLevel()
    {
        if (IsDataInvalid())
        {
            Debug.LogError("Level data is invalid!");
            return;
        }

        BuildAllShapes();
        BuildAllLights();
        BuildAllSpawns();
    }

    private void BuildAllShapes()
    {
        var staticShapeRenderers = new List<BuiltShapeBehaviour>();
        var staticShapeColliders = new List<GameObject>();

        for (int i = 0; i < loadedSimplifiedShapeData.Length; i++)
        {
            var shapeData = loadedSimplifiedShapeData[i];
            bool isStatic = EvaluateShapeStatic(i);

            if (!isStatic) CreateAnimatedShape(i, shapeData);
        }

        FinalizeAnimatedGroups();

        for (int i = 0; i < loadedSimplifiedShapeData.Length; i++)
        {
            var shapeData = loadedSimplifiedShapeData[i];
            bool isStatic = EvaluateShapeStatic(i);

            if (isStatic) CreateStaticShape(i, shapeData, staticShapeRenderers, staticShapeColliders);
        }

        GroupStaticShapesIntoIslands(staticShapeRenderers, staticShapeColliders);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateShapeStatic(int shapeIndex)
    {
        foreach (var animData in simplifiedAnimationDatas) foreach (var linkedShape in animData.linkedShapes) if (linkedShape == shapeIndex) return false;
        return true;
    }

    private void CreateStaticShape(int index, SimplifiedShapeData shapeData,
                                   List<BuiltShapeBehaviour> renderers,
                                   List<GameObject> colliders)
    {

        var renderer = Instantiate(shapeRendererPrefab, staticParent);
        renderer.Initialize(shapeData, index, false);
        renderers.Add(renderer);

        var colliderObj = Instantiate(shadowedColliderPrefab, staticParent);

        colliderObj.transform.position = renderer.transform.position;

        var polygonCollider = colliderObj.GetComponent<PolygonCollider2D>();
        polygonCollider.points = renderer.GetShapePoints();

        colliderObj.name = $"ShapeCollider_{index}";
        colliders.Add(colliderObj);
    }

    private void CreateAnimatedShape(int index, SimplifiedShapeData shapeData)
    {
        int groupIndex = GetAnimationGroupIndex(index);

        AnimationGroup group;
        if (!animatedGroups.TryGetValue(groupIndex, out group))
        {
            group = CreateAnimationGroup(groupIndex);
            animatedGroups.Add(groupIndex, group);
        }

        var renderer = Instantiate(shapeRendererPrefab, group.RenderersParent);
        renderer.Initialize(shapeData, index, true);
        group.AddShape(renderer);
    }

    private AnimationGroup CreateAnimationGroup(int groupIndex)
    {
        var animData = simplifiedAnimationDatas[groupIndex];
        var complexData = ConvertFromSimpleAnimationData(animData);

        var groupObj = Instantiate(animationGroupPrefab, animatedParent);
        groupObj.ConstructComplex(complexData);

        var group = new AnimationGroup
        {
            Root = groupObj.transform,
            AnimationGroupComponent = groupObj,
            RenderersParent = new GameObject("Renderers").transform,
            ColliderParent = new GameObject("Collider").transform
        };

        group.RenderersParent.SetParent(group.Root);
        group.ColliderParent.SetParent(group.Root);

        var rb = group.Root.gameObject.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        group.StencilId = currentStencilId++;

        return group;
    }

    private void GroupStaticShapesIntoIslands(List<BuiltShapeBehaviour> renderers, List<GameObject> colliders)
    {
        if (renderers.Count == 0) return;

        PolygonCollider2D cluster = CreateStaticCluster(colliders);

        PolygonCollider2D[] clusters = PolygonColliderMerger.SplitCluster(
            cluster,
            shadowedColliderPrefab.GetComponent<PolygonCollider2D>(),
            maxStaticIslandsPerCluster
        );

        foreach (var islandCollider in clusters)
        {
            var island = CreateShapeIsland(islandCollider);
            staticIslands.Add(island);
        }

        AssignRenderersToIslands(renderers);
    }

    private PolygonCollider2D CreateStaticCluster(List<GameObject> colliders)
    {
        var clusterObj = new GameObject("StaticCluster_Temp");
        clusterObj.transform.SetParent(staticParent);

        var clusterCollider = clusterObj.AddComponent<PolygonCollider2D>();
        clusterCollider.pathCount = 0;

        var colliderArray = new PolygonCollider2D[colliders.Count];
        for (int i = 0; i < colliders.Count; i++)
        {
            colliderArray[i] = colliders[i].GetComponent<PolygonCollider2D>();
        }

        PolygonColliderMerger.MergeIslands(clusterCollider, colliderArray);

        foreach (var colliderObj in colliders)
        {
            Destroy(colliderObj);
        }

        return clusterCollider;
    }

    private ShapeIsland CreateShapeIsland(PolygonCollider2D islandCollider)
    {
        var island = new ShapeIsland
        {
            StencilId = currentStencilId++
        };

        island.Root = new GameObject($"StaticIsland_{island.StencilId}").transform;
        island.Root.SetParent(staticParent);

        var shadowedObj = Instantiate(shadowedColliderPrefab, island.Root);
        shadowedObj.name = "Collider";
        island.ColliderParent = shadowedObj.transform;

        var targetCollider = shadowedObj.GetComponent<PolygonCollider2D>();
        if (targetCollider != null)
        {
            targetCollider.pathCount = islandCollider.pathCount;
            for (int i = 0; i < islandCollider.pathCount; i++)
            {
                targetCollider.SetPath(i, islandCollider.GetPath(i));
            }
        }

        var shadowController = shadowedObj.GetComponent<ShadowCaster2DController>();
        if (shadowController != null)
        {
            shadowController.UpdateFromCollider();
        }

        var stencilInfector = shadowedObj.GetComponent<StencilInfectorBehaviour>();
        if (stencilInfector != null)
        {
            stencilInfector.SetStencil(island.StencilId);
        }

        Destroy(islandCollider.gameObject);

        island.RenderersParent = new GameObject("Renderers").transform;
        island.RenderersParent.SetParent(island.Root);

        return island;
    }

    private void AssignRenderersToIslands(List<BuiltShapeBehaviour> renderers)
    {
        foreach (var renderer in renderers)
        {
            var rendererPos = renderer.transform.position;
            ShapeIsland closestIsland = null;
            float closestDistance = float.MaxValue;

            foreach (var island in staticIslands)
            {

                if (useEdgeCollidersOnly)
                {

                    float distance = Vector2.Distance(rendererPos, island.Root.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIsland = island;
                    }
                }
                else
                {

                    var collider = island.ColliderParent.GetComponent<PolygonCollider2D>();
                    if (collider != null && collider.enabled && collider.OverlapPoint(rendererPos))
                    {
                        closestIsland = island;
                        break;
                    }

                    float distance = Vector2.Distance(rendererPos, island.Root.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIsland = island;
                    }
                }
            }

            if (closestIsland != null)
            {
                renderer.transform.SetParent(closestIsland.RenderersParent);
                renderer.AssignStencil(closestIsland.StencilId);
            }
        }
    }

    private void FinalizeAnimatedGroups()
    {
        foreach (var group in animatedGroups)
        {
            if (group.Value == null) continue;

            var shadowedObj = Instantiate(shadowedColliderPrefab, group.Value.ColliderParent);
            shadowedObj.name = "MergedCollider";

            var clusterCollider = shadowedObj.GetComponent<PolygonCollider2D>();
            if (clusterCollider != null)
            {

                PolygonColliderMerger.MergeIslands(clusterCollider, group.Value.GetColliders());
                PolygonCollider2D[] islands = PolygonColliderMerger.SplitCluster(clusterCollider, shadowedColliderPrefab.GetComponent<PolygonCollider2D>(), maxStaticIslandsPerCluster);

                foreach (PolygonCollider2D island in islands)
                {
                    NavMeshModifier navMeshModifier = island.gameObject.GetComponent<NavMeshModifier>();
                    if (navMeshModifier) Destroy(navMeshModifier);

                    var shadowController = island.gameObject.GetComponent<ShadowCaster2DController>();
                    if (shadowController != null)
                    {
                        shadowController.UpdateFromCollider();
                    }

                    var stencilInfector = island.gameObject.GetComponent<StencilInfectorBehaviour>();
                    if (stencilInfector != null)
                    {
                        stencilInfector.SetStencil(group.Value.StencilId);
                    }

                    island.transform.SetParent(clusterCollider.transform.parent);
                }
            }

            group.Value.CleanupTempColliders();

            foreach (Transform renderer in group.Value.RenderersParent)
            {
                var shapeBehaviour = renderer.GetComponent<BuiltShapeBehaviour>();
                if (shapeBehaviour != null)
                {
                    shapeBehaviour.AssignStencil(group.Value.StencilId);
                }
            }
        }
    }

    private void BuildAllLights()
    {
        foreach (var lightCoord in simplifiedLightData)
        {
            var light = Instantiate(lightPrefab, lightsParent);
            light.transform.position = lightCoord.GetPosition();
            light.intensity = lightStrength / simplifiedLightData.Length;

            var worldColors = FindAnyObjectByType<WorldColors>();
            if (worldColors != null) worldColors.RegisterLight(light, lightStrength / simplifiedLightData.Length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildAllSpawns()
    {
        var mapSpawns = Instantiate(mapSpawnsPrefab, levelOutput);
        foreach (var spawnCoord in simplifiedSpawnData) Instantiate(spawnPointPrefab, spawnCoord.GetPosition(), Quaternion.identity, mapSpawns.transform);
        mapSpawns.InitializeSpawns();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetAnimationGroupIndex(int shapeIndex)
    {
        for (int i = 0; i < simplifiedAnimationDatas.Length; i++) foreach (var linkedShape in simplifiedAnimationDatas[i].linkedShapes) if (linkedShape == shapeIndex) return i;
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsDataInvalid()
    {
        return loadedSimplifiedShapeData == null ||
               simplifiedAnimationDatas == null ||
               simplifiedLightData == null ||
               simplifiedSpawnData == null;
    }

    // Helper classes
    private sealed class ShapeIsland
    {
        public Transform Root { get; set; }
        public Transform RenderersParent { get; set; }
        public Transform ColliderParent { get; set; }
        public int StencilId { get; set; }
    }

    private sealed class AnimationGroup
    {
        public Transform Root { get; set; }
        public LevelAnimationGroup AnimationGroupComponent { get; set; }
        public Transform RenderersParent { get; set; }
        public Transform ColliderParent { get; set; }
        public int StencilId { get; set; }

        private readonly List<BuiltShapeBehaviour> shapes = new List<BuiltShapeBehaviour>(16);
        private readonly List<GameObject> colliderObjects = new List<GameObject>(16);

        public void AddShape(BuiltShapeBehaviour shape)
        {
            shapes.Add(shape);

            var colliderObj = new GameObject($"AnimCollider_{shapes.Count}");
            colliderObj.transform.SetParent(ColliderParent);

            colliderObj.transform.localPosition = shape.transform.localPosition;

            var polygonCollider = colliderObj.AddComponent<PolygonCollider2D>();
            polygonCollider.pathCount = 0;
            polygonCollider.points = shape.GetShapePoints();

            colliderObjects.Add(colliderObj);

            var ObstaclePiece = new GameObject($"ObstaclePiece_{shapes.Count}");
            ObstaclePiece.transform.SetParent(ColliderParent);
            ObstaclePiece.transform.position = colliderObj.transform.position;
            ObstaclePiece.transform.rotation = Quaternion.Euler(0, 0, shape.shapeRotation);

            var navMeshObstacle = ObstaclePiece.AddComponent<NavMeshObstacle>();
            navMeshObstacle.carving = true;
            navMeshObstacle.carveOnlyStationary = false;

            // IMPORTANT: obstacle must be Box
            navMeshObstacle.shape = NavMeshObstacleShape.Box;

            // Use the polygon collider from earlier
            ConfigureObstacleFromPolygon(polygonCollider, navMeshObstacle, shape.shapeRotation);

            //Need to ensure position is 0 on the Z axis
            ObstaclePiece.transform.SetParent(Root, true);
            ObstaclePiece.transform.localPosition = new Vector3(ObstaclePiece.transform.localPosition.x, ObstaclePiece.transform.localPosition.y, 0f);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ConfigureObstacleFromPolygon(
    PolygonCollider2D polygon,
    NavMeshObstacle obstacle,
    float rotationZ)
        {
            Vector2[] points = polygon.points;

            Quaternion rot = Quaternion.Euler(0f, 0f, -rotationZ);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0f);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, 0f);

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 rotated = rot * new Vector3(points[i].x, points[i].y, 0f);

                min = Vector3.Min(min, rotated);
                max = Vector3.Max(max, rotated);
            }

            Vector3 size = max - min;
            Vector3 center = (min + max) * 0.5f;

            obstacle.size = new Vector3(size.x, size.y, 1f);
            obstacle.center = new Vector3(center.x, center.y, 0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PolygonCollider2D[] GetColliders()
        {
            var colliders = new PolygonCollider2D[colliderObjects.Count];
            for (int i = 0; i < colliderObjects.Count; i++)
            {
                colliders[i] = colliderObjects[i].GetComponent<PolygonCollider2D>();
            }
            return colliders;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CleanupTempColliders()
        {
            foreach (var colliderObj in colliderObjects)
            {
                if (colliderObj != null)
                {
                    UnityEngine.Object.Destroy(colliderObj);
                }
            }
            colliderObjects.Clear();
        }
    }
}