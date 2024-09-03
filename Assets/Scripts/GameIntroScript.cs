using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameIntroScript : MonoBehaviour
{

    float time;

    [SerializeField]
    float seconds = 0.5f;

    [SerializeField]
    Image fadeImage;

    [SerializeField]
    Image[] additionalImages;
    [SerializeField]
    Color[] additionalColorsI;

    [SerializeField]
    TMP_Text[] additionalTexts;
    [SerializeField]
    Color[] additionalColorsT;

    [SerializeField]
    AnimationCurve curve;

    [SerializeField]
    Color start;

    [SerializeField]
    Color end;

    [SerializeField]
    bool shouldDestroy = true;

    bool stop = false;

    private void Awake()
    {
        stop = false;
        time = 0;
        fadeImage.color = start;

    }

    private void OnEnable()
    {
        stop = false;
        time = 0;
        fadeImage.color = start;

    }

    private void LateUpdate()
    {

        if(stop) return;

        time += Time.deltaTime / seconds;

        if(time > 1)
        {
            time = 1;
        }

        fadeImage.color = Color.Lerp(start, end, Mathf.SmoothStep(0, 1, time));

        if(additionalImages != null)
        {
            if(additionalImages.Length > 0)
            {
                for (int i = 0; i < additionalImages.Length; i++)
                {
                    additionalImages[i].color = Color.Lerp(start, additionalColorsI[i], Mathf.SmoothStep(0, 1, time));
                }
            }
        }

        if (additionalTexts != null)
        {
            if (additionalTexts.Length > 0)
            {
                for (int i = 0; i < additionalTexts.Length; i++)
                {
                    additionalTexts[i].color = Color.Lerp(start, additionalColorsT[i], Mathf.SmoothStep(0, 1, time));
                }
            }
        }

        if (time == 1)
        {
            if (shouldDestroy) Destroy(gameObject);
            else stop = true;
        }

    }

}
