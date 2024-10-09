using UnityEngine;
using UnityEngine.UI;

public class KnobHueUpdater : MonoBehaviour
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

        Color activeColor = playerSynchronizer.localSquare.playerColor;

        Color baseColor = activeColor;
        Vector3 baseNormalized = new Vector3(baseColor.r, baseColor.g, baseColor.b).normalized;
        Color normalColor = new Color(baseNormalized.x, baseNormalized.y, baseNormalized.z);

        Color cursorColor = normalColor * 0.6f;
        Color cursorDarkerColor = normalColor * 0.38f;

        CursorBehaviour.SetColor(
            playerSynchronizer.localSquare.playerColor * 1.5f,
            playerSynchronizer.localSquare.playerDarkerColor * 0.8f);

        activeColor.r *= 0.8f;
        activeColor.g *= 0.8f;
        activeColor.b *= 0.8f;
        activeColor.a = 1;

        Color.RGBToHSV(activeColor, out h, out s, out v);

        activeColor = Color.HSVToRGB(h, s, v);

        sliderKnob.color = activeColor;

    }

}
