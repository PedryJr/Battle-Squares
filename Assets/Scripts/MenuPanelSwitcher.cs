using FMOD.Studio;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class MenuPanelSwitcher : MonoBehaviour
{

    [SerializeField]
    GameObject[] menus;

    [SerializeField]
    EventReference testSound;

    [SerializeField]
    TMP_Text[] initButtons;

    [SerializeField]
    bool ignoreInit;

    private void Awake()
    {

        string value;

        initButtons[0].text = $"Volume: {Mathf.RoundToInt(MySettings.Volume * 10)}";

        value = MySettings.Vsync == 0 ? "Off" : "On";
        initButtons[1].text = $"Vsync: {value}";

        value = "Off";
        initButtons[2].text = $"FPS Cap: {value}";

        switch (WindowManager.Instance.CurrentState)
        {
            case WindowManager.WindowState.Fullscreen: value = "Fullscreen"; break;
            case WindowManager.WindowState.Borderless: value = "Borderless"; break;
            case WindowManager.WindowState.Windowed: value = "Windowed"; break;
        }
        initButtons[3].text = $"{value}";

    }

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

        if(MySettings.Volume >= 1) MySettings.SetVolume(0);
        else MySettings.SetVolume(MySettings.Volume + 0.1001f);

        EventInstance eventInstance = RuntimeManager.CreateInstance(testSound);
        eventInstance.setVolume(MySettings.Volume);
        eventInstance.start();
        eventInstance.release();

        Tmp.text = $"Volume: {Mathf.RoundToInt(MySettings.Volume * 10)}";

    }

    public void VSYNC(TextMeshProUGUI Tmp)
    {

        if (MySettings.Vsync == 1) MySettings.SetVsync(0);
        else MySettings.SetVsync(1);

        string value = MySettings.Vsync == 0 ? "Off" : "On";

        Tmp.text = $"Vsync: {value}";

    }

    public void FPS(TextMeshProUGUI Tmp)
    {

        if (MySettings.Fps == 4) MySettings.SetFps(0);
        else MySettings.SetFps(MySettings.Fps + 1);

        string value = "Off";

        switch (MySettings.Fps)
        {
            case 0: value = "Off"; break;
            case 1: value = "30"; break;
            case 2: value = "60"; break;
            case 3: value = "144"; break;
            case 4: value = "240"; break;
        }

        Tmp.text = $"FPS Cap: {value}";

    }

    public void FULLSCREEN(TextMeshProUGUI Tmp)
    {

        if (MySettings.FullscreenMode == 2) MySettings.SetFullscreen(0);
        else MySettings.SetFullscreen(MySettings.FullscreenMode + 1);

        string value = "Fullscreen";

        switch (MySettings.FullscreenMode)
        {
            case 0: value = "Fullscreen"; break;
            case 1: value = "Borderless"; break;
            case 2: value = "Windowed"; break;
        }

        Tmp.text = $"{value}";

    }

    public void APPLY()
    {

        //MySettings.ApplySettings();

    }

}
