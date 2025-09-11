using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static ProximityPixelationSystem;

public sealed unsafe class ProximityPixel : MonoBehaviour
{
    public void GetProximitySensor(GridSpaceTileData* cStyleDataLocation)
    {
        *cStyleDataLocation = new GridSpaceTileData
        {
            originalTilePosition = new float2(transform.position.x, transform.position.y),
            calculatedTilePosition = new float2(transform.position.x, transform.position.y),
            tileZPosition = transform.position.z,
            originalTileZRotation = transform.rotation.eulerAngles.z,
            calculatedTileZRotation = transform.rotation.eulerAngles.z,
            originalTileScale = 1.001f,
            calculatedTileScale = 1.2f,
        };
        Destroy(gameObject);
    }
}