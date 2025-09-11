using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public sealed class ShapeSelector : MonoBehaviour
{

    float selectionBoxTimer = 0;

    int selectedItems = 0;

    NativeList<MimicData> nativeSelectableMimics;
    NativeArray<SelectorBox> nativeSelectorBox;

    List<ShapeMimicBehaviour> selectedShapes;

    List<ShapeMimicBehaviour> selectableShapes;
    DragAndScrollMod _dragMod;
    SelectorRenderer[] selectorRenderers;
    Transform[] selectorRendererTransforms;
    ToolMode toolMode = ToolMode.Idle;

    Vector3 selectionPositionStart;
    Vector3 selectionPositionEnd;
    Vector3 draggingPosition;

    Vector3 edgeAPos, edgeBPos, edgeCPos, edgeDPos;
    Vector3 edgeAScale, edgeBScale, edgeCScale, edgeDScale;

    private void Awake()
    {
        selectorRenderers = GetComponentsInChildren<SelectorRenderer>();
        selectorRendererTransforms = new Transform[selectorRenderers.Length];
        for (int i = 0; i < selectorRenderers.Length; i++) selectorRendererTransforms[i] = selectorRenderers[i].transform;

        selectionPositionStart = transform.position;
        selectionPositionEnd = transform.position;
        draggingPosition = transform.position;
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();
        selectableShapes = new List<ShapeMimicBehaviour>(2048);
        nativeSelectableMimics = new NativeList<MimicData>(2048, Allocator.Persistent);
        nativeSelectorBox = new NativeArray<SelectorBox>(1, Allocator.Persistent);
        selectedShapes = new List<ShapeMimicBehaviour>(2048);
    }

    private void OnDestroy()
    {
        nativeSelectableMimics.Dispose();
        nativeSelectorBox.Dispose();

        _dragMod.OnShaapeSpawn -= OnShapeSpawnCallback;
        _dragMod.OnShaapeDespawn -= OnShapeDespawnCallback;
        _dragMod.OnShapeIDChange -= UpdateMimicShapeID;
    }

    private void Start()
    {
        _dragMod.OnShaapeSpawn += OnShapeSpawnCallback;
        _dragMod.OnShaapeDespawn += OnShapeDespawnCallback;
        _dragMod.OnShapeIDChange += UpdateMimicShapeID;
    }

    private void Update()
    {
        UpdateSelecting();
    }


    void UpdateSelecting()
    {
        UpdateEdgePositionsAndScales();

        UpdateVisuals();

        PingMimicSelection();

        PingAlreadySelectedMimics();
    }

    public void StartItemsDragging(Vector2 mousePos)
    {
        //Use deltaPos to mouse for all selected transforms when dragging, dont snap to mouse.
        toolMode = ToolMode.Dragging;
    }

    public void EndItemsDragging(Vector2 mousePos)
    {
        if (toolMode != ToolMode.Dragging) return;
        toolMode = ToolMode.Idle;
        foreach (ShapeMimicBehaviour selectedMimic in selectedShapes) selectedMimic.LockInOffset();
        for (int i = 0; i < nativeSelectableMimics.Length; i++)
        {
            ShapeMimicBehaviour shapeMimic = ShapeMimicBehaviour.ShapeMimics[nativeSelectableMimics[i].mimicID];
            nativeSelectableMimics[i] = GetDataFromMimic(shapeMimic);
        }
    }

    public void UpdateDragging(Vector2 mouseDelta)
    {
        if (toolMode != ToolMode.Dragging) return;
        foreach (ShapeMimicBehaviour selectedMimic in selectedShapes) selectedMimic.offsetPosition += (Vector3)mouseDelta;
    }

    public void StartItemSelecting(Vector2 firstCornerPosition)
    {
        selectionPositionStart = firstCornerPosition;
        selectionPositionEnd = firstCornerPosition;
        toolMode = ToolMode.Selecting;
    }

    public void EndItemSelecting(Vector2 lastCornerPosition)
    {
        if (toolMode != ToolMode.Selecting) return;
        selectionPositionEnd = lastCornerPosition;
        toolMode = ToolMode.Idle;
        selectedShapes.Clear();
        for (int i = 0; i < nativeSelectableMimics.Length; i++)
        {
            if (nativeSelectableMimics[i].isSelected) selectedShapes.Add(ShapeMimicBehaviour.ShapeMimics[nativeSelectableMimics[i].mimicID]);
        }
    }

    public void UpdateItemSelecting(Vector2 previewLastCornerPosition)
    {
        if (toolMode != ToolMode.Selecting) return;
        selectionPositionEnd = previewLastCornerPosition;
        if (selectedShapes.Count > 0) selectedShapes.Clear();
    }

    public enum ToolMode
    {
        Selecting,
        Dragging,
        Idle,
    }

    void UpdateEdgePositionsAndScales()
    {
        Vector3 center = Vector3.Lerp(selectionPositionStart, selectionPositionEnd, 0.5f);
        float verticalDistance, horizontalDistance;

        center.z = transform.position.z;
        edgeAPos = center;
        edgeBPos = center;
        edgeCPos = center;
        edgeDPos = center;

        
        edgeAPos.x = selectionPositionStart.x;
        edgeBPos.x = selectionPositionEnd.x;

        edgeCPos.y = selectionPositionStart.y;
        edgeDPos.y = selectionPositionEnd.y;

        horizontalDistance = Vector3.Distance(edgeAPos, edgeBPos);
        verticalDistance = Vector3.Distance(edgeCPos, edgeDPos);

        Vector3 horizontalScale = new Vector3(1f, verticalDistance, 1f);
        Vector3 verticalScale = new Vector3(horizontalDistance, 1f, 1f);

        edgeAScale = horizontalScale;
        edgeBScale = horizontalScale;
        edgeCScale = verticalScale;
        edgeDScale = verticalScale;
    }

    void PingAlreadySelectedMimics()
    {
        for (int i = 0; i < selectedShapes.Count; i++) selectedShapes[i].PingSelected();
    }

    void PingMimicSelection()
    {

        if (toolMode != ToolMode.Selecting) return;

        float By, Ty, Lx, Rx;
        Rx = selectorRendererTransforms[0].position.x;
        Lx = selectorRendererTransforms[1].position.x;
        By = selectorRendererTransforms[2].position.y;
        Ty = selectorRendererTransforms[3].position.y;

        SelectorBox selectorBox = new SelectorBox
        {
            BL = new float2(Lx, By),
            BR = new float2(Rx, By),
            TL = new float2(Lx, Ty),
            TR = new float2(Rx, Ty)
        };

        nativeSelectorBox[0] = selectorBox;

        SelectionJob selectionJob = new SelectionJob
        {
            nativeSelectorBox = nativeSelectorBox,
            selectables = nativeSelectableMimics.AsDeferredJobArray()
        };

        selectionJob.Schedule(nativeSelectableMimics.Length, 4).Complete();

        selectedItems = 0;
        for (int i = 0; i < nativeSelectableMimics.Length; i++)
        {
            if (nativeSelectableMimics[i].isSelected)
            {
                ShapeMimicBehaviour.ShapeMimics[nativeSelectableMimics[i].mimicID].PingSelected();
                selectedItems++;
            }
        }

    }

    void UpdateVisuals()
    {
        selectionBoxTimer += (toolMode == ToolMode.Selecting ? Time.deltaTime : -Time.deltaTime) * 20;
        selectionBoxTimer = Mathf.Clamp01(selectionBoxTimer);
        float smoothSelectionBoxTimer = Mathf.SmoothStep(0, 1, selectionBoxTimer);

        selectorRendererTransforms[0].position = edgeAPos;
        selectorRendererTransforms[1].position = edgeBPos;
        selectorRendererTransforms[2].position = edgeCPos;
        selectorRendererTransforms[3].position = edgeDPos;

        selectorRendererTransforms[0].localScale = edgeAScale * smoothSelectionBoxTimer;
        selectorRendererTransforms[1].localScale = edgeBScale * smoothSelectionBoxTimer;
        selectorRendererTransforms[2].localScale = edgeCScale * smoothSelectionBoxTimer;
        selectorRendererTransforms[3].localScale = edgeDScale * smoothSelectionBoxTimer;

        selectorRenderers[0].outerAnimationMultiplier = smoothSelectionBoxTimer;
        selectorRenderers[1].outerAnimationMultiplier = smoothSelectionBoxTimer;
        selectorRenderers[2].outerAnimationMultiplier = smoothSelectionBoxTimer;
        selectorRenderers[3].outerAnimationMultiplier = smoothSelectionBoxTimer;
    }

    void OnShapeSpawnCallback(ShapeMimicBehaviour newShapeMimic)
    {
        selectableShapes.Add(newShapeMimic);

        Vector2[] mimicShapePoints = newShapeMimic.GetMimicPoints();

        Vector2 positionOffset = newShapeMimic.transform.position;

        nativeSelectableMimics.Add(GetDataFromMimic(newShapeMimic));
    }

    public void HardReset()
    {
        selectedShapes.Clear();
    }

    MimicData GetDataFromMimic(ShapeMimicBehaviour mimic)
    {

        Vector2[] mimicShapePoints = mimic.GetMimicPoints();

        Vector2 positionOffset = mimic.originalPosition + mimic.offsetPosition;

        return new MimicData()
        {
            mimicID = mimic.ShapeID,
            p0 = mimicShapePoints[0] + positionOffset,
            p1 = mimicShapePoints[1] + positionOffset,
            p2 = mimicShapePoints[2] + positionOffset,
            p3 = mimicShapePoints[3] + positionOffset,
            p4 = mimicShapePoints[4] + positionOffset,
            p5 = mimicShapePoints[5] + positionOffset,
            p6 = mimicShapePoints[6] + positionOffset,
            p7 = mimicShapePoints[7] + positionOffset,
            isSelected = false,
        };
    }

    void OnShapeDespawnCallback(ShapeMimicBehaviour oldShapeMimic)
    {
        selectableShapes.Remove(oldShapeMimic);

        int indexToRemove = 0;
        int len = nativeSelectableMimics.Length;
        for (int i = 0; i < len; i++)
        {
            if (nativeSelectableMimics[i].mimicID == oldShapeMimic.ShapeID)
            {
                indexToRemove = i;
                break;
            }
        }
        nativeSelectableMimics.RemoveAt(indexToRemove);
    }

    public void UpdateMimicShapeID(ShapeMimicBehaviour mimicWithChangedID, int oldID, int newID)
    {

        int len = nativeSelectableMimics.Length;
        for (int i = 0; i < len; i++)
        {
            if (nativeSelectableMimics[i].mimicID == oldID)
            {
                MimicData mimicData = nativeSelectableMimics[i];
                mimicData.mimicID = newID;
                nativeSelectableMimics[i] = mimicData;
                break;
            }
        }
    }

    public void PatchMimicsArray()
    {
        selectableShapes.Clear();
        ShapeMimicBehaviour[] patch = ShapeMimicBehaviour.ShapeMimics.Values.ToArray();
        for (int i = 0; i < patch.Length; i++) selectableShapes.Add(patch[i]);
    }

    public void PatchNativeMimicsArray()
    {
        for (int i = 0; i < nativeSelectableMimics.Length; i++)
        {
            ShapeMimicBehaviour shapeMimic = ShapeMimicBehaviour.ShapeMimics[nativeSelectableMimics[i].mimicID];
            nativeSelectableMimics[i] = GetDataFromMimic(shapeMimic);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool hasItemsSelected() => selectedItems != 0;
    public ToolMode GetToolMode() => toolMode;

    struct MimicData
    {
        public float2 p0;
        public float2 p1;
        public float2 p2;
        public float2 p3;
        public float2 p4;
        public float2 p5;
        public float2 p6;
        public float2 p7;
        public int mimicID;
        public bool isSelected;
    }

    struct SelectorBox
    {
        public float2 BL;
        public float2 BR;
        public float2 TL;
        public float2 TR;
    }

    [BurstCompile]
    struct SelectionJob : IJobParallelFor
    {

        public NativeArray<MimicData> selectables;
        [NativeDisableParallelForRestriction]
        public NativeArray<SelectorBox> nativeSelectorBox;
        [BurstCompile]
        public void Execute(int index)
        {

            MimicData mimicData = selectables[index];
            SelectorBox selectorBox = nativeSelectorBox[0];

            mimicData.isSelected = false;

            BoundsCheck(mimicData, selectorBox);

            if(BoundsCheck(mimicData, selectorBox)) mimicData.isSelected = true;
            else if(RayCheck(mimicData, selectorBox)) mimicData.isSelected = true;

            selectables[index] = mimicData;

        }
        [BurstCompile]
        bool BoundsCheck(in MimicData mimicData, in SelectorBox selectorBox)
        {

            float2 min, max;
            min = math.min(selectorBox.BL, selectorBox.TR);
            max = math.max(selectorBox.BL, selectorBox.TR);

            if (PointInBounds(mimicData.p0, min, max)) return true;
            if (PointInBounds(mimicData.p1, min, max)) return true;
            if (PointInBounds(mimicData.p2, min, max)) return true;
            if (PointInBounds(mimicData.p3, min, max)) return true;
            if (PointInBounds(mimicData.p4, min, max)) return true;
            if (PointInBounds(mimicData.p5, min, max)) return true;
            if (PointInBounds(mimicData.p6, min, max)) return true;
            if (PointInBounds(mimicData.p7, min, max)) return true;
            return false;
        }
        [BurstCompile]
        bool RayCheck(in MimicData mimicData, in SelectorBox selectorBox)
        {

            if (RayCheckSegment(mimicData, selectorBox.TR, selectorBox.TL)) return true;
            if (RayCheckSegment(mimicData, selectorBox.TL, selectorBox.BL)) return true;
            if (RayCheckSegment(mimicData, selectorBox.BL, selectorBox.BR)) return true;
            if (RayCheckSegment(mimicData, selectorBox.BR, selectorBox.TR)) return true;
            return false;

        }
        [BurstCompile]
        bool RayCheckSegment(in MimicData mimicData, in float2 c, in float2 d)
        {

            if (SegmentsOverlap(mimicData.p0, mimicData.p1, c, d)) return true;
            if (SegmentsOverlap(mimicData.p1, mimicData.p2, c, d)) return true;
            if (SegmentsOverlap(mimicData.p2, mimicData.p3, c, d)) return true;
            if (SegmentsOverlap(mimicData.p3, mimicData.p4, c, d)) return true;
            if (SegmentsOverlap(mimicData.p4, mimicData.p5, c, d)) return true;
            if (SegmentsOverlap(mimicData.p5, mimicData.p6, c, d)) return true;
            if (SegmentsOverlap(mimicData.p6, mimicData.p7, c, d)) return true;
            if (SegmentsOverlap(mimicData.p7, mimicData.p0, c, d)) return true;
            return false;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PointInBounds(in float2 point, in float2 min, in float2 max)
            => math.all(new bool4(point.x >= min.x, point.x <= max.x, point.y >= min.y, point.y <= max.y));
        [BurstCompile]
        public bool SegmentsOverlap(in float2 a, in float2 b, in float2 c, in float2 d)
        {
            float2 ab = b - a;
            float2 ac = c - a;
            float2 ad = d - a;
            float2 cd = d - c;
            float2 ca = a - c;
            float2 cb = b - c;

            float cross1 = Cross(ab, ac);
            float cross2 = Cross(ab, ad);
            float cross3 = Cross(cd, ca);
            float cross4 = Cross(cd, cb);

            if ((cross1 * cross2 < 0f) && (cross3 * cross4 < 0f)) return true;

            if (math.abs(cross1) < 1e-6f && OnSegment(a, b, c)) return true;
            if (math.abs(cross2) < 1e-6f && OnSegment(a, b, d)) return true;
            if (math.abs(cross3) < 1e-6f && OnSegment(c, d, a)) return true;
            if (math.abs(cross4) < 1e-6f && OnSegment(c, d, b)) return true;

            return false;
        }
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float Cross(in float2 u, in float2 v) 
            => u.x * v.y - u.y * v.x;
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool OnSegment(in float2 p, in float2 q, in float2 r)
            => math.all(new bool4(
                math.min(p.x, q.x) - 1e-6f <= r.x,
                r.x <= math.max(p.x, q.x) + 1e-6f,
                math.min(p.y, q.y) - 1e-6f <= r.y,
                r.y <= math.max(p.y, q.y) + 1e-6f));


    }

}
