using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public sealed class DrawEditGrid : MonoBehaviour
{
    Mesh mesh;

    [SerializeField]
    float gridSnappingStep = 1.0f;

    [SerializeField]
    Material levelEditorGridMaterial;

    LevelEditorPreviewBehaviour previewBehaviour;

    [SerializeField]
    int gridSize = 100; // Number of grid lines in each direction

    [SerializeField]
    float gridDepth;

    private Camera mainCamera;

    private void Awake()
    {
        previewBehaviour = GetComponent<LevelEditorPreviewBehaviour>();
        mesh = new Mesh();
        mainCamera = Camera.main;
        GenerateGridMesh();
    }

    private void Start()
    {
        previewBehaviour.normalColor = levelEditorGridMaterial.color;
    }

    [ContextMenu("Refresh Grid")]
    private void GenerateGridMesh()
    {

        float orthoSize = mainCamera.orthographicSize;
        float aspect = mainCamera.aspect;
        float width = Mathf.Ceil((orthoSize * aspect * 2) / gridSnappingStep) * gridSnappingStep;
        float height = Mathf.Ceil((orthoSize * 2) / gridSnappingStep) * gridSnappingStep;

        int lineCount = gridSize * 2 + 1;
        int totalLines = lineCount * 2; // Horizontal + Vertical
        int totalVerts = totalLines * 2; // 2 vertices per line

        NativeArray<Vector3> vertices = new NativeArray<Vector3>(totalVerts, Allocator.TempJob);
        NativeArray<int> indices = new NativeArray<int>(totalVerts, Allocator.TempJob);

        var job = new GridMeshJob
        {
            width = width,
            height = height,
            gridSnappingStep = gridSnappingStep,
            gridSize = gridSize,
            vertices = vertices,
            indices = indices
        };

        JobHandle handle = job.Schedule();
        handle.Complete();

        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();

        vertices.Dispose();
        indices.Dispose();

    }

    float lastOrthoSize = 0;

    private void Update()
    {

        transform.position = new Vector3(
            Mathf.Round(mainCamera.transform.position.x / gridSnappingStep) * gridSnappingStep,
            Mathf.Round(mainCamera.transform.position.y / gridSnappingStep) * gridSnappingStep,
            gridDepth
        );

        levelEditorGridMaterial.color = previewBehaviour.GetTargetColor();

        Graphics.DrawMesh(mesh, transform.localToWorldMatrix,
                         levelEditorGridMaterial, 0);
    }

    private void LateUpdate()
    {
        if (lastOrthoSize != Camera.main.orthographicSize) GenerateGridMesh();
        lastOrthoSize = Camera.main.orthographicSize;
    }


    [BurstCompile]
    public struct GridMeshJob : IJob
    {
        public float width;
        public float height;
        public float gridSnappingStep;
        public int gridSize;

        [WriteOnly] public NativeArray<Vector3> vertices;
        [WriteOnly] public NativeArray<int> indices;

        public void Execute()
        {
            int lineCount = gridSize * 2 + 1;

            for (int i = 0; i <= gridSize; i++)
            {
                float y = i * gridSnappingStep;
                vertices[i * 2] = new Vector3(-width / 2, y, 0);
                vertices[i * 2 + 1] = new Vector3(width / 2, y, 0);
                indices[i * 2] = i * 2;
                indices[i * 2 + 1] = i * 2 + 1;

                if (i > 0)
                {
                    y = -y;
                    int idx = (gridSize + i) * 2;
                    vertices[idx] = new Vector3(-width / 2, y, 0);
                    vertices[idx + 1] = new Vector3(width / 2, y, 0);
                    indices[idx] = idx;
                    indices[idx + 1] = idx + 1;
                }
            }

            int verticalOffset = lineCount * 2;
            for (int i = 0; i <= gridSize; i++)
            {
                float x = i * gridSnappingStep;
                int baseIndex = verticalOffset + i * 2;

                vertices[baseIndex] = new Vector3(x, -height / 2, 0);
                vertices[baseIndex + 1] = new Vector3(x, height / 2, 0);
                indices[baseIndex] = baseIndex;
                indices[baseIndex + 1] = baseIndex + 1;

                if (i > 0)
                {
                    x = -x;
                    int idx = verticalOffset + (gridSize + i) * 2;
                    vertices[idx] = new Vector3(x, -height / 2, 0);
                    vertices[idx + 1] = new Vector3(x, height / 2, 0);
                    indices[idx] = idx;
                    indices[idx + 1] = idx + 1;
                }
            }
        }
    }


}