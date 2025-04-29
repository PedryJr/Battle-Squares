using TMPro;
using UnityEngine;

public class IndicatorTextBehaviour : MonoBehaviour
{

    TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    float timer = 1;

    Color clear = Color.clear;
    Color visiable = Color.white;

    private void Update()
    {

        timer += Time.deltaTime * 1.8f;

        timer = Mathf.Clamp01(timer);

        text.color = Color.Lerp(visiable, clear, MyExtentions.EaseInExpo(timer));

    }

    public void INDICATE() => timer = 0;

}
