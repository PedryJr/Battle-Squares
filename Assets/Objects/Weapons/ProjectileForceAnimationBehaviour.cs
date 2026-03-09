using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static ForceFieldAnimation;
using static PlayerColoringBehaviour;
using static ProximityPixelationSystem;
using static ProximityPixelSenssor;

[RequireComponent(typeof(ProximityPixelSenssor))]
public class ProjectileForceAnimationBehaviour : AutoPooledBehaviour, IProximityPixelAnimation
{
    [Header("Animation duration")]
    [SerializeField] float duration = 1.0f;

    [Header("Animation Settings")]
    [SerializeField] ForceFieldAnimation animations;

    PlayerBehaviour player;
    ProjectileBehaviour attatchedProjectile;
    ProximityPixelSenssor senssor;

    float animationTimer = 0f;

    ForceFieldData baseSampleData;

    private void Awake()
    {
        senssor = GetComponent<ProximityPixelSenssor>();
    }

    public void Initialize(ProjectileBehaviour projectile)
    {
        attatchedProjectile = projectile;
        player = attatchedProjectile.owningPlayer;
        animationTimer = 0f;
        RegenerateGroundTruth();
    }

    public void RegenerateGroundTruth()
    {
        baseSampleData.gradient = senssor.proximityPixelSensor.gridSpaceColor;
        baseSampleData.parameters = senssor.proximityPixelSensor.gridSpaceSensor;
        baseSampleData.gradient.color = player.PlayerColor.ProjectileLightColor;
        baseSampleData.colorComp = new ColorComponent();
        baseSampleData.colorComp.SetHue(player.PlayerColor.ReadColorHue);
        baseSampleData.colorComp.saturation = player.PlayerColor.projectileLightColorSAT;
        baseSampleData.colorComp.value = player.PlayerColor.projectileLightColorVAL;
    }

    void Update()
    {
        animationTimer += Time.deltaTime;
        float t = Mathf.Clamp01(animationTimer / duration);
        RegenerateGroundTruth();
        animations.SampleFast(ref baseSampleData, senssor, t);
        if (attatchedProjectile) transform.position = attatchedProjectile.transform.position;
        if (t >= 1f) AutoPooledPool<ProjectileForceAnimationBehaviour>.ReturnToPool(this);
    }

    protected override void OnSpawned()
    {
        if (!senssor.enabled) senssor.enabled = true;
    }

    protected override void OnReturnedToPool()
    {
        if (senssor.enabled) senssor.enabled = false;
        senssor.gridSpaceSensor = baseSampleData.parameters;
        senssor.gridSpaceColor = baseSampleData.gradient;
        animationTimer = 0f;
        attatchedProjectile = null;
    }

    enum LifeState
    {
        Active,
        PostMortem
    }
}