using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public sealed unsafe class MyUpdateManager<T> where T : MonoBehaviour
{
    private static MyUpdateManager<T> instance;
    public static MyUpdateManager<T> Instance
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (instance == null)
            {
                instance = new MyUpdateManager<T>();
                MyUpdateDriver.RegisterManager(instance);
            }
            return instance;
        }
    }

    private struct FuncContainer
    {
        public FuncContainer(delegate*<in T, void> funcPtr, T obj, int* funcTracker)
        {
            FuncPtr = funcPtr;
            Obj = obj;
            FuncTracker = funcTracker;
        }
        public readonly delegate*<in T, void> FuncPtr;
        public readonly T Obj;
        public readonly int* FuncTracker;
    }

    private FuncContainer[] containers = new FuncContainer[2048];
    private int count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Register(delegate*<in T, void> funcPtr, T obj, int* tracker)
    {
        if (count >= containers.Length) Array.Resize(ref containers, containers.Length * 2);

        containers[count] = new FuncContainer
            (
            funcPtr,
            obj,
            tracker
            );

        *tracker = count;
        count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unregister(int* tracker)
    {
        int index = *tracker;
        int last = count - 1;
        containers[index] = containers[last];
        *containers[index].FuncTracker = index;
        count--;
        *tracker = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StaticUpdate() => Instance.Update();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        ref var searchSpace = ref MemoryMarshal.GetReference(containers.AsSpan(0, count));
        for (int i = 0; i < count; i++)
        {
            ref FuncContainer entry = ref Unsafe.Add(ref searchSpace, i);
            entry.FuncPtr(entry.Obj);
        }
    }

}
public sealed unsafe class MyUpdateDriver : MonoBehaviour
{
    private static MyUpdateDriver instance;


    struct ManagerEntry 
    {
        public delegate*<void> UpdateFunc; 
    }

    private const int ReallocMultiplier = 2;
    private const int InitialManagerCapacity = 4;
    private static delegate*<void>* managers;
    private static int managerCount;
    private static int allocatedManagers;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureDriverExists()
    {
        if (instance != null) return;

        var go = new GameObject("MyUpdateDriver");
        instance = go.AddComponent<MyUpdateDriver>();
        DontDestroyOnLoad(go);
        managers = (delegate*<void>*)UnsafeUtility.Malloc(
            InitialManagerCapacity * UnsafeUtility.SizeOf<ManagerEntry>(),
            UnsafeUtility.AlignOf<ManagerEntry>(),
            Allocator.Persistent
        );
        allocatedManagers = InitialManagerCapacity;

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterManager<T>(MyUpdateManager<T> manager) where T : MonoBehaviour
    {
        EnsureDriverExists();
        if (managerCount >= allocatedManagers)
        {
            int newCapacity = allocatedManagers * ReallocMultiplier;
            delegate*<void>* newEntry = (delegate*<void>*)UnsafeUtility.Malloc(
                newCapacity * UnsafeUtility.SizeOf<ManagerEntry>(),
                UnsafeUtility.AlignOf<ManagerEntry>(),
                Allocator.Persistent);

            UnsafeUtility.MemCpy(newEntry, managers, managerCount * UnsafeUtility.SizeOf<ManagerEntry>());
            UnsafeUtility.Free(managers, Allocator.Persistent);
            managers = newEntry;
            allocatedManagers = newCapacity;

        }
        managers[managerCount++] = &MyUpdateManager<T>.StaticUpdate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        for (int i = 0; i < managerCount; i++) managers[i]();
    }

/*    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < managerCount; i++) managers[i]();
        watch.Stop();
        var elapsedMs = watch.ElapsedTicks;
        Debug.Log($"MyUpdateDriver Update took {elapsedMs} ticks");
    }*/
}