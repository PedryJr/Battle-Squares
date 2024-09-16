using UnityEngine;

public class CompositeEnabler : MonoBehaviour
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
