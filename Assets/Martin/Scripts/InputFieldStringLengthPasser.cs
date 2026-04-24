using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldStringLengthPasser : MonoBehaviour
{
    [SerializeField] private WorkshopSectionTitleScript workshopSectionTitle;
    [SerializeField] private InputField inputField;

    public void PassAlongStringLength()
    {
        if (workshopSectionTitle != null && inputField != null)
        {
            workshopSectionTitle.UpdateQuantity(inputField.text.Length);
        }
    }
}
