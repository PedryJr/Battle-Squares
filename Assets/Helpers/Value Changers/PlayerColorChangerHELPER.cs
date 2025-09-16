using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.DebugUI;

public class PlayerColorChangerHELPER : MonoBehaviour
{

    public enum CType { S, V, DS, DV }
    public CType type;
    public string prefix;

    Slider slider;
    TMP_Text text;
    PlayerSynchronizer playerSynchronizer;
    private void Awake()
    {
        text = GetComponentInChildren<TMP_Text>();
        slider = GetComponent<Slider>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
    }

    public void CHANGECOLOR(float value)
    {

        if (type == CType.S) Saturation(value, false);
        if (type == CType.V) Valuate(value, false);
        if (type == CType.DS) Saturation(value, true);
        if (type == CType.DV) Valuate(value, true);
    }

    void Saturation(float sat, bool darken)
    {
        if (!playerSynchronizer.localSquare) return;
        if (darken) playerSynchronizer.localSquare.darkenSaturation = sat;
        else playerSynchronizer.localSquare.Saturation = sat;
        playerSynchronizer.localSquare.ApplyColors();
    }

    void Valuate(float val, bool darken)
    {
        if (!playerSynchronizer.localSquare) return;
        if (darken) playerSynchronizer.localSquare.darkenValue = val;
        else playerSynchronizer.localSquare.Value = val;
        playerSynchronizer.localSquare.ApplyColors();
    }

    private void LateUpdate()
    {
        if (!playerSynchronizer.localSquare) return;
        if (type == CType.S) slider.value = playerSynchronizer.localSquare.Saturation;
        if (type == CType.V) slider.value = playerSynchronizer.localSquare.Value;
        if (type == CType.DS) slider.value = playerSynchronizer.localSquare.darkenSaturation;
        if (type == CType.DV) slider.value = playerSynchronizer.localSquare.darkenValue;
        text.text = prefix + $": {Mathf.RoundToInt(slider.value * 100) / 100f}";
    }

}