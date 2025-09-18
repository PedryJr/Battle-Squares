using System.Collections.Generic;
using UnityEngine;

public sealed class Generate : MonoBehaviour
{

    [SerializeField]
    Vector3 offset;

    [SerializeField]
    ProximityPixel prefab;

    [ContextMenu("Decimate")]
    void DecimateFunc()
    {
        List<Transform> children = new();
        foreach (Transform t in transform) children.Add(t);

        for (int i = 0; i < children.Count; i++)
        {
            DestroyImmediate(children[i].gameObject);
        }

    }

    [ContextMenu("Generate")]
    void GenerateFunc()
    {

        int dim = 128;

        for (int i = -dim; i < dim; i++) for (int j = -dim; j < dim; j++) GeneraateLocation(i, j);

    }

    void GeneraateLocation(int x, int y)
    {

        ProximityPixel proximityPixel = Instantiate(prefab, offset + new Vector3(x, y, transform.position.z), Quaternion.identity, transform);

    }

}
