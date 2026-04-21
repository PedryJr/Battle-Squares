using UnityEngine;
using static SnekSegmentBehaviour;

[CreateAssetMenu(fileName = "SnekSegmentParams", menuName = "Scriptable Objects/SnekSegmentParams")]
public class SnekSegmentParams : ScriptableObject
{

    [SerializeField]
    public StateParameters spawnedStateParameters;
    [SerializeField]
    public StateParameters heldStateParameters;
    [SerializeField]
    public StateParameters deadOwnerStateParameters;

}
