using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static AnimationAnchor;
using static ShapeMimicBehaviour;

public sealed class LevelBuilderStuff : MonoBehaviour
{
    [SerializeField]
    float lightStrength = 0.37f;

    [SerializeField]
    BuiltMapSpawns mapSpawns;

    [SerializeField]
    Transform aMapSpawn;

    [SerializeField]
    LevelAnimationGroup animationGroup;

    [SerializeField]
    Light2D gameLight;

    [SerializeField]
    Transform staticParent;

    [SerializeField]
    BuiltShapeBehaviour builtShapeStaticTemplate;
    [SerializeField]
    BuiltShapeBehaviour builtShapeDynamicTemplate;

    [SerializeField]
    GameObject preparedShadowContainer;

    [SerializeField]
    GameObject preparedIslandContainer;

    Transform mapParent = null;

    public static SimplifiedShapeData[] loadedSimplifiedShapeData;
    public static SimplifiedAnimationData[] simplifiedAnimationDatas;
    public static ByteCoord[] simplifiedLightData;
    public static ByteCoord[] simplifiedSpawnData;

    public List<BuiltShapeBehaviour> builtShapes;
    public List<BuiltShapeBehaviour> builtShapesNoApplication;

    [SerializeField]
    Transform levelOutput;

    public Dictionary<int, List<Transform>> animatedAnimationsAwaitingShapes;

    public static float STENCIL_OFFSET = 0.0f;
    [MethodImpl(512)]
    public void Awake()
    {

        STENCIL_OFFSET = 0.1f;
        animatedAnimationsAwaitingShapes = new Dictionary<int, List<Transform>>();
        builtShapes = new List<BuiltShapeBehaviour>();
        builtShapesNoApplication = new List<BuiltShapeBehaviour>();

        staticParent = Instantiate(staticParent, levelOutput);
        staticParent.position = Vector3.zero;

        BuildLevelFromScratch();

    }

    private void OnDestroy() => STENCIL_OFFSET = 0.0f;

    [MethodImpl(512)]
    void BuildLevelFromScratch()
    {
        if (CachedMapIsInvalid())
        {
            Debug.Log("Map is invalid at the moment!");
            return;
        }
        mapParent = new GameObject("Map Parent").transform;

        BuildAllShapes();
        BuildAllLights();
        BuildAllAnimations();
        BuildAllMapSpawns();
        mapParent.SetParent(levelOutput, true);

        BuildProxies();

        CleanupBuilder();
    }
    [MethodImpl(512)]
    void CleanupBuilder()
    {
        Destroy(staticParent.GetComponent<CompositeCollider2D>());
        Destroy(staticParent.GetComponent<Rigidbody2D>());
        Destroy(staticParent.GetComponent<ShadowCaster2D>());

        foreach (var item in builtShapes)
        {
            if (item.IsStatic)
            {
                Destroy(item.GetComponent<ShadowCaster2DController>());
                Destroy(item.GetComponent<Rigidbody2D>());
                Destroy(item);
            }
            Destroy(item.GetComponent<PolygonCollider2D>());
            Destroy(item);
        }

        foreach (var item in builtShapesNoApplication) if (item) Destroy(item);

        builtShapesNoApplication.Clear();
        builtShapes.Clear();
        builtShapesNoApplication = null;
        builtShapes = null;
    }


    void BuildProxies()
    {
        stencilAccumulation++;
        CompositeCollider2D composite = staticParent.GetComponent<CompositeCollider2D>();
        composite.GenerateGeometry();
        int paths = composite.pathCount;
        for (int i = 0; i < paths; i++) BuildPath(i, composite, stencilAccumulation);
        MeshRenderer[] meshRenderersInComposite = staticParent.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderersInComposite.Length; i++)
        {
            if (meshRenderersInComposite[i].gameObject.name.Equals("BuiltShapeStencil"))
            {
                BuiltShapeBehaviour shape = meshRenderersInComposite[i].transform.parent.GetComponent<BuiltShapeBehaviour>();
                shape.AssignStencil(stencilAccumulation, false);
            }
        }
    }


    void BuildPath(int index, CompositeCollider2D composite, int stencil)
    {

        int pointCount = composite.GetPathPointCount(index);
        Vector2[] points = new Vector2[pointCount];
        composite.GetPath(index, points);
        GameObject test = Instantiate(preparedShadowContainer);
        test.layer = 9;
        test.transform.position = composite.transform.position;
        PolygonCollider2D col = test.AddComponent<PolygonCollider2D>();
        col.useDelaunayMesh = true;
        col.points = points;
        ShadowCaster2D shadowCaster2D = test.GetComponent<ShadowCaster2D>();
        shadowCaster2D.castingOption = ShadowCaster2D.ShadowCastingOptions.CastAndSelfShadow;
        ShadowCaster2DController shadowController2D = test.GetComponent<ShadowCaster2DController>();
        shadowController2D.UpdateFromCollider();
        test.AddComponent<StencilInfectorBehaviour>().SetStencil(stencil);
    }


    void BuildAllMapSpawns()
    {
        mapSpawns = Instantiate(mapSpawns, levelOutput);
        foreach (ByteCoord spawn in simplifiedSpawnData) Instantiate(aMapSpawn, spawn.GetPosition(), Quaternion.identity, mapSpawns.transform);
        mapSpawns.InitializeSpawns();
    }


    void BuildAllAnimations()
    {
        foreach (KeyValuePair<int, List<Transform>> item in animatedAnimationsAwaitingShapes)
        {
            ComplexAnimationData complexAnimationData = ConvertFromSimpleAnimationData(simplifiedAnimationDatas[item.Key]);
            LevelAnimationGroup levelAnimationGroup = Instantiate(animationGroup);
            levelAnimationGroup.ConstructComplex(complexAnimationData);
            foreach (var item1 in item.Value)
            {
                item1.SetParent(levelAnimationGroup.transform, true);
                item1.GetComponent<BuiltShapeBehaviour>().AssignStencil(stencilAccumulation, true);
            }
            levelAnimationGroup.gameObject.AddComponent<StencilInfectorBehaviour>().SetStencil(stencilAccumulation);
            levelAnimationGroup.transform.SetParent(mapParent, true);
            CombineAnimatedColliderIslands(levelAnimationGroup);
            stencilAccumulation++;
        }
    }

    void CombineAnimatedColliderIslands(LevelAnimationGroup animationGroup)
    {
        PolygonCollider2D islandClusterCollider = animationGroup.GetComponent<PolygonCollider2D>();
        islandClusterCollider.pathCount = 0;
        islandClusterCollider.useDelaunayMesh = true;
        PolygonCollider2D[] newIslands = new PolygonCollider2D[animationGroup.transform.childCount];
        for (int i = 0; i < newIslands.Length; i++) newIslands[i] = animationGroup.transform.GetChild(i).GetComponent<PolygonCollider2D>();
        PolygonColliderMerger.MergeIslands(islandClusterCollider, newIslands);
        for (int i = 0; i < newIslands.Length; i++)
        {
            Destroy(newIslands[i].gameObject.GetComponent<ShadowCaster2DController>());
            Destroy(newIslands[i].gameObject.GetComponent<ShadowCaster2D>());
            Destroy(newIslands[i].gameObject.GetComponent<PolygonCollider2D>());
            Destroy(newIslands[i].gameObject.GetComponent<StencilInfectorBehaviour>());
            Destroy(newIslands[i].gameObject.GetComponent<Rigidbody2D>());
        }
        animationGroup.GetComponent<ShadowCaster2DController>().UpdateFromCollider();
    }


    void BuildAllLights()
    {
        foreach (var item in simplifiedLightData)
        {
            Vector3 lightPosition = item.GetPosition();
            Light2D light = Instantiate(gameLight, lightPosition, Quaternion.identity, null);
            light.intensity = lightStrength / simplifiedLightData.Length;
            WorldColors wc = FindAnyObjectByType<WorldColors>();
            if (wc) wc.RegisterLight(light, lightStrength / simplifiedLightData.Length);
        }
    }
    int stencilAccumulation = 1;
    void BuildAllShapes()
    {
        for (int i = 0; i < loadedSimplifiedShapeData.Length; i++)
        {
            bool staticEvaluation = BuiltShapeBehaviour.EvaluateStatic(i);
            if (staticEvaluation)
            {
                BuiltShapeBehaviour newShape = Instantiate(builtShapeStaticTemplate, Vector3.zero, Quaternion.identity, mapParent);
                newShape.ApplyShape(loadedSimplifiedShapeData[i], i, this, staticEvaluation);
                newShape.transform.SetParent(staticParent);
                builtShapesNoApplication.Add(newShape);
            }
            else
            {
                BuiltShapeBehaviour newShape = Instantiate(builtShapeDynamicTemplate, Vector3.zero, Quaternion.identity, mapParent);
                newShape.ApplyShape(loadedSimplifiedShapeData[i], i, this, staticEvaluation);
                builtShapesNoApplication.Add(newShape);
            }
        }
        staticParent.GetComponent<CompositeCollider2D>().edgeRadius = 0f;
    }
    bool CachedMapIsInvalid()
    {
        if (loadedSimplifiedShapeData == null) return true;
        if (simplifiedAnimationDatas == null) return true;
        if (simplifiedLightData == null) return true;
        if (simplifiedSpawnData == null) return true;
        return false;
    }

}