using TMPro;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[BurstCompile]
public sealed class PixelStateAnimator : MonoBehaviour
{
    [SerializeField]
    public RectTransform rectTransform;

    PixelManager pixelManager;

    Vector2 offHoveredSize;
    Vector2 onHoveredSize;
    Vector2 currentSize;
    Vector2 offsetSizeStretch;
    Vector2 offsetSizeExpand;
    Vector2 fromSize;
    Vector2 toSize;
    Vector2 initSize;

    public bool isHovering;

    [SerializeField]
    float enterHoverTransitionTime;
    [SerializeField]
    float exitHoverTransitionTime;
    [SerializeField]
    float multiplier = 1;

    float animationTimer;

    int siblingIndex;

    [SerializeField]
    AnimationType animationType;

    enum AnimationType
    {
        Stretch,
        Expand
    }

    [BurstCompile]
    private void Awake()
    {

        rectTransform = GetComponent<RectTransform>();
        pixelManager = GetComponentInParent<PixelManager>();

        initSize = rectTransform.sizeDelta;

        offHoveredSize = initSize;
        onHoveredSize = initSize;

        currentSize = initSize;
        fromSize = initSize;
        toSize = initSize;

        offsetSizeStretch = new Vector2(initSize.x * 0.05f, 0) * multiplier;
        offsetSizeExpand = new Vector2(initSize.x * 0.03f, initSize.y * 0.03f) * multiplier;

        onHoveredSize += animationType == AnimationType.Expand ? offsetSizeExpand : offsetSizeStretch;

        siblingIndex = transform.GetSiblingIndex();

    }

    [BurstCompile]
    private void Start()
    {

        pixelManager.currentSize[siblingIndex] = currentSize;
        pixelManager.fromSize[siblingIndex] = fromSize;
        pixelManager.toSize[siblingIndex] = toSize;
        pixelManager.isHovering[siblingIndex] = isHovering;
        pixelManager.enterHoverTransitionTime[siblingIndex] = enterHoverTransitionTime;
        pixelManager.exitHoverTransitionTime[siblingIndex] = exitHoverTransitionTime;
        pixelManager.animationTimer[siblingIndex] = animationTimer;

    }

    [BurstCompile]
    private void OnEnable()
    {

        SetupEventTriggers();

        rectTransform.sizeDelta = offHoveredSize;

    }

    [BurstCompile]
    void OnHover()
    {

        isHovering = true;

        PlayerController.uiRegs += 1;

        animationTimer = 0;
        fromSize = rectTransform.sizeDelta;
        toSize = onHoveredSize;

        pixelManager.animationTimer[siblingIndex] = animationTimer;
        pixelManager.fromSize[siblingIndex] = fromSize;
        pixelManager.toSize[siblingIndex] = toSize;
        pixelManager.isHovering[siblingIndex] = isHovering;

    }
    [BurstCompile]
    public void ExitHover()
    {

        isHovering = false;

        PlayerController.uiRegs -= 1;
        if (PlayerController.uiRegs < 0) PlayerController.uiRegs = 0;

        animationTimer = 0;
        fromSize = rectTransform.sizeDelta;
        toSize = offHoveredSize;

        pixelManager.animationTimer[siblingIndex] = animationTimer;
        pixelManager.fromSize[siblingIndex] = fromSize;
        pixelManager.toSize[siblingIndex] = toSize;
        pixelManager.isHovering[siblingIndex] = isHovering;

    }

    [BurstCompile]
    void SetupEventTriggers()
    {

        EventTrigger eventTrigger = GetComponent<EventTrigger>();

        if (!eventTrigger) eventTrigger = gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };

        pointerEnterEntry.callback.AddListener((eventData) => { OnHover(); });
        pointerExitEntry.callback.AddListener((eventData) => { ExitHover(); });

        eventTrigger.triggers.Add(pointerEnterEntry);
        eventTrigger.triggers.Add(pointerExitEntry);

    }

    [BurstCompile]
    private void OnDestroy()
    {

        if (isHovering)
        {
            PlayerController.uiRegs -= 1;
            if (PlayerController.uiRegs < 0) PlayerController.uiRegs = 0;
        }

    }

    private void OnDisable()
    {

        EventTrigger eventTrigger = GetComponent<EventTrigger>();

        if (eventTrigger) Destroy(eventTrigger);



    }

}
