using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FeedTransformToShader : MonoBehaviour
{

    [SerializeField]
    TMP_Text text;

    [SerializeField] Material material;

    [SerializeField]
    bool feedModel = true;
    [SerializeField]
    string modelPropertyName = "_ObjectToWorldMatrix";
    [SerializeField] RectTransform targetTransform;

    [SerializeField]
    bool feedModel2 = true;
    [SerializeField]
    string modelPropertyName2 = "_ObjectToWorldMatrix";
    [SerializeField] Transform targetTransform2;

    void Update()
    {
        if (!material) return;
        if(feedModel) material.SetMatrix(modelPropertyName, targetTransform.localToWorldMatrix);
        if (feedModel2) material.SetMatrix(modelPropertyName2, targetTransform2.localToWorldMatrix);
    }

}
