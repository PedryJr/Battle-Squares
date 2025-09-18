using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public sealed class ScrollingElementBehaviour : MonoBehaviour
{

    public Inputs input;
    ScrollRect scrollRect;
    SpriteRenderer spriteRenderer;
    Image image;

    public Color offHoveredColor;
    public Color currentColor;
    public Color fromColor;
    public Color toColor;
    public Color onHoveredColor;

    public bool isHovering;
    public bool animateColor;

    [SerializeField]
    float enterHoverTransitionTime;
    [SerializeField]
    float exitHoverTransitionTime;
    [SerializeField]
    float clickTransitionTime;
    [SerializeField]
    bool inverseScroll;
    [SerializeField]
    bool dormant;


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
            float scroll = inverseScroll ? -context.ReadValue<float>() : context.ReadValue<float>();

            Vector2 capturedVelocity = scrollRect.velocity;
            Vector2 addedVelocity = new Vector2(scroll, scroll) * 100;

            if (!scrollRect.vertical) addedVelocity.y = 0;
            if (!scrollRect.horizontal) addedVelocity.x = 0;

            capturedVelocity = Vector2.ClampMagnitude(capturedVelocity + addedVelocity, 1000);

            scrollRect.velocity = capturedVelocity;


        };

    }


    private void OnEnable()
    {

        SetupEventTriggers();

        ExitHover();

    }


    void OnHover()
    {

        input.Enable();

        isHovering = true;

        PlayerController.uiRegs += 1;

    }

    public void ExitHover()
    {

        input.Disable();

        isHovering = false;

        PlayerController.uiRegs = Mathf.Clamp01(PlayerController.uiRegs - 1);

    }

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

    private void OnDestroy()
    {

        if (input != null)
        {
            input.Disable();
            input.Dispose();
        }

    }

}