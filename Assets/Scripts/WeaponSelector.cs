using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public sealed class WeaponSelector : MonoBehaviour
{

    [SerializeField]
    public ushort weaponType;

    Image selectorImage;

    [SerializeField]
    ScrollRect scroll;

    ButtonHoverAnimation hoverAnimation;
    PlayerSynchronizer playerSynchronizer;

    private void Awake()
    {
        
        playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        hoverAnimation = GetComponent<ButtonHoverAnimation>();
        selectorImage = GetComponent<Image>();

    }

    public void Initialize(WeaponBuilder weapon)
    {

        selectorImage.sprite = weapon.GetSprite;
        weaponType = weapon.typeID;

    }

    public void Select()
    {

        NozzleBehaviour nozzle = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>().localSquare.nozzleBehaviour;

        if (nozzle.primary == weaponType) return;

        nozzle.UpdateWeaponTypes(weaponType);

        AmmoCounterBehaviour[] ammoCounters = FindObjectsByType<AmmoCounterBehaviour>(FindObjectsSortMode.None);
        foreach (AmmoCounterBehaviour ammoCounter in ammoCounters) ammoCounter.UpdateWeaponType();

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Sprite GetImage() => selectorImage.sprite;
/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update()
    {
        if(playerSynchronizer.localSquare) hoverAnimation.onHoveredColor = playerSynchronizer.localSquare.PlayerColor.HighlightedWeaponColor;
    }
*/
}
