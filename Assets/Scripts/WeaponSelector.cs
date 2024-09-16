using UnityEngine;
using UnityEngine.UI;
using static ProjectileManager;

public sealed class WeaponSelector : MonoBehaviour
{

    [SerializeField]
    public ProjectileType weaponType;

    [SerializeField]
    Image primary;

    [SerializeField]
    Image secondary;

    Image selectorImage;

    [SerializeField]
    ScrollRect scroll;

    private void Awake()
    {
        
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

    public Sprite GetImage()
    {
        return selectorImage.sprite;
    }

}
