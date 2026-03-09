using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioInstanceBehaviour : AutoPooledBehaviour
{

    [SerializeField]
    EventReference soundRef;

    [SerializeField]
    float pitchVariation = 0.0f;

    [HideInInspector]
    EventInstance soundInstance;
    [HideInInspector]
    bool isAllocated = false;

    void TryAllocateAudio()
    {
        if(isAllocated) return;
        soundInstance = RuntimeManager.CreateInstance(soundRef);
        isAllocated = true;
    }

    void TryDeallocateAudio()
    {
        if(!isAllocated) return;
        soundInstance.release();
        isAllocated= false;
    }

    void PlayAudio()
    {
        TryAllocateAudio();

        float minPitch = 1.0f - pitchVariation;
        float maxPitch = 1.0f + pitchVariation;
        float randomPitch = Random.Range(minPitch, maxPitch);

        soundInstance.setPitch(randomPitch);
        soundInstance.setVolume(MySettings.Volume);
        soundInstance.setTimelinePosition(0);
        soundInstance.start();
    }

    protected override void OnReturnedToPool()
    {
    }

    protected override void OnSpawned()
    {
        PlayAudio();
    }
}