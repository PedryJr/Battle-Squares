using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public sealed class SolidMoverBehaviour : MonoBehaviour
{

    float timer;

    [SerializeField]
    Transform[] points;

    Vector3[] localPoints;
    Quaternion[] localRotations;

    [SerializeField]
    Transform solid;

    public byte id;

    [SerializeField]
    float timeBetweenPoints;

    MapSynchronizer mapSynchronizer;

    [SerializeField]
    float offset;

    [SerializeField]
    float siblingOffset;

    [SerializeField]
    bool customCurve;

    [SerializeField]
    AnimationCurve curve;

    Rigidbody2D rb;

    Vector3 fromPos;
    Vector3 toPos;
    Quaternion fromRot;
    Quaternion toRot;

    byte fromPosId = 0;
    byte toPosId = 1;

    byte lastPosId = 0;

    float lerp;
    float lastLerp;
    [BurstCompile]
    private void Awake()
    {
        
        localPoints = new Vector3[points.Length];
        localRotations = new Quaternion[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            localPoints[i] = points[i].localPosition;
            localRotations[i] = points[i].localRotation;
            Destroy(points[i].gameObject);
        }

        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();

        rb = solid.GetComponent<Rigidbody2D>();

    }
    [BurstCompile]
    private void Start()
    {

        id = (byte) transform.GetSiblingIndex();
        siblingOffset *= id;
        mapSynchronizer.solidMovers.Add(this);
        fromPos = localPoints[fromPosId];
        toPos = localPoints[toPosId];
        fromRot = localRotations[fromPosId];
        toRot = localRotations[toPosId];

        solid.localPosition = Vector3.Lerp(fromPos, toPos, lerp);
        solid.localRotation = Quaternion.Lerp(fromRot, toRot, lerp);

        UpdateAsHost();

    }
    [BurstCompile]
    private void LateUpdate()
    {

        if (rb) return;

        timer = Mathf.Repeat((mapSynchronizer.localTime + offset + siblingOffset) / timeBetweenPoints, localPoints.Length);

        if (customCurve)
        {

            lerp = Mathf.Repeat(timer, 1);

        }
        else
        {

            lerp = Mathf.SmoothStep(0, 1, Mathf.Repeat(timer, 1));

        }

        fromPosId = RepeatIntegerToByte(Mathf.FloorToInt(timer), localPoints.Length);
        toPosId = RepeatIntegerToByte(fromPosId + 1, localPoints.Length);

        if (lastPosId != fromPosId)
        {
            lastPosId = fromPosId;

            fromPos = localPoints[fromPosId];
            toPos = localPoints[toPosId];
            fromRot = localRotations[fromPosId];
            toRot = localRotations[toPosId];

        }



        solid.localPosition = Vector3.Lerp(fromPos, toPos, lerp);
        solid.localRotation = Quaternion.Slerp(fromRot, toRot, lerp);

    }
    [BurstCompile]
    private void FixedUpdate()
    {

        if (!rb) return;

        timer = Mathf.Repeat((mapSynchronizer.localTime + offset + siblingOffset) / timeBetweenPoints, localPoints.Length);

        if (customCurve)
        {

            lerp = Mathf.Repeat(timer, 1);

        }
        else
        {

            lerp = math.smoothstep(0, 1, Mathf.Repeat(timer, 1));

        }

        fromPosId = RepeatIntegerToByte((int)math.floor(timer), localPoints.Length);
        toPosId = RepeatIntegerToByte(fromPosId + 1, localPoints.Length);

        if (lastPosId != fromPosId)
        {
            lastPosId = fromPosId;

            fromPos = localPoints[fromPosId];
            toPos = localPoints[toPosId];
            fromRot = localRotations[fromPosId];
            toRot = localRotations[toPosId];

        }


/*
        solid.localPosition = Vector3.Lerp(fromPos, toPos, lerp);*/
/*        solid.localRotation = Quaternion.Lerp(fromRot, toRot, lerp);*/

        Vector2 targetPosition = transform.TransformPoint(Vector2.Lerp(fromPos, toPos, lerp));
        Vector2 targetDirection = targetPosition - rb.position;
        Vector2 targetVelocity = targetDirection;
        rb.linearVelocity = targetVelocity;
        rb.position = targetPosition;

        float parentRotation = transform.rotation.eulerAngles.z;
        float targetRotation = Quaternion.Slerp(fromRot, toRot, lerp).eulerAngles.z + parentRotation;
        float rotationVelocity = rb.rotation - targetRotation;

        rb.angularVelocity = rotationVelocity;
        rb.rotation = targetRotation;

/*
        Vector2 changeVector = 
            (solid.TransformPoint(toPos) 
            - solid.TransformPoint(fromPos)).normalized * Mathf.Clamp01(lerp - lastLerp);
        rb.linearVelocity = changeVector;
        rb.position = Vector3.Lerp(
            solid.TransformPoint(fromPos),
            solid.TransformPoint(toPos), lerp);*/

        lastLerp = lerp;

    }
    [BurstCompile]
    void UpdateAsHost()
    {
        mapSynchronizer.UpdateFromToPosRpc(id, timer);
    }
    [BurstCompile]
    public void UpdateFromToPos(float timer)
    {

        this.timer = timer;

    }
    [BurstCompile]
    public byte RepeatIntegerToByte(int input, int maxValue)
    {

        int wrappedValue = input % maxValue;

        if (wrappedValue < 0) wrappedValue += maxValue;

        return (byte) wrappedValue;
    }

}
