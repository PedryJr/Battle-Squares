using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


public static class ParticlePool
{
    private static readonly Dictionary<ulong, Stack<ParticleBehaviour>> pools = new();

    public static ParticleBehaviour Spawn(in ParticleBehaviour prefab, in Vector3 position, in Quaternion rotation, in Transform parent = null)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));

        ParticleBehaviour instance = null;

        if (prefab.supportObjectPooling)
        {
            ulong id = prefab.variantID;
            if (!pools.TryGetValue(id, out var stack))
            {
                stack = new Stack<ParticleBehaviour>();
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

        instance.ResetParticle();
        return instance;
    }


    public static void ReturnToPool(ParticleBehaviour particle)
    {
        if (particle == null || !particle.supportObjectPooling) return;

        //particle.gameObject.SetActive(false);
        for (int i = 0; i < particle.ParticleSystems.Length; i++) particle.ParticleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);

        ulong id = particle.variantID;
        if (!pools.TryGetValue(id, out var stack))
        {
            stack = new Stack<ParticleBehaviour>();
            pools[id] = stack;
        }

        stack.Push(particle);
        particle.enabled = false;
    }
}

public sealed class ParticleBehaviour : MonoBehaviour
{
    [SerializeField] ParticleSystem[] particleSystems;
    public ParticleSystem[] ParticleSystems => particleSystems;

    [SerializeField] ParticleSystemRenderer[] particleSystemsRenderers;
    public ParticleSystemRenderer[] ParticleSystemRenderers => particleSystemsRenderers;

    [SerializeField] public bool supportObjectPooling = false;

    [SerializeField] public ulong variantID = 0;

    [SerializeField] float lifeTime = 1.4f;
    [SerializeField] float attatchmentLifeTime = 1.4f;
    [SerializeField] GameObject attatchment;
    private float timer = 0;

    private bool initialized = false;

    private void CacheParticleComponents()
    {
        HashSet<ParticleSystem> setps = new HashSet<ParticleSystem>();

        foreach (var ps in GetComponentsInChildren<ParticleSystem>()) if (ps != null) setps.Add(ps);
        foreach (var ps in GetComponents<ParticleSystem>()) if (ps != null) setps.Add(ps);

        particleSystems = new ParticleSystem[setps.Count];
        particleSystemsRenderers = new ParticleSystemRenderer[setps.Count];

        int index = 0;

        foreach (var ps in setps)
        {
            particleSystems[index] = ps;
            particleSystemsRenderers[index] = ps.GetComponent<ParticleSystemRenderer>();

            index++;
        }
    }



    private void OnValidate() => RefreshComp();

    public void RefreshComp()
    {
        CacheParticleComponents();

        if (supportObjectPooling)
        {
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
    }

    private void OnEnable() => timer = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        timer += Time.deltaTime;
        if (attatchment && attatchment.activeSelf && timer > attatchmentLifeTime) attatchment.SetActive(false);
        if (timer > lifeTime)
        {
            if (supportObjectPooling)
                ParticlePool.ReturnToPool(this);
            else
                Destroy(gameObject);
        }
    }

    public void InitializeForPooling()
    {
        if (initialized) return;
        initialized = true;

        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    public void ResetParticle()
    {
        timer = 0;
        //Added self activation if necessary
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        foreach (var ps in particleSystems) ps.Play();
        if (attatchment) attatchment.SetActive(true);
    }
}
