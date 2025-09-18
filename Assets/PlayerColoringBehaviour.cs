using System;
using UnityEngine;

public class PlayerColoringBehaviour : MonoBehaviour
{

    private float hue = 0f;
    public void SetColorHue(float hue)
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

    [SerializeField] ColorComponent uiKnobColor;
    public Color UIKnobColor => uiKnobColor.ActiveColor;

    [SerializeField] ColorComponent ammoColor;
    public Color AmmoColor => ammoColor.ActiveColor;

    [SerializeField] ColorComponent lightColor;
    public Color LightColor => lightColor.ActiveColor;

    private void RefreshColorComponents()
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
        ammoColor.SetHue(hue);
        lightColor.SetHue(hue);
    }

    private void Awake()
    {
        
    }

    private void Start()
    {
        
    }

/*    private void Update()
    {
        RefreshColorComponents();
    }*/




    [Serializable]
    public struct ColorComponent
    {
        [SerializeField, Range(0f, 1f)] private float saturation;
        [SerializeField, Range(0f, 1f)] private float value;
        [SerializeField, Range(0f, 1f)] private float alpha;

        [HideInInspector] private Color activeColor;

        public void SetHue(float hue)
        {
            activeColor = Color.HSVToRGB(hue, saturation, value, true);
            activeColor.a = alpha;
        }

        public Color ActiveColor => activeColor;
    }

}
