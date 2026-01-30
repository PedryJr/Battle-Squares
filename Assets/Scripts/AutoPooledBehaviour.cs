using System;
using UnityEngine;
using System.Collections.Generic;

public static class AutoPooledPool<T> where T : AutoPooledBehaviour
{
    private static readonly Dictionary<ulong, Stack<T>> pools = new();

    public static T Spawn(
        in T prefab,
        in Vector3 position,
        in Quaternion rotation,
        in Transform parent = null)
    {
        T instance = null;

        if (prefab.SupportsPooling)
        {
            ulong id = prefab.VariantID;

            if (!pools.TryGetValue(id, out var stack))
            {
                stack = new Stack<T>();
                pools[id] = stack;
            }

            while (stack.Count > 0)
            {
                instance = stack.Pop();
                if (instance != null)
                {
                    instance.enabled = true;
                    break;
                }
            }

            if (!instance)
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
                instance.InitializeForPooling();
            }
        }
        else
        {
            instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
        }

        instance.transform.position = position;
        instance.transform.rotation = rotation;
        if (parent != null) instance.transform.SetParent(parent, true);

        instance.OnSpawnedInternal();
        return instance;
    }

    public static void ReturnToPool(T obj)
    {
        if (obj == null || !obj.SupportsPooling) return;

        ulong id = obj.VariantID;

        if (!pools.TryGetValue(id, out var stack))
        {
            stack = new Stack<T>();
            pools[id] = stack;
        }

        obj.OnReturnedToPoolInternal();
        obj.enabled = false;
        stack.Push(obj);
    }
}


public abstract class AutoPooledBehaviour : MonoBehaviour
{
    [SerializeField] private bool supportObjectPooling = false;
    public bool SupportsPooling => supportObjectPooling;

    [SerializeField] private ulong variantID = 0;
    public ulong VariantID => variantID;

    private bool initialized = false;

    protected virtual void OnValidate()
    {
        if (!supportObjectPooling) return;

        System.Random random = new System.Random();
        byte[] buf = new byte[8];
        random.NextBytes(buf);

        variantID =
            ((ulong)buf[0]) |
            ((ulong)buf[1] << 8) |
            ((ulong)buf[2] << 16) |
            ((ulong)buf[3] << 24) |
            ((ulong)buf[4] << 32) |
            ((ulong)buf[5] << 40) |
            ((ulong)buf[6] << 48) |
            ((ulong)buf[7] << 56);
    }

    public void InitializeForPooling()
    {
        if (initialized) return;
        initialized = true;
        gameObject.SetActive(false);
    }

    protected abstract void OnSpawned();
    protected abstract void OnReturnedToPool();
    public void OnSpawnedInternal()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        OnSpawned();
    }

    public void OnReturnedToPoolInternal()
    {
        OnReturnedToPool();
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}

