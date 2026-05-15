using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AutoReadyIconScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform spinningArrows;
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite onIcon;
    [SerializeField] private Sprite offIcon;

    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve arrowSpinCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private float arrowSpinDuration = 1f;
    [SerializeField] private float fadeDuration = 0.2f;

    public bool IsAutoReady { get; private set; }

    private Coroutine _activeSpinRoutine;
    private Coroutine _activeFadeRoutine;

    public void SetAutoReady(bool isOn)
    {
        if (IsAutoReady == isOn) return;
        IsAutoReady = isOn;

        if (_activeFadeRoutine != null) StopCoroutine(_activeFadeRoutine);
        _activeFadeRoutine = StartCoroutine(FadeIconRoutine(IsAutoReady));

        // Only spin arrows when turning on
        if (IsAutoReady)
        {
            if (_activeSpinRoutine != null) StopCoroutine(_activeSpinRoutine);
            _activeSpinRoutine = StartCoroutine(ArrowSpinRoutine());
        }
    }

    public void ToggleAutoReady() => SetAutoReady(!IsAutoReady);

    private IEnumerator FadeIconRoutine(bool turningOn)
    {
        float halfDuration = fadeDuration / 2f;
        Color fullAlpha = Color.white;
        Color zeroAlpha = new Color(1, 1, 1, 0);


        yield return StartCoroutine(LerpAlpha(fullAlpha, zeroAlpha, halfDuration));

        iconImage.sprite = turningOn ? onIcon : offIcon;

        yield return StartCoroutine(LerpAlpha(zeroAlpha, fullAlpha, halfDuration));

        _activeFadeRoutine = null;
    }

    private IEnumerator LerpAlpha(Color startColor, Color endColor, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            iconImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
        }
        iconImage.color = endColor;
    }

    private IEnumerator ArrowSpinRoutine()
    {
        float elapsed = 0f;

        while (elapsed < arrowSpinDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / arrowSpinDuration;

            float rotationZ = arrowSpinCurve.Evaluate(t) * 360f;
            spinningArrows.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

            yield return null;
        }

        spinningArrows.localRotation = Quaternion.identity;
        _activeSpinRoutine = null;
    }
}