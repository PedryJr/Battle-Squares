using UnityEngine;

public static class BS_Screen
{
    public static float ResolutionScaleStencil = 1;
    public static float ResolutionScaleThermal = 1;
    public static int SpixelsY => Mathf.RoundToInt(Screen.height * ResolutionScaleStencil);
    public static int SpixelsX => Mathf.RoundToInt(Screen.width * ResolutionScaleStencil);

    public static int TpixelsY => Mathf.RoundToInt(Screen.height * ResolutionScaleThermal);
    public static int TpixelsX => Mathf.RoundToInt(Screen.width * ResolutionScaleThermal);

}
