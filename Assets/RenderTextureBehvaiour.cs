using UnityEngine;
using UnityEngine.Rendering;

public class RenderTextureBehvaiour : MonoBehaviour
{
    [SerializeField]
    RenderTexture colorRT;
    RenderTexture stencilRT;
    void Start()
    {
        colorRT.width = Camera.main.pixelWidth;
        colorRT.height = Camera.main.pixelHeight;
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
