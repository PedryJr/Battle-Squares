using System.Collections;
using UnityEngine;
using TMPro;

public sealed class MyLog : MonoBehaviour
{
    public static MyLog Instance;

    [Header("UI References")]
    [SerializeField] private TMP_Text logText;

    [Header("Animation Settings")]
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float popScale = 1.1f;

    private Vector3 _originalScale;
    private Coroutine _animRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (logText == null)
            logText = GetComponent<TMP_Text>();

        _originalScale = logText.rectTransform.localScale;
    }

    /// <summary>
    /// Call this to output text to the logger.
    /// </summary>
    public static void Output(string text)
    {
        if (Instance == null || Instance.logText == null) return;

        Instance.logText.text += "\n" + text;
        Instance.PlayPopAnimation();
    }
    public static void Output(object text) => Output(text.ToString());

    private void PlayPopAnimation()
    {
        if (_animRoutine != null)
            StopCoroutine(_animRoutine);

        _animRoutine = StartCoroutine(PopAnimation());
    }

    private IEnumerator PopAnimation()
    {
        RectTransform rect = logText.rectTransform;
        float t = 0f;

        // Scale up fast
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / popDuration;
            float scale = Mathf.Lerp(1f, popScale, EaseOutCubic(progress));
            rect.localScale = _originalScale * scale;
            yield return null;
        }

        // Snap back smoothly
        t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = t / popDuration;
            float scale = Mathf.Lerp(popScale, 1f, EaseOutCubic(progress));
            rect.localScale = _originalScale * scale;
            yield return null;
        }

        rect.localScale = _originalScale;
        _animRoutine = null;
    }

    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
