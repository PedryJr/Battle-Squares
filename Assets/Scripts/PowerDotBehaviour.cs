using UnityEngine;

public class PowerDotBehaviour : MonoBehaviour
{

    MeshRenderer meshRenderer;
    MaterialPropertyBlock materialPropertyBlock;

    [SerializeField]
    AnimationCurve curve;

    [SerializeField]
    float lifeTime = 1.0f;

    float timer;

    [SerializeField]
    bool begone;

    private void Start()
    {
        if (begone)
        {
            Destroy(gameObject);
            return;
        }
        timer = 0;
        meshRenderer = GetComponent<MeshRenderer>();
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        timer += Time.deltaTime / lifeTime;
        if(timer > 1) Destroy(gameObject);
        else UpdateMaterialProperties();
    }

    void UpdateMaterialProperties()
    {
        materialPropertyBlock.SetFloat("_DistortionStrength", curve.Evaluate(timer));
        meshRenderer.SetPropertyBlock(materialPropertyBlock);
    }
}
