using UnityEngine;

public sealed class EditorCursor : ModularCursor
{

    [SerializeField]
    float animationSpeed = 20f;

    Transform cachedTransform;

    DragAndScrollMod dragAndScrollMod;

    [SerializeField]
    Color targetNoPanningColor;
    [SerializeField]
    Color targetPanningColor;
    Color smoothColor = new Color(0, 0, 0, 1);

    [SerializeField]
    SpriteRenderer[] indicatorSprites;

    protected override void Awake()
    {
        base.Awake();
        cachedTransform = transform;
        targetNodes = offClickNodes;
        originalTargetScale = indicatorTarget.localScale;
        targetScale = indicatorTarget.localScale;
    }

    [SerializeField]
    Transform[] cursorNodes; 
    [SerializeField]
    Transform[] onClickNodes;
    [SerializeField]
    Transform[] offClickNodes;


    Transform[] targetNodes;

    Vector3 originalTargetScale;
    Vector3 targetScale;

    bool isPanning = false;

    void MouseClick(MouseClickData data)
    {
        if(data.clickType == MouseClickData.ClickType.Left)
        {
            if(data.On) targetNodes = onClickNodes;
            else targetNodes = offClickNodes;
        }
        else if(data.clickType == MouseClickData.ClickType.Right)
        {
            if (data.On)
            {
                isPanning = true;
                targetScale = cursorVisual.localScale * 3f;
            }
            else
            {
                isPanning = false;
                targetScale = originalTargetScale;
            }
        }
        
    }

    Vector3 screenPosition = Vector3.zero;

    void OnMouseMoved(MouseMovementData data)
    {
        screenPosition = data.screenPosition;
    }

    [SerializeField] Transform cursorVisual;
    [SerializeField] Transform indicatorTarget;

    Vector3 targetSnapPosition = Vector3.zero;
    Vector3 targetSmoothPosition = Vector3.zero;

    private void Update()
    {
        if (!dragAndScrollMod) return;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        worldPosition.z = cachedTransform.position.z;
        cachedTransform.position = worldPosition;

        EnsureZDepth();

        smoothColor = Color.Lerp(smoothColor, isPanning ? targetPanningColor : targetNoPanningColor, Time.deltaTime * animationSpeed);

        targetSnapPosition = isPanning ? cursorVisual.position : GetSnappedPosition(cursorVisual.position);
        targetSmoothPosition = Vector3.Lerp(targetSmoothPosition, targetSnapPosition, Time.deltaTime * (isPanning ? animationSpeed * 2 : animationSpeed));
        indicatorTarget.position = targetSmoothPosition;

        Run();
        UpdateCursorNodes();
        UpdateTargetScale();
    }

    private void UpdateTargetScale()
    {
        indicatorTarget.localScale = Vector3.Lerp(indicatorTarget.localScale, targetScale, Time.deltaTime * animationSpeed * 2);
    }

    void UpdateCursorNodes()
    {

        for (int i = 0; i < cursorNodes.Length; i++)
        {
            
            Transform cursorNode = cursorNodes[i];
            Transform targetNode = targetNodes[i];

            cursorNode.localPosition = Vector3.Lerp(cursorNode.localPosition, targetNode.localPosition, Time.deltaTime * animationSpeed);
            cursorNode.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(cursorNode.localEulerAngles.z, targetNode.localEulerAngles.z, Time.deltaTime * animationSpeed));
            cursorNode.localScale = Vector3.Lerp(cursorNode.localScale, targetNode.localScale, Time.deltaTime * animationSpeed);

        }

    }

    void Run() => MoveIndicatorsIncrementallyToTarget(cursorVisual.position, indicatorTarget.position, cursorVisual.localScale, indicatorTarget.localScale);

    [SerializeField] Transform indicators;
    void MoveIndicatorsIncrementallyToTarget(Vector3 fromPos, Vector3 toPos, Vector3 fromScale, Vector3 toScale)
    {
        if (indicators == null || indicators.childCount == 0)
            return;

        toPos.z = fromPos.z + 1f;

        int count = indicators.childCount;

        for (int i = 0; i < count; i++)
        {
            float t = (float)(i + 1) / (count + 1);

            Vector3 pos = Vector3.Lerp(fromPos, toPos, t);
            Vector3 scale = Vector3.Lerp(fromScale, toScale, t);
            Color color = Color.Lerp(targetNoPanningColor, smoothColor, t);

            Transform indicator = indicators.GetChild(i);
            indicator.position = pos;
            indicator.localScale = scale;
            indicatorSprites[i].color = color;
        }
    }

    void EnsureZDepth()
    {
        Vector3 cachedPos = Vector3.zero;

        cachedPos = transform.localPosition;
        cachedPos.z = 0;
        transform.localPosition = cachedPos;

        cachedPos = cursorVisual.localPosition;
        cachedPos.z = 0;
        cursorVisual.localPosition = cachedPos;

        cachedPos = indicators.localPosition;
        cachedPos.z = 0;
        indicators.localPosition = cachedPos;

        foreach (Transform indicator in indicators)
        {
            cachedPos = indicator.localPosition;
            cachedPos.z = 0;
            indicator.localPosition = cachedPos;
        }

    }

    Vector2 GetSnappedPosition(Vector2 rawPosition)
    {

        if(!dragAndScrollMod) return rawPosition;

        float x, y;
        x = Mathf.Round(rawPosition.x / dragAndScrollMod.Snapping) * dragAndScrollMod.Snapping;
        y = Mathf.Round(rawPosition.y / dragAndScrollMod.Snapping) * dragAndScrollMod.Snapping;

        return new Vector2(x, y);
    }

    public override void OnHideCursor()
    {
        onMouseMoved -= OnMouseMoved;
        onMouseClicked -= MouseClick;
    }

    public override void OnShowCursor()
    {
        dragAndScrollMod = FindAnyObjectByType<DragAndScrollMod>();
        onMouseMoved += OnMouseMoved;
        onMouseClicked += MouseClick;
    }
}