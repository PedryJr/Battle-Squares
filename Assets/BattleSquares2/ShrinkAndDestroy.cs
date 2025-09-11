using UnityEngine;

public class ShrinkAndDestroy : MonoBehaviour
{

    public float timeToDestroy = 1f;
    public float timer = 0f;

    Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
    }

    private void Update()
    {

        if (timer >= timeToDestroy) Destroy(gameObject);
        else
        {
            timer += Time.deltaTime;
            cachedTransform.localScale = Vector3.Lerp(cachedTransform.localScale, Vector3.zero, Mathf.SmoothStep(0f, 1f, timer / timeToDestroy));
        }
        
    }

}
