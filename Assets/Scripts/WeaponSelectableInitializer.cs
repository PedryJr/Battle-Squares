using UnityEngine;

public class WeaponSelectableInitializer : MonoBehaviour
{

    [SerializeField]
    WeaponSelector weaponSelector;

    private void Awake()
    {
        
        ProjectileManager projectileManager = FindAnyObjectByType<ProjectileManager>();
        foreach (WeaponBuilder item in projectileManager.weapons.Values)
        {
            if (item.weapon.delistWeapon) continue;
            Instantiate(weaponSelector, transform).Initialize(item);
        }

    }

}
