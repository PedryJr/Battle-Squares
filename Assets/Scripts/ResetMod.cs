using UnityEngine;
using UnityEngine.UI;

public class ResetMod : MonoBehaviour
{

    Slider slider;

    float sliderValue;

    private void Awake()
    {
        
        slider = transform.parent.GetComponentInChildren<Slider>();
        sliderValue = slider.value;

    }

    public void CLICK()
    {

        slider.value = sliderValue;

    }

}
