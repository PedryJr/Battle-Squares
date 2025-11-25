using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class StencilRenderer : MonoBehaviour
{

    int w, h;
    static RenderTexture renderTexture;
    public static Action<Texture> OnStencilChange;

    Camera cam;

    private void Awake()
    {
        OnStencilChange = (e) => { };
        renderTexture = new RenderTexture(2, 2, 24);
        w = -1;
        h = -1;
        cam = GetComponent<Camera>();
        cam.targetTexture = renderTexture;
    }

    private void Start() => TryResize();
    private void Update() => TryResize();
    private void LateUpdate() => TryResize();

    void TryResize()
    {
        int testW = Screen.width, testH = Screen.height;

        if (testW != w || testH != h)
        {
            w = testW;
            h = testH;
            Resize();
        }
    }

    void Resize()
    {
        if (renderTexture) Destroy(renderTexture);
        renderTexture = CreateNewStencil(w, h);
        cam.targetTexture = renderTexture;
        OnStencilChange(renderTexture);
    }

    public static void AssignTextureToProp(in MaterialPropertyBlock mat, in string propName) => mat.SetTexture(propName, renderTexture);


    RenderTexture CreateNewStencil(int w, int h)
    {
        RenderTexture customRT;
        RenderTextureDescriptor desc = new RenderTextureDescriptor(w, h)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            msaaSamples = 1,
            graphicsFormat = GraphicsFormat.R32_SFloat,
            depthStencilFormat = GraphicsFormat.D32_SFloat,
            mipCount = 1,
            useMipMap = false,
            enableRandomWrite = true,
            useDynamicScale = true,
            sRGB = false
        };

        customRT = new RenderTexture(desc);
        customRT.name = "StencilTarget";
        customRT.wrapMode = TextureWrapMode.Clamp;
        customRT.filterMode = FilterMode.Point;

        customRT.Create();
        return customRT;
    }

}
