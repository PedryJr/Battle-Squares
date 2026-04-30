using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIComponentPercentageScaler : MonoBehaviour
{
    public enum ScalingReference { ImmediateParent, FirstParentCanvas, Manual }

    [Header("Reference Settings")]
    [SerializeField] private ScalingReference referenceSource = ScalingReference.ImmediateParent;

    [Tooltip("Only used if Reference Source is set to 'Manual'")]
    [SerializeField] private RectTransform manualReferenceTransform;

    [Header("Ratio Settings")]
    [Tooltip("If 0, uses the Image sprite's ratio. Otherwise, Width / Height (e.g., 1.77 for 16:9).")]
    [SerializeField] private float manualAspectRatio = 0f;

    [Header("Horizontal Constraints (Reference Relative)")]
    [Range(0.01f, 1.0f)][SerializeField] private float minHorizontalPercentage = 0.1f;
    [Range(0.01f, 1.0f)][SerializeField] private float maxHorizontalPercentage = 1.0f;

    [Header("Vertical Constraints (Reference Relative)")]
    [Range(0.01f, 1.0f)][SerializeField] private float minVerticalPercentage = 0.1f;
    [Range(0.01f, 1.0f)][SerializeField] private float maxVerticalPercentage = 1.0f;

    private RectTransform _rectTransform;
    private RectTransform _referenceRectTransform;
    private Image _image;

    private Vector2 lastScreenSize;

    private void OnEnable()
    {
        CacheReferences();
        ScaleUI();
    }

    private void CacheReferences()
    {
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        if (_image == null) _image = GetComponent<Image>();

        switch (referenceSource)
        {
            case ScalingReference.ImmediateParent:
                if (transform.parent != null)
                    _referenceRectTransform = transform.parent.GetComponent<RectTransform>();
                break;

            case ScalingReference.FirstParentCanvas:
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                    _referenceRectTransform = parentCanvas.GetComponent<RectTransform>();
                break;

            case ScalingReference.Manual:
                _referenceRectTransform = manualReferenceTransform;
                break;
        }
    }

    private bool HasResolutionChanged()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (currentScreenSize != lastScreenSize)
        {
            lastScreenSize = currentScreenSize;
            return true;
        }
        return false;
    }

    private void OnRectTransformDimensionsChange() => ScaleUI();



    private void OnValidate()
    {
        _referenceRectTransform = null;
        ScaleUI();
    }

    private void OnTransformParentChanged()
    {
        _referenceRectTransform = null;
        CacheReferences();
        ScaleUI();
    }

    private void ScaleUI()
    {
        CacheReferences();

        // If Manual is selected but the slot is empty, we bail early to avoid null refs
        if (_referenceRectTransform == null || _rectTransform == null) return;

        Vector2 refSize = _referenceRectTransform.rect.size;

        if (refSize.x <= 0 || refSize.y <= 0) return;

        float targetRatio = GetAspectRatio();

        float minW = refSize.x * minHorizontalPercentage;
        float maxW = refSize.x * maxHorizontalPercentage;
        float minH = refSize.y * minVerticalPercentage;
        float maxH = refSize.y * maxVerticalPercentage;

        float finalWidth = maxW;
        float finalHeight = finalWidth / targetRatio;

        if (finalHeight > maxH)
        {
            finalHeight = maxH;
            finalWidth = finalHeight * targetRatio;
        }

        if (finalWidth < minW)
        {
            finalWidth = minW;
            finalHeight = finalWidth / targetRatio;
        }

        if (finalHeight < minH)
        {
            finalHeight = minH;
            finalWidth = finalHeight * targetRatio;
        }

        if (finalWidth > maxW)
        {
            finalWidth = maxW;
            finalHeight = finalWidth / targetRatio;
        }

        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalWidth);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalHeight);
    }

    private float GetAspectRatio()
    {
        if (manualAspectRatio > 0) return manualAspectRatio;
        if (_image != null && _image.sprite != null)
        {
            return _image.sprite.rect.width / _image.sprite.rect.height;
        }
        return 1f;
    }

#if UNITY_EDITOR
    private void Update() => ScaleUI();
#else
    private void Update()
    {
        if (HasResolutionChanged()) ScaleUI();
    }
#endif
}