using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class TransformLockBehaviour : MonoBehaviour
{

    Transform cachedTransform;

    [SerializeField]
    TransformLock transformLock;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Awake() => cachedTransform = transform;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    private void FixedUpdate() => Lock();
    private void Update() => Lock();
    private void LateUpdate() => Lock();

    void Lock() => transformLock.ApplyLock(cachedTransform);

    [Serializable]
    struct TransformLock
    {
        [Header("Position locks")]
        [SerializeField] LockFlagsTR positionLocks;
        [SerializeField] float positionValueX;
        [SerializeField] float positionValueY;
        [SerializeField] float positionValueZ;

        [Header("Rotation locks")]
        [SerializeField] LockFlagsTR rotationLocks;
        [SerializeField] float rotationValueX;
        [SerializeField] float rotationValueY;
        [SerializeField] float rotationValueZ;

        [Header("Scale locks")]
        [SerializeField] LockFlagsS scaleLocks;
        [SerializeField] float scaleValueX;
        [SerializeField] float scaleValueY;
        [SerializeField] float scaleValueZ;

        public void ApplyLock(Transform t)
        {
            if ((positionLocks & (LockFlagsTR.X | LockFlagsTR.Y | LockFlagsTR.Z)) != 0)
            {
                bool world = (positionLocks & LockFlagsTR.World) != 0;
                Vector3 pos = world ? t.position : t.localPosition;

                pos.x = (positionLocks & LockFlagsTR.X) != 0 ? positionValueX : pos.x;
                pos.y = (positionLocks & LockFlagsTR.Y) != 0 ? positionValueY : pos.y;
                pos.z = (positionLocks & LockFlagsTR.Z) != 0 ? positionValueZ : pos.z;

                if (world) t.position = pos;
                else t.localPosition = pos;
            }

            if ((rotationLocks & (LockFlagsTR.X | LockFlagsTR.Y | LockFlagsTR.Z)) != 0)
            {
                bool world = (rotationLocks & LockFlagsTR.World) != 0;
                Vector3 euler = world ? t.eulerAngles : t.localEulerAngles;

                euler.x = (rotationLocks & LockFlagsTR.X) != 0 ? rotationValueX : euler.x;
                euler.y = (rotationLocks & LockFlagsTR.Y) != 0 ? rotationValueY : euler.y;
                euler.z = (rotationLocks & LockFlagsTR.Z) != 0 ? rotationValueZ : euler.z;

                if (world) t.rotation = Quaternion.Euler(euler);
                else t.localRotation = Quaternion.Euler(euler);
            }

            if ((scaleLocks & (LockFlagsS.X | LockFlagsS.Y | LockFlagsS.Z)) != 0)
            {
                Vector3 scale = t.localScale;

                scale.x = (scaleLocks & LockFlagsS.X) != 0 ? scaleValueX : scale.x;
                scale.y = (scaleLocks & LockFlagsS.Y) != 0 ? scaleValueY : scale.y;
                scale.z = (scaleLocks & LockFlagsS.Z) != 0 ? scaleValueZ : scale.z;

                t.localScale = scale;
            }
        }

        [Flags]
        enum LockFlagsTR : byte
        {
            None = 0,

            X = 1 << 0,
            Y = 1 << 1,
            Z = 1 << 2,

            World = 1 << 3,
        }

        [Flags]
        enum LockFlagsS : byte
        {
            None = 0,

            X = 1 << 0,
            Y = 1 << 1,
            Z = 1 << 2,
        }

    }
}
