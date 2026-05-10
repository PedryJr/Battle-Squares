using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DisplayWeapons : MonoBehaviour, IPointerClickHandler
{
	[SerializeField]
	WeaponSelector weaponSelector;
	public WeaponBuilder[] WeaponBuilder;
	public WeaponBuilder[] WorkShop;
	private void Awake()
	{
		ProjectileManager projectileManager = FindAnyObjectByType<ProjectileManager>();
		foreach (WeaponBuilder item in projectileManager.weapons.Values)
		{
			if (item.weapon.delistWeapon) continue;
			Instantiate(weaponSelector, transform).Initialize(item);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.scrollDelta != Vector2.zero)
			Debug.Log(eventData.scrollDelta);
	}
}
public class MonoChromaticSlider : MonoBehaviour
{

}