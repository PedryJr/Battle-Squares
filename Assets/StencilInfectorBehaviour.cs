using UnityEngine;

public class StencilInfectorBehaviour : MonoBehaviour
{
    [SerializeField]
    float Stencil;
    public void SetStencil(float stencil) => Stencil = stencil;
    public float GetStencil() => Stencil;

}
