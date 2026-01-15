using System;
using System.Numerics;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{
    public interface IComponentAccess
    {
        ITransform transform { get; }
        IGameObject gameObject { get; }
        T GetComponent<T>() where T : ComponentBase, IComponent;
        T[] GetComponents<T>() where T : ComponentBase, IComponent;
        bool TryGetComponent<T>(out T component) where T : ComponentBase, IComponent;
        T AddComponent<T>() where T : ComponentBase, IComponent, new();
        void RemoveComponent<T>() where T : ComponentBase, IComponent;
        void RemoveComponent(IComponent component);
    }

    public interface IComponent : IComponentAccess
    {
        internal object nativeRunner { get; set; }
        internal object nativeWrappedObject { get; set; }
        bool enabled { get; set; }
        void OnAwake();
        void OnStart();
        void OnUpdate();
        void OnFixedUpdate();
        void OnLateUpdate();
        void OnDestroy();
        void OnEnable();
        void OnDisable();
    }

    public interface ITransform : IComponentAccess
    {
        Vector3 position { get; set; }
        Vector3 localPosition { get; set; }
        Quaternion rotation { get; set; }
        Quaternion localRotation { get; set; }
        Vector3 localScale { get; set; }
        Vector3 eulerAngles { get; set; }
        Vector3 localEulerAngles { get; set; }
        Vector3 forward { get; set; }
        Vector3 right { get; set; }
        Vector3 up { get; set; }

        ITransform parent { get; set; }
        int childCount { get; }

        void Translate(Vector3 translation);
        void Translate(Vector3 translation, BattleSquaresSDK.Space relativeTo);
        void Rotate(Vector3 eulerAngles);
        void Rotate(Vector3 eulerAngles, BattleSquaresSDK.Space relativeTo);
        void RotateAround(Vector3 point, Vector3 axis, float angle);
        void LookAt(Vector3 worldPosition);
        void LookAt(Vector3 worldPosition, Vector3 up);
        void LookAt(ITransform target);

        ITransform GetChild(int index);
        ITransform Find(string name);
        ITransform[] GetChildren();
    }

    public interface IGameObject : IComponentAccess
    {
        internal object nativeObject { get; }
        string name { get; set; }
        string tag { get; set; }
        int layer { get; set; }
        bool activeSelf { get; }
        bool activeInHierarchy { get; }

        void SetActive(bool value);
        bool CompareTag(string tag);
    }
    public enum Space { World = 0, Self = 1 }
}