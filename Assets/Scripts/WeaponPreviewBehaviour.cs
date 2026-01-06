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
    ProjectileManager projectileManager;

    public ushort previewing = 0;

    private void Awake()
    {
        projectileManager = FindAnyObjectByType<ProjectileManager>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        image = GetComponent<Image>();
        weapons = weaponSelectorContent.GetComponentsInChildren<WeaponSelector>(true);
    }

    private void Update()
    {

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;
        if (!playerSynchronizer.localSquare.nozzleBehaviour) return;

        PlayerBehaviour localPlayer = playerSynchronizer.localSquare;

        if (previewType == WeaponPreviewType.Primary)
        {
            previewing = localPlayer.nozzleBehaviour.primary;
            WeaponBuilder weapon = projectileManager.GetWeaponBuilderByTypeID(previewing);
            image.sprite = weapon.GetSprite;
        }
        else
        {
            previewing = localPlayer.nozzleBehaviour.secondary;
            WeaponBuilder weapon = projectileManager.GetWeaponBuilderByTypeID(previewing);
            image.sprite = weapon.GetSprite;
        }

        Color colorReference = playerSynchronizer.localSquare.PlayerColor.SelectedWeaponColor;
        Vector3 colorVector = new Vector3(colorReference.r, colorReference.g, colorReference.b).normalized;
        Color displayColor = new Color(colorVector.x, colorVector.y, colorVector.z, 1);

        image.color = displayColor;

    }

    public enum WeaponPreviewType
    {
        Primary, Secondary
    }

}
