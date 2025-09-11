using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelEditorButton : MonoBehaviour
{


    [SerializeField]
    private RectTransform rectTransform;

    [SerializeField]
    private UnityEvent clickEvent;

    private TextMeshProUGUI tmp;

    private Button button;
    private ScrollRect scrollRect;
    private SpriteRenderer spriteRenderer;

    [NonSerialized]
    public Image image;

    private Vector2 offHoveredSize;
    private Vector2 onHoveredSize;
    private Vector2 onClickedSize;
    private Vector2 currentSize;
    private Vector2 offsetSizeStretch;
    private Vector2 offsetSizeExpand;
    private Vector2 offsetSizeClickedStretch;
    private Vector2 offsetSizeClickedExpand;
    private Vector2 fromSize;
    private Vector2 toSize;
    private Vector2 initSize;
    private Vector2 tmpPos;
    private Vector2 tmpSize;

    public Color offHoveredColor;
    public Color currentColor;
    public Color fromColor;
    public Color toColor;
    public Color onHoveredColor;

    public bool isHovering;
    public bool animateColor;
    private bool animatingClick = false;

    [SerializeField]
    private float enterHoverTransitionTime;

    [SerializeField]
    private float exitHoverTransitionTime;

    [SerializeField]
    private float clickTransitionTime;

    [SerializeField]
    private float multiplier = 1;

    [SerializeField]
    private bool inverseScroll;

    private float animationTimer;

    [SerializeField]
    private AnimationType animationType;

    private void Awake()
    {

        if (animateColor)
        {
            image = GetComponent<Image>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer) offHoveredColor = spriteRenderer.color;
            else if (image) offHoveredColor = image.color;

            currentColor = offHoveredColor;
            fromColor = offHoveredColor;
            toColor = offHoveredColor;
        }

        tmp = GetComponentInChildren<TextMeshProUGUI>();

        if (tmp)
        {
            tmpPos = tmp.rectTransform.localPosition;
            tmpSize = tmp.rectTransform.sizeDelta;
        }

        scrollRect = GetComponentInParent<ScrollRect>();

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
    }

    private void OnEnable()
    {
        SetupEventTriggers();

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

    private void OnHover()
    {

        isHovering = true;

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

    public void ExitHover()
    {

        isHovering = false;

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

        if (animatingClick) RunClickEvent();
    }

    public void ButtonClick()
    {
        if (animatingClick) return;

        animatingClick = true;
        animationTimer = 0;
        fromSize = rectTransform.sizeDelta;
        toSize = onClickedSize;
    }

    private void Animate()
    {
        animationTimer =

            animatingClick ?
                animationTimer < 1 ? animationTimer + (Time.deltaTime / clickTransitionTime) : 1
                :
                isHovering ? animationTimer < 1 ? animationTimer + (Time.deltaTime / enterHoverTransitionTime) : 1
                :
                animationTimer < 1 ? animationTimer + (Time.deltaTime / exitHoverTransitionTime) : 1;
    }

    private void SetupEventTriggers()
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

    public void RemoveTriggers()
    {
        EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();
        if (eventTrigger) Destroy(gameObject.GetComponent<EventTrigger>());
    }

    private void OnDestroy()
    {

        clickEvent.RemoveAllListeners();
    }

    private void OnDisable()
    {
        clickEvent.RemoveAllListeners();

        EventTrigger eventTrigger = GetComponent<EventTrigger>();

        if (eventTrigger) Destroy(eventTrigger);
    }

    #endregion Setup

    private void ApplyAnimation()
    {
        if (animatingClick)
        {
            float lerp;

            lerp = Mathf.SmoothStep(0, 1, animationTimer);
            currentSize = Vector2.LerpUnclamped(fromSize, toSize, lerp);

            if (animationTimer > 1) RunClickEvent();
        }
        else
        {
            float lerp;
            lerp = isHovering ? Mathf.SmoothStep(0, 1, animationTimer) : Mathf.SmoothStep(0, 1, animationTimer);
            currentSize = Vector2.LerpUnclamped(fromSize, toSize, lerp);

            if (animateColor)
            {
                if (spriteRenderer) spriteRenderer.color = Color.Lerp(fromColor, toColor, lerp);
                else image.color = Color.Lerp(fromColor, toColor, lerp);
            }
        }

        rectTransform.sizeDelta = currentSize;
    }

    public void RunClickEvent()
    {
        fromSize = rectTransform.sizeDelta;
        toSize = isHovering ? onHoveredSize : offHoveredSize;
        animationTimer = 0;
        animatingClick = false;

        clickEvent?.Invoke();
    }

    private enum AnimationType
    {
        Stretch,
        Expand
    }

}
