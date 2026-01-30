using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

public class EffectRenderer : MonoBehaviour
{

    int w, h;
    static RenderTexture effectTexture;
    public static Action<Texture> onEffectTextureChanged;

    Camera cam;

    [SerializeField]
    Material thermalScreen;

    [SerializeField]
    Renderer2DData renderer2D;

    private void Awake()
    {
        onEffectTextureChanged = (e) => { };
        effectTexture = new RenderTexture(2, 2, 24);
        w = -1;
        h = -1;
        cam = GetComponent<Camera>();
        cam.targetTexture = effectTexture;
        thermalScreen.SetTexture("_DistortionTexture", effectTexture);
        FullScreenPassRendererFeature fullScreenPass;
        renderer2D.TryGetRendererFeature(out fullScreenPass);
        fullScreenPass.passMaterial = thermalScreen;
    }

    private void Start() => TryResize();
    private void Update() => TryResize();
    private void LateUpdate() => TryResize();

    void TryResize()
    {
        int testW = BS_Screen.SpixelsX, testH = BS_Screen.SpixelsY;

        if (testW != w || testH != h)
        {
            w = testW;
            h = testH;
            Resize();
        }
    }

    void Resize()
    {
        if (effectTexture) Destroy(effectTexture);
        effectTexture = CreateNewStencil(w, h);
        cam.targetTexture = effectTexture;
        onEffectTextureChanged(effectTexture);

        thermalScreen.SetTexture("_DistortionTexture", effectTexture);
        FullScreenPassRendererFeature fullScreenPass = renderer2D.rendererFeatures[renderer2D.rendererFeatures.Count - 1] as FullScreenPassRendererFeature;
        fullScreenPass.passMaterial = thermalScreen;
    }

    public static void AssignTextureToProp(in MaterialPropertyBlock mat, in string propName) => mat.SetTexture(propName, effectTexture);


    RenderTexture CreateNewStencil(int w, int h)
    {
        RenderTexture customRT;
        RenderTextureDescriptor desc = new RenderTextureDescriptor(w, h)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            msaaSamples = 1,
            graphicsFormat = GraphicsFormat.R32_SFloat,
            depthStencilFormat = GraphicsFormat.S8_UInt,
            mipCount = 1,
            useMipMap = false,
            enableRandomWrite = false,
            useDynamicScale = false,
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
