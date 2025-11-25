using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class ThermalRenderer : MonoBehaviour
{

    int w, h;
    static RenderTexture renderTexture;
    public static Action<Texture> OnStencilChange = (e) => { };

    Camera mainCam;

    [SerializeField]
    Camera cam;
    [SerializeField]
    Material thermalScreen;

    [SerializeField]
    Renderer2DData renderer2D;

    private void Awake()
    {
        mainCam = GetComponentInParent<Camera>();
        OnStencilChange = (e) => { };
        renderTexture = new RenderTexture(2, 2, 24);
        w = 2;
        h = 2;
        cam = GetComponent<Camera>();
        cam.targetTexture = renderTexture;
        TryResize();
        thermalScreen.SetTexture("_DistortionTexture", renderTexture);
        FullScreenPassRendererFeature fullScreenPass;
        renderer2D.TryGetRendererFeature(out fullScreenPass);
        fullScreenPass.passMaterial = thermalScreen;
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
        cam.orthographic = mainCam.orthographic;
        cam.orthographicSize = mainCam.orthographicSize;
        RenderTexture oldRt = cam.targetTexture;
        renderTexture = CreateNewStencil(w, h);
        cam.targetTexture = renderTexture;
        OnStencilChange(renderTexture);
        if (oldRt)
        {
            oldRt.Release();
            DestroyImmediate(oldRt);
        }
        thermalScreen.SetTexture("_DistortionTexture", renderTexture);
        FullScreenPassRendererFeature fullScreenPass = renderer2D.rendererFeatures[renderer2D.rendererFeatures.Count - 1] as FullScreenPassRendererFeature;
        fullScreenPass.passMaterial = thermalScreen;
    }

    public static void AssignTextureToProp(in MaterialPropertyBlock mat, in string propName) => mat.SetTexture(propName, renderTexture);


    RenderTexture CreateNewStencil(int w, int h)
    {
        RenderTexture customRT;
        RenderTextureDescriptor desc = new RenderTextureDescriptor(w, h)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            msaaSamples = 1,
            graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat,
            depthStencilFormat = GraphicsFormat.D32_SFloat,
            mipCount = 1,
            useMipMap = false,
            enableRandomWrite = true,
            useDynamicScale = true,
            sRGB = false,
        };

        customRT = new RenderTexture(desc);
        customRT.name = "ThermalTarget";
        customRT.wrapMode = TextureWrapMode.Clamp;
        customRT.filterMode = FilterMode.Point;

        customRT.Create();
        return customRT;
    }

}
