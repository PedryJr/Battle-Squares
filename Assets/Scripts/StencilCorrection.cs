using UnityEngine;

public class StencilCorrection : MonoBehaviour
{

    [SerializeField]
    CorrectFor correctFor;

    [SerializeField]
    Material material;

    private void Awake()
    {
        material.SetFloat("_ForceAboveZeroStencil", (float) correctFor);
    }

    enum CorrectFor : int
    {
        Lobby = 1,
        InGame = 0,
    }

}
