using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AmmoCounterBehaviour : MonoBehaviour
{

    [SerializeField]
    bool primary;

    [SerializeField]
    Image ammoVisualizer;

    int primaryRemaining;
    int recordPrimaryRemaining;

    int secondaryRemaining;
    int recordSecondaryRemaining;

    NozzleBehaviour nozzleBehaviour;

    VisualElement[] ammoVisualizers;

    Color emptyColor;

    public void UnitHUD()
    {

        emptyColor = GetComponent<Image>().color;

        nozzleBehaviour = FindAnyObjectByType<PlayerSynchronizer>().localSquare.nozzleBehaviour;

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

    private void Update()
    {

        if (primary) UpdatePrimary();
        else UpdateSecondary();

    }

    public void UpdatePrimary()
    {


        if (!nozzleBehaviour) return;
        Color color;
        primaryRemaining = nozzleBehaviour.primaryAmmo - nozzleBehaviour.primaryShots;
        float lerp = Mathf.Clamp(nozzleBehaviour.primaryTimeSinceShot, 0, nozzleBehaviour.primaryReloadTime) / nozzleBehaviour.primaryReloadTime;
        lerp = MyExtentions.EaseInQuad( MyExtentions.EaseInExpo(lerp));

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

            ammoVisualizers[i].timer = Mathf.Clamp01(ammoVisualizers[i].timer);
            if (ammoVisualizers[i].timer == 0) ammoVisualizers[i].transition2 = true;

            if (ammoVisualizers[i].transition2)
            {
                color = Color.Lerp(emptyColor, nozzleBehaviour.owningPlayerColor, Mathf.SmoothStep(0, 1, ammoVisualizers[i].timer));

            }
            else
            {
                color = Color.Lerp(emptyColor, nozzleBehaviour.owningPlayerColor, ammoVisualizers[i].timer);

            }
            color.r *= 0.8f;
            color.g *= 0.8f;
            color.b *= 0.8f;
            color.a = 1;
            ammoVisualizers[i].image.color = color;

        }

    }

    public void UpdateSecondary()
    {

        if (!nozzleBehaviour) return;
        Color color;
        secondaryRemaining = nozzleBehaviour.secondaryAmmo - nozzleBehaviour.secondaryShots;
        float lerp = Mathf.Clamp(nozzleBehaviour.secondaryTimeSinceShot, 0, nozzleBehaviour.secondaryReloadTime) / nozzleBehaviour.secondaryReloadTime;
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
            else if (ammoVisualizers[i].transition) ammoVisualizers[i].timer -= Time.deltaTime * 8;

            ammoVisualizers[i].timer = Mathf.Clamp01(ammoVisualizers[i].timer);

            if (ammoVisualizers[i].timer == 0) ammoVisualizers[i].transition2 = true;
            if (ammoVisualizers[i].transition2)
            {
                color = Color.Lerp(emptyColor, nozzleBehaviour.owningPlayerColor, Mathf.SmoothStep(0, 1, ammoVisualizers[i].timer));

            }
            else
            {
                color = Color.Lerp(emptyColor, nozzleBehaviour.owningPlayerColor, ammoVisualizers[i].timer);

            }
            color.r *= 0.8f;
            color.g *= 0.8f;
            color.b *= 0.8f;
            color.a = 1;
            ammoVisualizers[i].image.color = color;

        }

    }

    public void UpdateWeaponType()
    {

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

    struct VisualElement
    {

        public Image image;
        public bool transition;
        public float timer;
        public bool transition2;

    }

}
