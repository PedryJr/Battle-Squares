#if UNITY_EDITOR
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static System.DateTime;

public class GameSimCaptureBehaviour : MonoBehaviour
{

    public enum VisionMask : byte
    {
        Shapes = 1 << 1,
        HUD = 1 << 2,
        Lights = 1 << 3,
        Shadows = 1 << 4,
        Paint = 1 << 5,
    }

    const string ShapesTag = "IngameShapes";
    const string HudTag = "HUD";
    const string LightTag = "GameLight";
    const string PaintTag = "EffectRenderer";

    const string shapesEnabled = "&r&a&lShapes";
    const string shapesDisabled = "&r&c&lShapes";
    const string hudEnabled = "&f, &r&a&lHud";
    const string hudDisabled = "&f, &r&c&lHud";
    const string lightEnabled = "&f, &r&a&lLight";
    const string lightDisabled = "&f, &r&c&lLight";
    const string shadowEnabled = "&f, &r&a&lShadow";
    const string shadowDisabled = "&f, &r&c&lShadow";
    const string paintEnabled = "&f, &r&a&lPaint";
    const string paintDisabled = "&f, &r&c&lPaint";
    const string togglesPrefix1 = "&r&3Toggles&b&l: ";
    const string togglesPrefix2 = "&f&l[&a&lOn&f&l] &f&l[&c&lOff&f&l]";

    const string simSpeedPart = "&r&3Sim speed&b&l:&r&f&l ";
    const string incrementLog = "&f&l[&a&l+&f&l] " + simSpeedPart;
    const string decrementLog = "&f&l[&c&l-&f&l] " + simSpeedPart;
    const string setLog = "&f&l[&e&l~&f&l] " + simSpeedPart;
    const float logDuration = 0.75f;
    
    [SerializeField][Range(0f, 1f)] public float simSpeed;
    [SerializeField] public string captureDestination = "Screenshots/";
    [SerializeField] public VisionMask visionMask = 
        VisionMask.Shapes |
        VisionMask.HUD |
        VisionMask.Lights |
        VisionMask.Shadows |
        VisionMask.Paint
        ;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HandleShapeGroups(GameObject shapeGroup)
    {
        bool shapesEnabled = MyExtentions.FlagIsSet(visionMask, VisionMask.Shapes);
        MeshRenderer[] renderers = shapeGroup.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer shapeRenderer in renderers) shapeRenderer.enabled = shapesEnabled;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HandleHudGroups(GameObject[] hudGroups)
    {
        bool hudEnabled = MyExtentions.FlagIsSet(visionMask, VisionMask.HUD);
        foreach (GameObject hudGroup in hudGroups)
        {
            Canvas[] canvases = hudGroup.GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases) canvas.enabled = hudEnabled;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HandleLights(GameObject[] lights)
    {
        bool lightsEnabled = MyExtentions.FlagIsSet(visionMask, VisionMask.Lights);
        bool shadowsEnabled = MyExtentions.FlagIsSet(visionMask, VisionMask.Shadows);
        foreach (GameObject lightObj in lights)
        {
            Light2D light = lightObj.GetComponent<Light2D>();
            light.enabled = lightsEnabled;
            light.shadowsEnabled = shadowsEnabled;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void HandleEffectRenderer(GameObject effectCam)
    {
        bool paintEnabled = MyExtentions.FlagIsSet(visionMask, VisionMask.Paint);
        Camera cam = effectCam.GetComponent<Camera>();
        if (paintEnabled == cam.enabled) return;
        Vector3 camPos = cam.transform.position;
        cam.transform.position = new Vector3(1000, 0, 1000);
        cam.Render();
        cam.transform.position = camPos;
        cam.enabled = paintEnabled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void RefreshVisMask()
    {
        HandleShapeGroups(GameObject.FindGameObjectWithTag(ShapesTag));
        HandleHudGroups(GameObject.FindGameObjectsWithTag(HudTag));
        HandleLights(GameObject.FindGameObjectsWithTag(LightTag));
        HandleEffectRenderer(GameObject.FindGameObjectWithTag(PaintTag));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string GetIncrementLog(bool increment) => (increment ? incrementLog : decrementLog) + simSpeed;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void LogChangeSimSpeed(bool increment) => VLog.Log(GetIncrementLog(increment), logDuration);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void LogSetSimSpeed() => VLog.Log(setLog + simSpeed, logDuration);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void LogToggles()
    {
        VLog.Log(togglesPrefix1
        + (ShapesEnabled() ? shapesEnabled : shapesDisabled)
        + (HudEnabled() ? hudEnabled : hudDisabled)
        + (LightsEnabled() ? lightEnabled : lightDisabled)
        + (ShadowsEnabled() ? shadowEnabled : shadowDisabled)
        + (PaintEnabled() ? paintEnabled : paintDisabled), logDuration * 2);
        VLog.Log(togglesPrefix2, logDuration * 2);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddToSimSpeed(float toAdd) => simSpeed = Mathf.Clamp01(Mathf.Round(((simSpeed + toAdd) * 100)) / 100);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecrementSimSpeed() => AddToSimSpeed(-0.1f);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementSimSpeed() => AddToSimSpeed(0.1f);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CaptureScreen()
    {
        string path = Path.Combine(Application.dataPath, captureDestination);
        if(!Directory.Exists(path)) Directory.CreateDirectory(path);
        path = Path.Combine(path,  $"{Now.Hour}_{Now.Minute}_{Now.Second}.png");
        ScreenCapture.CaptureScreenshot(path);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleShapes()
    { MyExtentions.FlagFlip(ref visionMask, VisionMask.Shapes); RefreshVisMask(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleHud()
    { MyExtentions.FlagFlip(ref visionMask, VisionMask.HUD); RefreshVisMask(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleLights()
    { MyExtentions.FlagFlip(ref visionMask, VisionMask.Lights); RefreshVisMask(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToggleShadows()
    { MyExtentions.FlagFlip(ref visionMask, VisionMask.Shadows); RefreshVisMask(); }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TogglePaint()
    { MyExtentions.FlagFlip(ref visionMask, VisionMask.Paint); RefreshVisMask(); }
    public void EnableAll()
    {
        MyExtentions.FlagSet(ref visionMask, VisionMask.Shapes);
        MyExtentions.FlagSet(ref visionMask, VisionMask.HUD);
        MyExtentions.FlagSet(ref visionMask, VisionMask.Lights);
        MyExtentions.FlagSet(ref visionMask, VisionMask.Shadows);
        MyExtentions.FlagSet(ref visionMask, VisionMask.Paint);
        RefreshVisMask();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShapesEnabled() => MyExtentions.FlagIsSet(visionMask, VisionMask.Shapes);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HudEnabled() => MyExtentions.FlagIsSet(visionMask, VisionMask.HUD);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool LightsEnabled() => MyExtentions.FlagIsSet(visionMask, VisionMask.Lights);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShadowsEnabled() => MyExtentions.FlagIsSet(visionMask, VisionMask.Shadows);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool PaintEnabled() => MyExtentions.FlagIsSet(visionMask, VisionMask.Paint);

    void Start()
    {
        EnableAll();
    }

    void Update()
    {

        bool doDec, doInc, doOne, doZero, doCap, doShapes, doHud, doLights, doShadows, doPaint;

        doDec = Input.GetKeyDown(KeyCode.Q);
        doInc = Input.GetKeyDown(KeyCode.E);
        doOne = Input.GetKeyDown(KeyCode.Alpha1);
        doZero = Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Tilde) || Input.GetKeyDown(KeyCode.BackQuote);
        doCap = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.LeftControl);
        doShapes = Input.GetKeyDown(KeyCode.Y);
        doHud = Input.GetKeyDown(KeyCode.U);
        doLights = Input.GetKeyDown(KeyCode.I);
        doShadows = Input.GetKeyDown(KeyCode.O);
        doPaint = Input.GetKeyDown(KeyCode.P);

        if (doDec)
        { DecrementSimSpeed(); LogChangeSimSpeed(false); }
        if (doInc)
        { IncrementSimSpeed(); LogChangeSimSpeed(true); }
        if(doOne)
        { AddToSimSpeed(1f); LogSetSimSpeed(); }
        if (doZero)
        { AddToSimSpeed(-1f); LogSetSimSpeed(); }
        if (doCap)
        { CaptureScreen(); }

        if (doShapes)
        { ToggleShapes(); LogToggles(); }
        if (doHud)
        { ToggleHud(); LogToggles(); }
        if (doLights)
        { ToggleLights(); LogToggles(); }
        if (doShadows)
        { ToggleShadows(); LogToggles(); }
        if (doPaint)
        { TogglePaint(); LogToggles(); }

        Time.timeScale = simSpeed;
    }
}
#endif