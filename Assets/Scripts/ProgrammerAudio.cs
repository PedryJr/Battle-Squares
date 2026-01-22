using FMOD.Studio;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class ProgrammerAudio : MonoBehaviour
{
    public static ProgrammerAudio Instance;

    private void Awake()
    {
        Instance = this;
    }

    public FMODUnity.EventReference EventName;
    private EVENT_CALLBACK dialogueCallback;

    void Start()
    {
        dialogueCallback = new FMOD.Studio.EVENT_CALLBACK(DialogueEventCallback);
    }

    public void PlayDialogue(string key)
    {
        EventInstance dialogueInstance = FMODUnity.RuntimeManager.CreateInstance(EventName);
        GCHandle stringHandle = GCHandle.Alloc(key);

        dialogueInstance.setUserData(GCHandle.ToIntPtr(stringHandle));
        dialogueInstance.setCallback(dialogueCallback);
        dialogueInstance.setVolume(MySettings.Volume);
        dialogueInstance.start();
        dialogueInstance.release();
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    static FMOD.RESULT DialogueEventCallback(
        EVENT_CALLBACK_TYPE type,
        IntPtr instancePtr,
        IntPtr parameterPtr)
    {
        EventInstance instance = new EventInstance(instancePtr);

        IntPtr stringPtr;
        instance.getUserData(out stringPtr);

        GCHandle stringHandle = GCHandle.FromIntPtr(stringPtr);
        String key = stringHandle.Target as String;

        switch (type)
        {
            case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                {
                    FMOD.MODE soundMode =
                        FMOD.MODE.LOOP_NORMAL |
                        FMOD.MODE.CREATECOMPRESSEDSAMPLE |
                        FMOD.MODE.NONBLOCKING;

                    var parameter = (PROGRAMMER_SOUND_PROPERTIES)
                        Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));

                    if (key.Contains("."))
                    {
                        FMOD.Sound dialogueSound;
                        var soundResult = FMODUnity.RuntimeManager.CoreSystem
                            .createSound(key, soundMode, out dialogueSound);

                        if (soundResult == FMOD.RESULT.OK)
                        {
                            parameter.sound = dialogueSound.handle;
                            parameter.subsoundIndex = -1;
                            Marshal.StructureToPtr(parameter, parameterPtr, false);
                        }
                    }
                    break;
                }

            case EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                {
                    var parameter = (PROGRAMMER_SOUND_PROPERTIES)
                        Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));

                    var sound = new FMOD.Sound(parameter.sound);
                    sound.release();
                    break;
                }

            case EVENT_CALLBACK_TYPE.DESTROYED:
                {
                    stringHandle.Free();
                    break;
                }
        }

        return FMOD.RESULT.OK;
    }
}
