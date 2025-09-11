using System;
using System.Collections.Generic;
using System.Resources;
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

    Transform mapParent = null;

    public static SimplifiedShapeData[] loadedSimplifiedShapeData;
    public static SimplifiedAnimationData[] simplifiedAnimationDatas;
    public static ByteCoord[] simplifiedLightData;
    public static ByteCoord[] simplifiedSpawnData;

    public List<BuiltShapeBehaviour> builtShapes;

    [SerializeField]
    Transform levelOutput;

    public Dictionary<int, List<Transform>> animatedAnimationsAwaitingShapes;

    public static float STENCIL_OFFSET = 0.0f;

    public void Awake()
    {
        STENCIL_OFFSET = -4.0f;
        animatedAnimationsAwaitingShapes = new Dictionary<int, List<Transform>>();
        builtShapes = new List<BuiltShapeBehaviour>();

        staticParent = Instantiate(staticParent, levelOutput);
        staticParent.position = Vector3.zero;

        BuildLevelFromScratch();

    }

    private void OnDestroy() => STENCIL_OFFSET = 0.0f;

    void BuildLevelFromScratch()
    {
        if (CachedMapIsInvalid())
        {
            Debug.Log("Map is invalid at the moment!");
            return;
        }
        mapParent = new GameObject("Map Parent").transform;
        mapParent.gameObject.AddComponent<CompositeShadowCaster2D>();
        MyLog.Output("Builder is expecting to build these arrays with length");
        MyLog.Output(loadedSimplifiedShapeData.Length);
        MyLog.Output(simplifiedAnimationDatas.Length);
        MyLog.Output(simplifiedLightData.Length);
        MyLog.Output(simplifiedSpawnData.Length);

        BuildAllShapes();
        BuildAllLights();
        BuildAllAnimations();
        BuildAllMapSpawns();
        mapParent.SetParent(levelOutput, true);

        BuildProxies();
        CleanupBuilder();
    }

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
            Destroy(item.GetComponent<BuiltShapeBehaviour>());
        }
        builtShapes.Clear();
        builtShapes = null;
        Resources.UnloadUnusedAssets();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    void BuildProxies()
    {
        CompositeCollider2D composite = staticParent.GetComponent<CompositeCollider2D>();
        composite.GenerateGeometry();
        int paths = composite.pathCount;
        for (int i = 0; i < paths; i++) BuildPath(i, composite);
    }

    void BuildPath(int index, CompositeCollider2D composite)
    {

        int pointCount = composite.GetPathPointCount(index);
        Vector2[] points = new Vector2[pointCount];
        composite.GetPath(index, points);

        GameObject test = new GameObject("Test");
        test.layer = 9;
        test.transform.position = composite.transform.position;
        PolygonCollider2D col = test.AddComponent<PolygonCollider2D>();
        col.useDelaunayMesh = true;
        col.points = points;
        ShadowCaster2D shadowCaster2D = test.AddComponent<ShadowCaster2D>();
        shadowCaster2D.castingOption = ShadowCaster2D.ShadowCastingOptions.CastAndSelfShadow;
        ShadowCaster2DController shadowController2D = test.AddComponent<ShadowCaster2DController>();
        shadowController2D.UpdateFromCollider();

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
            foreach (var item1 in item.Value) item1.SetParent(levelAnimationGroup.transform, true);
            levelAnimationGroup.transform.SetParent(mapParent, true);
        }
    }

    void BuildAllLights()
    {
        foreach (var item in simplifiedLightData)
        {
            Vector3 lightPosition = item.GetPosition();
            Light2D light = Instantiate(gameLight, lightPosition, Quaternion.identity, null);
            light.intensity = lightStrength / simplifiedLightData.Length;
        }
    }

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
            }
            else
            {
                BuiltShapeBehaviour newShape = Instantiate(builtShapeDynamicTemplate, Vector3.zero, Quaternion.identity, mapParent);
                newShape.ApplyShape(loadedSimplifiedShapeData[i], i, this, staticEvaluation);
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