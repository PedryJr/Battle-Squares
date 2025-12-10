using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "UIAudio", menuName = "Config/UIAudio")]
public sealed class UIAudio : ScriptableObject
{

    [SerializeField]
    public EventReference uiOnHover;

    [SerializeField]
    public EventReference uiClick;

    public void PlayHover(float pitch, float loudness = 1.0f) => PlayAudio(uiOnHover, pitch, loudness);
    public void PlayClick(float pitch, float loudness = 1.0f) => PlayAudio(uiClick, pitch, loudness);

    void PlayAudio(EventReference eventReference, float pitch, float loudness = 1.0f)
    {

        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstance.setVolume(MySettings.Volume * loudness);
        eventInstance.setPitch(pitch);
        eventInstance.start();
        eventInstance.release();

    }

}
