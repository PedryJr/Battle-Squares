using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class Translation_Manager 
{
	private const string k_GoogleSheetDocID = "1iTtRvpnYHMCI5agVDC6dbG5F-nPs0N-K8gb8e0DztBs";
	private const string url = "https://docs.google.com/spreadsheets/d/" + k_GoogleSheetDocID + "/export?format=tsv";
	private const string LatestUpdatedVersion = "Download";


	static int language = 4;
	public static string[] languages; //= new string[] { "English", "Swedish", "Norwegian", "Japanese", "French", "German", "Spanish", "Portuguese", "Italian", "Chinese", "Korean", "Dutch" }; 
	public static string[] percentages;
	const string savedInfoPath = "/infomation/gameInfo.txt";
	static List<string[]> translations = new List<string[]>();
	public static int MaxIndex = 0;
	public static Action done;
	[RuntimeInitializeOnLoadMethod]
	private static async void Init()
	{
		string fullPath = Application.dataPath + savedInfoPath;
		// When(if) we add more information to the infopath, we need to change this to go through them and call the appropreate functions using them
		//if (Application.version != PlayerPrefs.GetString(LatestUpdatedVersion) || File.Exists(fullPath) == false) // this has the latest information. no need to download tsv file
		{
			// For now we download each time, since there are a lot of changes, but add this back before launch
			EnsureAssetPathExists(fullPath);
			Debug.Log("Infomation not downloaded or old. Downloads translation files");
			using (UnityWebRequest request = UnityWebRequest.Get(url))
			{
				request.downloadHandler = new DownloadHandlerFile(fullPath);
				await request.SendWebRequest();
				if (request.result == UnityWebRequest.Result.DataProcessingError)
				{
					Debug.LogError("Download error: " + request.error);
					request.Dispose();
					return;
				}
				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.LogError("Download error: " + request.error);
					request.Dispose();
					return;
				}

				Debug.Log("Information downloaded successfully to: " + fullPath);
				request.Dispose();
			}
		}

		StreamReader reader = new StreamReader(fullPath); // we still need to initialized all the languages as they are not saved in memory
		percentages = reader.ReadLine().Split('\t')[3..]; // english is considered full, so that one does not have a %age check
		languages = reader.ReadLine().Split('\t')[2..]; // Take the names of the languages and add them into an array for displaying things. Means that if we change them it will update
		/*for (int i = 0; i < percentages.Length; i++)
		{
			Debug.Log(languages[i + 1] + "_" +  percentages[i] + "%");
		}*/
		string thisLine = reader.ReadLine();
		string[] sections = thisLine.Split('\t')[2..];

		MaxIndex = 0;
		translations.Clear();
		while (string.IsNullOrEmpty(thisLine) == false)
		{
			Debug.Log(thisLine);
			if (string.IsNullOrEmpty(sections[0]) == false) 
				translations.Add(sections);
			if (reader.EndOfStream)
				break;
			thisLine = reader.ReadLine();
			sections = thisLine.Split('\t')[2..];
			MaxIndex++;
		}
		reader.Close();
		language %= languages.Length;
		PlayerPrefs.SetString(LatestUpdatedVersion, Application.version);
		//Debug.Log(PlayerPrefs.GetString(LatestUpdatedVersion) + "_" + Application.version);
	}
	public static string GetTranslation(int index)
	{
		index %= MaxIndex;
		if ( index >= translations[ index ].Length ) 
			return translations[ index ][ 0 ];
		if ( string.IsNullOrEmpty( translations[ index ][ language ] ) )
			return translations[ index ][ 0 ];
		return translations[ index ][ language ];
	}
	public static string GetTranslationCompletion(int languageIndex)
	{
		languageIndex %= translations.Count;
		return percentages[languageIndex];
	}
	public static void ChangeLanguage(int languageID)
	{
		language = languageID;
		ForceUpdateAllActiveText();
	}
	public static void ForceUpdateAllActiveText() // In case we want this 
	{
		foreach (GetTranslation stuff in GameObject.FindObjectsByType<GetTranslation>(0))
			stuff.SetText();
	}
	public static void EnsureAssetPathExists(string fullPath)
	{
		// Path should be relative to the Project folder, e.g., "Assets/MyFolder/SubFolder"
		string[] folders = fullPath.Split('/');
		string currentPath = folders[0];

		for (int i = 1; i < folders.Length; i++)
		{
			string folderName = folders[i];
			string nextPath = $"{currentPath}/{folderName}";

#if UNITY_EDITOR
			if (AssetDatabase.IsValidFolder(nextPath) == false)
			{
				AssetDatabase.CreateFolder(currentPath, folderName);
			}
#endif
			currentPath = nextPath;
		}
	}
}
