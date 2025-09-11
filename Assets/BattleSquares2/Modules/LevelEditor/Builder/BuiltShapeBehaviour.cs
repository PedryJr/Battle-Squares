using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static AnimationAnchor;
using static ShapeMimicBehaviour;

public sealed class BuiltShapeBehaviour : MonoBehaviour
{

    [SerializeField] public Color arenaColor;
    [SerializeField] public Color animatedColor;

    MeshRenderer shapeRenderer;
    private void Awake()
    {
        shapeRenderer = GetComponent<MeshRenderer>();
        TEMPFUNC();
        GetComponent<MeshFilter>().sharedMesh = octagonalMesh;
        stencilRenderer.GetComponent<MeshFilter>().sharedMesh = octagonalMesh;
    }

    void TEMPFUNC()
    {
        if (BuiltShapeBehaviour.octagonalMesh) return;

        Mesh octagonalMesh = new Mesh();

        octagonalMesh.name = "Octagon";

        octagonalMesh.SetVertexBufferParams(8, BuiltShapeBehaviour.GetOctagonalAttribute);
        octagonalMesh.SetVertexBufferData(BuiltShapeBehaviour.GetOctagonalVerticesVec2, 0, 0, 8);

        octagonalMesh.indexFormat = IndexFormat.UInt16;
        octagonalMesh.SetIndices(BuiltShapeBehaviour.GetOctagonalIndices, MeshTopology.Triangles, 0);

        octagonalMesh.bounds = new Bounds(Vector3.zero, new Vector3(512, 512, 1));

        octagonalMesh.UploadMeshData(true);

        BuiltShapeBehaviour.octagonalMesh = octagonalMesh;
        /*
                spriteAsMesh.indexFormat = IndexFormat.UInt32;

                spriteAsMesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);*/

    }

    public static Mesh octagonalMesh;

    const float OctaOffset = 0.001f;
    const float OctaCorn = 0.2071068f + OctaOffset;
    const float OctaStra = 0.5f + OctaOffset;

    public static Vector3[] GetOctagonalVerticesVec3 => InternalOctagonalVerticesVec3;
    public static Vector2[] GetOctagonalVerticesVec2 => InternalOctagonalVerticesVec2;
    static Vector3[] InternalOctagonalVerticesVec3 = new Vector3[]
    {
        new Vector3(-OctaCorn, OctaStra),
        new Vector3(-OctaStra, OctaCorn),
        new Vector3(-OctaStra, -OctaCorn),
        new Vector3(-OctaCorn, -OctaStra),
        new Vector3(OctaCorn, -OctaStra),
        new Vector3(OctaStra, -OctaCorn),
        new Vector3(OctaStra, OctaCorn),
        new Vector3(OctaCorn, OctaStra),
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

    public static VertexAttributeDescriptor GetOctagonalAttribute => new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 2);

    Vector2[] correctedPoints;
    public void ApplyShape(SimplifiedShapeData simplifiedShapeData, int indexedId, LevelBuilderStuff builder, bool isStatic)
    {
        this.isStatic = isStatic;
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        Vector3 param = simplifiedShapeData.param.GetVec3();
        float rot, len, wid;
        rot = param.x;
        len = param.y;
        wid = param.z;

        correctedPoints = new Vector2[8];
        for (int i = 0; i < correctedPoints.Length; i++)
        {
            float yToAdd = 0;
            float xToAdd = 0;

            if (i == 0 || i == 1 || i == 6 || i == 7) yToAdd = wid / 2f;
            if (i == 2 || i == 3 || i == 4 || i == 5) yToAdd = -wid / 2f;
            if (i == 4 || i == 5 || i == 6 || i == 7) xToAdd = len;

            Vector2 pointNoRotation = (Vector2)GetOctagonalVerticesVec3[i] + new Vector2(xToAdd, yToAdd);
            float pointBaseRotation = Mathf.Atan2(pointNoRotation.y, pointNoRotation.x) * Mathf.Rad2Deg;

            float rotationAccum = pointBaseRotation + rot;
            Vector2 pointAsRotated = new Vector2(Mathf.Cos(rotationAccum), Mathf.Sin(rotationAccum)).normalized;

            correctedPoints[i] = rotate(pointNoRotation, rot * Mathf.Deg2Rad);

            propertyBlock.SetVector($"_Pos{i}", new Vector4(correctedPoints[i].x, correctedPoints[i].y, 0f, 1f));
        }

        transform.position = simplifiedShapeData.coord.GetPosition();

        int animatorIndex = EvaluateAnimationIndex(indexedId);


        if (!IsStatic)
        {
            gameObject.AddComponent<ShadowCaster2D>().castingOption = ShadowCaster2D.ShadowCastingOptions.CastAndSelfShadow;
            gameObject.AddComponent<ShadowCaster2DController>().UpdateShadowFromPoints(correctedPoints);
            if (!builder.animatedAnimationsAwaitingShapes.ContainsKey(animatorIndex)) builder.animatedAnimationsAwaitingShapes.Add(animatorIndex, new List<Transform>());
            builder.animatedAnimationsAwaitingShapes[animatorIndex].Add(transform);
        }

        PolygonCollider2D polygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider2D.points = correctedPoints;

        if (IsStatic)
        {
            polygonCollider2D.compositeOperation = Collider2D.CompositeOperation.Merge;
            propertyBlock.SetColor("_MyColor", arenaColor);
            builder.builtShapes.Add(this);
        }
        else
        {
            propertyBlock.SetColor("_MyColor", animatedColor);
        }

        shapeRenderer.SetPropertyBlock(propertyBlock);

    }

    bool isStatic = true;
    public bool IsStatic => isStatic;

    public static int EvaluateAnimationIndex(int indexedId)
    {
        int animatorIndex = -1;
        bool isStatic = true;
        for (int i = 0; i < LevelBuilderStuff.simplifiedAnimationDatas.Length; i++)
        {
            SimplifiedAnimationData animData = LevelBuilderStuff.simplifiedAnimationDatas[i];
            for (int j = 0; j < animData.linkedShapes.Length; j++)
            {
                if (animData.linkedShapes[j] == indexedId)
                {
                    isStatic = false;
                    animatorIndex = i;
                    break;
                }
            }
            if (!isStatic) break;
        }

        return animatorIndex;
    }

    [SerializeField]
    MeshRenderer stencilRenderer;
    public void AssignStencil(int stencilValueInt)
    {
        MaterialPropertyBlock stencilProperty = new MaterialPropertyBlock();

        float stencilValue = 1f / (stencilValueInt + 1f);
        Debug.Log(stencilValue);
        stencilProperty.SetVector("_Stencil", new Vector4(stencilValue, stencilValue, stencilValue, stencilValue));
        for(int i = 0; i < correctedPoints.Length; i++)
        {
            stencilProperty.SetVector($"_Pos{i}", new Vector4(correctedPoints[i].x, correctedPoints[i].y, 0f, 1f));
        }

        stencilRenderer.SetPropertyBlock(stencilProperty);

        gameObject.AddComponent<StencilInfectorBehaviour>().SetStencil(stencilValueInt);

    }

    public static bool EvaluateStatic(int indexedId)
    {
        bool isStatic = true;
        for (int i = 0; i < LevelBuilderStuff.simplifiedAnimationDatas.Length; i++)
        {
            SimplifiedAnimationData animData = LevelBuilderStuff.simplifiedAnimationDatas[i];
            for (int j = 0; j < animData.linkedShapes.Length; j++)
            {
                if (animData.linkedShapes[j] == indexedId)
                {
                    isStatic = false;
                    break;
                }
            }
            if (!isStatic) break;
        }

        return isStatic;
    }

}
