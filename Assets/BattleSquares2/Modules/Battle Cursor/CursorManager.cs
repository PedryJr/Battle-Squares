using System;
using UnityEngine;

public sealed class CursorManager : MonoBehaviour
{

    public static CursorManager Singleton;

    [SerializeField] CursorVariant[] cursors;

    Transform cachedTransform;

    Vector3 lockPos;

    private void Awake()
    {
        cachedTransform = transform;
        lockPos = cachedTransform.position;
        UpdateSingleton();

        DisableUnityCursor();
        SpawnCursors();
    }

    private void Update()
    {
        cachedTransform.position = lockPos;
    }

    bool UpdateSingleton()
    {
        bool SingletonExists = Singleton;
        if (SingletonExists) Destroy(Singleton.gameObject);
        Singleton = this;
        return SingletonExists;
    }
    private void DisableUnityCursor() 
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }
    private void SpawnCursors()
    {
        for (int i = 0; i < cursors.Length; i++)
        {
            cursors[i].cursorObject = Instantiate(cursors[i].cursorObject, transform);
            cursors[i].cursorObject.HideCursor();
        }
        transform.SetParent(null, true);
    }

    public void UseCursor(CursorType cursorType)
    {
        foreach (CursorVariant item in cursors)
        {
            if (item.cursorObject.Type == cursorType) item.cursorObject.ShowCursor();
            else item.cursorObject.HideCursor();
        }
    }


    [Serializable]
    struct CursorVariant
    {
        [SerializeField]
        public ModularCursor cursorObject;

    }

    public enum CursorType
    {
        Default,
        LevelEditor
    }

}
