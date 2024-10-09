using System;
using UnityEngine;

public class SolidMoverBehaviour : MonoBehaviour
{

    float timer;

    [SerializeField]
    Transform[] points;

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

    Rigidbody2D rb;

    Vector3 fromPos;
    Vector3 toPos;
    float fromRot;
    float toRot;

    byte fromPosId = 0;
    byte toPosId = 1;

    byte lastPosId = 0;

    float lerp;
    float lastLerp;

    private void Awake()
    {
        
        mapSynchronizer = FindAnyObjectByType<MapSynchronizer>();

        rb = solid.GetComponent<Rigidbody2D>();

    }

    private void Start()
    {

        id = (byte) transform.GetSiblingIndex();
        siblingOffset *= id;
        mapSynchronizer.solidMovers.Add(this);
        fromPos = points[fromPosId].position;
        toPos = points[toPosId].position;
        fromRot = points[fromPosId].eulerAngles.z;
        toRot = points[toPosId].eulerAngles.z;
        UpdateAsHost();

    }

    private void FixedUpdate()
    {

        timer = Mathf.Repeat((mapSynchronizer.localTime + offset + siblingOffset) / timeBetweenPoints, points.Length);
        lerp = Mathf.SmoothStep(0, 1, Mathf.Repeat(timer, 1));

        fromPosId = RepeatIntegerToByte(Mathf.FloorToInt(timer), points.Length);
        toPosId = RepeatIntegerToByte(fromPosId + 1, points.Length);

        if (lastPosId != fromPosId)
        {
            lastPosId = fromPosId;

            fromPos = points[fromPosId].position;
            toPos = points[toPosId].position;
            fromRot = points[fromPosId].eulerAngles.z;
            toRot = points[toPosId].eulerAngles.z;

        }

        solid.position = Vector3.Lerp(fromPos, toPos, lerp);
        solid.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(fromRot, toRot, lerp));

        if (rb)
        {

            Vector2 changeVector = (toPos - fromPos).normalized * Mathf.Clamp01(lerp - lastLerp);
            rb.linearVelocity = changeVector;
            rb.position = Vector3.Lerp(fromPos, toPos, lerp);

            lastLerp = lerp;

        }

    }

    void UpdateAsHost()
    {
        mapSynchronizer.UpdateFromToPosRpc(id, timer);
    }

    public void UpdateFromToPos(float timer)
    {

        this.timer = timer;

    }

    public byte RepeatIntegerToByte(int input, int maxValue)
    {

        int wrappedValue = input % maxValue;

        if (wrappedValue < 0) wrappedValue += maxValue;

        return (byte) wrappedValue;
    }

}
