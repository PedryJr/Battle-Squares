using UnityEngine;
using UnityEngine.Events;

public class GetTranslation : MonoBehaviour
{
	[SerializeField]
	private int WantedIndex = 0;
	[SerializeField]
	private UnityEvent<string> PopulateTextfield;

	internal void SetText()
	{
		PopulateTextfield?.Invoke(Translation_Manager.GetTranslation(WantedIndex));
	}

	private void Awake()
	{
		SetText();
	}
	[ContextMenu("Set language to English")]
	public void ChangeLanguage_0() => Translation_Manager.ChangeLanguage(0);
	[ContextMenu("Set language to Japanese")]
	public void ChangeLanguage_1() => Translation_Manager.ChangeLanguage(1);
	[ContextMenu("Set language to French")]
    public void ChangeLanguage_2() => Translation_Manager.ChangeLanguage(2);
	[ContextMenu("Set language to German")]
    public void ChangeLanguage_3() => Translation_Manager.ChangeLanguage(3);
	[ContextMenu("Set language to Spanish")]
    public void ChangeLanguage_4() => Translation_Manager.ChangeLanguage(4);
}