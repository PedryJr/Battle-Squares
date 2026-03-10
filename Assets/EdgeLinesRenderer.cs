using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EdgeLinesRenderer : MonoBehaviour
{
    [SerializeField]
    EdgeDefine[] edges;

    [SerializeField]
    Material edgeMaterial;

    [SerializeField]
    float patternWidth = 10f;

    private List<Mesh> edgeMeshes = new List<Mesh>();
    private List<Matrix4x4> edgeMatrices = new List<Matrix4x4>();

    [SerializeField]
    ButtonHoverAnimation HUD_buttons;

    [SerializeField]
    ButtonHoverAnimation animationMode;
    [SerializeField]
    ButtonHoverAnimation layoutMode;

    DragAndScrollMod dragAndScrollMod;

    [ContextMenu("TestStateChange1")]
    void TSC1()
    {
        RectTransformFollowStates[] rectTransformFollowStates = GetComponentsInChildren<RectTransformFollowStates>();
        foreach (var item in rectTransformFollowStates) item.SetTargetState(1);
    }

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

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += DrawLiveUIContainer;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= DrawLiveUIContainer;
    }

    void DrawLiveUIContainer(ScriptableRenderContext arg1, Camera cam)
    {
        Debug.Log("aa");
        if (cam != Camera.main) return;
        if (animationMode.isHovering || layoutMode.isHovering)
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

        Vector2 direction = (endPos - startPos).normalized;
        float distance = Vector2.Distance(startPos, endPos);

        float computeMul = Camera.main.orthographicSize / 100f;
        float computeWidth = computeMul * patternWidth;

        Vector3 extendedStart = startPos - direction * computeWidth;
        Vector3 extendedEnd = endPos + direction * computeWidth;

        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f) * computeWidth;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        vertices.Add(extendedStart - perpendicular);
        vertices.Add(extendedStart + perpendicular);
        vertices.Add(extendedEnd + perpendicular);
        vertices.Add(extendedEnd - perpendicular);
         
        float uvLength = (distance + computeWidth) / computeWidth;

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(uvLength, 1));
        uvs.Add(new Vector2(uvLength, 0));

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(3);

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