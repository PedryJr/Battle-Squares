using BattleSquaresSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using static AssetWrappers;
using static UnityVecToSystemVec;

public class ComponentDriver : MonoBehaviour
{
    internal bool lazyEnableFlag = false;
    public IComponent ModComponentInstance { get; set; }
    public void LazyAwake() => ModComponentInstance.OnAwake();
    public void LazyEnable() => ModComponentInstance.OnEnable();
    private void Start() => ModComponentInstance.OnStart();
    private void Update() => ModComponentInstance.OnUpdate(Time.deltaTime);
    private void FixedUpdate() => ModComponentInstance.OnFixedUpdate(Time.deltaTime);
    private void LateUpdate() => ModComponentInstance.OnLateUpdate(Time.deltaTime);
    private void OnEnable()
    {
        if (ModComponentInstance == null) lazyEnableFlag = true;
        else ModComponentInstance.OnEnable();
    }
    private void OnDisable() => ModComponentInstance.OnDisable();
    private void OnDestroy()
    {
        ModComponentInstance.OnDestroy();
        RegistryJanitor.RunCleanupCheck();
    }

    public void InitializeBuiltinWrapper<T>(T instance) where T : ComponentBase, IComponent
    {
        if (instance.integrationType == IntegrationType.None) return;
        if (instance.integrationType == IntegrationType.RigidBody) instance.nativeWrappedObject = new RigidBodyWrapper(this, instance);
        if (instance.integrationType == IntegrationType.SpriteRenderer) instance.nativeWrappedObject = new SpriteRendererWrapper(this, instance);
    }
}

public sealed unsafe class RegistryJanitor : MonoBehaviour
{
    private static int _capacity = 64;
    private static int _mask = _capacity - 1;
    private static int _count = 0;

    private static Entry* _entries;

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    private struct Entry
    {
        public IntPtr KeyPtr;
        public IntPtr ValuePtr;
        public int Hash;
    }

    static RegistryJanitor() => AllocateTable(_capacity);

    private static void AllocateTable(int size)
    {
        int bytes = size * sizeof(Entry);
        _entries = (Entry*)Marshal.AllocHGlobal(bytes);
        Unsafe.InitBlock(_entries, 0, (uint)bytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterGameObject(GameObject gameObject, GameObjectWrapper wrapper)
        => Insert(gameObject, wrapper);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RegisterTransformObject(Transform transform, TransformWrapper wrapper)
        => Insert(transform, wrapper);

    private static void Insert(object key, object value)
    {
        if (_count >= _capacity * 0.7f) Rehash();

        int hash = RuntimeHelpers.GetHashCode(key);
        int index = hash & _mask;

        while (_entries[index].KeyPtr != IntPtr.Zero)
        {
            index = (index + 1) & _mask;
        }

        _entries[index].KeyPtr = GCHandle.ToIntPtr(GCHandle.Alloc(key, GCHandleType.Weak));
        _entries[index].ValuePtr = GCHandle.ToIntPtr(GCHandle.Alloc(value, GCHandleType.Normal));
        _entries[index].Hash = hash;
        _count++;
    }

    public static bool TryGetValue(GameObject key, out GameObjectWrapper value)
    {
        if (key == null) { value = null; return false; }
        bool found = TryFind(key, out object result);
        value = (GameObjectWrapper)result;
        return found;
    }

    public static bool TryGetValue(Transform key, out TransformWrapper value)
    {
        if (key == null) { value = null; return false; }
        bool found = TryFind(key, out object result);
        value = (TransformWrapper)result;
        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryFind(object key, out object value)
    {
        int hash = RuntimeHelpers.GetHashCode(key);
        int index = hash & _mask;

        while (true)
        {
            Entry* e = &_entries[index];
            if (e->KeyPtr == IntPtr.Zero) break;

            if (e->Hash == hash)
            {
                GCHandle handle = GCHandle.FromIntPtr(e->KeyPtr);
                if (ReferenceEquals(handle.Target, key))
                {
                    value = GCHandle.FromIntPtr(e->ValuePtr).Target;
                    return true;
                }
            }
            index = (index + 1) & _mask;
        }

        value = null;
        return false;
    }

    public static void RunCleanupCheck()
    {
        if (_count == 0) return;
        int oldCap = _capacity;
        Entry* oldEntries = _entries;

        AllocateTable(oldCap);
        _count = 0;

        for (int i = 0; i < oldCap; i++)
        {
            Entry* e = &oldEntries[i];
            if (e->KeyPtr == IntPtr.Zero) continue;

            GCHandle keyHandle = GCHandle.FromIntPtr(e->KeyPtr);
            if (keyHandle.IsAllocated && keyHandle.Target != null) ReInsertAfterCleanup(e->KeyPtr, e->ValuePtr, e->Hash);
            else
            {
                if (keyHandle.IsAllocated) keyHandle.Free();
                GCHandle valHandle = GCHandle.FromIntPtr(e->ValuePtr);
                if (valHandle.IsAllocated) valHandle.Free();
            }
        }
        Marshal.FreeHGlobal((IntPtr)oldEntries);
    }

    private static void ReInsertAfterCleanup(IntPtr k, IntPtr v, int h)
    {
        int index = h & _mask;
        while (_entries[index].KeyPtr != IntPtr.Zero) index = (index + 1) & _mask;
        _entries[index].KeyPtr = k;
        _entries[index].ValuePtr = v;
        _entries[index].Hash = h;
        _count++;
    }

    private static void Rehash()
    {
        int oldCap = _capacity;
        Entry* oldEntries = _entries;

        _capacity <<= 1;
        _mask = _capacity - 1;
        AllocateTable(_capacity);
        _count = 0;

        for (int i = 0; i < oldCap; i++) if (oldEntries[i].KeyPtr != IntPtr.Zero) ReInsertAfterCleanup(oldEntries[i].KeyPtr, oldEntries[i].ValuePtr, oldEntries[i].Hash);
        Marshal.FreeHGlobal((IntPtr)oldEntries);
    }
}


public partial class GameObjectWrapper : IGameObject
{
    private readonly GameObject _unityGameObject;
    private readonly TransformWrapper _transformWrapper;

    private GameObjectWrapper(GameObject unityGameObject, TransformWrapper transformWrapper, bool register)
    {
        _unityGameObject = unityGameObject;
        _transformWrapper = transformWrapper;

        if (register)
        {
            RegistryJanitor.RegisterGameObject(unityGameObject, this);
            GameSideBridge.RegisterGameObject(this, unityGameObject);
        }
    }

    public static GameObjectWrapper GetOrCreate(GameObject unityGameObject, TransformWrapper existingTransformWrapper = null)
    {
        if (unityGameObject == null) return null;

        if (RegistryJanitor.TryGetValue(unityGameObject, out var existing)) return existing;

        TransformWrapper transformWrapper = existingTransformWrapper ?? TransformWrapper.GetOrCreate(unityGameObject.transform);
        GameObjectWrapper wrapper = new GameObjectWrapper(unityGameObject, transformWrapper, register: true);

        return wrapper;
    }

    private ComponentDriver[] GetDrivers() => _unityGameObject.GetComponents<ComponentDriver>();

    public string name
    {
        get => _unityGameObject.name;
        set => _unityGameObject.name = value;
    }

    public string tag
    {
        get => _unityGameObject.tag;
        set => _unityGameObject.tag = value;
    }

    public int layer
    {
        get => _unityGameObject.layer;
        set => _unityGameObject.layer = value;
    }

    public bool activeSelf => _unityGameObject.activeSelf;
    public bool activeInHierarchy => _unityGameObject.activeInHierarchy;
    public ITransform transform => _transformWrapper;
    public IGameObject gameObject => this;
    object IGameObject.nativeObject => _unityGameObject;

    public void SetActive(bool value) => _unityGameObject.SetActive(value);
    public bool CompareTag(string tag) => _unityGameObject.CompareTag(tag);

    T IComponentAccess.GetComponent<T>()
    {
        foreach (ComponentDriver runner in GetDrivers()) if (runner.ModComponentInstance is T t) return t;
        return null;
    }

    T[] IComponentAccess.GetComponents<T>()
    {
        ComponentDriver[] runners = GetDrivers();
        List<T> list = new List<T>();
        foreach (var driver in runners) if (driver.ModComponentInstance is T t) list.Add(t);
        return list.ToArray();
    }

    bool IComponentAccess.TryGetComponent<T>(out T component)
    {
        component = ((IComponentAccess)this).GetComponent<T>();
        return component != null;
    }

    T IComponentAccess.AddComponent<T>()
    {
        ComponentDriver driver = _unityGameObject.AddComponent<ComponentDriver>();
        T instance = new T();
        driver.ModComponentInstance = instance;
        instance._enableAction = (_) => { driver.enabled = _; };
        instance._enableCheck = () => driver.enabled;
        instance.SetReferences(gameObject, transform);
        GameSideBridge.RegisterComponent(instance, driver);
        driver.InitializeBuiltinWrapper(instance);

        driver.LazyAwake();
        if (driver.lazyEnableFlag) driver.LazyEnable();

        return instance;
    }

    void IComponentAccess.RemoveComponent<T>()
    {
        foreach (var driver in GetDrivers())
        {
            if (driver.ModComponentInstance is T)
            {
                GameSideBridge.ReleaseRegisteredComponent(driver.ModComponentInstance, driver);
                UnityEngine.Object.Destroy(driver);
                return;
            }
        }
    }

    public void RemoveComponent(ref IComponent component)
    {
        foreach (var driver in GetDrivers())
        {
            if (driver.ModComponentInstance == component)
            {
                GameSideBridge.ReleaseRegisteredComponent(component, driver);
                UnityEngine.Object.Destroy(driver);
                component = null;
                return;
            }
        }
    }

    public static GameObjectWrapper[] FindGameObjectsWithTag(string tag)
    {
        GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
        GameObjectWrapper[] wrappers = new GameObjectWrapper[found.Length];
        for (int i = 0; i < found.Length; i++) wrappers[i] = GetOrCreate(found[i]);
        return wrappers;
    }
}

public partial class TransformWrapper : ITransform
{
    private readonly Transform _unityTransform;
    private GameObjectWrapper _gameObjectWrapper;

    private TransformWrapper(Transform unityTransform, bool register)
    {
        _unityTransform = unityTransform;
        if (register) RegistryJanitor.RegisterTransformObject(unityTransform, this);
    }

    public static TransformWrapper GetOrCreate(Transform unityTransform)
    {
        if (unityTransform == null) return null;

        if (RegistryJanitor.TryGetValue(unityTransform, out var existing)) return existing;

        TransformWrapper wrapper = new TransformWrapper(unityTransform, register: true);
        wrapper._gameObjectWrapper = GameObjectWrapper.GetOrCreate(unityTransform.gameObject, wrapper);

        return wrapper;
    }

    public void InfectHierarchy(bool includeParents = true, bool includeChildren = true)
    {
        if (includeParents && _unityTransform.parent != null)
        {
            TransformWrapper parentWrapper = GetOrCreate(_unityTransform.parent);
            parentWrapper?.InfectHierarchy(includeParents: true, includeChildren: false);
        }

        if (includeChildren)
        {
            for (int i = 0; i < _unityTransform.childCount; i++)
            {
                TransformWrapper childWrapper = GetOrCreate(_unityTransform.GetChild(i));
                childWrapper?.InfectHierarchy(includeParents: false, includeChildren: true);
            }
        }
    }

    public IGameObject gameObject => _gameObjectWrapper;

    public System.Numerics.Vector3 position
    {
        get => cVec3(_unityTransform.position);
        set => _unityTransform.position = cVec3(value);
    }

    public System.Numerics.Vector3 localPosition
    {
        get => cVec3(_unityTransform.localPosition);
        set => _unityTransform.localPosition = cVec3(value);
    }

    public System.Numerics.Quaternion rotation
    {
        get => cQuat(_unityTransform.rotation);
        set => _unityTransform.rotation = cQuat(value);
    }

    public System.Numerics.Quaternion localRotation
    {
        get => cQuat(_unityTransform.localRotation);
        set => _unityTransform.localRotation = cQuat(value);
    }

    public System.Numerics.Vector3 localScale
    {
        get => cVec3(_unityTransform.localScale);
        set => _unityTransform.localScale = cVec3(value);
    }

    public System.Numerics.Vector3 eulerAngles
    {
        get => cVec3(_unityTransform.eulerAngles);
        set => _unityTransform.eulerAngles = cVec3(value);
    }

    public System.Numerics.Vector3 localEulerAngles
    {
        get => cVec3(_unityTransform.localEulerAngles);
        set => _unityTransform.localEulerAngles = cVec3(value);
    }

    public System.Numerics.Vector3 forward
    {
        get => cVec3(_unityTransform.forward);
        set => _unityTransform.forward = cVec3(value);
    }

    public System.Numerics.Vector3 right
    {
        get => cVec3(_unityTransform.right);
        set => _unityTransform.right = cVec3(value);
    }

    public System.Numerics.Vector3 up
    {
        get => cVec3(_unityTransform.up);
        set => _unityTransform.up = cVec3(value);
    }

    public int childCount => _unityTransform.childCount;

    public ITransform parent
    {
        get => _unityTransform.parent == null ? null : GetOrCreate(_unityTransform.parent);
        set
        {
            if (value is TransformWrapper wrapper) _unityTransform.parent = wrapper._unityTransform;
            else if (value == null)  _unityTransform.parent = null;
        }
    }

    public ITransform transform => this;

    public void Translate(System.Numerics.Vector3 t) =>
        _unityTransform.Translate(cVec3(t));

    public void Translate(System.Numerics.Vector3 t, BattleSquaresSDK.Space s) =>
        _unityTransform.Translate(cVec3(t), (UnityEngine.Space)(int)s);

    public void Rotate(System.Numerics.Vector3 e, BattleSquaresSDK.Space s) =>
        _unityTransform.Rotate(cVec3(e), (UnityEngine.Space)(int)s);

    public void Rotate(System.Numerics.Vector3 e) =>
        _unityTransform.Rotate(cVec3(e));

    public void RotateAround(System.Numerics.Vector3 p, System.Numerics.Vector3 a, float ang) =>
        _unityTransform.RotateAround(cVec3(p), cVec3(a), ang);

    public void LookAt(System.Numerics.Vector3 w) =>
        _unityTransform.LookAt(cVec3(w));

    public void LookAt(System.Numerics.Vector3 w, System.Numerics.Vector3 up) =>
        _unityTransform.LookAt(cVec3(w), cVec3(up));

    public void LookAt(ITransform target)
    {
        if (target != null)
            _unityTransform.LookAt(cVec3(target.position));
    }

    public ITransform GetChild(int index)
    {
        if (index < 0 || index >= _unityTransform.childCount)
            return null;
        return GetOrCreate(_unityTransform.GetChild(index));
    }

    public ITransform Find(string name)
    {
        Transform found = _unityTransform.Find(name);
        return found ? GetOrCreate(found) : null;
    }

    public ITransform[] GetChildren()
    {
        ITransform[] result = new ITransform[_unityTransform.childCount];
        for (int i = 0; i < _unityTransform.childCount; i++) result[i] = GetOrCreate(_unityTransform.GetChild(i));
        return result;
    }

    T IComponentAccess.GetComponent<T>() => gameObject.GetComponent<T>();
    T[] IComponentAccess.GetComponents<T>() => gameObject.GetComponents<T>();
    bool IComponentAccess.TryGetComponent<T>(out T component) => gameObject.TryGetComponent<T>(out component);
    T IComponentAccess.AddComponent<T>() => gameObject.AddComponent<T>();
    void IComponentAccess.RemoveComponent<T>() => gameObject.RemoveComponent<T>();
    public void RemoveComponent(IComponent component) => gameObject.RemoveComponent(component);
}

public static class GameSideBridge
{ 
    private static readonly Dictionary<IGameObject, GameObject> _gameObjects = new();
    private static readonly Dictionary<IComponent, ComponentDriver> _components = new(); 
    internal static void RegisterGameObject(IGameObject key, GameObject value) => _gameObjects[key] = value;
    internal static void RegisterComponent(IComponent key, ComponentDriver value) => _components[key] = value;
    internal static void ReleaseRegisteredComponent(IComponent key, ComponentDriver value) => _components.Remove(key);
    public static void InitializeBridge()
    {
        SdkGameObject.OnCreateGameObject = OnCreateGameObject;
        SdkGameObject.OnDontDestroyOnLoadOBJ = OnDontDestroyOnLoadOBJ;
        SdkGameObject.OnDestroyGameObject = OnDestroyGameObject;
        SdkGameObject.OnDestroyComponent = OnDestroyComponent;

        AssetCreator.createTextureDelegate = CreateTextureImpl;
        AssetCreator.createSpriteDelegate = CreateSpriteImpl;
        AssetCreator.createMaterialDelegate = CreateMaterialImpl;
        AssetCreator.createShaderDelegate = CreateShaderImpl;
        AssetCreator.createMeshDelegate = CreateMeshImpl;

        //This is the start of being able to get components already in use :>
        //FindObjectsByTypeFromSDK();

    }

    private static ITransform[] FindObjectsByTypeFromSDK()
    {
        Transform[] unityTransforms = Transform.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        ITransform[] transformWrapper = new TransformWrapper[unityTransforms.Length];
        for (int i = 0; i < transformWrapper.Length; i++)
        {
            transformWrapper[i] = TransformWrapper.GetOrCreate(unityTransforms[i]);
            transformWrapper[i].AddComponent<SpriteRendererComponent>();
        }
        return transformWrapper;
    }

    private static ITexture2D CreateTextureImpl(string pngPath, BattleSquaresSDK.TextureWrapMode wrapMode, BattleSquaresSDK.FilterMode filterMode)
    {
        Texture2D tex = LoadTextureFromFile(pngPath); // implement your PNG loader
        tex.wrapMode = wrapMode switch
        {
            BattleSquaresSDK.TextureWrapMode.Repeat => UnityEngine.TextureWrapMode.Repeat,
            BattleSquaresSDK.TextureWrapMode.Clamp => UnityEngine.TextureWrapMode.Clamp,
            BattleSquaresSDK.TextureWrapMode.Mirror => UnityEngine.TextureWrapMode.Mirror,
            BattleSquaresSDK.TextureWrapMode.MirrorOnce => UnityEngine.TextureWrapMode.MirrorOnce,
            _ => UnityEngine.TextureWrapMode.Repeat
        };
        tex.filterMode = filterMode switch
        {
            BattleSquaresSDK.FilterMode.Point => UnityEngine.FilterMode.Point,
            BattleSquaresSDK.FilterMode.Bilinear => UnityEngine.FilterMode.Bilinear,
            BattleSquaresSDK.FilterMode.Trilinear => UnityEngine.FilterMode.Trilinear,
            _ => UnityEngine.FilterMode.Point
        };
        return new Texture2DWrapper(tex);
    }

    private static ISprite CreateSpriteImpl(ITexture2D texture, int pixelsPerUnit)
    {
        if (texture is Texture2DWrapper wrapper)
        {
            Sprite sprite = Sprite.Create(wrapper.texture,
                new Rect(0, 0, wrapper.texture.width, wrapper.texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            return new SpriteWrapper(sprite);
        }
        return null;
    }

    private static IMaterial CreateMaterialImpl(IMaterial source = null, IShader shader = null)
    {
        Material mat = null;
        if (source is MaterialWrapper matWrapper) mat = UnityEngine.Object.Instantiate(matWrapper.material);
        Shader unityShader = shader is ShaderWrapper sw ? sw.shader : Shader.Find("Sprites/Default");
        return new MaterialWrapper(unityShader, mat);
    }

    private static IShader CreateShaderImpl(string assetBundlePath, string shaderName)
    {
        AssetBundle ab = AssetBundle.LoadFromFile(assetBundlePath);
        if (ab == null) throw new Exception($"Failed to load AssetBundle at {assetBundlePath}");

        Shader shader = ab.LoadAsset<Shader>(shaderName);

        if (shader == null)
        {
            Material mat = ab.LoadAsset<Material>(shaderName);
            if (mat != null)
                shader = mat.shader;
        }

        if (shader == null)
        {
            Debug.LogWarning("Available assets in bundle:");
            foreach (var name in ab.GetAllAssetNames())
            {
                Debug.Log(name);
                Material mat = ab.LoadAsset<Material>(name);
                if (mat != null) shader = mat.shader;
                if (shader != null)
                {
                    Debug.Log("Shader indirection found");
                    return new ShaderWrapper(shader);
                }
            }

            throw new Exception($"Shader '{shaderName}' not found in AssetBundle");
        }

        return new ShaderWrapper(shader);
    }

    private static IMesh CreateMeshImpl()
    {
        return new MeshWrapper();
    }

    private static Texture2D LoadTextureFromFile(string path)
    {
        byte[] data = System.IO.File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(data);
        return tex;
    }



    public static void OnDontDestroyOnLoadOBJ(IGameObject gameObject)
    {
        GameObject.DontDestroyOnLoad(gameObject.nativeObject as GameObject);
    }

    public static GameObjectWrapper OnCreateGameObject(string name)
    {
        GameObject unityGO = new GameObject(name);
        return GameObjectWrapper.GetOrCreate(unityGO);
    }
    public static void OnDestroyComponent(ref IComponent component)
    {
        if (!_components.TryGetValue(component, out var runner)) return;
        _components.Remove(component);
        UnityEngine.Object.Destroy(runner);
        component = null;
    }
    public static void OnDestroyGameObject(ref IGameObject gameObject)
    {
        if (!_gameObjects.TryGetValue(gameObject, out var go)) return;
        _gameObjects.Remove(gameObject);
        GameObject.Destroy(go);
        gameObject = null;
    }
}