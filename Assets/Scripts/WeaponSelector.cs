using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static ProjectileManager;

public sealed class WeaponSelector : MonoBehaviour
{

    private int funcTracker = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CallFromUpdateManager(in WeaponSelector obj) => obj.MyUpdate();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void OnEnable()
    {
        fixed (int* trackerPtr = &funcTracker) MyUpdateManager<WeaponSelector>.Instance.Register(&CallFromUpdateManager, this, trackerPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void OnDisable()
    {
        fixed (int* trackerPtr = &funcTracker) MyUpdateManager<WeaponSelector>.Instance.Unregister(trackerPtr);
    }

    [SerializeField]
    public ProjectileType weaponType;

    [SerializeField]
    Image primary;

    [SerializeField]
    Image secondary;

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

    public void Select()
    {

        NozzleBehaviour nozzle = GameObject.FindGameObjectWithTag("Sync").GetComponent<PlayerSynchronizer>().localSquare.nozzleBehaviour;

        if (nozzle.primary == weaponType) return;

        secondary.sprite = primary.sprite;
        primary.sprite = GetComponent<Image>().sprite;

        nozzle.UpdateWeaponTypes(weaponType);

        AmmoCounterBehaviour[] ammoCounters = FindObjectsByType<AmmoCounterBehaviour>(FindObjectsSortMode.None);
        foreach (AmmoCounterBehaviour ammoCounter in ammoCounters)
        {
            ammoCounter.UpdateWeaponType();
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Sprite GetImage() => selectorImage.sprite;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MyUpdate() => hoverAnimation.onHoveredColor = playerSynchronizer.localSquare.PlayerColor.HighlightedWeaponColor;

}
