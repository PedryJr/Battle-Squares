using UnityEngine;

public sealed class DefaultCursor : ModularCursor
{

    Transform cachedTransform;

    protected override void Awake()
    {
        base.Awake();
        cachedTransform = transform;
    }

    void OnMouseMoved(MouseMovementData data)
    {
        Vector3 worldPosition = data.worldPosition;
        worldPosition.z = cachedTransform.position.z;
        cachedTransform.position = worldPosition;
    }

    public override void OnHideCursor() => onMouseMoved -= OnMouseMoved;

    public override void OnShowCursor() => onMouseMoved += OnMouseMoved;
}