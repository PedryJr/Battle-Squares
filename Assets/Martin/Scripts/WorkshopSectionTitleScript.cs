using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopSectionTitleScript : MonoBehaviour
{
    public enum Status { Green, Yellow, Red }
    [SerializeField] private Status status = Status.Green;

    [Tooltip("The maximum quantity allowed for this section. Set to -1 for unlimited.")]
    [SerializeField] private int quantityLimit = -1;
    private int currentQuantity = 0;

    [Header("Visual Settings")]
    [Tooltip("The color of the text based on current quantity vs limit (0 to 1).")]
    [SerializeField] private Gradient quantityGradient;

    [Header("References")]
    [SerializeField] private Sprite successIcon;
    [SerializeField] private Sprite warningIcon;
    [SerializeField] private Sprite errorIcon;
    [SerializeField] private Image statusIconImage;
    [SerializeField] private TextMeshProUGUI quantityTMPComponent;

    private void OnValidate() => UpdateTitle();
    private void OnEnable() => UpdateTitle();

    private void UpdateTitle()
    {
        // 1. Handle Status Override logic
        Status effectiveStatus = status;

        if (quantityLimit > 0)
        {
            // If we hit or exceed the limit, force the status to Red
            if (currentQuantity >= quantityLimit)
            {
                effectiveStatus = Status.Red;
            }

            // 2. Handle Quantity Display and Gradient
            quantityTMPComponent.gameObject.SetActive(true);
            quantityTMPComponent.text = $"{currentQuantity}/{quantityLimit}";

            float progress = Mathf.Clamp01((float)currentQuantity / quantityLimit);
            quantityTMPComponent.color = quantityGradient.Evaluate(progress);
        }
        else
        {
            quantityTMPComponent.gameObject.SetActive(false);
        }

        // 3. Apply Title and Icon
        SetStatusIcon(effectiveStatus);
    }

    public void UpdateQuantity(int newQuantity)
    {
        currentQuantity = newQuantity;
        UpdateTitle();
    }

    public void UpdateStatus(Status newStatus)
    {
        status = newStatus;
        UpdateTitle();
    }

    private void SetStatusIcon(Status iconStatus)
    {
        if (statusIconImage == null) return;

        switch (iconStatus)
        {
            case Status.Green:
                statusIconImage.sprite = successIcon;
                break;
            case Status.Yellow:
                statusIconImage.sprite = warningIcon;
                break;
            case Status.Red:
                statusIconImage.sprite = errorIcon;
                break;
        }
    }
}