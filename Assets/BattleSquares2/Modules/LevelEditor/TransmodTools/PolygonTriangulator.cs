using System;
using System.Collections.Generic; 
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public static class PolygonTriangulator
{

    public static void Triangulate8(Vector3[] points, out Vector3[] vertices, out int[] indices)
    {
        if (points == null || points.Length != 8)
            throw new ArgumentException("Polygon must have exactly 8 points.");

        // Ensure winding order
        float area = 0f;
        for (int i = 0, j = 7; i < 8; j = i++)
            area += (points[j].x * points[i].y - points[i].x * points[j].y);

        bool ccw = area > 0f;
        int incr = 0;
        indices = new int[18]; // 8-2 = 6 triangles = 18 indices
        if (ccw)
        {
            for (int i = 1; i < 7; i++)
            {
                indices[incr++] = 0;
                indices[incr++] = i;
                indices[incr++] = i + 1;
            }
        }
        else
        {
            for (int i = 1; i < 7; i++)
            {
                indices[incr++] = 0;
                indices[incr++] = i + 1;
                indices[incr++] = i;
            }
        }

        vertices = points;
    }


    public static void Triangulate(Vector3[] points, out Vector3[] vertices, out int[] indices)
    {
        if (points == null || points.Length < 3)
            throw new ArgumentException("Polygon must have at least 3 points.");

        // Copy points into working list of indices
        List<int> V = new List<int>();
        for (int i = 0; i < points.Length; i++)
            V.Add(i);

        // Ensure winding order is counter-clockwise
        if (Area(points) < 0)
            V.Reverse();

        List<int> triangles = new List<int>();

        int count = 0; // Infinite loop guard
        while (V.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < V.Count; i++)
            {
                int prev = V[(i - 1 + V.Count) % V.Count];
                int curr = V[i];
                int next = V[(i + 1) % V.Count];

                if (IsEar(prev, curr, next, points, V))
                {
                    triangles.Add(prev);
                    triangles.Add(curr);
                    triangles.Add(next);
                    V.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound)
                throw new Exception("Failed to find an ear — polygon might be self-intersecting or malformed.");

            if (++count > 5000)
                throw new Exception("Triangulation infinite loop guard triggered.");
        }

        // Add the final triangle
        triangles.Add(V[0]);
        triangles.Add(V[1]);
        triangles.Add(V[2]);

        vertices = points;
        indices = triangles.ToArray();
    }
    public static void Triangulate(Vector2[] points, out Vector2[] vertices, out int[] indices)
    {
        if (points == null || points.Length < 3)
            throw new ArgumentException("Polygon must have at least 3 points.");

        // Copy points into working list of indices
        List<int> V = new List<int>();
        for (int i = 0; i < points.Length; i++)
            V.Add(i);

        // Ensure winding order is counter-clockwise
        if (Area(points) < 0)
            V.Reverse();

        List<int> triangles = new List<int>();

        int count = 0; // Infinite loop guard
        while (V.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < V.Count; i++)
            {
                int prev = V[(i - 1 + V.Count) % V.Count];
                int curr = V[i];
                int next = V[(i + 1) % V.Count];

                if (IsEar(prev, curr, next, points, V))
                {
                    triangles.Add(prev);
                    triangles.Add(curr);
                    triangles.Add(next);
                    V.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }

            if (!earFound)
                throw new Exception("Failed to find an ear — polygon might be self-intersecting or malformed.");

            if (++count > 5000)
                throw new Exception("Triangulation infinite loop guard triggered.");
        }

        // Add the final triangle
        triangles.Add(V[0]);
        triangles.Add(V[1]);
        triangles.Add(V[2]);

        vertices = points;
        indices = triangles.ToArray();
    }

    private static float Area(Vector3[] points)
    {
        float area = 0f;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
        {
            area += (points[j].x * points[i].y) - (points[i].x * points[j].y);
        }
        return area * 0.5f;
    }
    private static float Area(Vector2[] points)
    {
        float area = 0f;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
        {
            area += (points[j].x * points[i].y) - (points[i].x * points[j].y);
        }
        return area * 0.5f;
    }

    private static bool IsEar(int prev, int curr, int next, Vector3[] points, List<int> indices)
    {
        Vector2 a = points[prev];
        Vector2 b = points[curr];
        Vector2 c = points[next];

        if (Vector2.SignedAngle(b - a, c - a) <= 0) // Reflex vertex
            return false;

        // Check if any other vertex is inside the triangle
        for (int i = 0; i < indices.Count; i++)
        {
            int vi = indices[i];
            if (vi == prev || vi == curr || vi == next) continue;
            if (PointInTriangle(points[vi], a, b, c))
                return false;
        }

        return true;
    }
    private static bool IsEar(int prev, int curr, int next, Vector2[] points, List<int> indices)
    {
        Vector2 a = points[prev];
        Vector2 b = points[curr];
        Vector2 c = points[next];

        if (Vector2.SignedAngle(b - a, c - a) <= 0) // Reflex vertex
            return false;

        // Check if any other vertex is inside the triangle
        for (int i = 0; i < indices.Count; i++)
        {
            int vi = indices[i];
            if (vi == prev || vi == curr || vi == next) continue;
            if (PointInTriangle(points[vi], a, b, c))
                return false;
        }

        return true;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) -
               (p2.x - p3.x) * (p1.y - p3.y);
    }

    public static Vector2 GetSpritePivotLocal(Sprite sprite)
    {
        // Pivot in pixels from the bottom-left of the sprite rect
        Vector2 pivotPixels = sprite.pivot;

        // Convert to local units
        Vector2 pivotUnits = pivotPixels / sprite.pixelsPerUnit;

        // The sprite's local origin is the center of its rect
        Vector2 centerUnits = (sprite.rect.size / 2f) / sprite.pixelsPerUnit;

        // Offset from the local origin
        Vector2 localPivotOffset = pivotUnits - centerUnits;

        return localPivotOffset;
    }


    public static Vector2 GetCenterBounds(Vector2[] verts)
    {


        Vector2 min = verts[0], max = verts[0];
        for (int i = 1; i < verts.Length; i++)
        {
            min = Vector2.Min(min, verts[i]);
            max = Vector2.Max(max, verts[i]);
        }

        Vector2 center = min + max;
        return center;

    }

    [BurstCompile(FloatPrecision = floatPrecision, FloatMode = floatMode, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, OptimizeFor = optimizeFor)]
    public struct RasterizeJob : IJobParallelFor
    {

        const FloatPrecision floatPrecision = FloatPrecision.Low;
        const FloatMode floatMode = FloatMode.Fast;
        const OptimizeFor optimizeFor = OptimizeFor.Performance;
        const bool disableDirectCall = true;
        const bool disableSafetyChecks = true;

        [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<float2> points;
        [ReadOnly][NativeDisableParallelForRestriction] public NativeArray<int> indices;
        [ReadOnly] public float2 min;
        [ReadOnly] public float scale;
        [ReadOnly] public float2 meshSize;
        [ReadOnly] public int width;
        [ReadOnly] public int height;

        public NativeArray<byte> pixels;

        [BurstCompile(FloatPrecision = floatPrecision, FloatMode = floatMode, DisableDirectCall = disableDirectCall, DisableSafetyChecks = disableSafetyChecks, OptimizeFor = optimizeFor)]
        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;

            float2 p = new float2(x + 0.5f, y + 0.5f);
            byte filled = 0;

            // Check against all triangles
            for (int t = 0; t < indices.Length; t += 3)
            {
                float2 p0 = (points[indices[t]] - min) * scale;
                float2 p1 = (points[indices[t + 1]] - min) * scale;
                float2 p2 = (points[indices[t + 2]] - min) * scale;

                float2 offset = new float2(
                    (width - meshSize.x * scale) * 0.5f,
                    (height - meshSize.y * scale) * 0.5f
                );
                p0 += offset;
                p1 += offset;
                p2 += offset;

                bool4 boundingBoxCheck = new bool4
                    (
                        p.x < math.min(p0.x, math.min(p1.x, p2.x)),
                        p.x > math.max(p0.x, math.max(p1.x, p2.x)),
                        p.y < math.min(p0.y, math.min(p1.y, p2.y)),
                        p.y > math.max(p0.y, math.max(p1.y, p2.y))
                    );

                if (math.any(boundingBoxCheck)) continue;

                float area = (p2.x - p0.x) * (p1.y - p0.y) - (p2.y - p0.y) * (p1.x - p0.x);
                if (math.abs(area) < 1e-6f) continue;

                float w0 = (p.x - p1.x) * (p2.y - p1.y) - (p.y - p1.y) * (p2.x - p1.x);
                float w1 = (p.x - p2.x) * (p0.y - p2.y) - (p.y - p2.y) * (p0.x - p2.x);
                float w2 = (p.x - p0.x) * (p1.y - p0.y) - (p.y - p0.y) * (p1.x - p0.x);

                if (math.any(new bool2(math.all(new bool3
                    (
                        w0 >= 0,
                        w1 >= 0,
                        w2 >= 0
                    )), math.all(new bool3
                    (
                        w0 <= 0,
                        w1 <= 0,
                        w2 <= 0
                    )))))
                {
                    filled = 255;
                    break;
                }
            }
            pixels[index] = filled;
        }

    }

    public static unsafe byte[] RasterizeMeshDATA(Vector2[] pointsArr, int[] indicesArr, out bool v, out int width, out int height, out float ppu, float pixelsPerUnit = 100f)
    {
        width = 0; height = 0; ppu = 0; v = false;
        if (pointsArr.Length == 0) return new byte[0];
        if (pointsArr == null) return new byte[0];
        ppu = pixelsPerUnit;
        Vector2 min = pointsArr[0], max = pointsArr[0];
        for (int i = 1; i < pointsArr.Length; i++)
        {
            min = Vector2.Min(min, pointsArr[i]);
            max = Vector2.Max(max, pointsArr[i]);
        }

        Vector2 meshSize = max - min;
        width = Mathf.CeilToInt(meshSize.x * pixelsPerUnit);
        height = Mathf.CeilToInt(meshSize.y * pixelsPerUnit);
        int iterations = width * height;

        NativeArray<float2> points = new NativeArray<float2>(pointsArr.Length, Allocator.TempJob);
        for (int i = 0; i < points.Length; i++) points[i] = pointsArr[i];
        NativeArray<int> indices = new NativeArray<int>(indicesArr, Allocator.TempJob);
        NativeArray<byte> pixels = new NativeArray<byte>(iterations, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        var job = new RasterizeJob
        {
            points = points,
            indices = indices,
            min = min,
            scale = pixelsPerUnit,
            meshSize = meshSize,
            width = width,
            height = height,
            pixels = pixels
        };

        job.Schedule(iterations, iterations / (Environment.ProcessorCount * 4)).Complete();
        v = true;

        byte[] result = pixels.ToArray();

        points.Dispose();
        indices.Dispose();
        pixels.Dispose();

        return result;

    }

    public static unsafe Sprite RasterizeMesh(Vector2[] pointsArr, int[] indicesArr, float pixelsPerUnit = 100f)
    {

        Vector2 min = pointsArr[0], max = pointsArr[0];
        for (int i = 1; i < pointsArr.Length; i++)
        {
            min = Vector2.Min(min, pointsArr[i]);
            max = Vector2.Max(max, pointsArr[i]);
        }

        Vector2 meshSize = max - min;
        int width = Mathf.CeilToInt(meshSize.x * pixelsPerUnit);
        int height = Mathf.CeilToInt(meshSize.y * pixelsPerUnit);

        NativeArray<float2> points = new NativeArray<float2>(pointsArr.Length, Allocator.TempJob);
        for (int i = 0; i < points.Length; i++) points[i] = pointsArr[i];
        NativeArray<int> indices = new NativeArray<int>(indicesArr, Allocator.TempJob);
        NativeArray<byte> pixels = new NativeArray<byte>(width * height, Allocator.TempJob, NativeArrayOptions.ClearMemory);

        var job = new RasterizeJob
        {
            points = points,
            indices = indices,
            min = min,
            scale = pixelsPerUnit,
            meshSize = meshSize,
            width = width,
            height = height,
            pixels = pixels
        };

        int iterations = width * height;

        JobHandle handle = job.Schedule(iterations, iterations / (Environment.ProcessorCount * 2));

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        handle.Complete();

        tex.SetPixelData(pixels, 0, 0);
        tex.Apply();

        points.Dispose();
        pixels.Dispose();
        indices.Dispose();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

}
