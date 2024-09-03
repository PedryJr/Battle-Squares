using System.Collections;
using System.Collections.Generic;
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

    private void OnEnable()
    {
        image = GetComponent<Image>();
        weapons = weaponSelectorContent.GetComponentsInChildren<WeaponSelector>();

    }

    public enum WeaponPreviewType
    {
        Primary, Secondary
    }

}
