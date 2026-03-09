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

        sliderKnob.color = playerSynchronizer.localSquare.PlayerColor.UIKnobColor;


    }

}
