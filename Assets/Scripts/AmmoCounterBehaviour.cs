using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

[BurstCompile]
public sealed class AmmoCounterBehaviour : MonoBehaviour
{
    [SerializeField]
    private bool primary;

    [SerializeField]
    private Image ammoVisualizer;

    private int primaryRemaining;
    private int recordPrimaryRemaining;

    private int secondaryRemaining;
    private int recordSecondaryRemaining;

    private NozzleBehaviour nozzleBehaviour;
    private PlayerColoringBehaviour playerColoringBehaviour;

    private VisualElement[] ammoVisualizers;

    private Color emptyColor;

    [BurstCompile]
    public void UnitHUD()
    {
        emptyColor = GetComponent<Image>().color;

        PlayerSynchronizer playerSynchronizer = FindAnyObjectByType<PlayerSynchronizer>();
        nozzleBehaviour = playerSynchronizer.localSquare.nozzleBehaviour;
        playerColoringBehaviour = playerSynchronizer.localSquare.PlayerColor;

        if (primary)
        {
            ammoVisualizers = new VisualElement[nozzleBehaviour.primaryAmmo];
            for (int i = 0; i < ammoVisualizers.Length; i++)
            {
                ammoVisualizers[i].image = Instantiate(ammoVisualizer, transform);
            }
        }
        else
        {
            ammoVisualizers = new VisualElement[nozzleBehaviour.secondaryAmmo];
            for (int i = 0; i < ammoVisualizers.Length; i++)
            {
                ammoVisualizers[i].image = Instantiate(ammoVisualizer, transform);
            }
        }
    }

    [BurstCompile]
    private void Update()
    {
        if (primary) UpdatePrimary();
        else UpdateSecondary();
    }

    [BurstCompile]
    public void UpdatePrimary()
    {
        if (!nozzleBehaviour) return;
        Color color;
        primaryRemaining = nozzleBehaviour.primaryAmmo - nozzleBehaviour.primaryShots;
        float lerp = math.clamp(nozzleBehaviour.primaryTimeSinceShot, 0, nozzleBehaviour.primaryReloadTime) / nozzleBehaviour.primaryReloadTime;
        lerp = MyExtentions.EaseInQuad(MyExtentions.EaseInExpo(lerp));

        if (primaryRemaining != recordPrimaryRemaining)
        {
            recordPrimaryRemaining = primaryRemaining;

            for (int i = 0; i < ammoVisualizers.Length; i++)
            {
                if (i < primaryRemaining)
                {
                    ammoVisualizers[i].transition = false;
                    ammoVisualizers[i].transition2 = false;
                }
                else ammoVisualizers[i].transition = true;
            }
        }

        for (int i = 0; i < ammoVisualizers.Length; i++)
        {
            if (ammoVisualizers[i].transition2) ammoVisualizers[i].timer = lerp;
            else if (ammoVisualizers[i].transition) ammoVisualizers[i].timer -= Time.deltaTime * 8;

            ammoVisualizers[i].timer = math.clamp(ammoVisualizers[i].timer, 0, 1);
            if (ammoVisualizers[i].timer == 0) ammoVisualizers[i].transition2 = true;

            if (ammoVisualizers[i].transition2)
            {
                color = Color.Lerp(emptyColor, playerColoringBehaviour.AmmoColor, math.smoothstep(0, 1, ammoVisualizers[i].timer));
            }
            else
            {
                color = Color.Lerp(emptyColor, playerColoringBehaviour.AmmoColor, ammoVisualizers[i].timer);
            }
            ammoVisualizers[i].image.color = color;
        }
    }

    [BurstCompile]
    public void UpdateSecondary()
    {
        if (!nozzleBehaviour) return;
        Color color;
        secondaryRemaining = nozzleBehaviour.secondaryAmmo - nozzleBehaviour.secondaryShots;
        float lerp = math.clamp(nozzleBehaviour.secondaryTimeSinceShot, 0, nozzleBehaviour.secondaryReloadTime) / nozzleBehaviour.secondaryReloadTime;
        lerp = MyExtentions.EaseInQuad(MyExtentions.EaseInExpo(lerp));
        if (secondaryRemaining != recordSecondaryRemaining)
        {
            recordSecondaryRemaining = secondaryRemaining;

            for (int i = 0; i < ammoVisualizers.Length; i++)
            {
                if (i < secondaryRemaining)
                {
                    ammoVisualizers[i].transition = false;
                    ammoVisualizers[i].transition2 = false;
                }
                else ammoVisualizers[i].transition = true;
            }
        }

        for (int i = 0; i < ammoVisualizers.Length; i++)
        {
            if (ammoVisualizers[i].transition2) ammoVisualizers[i].timer = lerp;
            else if (ammoVisualizers[i].transition) ammoVisualizers[i].timer -= Time.deltaTime * 6.5f;

            ammoVisualizers[i].timer = math.clamp(ammoVisualizers[i].timer, 0, 1);

            if (ammoVisualizers[i].timer == 0) ammoVisualizers[i].transition2 = true;
            if (ammoVisualizers[i].transition2)
            {
                color = Color.Lerp(emptyColor, playerColoringBehaviour.AmmoColor, math.smoothstep(0, 1, ammoVisualizers[i].timer));
            }
            else
            {
                color = Color.Lerp(emptyColor, playerColoringBehaviour.AmmoColor, ammoVisualizers[i].timer);
            }
            ammoVisualizers[i].image.color = color;
        }
    }

    [BurstCompile]
    public void UpdateWeaponType()
    {

        if (ammoVisualizers == null) return;

        for (int i = 0; i < ammoVisualizers.Length; i++)
        {
            Destroy(ammoVisualizers[i].image.gameObject);
            ammoVisualizers[i].image = null;
        }

        if (primary)
        {
            ammoVisualizers = new VisualElement[nozzleBehaviour.primaryAmmo];
            for (int i = 0; i < ammoVisualizers.Length; i++)
            {
                ammoVisualizers[i].image = Instantiate(ammoVisualizer, transform);
            }
        }
        else
        {
            ammoVisualizers = new VisualElement[nozzleBehaviour.secondaryAmmo];
            for (int i = 0; i < ammoVisualizers.Length; i++)
            {
                ammoVisualizers[i].image = Instantiate(ammoVisualizer, transform);
            }
        }
    }

    private void OnEnable()
    {
        UpdateWeaponType();
    }

    private void OnDisable()
    {
        UpdateWeaponType();
    }

    [BurstCompile]
    private struct VisualElement
    {
        public Image image;
        public bool transition;
        public float timer;
        public bool transition2;
    }
}