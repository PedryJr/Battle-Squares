using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIAspectRatioLimiter : MonoBehaviour
{
    [SerializeField] private Vector2 narrowestAspectRatio = new Vector2(4, 3);
    [SerializeField] private Vector2 widestAspectRatio = new Vector2(16, 9);

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private bool isAdjusting = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (transform.parent != null)
            parentRectTransform = transform.parent.GetComponent<RectTransform>();
    }

    private void LimitAspectRatio()
    {
        // 1. Recursion Guard
        if (isAdjusting) return;

        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (parentRectTransform == null)
        {
            if (transform.parent != null)
                parentRectTransform = transform.parent.GetComponent<RectTransform>();
            else
                return; // Can't limit based on parent if no parent exists
        }

        float parentWidth = parentRectTransform.rect.width;
        float parentHeight = parentRectTransform.rect.height;

        if (parentWidth <= 0 || parentHeight <= 0) return;

        float parentAspect = parentWidth / parentHeight;
        float narrowestRatio = narrowestAspectRatio.x / narrowestAspectRatio.y;
        float widestRatio = widestAspectRatio.x / widestAspectRatio.y;

        // Ensure narrowest isn't wider than widest
        narrowestRatio = Mathf.Min(narrowestRatio, widestRatio);

        isAdjusting = true;

        if (parentAspect < narrowestRatio)
        {
            // Parent is too tall/narrow (e.g. Portrait phone)
            // Match the width, shrink the height
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(1, 0.5f);
            rectTransform.sizeDelta = new Vector2(0, parentWidth / narrowestRatio);
        }
        else if (parentAspect > widestRatio)
        {
            // Parent is too wide (e.g. Ultra-wide monitor)
            // Match the height, shrink the width
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.sizeDelta = new Vector2(parentHeight * widestRatio, 0);
        }
        else
        {
            // Within bounds: Stretch to fill parent completely
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
        }

        isAdjusting = false;
    }

    private void OnRectTransformDimensionsChange() => LimitAspectRatio();

    private void OnDrawGizmos() => LimitAspectRatio();

#if UNITY_EDITOR
    private void Update() => LimitAspectRatio();
#endif
    private void OnValidate()
    {
        narrowestAspectRatio.x = Mathf.Max(0.1f, narrowestAspectRatio.x);
        narrowestAspectRatio.y = Mathf.Max(0.1f, narrowestAspectRatio.y);
        widestAspectRatio.x = Mathf.Max(0.1f, widestAspectRatio.x);
        widestAspectRatio.y = Mathf.Max(0.1f, widestAspectRatio.y);
        LimitAspectRatio();
    }
}