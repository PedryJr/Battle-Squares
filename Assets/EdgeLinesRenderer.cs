using System;
using System.Collections.Generic;
using UnityEngine;

public class EdgeLinesRenderer : MonoBehaviour
{
    [SerializeField]
    EdgeDefine[] edges;

    [SerializeField]
    Material edgeMaterial;

    [SerializeField]
    float patternWidth = 10f;

    [SerializeField]
    float patternHeight = 10f;

    [SerializeField]
    float segmentsPerUnit = 0.1f; // How many segments per unit of distance

    private List<Mesh> edgeMeshes = new List<Mesh>();
    private List<Matrix4x4> edgeMatrices = new List<Matrix4x4>();

    [SerializeField]
    ButtonHoverAnimation animationMode;
    [SerializeField]
    ButtonHoverAnimation layoutMode;

    DragAndScrollMod dragAndScrollMod;

    //layt
    [ContextMenu("TestStateChange1")]
    void TSC1()
    {
        RectTransformFollowStates[] rectTransformFollowStates = GetComponentsInChildren<RectTransformFollowStates>();
        foreach (var item in rectTransformFollowStates) item.SetTargetState(1);
    }

    //anim
    [ContextMenu("TestStateChange0")]
    void TSC0()
    {
        RectTransformFollowStates[] rectTransformFollowStates = GetComponentsInChildren<RectTransformFollowStates>();
        foreach (var item in rectTransformFollowStates) item.SetTargetState(0);
    }

    int editorState = 0;

    int currentAnimationState = 0;
    int previousAnimationState = 0;

    private void Awake()
    {
        dragAndScrollMod = FindAnyObjectByType<DragAndScrollMod>();
    }

    void Start()
    {
        RebuildEdgeMeshes();
    }

    public void ChangeMode(int mode)
    {
        editorState = mode;
    }

    void LateUpdate()
    {

        if(animationMode.isHovering || layoutMode.isHovering)
        {
            if (animationMode.isHovering) currentAnimationState = 0;
            else currentAnimationState = 1;
        }
        else currentAnimationState = editorState;

        if (currentAnimationState != previousAnimationState)
        {
            RectTransformFollowStates[] rectTransformFollowStates = GetComponentsInChildren<RectTransformFollowStates>();
            foreach (var item in rectTransformFollowStates) item.SetTargetState(currentAnimationState);
        }

        previousAnimationState = currentAnimationState;

        RebuildEdgeMeshes();
        for (int i = 0; i < edgeMeshes.Count; i++)
        {
            if (edgeMeshes[i] != null && edgeMaterial != null)
            {
                Graphics.DrawMesh(
                    edgeMeshes[i],
                    edgeMatrices[i],
                    edgeMaterial,
                    gameObject.layer,
                    null,
                    0,
                    null,
                    UnityEngine.Rendering.ShadowCastingMode.Off,
                    false
                );
            }
        }
    }

    public void RebuildEdgeMeshes()
    {
        foreach (var mesh in edgeMeshes)
        {
            if (mesh != null) Destroy(mesh);
        }

        edgeMeshes.Clear();
        edgeMatrices.Clear();

        for (int i = 0; i < edges.Length; i++)
        {
            if (edges[i].a != null && edges[i].b != null)
            {
                Mesh mesh = CreateEdgeMesh(edges[i]);
                edgeMeshes.Add(mesh);
                edgeMatrices.Add(Matrix4x4.identity);
            }
        }
    }

    Mesh CreateEdgeMesh(EdgeDefine edge)
    {
        Mesh mesh = new Mesh();
        mesh.name = "EdgeMesh";

        Vector2 startPos = edge.a.position;
        Vector2 endPos = edge.b.position;
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);

        // Calculate number of segments based on distance
        int segments = Mathf.Max(1, Mathf.RoundToInt(distance * segmentsPerUnit));

        float computeMul = Camera.main.orthographicSize / 100;
        float computeHeight = computeMul * patternHeight;
        float computeWidth = computeMul * patternWidth;

        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * (computeHeight * 0.5f);
        Vector3 forward = direction * (computeWidth * 0.5f);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / Mathf.Max(1, segments - 1);
            if (segments == 1) t = 0.5f;

            Vector3 center = Vector3.Lerp(startPos, endPos, t);

            int vertexOffset = i * 4;

            vertices.Add(center - forward - perpendicular);
            vertices.Add(center - forward + perpendicular);
            vertices.Add(center + forward + perpendicular);
            vertices.Add(center + forward - perpendicular); 

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));

            triangles.Add(vertexOffset + 0);
            triangles.Add(vertexOffset + 1);
            triangles.Add(vertexOffset + 2);

            triangles.Add(vertexOffset + 0);
            triangles.Add(vertexOffset + 2);
            triangles.Add(vertexOffset + 3);
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void OnDestroy()
    {
        foreach (var mesh in edgeMeshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            RebuildEdgeMeshes();
        }
    }

    [Serializable]
    public struct EdgeDefine
    {
        public RectTransform a, b;
    }
}