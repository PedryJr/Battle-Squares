using System;
using TMPro;
using UnityEngine;
using static ProjectileManager;

public class WeaponTextBehaviour : MonoBehaviour
{

    TMP_Text equippedClassesField;

    PlayerSynchronizer playerSynchronizer;

    string output = string.Empty;

    float fadeTimer;

    [SerializeField]
    Weapon[] weapons;

    [SerializeField]
    WeaponDescription[] weaponDescriptions;

    [SerializeField]
    ButtonHoverAnimation[] weaponPreviews;

    [SerializeField]
    ButtonHoverAnimation[] weaponSelectors;

    private void Start()
    {

        equippedClassesField = GetComponent<TMP_Text>();
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();

    }

    private void Update()
    {

        if (fadeTimer < 1) fadeTimer += Time.deltaTime * 4;
        if (fadeTimer > 1) fadeTimer = 1;

        if (!playerSynchronizer) return;
        if (!playerSynchronizer.localSquare) return;
        if (!playerSynchronizer.localSquare.nozzleBehaviour) return;

        string output = string.Empty;

        string weapon1, weapon2;
        weapon1 = playerSynchronizer.localSquare.nozzleBehaviour.primary.ToString();
        weapon2 = playerSynchronizer.localSquare.nozzleBehaviour.secondary.ToString();

        weapon1 = weapon1.Substring(0, 1).ToUpper() + weapon1.Substring(1, weapon1.Length - 1);
        weapon2 = weapon2.Substring(0, 1).ToUpper() + weapon2.Substring(1, weapon2.Length - 1);

        output = weapon1 + " - " + weapon2;

        foreach (ButtonHoverAnimation weapon in weaponPreviews)
        {

            if (weapon.isHovering)
            {

                foreach (WeaponDescription description in weaponDescriptions)
                {

                    if (weapon.GetComponent<WeaponPreviewBehaviour>().weaponType == description.weaponType)
                    {
                        output = string.Empty;
                        if (!description.row1.Equals("")) output += description.row1;
                        if (!description.row2.Equals("")) output += "\n" + description.row2;
                        if (!description.row3.Equals("")) output += "\n" + description.row3;
                        if (!description.row4.Equals("")) output += "\n" + description.row4;
                    }

                }

            }

        }

        foreach (ButtonHoverAnimation selector in weaponSelectors)
        {

            if (selector.isHovering)
            {

                output = selector.GetComponent<WeaponSelector>().weaponType.ToString();

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
        public ProjectileType weaponType;
        public string row1;
        public string row2;
        public string row3;
        public string row4;
    }

}
