using Unity.Mathematics;
using UnityEngine;

public sealed class SelectorRenderer : MonoBehaviour
{

    //OrderStandard

    public float outerAnimationMultiplier = 0.0f;

    [SerializeField]
    float selectorBaseThickness = 1.0f;
    [SerializeField]
    float selectorAdditiveAmplitude = 1.0f;
    [SerializeField]
    float selectorAnimationFrequency = 1.0f;

    float animationScalar = 1.0f;
    float timer = 0.0f;

    [SerializeField]
    Transform offsetA;
    [SerializeField]
    Transform offsetB;

    [SerializeField]
    bool offsetHorizontal;

    [SerializeField]
    Material spriteMat;

    [SerializeField]
    Vector3[] originalOctagonVertices;
    int[] meshIndices;

    Vector3[] octagonVerticesWithOffsets;

    Mesh meshToRender;
    RenderParams renderParams;

    private void Awake()
    {

        meshToRender = new Mesh();
        PolygonTriangulator.Triangulate8(originalOctagonVertices, out originalOctagonVertices, out meshIndices);
        meshToRender.vertices = originalOctagonVertices;
        octagonVerticesWithOffsets = meshToRender.vertices;
        meshToRender.triangles = meshIndices;
        renderParams = new RenderParams(spriteMat);
        meshToRender.bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(1000f, 1000f, 0));
    }

    private void OnDestroy() => Destroy(meshToRender);


    private void Update()
    {

        ComputeAnimationScalar();

        if (offsetHorizontal) OffsetVertsHorizontal();
        else OffsetVertsVertical();

        meshToRender.vertices = octagonVerticesWithOffsets;

        float4x4 modelWithoutScale = float4x4.TRS(transform.position, transform.rotation, new float3(1f));

        Graphics.RenderMesh(renderParams, meshToRender, 0, modelWithoutScale);
    }

    void ComputeAnimationScalar()
    {
        timer += Time.deltaTime;

        animationScalar = selectorBaseThickness + (selectorAdditiveAmplitude * Mathf.Sin(timer * selectorAnimationFrequency));
    }

    void OffsetVertsVertical()
    {
        //First two and last two offset up
        //index Two to index 5 offset down

        for (int i = 0; i < originalOctagonVertices.Length; i++)
        {
            Vector3 point = originalOctagonVertices[i] * animationScalar * outerAnimationMultiplier;
            if (i < 2 || i > 5)
            {
                //Up
                float yDiff = Vector2.Distance(transform.position, offsetA.position) / 2f;
                point.y = point.y + yDiff;
            }
            else
            {
                //Down
                float yDiff = Vector2.Distance(transform.position, offsetB.position) / 2f;
                point.y = point.y - yDiff;
            }
            octagonVerticesWithOffsets[i] = point;
        }

    }

    void OffsetVertsHorizontal()
    {
        //First four offset to left
        //Last four offset to right

        for (int i = 0; i < originalOctagonVertices.Length; i++)
        {
            Vector3 point = originalOctagonVertices[i] * animationScalar * outerAnimationMultiplier;
            if (i < 4)
            {
                //Left
                float xDiff = Vector2.Distance(transform.position, offsetA.position) / 2f;
                point.x = point.x - xDiff;
            }
            else
            {
                //Right
                float xDiff = Vector2.Distance(transform.position, offsetB.position) / 2f;
                point.x = point.x + xDiff;
            }
            octagonVerticesWithOffsets[i] = point;
        }

    }

}
