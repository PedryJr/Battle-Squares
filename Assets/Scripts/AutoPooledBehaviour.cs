using System;
using UnityEngine;
using System.Collections.Generic;

public class DestroyedFlag
{
    public bool IsDestroyed;
}

public static class AutoPooledPool<T> where T : AutoPooledBehaviour
{
    struct AutoPooledTracker
    {
        public T behaviour;
        public DestroyedFlag destroyedFlag;

        public AutoPooledTracker(T behaviour)
        {
            this.behaviour = behaviour;
            destroyedFlag = behaviour.DestroyedFlag;
            destroyedFlag.IsDestroyed = false;
        }
    }

    private static readonly Dictionary<ulong, Stack<AutoPooledTracker>> pools = new();

    public static T Spawn(
        in T prefab,
        in Vector3 position,
        in Quaternion rotation,
        in Transform parent = null)
    {
        T behaviour = null;

        if (prefab.SupportsPooling)
        {
            ulong id = prefab.VariantID;

            if (!pools.TryGetValue(id, out var stack))
            {
                stack = new Stack<AutoPooledTracker>();
                pools[id] = stack;
            }

            while (stack.Count > 0)
            {
                AutoPooledTracker tracker = stack.Pop();
                if (!tracker.destroyedFlag.IsDestroyed)
                {
                    behaviour = tracker.behaviour;
                    behaviour.enabled = true;
                    break;
                }
            }

            if (behaviour == null)
            {
                behaviour = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
                behaviour.InitializeForPooling(new DestroyedFlag());
            }
        }
        else behaviour = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);

        behaviour.transform.position = position;
        behaviour.transform.rotation = rotation;
        if (parent != null) behaviour.transform.SetParent(parent, true);

        behaviour.OnSpawnedInternal();
        return behaviour;
    }

    public static void ReturnToPool(T obj)
    {
        if (!obj.SupportsPooling) return;

        if (obj.DestroyedFlag == null || obj.DestroyedFlag.IsDestroyed) return;

        obj.OnReturnedToPoolInternal();
        obj.enabled = false;

        pools[obj.VariantID].Push(new AutoPooledTracker(obj));
    }
}


public abstract class AutoPooledBehaviour : MonoBehaviour
{
    [SerializeField] private bool supportObjectPooling = true;
    [SerializeField] private ulong variantID = 0;
    private bool initialized = false;
    private DestroyedFlag destroyedFlag;
    public bool SupportsPooling => supportObjectPooling;
    public ulong VariantID => variantID;
    internal DestroyedFlag DestroyedFlag => destroyedFlag;

    private void OnDestroy()
    {
        if (SupportsPooling && destroyedFlag != null) destroyedFlag.IsDestroyed = true;
    }

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

    internal void InitializeForPooling(DestroyedFlag destroyedFlag)
    {
        if (initialized) return;
        initialized = true;
        gameObject.SetActive(false);
        this.destroyedFlag = destroyedFlag;
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