using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static AnimationAnchor;
using static ShapeMimicBehaviour;


public sealed class BuiltShapeBehaviour : MonoBehaviour
{
    [Header("Rendering Colors")]
    [SerializeField] private Color staticColor = Color.white;
    [SerializeField] private Color animatedColor = Color.gray;

    [Header("Mesh Components")]
    [SerializeField] private MeshRenderer shapeRenderer;
    [SerializeField] private MeshFilter renderMeshFilter;
    [SerializeField] private MeshRenderer stencilRenderer;
    [SerializeField] private MeshFilter stencilMeshFilter;

    // Cached meshes
    public static Mesh octagonalMesh;
    public static Mesh octagonalMinimalMesh;

    // Shape data
    private Vector2[] shapePoints;
    private bool isAnimated;
    private int shapeIndex;

    private const float OctagonCorner = 0.2071068f;
    private const float OctagonStraight = 0.5f;

    public static VertexAttributeDescriptor GetOctagonalAttribute => new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 2);

    public static VertexAttributeDescriptor GetOctagonalAttributelLight => new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 2);
    public static int[] GetOctagonalIndices => InternalOctagonalIndices;

    static int[] InternalOctagonalIndices = new int[]
    {
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
        0, 5, 6,
        0, 6, 7,
    };


    public static Vector3[] GetOctagonalVerticesVec3 => InternalOctagonalVerticesVec3;
    public static Vector2[] GetOctagonalVerticesVec2 => InternalOctagonalVerticesVec2;
    public static half2[] GetOctagonalVerticesVec2Light => InternalOctagonalVerticesVec2Light;
    static Vector3[] InternalOctagonalVerticesVec3 = new Vector3[]
    {
        new Vector3(-OctagonCorner, OctagonStraight),
        new Vector3(-OctagonStraight, OctagonCorner),
        new Vector3(-OctagonStraight, -OctagonCorner),
        new Vector3(-OctagonCorner, -OctagonStraight),
        new Vector3(OctagonCorner, -OctagonStraight),
        new Vector3(OctagonStraight, -OctagonCorner),
        new Vector3(OctagonStraight, OctagonCorner),
        new Vector3(OctagonCorner, OctagonStraight),
    };

    static Vector2[] InternalOctagonalVerticesVec2 = new Vector2[]
    {
        new Vector2(-0.2071068f, 0.5f),
        new Vector2(-0.5f, 0.2071068f),
        new Vector2(-0.5f, -0.2071068f),
        new Vector2(-0.2071068f, -0.5f),
        new Vector2(0.2071068f, -0.5f),
        new Vector2(0.5f, -0.2071067f),
        new Vector2(0.5f, 0.2071068f),
        new Vector2(0.2071067f, 0.5f),
    };



    static half2 Vec2ToHalf2(Vector2 vec) => new half2((half)vec.x, (half)vec.y);



    static half2[] InternalOctagonalVerticesVec2Light = new half2[]
{
        Vec2ToHalf2(new Vector2(-0.2071068f, 0.5f)),
        Vec2ToHalf2(new Vector2(-0.5f, 0.2071068f)),
        Vec2ToHalf2(new Vector2(-0.5f, -0.2071068f)),
        Vec2ToHalf2(new Vector2(-0.2071068f, -0.5f)),
        Vec2ToHalf2(new Vector2(0.2071068f, -0.5f)),
        Vec2ToHalf2(new Vector2(0.5f, -0.2071067f)),
        Vec2ToHalf2(new Vector2(0.5f, 0.2071068f)),
        Vec2ToHalf2(new Vector2(0.2071067f, 0.5f)),
};

    private static readonly Vector2[] OctagonVertices = new Vector2[]
    {
        new Vector2(-OctagonCorner, OctagonStraight),
        new Vector2(-OctagonStraight, OctagonCorner),
        new Vector2(-OctagonStraight, -OctagonCorner),
        new Vector2(-OctagonCorner, -OctagonStraight),
        new Vector2(OctagonCorner, -OctagonStraight),
        new Vector2(OctagonStraight, -OctagonCorner),
        new Vector2(OctagonStraight, OctagonCorner),
        new Vector2(OctagonCorner, OctagonStraight)
    };

    private static readonly int[] OctagonIndices = new int[]
    {
        0, 1, 2,
        0, 2, 3,
        0, 3, 4,
        0, 4, 5,
        0, 5, 6,
        0, 6, 7
    };

    private void Awake()
    {
        EnsureMeshesExist();
        renderMeshFilter.sharedMesh = octagonalMinimalMesh;
        stencilMeshFilter.sharedMesh = octagonalMinimalMesh;
    }

    public void Initialize(SimplifiedShapeData shapeData, int index, bool animated)
    {
        shapeIndex = index;
        isAnimated = animated;

        CalculateShapePoints(shapeData);
        SetupRendering(shapeData);
        PositionShape(shapeData);
    }

    private void CalculateShapePoints(SimplifiedShapeData shapeData)
    {
        var param = shapeData.param.GetVec4();
        float rotation = param.x;
        float length = param.y;
        float width = param.z;
        float scale = param.w;

        shapePoints = new Vector2[8];

        for (int i = 0; i < 8; i++)
        {
            float yOffset = 0;
            float xOffset = 0;

            // Top vertices
            if (i == 0 || i == 1 || i == 6 || i == 7)
                yOffset = width / 2f;
            // Bottom vertices
            if (i == 2 || i == 3 || i == 4 || i == 5)
                yOffset = -width / 2f;
            // Right vertices
            if (i == 4 || i == 5 || i == 6 || i == 7)
                xOffset = length;

            Vector2 basePoint = (OctagonVertices[i] * scale) + new Vector2(xOffset, yOffset);
            shapePoints[i] = RotatePoint(basePoint, rotation * Mathf.Deg2Rad);
        }
    }

    private void SetupRendering(SimplifiedShapeData shapeData)
    {
        var propertyBlock = new MaterialPropertyBlock();

        // Set vertex positions for shader
        for (int i = 0; i < shapePoints.Length; i++)
        {
            propertyBlock.SetVector($"_Pos{i}",
                new Vector4(shapePoints[i].x, shapePoints[i].y, 0f, 1f));
        }

        // Set color based on animation state
        propertyBlock.SetColor("_MyColor", isAnimated ? animatedColor : staticColor);

        shapeRenderer.SetPropertyBlock(propertyBlock);
    }

    private void PositionShape(SimplifiedShapeData shapeData)
    {
        transform.position = shapeData.coord.GetPosition();

        // Set Z position for proper rendering order
        var pos = transform.localPosition;
        pos.z = isAnimated ? -0.6f : -0.8f;
        transform.localPosition = pos;
    }

    public void AssignStencil(int stencilId)
    {
        float stencilValue = stencilId / 2048f;
        var stencilProperty = new MaterialPropertyBlock();

        // Set stencil value
        stencilProperty.SetVector("_Stencil",
            new Vector4(stencilValue, stencilValue, stencilValue, stencilValue));

        // Set vertex positions for stencil shader
        for (int i = 0; i < shapePoints.Length; i++)
        {
            stencilProperty.SetVector($"_Pos{i}",
                new Vector4(shapePoints[i].x, shapePoints[i].y, 0f, 1f));
        }

        stencilRenderer.SetPropertyBlock(stencilProperty);
    }

    public Vector2[] GetShapePoints() => shapePoints;

    public bool IsAnimated => isAnimated;
    public int ShapeIndex => shapeIndex;

    private static void EnsureMeshesExist()
    {
        CreateOctagonalMesh();
        CreateMinimalMesh();
    }

    private static void CreateOctagonalMesh()
    {
        if (octagonalMesh != null) return;

        octagonalMesh = new Mesh();
        octagonalMesh.name = "Octagon";

        var vertices = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            vertices[i] = new Vector3(OctagonVertices[i].x, OctagonVertices[i].y, 0);
        }

        octagonalMesh.vertices = vertices;
        octagonalMesh.triangles = OctagonIndices;
        octagonalMesh.bounds = new Bounds(Vector3.zero, new Vector3(512, 512, 1));
        octagonalMesh.UploadMeshData(true);
    }

    private static void CreateMinimalMesh()
    {
        if (octagonalMinimalMesh != null) return;

        octagonalMinimalMesh = new Mesh();
        octagonalMinimalMesh.name = "OctagonMinimal";

        var vertices = new Vector3[8];
        float scale = 0.1f;

        for (int i = 0; i < 8; i++)
        {
            vertices[i] = new Vector3(
                OctagonVertices[i].x * scale,
                OctagonVertices[i].y * scale,
                0
            );
        }

        octagonalMinimalMesh.vertices = vertices;
        octagonalMinimalMesh.triangles = OctagonIndices;
        octagonalMinimalMesh.bounds = new Bounds(Vector3.zero, new Vector3(512, 512, 1));
        octagonalMinimalMesh.UploadMeshData(true);
    }

    private static Vector2 RotatePoint(Vector2 point, float angle)
    {
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        return new Vector2(
            point.x * cos - point.y * sin,
            point.x * sin + point.y * cos
        );
    }
}