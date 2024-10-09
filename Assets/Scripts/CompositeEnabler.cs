using UnityEngine;

public sealed class CompositeEnabler : MonoBehaviour
{

    CompositeCollider2D compositeCollider;

    private void Awake()
    {
        
        compositeCollider = GetComponent<CompositeCollider2D>();

    }

    private void OnEnable()
    {
        
        compositeCollider.GenerateGeometry();

    }

}
