using System;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public abstract class ModularCursor : MonoBehaviour
{
    [SerializeField]
    private CursorManager.CursorType type;
    public CursorManager.CursorType Type => type;
    private CursorActions cursorActions;
    protected Action<MouseMovementData> onMouseMoved = empty => {};
    protected Action<MouseClickData> onMouseClicked = empty => {};
    private Vector2 lastScreenPos;


    protected virtual void Awake()
    {
        cursorActions = new CursorActions();
        cursorActions.CursorMaps.ScreenPosition.performed += CursorPositionChanged;
        cursorActions.CursorMaps.ScreenPosition.canceled += CursorPositionChanged;

        cursorActions.CursorMaps.RightClick.performed += OnMouseRightClick;
        cursorActions.CursorMaps.LeftClick.performed += OnMouseLeftClick;
        cursorActions.CursorMaps.MiddleClick.performed += OnMouseMiddleClick;
        cursorActions.CursorMaps.RightClick.canceled += OffMouseRightClick;
        cursorActions.CursorMaps.LeftClick.canceled += OffMouseLeftClick;
        cursorActions.CursorMaps.MiddleClick.canceled += OffMouseMiddleClick;

    }

    void OnMouseRightClick(CallbackContext obj) => onMouseClicked(new MouseClickData { clickType = MouseClickData.ClickType.Right, On = true });
    void OnMouseLeftClick(CallbackContext obj) => onMouseClicked(new MouseClickData { clickType = MouseClickData.ClickType.Left, On = true });
    void OnMouseMiddleClick(CallbackContext obj) => onMouseClicked(new MouseClickData { clickType = MouseClickData.ClickType.Middle, On = true });
    void OffMouseRightClick(CallbackContext obj) => onMouseClicked(new MouseClickData { clickType = MouseClickData.ClickType.Right, On = false });
    void OffMouseLeftClick(CallbackContext obj) => onMouseClicked(new MouseClickData { clickType = MouseClickData.ClickType.Left, On = false });
    void OffMouseMiddleClick(CallbackContext obj) => onMouseClicked(new MouseClickData { clickType = MouseClickData.ClickType.Middle, On = false });

    protected virtual void OnDestroy() => cursorActions.Dispose();

    private void CursorPositionChanged(CallbackContext obj)
    {
        Vector2 screenPosition = obj.ReadValue<Vector2>();
        Vector2 screenDelta = screenPosition - lastScreenPos;

        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 worldDelta = Camera.main.ScreenToWorldPoint(screenPosition) - Camera.main.ScreenToWorldPoint(lastScreenPos);

        MouseMovementData data = new MouseMovementData
        {
            screenPosition = screenPosition,
            screenDelta = screenDelta,

            worldPosition = worldPosition,
            worldDelta = worldDelta
        };

        lastScreenPos = screenPosition;

        onMouseMoved(data);
    }

    public abstract void OnHideCursor();
    public abstract void OnShowCursor();

    public void HideCursor()
    {
        OnHideCursor();
        cursorActions.Disable();
        gameObject.SetActive(false);
    }

    public void ShowCursor()
    {
        OnShowCursor();
        cursorActions.Enable();
        gameObject.SetActive(true);
    }


    protected struct MouseMovementData
    {
        public Vector3 worldPosition;
        public Vector3 worldDelta;
        public Vector3 screenPosition;
        public Vector3 screenDelta;
    }

    protected struct MouseClickData
    {
        public bool On;
        public ClickType clickType;
        public enum ClickType
        {
            Right, Left, Middle
        }
    }
}
