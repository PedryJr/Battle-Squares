using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FeedTransformToImageShader : MonoBehaviour
{

    [SerializeField]
    bool updateInEditMode = false;

    [SerializeField]
    private Image img;

    [SerializeField] private TransformShaderFeed[] transformFeeds;
    [SerializeField] private RectTransformShaderFeed[] rectTransformFeeds;
    [SerializeField] private TimeShaderFeed[] timeShaderFeeds;
    [SerializeField] private FloatShaderFeed[] floatShaderFeeds;
    [SerializeField] private VectorShaderFeed[] vectorShaderFeeds;

    private void OnDrawGizmos()
    {
        if(updateInEditMode)
        {
            if (!img) img = GetComponent<Image>();
            Update();
        }
    }

    private void Awake()
    {
        if (!img) img = GetComponent<Image>();
    }

    private void Update()
    {

        if (!img.material) return;

        if(transformFeeds != null) for (int i = 0; i < transformFeeds.Length; i++) transformFeeds[i].FeedMaterial(img.material);

        if (rectTransformFeeds != null) for (int i = 0; i < rectTransformFeeds.Length; i++) rectTransformFeeds[i].FeedMaterial(img.material);

        if (timeShaderFeeds != null) for (int i = 0; i < timeShaderFeeds.Length; i++) timeShaderFeeds[i].FeedMaterial(img.material);

        if (floatShaderFeeds != null) for (int i = 0; i < floatShaderFeeds.Length; i++) floatShaderFeeds[i].FeedMaterial(img.material);

        if (vectorShaderFeeds != null) for (int i = 0; i < vectorShaderFeeds.Length; i++) vectorShaderFeeds[i].FeedMaterial(img.material);
    }
}
[Serializable]
public struct VectorShaderFeed
{
    [SerializeField]
    private bool enabled;

    [SerializeField]
    [Tooltip("Shader property name")]
    private string shaderProperty;

    [SerializeField]
    private Vector4 value;

    public void FeedMaterial(Material mat)
    {
        if (!enabled) return;
        if (!mat) return;

        mat.SetVector(shaderProperty, value);
    }
}
[Serializable]
public struct FloatShaderFeed
{
    [SerializeField]
    private bool enabled;

    [SerializeField]
    [Tooltip("Shader property name")]
    private string shaderProperty;

    [SerializeField]
    private float value;

    public void FeedMaterial(Material mat)
    {
        if (!enabled) return;
        if (!mat) return;

        mat.SetFloat(shaderProperty, value);
    }
}
[Serializable]
public struct TimeShaderFeed
{
    [SerializeField]
    private bool enabled;

    [SerializeField]
    private ShaderCustomFeedType feedType;

    [SerializeField]
    [Tooltip("Shader property name")]
    private string shaderProperty;

    private float lifeTime;

    public void FeedMaterial(Material mat)
    {
        lifeTime += Time.deltaTime;

        if (!enabled) return;
        if (!mat) return;

        Camera cam = Camera.main;

        switch (feedType)
        {

            case ShaderCustomFeedType.LifeTime: mat.SetFloat(shaderProperty, lifeTime); break;
            case ShaderCustomFeedType.DeltaTime: mat.SetFloat(shaderProperty, Time.deltaTime); break;
            case ShaderCustomFeedType.SinLifeTime: mat.SetFloat(shaderProperty, Mathf.Sin(lifeTime)); break;
            case ShaderCustomFeedType.CosLifeTime: mat.SetFloat(shaderProperty, Mathf.Cos(lifeTime)); break;
        }
    }
}
[Serializable]
public struct TransformShaderFeed
{
    [SerializeField]
    private bool enabled;

    [SerializeField]
    private ShaderFeedType feedType;

    [SerializeField]
    [Tooltip("Shader property name")]
    private string shaderProperty;

    [SerializeField]
    [Tooltip("Transform reference")]
    private Transform transform;

    public void FeedMaterial(Material mat)
    {
            if (!enabled) return;
            if (!transform) return;
            if (!mat) return;

            Camera cam = Camera.main;

        switch (feedType)
        {
            case ShaderFeedType.ModelMatrix: mat.SetMatrix(shaderProperty, transform.localToWorldMatrix); break;
            case ShaderFeedType.InverseModelMatrix: mat.SetMatrix(shaderProperty, transform.worldToLocalMatrix); break;
            case ShaderFeedType.ViewMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.worldToCameraMatrix); break;
            case ShaderFeedType.InverseViewMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.cameraToWorldMatrix); break;
            case ShaderFeedType.ProjectionMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.projectionMatrix); break;
            case ShaderFeedType.ViewProjectionMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.projectionMatrix * cam.worldToCameraMatrix); break;
            case ShaderFeedType.InverseProjectionMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.projectionMatrix.inverse); break;

            case ShaderFeedType.Position: mat.SetVector(shaderProperty, transform.position); break;
            case ShaderFeedType.LocalPosition: mat.SetVector(shaderProperty, transform.localPosition); break;

            case ShaderFeedType.RotationQuat: mat.SetVector(shaderProperty, new Vector4(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w)); break;
            case ShaderFeedType.RotationEuler: mat.SetVector(shaderProperty, transform.eulerAngles); break;
            case ShaderFeedType.LocalRotationQuat: mat.SetVector(shaderProperty, new Vector4(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w)); break;
            case ShaderFeedType.LocalRotationEuler: mat.SetVector(shaderProperty, transform.localEulerAngles); break;

            case ShaderFeedType.Scale: mat.SetVector(shaderProperty, transform.localScale); break;
            case ShaderFeedType.LossyScale: mat.SetVector(shaderProperty, transform.lossyScale); break;

            case ShaderFeedType.Forward: mat.SetVector(shaderProperty, transform.forward); break;
            case ShaderFeedType.Right: mat.SetVector(shaderProperty, transform.right); break;
            case ShaderFeedType.Up: mat.SetVector(shaderProperty, transform.up); break;

            case ShaderFeedType.CameraPosition: if (cam) mat.SetVector(shaderProperty, cam.transform.position); break;
            case ShaderFeedType.CameraForward: if (cam) mat.SetVector(shaderProperty, cam.transform.forward); break;
            case ShaderFeedType.CameraProjectionParams: if (cam) mat.SetVector(shaderProperty, new Vector4(cam.nearClipPlane, cam.farClipPlane, cam.fieldOfView, cam.aspect)); break;
            case ShaderFeedType.DistanceToCamera: if (cam) mat.SetFloat(shaderProperty, Vector3.Distance(transform.position, cam.transform.position)); break;
            case ShaderFeedType.DirectionToCamera: if (cam) mat.SetVector(shaderProperty, (cam.transform.position - transform.position).normalized); break;
        }
    }
    public enum ShaderFeedType
    {
        ModelMatrix,
        ViewMatrix,
        ProjectionMatrix,
        ViewProjectionMatrix,
        InverseModelMatrix,
        InverseViewMatrix,
        InverseProjectionMatrix,
        InverseViewProjectionMatrix,

        Position,
        LocalPosition,

        RotationQuat,
        RotationEuler,
        LocalRotationQuat,
        LocalRotationEuler,

        Scale,
        LossyScale,

        Forward,
        Right,
        Up,

        CameraPosition,
        CameraForward,
        CameraProjectionParams,
        DistanceToCamera,
        DirectionToCamera,
    }
}

[Serializable]
public struct RectTransformShaderFeed
{
    [SerializeField]
    private bool enabled;

    [SerializeField]
    private ShaderFeedType feedType;

    [SerializeField]
    [Tooltip("Shader property name")]
    private string shaderProperty;

    [SerializeField]
    [Tooltip("Transform reference")]
    private RectTransform transform;

    private float lifeTime;

    public void FeedMaterial(Material mat)
    {
        if (!enabled) return;
        if (!transform) return;
        if (!mat) return;
        Camera cam = Camera.main;

        switch (feedType)
        {
            case ShaderFeedType.ModelMatrix: mat.SetMatrix(shaderProperty, transform.localToWorldMatrix); break;
            case ShaderFeedType.InverseModelMatrix: mat.SetMatrix(shaderProperty, transform.worldToLocalMatrix); break;
            case ShaderFeedType.ViewMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.worldToCameraMatrix); break;
            case ShaderFeedType.InverseViewMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.cameraToWorldMatrix); break;
            case ShaderFeedType.ProjectionMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.projectionMatrix); break;
            case ShaderFeedType.ViewProjectionMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.projectionMatrix * cam.worldToCameraMatrix); break;
            case ShaderFeedType.InverseProjectionMatrix: if (cam) mat.SetMatrix(shaderProperty, cam.projectionMatrix.inverse); break;

            case ShaderFeedType.Position: mat.SetVector(shaderProperty, transform.position); break;
            case ShaderFeedType.LocalPosition: mat.SetVector(shaderProperty, transform.localPosition); break;
            case ShaderFeedType.AnchoredPosition: mat.SetVector(shaderProperty, transform.anchoredPosition); break;

            case ShaderFeedType.RotationQuat: mat.SetVector(shaderProperty, new Vector4(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w)); break;
            case ShaderFeedType.RotationEuler: mat.SetVector(shaderProperty, transform.eulerAngles); break;
            case ShaderFeedType.LocalRotationQuat: mat.SetVector(shaderProperty, new Vector4(transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w)); break;
            case ShaderFeedType.LocalRotationEuler: mat.SetVector(shaderProperty, transform.localEulerAngles); break;

            case ShaderFeedType.Scale: mat.SetVector(shaderProperty, transform.localScale); break;
            case ShaderFeedType.LossyScale: mat.SetVector(shaderProperty, transform.lossyScale); break;
            case ShaderFeedType.RectScale: mat.SetVector(shaderProperty, new Vector4(transform.rect.width, transform.rect.height)); break;
            case ShaderFeedType.RectWidth: mat.SetFloat(shaderProperty, transform.rect.width); break;
            case ShaderFeedType.RectHeigh: mat.SetFloat(shaderProperty, transform.rect.height); break;

            case ShaderFeedType.Forward: mat.SetVector(shaderProperty, transform.forward); break;
            case ShaderFeedType.Right: mat.SetVector(shaderProperty, transform.right); break;
            case ShaderFeedType.Up: mat.SetVector(shaderProperty, transform.up); break;

            case ShaderFeedType.CameraPosition: if (cam) mat.SetVector(shaderProperty, cam.transform.position); break;
            case ShaderFeedType.CameraForward: if (cam) mat.SetVector(shaderProperty, cam.transform.forward); break;
            case ShaderFeedType.CameraProjectionParams: if (cam) mat.SetVector(shaderProperty, new Vector4(cam.nearClipPlane, cam.farClipPlane, cam.fieldOfView, cam.aspect)); break;
            case ShaderFeedType.DistanceToCamera: if (cam) mat.SetFloat(shaderProperty, Vector3.Distance(transform.position, cam.transform.position)); break;
            case ShaderFeedType.DirectionToCamera: if (cam) mat.SetVector(shaderProperty, (cam.transform.position - transform.position).normalized); break;
        }
    }

    public enum ShaderFeedType
    {
        ModelMatrix,
        ViewMatrix,
        ProjectionMatrix,
        ViewProjectionMatrix,
        InverseModelMatrix,
        InverseViewMatrix,
        InverseProjectionMatrix,
        InverseViewProjectionMatrix,

        Position,
        LocalPosition,
        AnchoredPosition,

        RotationQuat,
        RotationEuler,
        LocalRotationQuat,
        LocalRotationEuler,

        Scale,
        LossyScale,
        RectScale, 
        RectWidth, 
        RectHeigh, 

        Forward,
        Right,
        Up,

        CameraPosition,
        CameraForward,
        CameraProjectionParams,
        DistanceToCamera,
        DirectionToCamera,
    }
}

public enum ShaderCustomFeedType
{
    LifeTime,
    DeltaTime,
    SinLifeTime,
    CosLifeTime,
}