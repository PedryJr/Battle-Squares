using UnityEngine;
using UnityEngine.UI;

public class ScrollerBehaviour : MonoBehaviour
{

    ScrollRect scrollRect;
    Slider slider;
    float timer;
    bool animate = true;

    [SerializeField]
    bool flip;

    private void OnEnable()
    {
        
        slider = GetComponent<Slider>();
        scrollRect = transform.parent.GetComponent<ScrollRect>();
        if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = 1;
        if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = 1;
        timer = flip ? 1 : 0;

    }

    private void Update()
    {

        if (animate)
        {

            if (flip)
            {

                if (timer > 0) timer -= Time.deltaTime;
                if (timer < 0) timer = 0;

                if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, 1 - timer));
                if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, 1 - timer));

                if (timer == 0) animate = false;

            }
            else
            {

                if (timer < 1) timer += Time.deltaTime;
                if (timer > 1) timer = 1;

                if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, 1 - timer));
                if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, 1 - timer));

                if (timer == 1) animate = false;

            }

        }

        if (scrollRect.horizontal) slider.SetValueWithoutNotify(scrollRect.horizontalNormalizedPosition);
        if (scrollRect.vertical) slider.SetValueWithoutNotify(scrollRect.verticalNormalizedPosition);

    }

    public void CHANGE(float value)
    {

        if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = value;
        if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = value;

    }

}
