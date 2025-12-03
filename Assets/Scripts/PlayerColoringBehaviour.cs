using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public sealed class PlayerColoringBehaviour : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float hue = 0f;

    [SerializeField] bool EvenColorSpace;

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

    [SerializeField] ColorComponent ammoColor;
    public Color AmmoColor => ammoColor.ActiveColor;

    [SerializeField] ColorComponent lightColor;
    public Color LightColor => lightColor.ActiveColor;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RefreshColorComponents()
    {
        primaryColor.SetHue(hue, EvenColorSpace);
        secondaryColor.SetHue(hue, EvenColorSpace);
        exposedHealthColor.SetHue(hue, EvenColorSpace);
        nozzleColor.SetHue(hue, EvenColorSpace);
        projectileColor.SetHue(hue, EvenColorSpace);
        particleColor.SetHue(hue, EvenColorSpace);
        chatBubbleColor.SetHue(hue, EvenColorSpace);
        dogTagColor.SetHue(hue, EvenColorSpace);
        hitMarkColor.SetHue(hue, EvenColorSpace);
        pfpBorderNotReadyColor.SetHue(hue, EvenColorSpace);
        pfpBorderIsReadyColor.SetHue(hue, EvenColorSpace);
        cursorColorOnHover.SetHue(hue, EvenColorSpace);
        cursorColorOffHover.SetHue(hue, EvenColorSpace);
        selectedWeaponColor.SetHue(hue, EvenColorSpace);
        uiKnobColor.SetHue(hue, EvenColorSpace);
        ammoColor.SetHue(hue, EvenColorSpace);
        lightColor.SetHue(hue, EvenColorSpace);
        hitMarkFadeColor.SetHue(hue, EvenColorSpace);
        highlightedWeaponColor.SetHue(hue, EvenColorSpace);
    }

    [Serializable]
    public struct ColorComponent
    {
        [SerializeField] bool convertToLinear;
        [SerializeField, Range(0f, 1f)] private float saturation;
        [SerializeField, Range(0f, 1f)] private float value;
        [SerializeField, Range(0f, 1f)] private float alpha;

        [HideInInspector] private Color activeColor;
        [HideInInspector] private Color activeColorLinear;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetHue(in float hue, bool EvenColorSpace)
        {
            if (EvenColorSpace) activeColor = PerceptuallyEvenHSVToRGB(hue, saturation, value * 0.99f);
            else activeColor = Color.HSVToRGB(hue, saturation, value, true);
            activeColor.a = alpha;
            activeColorLinear.r = Mathf.Pow(activeColor.r, 1f / 2.2f);
            activeColorLinear.g = Mathf.Pow(activeColor.g, 1f / 2.2f);
            activeColorLinear.b = Mathf.Pow(activeColor.b, 1f / 2.2f);
            activeColorLinear.a = alpha;
        }

        public Color ActiveColor => convertToLinear ? activeColorLinear : activeColor;


        /// <summary>
        /// Converts HSV to RGB with even perceptual brightness distribution using CIELAB L* space
        /// </summary>
        public static Color PerceptuallyEvenHSVToRGB(float h, float s, float v)
        {
            // Convert HSV to linear RGB first
            Color hsvColor = Color.HSVToRGB(h, s, v, true);

            // Convert linear RGB to CIELAB
            RGBToLab(hsvColor, out float L, out float a, out float b);

            // Adjust lightness (L*) to be perceptually uniform based on value
            float targetL = v * 100f; // L* ranges from 0 (black) to 100 (white)

            // Scale chroma to maintain saturation while achieving target lightness
            float currentChroma = Mathf.Sqrt(a * a + b * b);
            float maxChroma = MaxChromaForL(targetL, h * 360f);

            if (maxChroma > 0 && currentChroma > maxChroma)
            {
                float scale = maxChroma / currentChroma;
                a *= scale;
                b *= scale;
            }

            // Set the target perceptual lightness
            L = targetL;

            // Convert back to linear RGB
            return LabToRGB(L, a, b);
        }

        private static void RGBToLab(Color rgb, out float L, out float a, out float b)
        {
            // Convert linear RGB to XYZ
            float r = rgb.r;
            float g = rgb.g;
            float bVal = rgb.b;

            // sRGB to XYZ matrix (linear)
            float x = 0.4124564f * r + 0.3575761f * g + 0.1804375f * bVal;
            float y = 0.2126729f * r + 0.7151522f * g + 0.0721750f * bVal;
            float z = 0.0193339f * r + 0.1191920f * g + 0.9503041f * bVal;

            // XYZ to CIELAB conversion
            // Reference white - D65
            const float refX = 0.95047f;
            const float refY = 1.00000f;
            const float refZ = 1.08883f;

            float xRatio = x / refX;
            float yRatio = y / refY;
            float zRatio = z / refZ;

            float fx = xRatio > 0.008856f ? Mathf.Pow(xRatio, 1.0f / 3.0f) : (7.787f * xRatio) + (16.0f / 116.0f);
            float fy = yRatio > 0.008856f ? Mathf.Pow(yRatio, 1.0f / 3.0f) : (7.787f * yRatio) + (16.0f / 116.0f);
            float fz = zRatio > 0.008856f ? Mathf.Pow(zRatio, 1.0f / 3.0f) : (7.787f * zRatio) + (16.0f / 116.0f);

            L = Mathf.Max(0, 116.0f * fy - 16.0f);
            a = 500.0f * (fx - fy);
            b = 200.0f * (fy - fz);
        }

        private static Color LabToRGB(float L, float a, float b)
        {
            // CIELAB to XYZ conversion
            float fy = (L + 16.0f) / 116.0f;
            float fx = a / 500.0f + fy;
            float fz = fy - b / 200.0f;

            // Reference white - D65
            const float refX = 0.95047f;
            const float refY = 1.00000f;
            const float refZ = 1.08883f;

            float xRatio = fx > 0.2068966f ? Mathf.Pow(fx, 3.0f) : (fx - 16.0f / 116.0f) / 7.787f;
            float yRatio = fy > 0.2068966f ? Mathf.Pow(fy, 3.0f) : (fy - 16.0f / 116.0f) / 7.787f;
            float zRatio = fz > 0.2068966f ? Mathf.Pow(fz, 3.0f) : (fz - 16.0f / 116.0f) / 7.787f;

            float x = xRatio * refX;
            float y = yRatio * refY;
            float z = zRatio * refZ;

            // XYZ to linear RGB
            float r = Mathf.Clamp01(3.2404542f * x - 1.5371385f * y - 0.4985314f * z);
            float g = Mathf.Clamp01(-0.9692660f * x + 1.8760108f * y + 0.0415560f * z);
            float bVal = Mathf.Clamp01(0.0556434f * x - 0.2040259f * y + 1.0572252f * z);

            return new Color(r, g, bVal, 1f);
        }

        private static float MaxChromaForL(float L, float hue)
        {
            // Simplified approximation of maximum chroma for given L* and hue
            // This ensures we stay within the RGB gamut
            float hueRad = hue * Mathf.Deg2Rad;

            // Chroma limits vary by hue and lightness
            float baseChroma = L * (100f - L) / 100f;

            // Adjust for hue (rough approximation)
            float hueFactor = 1f + 0.5f * Mathf.Sin(2f * hueRad);

            return Mathf.Min(128f, baseChroma * hueFactor);
        }
    }

}
