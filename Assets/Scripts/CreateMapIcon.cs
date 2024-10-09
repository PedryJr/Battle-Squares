using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public sealed class CreateMapIcon : MonoBehaviour
{

    [SerializeField]
    GameObject map;

    private void Start()
    {

        Invoke("Snap", 0.5f);

    }

    void Snap()
    {

        string assetPath = AssetDatabase.GetAssetPath(map);
        string icon = @"/" + map.GetComponent<MapBehaviour>().arenaName + ".png";

        List<char> flippedPath = new List<char>();
        for (int i = assetPath.Length - 1; i >= 0; i--)
        {
            flippedPath.Add(assetPath[i]);
        }

        List<char> choppedPath = new List<char>();
        bool chop = false;
        for (int i = 0; i < flippedPath.Count; i++)
        {

            if (chop) choppedPath.Add(flippedPath[i]);
            else if (flippedPath[i].Equals('/')) chop = true;
        }

        List<char> correctedPath = new List<char>();
        for (int i = choppedPath.Count - 1; i >= 0; i--)
        {
            correctedPath.Add(choppedPath[i]);
        }

        string finalPath = string.Empty;
        for (int i = 0; i < correctedPath.Count; i++)
        {
            finalPath += correctedPath[i];
        }

        finalPath += icon;

        ScreenCapture.CaptureScreenshot(finalPath, 1);

        Debug.Log("Snap!");

    }

}
#endif