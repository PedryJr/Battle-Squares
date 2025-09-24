using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Random = UnityEngine.Random;

public unsafe sealed class HitMarkBehaviour : MonoBehaviour
{

    private int funcTracker = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CallFromUpdateManager(in HitMarkBehaviour obj) => obj.MyUpdate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnEnable()
    {
        fixed (int* trackerPtr = &funcTracker) MyUpdateManager<HitMarkBehaviour>.Instance.Register(&CallFromUpdateManager, this, trackerPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void OnDisable()
    {
        fixed (int* trackerPtr = &funcTracker) MyUpdateManager<HitMarkBehaviour>.Instance.Unregister(trackerPtr);
    }


    [SerializeField]
    ImpactForceBehaviour impactForce;
    public float zPos;
    public float timer;
    float timeAlive = 10;
    public byte ownerId;
    public PlayerBehaviour owner;
    int skipPhysicsSteps;
    bool spawned;
    float fadeOut = 0;

    [SerializeField]
    bool canExpand;

    [SerializeField]
    bool randomSpawning;

    [SerializeField]
    bool randomRotation;

    [SerializeField]
    float spawnChance;

    [SerializeField]
    bool grow;

    [SerializeField]
    public SpawnStageBehaviour[] spawnStages;

    float spawnTimerOne;
    bool spawn1;

    float spawnTimerTwo;
    bool spawn2;

    float spawnTimerThree;
    bool spawn3;

    public Color spawnColor;
    public Color fadeColor;

    private const float TimeAlive = 35f;

    private SpriteRenderer mainRenderer;
    private static MaterialPropertyBlock SharedBlock = null;

    private void Awake()
    {
        if(SharedBlock == null) SharedBlock = new MaterialPropertyBlock();
        impactForce = Instantiate(impactForce, transform.position, transform.rotation, null);

        if (randomRotation && spawnStages != null)
        {
            for (int i = 0; i < spawnStages.Length; i++)
            {
                spawnStages[i].transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            }
        }

        mainRenderer = GetComponentInChildren<SpriteRenderer>();
        if (mainRenderer != null)
            mainRenderer.sortingOrder = 2;

        transform.position += new Vector3(0, 0, LevelBuilderStuff.STENCIL_OFFSET);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignStencil(float stencil)
    {
        float scaled = stencil / 2048f;
        SharedBlock.SetVector("_HitMarkStencil", new Vector4(scaled, scaled, scaled, scaled));
        mainRenderer.SetPropertyBlock(SharedBlock);
    }

    private void MyUpdate()
    {
        float dt = Time.deltaTime;

        spawnColor = owner.PlayerColor.HitMarkColor;
        timer += dt;

        if (timer > TimeAlive) fadeOut = (timer - TimeAlive) * 4f;

        SpawnStages(dt);

        if (fadeOut > 0f)
        {
            float t = fadeOut;
            int countSpawnStages = spawnStages.Length;
            ref var spawnStageSearchSpace = ref MemoryMarshal.GetReference(spawnStages.AsSpan());
            for (int i = 0; i < countSpawnStages; i++)
            {
                ref SpawnStageBehaviour stage = ref Unsafe.Add(ref spawnStageSearchSpace, i);
                int countSprites = stage.sprites.Length;
                ref var spriteSearchSpace = ref MemoryMarshal.GetReference(stage.sprites.AsSpan());
                for (int j = 0; j < countSprites; j++)
                {
                    ref SpriteRenderer sr = ref Unsafe.Add(ref spriteSearchSpace, j);
                    if (sr.enabled) sr.color = Color.Lerp(spawnColor, fadeColor, t);
                }
            }
        }

        if (fadeOut >= 1f) Destroy(gameObject);
        else
        {
            int countSpawnStages = spawnStages.Length;
            ref var spawnStageSearchSpace = ref MemoryMarshal.GetReference(spawnStages.AsSpan());
            for (int i = 0; i < countSpawnStages; i++)
            {
                ref SpawnStageBehaviour stage = ref Unsafe.Add(ref spawnStageSearchSpace, i);
                int countSprites = stage.sprites.Length;
                ref var spriteSearchSpace = ref MemoryMarshal.GetReference(stage.sprites.AsSpan());
                for (int j = 0; j < countSprites; j++)
                {
                    ref SpriteRenderer sr = ref Unsafe.Add(ref spriteSearchSpace, j);
                    if (sr.enabled)
                        sr.color = spawnColor;
                }
            }
        }
    }

    private void SpawnStages(float dt)
    {

        int countSpawnStages = spawnStages.Length;
        ref var spawnStageSearchSpace = ref MemoryMarshal.GetReference(spawnStages.AsSpan());
        for (int i = 0; i < countSpawnStages; i++)
        {
            ref SpawnStageBehaviour stage = ref Unsafe.Add(ref spawnStageSearchSpace, i);
            if (stage.hasSpawned) continue;

            stage.spawnTimer += dt;
            if (stage.spawnTimer > stage.spawnTime)
            {
                int countSprites = stage.sprites.Length;
                ref var spriteSearchSpace = ref MemoryMarshal.GetReference(stage.sprites.AsSpan());
                for (int j = 0; j < countSprites; j++)
                {
                    ref SpriteRenderer sr = ref Unsafe.Add(ref spriteSearchSpace, j);
                    if (randomSpawning && Random.Range(0f, 1f) > spawnChance) continue;

                    sr.enabled = true;
                    stage.hasSpawned = true;
                }

                if (grow) stage.doScale = true;
            }
        }
    }
}
