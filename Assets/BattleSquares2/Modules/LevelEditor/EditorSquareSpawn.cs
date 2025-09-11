using UnityEngine;

public sealed class EditorSquareSpawn : MonoBehaviour
{

    [SerializeField]
    Transform nozzle;

    float nozzleDistance = 0f;

    private void Awake()
    {
        nozzleDistance = Vector2.Distance(nozzle.position, transform.position);
        targetPosition = transform.position;
        originalPosition = transform.position;
    }

    public Vector3 originalPosition;
    public Vector3 targetPosition;

    private void Update()
    {
        targetPosition.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 20f);

        Vector3 nozzleLocalPos = ((Vector2)transform.position).normalized * nozzleDistance;
        nozzleLocalPos.z = 0;

        float nozzleRot = Mathf.Atan2(nozzleLocalPos.y, nozzleLocalPos.x) * Mathf.Rad2Deg;

        nozzle.position = transform.position + nozzleLocalPos;
        nozzle.rotation = Quaternion.Euler(0, 0, nozzleRot);
    }

    public void Drag(Vector2 mousePos)
    {
        targetPosition = mousePos;
    }

}
