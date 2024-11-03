using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "UIAudio", menuName = "Config/UIAudio")]
public class UIAudio : ScriptableObject
{

    [SerializeField]
    public EventReference uiOnHover;

    [SerializeField]
    public EventReference uiClick;

    public void PlayHover(float pitch) => PlayAudio(uiOnHover, pitch);
    public void PlayClick(float pitch) => PlayAudio(uiClick, pitch);

    void PlayAudio(EventReference eventReference, float pitch)
    {

        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        eventInstance.setVolume(MySettings.volume);
        eventInstance.setPitch(pitch);
        eventInstance.start();

    }

}
