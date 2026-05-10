using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GetTranslation))]
public class GetTranslationEditor : Editor
{
	private string[] languages;
	private GetTranslation origin;
	int laguageIndex, newIndex;
	private void OnEnable()
	{
		Translation_Manager.ForceInit();
		origin = target as GetTranslation;
		origin.chosenEnglishText = Translation_Manager.GetEnglishVersion(new SerializedObject(origin).FindProperty("WantedIndex").intValue);
		languages = Translation_Manager.languages;
		laguageIndex = Translation_Manager.language;
	}
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		GUILayout.BeginHorizontal();
		newIndex = EditorGUILayout.Popup(laguageIndex, languages, GUILayout.Width(200));
		GUILayout.Label("English text equivalent: " + origin.chosenEnglishText);
		GUILayout.EndHorizontal();
		if (newIndex != laguageIndex)
		{
			Translation_Manager.ChangeLanguage(newIndex);
			laguageIndex = newIndex;
		}
	}
}
