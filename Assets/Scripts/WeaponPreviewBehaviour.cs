using UnityEngine;
using UnityEngine.UI;
using static ProjectileManager;

public sealed class WeaponPreviewBehaviour : MonoBehaviour
{

    [SerializeField]
    WeaponPreviewType previewType;

    [SerializeField]
    Transform weaponSelectorContent;

    WeaponSelector[] weapons;

    Image image;

    PlayerSynchronizer playerSynchronizer;

    public ProjectileType weaponType;

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
                    weaponType = weaponSelector.weaponType;
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
                    weaponType = weaponSelector.weaponType;
                }
            }
        }
    }

    private void Update()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;
        if (!playerSynchronizer.localSquare.nozzleBehaviour) return;

        if (previewType == WeaponPreviewType.Primary)
        {
            foreach (WeaponSelector weaponSelector in weapons)
            {
                if (weaponSelector.weaponType == playerSynchronizer.localSquare.nozzleBehaviour.primary)
                {
                    weaponType = weaponSelector.weaponType;
                }
            }
        }
        else
        {
            foreach (WeaponSelector weaponSelector in weapons)
            {
                if (weaponSelector.weaponType == playerSynchronizer.localSquare.nozzleBehaviour.secondary)
                {
                    weaponType = weaponSelector.weaponType;
                }
            }
        }

        Color colorReference = playerSynchronizer.localSquare.playerColor;
        Vector3 colorVector = new Vector3(colorReference.r, colorReference.g, colorReference.b).normalized;
        Color displayColor = new Color(colorVector.x, colorVector.y, colorVector.z, 1);

        image.color = displayColor;

    }

    public enum WeaponPreviewType
    {
        Primary, Secondary
    }

}
