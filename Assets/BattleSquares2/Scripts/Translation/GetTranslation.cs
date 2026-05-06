using UnityEngine;
using UnityEngine.Events;

public class GetTranslation : MonoBehaviour
{
	[SerializeField]
	private int WantedIndex = 0;
	[SerializeField]
	private UnityEvent<string> PopulateTextfield;
	private void Awake()
	{
		PopulateTextfield?.Invoke(Translation_Manager.GetTranslation(WantedIndex));
	}
}