using UnityEngine;

public class ParticleSystemExportTool : MonoBehaviour
{

    [SerializeField]
    ParticleSystem particleSystem;
    [SerializeField]
    string name;

    private void OnValidate()
    {
        particleSystem = GetComponent<ParticleSystem>();
    }

    [ContextMenu("Export")]
    void Export()
    {

        ParticleSystemSerializer.SaveToFile(particleSystem, "Assets/BuildHelper/" + name + ".json");

    }

    [ContextMenu("Import")]
    void Import()
    {

        ParticleSystemSerializer.LoadFromFile(particleSystem, "Assets/BuildHelper/" + name + ".json");

    }

}
