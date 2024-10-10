using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Material = UnityEngine.Material;

public class ConvertSolidToMesh : MonoBehaviour
{

    [SerializeField]
    Material mat;

    [SerializeField]
    Transform[] solids;

    Material fetchedMaterial;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    void Awake()
    {

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.receiveShadows = false;

        fetchedMaterial = Resources.Load<Material>("ArenaBlock");

        Mesh mesh = new Mesh();
        CombineInstance[] meshes = new CombineInstance[solids.Length];

        for (int i = 0; i < solids.Length; i++)
        {

            meshes[i] = GenerateSpriteFromCollider(solids[i]);

        }

        mesh.CombineMeshes(meshes, true, true);

        meshFilter.mesh = mesh;

        meshRenderer.material = fetchedMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

    }

    private CombineInstance GenerateSpriteFromCollider(Transform solid)
    {

        CombineInstance combineInstance = new CombineInstance();

        SpriteShapeRenderer spriteShapeRenderer = solid.GetComponent<SpriteShapeRenderer>();
        spriteShapeRenderer.forceRenderingOff = true;
        PolygonCollider2D polygonCollider = solid.gameObject.AddComponent<PolygonCollider2D>();

        List<Vector2> shapePoints = GetSpriteShapePoints(solid.GetComponent<SpriteShapeController>());
        polygonCollider.SetPath(0, shapePoints.ToArray());
        polygonCollider.useDelaunayMesh = true;

        Vector2[] points = polygonCollider.points;
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            vertices[i] = points[i];
        }

        Triangulator triangulator = new Triangulator(points);
        int[] triangles = triangulator.Triangulate();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        combineInstance.mesh = mesh;
        combineInstance.transform = solid.localToWorldMatrix;

        Destroy(polygonCollider);

        return combineInstance;

    }

    public class Triangulator
    {
        private List<Vector2> m_points;

        public Triangulator(Vector2[] points)
        {
            m_points = new List<Vector2>(points);
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray();

            int[] V = new int[n];
            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                    return indices.ToArray();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a = V[u];
                    int b = V[v];
                    int c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    for (int s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];

            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;

            for (int p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }

            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax = C.x - B.x; float ay = C.y - B.y;
            float bx = A.x - C.x; float by = A.y - C.y;
            float cx = B.x - A.x; float cy = B.y - A.y;
            float apx = P.x - A.x; float apy = P.y - A.y;
            float bpx = P.x - B.x; float bpy = P.y - B.y;
            float cpx = P.x - C.x; float cpy = P.y - C.y;

            float aCROSSbp = ax * bpy - ay * bpx;
            float cCROSSap = cx * apy - cy * apx;
            float bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }

    List<Vector2> GetSpriteShapePoints(SpriteShapeController spriteShapeController)
    {
        List<Vector2> points = new List<Vector2>();

        Spline spline = spriteShapeController.spline;

        for (int i = 0; i < spline.GetPointCount(); i++)
        {
            Vector3 splinePoint = spline.GetPosition(i);

            points.Add(new Vector2(splinePoint.x, splinePoint.y));
        }

        return points;
    }

}