using UnityEngine;

public sealed class StencilInfectorBehaviour : MonoBehaviour
{
    [SerializeField]
    float Stencil;
    public void SetStencil(float stencil) => Stencil = stencil;
    public float GetStencil() => Stencil;

}
