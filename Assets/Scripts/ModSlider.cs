using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ModSlider : MonoBehaviour
{

    [SerializeField]
    Image sliderKnob;

    [SerializeField]
    TMP_Text valueField;

    [SerializeField]
    public int modIndex;

    public float defaultValue;

    public Slider slider;

    PlayerSynchronizer playerSynchronizer;

    void Awake()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        slider = GetComponentInChildren<Slider>();
        defaultValue = slider.value;
    }

    private void OnEnable()
    {
        UpdateHueKnob();
        ModChange(slider.value);
    }

    public void ModChange(float value)
    {

        playerSynchronizer.SyncMods(modIndex, value);
        valueField.text = (Mathf.Round(value * 100f)/100f).ToString();

    }

    public void UpdateHueKnob()
    {

        if (!playerSynchronizer.localSquare) return;

        float h, s, v;

        Color activeColor = playerSynchronizer.localSquare.PlayerColor.PrimaryColor;

        Color baseColor = activeColor;
        Vector3 baseNormalized = new Vector3(baseColor.r, baseColor.g, baseColor.b).normalized;
        Color normalColor = new Color(baseNormalized.x, baseNormalized.y, baseNormalized.z);

        Color cursorColor = normalColor * 0.6f;
        Color cursorDarkerColor = normalColor * 0.38f;

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
