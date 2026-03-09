using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BloomController : MonoBehaviour
{

    Volume volume;
    Bloom bloom;

    private void Awake()
    {
        volume = GetComponent<Volume>();
        volume.profile.TryGet(out bloom);
    }

    const float pixelReference = 1080.0f;
    const float intensity = 2.64f;

    private void Update()
    {
        float calculatedScatter = BS_Screen.SpixelsY / Camera.main.orthographicSize / pixelReference * intensity;
        if (bloom.scatter.value != calculatedScatter) bloom.scatter.value = calculatedScatter;
    }

}
