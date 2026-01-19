using TMPro;
using UnityEngine;

public class UgcOwnershipFilter : MonoBehaviour
{

    WorkshopLoader workshopLoader;
    private void Awake() => workshopLoader = GetComponentInChildren<WorkshopLoader>();

    public void TOGGLEOWNER(TMP_Text field)
    {
        WorkshopFilterSettings.ugcOwnershipType++;
        if ((int)WorkshopFilterSettings.ugcOwnershipType > System.Enum.GetValues(typeof(UgcOwnershipType)).Length - 1) WorkshopFilterSettings.ugcOwnershipType = 0;
        field.text = WorkshopFilterSettings.ugcOwnershipType.ToString();
        workshopLoader.UpdateSearch();
    }

    public void TOGGLESORTING(TMP_Text field)
    {
        WorkshopFilterSettings.ugcSortOrder++;
        if ((int)WorkshopFilterSettings.ugcSortOrder > System.Enum.GetValues(typeof(UgcSortOrder)).Length - 1) WorkshopFilterSettings.ugcSortOrder = 0;
        field.text = WorkshopFilterSettings.ugcSortOrder.ToString();
        workshopLoader.UpdateSearch();
    }

}
