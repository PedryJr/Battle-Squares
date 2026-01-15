using System.Numerics;

namespace BattleSquaresSDK
{
    public abstract class ModBase
    {
        public abstract string Name { get; }

        public abstract void OnLoad(IModContext context);
        public virtual void OnLateUpdate(float deltaTime) { }
        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnFixedUpdate(float deltaTime) { }
        public abstract void OnUnload();
    }

    public static class SdkGameObject
    {
        //internal delegate ITransform[] Find

        internal delegate IGameObject OnCreateGameObjectEvent(string name);
        internal delegate void OnDestroyGameObjectEvent(ref IGameObject IGameObject);
        internal delegate void OnDestroyComponentEvent(ref IComponent IComponent);
        internal delegate void OnDontDestroyOnLoadOBJEvent(IGameObject IGameObject);


        internal static OnCreateGameObjectEvent OnCreateGameObject;
        internal static OnDestroyGameObjectEvent OnDestroyGameObject;
        internal static OnDestroyComponentEvent OnDestroyComponent;
        internal static OnDontDestroyOnLoadOBJEvent OnDontDestroyOnLoadOBJ;

        public static IGameObject CreateGameObject(string name = "New Object") => OnCreateGameObject(name);
        public static void DontDestroyOnLoad(IGameObject IGameObject) => OnDontDestroyOnLoadOBJ(IGameObject);
        public static void Destroy(ref IGameObject obj) => OnDestroyGameObject(ref obj);
        public static void Destroy(ref IComponent component) => OnDestroyComponent(ref component);
    }
}
