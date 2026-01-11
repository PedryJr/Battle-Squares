using FMOD;
using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

public class ProgrammerAudio : MonoBehaviour
{
    public static ProgrammerAudio Instance;
    private void Awake() => Instance = this;

    private Dictionary<string, SoundPool> _soundPools = new Dictionary<string, SoundPool>();

    public FMODUnity.EventReference EventName;
    private EVENT_CALLBACK dialogueCallback;

    void Start()
    {
        dialogueCallback = new FMOD.Studio.EVENT_CALLBACK(DialogueEventCallback);
    }

    public SoundHandle PlayDialogue(string path)
    {
        if (!_soundPools.TryGetValue(path, out var pool))
        {
            FMOD.MODE mode = FMOD.MODE.LOOP_OFF | FMOD.MODE.CREATECOMPRESSEDSAMPLE | FMOD.MODE.NONBLOCKING;
            pool = new SoundPool(path, mode, 10);
            _soundPools[path] = pool;
        }

        EventInstance instance = RuntimeManager.CreateInstance(EventName);
        var handle = GCHandle.Alloc(path);

        instance.setUserData(GCHandle.ToIntPtr(handle));

        // Callback for programmer sound & cleanup
        bool stopped = false;
        instance.setCallback((type, instPtr, paramPtr) =>
        {
            if (type == EVENT_CALLBACK_TYPE.STOPPED || type == EVENT_CALLBACK_TYPE.DESTROYED)
            {
                if (!stopped)
                {
                    stopped = true;
                    instance.release();
                    handle.Free();
                }
            }

            if (type == EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND)
            {
                var parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(paramPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));
                parameter.sound = pool.GetNextSound().handle;
                parameter.subsoundIndex = -1;
                Marshal.StructureToPtr(parameter, paramPtr, false);
            }

            return FMOD.RESULT.OK;
        });

        instance.start();

        return new SoundHandle(instance, handle);
    }



    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    static FMOD.RESULT DialogueEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        EventInstance instance = new EventInstance(instancePtr);

        IntPtr stringPtr;
        instance.getUserData(out stringPtr);
        GCHandle stringHandle = GCHandle.FromIntPtr(stringPtr);
        string path = stringHandle.Target as string;

        switch (type)
        {
            case EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND:
                {
                    var parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));

                    SoundPool pool = Instance._soundPools[path];
                    parameter.sound = pool.GetNextSound().handle;
                    parameter.subsoundIndex = -1;

                    Marshal.StructureToPtr(parameter, parameterPtr, false);
                    break;
                }
            case EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND:
                {
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

    private void OnDestroy()
    {
        foreach (var pool in _soundPools.Values) pool.ReleaseAll();
        _soundPools.Clear();
    }
}


public class SoundPool
{
    private int _maxInstances;
    private Sound[] _sounds;
    private int _currentIndex;

    public SoundPool(string path, MODE mode, int maxInstances = 10)
    {
        _maxInstances = maxInstances;
        _sounds = new Sound[maxInstances];
        for (int i = 0; i < maxInstances; i++) RuntimeManager.CoreSystem.createSound(path, mode, out _sounds[i]);
        _currentIndex = 0;
    }

    public Sound GetNextSound()
    {
        Sound s = _sounds[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _maxInstances;
        return s;
    }

    public void ReleaseAll()
    {
        for (int i = 0; i < _maxInstances; i++) _sounds[i].release();
    }
}
