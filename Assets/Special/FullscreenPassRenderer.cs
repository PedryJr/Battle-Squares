using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FullscreenPassRenderer : MonoBehaviour
{
    [Tooltip("Material used for the fullscreen pass.")]
    public Material material;

    private Camera cam;
    private Mesh fullscreenQuad;
    private Vector3[] vertices = new Vector3[4];
    private Vector2[] uvs = new Vector2[4];
    private int[] indices = { 0, 1, 2, 0, 2, 3 };

    RenderParams renderParams;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        GenerateQuad();
    }


    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        GenerateQuad();
    }

    private void GenerateQuad()
    {
        if (fullscreenQuad == null)
            fullscreenQuad = new Mesh() { name = "FullscreenQuad" };
        else
            fullscreenQuad.Clear();

        renderParams = new RenderParams(material);

        // Quad in camera space (Z = near plane + epsilon)
        float z = cam.nearClipPlane + 0.01f;

        vertices[0] = cam.ViewportToWorldPoint(new Vector3(0, 0, z));
        vertices[1] = cam.ViewportToWorldPoint(new Vector3(1, 0, z));
        vertices[2] = cam.ViewportToWorldPoint(new Vector3(1, 1, z));
        vertices[3] = cam.ViewportToWorldPoint(new Vector3(0, 1, z));

        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(1, 1);
        uvs[3] = new Vector2(0, 1);

        fullscreenQuad.vertices = vertices;
        fullscreenQuad.uv = uvs;
        fullscreenQuad.triangles = indices;
        fullscreenQuad.RecalculateBounds();

        renderParams.camera = cam;
        renderParams.layer = 1;
    }

    private void Update()
    {
        if (material == null) return;
        GenerateQuad();
        Graphics.RenderMesh(renderParams, fullscreenQuad, 0, Matrix4x4.identity);
    }
}
