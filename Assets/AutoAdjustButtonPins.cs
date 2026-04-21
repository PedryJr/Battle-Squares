using UnityEngine;

[ExecuteInEditMode]
public class AutoAdjustButtonPins : MonoBehaviour
{

    [SerializeField]
    float pinDistance = 10f;

    [SerializeField]
    bool autoAdjust = false;

    [SerializeField]
    RectTransform[] pins;

    private void Update()
    {
        if (!autoAdjust) return;
        if (pins.Length != 6) return;

        for (int i = 0; i < pins.Length; i++) AdjustPin(pins[i], i);

    }

    void AdjustPin(RectTransform pin, int version)
    {

        if(version == 1)
        {

            RectTransform parent = (RectTransform) pin.parent;
            pin.anchoredPosition = new Vector2(pinDistance - parent.sizeDelta.x, 0f);

        }

    }

}
