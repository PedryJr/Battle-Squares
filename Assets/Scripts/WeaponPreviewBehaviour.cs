using UnityEngine;
using UnityEngine.UI;

public sealed class WeaponPreviewBehaviour : MonoBehaviour
{

    [SerializeField]
    WeaponPreviewType previewType;

    [SerializeField]
    Transform weaponSelectorContent;

    WeaponSelector[] weapons;

    Image image;

    PlayerSynchronizer playerSynchronizer;

    private void OnEnable()
    {
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        image = GetComponent<Image>();
    }

    private void Start()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;
        if (!playerSynchronizer.localSquare.nozzleBehaviour) return;

        weapons = weaponSelectorContent.GetComponentsInChildren<WeaponSelector>();
        if (previewType == WeaponPreviewType.Primary)
        {
            foreach (WeaponSelector weaponSelector in weapons)
            {
                if (weaponSelector.weaponType == playerSynchronizer.localSquare.nozzleBehaviour.primary)
                {
                    image.sprite = weaponSelector.GetImage();
                }
            }
        }
        else
        {
            foreach (WeaponSelector weaponSelector in weapons)
            {
                if (weaponSelector.weaponType == playerSynchronizer.localSquare.nozzleBehaviour.secondary)
                {
                    image.sprite = weaponSelector.GetImage();
                }
            }
        }
    }

    private void Update()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;
        if (!playerSynchronizer.localSquare.nozzleBehaviour) return;

        image.color = playerSynchronizer.localSquare.playerColor * 1.2f;

    }

    public enum WeaponPreviewType
    {
        Primary, Secondary
    }

}
