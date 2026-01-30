using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public sealed class PlayerColoringBehaviour : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float hue = 0f;

    [SerializeField] Material playerMaterial;
    [SerializeField] Material projectileMaterial;
    [SerializeField] Material particleMaterial;

    private void Awake()
    {
        playerMaterial = Instantiate(playerMaterial);
        projectileMaterial = Instantiate(projectileMaterial);
        particleMaterial = Instantiate(particleMaterial);

        playerMaterial.enableInstancing = true;
        projectileMaterial.enableInstancing = true;
        particleMaterial.enableInstancing = true;
    }

#if UNITY_EDITOR
    private void Update() => RefreshColorComponents();
#endif


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignMaterialToProjectile(in SpriteRenderer projectileRenderer) => projectileRenderer.sharedMaterial = projectileMaterial;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignMaterialToPlayer(in SpriteRenderer playerRenderer) => playerRenderer.sharedMaterial = playerMaterial;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AssignMaterialToParticleRenderer(in ParticleSystemRenderer particleRenderer, in ParticleSystem particleSystem)
    {
        particleMaterial.color = Color.white;
        particleRenderer.sharedMaterial = particleMaterial;
        particleRenderer.trailMaterial = particleMaterial;
        particleRenderer.applyActiveColorSpace = false;

        MainModule mainModuleForParticles = particleSystem.main;
        mainModuleForParticles.startColor = ParticleColor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetColorHue(in float hue)
    {
        this.hue = hue;
        RefreshColorComponents();
    }

    public float ReadColorHue => hue;

    [SerializeField] ColorComponent primaryColor;
    public Color PrimaryColor => primaryColor.ActiveColor;

    [SerializeField] ColorComponent secondaryColor;
    public Color SecondaryColor => secondaryColor.ActiveColor;

    [SerializeField] ColorComponent exposedHealthColor;
    public Color ExposedHealthColor => exposedHealthColor.ActiveColor;

    [SerializeField] ColorComponent nozzleColor;
    public Color NozzleColor => nozzleColor.ActiveColor;

    [SerializeField] ColorComponent projectileColor;
    public Color ProjectileColor => projectileColor.ActiveColor;

    [SerializeField] ColorComponent particleColor;
    public Color ParticleColor => particleColor.ActiveColor;

    [SerializeField] ColorComponent chatBubbleColor;
    public Color ChatBoxColor => chatBubbleColor.ActiveColor;

    [SerializeField] ColorComponent dogTagColor;
    public Color DogTagColor => dogTagColor.ActiveColor;

    [SerializeField] ColorComponent hitMarkColor;
    public Color HitMarkColor => hitMarkColor.ActiveColor;
    [SerializeField] ColorComponent hitMarkFadeColor;
    public Color HitMarkFadeColor => hitMarkFadeColor.ActiveColor;

    [SerializeField] ColorComponent pfpBorderNotReadyColor;
    public Color PfpBorderNotReadyColor => pfpBorderNotReadyColor.ActiveColor;

    [SerializeField] ColorComponent pfpBorderIsReadyColor;
    public Color PfpBorderIsReadyColor => pfpBorderIsReadyColor.ActiveColor;

    [SerializeField] ColorComponent cursorColorOnHover;
    public Color CursorDefaultColor => cursorColorOnHover.ActiveColor;

    [SerializeField] ColorComponent cursorColorOffHover;
    public Color CursorHoverColor => cursorColorOffHover.ActiveColor;

    [SerializeField] ColorComponent selectedWeaponColor;
    public Color SelectedWeaponColor => selectedWeaponColor.ActiveColor;
    [SerializeField] ColorComponent highlightedWeaponColor;
    public Color HighlightedWeaponColor => highlightedWeaponColor.ActiveColor;

    [SerializeField] ColorComponent uiKnobColor;
    public Color UIKnobColor => uiKnobColor.ActiveColor;

    [SerializeField] ColorComponent uiButtonColorNormal;
    public Color UiButtonColorNormal => uiButtonColorNormal.ActiveColor;

    [SerializeField] ColorComponent uiButtonColorHighlighted;
    public Color UiButtonColorHighlighted => uiButtonColorHighlighted.ActiveColor;

    [SerializeField] ColorComponent ammoColor;
    public Color AmmoColor => ammoColor.ActiveColor;
    [SerializeField] ColorComponent ammoContainerColor;
    public Color AmmoContainerColor => ammoContainerColor.ActiveColor;

    [SerializeField] ColorComponent lightColor;
    public Color LightColor => lightColor.ActiveColor;

    [SerializeField] ColorComponent projectileLightColor;
    public Color ProjectileLightColor => projectileLightColor.ActiveColor;
    public float projectileLightColorSAT => projectileLightColor.ReadSat;
    public float projectileLightColorVAL => projectileLightColor.ReadValue;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshColorComponents()
    {
        primaryColor.SetHue(hue);
        secondaryColor.SetHue(hue);
        exposedHealthColor.SetHue(hue);
        nozzleColor.SetHue(hue);
        projectileColor.SetHue(hue);
        particleColor.SetHue(hue);
        chatBubbleColor.SetHue(hue);
        dogTagColor.SetHue(hue);
        hitMarkColor.SetHue(hue);
        pfpBorderNotReadyColor.SetHue(hue);
        pfpBorderIsReadyColor.SetHue(hue);
        cursorColorOnHover.SetHue(hue);
        cursorColorOffHover.SetHue(hue);
        selectedWeaponColor.SetHue(hue);
        uiKnobColor.SetHue(hue);
        uiButtonColorNormal.SetHue(hue);
        uiButtonColorHighlighted.SetHue(hue);
        ammoColor.SetHue(hue);
        lightColor.SetHue(hue);
        hitMarkFadeColor.SetHue(hue);
        highlightedWeaponColor.SetHue(hue);
        ammoContainerColor.SetHue(hue);
        projectileLightColor.SetHue(hue);
    }

    [Serializable]
    public struct ColorComponent
    {
        [SerializeField] CorrectionType colorCorrectionType;
        [HideInInspector] public float hue;
        [SerializeField, Range(0f, 1f)] public float saturation;
        [SerializeField, Range(0f, 1f)] public float value;
        [SerializeField, Range(0f, 1f)] public float alpha;
        [HideInInspector] private Color activeColor;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetHue(in float hue)
        {
            this.hue = hue;
            activeColor = Color.HSVToRGB(hue, saturation, value, true);
            activeColor.a = alpha;
        }

        public float ReadSat => saturation;
        public float ReadValue => value;
        public float ReadHue => hue;

        public Color ActiveColor
        {
            get
            {
                switch (colorCorrectionType)
                {
                    case CorrectionType.Linear: return activeColor.linear;
                    case CorrectionType.Gamma: return activeColor.gamma;
                    default: return activeColor;
                }
            }
        }
        enum CorrectionType
        {
            Raw = 0,
            Linear = 1,
            Gamma = 2,
        }
    }
}
