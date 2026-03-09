using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.Rendering.SplashScreen;
/*

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
*/
public sealed class ParticleBehaviour : AutoPooledBehaviour
{
    [SerializeField] ParticleSystem[] particleSystems;
    public ParticleSystem[] ParticleSystems => particleSystems;

    [SerializeField] ParticleSystemRenderer[] particleSystemsRenderers;
    public ParticleSystemRenderer[] ParticleSystemRenderers => particleSystemsRenderers;

    [SerializeField] ParticleSystemStopBehavior stopBehaviour = ParticleSystemStopBehavior.StopEmittingAndClear;
    [SerializeField] public float lifeTime = 1.4f;
    [SerializeField] private float emissionStopTimePadding = 1f;
    [SerializeField] float attatchmentLifeTime = 1.4f;
    [SerializeField] GameObject attatchment;
    private float timer = 0;
    bool stopPadding = true;

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



    public override void OnValidate()
    {
        base.OnValidate();
        CacheParticleComponents();
    }

    public void Refresh()
    {
        CacheParticleComponents();
        RefreshComp();
    }


    private void OnEnable() => timer = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        timer += Time.deltaTime;
        if (attatchment && attatchment.activeSelf && timer > attatchmentLifeTime) attatchment.SetActive(false);
        if (timer > lifeTime)
        {
            if (particleSystems != null && stopPadding) foreach (var ps in particleSystems) ps.Stop(true, stopBehaviour);
            stopPadding = false;
        }
        if (timer > lifeTime + emissionStopTimePadding)
        {
            if (base.supportObjectPooling) AutoPooledPool<ParticleBehaviour>.ReturnToPool(this);
            else Destroy(gameObject);
        }
    }

    public void ResetParticle()
    {
        stopPadding = true;
        timer = 0;
        //Added self activation if necessary
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        foreach (var ps in particleSystems) ps.Play();
        if (attatchment) attatchment.SetActive(true);
    }

    protected override void OnSpawned()
    {
        ResetParticle();
    }

    protected override void OnReturnedToPool()
    {

    }
}
