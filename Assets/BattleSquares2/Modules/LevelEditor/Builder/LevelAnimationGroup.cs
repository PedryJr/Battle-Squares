using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using static AnimationAnchor;

public sealed class LevelAnimationGroup : MonoBehaviour
{
    bool constructed = false;
    Spline2D animationPath;

    public float animationTimer = 0;
    public float animationSpeed;
    public float animationOffset;

    Transform cachedTransform;
    Rigidbody2D rb;

    List<PlayerBehaviour> playersOnShape;

    private void Awake()
    {
        playersOnShape = new List<PlayerBehaviour>();
        rb = GetComponent<Rigidbody2D>();
        cachedTransform = transform;
    }

    void Start()
    {
        Rigidbody2D[] childBodies = GetComponentsInChildren<Rigidbody2D>();
        for (int i = 0; i < childBodies.Length; i++) if (childBodies[i] != rb) Destroy(childBodies[i]);
    }

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

    void FixedUpdate()
    {
        if (!constructed) return;

        animationTimer = NetworkManager.Singleton.ServerTime.TimeAsFloat * animationSpeed;

        Vector2 targetPosition = animationPath.Evaluate(Mathf.Repeat(animationTimer + animationOffset, 1f));

        MoveToward(targetPosition);
    }

    private void Update()
    {
        if (!constructed) return;

        animationTimer = NetworkManager.Singleton.ServerTime.TimeAsFloat * animationSpeed;

        Vector2 targetPosition = animationPath.Evaluate(Mathf.Repeat(animationTimer + animationOffset, 1f));

        transform.position = targetPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out PlayerBehaviour playerBehaviour)) return;
        if (!playersOnShape.Contains(playerBehaviour)) playersOnShape.Add(playerBehaviour);
        
        VLog.Log($"&a{playerBehaviour.playerName}");
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.TryGetComponent(out PlayerBehaviour playerBehaviour)) return;
        if (playersOnShape.Contains(playerBehaviour)) playersOnShape.Remove(playerBehaviour);
        VLog.Log($"&e{playerBehaviour.playerName}");
        
    }

    void MoveToward(Vector2 targetPosition)
    {
        Vector2 delta = targetPosition - rb.position;
        rb.linearVelocity = delta / Time.fixedDeltaTime / 2f;
        rb.position = targetPosition;
        foreach (PlayerBehaviour player in playersOnShape)
        {
            player.rb.position = player.rb.position + delta;
        }
    }


}

[BurstCompile]
[System.Serializable]
public struct Spline2D
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance, DisableSafetyChecks = true)]
    [System.Serializable]
    private struct BezierSegment
    {
        public float2 P0, P1, P2, P3;
        private float2 _unused;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BezierSegment(float2 p0, float2 p1, float2 p2, float2 p3)
        {
            P0 = p0; P1 = p1; P2 = p2; P3 = p3;
            _unused = float2.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Evaluate(float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            _unused = (uuu * P0) + (3f * uu * t * P1) + (3f * u * tt * P2) + (ttt * P3);
            return _unused;
        }
    }

    private BezierSegment[] _segments;
    private float _cachedLength;
    private float[] _arcLengths;

    public Spline2D(Vector2[] controlPoints)
    {
        if (controlPoints == null || controlPoints.Length < 4)
        {
            _segments = new BezierSegment[0];
            _cachedLength = 0f;
            _arcLengths = System.Array.Empty<float>();
            return;
        }

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
            idx += 3;
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


