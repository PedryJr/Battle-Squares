using UnityEngine;

public sealed class ApplyDefaultMods : MonoBehaviour
{
    [SerializeField]
    private GameObject mods;

    [SerializeField]
    private bool init;

    private ModSlider[] behaviours;

    private void Awake()
    {
        behaviours = mods.GetComponentsInChildren<ModSlider>(true);

        if (init) RESUME();
    }

    public void SELECT()
    {
        foreach (var behaviour in behaviours)
        {
            behaviour.slider.value = behaviour.defaultValue;
            behaviour.ModChange(behaviour.slider.value);
        }
    }

    public void SAVE()
    {
        Mods.SaveMods();
    }

    public void LOAD()
    {
        Mods.LoadMods();

        foreach (var behaviour in behaviours)
        {
            behaviour.slider.value = Mods.at[behaviour.modIndex];
            behaviour.ModChange(behaviour.slider.value);
        }
    }

    public void RESUME()
    {
        foreach (var behaviour in behaviours)
        {
            behaviour.slider.value = Mods.at[behaviour.modIndex];
            behaviour.ModChange(behaviour.slider.value);
        }
    }
}