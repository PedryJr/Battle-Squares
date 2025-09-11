using Unity.Mathematics;
using UnityEngine;

public unsafe sealed class ForceFieldBlocker : MonoBehaviour
{

    Transform cachedTransform;

    [SerializeField]
    public ForceFieldLine[] forceFieldBlockers;

    private void Awake() => cachedTransform = transform;

    private void Update()
    {
        foreach (ForceFieldLine item in forceFieldBlockers) item.Register(cachedTransform);
    }


    [System.Serializable]
    public struct ForceFieldLine
    {
        public float2 aLocal;
        public float2 bLocal;

        public void Register(Transform ownerTransform)
        {
            if (!ProximityPixelationSystem.Singleton) return;
            Vector3 pointAInWorld = ownerTransform.TransformPoint(new Vector3(aLocal.x, aLocal.y));
            Vector3 pointBInWorld = ownerTransform.TransformPoint(new Vector3(bLocal.x, bLocal.y));

            float2 pointALocal = aLocal;
            float2 pointBLocal = bLocal;
            float2 pointAWorld = new float2(pointAInWorld.x, pointAInWorld.y);
            float2 pointBWorld = new float2(pointBInWorld.x, pointBInWorld.y);

            ProximityPixelationSystem.ForceFieldBlockerData forceFieldBlocker = new ProximityPixelationSystem.ForceFieldBlockerData
            {
                pointALocal = pointALocal,
                pointBLocal = pointBLocal,
                pointAWorld = pointAWorld,
                pointBWorld = pointBWorld,
                worldMin = math.min(pointAWorld, pointBWorld),
                worldMax = math.max(pointAWorld, pointBWorld),
            };

            ProximityPixelationSystem.Singleton.AddForceFieldBlocker(forceFieldBlocker);
        }
    }


}
