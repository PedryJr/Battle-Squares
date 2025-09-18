using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.InputSystem.InputAction;

public sealed class LevelEditorInitializer : MonoBehaviour
{
    DragAndScrollMod _dragMod;
    [SerializeField]
    Transform shapeParent;
    static Transform staticShapeParent;
    public static Transform StaticShapeParent => staticShapeParent;

    EditorMode editorMode = EditorMode.Shaping;

    TMP_Text modeText;

    private void Awake()
    {
        QualitySettings.SetQualityLevel(1);
        TEMPFUNC();
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();

        ShapeMimicBehaviour.ShapeIDCounter = 0;
        ShapeMimicBehaviour.ShapeMimics.Clear();
        AnimationAnchor.AnimationIDCounter = 0;
        AnimationAnchor.AnimationAnchors.Clear();
        WorldLight.worldLightIDCounter = 0;
        WorldLight.WorldLightCount = 0;
        WorldLight.WorldLights.Clear();

        staticShapeParent = shapeParent;

        modeText = GameObject.Find("Mode Stat").GetComponent<TMP_Text>();

        modeText.text = editorMode switch
        {
            EditorMode.Shaping => "Mode: Shaping",
            EditorMode.Lighting => "Mode: Lighting",
            EditorMode.Animating => "Mode: Animating",
            EditorMode.Transforming => "Mode: Transforming",
            _ => "Unknown"
        };
    }

    private void OnDestroy()
    {
        QualitySettings.SetQualityLevel(0);
    }
    public struct EditModeChangeData
    {
        public EditorMode PreviousMode;
        public EditorMode NewMode;
    }

    public static Action<EditModeChangeData> editModeChanged = e => { };

    public EditorMode GetMode() => editorMode;

    internal void SetMode(CallbackContext obj)
    {

        if (editorMode == EditorMode.Animating) SetMode(EditorMode.Shaping);
        else if (editorMode == EditorMode.Shaping) SetMode(EditorMode.Animating);

    }

    internal void SetMode(EditorMode obj)
    {

        EditModeChangeData data = new EditModeChangeData
        {
            PreviousMode = editorMode,
            NewMode = obj
        };
        editorMode = obj;
        editModeChanged(data);

        modeText.text = editorMode switch
        {
            EditorMode.Shaping => "Mode: Shaping",
            EditorMode.Lighting => "Mode: Lighting",
            EditorMode.Animating => "Mode: Animating",
            _ => "Unknown"
        };

    }

    public void CompileMap()
    {
        ShapeMimicBehaviour[] currentlyPlacedMimics = FindObjectsByType<ShapeMimicBehaviour>(FindObjectsSortMode.InstanceID);
        AnimationAnchor[] currentlyPlacedAnchors = FindObjectsByType<AnimationAnchor>(FindObjectsSortMode.InstanceID);
        WorldLight[] worldLights = FindObjectsByType<WorldLight>(FindObjectsSortMode.InstanceID);
        EditorSquareSpawn[] currentSpawns = FindObjectsByType<EditorSquareSpawn>(FindObjectsSortMode.InstanceID);

        Debug.Log($"{currentlyPlacedMimics.Length} mimics found");
        Debug.Log($"{currentlyPlacedAnchors.Length} mimics found");
        Debug.Log($"{(worldLights.Length > 0 ? worldLights.Length : 1)} lights found");

        string saveStateHash
            = System.DateTime.Now.Second.ToString()
            + System.DateTime.Now.Minute.ToString()
            + System.DateTime.Now.Hour.ToString()
            + _dragMod.activeLevelName;

        LevelExpectation levelExpectation = new LevelExpectation();
        levelExpectation.levelHashCode = saveStateHash.GetHashCode();
        levelExpectation.shapeCount = (ushort) currentlyPlacedMimics.Length;
        levelExpectation.animationCount = (ushort)currentlyPlacedAnchors.Length;
        
        //Temp if more spawns are not needed/desired.
        levelExpectation.spawnCount = (ushort)currentSpawns.Length;
        
        if (worldLights.Length == 0) levelExpectation.lightCount = 1;
        else levelExpectation.lightCount = (ushort)worldLights.Length;

        LevelPrep newLevelPrep = new LevelPrep(levelExpectation, _dragMod.activeLevelName);
        int[] mimicIds = newLevelPrep.StoreFromMimics(currentlyPlacedMimics);
        newLevelPrep.StoreFromAnchors(currentlyPlacedAnchors, mimicIds);
        if (worldLights.Length == 0) newLevelPrep.StoreFromPermaLight(FindAnyObjectByType<PermaLightBehvaiour>());
        else newLevelPrep.StoreFromWorldLights(worldLights);
        newLevelPrep.StoreFromWorldSpawns(currentSpawns);

        newLevelPrep.StoreCompiledLeved();
    }

    void TEMPFUNC()
    {
        if(BuiltShapeBehaviour.octagonalMesh) Destroy(BuiltShapeBehaviour.octagonalMesh);

        Mesh octagonalMesh = new Mesh();

        octagonalMesh.name = "Octagon";

        octagonalMesh.SetVertexBufferParams(8, BuiltShapeBehaviour.GetOctagonalAttribute);
        octagonalMesh.SetVertexBufferData(BuiltShapeBehaviour.GetOctagonalVerticesVec2, 0, 0, 8);

        octagonalMesh.indexFormat = IndexFormat.UInt16;
        octagonalMesh.SetIndices(BuiltShapeBehaviour.GetOctagonalIndices, MeshTopology.Triangles, 0);

        octagonalMesh.bounds = new Bounds(Vector3.zero, new Vector3(512, 512, 1));

        octagonalMesh.UploadMeshData(true);

        BuiltShapeBehaviour.octagonalMesh = octagonalMesh;
/*
        spriteAsMesh.indexFormat = IndexFormat.UInt32;

        spriteAsMesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);*/

    }

    private void Start() => CursorManager.Singleton.UseCursor(CursorManager.CursorType.LevelEditor);

}

public enum EditorMode
{
    Shaping,
    Lighting,
    Animating,
    Transforming,
    Compile
}
