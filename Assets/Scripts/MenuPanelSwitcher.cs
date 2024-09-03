using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.Rendering.DebugUI;

public sealed class MenuPanelSwitcher : MonoBehaviour
{

    [SerializeField]
    GameObject[] menus;

    [SerializeField]
    EventReference testSound;

    public void SwitchMenu(GameObject menuToEnable)
    {

        foreach (GameObject menu in menus)
        {
            menu.SetActive(false);
        }

        menuToEnable.SetActive(true);

    }

    public void VOLUME(TextMeshProUGUI Tmp)
    {

        if(MySettings.volume >= 1) MySettings.volume = 0;
        else MySettings.volume += 0.1001f;

        EventInstance eventInstance = RuntimeManager.CreateInstance(testSound);
        eventInstance.setVolume(MySettings.volume);
        eventInstance.start();

        Tmp.text = $"Volume: {Mathf.RoundToInt(MySettings.volume * 10)}";

    }

    public void VSYNC(TextMeshProUGUI Tmp)
    {

        if (MySettings.vsync == 1) MySettings.vsync = 0;
        else MySettings.vsync = 1;

        QualitySettings.vSyncCount = MySettings.vsync;

        string value = MySettings.vsync == 0 ? "Off" : "On";

        Tmp.text = $"Vsync: {value}";

    }

    public void FPS(TextMeshProUGUI Tmp)
    {

        if (MySettings.fps == 4) MySettings.fps = 0;
        else MySettings.fps++;

        string value = "Off";

        switch (MySettings.fps)
        {
            case 0: value = "Off"; Application.targetFrameRate = -1; break;
            case 1: value = "30"; Application.targetFrameRate = 30; break;
            case 2: value = "60"; Application.targetFrameRate = 60; break;
            case 3: value = "144"; Application.targetFrameRate = 144; break;
            case 4: value = "240"; Application.targetFrameRate = 240; break;
        }

        Tmp.text = $"FPS Cap: {value}";

    }

    public void FULLSCREEN(TextMeshProUGUI Tmp)
    {

        if (MySettings.fullscreen == 2) MySettings.fullscreen = 0;
        else MySettings.fullscreen++;

        string value = "Fullscreen";

        switch (MySettings.fullscreen)
        {
            case 0: value = "Fullscreen"; Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: value = "Borderless"; Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            case 2: value = "Windowed"; Screen.fullScreenMode = FullScreenMode.Windowed; break;
        }

        Tmp.text = $"{value}";

    }

}
