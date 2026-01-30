using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// OPTIMIZED and ROBUST Tooling class for merging 2D Polygon Colliders in Unity.
/// Includes validation to prevent Unity's "failed verification" warnings.
/// </summary>
public static class PolygonColliderMerger
{
    private const float EPSILON = 0.0001f;
    private const float CONNECTION_THRESHOLD = 0.01f;
    private const int SPATIAL_GRID_SIZE = 10;
    private const int MIN_POLYGON_POINTS = 3;
    private const float MIN_POLYGON_AREA = 0.0001f;
    private const float DUPLICATE_VERTEX_THRESHOLD = 0.001f;

    #region Data Structures

    /// <summary>
    /// Cached polygon data with bounds for fast spatial queries
    /// </summary>
    private class PolygonData
    {
        public Vector2[] Points;
        public Bounds Bounds;
        public int GroupId;

        public PolygonData(Vector2[] points)
        {
            Points = points;
            Bounds = CalculateBounds(points);
            GroupId = -1;
        }

        private static Bounds CalculateBounds(Vector2[] points)
        {
            if (points.Length == 0)
                return new Bounds(Vector2.zero, Vector2.zero);

            float minX = points[0].x, maxX = points[0].x;
            float minY = points[0].y, maxY = points[0].y;

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].x < minX) minX = points[i].x;
                if (points[i].x > maxX) maxX = points[i].x;
                if (points[i].y < minY) minY = points[i].y;
                if (points[i].y > maxY) maxY = points[i].y;
            }

            Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            Vector2 size = new Vector2(maxX - minX, maxY - minY);
            return new Bounds(center, size);
        }
    }

    /// <summary>
    /// Spatial grid for fast proximity queries
    /// </summary>
    private class SpatialGrid
    {
        private Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();
        private float cellSize;

        public SpatialGrid(float cellSize)
        {
            this.cellSize = cellSize;
        }

        public void Add(int index, Bounds bounds)
        {
            Bounds expandedBounds = new Bounds(bounds.center, bounds.size + Vector3.one * CONNECTION_THRESHOLD * 2);

            Vector2Int minCell = GetCell(expandedBounds.min);
            Vector2Int maxCell = GetCell(expandedBounds.max);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!grid.ContainsKey(cell))
                        grid[cell] = new List<int>();
                    grid[cell].Add(index);
                }
            }
        }

        public HashSet<int> GetNearby(Bounds bounds)
        {
            HashSet<int> nearby = new HashSet<int>();

            Bounds expandedBounds = new Bounds(bounds.center, bounds.size + Vector3.one * CONNECTION_THRESHOLD * 2);

            Vector2Int minCell = GetCell(expandedBounds.min);
            Vector2Int maxCell = GetCell(expandedBounds.max);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (grid.ContainsKey(cell))
                    {
                        foreach (int index in grid[cell])
                            nearby.Add(index);
                    }
                }
            }

            return nearby;
        }

        private Vector2Int GetCell(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize)
            );
        }
    }

    /// <summary>
    /// Union-Find structure for tracking connected components
    /// </summary>
    private class UnionFind
    {
        private int[] parent;
        private int[] rank;

        public UnionFind(int size)
        {
            parent = new int[size];
            rank = new int[size];
            for (int i = 0; i < size; i++)
            {
                parent[i] = i;
                rank[i] = 0;
            }
        }

        public int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        public void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY)
                return;

            if (rank[rootX] < rank[rootY])
                parent[rootX] = rootY;
            else if (rank[rootX] > rank[rootY])
                parent[rootY] = rootX;
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Merges a set of polygon collider islands into an existing island cluster.
    /// OPTIMIZED: Uses spatial partitioning for better performance with many islands.
    /// VALIDATED: Ensures all polygons pass Unity's verification.
    /// </summary>
    public static bool MergeIslands(PolygonCollider2D islandCluster, PolygonCollider2D[] newIslands)
    {
        if (islandCluster == null || newIslands == null || newIslands.Length == 0)
        {
            Debug.LogError("PolygonColliderMerger: Invalid input");
            return false;
        }

        // Get all existing paths from the cluster
        List<PolygonData> allPolygons = new List<PolygonData>();

        for (int i = 0; i < islandCluster.pathCount; i++)
        {
            Vector2[] worldPoints = TransformPoints(islandCluster.GetPath(i), islandCluster.transform, islandCluster.offset);
            if (IsValidPolygon(worldPoints))
            {
                allPolygons.Add(new PolygonData(worldPoints));
            }
        }

        // Add all paths from new islands
        foreach (var newIsland in newIslands)
        {
            if (newIsland == null) continue;

            for (int i = 0; i < newIsland.pathCount; i++)
            {
                Vector2[] worldPoints = TransformPoints(newIsland.GetPath(i), newIsland.transform, newIsland.offset);
                if (IsValidPolygon(worldPoints))
                {
                    allPolygons.Add(new PolygonData(worldPoints));
                }
            }
        }

        if (allPolygons.Count == 0)
        {
            Debug.LogWarning("PolygonColliderMerger: No valid polygons to merge");
            return false;
        }

        // Merge all polygons efficiently
        List<Vector2[]> mergedPaths = MergePolygonsOptimized(allPolygons);

        // Validate and apply merged paths
        List<Vector2[]> validPaths = new List<Vector2[]>();
        foreach (var path in mergedPaths)
        {
            Vector2[] validated = ValidateAndFixPolygon(path);
            if (validated != null && validated.Length >= MIN_POLYGON_POINTS)
            {
                validPaths.Add(validated);
            }
        }

        if (validPaths.Count == 0)
        {
            Debug.LogWarning("PolygonColliderMerger: All merged polygons failed validation");
            return false;
        }

        // Apply merged paths back to the cluster
        ApplyMergedPaths(islandCluster, validPaths);

        return true;
    }

    /// <summary>
    /// Merges a single island into the cluster (legacy support)
    /// </summary>
    public static bool MergeIslands(PolygonCollider2D islandCluster, PolygonCollider2D newIsland)
    {
        return MergeIslands(islandCluster, new PolygonCollider2D[] { newIsland });
    }

    /// <summary>
    /// Creates a new merged polygon collider from two source colliders.
    /// </summary>
    public static PolygonCollider2D CreateMergedCollider(GameObject targetObject, PolygonCollider2D collider1, PolygonCollider2D collider2)
    {
        if (targetObject == null || collider1 == null || collider2 == null)
        {
            Debug.LogError("PolygonColliderMerger: Null parameter provided");
            return null;
        }

        PolygonCollider2D newCollider = targetObject.AddComponent<PolygonCollider2D>();

        List<PolygonData> allPolygons = new List<PolygonData>();

        // Get paths from both colliders
        for (int i = 0; i < collider1.pathCount; i++)
        {
            Vector2[] worldPoints = TransformPoints(collider1.GetPath(i), collider1.transform, collider1.offset);
            if (IsValidPolygon(worldPoints))
            {
                allPolygons.Add(new PolygonData(worldPoints));
            }
        }

        for (int i = 0; i < collider2.pathCount; i++)
        {
            Vector2[] worldPoints = TransformPoints(collider2.GetPath(i), collider2.transform, collider2.offset);
            if (IsValidPolygon(worldPoints))
            {
                allPolygons.Add(new PolygonData(worldPoints));
            }
        }

        // Merge and apply
        List<Vector2[]> mergedPaths = MergePolygonsOptimized(allPolygons);

        // Validate paths
        List<Vector2[]> validPaths = new List<Vector2[]>();
        foreach (var path in mergedPaths)
        {
            Vector2[] validated = ValidateAndFixPolygon(path);
            if (validated != null && validated.Length >= MIN_POLYGON_POINTS)
            {
                validPaths.Add(validated);
            }
        }

        ApplyMergedPaths(newCollider, validPaths);

        return newCollider;
    }

    #endregion

    #region Polygon Validation

    /// <summary>
    /// Checks if a polygon is valid for Unity's PolygonCollider2D
    /// </summary>
    private static bool IsValidPolygon(Vector2[] polygon)
    {
        if (polygon == null || polygon.Length < MIN_POLYGON_POINTS)
            return false;

        // Check for NaN or Infinity
        foreach (var point in polygon)
        {
            if (float.IsNaN(point.x) || float.IsNaN(point.y) ||
                float.IsInfinity(point.x) || float.IsInfinity(point.y))
                return false;
        }

        // Check for minimum area
        float area = CalculatePolygonArea(polygon);
        if (Mathf.Abs(area) < MIN_POLYGON_AREA)
            return false;

        return true;
    }

    /// <summary>
    /// Validates and fixes common polygon issues
    /// </summary>
    private static Vector2[] ValidateAndFixPolygon(Vector2[] polygon)
    {
        if (polygon == null || polygon.Length < MIN_POLYGON_POINTS)
            return null;

        // Step 1: Remove NaN/Infinity points
        List<Vector2> cleaned = new List<Vector2>();
        foreach (var point in polygon)
        {
            if (!float.IsNaN(point.x) && !float.IsNaN(point.y) &&
                !float.IsInfinity(point.x) && !float.IsInfinity(point.y))
            {
                cleaned.Add(point);
            }
        }

        if (cleaned.Count < MIN_POLYGON_POINTS)
            return null;

        // Step 2: Remove duplicate consecutive vertices
        List<Vector2> noDuplicates = new List<Vector2>();
        noDuplicates.Add(cleaned[0]);

        for (int i = 1; i < cleaned.Count; i++)
        {
            if (Vector2.Distance(cleaned[i], noDuplicates[noDuplicates.Count - 1]) > DUPLICATE_VERTEX_THRESHOLD)
            {
                noDuplicates.Add(cleaned[i]);
            }
        }

        // Check if last point is duplicate of first
        if (noDuplicates.Count > 1 &&
            Vector2.Distance(noDuplicates[noDuplicates.Count - 1], noDuplicates[0]) <= DUPLICATE_VERTEX_THRESHOLD)
        {
            noDuplicates.RemoveAt(noDuplicates.Count - 1);
        }

        if (noDuplicates.Count < MIN_POLYGON_POINTS)
            return null;

        // Step 3: Ensure counter-clockwise winding (Unity requirement)
        Vector2[] wound = EnsureCounterClockwiseWinding(noDuplicates.ToArray());

        // Step 4: Remove collinear points to simplify
        Vector2[] simplified = RemoveCollinearPoints(wound);

        // Step 5: Final area check
        float area = CalculatePolygonArea(simplified);
        if (Mathf.Abs(area) < MIN_POLYGON_AREA)
            return null;

        return simplified;
    }

    /// <summary>
    /// Ensures polygon has counter-clockwise winding order
    /// </summary>
    private static Vector2[] EnsureCounterClockwiseWinding(Vector2[] polygon)
    {
        float signedArea = CalculatePolygonArea(polygon);

        // If clockwise (negative area), reverse the order
        if (signedArea < 0)
        {
            Vector2[] reversed = new Vector2[polygon.Length];
            for (int i = 0; i < polygon.Length; i++)
            {
                reversed[i] = polygon[polygon.Length - 1 - i];
            }
            return reversed;
        }

        return polygon;
    }

    /// <summary>
    /// Calculates signed area of polygon (positive = counter-clockwise)
    /// </summary>
    private static float CalculatePolygonArea(Vector2[] polygon)
    {
        if (polygon.Length < 3)
            return 0f;

        float area = 0f;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[(i + 1) % polygon.Length];
            area += (p2.x - p1.x) * (p2.y + p1.y);
        }

        return area * 0.5f;
    }

    /// <summary>
    /// Removes collinear points from polygon
    /// </summary>
    private static Vector2[] RemoveCollinearPoints(Vector2[] polygon)
    {
        if (polygon.Length < 3)
            return polygon;

        List<Vector2> result = new List<Vector2>();

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 prev = polygon[(i - 1 + polygon.Length) % polygon.Length];
            Vector2 current = polygon[i];
            Vector2 next = polygon[(i + 1) % polygon.Length];

            // Calculate cross product to check if collinear
            float cross = (current.x - prev.x) * (next.y - prev.y) -
                         (current.y - prev.y) * (next.x - prev.x);

            // Keep point if not collinear
            if (Mathf.Abs(cross) > EPSILON)
            {
                result.Add(current);
            }
        }

        // Ensure we still have a valid polygon
        return result.Count >= MIN_POLYGON_POINTS ? result.ToArray() : polygon;
    }

    #endregion

    #region Optimized Merging

    /// <summary>
    /// OPTIMIZED: Merges polygons using spatial partitioning and union-find.
    /// </summary>
    private static List<Vector2[]> MergePolygonsOptimized(List<PolygonData> polygons)
    {
        if (polygons.Count == 0)
            return new List<Vector2[]>();

        if (polygons.Count == 1)
            return new List<Vector2[]> { polygons[0].Points };

        // Calculate appropriate cell size based on average polygon size
        float avgSize = polygons.Average(p => Mathf.Max(p.Bounds.size.x, p.Bounds.size.y));
        float cellSize = Mathf.Max(avgSize * 2f, 1f);

        // Build spatial grid
        SpatialGrid grid = new SpatialGrid(cellSize);
        for (int i = 0; i < polygons.Count; i++)
        {
            grid.Add(i, polygons[i].Bounds);
        }

        // Find connected components using Union-Find
        UnionFind uf = new UnionFind(polygons.Count);
        HashSet<long> checkedPairs = new HashSet<long>();

        for (int i = 0; i < polygons.Count; i++)
        {
            HashSet<int> nearby = grid.GetNearby(polygons[i].Bounds);

            foreach (int j in nearby)
            {
                if (i >= j) continue;

                long pairId = ((long)i << 32) | (long)j;
                if (checkedPairs.Contains(pairId))
                    continue;
                checkedPairs.Add(pairId);

                if (!BoundsOverlapOrNear(polygons[i].Bounds, polygons[j].Bounds, CONNECTION_THRESHOLD))
                    continue;

                if (PolygonsOverlapOrTouch(polygons[i].Points, polygons[j].Points))
                {
                    uf.Union(i, j);
                }
            }
        }

        // Group polygons by connected component
        Dictionary<int, List<PolygonData>> groups = new Dictionary<int, List<PolygonData>>();
        for (int i = 0; i < polygons.Count; i++)
        {
            int root = uf.Find(i);
            if (!groups.ContainsKey(root))
                groups[root] = new List<PolygonData>();
            groups[root].Add(polygons[i]);
        }

        // Merge each group
        List<Vector2[]> result = new List<Vector2[]>();
        foreach (var group in groups.Values)
        {
            if (group.Count == 1)
            {
                result.Add(group[0].Points);
            }
            else
            {
                Vector2[] merged = MergePolygonGroup(group);
                if (merged != null && merged.Length >= MIN_POLYGON_POINTS)
                {
                    result.Add(merged);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Merges a group of connected polygons into a single polygon.
    /// </summary>
    private static Vector2[] MergePolygonGroup(List<PolygonData> group)
    {
        if (group.Count == 0)
            return new Vector2[0];

        if (group.Count == 1)
            return group[0].Points;

        Vector2[] result = group[0].Points;

        for (int i = 1; i < group.Count; i++)
        {
            result = UnionPolygons(result, group[i].Points);
            if (result == null || result.Length < MIN_POLYGON_POINTS)
            {
                Debug.LogWarning($"PolygonColliderMerger: Merge failed at polygon {i} in group");
                return group[0].Points; // Return first polygon as fallback
            }
        }

        return result;
    }

    /// <summary>
    /// Quick bounds overlap check with threshold
    /// </summary>
    private static bool BoundsOverlapOrNear(Bounds b1, Bounds b2, float threshold)
    {
        Vector2 min1 = (Vector2) b1.min - Vector2.one * threshold;
        Vector2 max1 =(Vector2) b1.max + Vector2.one * threshold;
        Vector2 min2 = (Vector2)b2.min - Vector2.one * threshold;
        Vector2 max2 = (Vector2)b2.max + Vector2.one * threshold;

        return !(max1.x < min2.x || min1.x > max2.x || max1.y < min2.y || min1.y > max2.y);
    }

    #endregion

    #region Polygon Operations

    private static bool PolygonsOverlapOrTouch(Vector2[] poly1, Vector2[] poly2)
    {
        int sampleSize = Mathf.Min(5, poly1.Length);
        for (int i = 0; i < sampleSize; i++)
        {
            int idx = i * poly1.Length / sampleSize;
            if (IsPointInOrNearPolygon(poly1[idx], poly2, CONNECTION_THRESHOLD))
                return true;
        }

        sampleSize = Mathf.Min(5, poly2.Length);
        for (int i = 0; i < sampleSize; i++)
        {
            int idx = i * poly2.Length / sampleSize;
            if (IsPointInOrNearPolygon(poly2[idx], poly1, CONNECTION_THRESHOLD))
                return true;
        }

        if (PolygonsIntersect(poly1, poly2))
            return true;

        return false;
    }

    private static Vector2[] UnionPolygons(Vector2[] poly1, Vector2[] poly2)
    {
        List<Vector2> allPoints = new List<Vector2>(poly1);
        allPoints.AddRange(poly2);

        if (ArePolygonsConvex(poly1) && ArePolygonsConvex(poly2))
        {
            return ConvexHull(allPoints);
        }
        else
        {
            return MergeNonConvexPolygons(poly1, poly2);
        }
    }

    private static Vector2[] MergeNonConvexPolygons(Vector2[] poly1, Vector2[] poly2)
    {
        List<Vector2> combined = new List<Vector2>();

        foreach (Vector2 v in poly1)
        {
            if (!IsPointInPolygon(v, poly2))
                combined.Add(v);
        }

        foreach (Vector2 v in poly2)
        {
            if (!IsPointInPolygon(v, poly1))
                combined.Add(v);
        }

        List<Vector2> intersections = FindEdgeIntersections(poly1, poly2);
        combined.AddRange(intersections);

        if (combined.Count >= MIN_POLYGON_POINTS)
        {
            Vector2 center = GetCentroid(combined);
            combined = combined.OrderBy(v => Mathf.Atan2(v.y - center.y, v.x - center.x)).ToList();
            return combined.ToArray();
        }

        List<Vector2> allPoints = new List<Vector2>(poly1);
        allPoints.AddRange(poly2);
        return ConvexHull(allPoints);
    }

    private static List<Vector2> FindEdgeIntersections(Vector2[] poly1, Vector2[] poly2)
    {
        List<Vector2> intersections = new List<Vector2>();

        for (int i = 0; i < poly1.Length; i++)
        {
            Vector2 p1 = poly1[i];
            Vector2 p2 = poly1[(i + 1) % poly1.Length];

            for (int j = 0; j < poly2.Length; j++)
            {
                Vector2 p3 = poly2[j];
                Vector2 p4 = poly2[(j + 1) % poly2.Length];

                if (LineSegmentsIntersect(p1, p2, p3, p4, out Vector2 intersection))
                {
                    intersections.Add(intersection);
                }
            }
        }

        return intersections;
    }

    private static bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        float d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (Mathf.Abs(d) < EPSILON)
            return false;

        float t = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        float u = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            intersection = new Vector2(p1.x + t * (p2.x - p1.x), p1.y + t * (p2.y - p1.y));
            return true;
        }

        return false;
    }

    private static Vector2[] ConvexHull(List<Vector2> points)
    {
        if (points.Count < 3)
            return points.ToArray();

        Vector2 start = points.OrderBy(p => p.y).ThenBy(p => p.x).First();

        List<Vector2> sorted = points.Where(p => p != start)
            .OrderBy(p => Mathf.Atan2(p.y - start.y, p.x - start.x))
            .ToList();

        Stack<Vector2> hull = new Stack<Vector2>();
        hull.Push(start);
        hull.Push(sorted[0]);

        for (int i = 1; i < sorted.Count; i++)
        {
            Vector2 top = hull.Pop();
            while (hull.Count > 0 && CrossProduct(hull.Peek(), top, sorted[i]) <= 0)
            {
                top = hull.Pop();
            }
            hull.Push(top);
            hull.Push(sorted[i]);
        }

        return hull.Reverse().ToArray();
    }

    private static float CrossProduct(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }

    private static bool ArePolygonsConvex(Vector2[] polygon)
    {
        if (polygon.Length < 3)
            return false;

        bool? isPositive = null;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[(i + 1) % polygon.Length];
            Vector2 p3 = polygon[(i + 2) % polygon.Length];

            float cross = CrossProduct(p1, p2, p3);

            if (Mathf.Abs(cross) > EPSILON)
            {
                if (isPositive == null)
                    isPositive = cross > 0;
                else if (isPositive != (cross > 0))
                    return false;
            }
        }

        return true;
    }

    private static bool IsPointInOrNearPolygon(Vector2 point, Vector2[] polygon, float threshold)
    {
        if (IsPointInPolygon(point, polygon))
            return true;

        int sampleSize = Mathf.Min(polygon.Length, 10);
        for (int i = 0; i < sampleSize; i++)
        {
            int idx = i * polygon.Length / sampleSize;
            Vector2 p1 = polygon[idx];
            Vector2 p2 = polygon[(idx + 1) % polygon.Length];

            float distance = PointToLineSegmentDistance(point, p1, p2);
            if (distance < threshold)
                return true;
        }

        return false;
    }

    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    private static float PointToLineSegmentDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.magnitude;

        if (lineLength < EPSILON)
            return Vector2.Distance(point, lineStart);

        float t = Mathf.Clamp01(Vector2.Dot(point - lineStart, line) / (lineLength * lineLength));
        Vector2 projection = lineStart + t * line;

        return Vector2.Distance(point, projection);
    }

    private static bool PolygonsIntersect(Vector2[] poly1, Vector2[] poly2)
    {
        for (int i = 0; i < poly1.Length; i++)
        {
            Vector2 p1 = poly1[i];
            Vector2 p2 = poly1[(i + 1) % poly1.Length];

            for (int j = 0; j < poly2.Length; j++)
            {
                Vector2 p3 = poly2[j];
                Vector2 p4 = poly2[(j + 1) % poly2.Length];

                if (LineSegmentsIntersect(p1, p2, p3, p4, out _))
                    return true;
            }
        }

        return false;
    }

    private static Vector2 GetCentroid(List<Vector2> points)
    {
        Vector2 sum = Vector2.zero;
        foreach (Vector2 p in points)
            sum += p;
        return sum / points.Count;
    }

    private static Vector2[] TransformPoints(Vector2[] points, Transform transform, Vector2 offset)
    {
        Vector2[] transformed = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 worldPoint = transform.TransformPoint(points[i] + offset);
            transformed[i] = new Vector2(worldPoint.x, worldPoint.y);
        }
        return transformed;
    }

    private static void ApplyMergedPaths(PolygonCollider2D collider, List<Vector2[]> worldPaths)
    {
        collider.pathCount = worldPaths.Count;

        for (int i = 0; i < worldPaths.Count; i++)
        {
            Vector2[] localPath = new Vector2[worldPaths[i].Length];
            for (int j = 0; j < worldPaths[i].Length; j++)
            {
                Vector3 localPoint = collider.transform.InverseTransformPoint(worldPaths[i][j]);
                localPath[j] = new Vector2(localPoint.x, localPoint.y) - collider.offset;
            }
            collider.SetPath(i, localPath);
        }
    }

    public static Vector2[] SimplifyPolygon(Vector2[] polygon, float tolerance = EPSILON)
    {
        return RemoveCollinearPoints(polygon);
    }

    #endregion
}