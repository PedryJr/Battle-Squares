using TMPro;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[BurstCompile]
public sealed class ScrollingElementBehaviour : MonoBehaviour
{

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

    public Color offHoveredColor;
    public Color currentColor;
    public Color fromColor;
    public Color toColor;
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

    [BurstCompile]
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

    }

    [BurstCompile]
    private void OnEnable()
    {

        SetupEventTriggers();

        ExitHover();

    }

    [BurstCompile]
    void OnHover()
    {

        input.Enable();

        isHovering = true;

    }
    [BurstCompile]
    public void ExitHover()
    {

        input.Disable();

        isHovering = false;

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

        if (input != null)
        {
            input.Disable();
            input.Dispose();
        }

    }

}