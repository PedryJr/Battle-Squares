using UnityEngine;
using UnityEngine.UI;

public class TestPersistentValue : MonoBehaviour
{

    Slider slider;
    PersistentValue<float> sliderValue;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        sliderValue = new PersistentValue<float>("SliderValue", slider.value);
        slider.value = sliderValue.Value;
    }

    public void ChangeSliderValue(float newValue) => sliderValue.Value = newValue;

}
