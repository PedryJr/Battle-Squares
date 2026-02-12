using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class HitMarkBehaviour : AutoPooledBehaviour
{
    /*    private const float FadeInDuration = 0.15f;
        private const float StayDuration = 10f;
        private const float FadeOutDuration = 0.15f;*/

    private const float FadeInDuration = 0.1f;
    private const float StayDuration = 15f;
    private const float FadeOutDuration = 0.1f;

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

    public void Initialize(PlayerBehaviour owner, float hitMarkSize)
    {
        targetScale = new Vector3(hitMarkSize, hitMarkSize, 1f);
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
                    SpawnStayParticles();
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

    public void SpawnStayParticles()
    {
        int edgeCount = 0;
        OverlapSquare2DResult result = OverlapSquare2D(transform, resolutionPerUnit, PhysicsMasks.ENVIRONTMENT_MASK, (sample) =>
        {
            if (sample.isEdge)
            {
                edgeCount++;
                Vector2 direction = sample.edgeDirection;
                Vector3 pos = sample.worldPosition;
                float zRot = MyExtentions.Vector2ToDegrees(direction);
                ParticleBehaviour particle = AutoPooledPool<ParticleBehaviour>.Spawn(particleBehaviour, pos, Quaternion.Euler(0, 0, zRot), transform.parent);
                ParticleSystemRenderer[] renderers = particle.ParticleSystemRenderers;
                ParticleSystem[] systems = particle.ParticleSystems;
                for (int i = 0; i < systems.Length; i++) owner.PlayerColor.AssignMaterialToParticleRendererVariant2(renderers[i], systems[i]);
            }
        });
    }

    [SerializeField]
    ParticleBehaviour particleBehaviour;

    public override void OnValidate()
    {
        base.OnValidate();
        if (particleBehaviour) particleBehaviour.lifeTime = StayDuration;
    }
    [SerializeField]
    int resolutionPerUnit = 10;

    private void OnDrawGizmos()
    {
        OverlapSquare2DResult result = OverlapSquare2D(transform, resolutionPerUnit, PhysicsMasks.ENVIRONTMENT_MASK, (sample) =>
        {
            if (sample.isEdge)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(sample.worldPosition, 0.5f / resolutionPerUnit);

                // Draw edge direction
                if (sample.edgeDirection != Vector2.zero)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(sample.worldPosition, (Vector3)sample.worldPosition + (Vector3)sample.edgeDirection * 0.3f);
                }
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(sample.worldPosition, 0.5f / resolutionPerUnit);
            }
        });

        // Draw overall direction
        if (result.hits == result.scans || result.hits == 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
        }
        else
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)result.majorDirection.normalized);
        }
    }

    public struct SamplePoint
    {
        public Vector2 worldPosition;
        public bool isEdge;
        public Vector2 edgeDirection;

        public SamplePoint(Vector2 worldPosition, bool isEdge, Vector2 edgeDirection)
        {
            this.worldPosition = worldPosition;
            this.isEdge = isEdge;
            this.edgeDirection = edgeDirection;
        }
    }

    public struct OverlapSquare2DResult
    {
        public Vector2 majorDirection;
        public int hits;
        public int scans;

        public OverlapSquare2DResult(Vector2 majorDirection, int hits, int scans)
        {
            this.majorDirection = majorDirection;
            this.hits = hits;
            this.scans = scans;
        }
    }

    public static OverlapSquare2DResult OverlapSquare2D(
        Transform areaTransform,
        int resolutionPerUnit,
        LayerMask layerMask,
        Action<SamplePoint> onSample = null)
    {
        if (resolutionPerUnit <= 0)
            return new OverlapSquare2DResult(Vector2.zero, 0, 0);

        Vector2 vectorFieldDirection = Vector2.zero;
        int hits = 0;
        int scans = 0;

        Vector3 scale = areaTransform.lossyScale;
        Vector3 position = areaTransform.position;
        Quaternion rotation = areaTransform.rotation;

        // Calculate resolution based on area size
        int resX = Mathf.Max(1, Mathf.RoundToInt(scale.x * resolutionPerUnit));
        int resY = Mathf.Max(1, Mathf.RoundToInt(scale.y * resolutionPerUnit));

        float stepX = resX > 1 ? 1f / (resX - 1) : 0f;
        float stepY = resY > 1 ? 1f / (resY - 1) : 0f;

        // First pass: collect hit data
        bool[,] hitMap = new bool[resX, resY];

        for (int y = 0; y < resY; y++)
        {
            float ny = -0.5f + y * stepY;

            for (int x = 0; x < resX; x++)
            {
                float nx = -0.5f + x * stepX;

                Vector2 vectorInField = new Vector2(nx * scale.x, ny * scale.y);
                Vector3 localPoint = new Vector3(vectorInField.x, vectorInField.y, 0f);
                Vector2 worldPoint = position + rotation * localPoint;

                bool hasHit = Physics2D.OverlapPoint(worldPoint, layerMask) != null;
                hitMap[x, y] = hasHit;

                if (hasHit)
                {
                    hits++;
                    vectorFieldDirection += vectorInField;
                }
                scans++;
            }
        }

        // Second pass: detect edges and calculate edge directions
        Vector2Int[] neighborOffsets = new Vector2Int[]
        {
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 0),   // Right
            new Vector2Int(0, -1),  // Down
            new Vector2Int(0, 1),   // Up
            new Vector2Int(-1, -1), // Bottom-left
            new Vector2Int(1, -1),  // Bottom-right
            new Vector2Int(-1, 1),  // Top-left
            new Vector2Int(1, 1)    // Top-right
        };

        for (int y = 0; y < resY; y++)
        {
            float ny = -0.5f + y * stepY;

            for (int x = 0; x < resX; x++)
            {
                if (!hitMap[x, y]) continue;

                float nx = -0.5f + x * stepX;
                Vector2 vectorInField = new Vector2(nx * scale.x, ny * scale.y);
                Vector3 localPoint = new Vector3(vectorInField.x, vectorInField.y, 0f);
                Vector2 worldPoint = position + rotation * localPoint;

                // Check neighbors for edges
                bool isEdge = false;
                Vector2 edgeDirectionSum = Vector2.zero;
                int edgeCount = 0;

                foreach (var offset in neighborOffsets)
                {
                    int neighborX = x + offset.x;
                    int neighborY = y + offset.y;

                    // Check if neighbor is within bounds
                    bool neighborInBounds = neighborX >= 0 && neighborX < resX &&
                                           neighborY >= 0 && neighborY < resY;

                    if (neighborInBounds)
                    {
                        // Only count as edge if neighbor exists and has no hit
                        if (!hitMap[neighborX, neighborY])
                        {
                            isEdge = true;
                            edgeDirectionSum += new Vector2(offset.x, offset.y);
                            edgeCount++;
                        }
                    }
                    // Neighbors outside bounds don't count as edges
                }

                Vector2 edgeDirection = Vector2.zero;
                if (edgeCount > 0)
                {
                    edgeDirection = (edgeDirectionSum / edgeCount).normalized * (1f / resolutionPerUnit);
                    // Rotate to world space
                    edgeDirection = rotation * edgeDirection;
                }

                SamplePoint sample = new SamplePoint(worldPoint, isEdge, edgeDirection);
                onSample?.Invoke(sample);
            }
        }

        Vector2 finalMajorDirection = hits > 0 ? vectorFieldDirection / hits : Vector2.zero;
        return new OverlapSquare2DResult(finalMajorDirection, hits, scans);
    }

}