using System.Collections.Generic;
using UnityEngine;

public class TransmitStencilBehaviour : MonoBehaviour
{

    public static List<Collider2D> compositeCompletes;

    private void Awake()
    {
        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.includeLayers = 0b01111111111111111111111111111111;
    }

    private void Update()
    {
        foreach (var item in compositeCompletes)
        {
            if(TryGetComponent(out Collider2D myCol))
            {
                if (myCol.IsTouching(item))
                {
                    if(item.TryGetComponent(out StencilInfectorBehaviour infector))
                    {
                        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                        materialPropertyBlock.SetVector("_Stencil", new Vector4(infector.GetStencil(), infector.GetStencil(), infector.GetStencil(), infector.GetStencil()));
                        transform.GetChild(0).GetComponent<MeshRenderer>().SetPropertyBlock(materialPropertyBlock);
                        Destroy(GetComponent<PolygonCollider2D>());
                        Destroy(GetComponent<Rigidbody2D>());
                        Destroy(this);
                    }
                    Debug.Log("Bruh");
                }
            }
        }
    }

}
