using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogElementAnimation : MonoBehaviour
{

    public Action<LogElementAnimation> onExpire = (_) => {};

    [SerializeField]
    float timeToFadeIn;
    [SerializeField]
    public float timeToStay;
    [SerializeField]
    float timeToFadeOut;
    [SerializeField]
    float maxPreferredHeight = 40f;
    [SerializeField]
    float maxPreferredWidth = 40f;

    Color visibleColor = Color.white;
    Color invisibleColor = Color.clear;

    public TMP_Text text;
    LayoutElement element;

    [SerializeField]
    AnimationCurve spawnCurve;
    [SerializeField]
    AnimationCurve despawnCurve;

    float elapsedTime = 0f;
    float totalDuration;
    bool isAnimating = false;
    bool expired = false;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        element = GetComponent<LayoutElement>();
        totalDuration = timeToFadeIn + timeToStay + timeToFadeOut;
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
        isAnimating = true;
        text.color = invisibleColor;
        element.preferredHeight = 0f;
    }

    void Update()
    {
        if (!isAnimating) return;

        bool oldExpired = expired;
        elapsedTime += Time.deltaTime;

        if (elapsedTime < timeToFadeIn)
        {
            float t = elapsedTime / timeToFadeIn;
            float curveValue = spawnCurve != null ? spawnCurve.Evaluate(t) : t;

            text.color = Color.Lerp(invisibleColor, visibleColor, curveValue);

            element.preferredHeight = Mathf.Lerp(0f, maxPreferredHeight, curveValue);
            element.preferredWidth = Mathf.Lerp(0f, maxPreferredWidth, curveValue);
        }
        else if (elapsedTime < timeToFadeIn + timeToStay)
        {
            text.color = visibleColor;
            if (element != null)
            {
                element.preferredHeight = maxPreferredHeight;
                element.preferredWidth = maxPreferredWidth;
            }
        }
        else if (elapsedTime < totalDuration)
        {

            expired = true;

            if (oldExpired != expired) onExpire(this);

            float t = (elapsedTime - timeToFadeIn - timeToStay) / timeToFadeOut;
            float curveValue = despawnCurve != null ? despawnCurve.Evaluate(t) : t;

            text.color = Color.Lerp(visibleColor, invisibleColor, curveValue);
            element.preferredHeight = Mathf.Lerp(maxPreferredHeight, 0f, curveValue);
            element.preferredWidth = Mathf.Lerp(maxPreferredWidth, 0f, curveValue);
        }
        else
        {
            text.color = invisibleColor;
            element.preferredHeight = 0f;
            isAnimating = false;

            Destroy(gameObject);
        }
    }

    public void PrematureFadeout()
    {
        if (elapsedTime < timeToFadeIn + timeToStay) elapsedTime = timeToFadeIn + timeToStay;
        expired = true;
        onExpire(this);
    }
}
