using UnityEngine;

[ExecuteAlways]
public class TransformFollowBehaviour : MonoBehaviour
{

    [SerializeField] float speed;
    [SerializeField] Transform target;

    [SerializeField] bool followPosition = true;
    [SerializeField] bool followRotation = false;
    [SerializeField] bool followScale = false;

    public static TransformFollowBehaviour instance;

    private void Awake()
    {
        if ((target.GetComponent<CursorBehaviour>())) instance = this;
    }

    private void Update()
    {

        if (target == null) return;
        if (followPosition) transform.position = Vector3.Lerp(transform.position, target.position, speed * Time.deltaTime);
        if (followRotation) transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, speed * Time.deltaTime);
        if (followScale) transform.localScale = Vector3.Lerp(transform.localScale, target.localScale, speed * Time.deltaTime);
    }

}
