using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ButtonHoverAnimation : MonoBehaviour
{

    [SerializeField]
    RectTransform rectTransform;

    [SerializeField] 
    UnityEvent clickEvent;

    TextMeshProUGUI tmp;

    Button button;
    public Inputs input;
    ScrollRect scrollRect;
    SpriteRenderer spriteRenderer;
    Image image;

    Vector2 offHoveredSize;
    Vector2 onHoveredSize;
    Vector2 onClickedSize;
    Vector2 currentSize;
    Vector2 offsetSizeStretch;
    Vector2 offsetSizeExpand;
    Vector2 offsetSizeClickedStretch;
    Vector2 offsetSizeClickedExpand;
    Vector2 fromSize;
    Vector2 toSize;
    Vector2 initSize;
    Vector2 tmpPos;
    Vector2 tmpSize;

    Color offHoveredColor;
    Color currentColor;
    Color fromColor;
    Color toColor;
    public Color onHoveredColor;

    public bool isHovering;
    public bool animateColor;
    bool animatingClick = false;

    [SerializeField]
    float enterHoverTransitionTime;
    [SerializeField]
    float exitHoverTransitionTime;
    [SerializeField]
    float clickTransitionTime;
    [SerializeField]
    float multiplier = 1;

    float animationTimer;

    [SerializeField]
    AnimationType animationType;

    const float c1 = 1.70158f;
    const float c3 = c1 + 1;

    private void Awake()
    {

        if (animateColor)
        {
            image = GetComponent<Image>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if(spriteRenderer) offHoveredColor = spriteRenderer.color;
            else if (image) offHoveredColor = image.color;

            currentColor = offHoveredColor;
            fromColor = offHoveredColor;
            toColor = offHoveredColor;

        }

        tmp = GetComponentInChildren<TextMeshProUGUI>();

        if(tmp)
        {
            tmpPos = tmp.rectTransform.localPosition;
            tmpSize = tmp.rectTransform.sizeDelta;
        }

        scrollRect = GetComponentInParent<ScrollRect>();

        input = new Inputs();

        input.GameUI.ScrollUI.performed += (context) =>
        {
            if (!scrollRect) return;
            if (!isHovering) return;
            float scroll = context.ReadValue<float>();

            Vector2 capturedVelocity = scrollRect.velocity;
            Vector2 addedVelocity = new Vector2(scroll, scroll) * 100;

            if (!scrollRect.vertical) addedVelocity.y = 0;
            if (!scrollRect.horizontal) addedVelocity.x = 0;

            capturedVelocity = Vector2.ClampMagnitude(capturedVelocity + addedVelocity, 1000);

            scrollRect.velocity = capturedVelocity;

        };

        if (!rectTransform) rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();

        initSize = rectTransform.sizeDelta;

        offHoveredSize = initSize;
        onHoveredSize = initSize;
        onClickedSize = initSize;

        currentSize = initSize;
        fromSize = initSize;
        toSize = initSize;

        offsetSizeStretch = new Vector2(initSize.x * 0.05f, 0) * multiplier;
        offsetSizeExpand = new Vector2(initSize.x * 0.03f, initSize.y * 0.03f) * multiplier;
        offsetSizeClickedStretch = new Vector2(initSize.x * 0.02f, 0) * multiplier;
        offsetSizeClickedExpand = new Vector2(initSize.x * 0.016f, initSize.y * 0.016f) * multiplier;

        onHoveredSize += animationType == AnimationType.Expand ? offsetSizeExpand : offsetSizeStretch;
        onClickedSize -= animationType == AnimationType.Expand ? offsetSizeClickedExpand : offsetSizeClickedStretch;

        SetupEventTriggers();

    }

    private void OnEnable()
    {
        rectTransform.sizeDelta = offHoveredSize;
        if (tmp)
        {
            tmp.rectTransform.localPosition = tmpPos;
            tmp.rectTransform.sizeDelta = tmpSize;
        }

        ExitHover();

    }

    private void Update()
    {

        Animate();

        ApplyAnimation();

    }

    #region Setup

    void OnHover()
    {

        input.Enable();

        isHovering = true;

        PlayerController.uiRegs += 1;

        if (animatingClick) return;

        animationTimer = 0;
        fromSize = rectTransform.sizeDelta;
        toSize = onHoveredSize;

        if (animateColor)
        {
            toColor = onHoveredColor;

            if (spriteRenderer)
            {
                fromColor = spriteRenderer.color;
            }
            else
            {
                fromColor = image.color;
            }
        }

    }

    void ExitHover()
    {

        input.Disable();

        isHovering = false;

        PlayerController.uiRegs -= 1;
        if (PlayerController.uiRegs < 0) PlayerController.uiRegs = 0;

        if (animatingClick) return;

        animationTimer = 0;
        fromSize = rectTransform.sizeDelta;
        toSize = offHoveredSize;

        if (animateColor)
        {
            toColor = offHoveredColor;

            if (spriteRenderer)
            {
                fromColor = spriteRenderer.color;
            }
            else
            {
                fromColor = image.color;
            }
        }

    }

    void ButtonClick()
    {

        if (animatingClick) return;

        animatingClick = true;
        animationTimer = 0;
        fromSize = rectTransform.sizeDelta;
        toSize = onClickedSize;

    }

    void Animate()
    {

        animationTimer = 

            animatingClick ? 
                animationTimer < 1 ? animationTimer + (Time.deltaTime / clickTransitionTime) : 1
                :
                isHovering ? animationTimer < 1 ? animationTimer + (Time.deltaTime / enterHoverTransitionTime) : 1
                :
                animationTimer < 1 ? animationTimer + (Time.deltaTime / exitHoverTransitionTime) : 1;

    }

    void SetupEventTriggers()
    {

        EventTrigger eventTrigger = GetComponent<EventTrigger>();

        if (!eventTrigger) eventTrigger = gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };

        pointerEnterEntry.callback.AddListener((eventData) => { OnHover(); });
        pointerExitEntry.callback.AddListener((eventData) => { ExitHover(); });
        pointerClickEntry.callback.AddListener((eventData) => { ButtonClick(); });

        eventTrigger.triggers.Add(pointerEnterEntry);
        eventTrigger.triggers.Add(pointerExitEntry);
        eventTrigger.triggers.Add(pointerClickEntry);

    }

    private void OnDestroy()
    {

        if (isHovering)
        {
            PlayerController.uiRegs -= 1;
            if (PlayerController.uiRegs < 0) PlayerController.uiRegs = 0;
        }

        if (input != null)
        {
            input.Disable();
            input.Dispose();
        }

    }

    #endregion

    void ApplyAnimation()
    {


        if (animatingClick) 
        {

            float lerp;

            lerp = EaseOnClick(animationTimer);
            currentSize = Vector2.LerpUnclamped(fromSize, toSize, lerp);

            if(animationTimer > 1) RunClickEvent();

        }
        else
        {
            float lerp;
            lerp = isHovering ? EaseOnHover(animationTimer) : EaseOffHover(animationTimer);
            currentSize = Vector2.LerpUnclamped(fromSize, toSize, lerp);

            if (animateColor)
            {

                if (spriteRenderer) spriteRenderer.color = Color.Lerp(fromColor, toColor, lerp);
                else image.color = Color.Lerp(fromColor, toColor, lerp);
            }

        }

        rectTransform.sizeDelta = currentSize;

    }

    void RunClickEvent()
    {

        fromSize = rectTransform.sizeDelta;
        toSize = isHovering ? onHoveredSize : offHoveredSize;
        animationTimer = 0;
        animatingClick = false;

        clickEvent?.Invoke();

    }

    float EaseOnHover(float x) 
    {
        
        return 1 - Mathf.Pow(1 - x, 5);

    }

    float EaseOffHover(float x)
    {

        float f1, f2, sum;
        float a, b;
        a = 4.1f;
        b = -3.8f;

        f1 = c3 * x * x * x - c1 * x * x;
        f2 = Mathf.Exp(-a * x) * Mathf.Sin(b * x);

        sum = f1 + f2;

        return sum;

    }

    float EaseOnClick(float x)
    {

        return 1 - Mathf.Pow(1 - x, 5);

    }

    enum AnimationType
    {
        Stretch,
        Expand
    }

}
