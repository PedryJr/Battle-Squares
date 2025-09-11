using System.Runtime.CompilerServices;
using UnityEngine;
using static AnimationAnchor;

public class LevelAnimationGroup : MonoBehaviour
{
    bool constructed = false;
    Spline2D animationPath;

    public float animationTimer = 0;
    public float animationSpeed;
    public float animationOffset;

    public void ConstructComplex(ComplexAnimationData data)
    {
        float keepZ = transform.position.z;
        Vector3 startPosition = data.segmentCoords[0];
        startPosition.z = keepZ;
        transform.position = startPosition;
        animationSpeed = data.animationSpeed;
        animationOffset = data.animationOffset;
        animationPath = new Spline2D(data.segmentCoords);
        constructed = true;
    }

    private void Update()
    {
        if (!constructed) return;

        animationTimer += Time.deltaTime * animationSpeed;
        float eval = Mathf.Repeat(animationTimer + animationOffset, 1f);
        Vector2 evalPosition = animationPath.Evaluate(eval);
        float keepZ = transform.position.z;
        Vector3 animatedPosition = evalPosition;
        animatedPosition.z = keepZ;
        transform.position = animatedPosition;
    }

}

[System.Serializable]
public struct Spline2D
{
    [System.Serializable]
    private struct BezierSegment
    {
        public Vector2 P0, P1, P2, P3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BezierSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            P0 = p0; P1 = p1; P2 = p2; P3 = p3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 Evaluate(float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * P0) +
                   (3f * uu * t * P1) +
                   (3f * u * tt * P2) +
                   (ttt * P3);
        }
    }

    private BezierSegment[] _segments;
    private float _cachedLength;
    private float[] _arcLengths;

    /// <summary>
    /// Creates a spline from a linked array of control points.
    /// Valid counts: 4, 7, 10, 13... (4 + 3*(n-1)).
    /// </summary>
    public Spline2D(Vector2[] controlPoints)
    {
        if (controlPoints == null || controlPoints.Length < 4)
        {
            _segments = new BezierSegment[0];
            _cachedLength = 0f;
            _arcLengths = System.Array.Empty<float>();
            return;
        }

        // each extra segment contributes 3 points
        int segCount = 1 + (controlPoints.Length - 4) / 3;
        _segments = new BezierSegment[segCount];

        int idx = 0;
        for (int i = 0; i < segCount; i++)
        {
            _segments[i] = new BezierSegment(
                controlPoints[idx + 0],
                controlPoints[idx + 1],
                controlPoints[idx + 2],
                controlPoints[idx + 3]
            );
            idx += 3; // advance by 3 to reuse the shared endpoint
        }

        _cachedLength = 0f;
        _arcLengths = null;
        CacheArcLength(512);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Evaluate(float t)
    {
        if (_segments == null || _segments.Length == 0) return Vector2.zero;
        if (_segments.Length == 1) return _segments[0].Evaluate(Mathf.Clamp01(t));

        t = Mathf.Clamp01(t);
        float scaledT = t * _segments.Length;
        int seg = Mathf.Min(_segments.Length - 1, Mathf.FloorToInt(scaledT));
        float localT = scaledT - seg;
        return _segments[seg].Evaluate(localT);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 EvaluateEven(float t)
    {
        if (_arcLengths == null || _arcLengths.Length == 0) return Evaluate(t);

        float target = t * _cachedLength;

        int lo = 0, hi = _arcLengths.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_arcLengths[mid] < target) lo = mid + 1;
            else hi = mid;
        }

        float segmentT = (float)lo / (_arcLengths.Length - 1);
        return Evaluate(segmentT);
    }

    private void CacheArcLength(int samples)
    {
        if (_segments == null || _segments.Length == 0)
        {
            _arcLengths = new float[0];
            _cachedLength = 0f;
            return;
        }

        _arcLengths = new float[samples + 1];
        _arcLengths[0] = 0f;

        Vector2 prev = Evaluate(0f);
        float length = 0f;

        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector2 p = Evaluate(t);
            length += Vector2.Distance(prev, p);
            _arcLengths[i] = length;
            prev = p;
        }

        _cachedLength = length;
    }
}


