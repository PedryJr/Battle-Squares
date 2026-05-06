using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DisplayWeapons : MonoBehaviour, IPointerClickHandler
{
	public WeaponBuilder[] WeaponBuilder;
	public WeaponBuilder[] WorkShop;
	private void Awake()
	{
		for (int i = 0; i < WeaponBuilder.Length; i++)
		{
			Image weapon = new GameObject(WeaponBuilder[i].WeaponName).AddComponent<Image>();
			weapon.sprite = WeaponBuilder[i].GetIcon;
			weapon.transform.SetParent(transform);
			weapon.transform.localScale = Vector3.one;
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