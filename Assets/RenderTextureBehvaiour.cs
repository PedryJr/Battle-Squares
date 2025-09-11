using UnityEngine;
using UnityEngine.Rendering;

public class RenderTextureBehvaiour : MonoBehaviour
{
    [SerializeField]
    RenderTexture colorRT;
    RenderTexture stencilRT;
    void Start()
    {
        colorRT = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 24, RenderTextureFormat.ARGB32);
        stencilRT = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0, RenderTextureFormat.R8);
        Camera.main.SetTargetBuffers(new RenderBuffer[]
        {
                    colorRT.colorBuffer,
                    stencilRT.colorBuffer
        }, colorRT.depthBuffer);
    }

    private void Update()
    {
    }
}
