using UnityEngine;

public class AgentTargetRandomizer : MonoBehaviour
{
    public static AgentTargetRandomizer Instance;
    private void Awake() => Instance = this;
    public Transform GetARandomTarget() => transform.GetChild(Random.Range(0, 9999) % transform.childCount);
    public void OnDrawGizmos()
    {
        foreach (Transform item in transform) Gizmos.DrawSphere(item.position, 0.5f);
    }
}
