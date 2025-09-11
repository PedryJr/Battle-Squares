using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Standalone cubic Bezier spline with rendering support.
/// Does not inherit MonoBehaviour.
/// </summary>
public sealed class SplineRenderer
{
    private sealed class BezierSegment
    {
        public Transform P0; // start
        public Transform P1; // control 1
        public Transform P2; // control 2
        public Transform P3; // end
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BezierSegment(Transform p0, Transform p1, Transform p2, Transform p3)
        {
            P0 = p0; P1 = p1; P2 = p2; P3 = p3;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDistance() => Vector3.Distance(P0.position, P1.position);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Evaluate(float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * P0.position) +
                   (3f * uu * t * P1.position) +
                   (3f * u * tt * P2.position) +
                   (ttt * P3.position);
        }
    }

    private readonly List<BezierSegment> _segments = new List<BezierSegment>();
    private Mesh _mesh;
    private float _cachedLength;
    private readonly List<float> _arcLengths = new List<float>();
    private Bounds _bounds;

    /// <summary>
    /// Adds a spline segment defined by 3 transforms: center, left, right.
    /// Convention: center = anchor, left/right = curve handles.
    /// Next center is the end point.
    /// </summary>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSegment(Transform start, Transform handle1, Transform handle2, Transform end)
    {
        _segments.Add(new BezierSegment(start, handle1, handle2, end));
        CacheArcLength(8192);
    }

    /// <summary>
    /// Clears all spline segments.
    /// </summary>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _segments.Clear();
        _arcLengths.Clear();
        if (_mesh != null)
        {
            Object.Destroy(_mesh);
            _mesh = null;
        }
    }

    /// <summary>
    /// Evaluate spline position at t in [0,1].
    /// If multiple segments, t spans across them evenly.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Evaluate(float t)
    {
        if (_segments.Count == 0) return Vector3.zero;
        if (_segments.Count == 1) return _segments[0].Evaluate(t);

        t = Mathf.Clamp01(t);
        float scaledT = t * _segments.Count;
        int seg = Mathf.Min(_segments.Count - 1, Mathf.FloorToInt(scaledT));
        float localT = scaledT - seg;
        return _segments[seg].Evaluate(localT);
    }

    /// <summary>
    /// Evaluate evenly along spline arc length.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 EvaluateEven(float t)
    {
        if (_arcLengths.Count == 0) return Evaluate(t);

        float target = t * _cachedLength;

        // binary search through arc lengths
        int lo = 0, hi = _arcLengths.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_arcLengths[mid] < target) lo = mid + 1;
            else hi = mid;
        }

        float segmentT = (float)lo / (_arcLengths.Count - 1);
        return Evaluate(segmentT);
    }

    /// <summary>
    /// Rebuilds arc length table for even evaluation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CacheArcLength(int samples = 100)
    {
        _arcLengths.Clear();
        _arcLengths.Add(0f);

        Vector3 prev = Evaluate(0f);
        float length = 0f;

        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 p = Evaluate(t);
            length += Vector3.Distance(prev, p);
            _arcLengths.Add(length);
            prev = p;
        }

        _cachedLength = length;
    }


    List<int> indices = new List<int>();
    /// <summary>
    /// Renders spline as a polyline (simple) with given material.
    /// For ribbon/mesh, expand this to use normals/edges.
    /// </summary>

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Render(in RenderParams renderParams, Material material, int resolution = 32)
    {
        if (_segments.Count == 0) return;
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 512);
        }

        indices.Clear();
        int indicesLength = 0;

        NativeArray<float3> verts = new NativeArray<float3>(resolution + 1, Allocator.Temp);

        for (int i = 0; i <= resolution; i++)
        {
            verts[i] = Evaluate((float)i / resolution);
            if (i > 0)
            {
                indicesLength += 2;
                indices.Add(i - 1);
                indices.Add(i);
            }
        }
        UnityEngine.Rendering.MeshUpdateFlags flags =
UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds |
UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices |
UnityEngine.Rendering.MeshUpdateFlags.DontResetBoneBounds |
UnityEngine.Rendering.MeshUpdateFlags.DontNotifyMeshUsers;
        _mesh.SetVertices(verts, 0, verts.Length, flags);
        _mesh.SetIndices(indices, 0, indicesLength, MeshTopology.LineStrip, 0, false, 0);
        Graphics.RenderMesh(renderParams, _mesh, 0, Matrix4x4.identity);
        verts.Dispose();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetDistance()
    {
        float distance = 1.0f;
        _segments.ForEach(seg => distance += seg.GetDistance());
        return Mathf.RoundToInt(distance);
    }
}
