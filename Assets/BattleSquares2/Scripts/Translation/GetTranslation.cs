using System;
using UnityEngine;
using UnityEngine.Events;

public class GetTranslation : MonoBehaviour
{
	[SerializeField]
	private UnityEvent<string> PopulateTextfield;
	[SerializeField]
	private int WantedIndex = 0;
	[NonSerialized]
	public string chosenEnglishText;

	internal void SetText()
	{
		PopulateTextfield?.Invoke(Translation_Manager.GetTranslation(WantedIndex));
	}

	private void OnValidate()
	{
		chosenEnglishText = Translation_Manager.GetTranslation(WantedIndex);
	}

	private void Awake()
	{
		SetText();
	}
}