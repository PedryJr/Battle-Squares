using System;
using UnityEngine;

public class LevelTransMod : MonoBehaviour
{

    Transform cachedTransform;

    public bool CanUse;

    bool hasModifiedState = false;

    public Action<Vector2> rightClickCallback;
    public Action<Vector2> rightHoldCallback;
    public Action<Vector2> leftClickCallback;
    public Action<Vector2> leftHoldCallback;

    public Action<bool> releaseCallback;

    void ModifyTracker(Vector2 placeHolder) { hasModifiedState = true; }
    void ReleaseTracker(bool placeHolder) { }

    protected virtual void InheritStart() { }
    protected virtual void InheritDestroy() { }

    private void Start()
    {
        cachedTransform = transform;
        rightClickCallback += ModifyTracker;
        rightHoldCallback += ModifyTracker;
        leftClickCallback += ModifyTracker;
        leftHoldCallback += ModifyTracker;
        releaseCallback += ReleaseTracker;
        AddToManager();
        InheritStart();
    }

    private void OnDestroy()
    {
        rightClickCallback -= ModifyTracker;
        rightHoldCallback -= ModifyTracker;
        leftClickCallback -= ModifyTracker;
        leftHoldCallback -= ModifyTracker;
        releaseCallback -= ReleaseTracker;
        RemoveFromManager();
        InheritDestroy();
    }

    void AddToManager()
    {
        if (TransmodManager.Singleton) TransmodManager.Singleton.AddTransmod(this);
    }
    void RemoveFromManager()
    {
        if (TransmodManager.Singleton) TransmodManager.Singleton.RemoveTransmod(this);
    }

    public void SetPosition(Vector2 newPosition)
    {
        Vector3 withZDepth = newPosition;
        withZDepth.z = cachedTransform.position.z;
        cachedTransform.position = withZDepth;
    }

    public Vector2 GetPosition() => cachedTransform.position;

    public void RunRightClickCallbackCLICK(Vector2 worldSpacePosition) => rightClickCallback.Invoke(worldSpacePosition);
    public void RunRightClickCallbackHOLD(Vector2 worldSpacePosition) => rightHoldCallback.Invoke(worldSpacePosition);
    public void RunLeftClickCallbackCLICK(Vector2 worldSpacePosition) => leftClickCallback.Invoke(worldSpacePosition);
    public void RunLeftClickCallbackHOLD(Vector2 worldSpacePosition) => leftHoldCallback.Invoke(worldSpacePosition);

    public void RunReleaseCallback()
    {
        releaseCallback.Invoke(hasModifiedState);
        hasModifiedState = false;
    }

}
