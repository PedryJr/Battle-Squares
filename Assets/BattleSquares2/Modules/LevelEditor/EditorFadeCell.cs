using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public sealed class EditorFadeCell : MonoBehaviour
{

    RectTransform imageTransform;
    Image image;
    Color imageColor;

    private void Awake()
    {
        imageTransform = GetComponent<RectTransform>();
        image ??= GetComponent<Image>();
        imageColor = image ? image.color : Color.white;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetFade(float fade)
    {
        if (!image) return;
        imageColor.a = fade;
        image.color = imageColor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPosition(float3 position) => imageTransform.localPosition = position;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSize(float3 size) => imageTransform.localScale = size;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float3 GetOnPosition() => imageTransform.localPosition;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float3 GetOnSize() => imageTransform.localScale;

}
