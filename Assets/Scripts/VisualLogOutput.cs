using BattleSquaresSDK;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using UnityEngine;

[BurstCompile(CompileSynchronously = compileSynchronously, Debug = debug, DisableDirectCall = disableDirectCall,
    DisableSafetyChecks = disableSafetyChecks, FloatMode = floatMode,
    FloatPrecision = floatPrecision, OptimizeFor = optimizeFor)]
public class VLog : MonoBehaviour
{
    public const bool compileSynchronously = false;
    public const bool debug = false;
    public const bool disableDirectCall = false;
    public const bool disableSafetyChecks = true;
    public const FloatMode floatMode = FloatMode.Fast;
    public const FloatPrecision floatPrecision = FloatPrecision.Low;
    public const OptimizeFor optimizeFor = OptimizeFor.Performance;

    [SerializeField]
    LogElementAnimation logElement;

    [SerializeField]
    int maxLogs = 10;

    static VLog instance;
    static Queue<LogElementAnimation> activeElements = new Queue<LogElementAnimation>();

    const bool DEBUG_MODE = true;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(transform.parent);
    }

    public static void Log(string message)
    {
        if (!DEBUG_MODE) return;
        while (activeElements.Count >= instance.maxLogs)
        {
            LogElementAnimation oldest = activeElements.Dequeue();
            if (oldest != null) oldest.PrematureFadeout();
        }

        LogElementAnimation newLog = Instantiate(instance.logElement, instance.transform);
        newLog.text.text = MyExtentions.Format(message);
        newLog.onExpire = ElementExpired;
        activeElements.Enqueue(newLog);

        Debug.Log(message);
    }

    public static void Log(string message, float duration)
    {
        if (!DEBUG_MODE) return;
        while (activeElements.Count >= instance.maxLogs)
        {
            LogElementAnimation oldest = activeElements.Dequeue();
            if (oldest != null) oldest.PrematureFadeout();
        }

        LogElementAnimation newLog = Instantiate(instance.logElement, instance.transform);
        newLog.text.text = MyExtentions.Format(message);
        newLog.onExpire = ElementExpired;
        newLog.timeToStay = duration;
        activeElements.Enqueue(newLog);

        Debug.Log(message);
    }

    static void ElementExpired(LogElementAnimation element)
    {
        if (activeElements.Contains(element))
        {
            Queue<LogElementAnimation> temp = new Queue<LogElementAnimation>();
            while (activeElements.Count > 0)
            {
                LogElementAnimation item = activeElements.Dequeue();
                if (item != element) temp.Enqueue(item);
            }
            activeElements = temp;
        }
    }
}