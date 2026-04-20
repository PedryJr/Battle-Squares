using UnityEngine;
using UnityEngine.UI;

// This replaces the standard Button component
public class MartinButtonScript : Button
{
    [Header("Custom Button Settings")]
    [SerializeField] private float standardOutlineThickness = 0f;
    [SerializeField] private float hoverOutlineThickness = 10f;

    [Header("UI References")]
    [SerializeField] private Image borderImage;

    // DoStateTransition is the internal "brain" of the Button.
    // It handles Mouse, Gamepad, and Keyboard states automatically.
    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        // Keep the base functionality (Color Tint, Sprite Swap, etc.)
        base.DoStateTransition(state, instant);

        if (borderImage == null) return;

        switch (state)
        {
            case SelectionState.Highlighted:
            case SelectionState.Selected:
            case SelectionState.Pressed:
                // Triggers on Hover OR Gamepad Select
                SetBorderSize(hoverOutlineThickness);
                break;

            case SelectionState.Normal:
            case SelectionState.Disabled:
                SetBorderSize(standardOutlineThickness);
                break;
        }
    }

    private void SetBorderSize(float size)
    {
        borderImage.rectTransform.sizeDelta = new Vector2(size, size);
    }
}