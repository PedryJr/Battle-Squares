using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class LocalSnappingPoint : MonoBehaviour
{

    float zRot;

    float snapping;
    Vector2 rawWorldPosition;
    LocalSnappingPoint otherLocal;
    bool start = false;

    bool notifyChange = false;
    public bool HasChanged => notifyChange;

    ShapeContainer parentContainer;

    [SerializeField]
    Transform[] localHalfOctagonPoints;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignShapeContainer(ShapeContainer shapeContainer)
    {
        parentContainer = shapeContainer;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignrawWorldPositionPosition(Vector2 rawWorldPosition)
    {
        if(otherLocal) otherLocal.notifyChange = true;
        notifyChange = true;
        this.rawWorldPosition = rawWorldPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignLookatTarget(Transform otherLocal)
    {
        notifyChange = true;
        this.otherLocal = otherLocal.GetComponent<LocalSnappingPoint>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignSnapping(float snapping)
    {
        notifyChange = true;
        this.snapping = snapping;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignStart(bool start)
    {
        notifyChange = true;
        this.start = start;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        if (otherLocal) UpdateRotationAndPosition();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void UpdateRotationAndPosition()
    {
        transform.position = GetSnappedPosition(rawWorldPosition);
        zRot = GetFromToZRotation();
        transform.rotation = Quaternion.Euler(0f, 0f, zRot);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Vector2 GetSnappedPosition(Vector2 rawPosition)
    {

        float x, y;
        x = Mathf.Round(rawPosition.x / snapping) * snapping;
        y = Mathf.Round(rawPosition.y / snapping) * snapping;

        return new Vector2(x, y);
    }

    [SerializeField]
    float scaleOffset = 0f;
    [MethodImpl(512)]
    float GetSnappedScalar(float scalar)
    {
        float angle = GetFromToZRotation();
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

        Vector2 worldOffset = dir * scalar;

        worldOffset.x = Mathf.Round(worldOffset.x / snapping) * snapping;
        worldOffset.y = Mathf.Round(worldOffset.y / snapping) * snapping;

        float snappedScalar = Vector2.Dot(worldOffset, dir.normalized);

        return snappedScalar;
    }
    [MethodImpl(512)]
    public float AdjustScale(float delta)
    {
        notifyChange = true;
        scaleOffset += delta; // keep unsnapped value
        scaleOffset = Mathf.Clamp(scaleOffset, 0f, 15f);
        return scaleOffset;
    }

    [MethodImpl(512)]
    public float HardAdjustScale(float val)
    {
        notifyChange = true;
        scaleOffset = val; // keep unsnapped value
        scaleOffset = Mathf.Clamp(scaleOffset, 0f, 15f);
        return scaleOffset;
    }
    [MethodImpl(512)]
    public float GetFromToZRotation()
    {
        Vector2 delta = otherLocal.transform.position - transform.position;
        if (delta.magnitude <= float.Epsilon) return start ? 0 : 180;
        float z = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        return z;
    }


    [ContextMenu("NotifyChange")] void NC() => notifyChange = true;

    [MethodImpl(512)]
    public Vector3[] GetLocalPoints(Transform inverse)
    {

        float snappedScale = GetSnappedScalar(scaleOffset);
        Vector3[] scaleOffsets = new Vector3[localHalfOctagonPoints.Length];
        Vector3[] points = new Vector3[localHalfOctagonPoints.Length];

        for (int i = 0; i < points.Length; i++)
        {
            if(i < 2) scaleOffsets[i] = transform.up * snappedScale;
            else scaleOffsets[i] = -transform.up * snappedScale;

            points[i] = inverse.InverseTransformPoint(localHalfOctagonPoints[i].position + scaleOffsets[i]);
        }
        notifyChange = false;
        return points;

    }

}
