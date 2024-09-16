using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class CreateMapIcon : MonoBehaviour
{

    [SerializeField]
    GameObject map;

    private void Start()
    {
        int screenshotWidth = 256;
        int screenshotHeight = 256;
        string assetPath = AssetDatabase.GetAssetPath(map);
        string icon = @"/Icon.png";

        List<char> flippedPath = new List<char>();
        for(int i = assetPath.Length - 1; i >= 0; i--)
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
    }
}
#endif