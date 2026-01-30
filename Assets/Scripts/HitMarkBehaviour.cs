using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class HitMarkBehaviour : AutoPooledBehaviour
{
    private const float FadeInDuration = 0.15f;
    private const float StayDuration = 20f;
    private const float FadeOutDuration = 0.15f;

    public float zPos;
    public byte ownerId;
    private PlayerBehaviour owner;

    private SpriteRenderer spriteRenderer;
    private static MaterialPropertyBlock SharedBlock = null;

    //Starts at 0
    private Vector3 targetScale;

    private enum FadeState
    {
        FadeIn,
        Stay,
        FadeOut
    }

    private FadeState currentState = FadeState.FadeIn;
    private float stateTimer = 0f;
    private Color hitmarkSpawnColor;
    private Color hitMarkFadeColor;

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    public void Initialize(PlayerBehaviour owner)
    {
        this.owner = owner;
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (SharedBlock == null) SharedBlock = new MaterialPropertyBlock();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 2;

        transform.position += new Vector3(0, 0, LevelBuilderStuff.STENCIL_OFFSET);
        transform.localScale = Vector3.zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignStencil(float stencil)
    {
        float scaled = stencil / 2048f;
        SharedBlock.SetVector("_HitMarkStencil", new Vector4(scaled, scaled, scaled, scaled));
        EffectRenderer.AssignTextureToProp(SharedBlock, "_StencilGroup");
        if (spriteRenderer != null)
        {
            spriteRenderer.SetPropertyBlock(SharedBlock);
        }
    }

    void AssignStencilTexture(Texture renderTexture)
    {
        if (SharedBlock == null) SharedBlock = new MaterialPropertyBlock();
        SharedBlock.SetTexture("_StencilGroup", renderTexture);
        if (spriteRenderer != null)
        {
            spriteRenderer.SetPropertyBlock(SharedBlock);
        }
    }

    private void Update()
    {
        if (!owner)
        {
            AutoPooledPool<HitMarkBehaviour>.ReturnToPool(this);
            return;
        }

        float dt = Time.deltaTime;
        stateTimer += dt;

        switch (currentState)
        {
            case FadeState.FadeIn:
                float fadeInProgress = MyExtentions.EaseOutQuad(Mathf.Clamp01(stateTimer / FadeInDuration));
                transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, fadeInProgress);
                spriteRenderer.color = Color.Lerp(owner.PlayerColor.HitMarkColor, owner.PlayerColor.HitMarkFadeColor, Mathf.SmoothStep(0, 1, fadeInProgress));

                if (stateTimer >= FadeInDuration)
                {
                    currentState = FadeState.Stay;
                    stateTimer = 0f;
                    transform.localScale = targetScale;
                }
                break;

            case FadeState.Stay:

                if (stateTimer >= StayDuration)
                {
                    currentState = FadeState.FadeOut;
                    stateTimer = 0f;
                }
                break;

            case FadeState.FadeOut:
                float fadeOutProgress = MyExtentions.EaseInQuad(Mathf.Clamp01(stateTimer / FadeOutDuration));
                transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, MyExtentions.EaseInQuad(fadeOutProgress));

                if (fadeOutProgress >= 1f)
                {
                    AutoPooledPool<HitMarkBehaviour>.ReturnToPool(this);
                    return;
                }
                break;
        }

        Vector3 posBuffer = transform.position;
        posBuffer.z += 0.001f * dt;
        transform.position = posBuffer;
    }

    private void OnEnable()
    {
        EffectRenderer.onEffectTextureChanged += AssignStencilTexture;
    }

    private void OnDisable()
    {
        EffectRenderer.onEffectTextureChanged -= AssignStencilTexture;
    }

    protected override void OnSpawned()
    {
        stateTimer = 0f;
        currentState = FadeState.FadeIn;
    }

    protected override void OnReturnedToPool()
    {
        stateTimer = 0f;
        currentState = FadeState.FadeIn;
    }
}