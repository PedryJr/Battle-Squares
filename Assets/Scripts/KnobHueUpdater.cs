using UnityEngine;
using UnityEngine.UI;

public sealed class KnobHueUpdater : MonoBehaviour
{

    Image sliderKnob;

    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        sliderKnob = GetComponent<Image>();

    }

    private void Update()
    {
        
        UpdateHueKnob();

    }

    public void UpdateHueKnob()
    {

        if (!playerSynchronizer.localSquare) return;

        float h, s, v;

        Color activeColor = playerSynchronizer.localSquare.PlayerColor.PrimaryColor;

        Color baseColor = activeColor;
        Vector3 baseNormalized = new Vector3(baseColor.r, baseColor.g, baseColor.b).normalized;
        Color normalColor = new Color(baseNormalized.x, baseNormalized.y, baseNormalized.z);

        CursorBehaviour.SetColor(
            playerSynchronizer.localSquare.PlayerColor.CursorDefaultColor,
            playerSynchronizer.localSquare.PlayerColor.CursorHoverColor);

        activeColor.r *= 0.8f;
        activeColor.g *= 0.8f;
        activeColor.b *= 0.8f;
        activeColor.a = 1;

        Color.RGBToHSV(activeColor, out h, out s, out v);

        activeColor = Color.HSVToRGB(h, s, v);

        sliderKnob.color = activeColor;

    }

}
