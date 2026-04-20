using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class GameLogoTransitionScript : MonoBehaviour
{
    [Header("Groups")]
    [SerializeField] private CanvasGroup pressAnyKeyGroup;
    [SerializeField] private CanvasGroup mainMenuGroup;

    [Header("Logo Elements")]
    [SerializeField] private RectTransform logoOne;
    [SerializeField] private RectTransform logoTwo;
    [SerializeField] private TMPro.TextMeshProUGUI pressAnyKeyText;

    [Header("Settings")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve textScaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [SerializeField] private float introFadeDuration = 1.0f;
    [SerializeField] private float textFadeOutDuration = 1.0f;
    [SerializeField] private float transitionDuration = 0.5f;

    private bool isTransitioning = false;
    private bool transitionDone = false;
    private bool canStartTransition = false;

    private void Awake()
    {
        pressAnyKeyGroup.alpha = 0;
        pressAnyKeyGroup.gameObject.SetActive(true);
        mainMenuGroup.gameObject.SetActive(false);

        StartCoroutine(InitialFadeInCoroutine());
    }
    private IEnumerator InitialFadeInCoroutine()
    {
        float elapsed = 0;
        while (elapsed < introFadeDuration)

        {
            // for some reason frame one the originAnchor has scale 0, so we gotta skip the first frame, but this way it should skip as many frames as needed.
            if (logoOne.sizeDelta == Vector2.zero) yield return null; 

            // time management
            elapsed += Time.deltaTime;
            float alpha = elapsed / introFadeDuration;

            // fade in
            pressAnyKeyGroup.alpha = alpha;
            yield return null;
        }
        canStartTransition = true;
    }
    private void Update()
    {
        if (canStartTransition && !transitionDone && !isTransitioning && Input.anyKey)
        {
            StartCoroutine(TransitionCoroutine());
        }
    }
    private IEnumerator TransitionCoroutine()
    {
        isTransitioning = true;

        //// uncomment to be able to retrigger the transition
        //pressAnyKeyText.rectTransform.localScale = Vector3.one;
        //pressAnyKeyGroup.gameObject.GetComponent<VerticalLayoutGroup>().enabled = true;
        //pressAnyKeyGroup.gameObject.SetActive(true);
        //logoTwo.GetComponent<Image>().canvasRenderer.SetAlpha(0);
        //mainMenuGroup.alpha = 0;

        #region Fade out the text before moving logo

        float fadeElapsed = 0f;

        while (fadeElapsed < textFadeOutDuration)
        {
            fadeElapsed += Time.deltaTime;
            float transitionProgress = Mathf.Clamp01(fadeElapsed / textFadeOutDuration);

            pressAnyKeyText.rectTransform.localScale = Vector3.one * textScaleCurve.Evaluate(transitionProgress);
            yield return null;
        }

        pressAnyKeyText.rectTransform.localScale = Vector3.zero; // hide the text after fading out

        #endregion

        #region Moving the logo

        logoTwo.GetComponent<Image>().canvasRenderer.SetAlpha(0);

        mainMenuGroup.alpha = 0;
        mainMenuGroup.gameObject.SetActive(true);

        // corners[0] is bottom-left, [2] is top-right, add them together and divide by 2 to get the point right inbetween them
        Vector3[] corners = new Vector3[4];
        logoOne.GetWorldCorners(corners);
        Vector3 oldPositionCentre = (corners[0] + corners[2]) / 2;

        Vector2 originalSize = logoOne.rect.size;
        Vector3 originalScale = logoOne.localScale;

        // disable layout group so we can control its position again
        pressAnyKeyGroup.gameObject.GetComponent<VerticalLayoutGroup>().enabled = false;

        // set the anchors because otherwise the locations dont transfer correctly
        logoOne.anchorMin = logoTwo.anchorMin;
        logoOne.anchorMax = logoTwo.anchorMax;
        logoOne.pivot = logoTwo.pivot;

        // yuh
        logoOne.position = oldPositionCentre;
        SetDefaultPositionByPivot(logoOne, oldPositionCentre);

        float elapsedTime = 0f;
        Vector3 startPos = logoOne.position;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / transitionDuration);

            // Evaluate the curve (this will return values < 0 and > 1 based on your image)
            float t = transitionCurve.Evaluate(normalizedTime);

            // Use Unclamped for position to allow the "dip" and "overshoot"
            logoOne.position = Vector3.LerpUnclamped(startPos, logoTwo.position, t);

            // You mentioned you want these to stay linear
            // We use 'normalizedTime' (0 to 1) instead of 't' to keep them linear
            logoOne.sizeDelta = Vector3.LerpUnclamped(originalSize, logoTwo.sizeDelta, t);
            logoOne.localScale = Vector3.LerpUnclamped(originalScale, logoTwo.localScale, t);

            mainMenuGroup.alpha = t;

            yield return null;
        }

        // set it to be sure
        MatchTransform(logoOne, logoTwo);

        // show the real target and hide the moving placeholder
        logoTwo.GetComponent<Image>().canvasRenderer.SetAlpha(1);

        // Clean up
        pressAnyKeyGroup.gameObject.SetActive(false);
        #endregion

        isTransitioning = false;
        transitionDone = true;
    }

    // ai generated code that i dont understand and makes me want to die
    private void SetDefaultPositionByPivot(RectTransform rect, Vector3 worldCenter)
    {
        Vector2 pivotOffset = new Vector2(rect.pivot.x - 0.5f, rect.pivot.y - 0.5f);
        Vector2 localOffset = new Vector2(pivotOffset.x * rect.rect.width, pivotOffset.y * rect.rect.height);
        rect.position = worldCenter + rect.TransformVector(localOffset);
    }
    private void MatchTransform(RectTransform source, RectTransform target)
    {
        source.position = target.position;
        source.sizeDelta = target.sizeDelta;
        source.localScale = target.localScale;
    }
}