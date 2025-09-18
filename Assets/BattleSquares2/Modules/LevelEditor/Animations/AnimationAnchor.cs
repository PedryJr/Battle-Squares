using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using static DragAndScrollMod;
using static ShapeMimicBehaviour;

public sealed class AnimationAnchor : MonoBehaviour
{

    [SerializeField]
    TMP_Text speedIndicator;

    [SerializeField]
    Color speedIndicatorColor;
    [SerializeField]
    Color speedIndicatorAlpha;

    public float lastSplineSpeed = 0.0f;
    public float lastSplineOffset = 0.0f;
    public float speedChangeVisibleTimer = 1.0f;

    RenderParams renderParams;


    public static Dictionary<int, AnimationAnchor> AnimationAnchors = new Dictionary<int, AnimationAnchor>();
    public static int AnimationIDCounter = 0;
    private int animationID;
    public int AnimationID => animationID;

    public const float animationSpeed = 20f;

    public bool previewEnabled = false;
    public bool selected = true;

    List<SpriteRenderer> spriteRenderers;
    [MethodImpl(512)]
    public void RefreshRenderers()
    {
        spriteRenderers.Clear();
        spriteRenderers.Add(GetComponent<SpriteRenderer>());
        splineMods.ForEach(mod =>
        {
            if (mod.GetEnd().GetComponent<SpriteRenderer>()) spriteRenderers.Add(mod.GetEnd().GetComponent<SpriteRenderer>());
            if (mod.GetLeft().GetComponent<SpriteRenderer>()) spriteRenderers.Add(mod.GetLeft().GetComponent<SpriteRenderer>());
            if (mod.GetRight().GetComponent<SpriteRenderer>()) spriteRenderers.Add(mod.GetRight().GetComponent<SpriteRenderer>());
        });
        foreach (var item in spriteRenderers) item.material = clonedMat;
    }

    [SerializeField]
    Color selectedColor;
    [SerializeField]
    Color normalColor;
    [SerializeField]
    Color usedColor;

    public float splineTravel;
    public float splineSpeed;
    public float splineOffset;

    [SerializeField]
    Material splineVisual;

    Material clonedMat;

    SplineRenderer splineRenderer;

    public List<SplineMod> splineMods;
    Transform splineStart;
    [MethodImpl(512)]
    void Awake()
    {

        splineVisual = Instantiate(splineVisual);
        clonedMat = Instantiate(GetComponent<SpriteRenderer>().material);

        animationID = AnimationIDCounter;
        AnimationIDCounter++;

        spriteRenderers = new List<SpriteRenderer>();
        splineMods = new List<SplineMod>();
        splineStart = transform;
        splineRenderer = new SplineRenderer();
        attatchedBody = new GameObject("AttatchmentBody").transform;
        attatchedBody.localScale = Vector3.one;
        attatchedBody.position = transform.position;
        attatchedBody.rotation = transform.rotation;
        renderParams = new RenderParams(splineVisual);

        attatchedTransforms = new List<Transform>();
        attatchedMimics = new List<ShapeMimicBehaviour>();
        cachedTransform = transform;
    }

    [SerializeField]
    SplineMod splineModPrefab;

    Transform cachedTransform;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    SplineMod GetLastSplineMod() => splineMods.Count > 0 ? splineMods[splineMods.Count - 1] : null;

    Transform attatchedBody;

    List<ShapeMimicBehaviour> attatchedMimics;

    List<Transform> attatchedTransforms;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetAttatchedMimicIDs()
    {
        int[] IDs = new int[attatchedMimics.Count];
        for (int i = 0; i < IDs.Length; i++) IDs[i] = attatchedMimics[i].ShapeID;
        return IDs;
    }
    [MethodImpl(512)]
    private void AssignToAttatchedBody(Transform toAssign)
    {
        Vector3 rememberAttatchedPosition = attatchedBody.position;
        Vector3 rememberAssigneePosition = toAssign.position;

        attatchedBody.position = new Vector3(targetPos.x, targetPos.y, attatchedBody.position.z);
        toAssign.SetParent(attatchedBody);
        toAssign.position = rememberAssigneePosition;

        attatchedBody.position = rememberAttatchedPosition;
        attatchedTransforms.Add(toAssign);
    }
    [MethodImpl(512)]
    private void RemoveAttatchedBody(Transform toRemove)
    {

        Vector3 rememberAttatchedPosition = attatchedBody.position;
        attatchedBody.position = new Vector3(targetPos.x, targetPos.y, attatchedBody.position.z);
        toRemove.SetParent(null, true);
        attatchedBody.position = rememberAttatchedPosition;

        attatchedTransforms.Remove(toRemove);

    }

    public void AttatchMimic(int mimicId)
    {

        if (previewEnabled) return;

        ShapeMimicBehaviour mimic = ShapeMimicBehaviour.ShapeMimics[mimicId];
        mimic.EnsureNoAnimation();
        if (!attatchedMimics.Contains(mimic))
        {
            attatchedMimics.Add(mimic);
            mimic.AssignAnimationAnchor(this);
        }

    }

    public void DetatchMimic(int mimicId)
    {

        if (previewEnabled) return;

        ShapeMimicBehaviour mimic = ShapeMimicBehaviour.ShapeMimics[mimicId];
        if (attatchedMimics.Contains(ShapeMimicBehaviour.ShapeMimics[mimicId]))
        {
            attatchedMimics.Remove(mimic);
            mimic.AssignAnimationAnchor(null);
        }

    }

    [MethodImpl(512)]
    public void RetatchMimic(int mimicId)
    {

        if (previewEnabled) return;

        ShapeMimicBehaviour mimic = ShapeMimicBehaviour.ShapeMimics[mimicId];
        mimic.EnsureNoAnimation();
        if (attatchedMimics.Contains(mimic))
        {
            attatchedMimics.Remove(mimic);
        }
        else
        {
            attatchedMimics.Add(mimic);
            mimic.AssignAnimationAnchor(this);
        }

        Debug.Log($"Retatched mimic {mimicId} to animation anchor {name}");

    }
    [MethodImpl(512)]
    public void EnsureDetatchMimic(int mimicId)
    {
        DetatchMimic(mimicId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnablePreview()
    {
        attatchedMimics.ForEach(mimic =>AssignToAttatchedBody(mimic.transform));
        previewEnabled = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DisablePreview()
    {
        attatchedMimics.ForEach(mimic => RemoveAttatchedBody(mimic.transform));
        previewEnabled = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector3 GetCurrentEndPos() => splineMods.Count > 0
    ? ((Vector3)((Vector2)GetLastSplineMod().GetEnd().position - targetPos).normalized * 5f) + GetLastSplineMod().GetEnd().position
    : splineStart.position + new Vector3(0f, 5f, 0f);
    [MethodImpl(512)]
    Vector2 GetSnappedPosition(Vector2 rawPosition)
    {

        float x, y;
        x = Mathf.Round(rawPosition.x / 1f) * 1f;
        y = Mathf.Round(rawPosition.y / 1f) * 1f;

        return new Vector2(x, y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OverrideID(int ID)
    {
        animationID = ID;
        AnimationAnchors.Add(ID, this);
        foreach (var item in splineMods)
        {
            item.GetLeftMod().SetConnectedAnchor(this);
            item.GetRightMod().SetConnectedAnchor(this);
            item.GetEndMod().SetConnectedAnchor(this);
        }
    }
    [MethodImpl(512)]
    public void BuildFromData(AnimationMemory data)
    {

        Vector2[] splinesData = data.GetSegmentsAsVector2Arr();

        splineMods = new List<SplineMod>();

        targetPos = GetSnappedPosition(data.startSegment.AsVector2());
        HardSetAnchorPosition();

        for (int i = 0; i < splinesData.Length; i+=3)
        {
            SplineMod newSplineMod = Instantiate(splineModPrefab, null);

            SplineDrag d1 = newSplineMod.GetEndMod(), d2 = newSplineMod.GetLeftMod(), d3 = newSplineMod.GetRightMod();

            d1.HardUpdateAnchorPosition(targetPos);
            d2.HardUpdateAnchorPosition(targetPos);
            d3.HardUpdateAnchorPosition(targetPos);

            d1.DoDrag(splinesData[i + 0]);
            d2.DoDrag(splinesData[i + 1]);
            d3.DoDrag(splinesData[i + 2]);

            splineMods.Add(newSplineMod);
        }


        RebuildSpline();

        RefreshRenderers();

    }
    [MethodImpl(512)]
    public void AddSplineMod()
    {

        SplineMod newSplineMod = Instantiate(splineModPrefab, null);

        Vector3 currentEndPosition = GetSnappedPosition(GetCurrentEndPos());

        Vector3 middlePosition1, middlePosition2;

        middlePosition1 = GetSnappedPosition(Vector3.Lerp(
            currentEndPosition,
            splineStart.position,
            0.66f
        ));

        middlePosition2 = GetSnappedPosition(Vector3.Lerp(
            currentEndPosition,
            splineStart.position,
            0.33f
        ));

        SplineDrag d1 = newSplineMod.GetEndMod(), d2 = newSplineMod.GetLeftMod(), d3 = newSplineMod.GetRightMod();

        d1.HardUpdateAnchorPosition(targetPos);
        d2.HardUpdateAnchorPosition(targetPos);
        d3.HardUpdateAnchorPosition(targetPos);

        d1.DoDrag(currentEndPosition);
        d2.DoDrag(middlePosition1);
        d3.DoDrag(middlePosition2);

        splineMods.Add(newSplineMod);
        splineRenderer.AddSegment(
            splineStart,
            newSplineMod.GetLeft(),
            newSplineMod.GetRight(),
            newSplineMod.GetEnd()
        );
        splineStart = newSplineMod.GetEnd();
        newSplineMod.AssignAnchor(this);

        RefreshRenderers();

    }
    [MethodImpl(512)]
    public void RemoveSplineMod(SplineMod splineMod)
    {
        splineMods.Remove(splineMod);
        Destroy(splineMod.gameObject);
        RebuildSpline();
        RefreshRenderers();
    }
    [MethodImpl(512)]
    void RebuildSpline()
    {
        splineRenderer.Clear();

        splineStart = cachedTransform;
    
        foreach (SplineMod mod in splineMods)
        {
            splineRenderer.AddSegment(
                splineStart,
                mod.GetLeft(),
                mod.GetRight(),
                mod.GetEnd()
            );
            splineStart = mod.GetEnd();
        }

    }
    [MethodImpl(512)]
    public void MoveAll(Vector2 newPos)
    {

        Vector2 delta = newPos - targetPos;
        splineMods.ForEach(mod => mod.DragAll(delta, true));
        targetPos = newPos;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MoveSplineMod(SplineMod splineMod, Vector2 newPos)
    {

        splineMod.DragAll(newPos, true);

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ChangeAnchorPosition(Vector2 newPos) => targetPos = newPos;

    Vector2 targetPos;
    public Vector2 TargetPos => targetPos;

    EditActions editActions;
    [MethodImpl(512)]
    private void Start()
    {
        editActions = new EditActions();
        editActions.Enable();
        editActions.Mouse.Test.performed += e => burstEnabled = !burstEnabled;
        targetPos = cachedTransform.position;
        animationReference = Instantiate(animationReference, null);
    }
    [MethodImpl(512)]
    private void OnDestroy()
    {
        speedIndicator.color = Color.clear;
        attatchedTransforms.ForEach
            (
            attatchedTransform =>
            {
                if (attatchedTransform.TryGetComponent(out ShapeMimicBehaviour mimic)) DetatchMimic(mimic.ShapeID);
            }
            );
        editActions.Dispose();
        Destroy(animationReference.gameObject);
        Destroy(attatchedBody.gameObject);
        Destroy(clonedMat);
        Destroy(splineVisual);
        Destroy(speedIndicator.gameObject);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void UpdateAnchorPosition() => cachedTransform.position = Vector3.Lerp(cachedTransform.position, new Vector3(targetPos.x, targetPos.y, cachedTransform.position.z), Time.deltaTime * animationSpeed);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HardSetAnchorPosition() => cachedTransform.position = new Vector3(targetPos.x, targetPos.y, cachedTransform.position.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform GetAnchor() => cachedTransform;

    [SerializeField] bool burstEnabled = true;

    [SerializeField] Transform animationReference;

    private void Update()
    {

        UpdateSpeedIndicator();

        UpdateAnchorPosition();

        RenderSplines();

        CalculateAnimation();

        HighlighSelection();

    }

    void UpdateSpeedIndicator()
    {
        splineSpeed = Mathf.Clamp(splineSpeed, -1f, 1f);
        lastSplineOffset = Mathf.Clamp(lastSplineOffset, -1f, 1f);
        speedIndicator.text = $"{Mathf.RoundToInt(splineSpeed * 100)} + {Mathf.RoundToInt(splineOffset * 100)}";
        speedChangeVisibleTimer -= Time.deltaTime;
        speedChangeVisibleTimer = Mathf.Clamp01(speedChangeVisibleTimer);
        if(lastSplineSpeed != splineSpeed || lastSplineOffset != splineOffset)
        {
            lastSplineOffset = splineOffset;
            lastSplineSpeed = splineSpeed;
            speedChangeVisibleTimer = 1.0f;
        }
        speedIndicator.color = Color.Lerp(speedIndicatorAlpha, speedIndicatorColor, Mathf.SmoothStep(0f, 1f, speedChangeVisibleTimer));
    }

    [MethodImpl(512)]
    private void HighlighSelection()
    {
        if(selected) foreach (var item in attatchedMimics) item.PingSelected();

        usedColor = Color.Lerp(usedColor, previewEnabled ? new Color(1f, 1f, 1f, 0f) : (selected ? selectedColor : normalColor), Time.deltaTime * animationSpeed);
        foreach (var item in spriteRenderers) item.color = usedColor;
        splineVisual.color = usedColor;
    }
    [MethodImpl(512)]
    void CalculateAnimation()
    {

        splineTravel += Time.deltaTime * splineSpeed;
        splineTravel = Mathf.Repeat(splineTravel, 1f);

        float withOffset = Mathf.Repeat(splineTravel + splineOffset, 1f);

        Vector3 evaluatedPosition = splineRenderer.Evaluate(withOffset);

        attatchedBody.position = evaluatedPosition;
        animationReference.position = evaluatedPosition;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void RenderSplines()
    {
        if(!previewEnabled) splineRenderer.Render(renderParams, splineVisual, 100);
    }

    [MethodImpl(512)]
    public void DeleteAnimation()
    {

        for (int i = splineMods.Count - 1; i >= 0; i--) Destroy(splineMods[i].gameObject);
        splineMods.Clear();
        splineRenderer.Clear();
        for (int i = 0; i < cachedTransform.childCount; i++) cachedTransform.GetChild(i).SetParent(null);

        splineMods = null;
        splineRenderer = null;

        Destroy(gameObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ResetTimer() => splineTravel = 0f;

    public static ComplexAnimationData ConvertFromSimpleAnimationData(SimplifiedAnimationData simple)
    {
        ComplexAnimationData complexAnimationData = new ComplexAnimationData();
        complexAnimationData.animationSpeed = (simple.animationSpeed / 100f) - 1f;
        complexAnimationData.animationOffset = (simple.animationOffset / 100f) - 1f;

        complexAnimationData.segmentCoords = new Vector2[simple.segmentCoords.Length];
        for (int i = 0; i < complexAnimationData.segmentCoords.Length; i++)
            complexAnimationData.segmentCoords[i] = simple.segmentCoords[i].GetPosition();

        complexAnimationData.linkedShapes = new int[simple.linkedShapes.Length];
        for (int i = 0; i < complexAnimationData.linkedShapes.Length; i++)
            complexAnimationData.linkedShapes[i] = simple.linkedShapes[i];
        return complexAnimationData;
    }

    public SimplifiedAnimationData GetSimplifiedAnimationData()
    {
        SimplifiedAnimationData data = new SimplifiedAnimationData();


        data.animationSpeed = (byte) Mathf.RoundToInt((splineSpeed + 1f) * 100);
        data.animationOffset = (byte) Mathf.RoundToInt((splineOffset + 1f) * 100);

        data.segmentCoords = new ByteCoord[(splineMods.Count * 3) + 1];

        //Add start first, then the rest of the mods

        ByteCoord start = new ByteCoord();
        start.SetPosition(transform.position);
        data.segmentCoords[0] = start;
        for (int i = 1; i < data.segmentCoords.Length; i+=3)
        {
            ByteCoord left = new ByteCoord(), right = new ByteCoord(), end = new ByteCoord();

            left.SetPosition(splineMods[i / 3].GetLeft().transform.position);
            right.SetPosition(splineMods[i / 3].GetRight().transform.position);
            end.SetPosition(splineMods[i / 3].GetEnd().transform.position);

            data.segmentCoords[i + 0] = left;
            data.segmentCoords[i + 1] = right;
            data.segmentCoords[i + 2] = end;
        }

        data.linkedShapes = new int[attatchedMimics.Count];
        for (int i = 0; i < data.linkedShapes.Length; i++) data.linkedShapes[i] = attatchedMimics[i].ShapeID;

        return data;
    }

    [Serializable]
    public struct SimplifiedAnimationData : INetworkSerializable
    {
        public ByteCoord animationParams;
        public ByteCoord[] segmentCoords;
        public int[] linkedShapes;

        [JsonIgnore]
        public byte animationSpeed
        {
            get { return animationParams.x; }
            set { animationParams.x = value; }
        }
        [JsonIgnore]
        public byte animationOffset
        {
            get { return animationParams.y; }
            set { animationParams.y = value; }
        }

        public int GetSize() => 1 + 1 + (sizeof(int) * (linkedShapes != null ? linkedShapes.Length : 0)) + (ByteCoord.GetSize() * (segmentCoords != null ? segmentCoords.Length : 0));

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref animationParams);
            serializer.SerializeValue(ref segmentCoords);
            serializer.SerializeValue(ref linkedShapes);
        }
    }

    public struct ComplexAnimationData
    {

        public float animationSpeed;
        public float animationOffset;
        public Vector2[] segmentCoords;
        public int[] linkedShapes;

    }

}
