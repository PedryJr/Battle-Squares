
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public sealed class BackdropBehaviour : MonoBehaviour
{

    [SerializeField]
    float _fallofExponential;
    [SerializeField]
    float _othersInfluenceOverMe;
    [SerializeField]
    float _maxIntensity;

    public static BackdropBehaviour Singleton;
    public int ActiveProximityColorCount => proximityColors.Count;

    ComputeBuffer proximityColorBuffer;
    ComputeBuffer proximityPositionBuffer;
    ComputeBuffer proximityRadiusBuffer;
    ComputeBuffer proximitySaturationBoosts;
    
    List<float3> proximityColors = new List<float3>();
    List<float2> proximityPositions = new List<float2>();
    List<float> proximityRadiuses = new List<float>();
    List<float> proximitySaturationBoosters = new List<float>();

    [SerializeField]
    Material backdropMaterial;

    const int floatSize = sizeof(float);
    const int float3Size = floatSize * 3;
    const int float2Size = floatSize * 2;
    const int float1Size = floatSize;

    private void Awake()
    {
        Singleton = this;
        GetComponent<SpriteRenderer>().material = backdropMaterial;
    }

    private void OnDestroy()
    {
        Singleton = null;
        ReleaseBuffers();
    }

    private void Update()
    {

        backdropMaterial.SetFloat("_ProximityCount", ActiveProximityColorCount);
        backdropMaterial.SetFloat("_fallofExponential", _fallofExponential);
        backdropMaterial.SetFloat("_othersInfluenceOverMe", ProximityPixelationSystem.Singleton ? _othersInfluenceOverMe : 0);
        backdropMaterial.SetFloat("_maxIntensity", _maxIntensity);
        if (ActiveProximityColorCount > 0) RunGpuProximityColorUpload();

    }

    void RunGpuProximityColorUpload()
    {

        InitializeBuffers();
        UploadBufferData();
        UploadBufferToGPU();
        BufferCleanup();
    }

    void InitializeBuffers()
    {
        EnsureBuffer(ref proximityColorBuffer, proximityColors.Count, float3Size);
        EnsureBuffer(ref proximityPositionBuffer, proximityPositions.Count, float2Size);
        EnsureBuffer(ref proximityRadiusBuffer, proximityRadiuses.Count, float1Size);
        EnsureBuffer(ref proximitySaturationBoosts, proximityRadiuses.Count, float1Size);
    }

    void EnsureBuffer(ref ComputeBuffer buffer, int count, int stride)
    {
        if (buffer == null || !buffer.IsValid() || buffer.count != count)
        {
            buffer?.Release();
            if (count > 0) buffer = new ComputeBuffer(count, stride, ComputeBufferType.Structured);
        }
    }


    void ReleaseBuffers()
    {
        proximityColorBuffer?.Release();
        proximityPositionBuffer?.Release();
        proximityRadiusBuffer?.Release();
        proximitySaturationBoosts?.Release();
    }


    void UploadBufferData()
    {
        proximityColorBuffer.SetData(proximityColors);
        proximityPositionBuffer.SetData(proximityPositions);
        proximityRadiusBuffer.SetData(proximityRadiuses);
        proximitySaturationBoosts.SetData(proximitySaturationBoosters);
    }

    void UploadBufferToGPU()
    {
        backdropMaterial.SetBuffer("_ProximityColors", proximityColorBuffer);
        backdropMaterial.SetBuffer("_ProximityPositions", proximityPositionBuffer);
        backdropMaterial.SetBuffer("_ProximityRadiuses", proximityRadiusBuffer);
        backdropMaterial.SetBuffer("_ProximitySaturationBoosts", proximitySaturationBoosts);
    }

    void BufferCleanup()
    {
        proximityColors.Clear();
        proximityPositions.Clear();
        proximityRadiuses.Clear();
        proximitySaturationBoosters.Clear();
    }

    public void AddProximityColor(Color proximityColor, Vector2 proximityPosition, float proximityRadius, float saturationBoost)
    {

        proximityColors.Add(new float3 { x = proximityColor.r, y = proximityColor.g, z = proximityColor.b });
        proximityPositions.Add((float2)proximityPosition);
        proximityRadiuses.Add(proximityRadius);
        proximitySaturationBoosters.Add(saturationBoost);
    }

}
