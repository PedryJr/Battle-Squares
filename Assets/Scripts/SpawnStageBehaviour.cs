using System.Runtime.CompilerServices;
using UnityEngine;

public unsafe sealed class SpawnStageBehaviour : MonoBehaviour
{


    [SerializeField]
    public float spawnTime;

    [SerializeField]
    public float growSpeed;

    [SerializeField]
    public Vector3 growTarget;
    public Vector3 growFrom = Vector3.zero;

    [SerializeField]
    SpriteRenderer[] guaranteeds;

    public float timer;

    Vector3 scale;

    public bool doScale;

    public float spawnTimer;
    public bool hasSpawned;

    public SpriteRenderer[] sprites;

    Transform thisTransform;

    private void Awake()
    {
        
        sprites = GetComponentsInChildren<SpriteRenderer>(true);

        thisTransform = GetComponent<Transform>();

        foreach (SpriteRenderer spriteRenderer in guaranteeds)
        {
            spriteRenderer.enabled = true;
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        if (!doScale) return;

        if (timer < 1) timer += Time.deltaTime * growSpeed;
        if (timer > 1) timer = 1;

        if (timer == 1) doScale = false;

        thisTransform.localScale = Vector3.Lerp(growFrom, growTarget, MyExtentions.EaseOutQuad(timer));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MonoBehaviour AsMonoBehaviour() => this;
}
