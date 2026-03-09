using UnityEngine;

public class DeleteLevelListing : MonoBehaviour
{

    public static bool isDeleting = false;

    DragAndScrollMod _dragMod;

    private void Awake()
    {
        isDeleting = false;
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();
    }

    public void DELETE_LEVEL() => isDeleting = !isDeleting;

}
