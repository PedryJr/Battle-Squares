using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class ButtonHoverAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private UnityEvent clickEvent;

    private TextMeshProUGUI tmp;
    private Button button;
    private ScrollRect scrollRect;
    private SpriteRenderer spriteRenderer;
    public Image image;

    private UIAudio uIAudio;
    public Inputs input;

    private Vector2 initSize;
    private Vector2 offHoveredSize;
    private Vector2 onHoveredSize;
    private Vector2 onClickedSize;

    private Vector2 fromSize;
    private Vector2 toSize;
    private Vector2 currentSize;

    private Vector2 tmpPos;
    private Vector2 tmpSize;

    public Color offHoveredColor;
    public Color onHoveredColor;
    public bool ignoreHoverColorOptions = false;
    public bool animateColor;

    public ButtonHoverAnimationColorSettings hoverColorOptions;
    public Material overrideMaterial;

    private Color fromColor;
    public Color toColor;

    public bool isHovering;
    private bool animatingClick;

    [SerializeField] private float enterHoverTransitionTime = 0.1f;
    [SerializeField] private float exitHoverTransitionTime = 0.1f;
    [SerializeField] private float clickTransitionTime = 0.1f;

    [SerializeField] private float multiplier = 1f;
    [SerializeField] private bool inverseScroll;

    private float animationTimer;

    [SerializeField] private AnimationType animationType;
    [SerializeField] private SoundInteractionHoverType soundInteractionHoverType;
    [SerializeField] private SoundInteractionClickType soundInteractionClickType;

    private Material unique;
    private bool uiRegClaimed;

    private Action<InputAction.CallbackContext> scrollCallback;

    #region Unity Lifecycle

    private void Awake()
    {
        if (!rectTransform) rectTransform = GetComponent<RectTransform>();

        image = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        button = GetComponent<Button>();
        tmp = GetComponentInChildren<TextMeshProUGUI>();
        scrollRect = GetComponentInParent<ScrollRect>();

        if (!hoverColorOptions)
            hoverColorOptions = AssetResources.GetDefaultButtonHoverColorSettings;

        uIAudio = Resources.Load<UIAudio>("UIAudio");

        SetupVisuals();
        SetupSizes();
        SetupInput();
    }

    private void OnEnable()
    {
        SetupEventTriggers();
        ResetState();
    }

    private void Update()
    {
        UpdateColorsFromSettings();
        SyncTargetColorIfNeeded();
        Animate();
        ApplyAnimation();
    }

    private void OnDisable()
    {
        RemoveEventTriggers();
        ReleaseUIReg();
        DisableInput();
    }

    private void OnDestroy()
    {
        ReleaseUIReg();
        DisableInput();

        if (unique)
            Destroy(unique);
    }

    #endregion

    #region Setup

    private void SetupVisuals()
    {
        if (animateColor && !ignoreHoverColorOptions)
        {
            onHoveredColor = hoverColorOptions.onHoveredColor;
            offHoveredColor = hoverColorOptions.offHoveredColor;
        }

        if (!ignoreHoverColorOptions)
        {
            if (!unique)
            {
                unique = Instantiate(overrideMaterial
                    ? overrideMaterial
                    : AssetResources.GetDefaultButtonMaterial);
            }

            if (image)
            {
                image.material = unique;
                image.color = Color.white;
            }
            else if (spriteRenderer)
            {
                spriteRenderer.material = unique;
                spriteRenderer.color = Color.white;
            }
        }

        if (tmp)
        {
            tmpPos = tmp.rectTransform.localPosition;
            tmpSize = tmp.rectTransform.sizeDelta;
        }

        toColor = offHoveredColor;
    }

    private void SetupSizes()
    {
        initSize = rectTransform.sizeDelta;

        offHoveredSize = initSize;
        onHoveredSize = initSize;
        onClickedSize = initSize;

        Vector2 stretch = new Vector2(initSize.x * 0.05f, 0f) * multiplier;
        Vector2 expand = initSize * 0.03f * multiplier;
        Vector2 clickStretch = new Vector2(initSize.x * 0.02f, 0f) * multiplier;
        Vector2 clickExpand = initSize * 0.016f * multiplier;

        onHoveredSize += animationType == AnimationType.Expand ? expand : stretch;
        onClickedSize -= animationType == AnimationType.Expand ? clickExpand : clickStretch;

        currentSize = fromSize = toSize = initSize;
    }

    private void SetupInput()
    {
        input = new Inputs();

        scrollCallback = ctx =>
        {
            if (!scrollRect || !isHovering)
                return;

            float scroll = inverseScroll ? -ctx.ReadValue<float>() : ctx.ReadValue<float>();

            Vector2 added = new Vector2(
                scrollRect.horizontal ? scroll : 0f,
                scrollRect.vertical ? scroll : 0f
            ) * 100f;

            scrollRect.velocity = Vector2.ClampMagnitude(scrollRect.velocity + added, 1000f);
        };

        input.GameUI.ScrollUI.performed += scrollCallback;
    }

    private void DisableInput()
    {
        if (input != null)
        {
            input.GameUI.ScrollUI.performed -= scrollCallback;
            input.Disable();
        }
    }

    private void ResetState()
    {
        isHovering = false;
        animatingClick = false;
        animationTimer = 0f;

        rectTransform.sizeDelta = offHoveredSize;

        if (tmp)
        {
            tmp.rectTransform.localPosition = tmpPos;
            tmp.rectTransform.sizeDelta = tmpSize;
        }
        SetRenderColor(offHoveredColor);
    }

    #endregion

    #region Hover / Click

    private void OnHover()
    {
        if (isHovering) return;

        PlayHoverSound();
        ClaimUIReg();

        isHovering = true;
        animationTimer = 0f;
        fromSize = rectTransform.sizeDelta;
        toSize = onHoveredSize;

        if (animateColor)
        {
            fromColor = GetCurrentColor();
            toColor = onHoveredColor;
        }

        input.Enable();
    }

    private void ExitHover()
    {
        if (!isHovering) return;

        isHovering = false;
        ReleaseUIReg();

        animationTimer = 0f;
        fromSize = rectTransform.sizeDelta;
        toSize = offHoveredSize;

        if (animateColor)
        {
            fromColor = GetCurrentColor();
            toColor = offHoveredColor;
        }

        input.Disable();
    }

    private void ButtonClick()
    {
        if (animatingClick) return;

        PlayClickSound();

        animatingClick = true;
        animationTimer = 0f;
        fromSize = rectTransform.sizeDelta;
        toSize = onClickedSize;
    }

    #endregion

    #region Animation

    private void Animate()
    {
        float duration =
            animatingClick ? clickTransitionTime :
            isHovering ? enterHoverTransitionTime :
            exitHoverTransitionTime;

        if (duration <= 0f)
        {
            animationTimer = 1f;
            return;
        }

        animationTimer = Mathf.Min(1f, animationTimer + Time.deltaTime / duration);
    }

    private void ApplyAnimation()
    {
        float t = animatingClick
            ? MyExtentions.EaseOnClick(animationTimer)
            : isHovering
                ? MyExtentions.EaseOnHover(animationTimer)
                : MyExtentions.EaseOutQuad(animationTimer);

        currentSize = Vector2.LerpUnclamped(fromSize, toSize, t);
        rectTransform.sizeDelta = currentSize;

        if (animateColor)
            SetRenderColor(Color.Lerp(fromColor, toColor, t));

        if (animatingClick && animationTimer >= 1f)
            FinishClick();
    }

    private void FinishClick()
    {
        animatingClick = false;
        animationTimer = 0f;

        fromSize = rectTransform.sizeDelta;
        toSize = isHovering ? onHoveredSize : offHoveredSize;

        clickEvent?.Invoke();
        if (this && !gameObject.activeInHierarchy)
        {
            ResetState();
            SetupSizes();
        }
    }

    #endregion

    #region Helpers

    private void SetupEventTriggers()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (!trigger)
            trigger = gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        AddTrigger(trigger, EventTriggerType.PointerEnter, _ => OnHover());
        AddTrigger(trigger, EventTriggerType.PointerExit, _ => ExitHover());
        AddTrigger(trigger, EventTriggerType.PointerClick, _ => ButtonClick());
    }

    private void RemoveEventTriggers()
    {
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger)
            trigger.triggers.Clear();
    }

    private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => action(data));
        trigger.triggers.Add(entry);
    }

    private void ClaimUIReg()
    {
        if (uiRegClaimed) return;
        uiRegClaimed = true;
        PlayerController.uiRegs++;
    }

    private void ReleaseUIReg()
    {
        if (!uiRegClaimed) return;
        uiRegClaimed = false;
        PlayerController.uiRegs = Mathf.Max(0, PlayerController.uiRegs - 1);
    }

    private void PlayHoverSound()
    {
        if (!uIAudio) return;

        if (soundInteractionHoverType == SoundInteractionHoverType.Normal) uIAudio.PlayHover(1f);
        if (soundInteractionHoverType == SoundInteractionHoverType.HighPitch) uIAudio.PlayHover(1.2f);
        if (soundInteractionHoverType == SoundInteractionHoverType.LowPitch) uIAudio.PlayHover(0.8f);
    }

    private void PlayClickSound()
    {
        if (!uIAudio) return;

        if (soundInteractionClickType == SoundInteractionClickType.Normal) uIAudio.PlayClick(1f);
        if (soundInteractionClickType == SoundInteractionClickType.HighPitch) uIAudio.PlayClick(1.2f);
        if (soundInteractionClickType == SoundInteractionClickType.LowPitch) uIAudio.PlayClick(0.8f);
    }

    private void SyncTargetColorIfNeeded()
    {
        if (!animateColor)
            return;

        Color desired =
            animatingClick ? toColor :
            isHovering ? onHoveredColor :
            offHoveredColor;

        if (toColor != desired)
        {
            fromColor = GetCurrentColor();
            toColor = desired;
            animationTimer = 0f;
        }
    }


    private void UpdateColorsFromSettings()
    {
        if (ignoreHoverColorOptions || !hoverColorOptions) return;

        onHoveredColor = hoverColorOptions.onHoveredColor;
        offHoveredColor = hoverColorOptions.offHoveredColor;
    }

    public Color GetCurrentColor()
    {
        if (unique)
            return unique.GetColor("_Color");

        if (image)
            return image.color;

        if (spriteRenderer)
            return spriteRenderer.color;

        return Color.white;
    }

    public void SetRenderColor(Color color)
    {
        if (unique)
            unique.SetColor("_Color", color);
        else if (image)
            image.color = color;
        else if (spriteRenderer)
            spriteRenderer.color = color;
    }

    #endregion

    private enum AnimationType { Stretch, Expand }
    private enum SoundInteractionHoverType { Normal, HighPitch, LowPitch, None }
    private enum SoundInteractionClickType { Normal, HighPitch, LowPitch, None }
}
