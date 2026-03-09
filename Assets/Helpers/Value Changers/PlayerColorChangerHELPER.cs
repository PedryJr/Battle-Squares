using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        playerSynchronizer.localSquare.ApplyColors();
    }

    void Valuate(float val, bool darken)
    {
        if (!playerSynchronizer.localSquare) return;
        playerSynchronizer.localSquare.ApplyColors();
    }

    private void LateUpdate()
    {
        if (!playerSynchronizer.localSquare) return;
        text.text = prefix + $": {Mathf.RoundToInt(slider.value * 100) / 100f}";
    }

}