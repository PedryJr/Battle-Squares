using UnityEngine;

public static class ScreenParams
{
    public static float ResolutionScale = 1;
    public static int pixelsY => Mathf.RoundToInt(Screen.height * ResolutionScale);
    public static int pixelsX => Mathf.RoundToInt(Screen.width * ResolutionScale);

}
