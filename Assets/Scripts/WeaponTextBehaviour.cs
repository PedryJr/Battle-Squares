using System;
using TMPro;
using UnityEngine;
using static WeaponBuilder;

public sealed class WeaponTextBehaviour : MonoBehaviour
{

    TMP_Text equippedClassesField;

    PlayerSynchronizer playerSynchronizer;
    ProjectileManager projectileManager;

    string output = string.Empty;

    float fadeTimer;

    [SerializeField]
    Weapon[] weapons;

    [SerializeField]
    WeaponDescription[] weaponDescriptions;

    [SerializeField]
    GameObject previewParent;
    [SerializeField]
    GameObject selectorParent;

    ButtonHoverAnimation[] weaponPreviews;
    ButtonHoverAnimation[] weaponSelectors;

    private void Start()
    {
        projectileManager = FindAnyObjectByType<ProjectileManager>();
        equippedClassesField = GetComponent<TMP_Text>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        weaponSelectors = selectorParent.GetComponentsInChildren<ButtonHoverAnimation>();
        weaponPreviews = previewParent.GetComponentsInChildren<ButtonHoverAnimation>();
    }

    private void Update()
    {

        if (fadeTimer < 1) fadeTimer += Time.deltaTime * 4;
        if (fadeTimer > 1) fadeTimer = 1;

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;
        if (!playerSynchronizer.localSquare.nozzleBehaviour) return;

        string output = string.Empty;

        ushort typeId1, typeId2;
        typeId1 = playerSynchronizer.localSquare.nozzleBehaviour.primary;
        typeId2 = playerSynchronizer.localSquare.nozzleBehaviour.secondary;
        string weapon1, weapon2;
        weapon1 = projectileManager.GetWeaponName(typeId1);
        weapon2 = projectileManager.GetWeaponName(typeId2);

        weapon1 = weapon1.Substring(0, 1).ToUpper() + weapon1.Substring(1, weapon1.Length - 1);
        weapon2 = weapon2.Substring(0, 1).ToUpper() + weapon2.Substring(1, weapon2.Length - 1);

        output = weapon1 + " - " + weapon2;

        for (int i = 0; i < weaponPreviews.Length; i++)
        {
            ButtonHoverAnimation button = weaponPreviews[i];
            if (button.isHovering)
            {
                output = projectileManager.GetWeaponName(button.GetComponent<WeaponPreviewBehaviour>().previewing);
                break;
            }
        }

        for (int i = 0; i < weaponSelectors.Length; i++)
        {
            ButtonHoverAnimation button = weaponSelectors[i];
            if (button.isHovering)
            {
                output = projectileManager.GetWeaponName(button.GetComponent<WeaponSelector>().weaponType);
                break;
            }
        }

        if (!this.output.Equals(output))
        {
            this.output = output;
            fadeTimer = 0;
        }

        equippedClassesField.text = this.output;
        equippedClassesField.color = Color.Lerp(Color.clear, Color.white, Mathf.SmoothStep(0, 1, fadeTimer));

    }

    [Serializable]
    public struct WeaponDescription
    {
        public ushort weaponType;
        public string row1;
        public string row2;
        public string row3;
        public string row4;
    }

}
