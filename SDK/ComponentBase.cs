using System;
using System.Numerics;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{

    public interface IDestroyableComponent
    {
        //internal void Destroy();
    }

    public abstract class ComponentBase : IComponent
    {
        internal IntegrationType integrationType = IntegrationType.None;
        internal Action<bool> _enableAction = (_) => { };
        internal Func<bool> _enableCheck = () => false;
        public bool enabled { get => _enableCheck(); set => _enableAction(value); }

        public ITransform transform { get; set; }
        public IGameObject gameObject { get; set; }
        object IComponent.nativeRunner { get; set; }
        object IComponent.nativeWrappedObject { get; set; }

        internal void SetReferences(IGameObject go, ITransform trans)
        {
            gameObject = go;
            transform = trans;
        }

        public virtual void OnAwake() { }
        public virtual void OnStart() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnLateUpdate() { }
        public virtual void OnDestroy() { }
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }

        T IComponentAccess.GetComponent<T>() => gameObject.GetComponent<T>();
        T[] IComponentAccess.GetComponents<T>() => gameObject.GetComponents<T>();
        bool IComponentAccess.TryGetComponent<T>(out T component) => gameObject.TryGetComponent<T>(out component);
        T IComponentAccess.AddComponent<T>() => gameObject.AddComponent<T>();
        void IComponentAccess.RemoveComponent<T>() => gameObject.RemoveComponent<T>();
        public void RemoveComponent(IComponent component) => gameObject.RemoveComponent(component);
    }

    internal enum IntegrationType
    {
        None = 0,
        RigidBody = 1,
        SpriteRenderer = 2,
    }

}