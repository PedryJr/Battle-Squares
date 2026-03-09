using BattleSquaresSDK;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static AssetWrappers;
using static UnityVecToSystemVec;

public static class ModAssetDatabase
{
    private static readonly Dictionary<string, ITexture2D> _textureCache = new();
    private static readonly Dictionary<string, ISprite> _spriteCache = new();
    private static readonly Dictionary<string, IMaterial> _materialCache = new();

}

public static class WrapperHelpers
{

    private static readonly Dictionary<string, Texture2D> _textureCache = new();

}

public class RigidBodyWrapper : IRigidBody
{

    Rigidbody2D rigidBody2D;
    public RigidBodyWrapper(ComponentDriver driver, object instance)
    {
        rigidBody2D = driver.gameObject.AddComponent<Rigidbody2D>();
        driver.ModComponentInstance.nativeWrappedObject = rigidBody2D;
        (instance as RigidBodyComponent).component = this;
    }
    public void OnDestroy() => UnityEngine.Object.Destroy(rigidBody2D);
    public System.Numerics.Vector2 position { get => cVec2(rigidBody2D.position); set =>  rigidBody2D.position = cVec2(value); }
}

public class SpriteRendererWrapper : ISpriteRenderer
{
    readonly SpriteRenderer spriteRenderer;

    public SpriteRendererWrapper(ComponentDriver driver, object instance)
    {
        spriteRenderer = driver.gameObject.AddComponent<SpriteRenderer>();
        driver.ModComponentInstance.nativeWrappedObject = spriteRenderer;
        (instance as SpriteRendererComponent).component = this;
    }

    public void OnDestroy()
    {
        if (spriteRenderer != null) UnityEngine.Object.Destroy(spriteRenderer);
    }


    public void SetSprite(ISprite iSprite)
    {
        if (iSprite is SpriteWrapper spriteW) spriteRenderer.sprite = spriteW.sprite;
    }

    public ISprite GetSprite()
    {
        if (spriteRenderer.sprite == null) return null;
        return new SpriteWrapper(spriteRenderer.sprite);
    }


    public IMaterial Material
    {
        get => spriteRenderer.material != null ? new MaterialWrapper(spriteRenderer.material) : null;
        set
        {
            if (value is MaterialWrapper wrapper)
                spriteRenderer.material = wrapper.material;
        }
    }

    public IMaterial SharedMaterial
    {
        get => spriteRenderer.sharedMaterial != null ? new MaterialWrapper(spriteRenderer.sharedMaterial) : null;
        set
        {
            if (value is MaterialWrapper wrapper) spriteRenderer.sharedMaterial = wrapper.material;
        }
    }

    public System.Numerics.Vector4 Color
    {
        get => cVec4(spriteRenderer.color);
        set => spriteRenderer.color = cVec4(value);
    }


    public int SortingOrder
    {
        get => spriteRenderer.sortingOrder;
        set => spriteRenderer.sortingOrder = value;
    }

    public string SortingLayerName
    {
        get => spriteRenderer.sortingLayerName;
        set => spriteRenderer.sortingLayerName = value;
    }


    public bool FlipX
    {
        get => spriteRenderer.flipX;
        set => spriteRenderer.flipX = value;
    }

    public bool FlipY
    {
        get => spriteRenderer.flipY;
        set => spriteRenderer.flipY = value;
    }


    public BattleSquaresSDK.SpriteDrawMode DrawMode
    {
        get => spriteRenderer.drawMode switch
        {
            UnityEngine.SpriteDrawMode.Simple => BattleSquaresSDK.SpriteDrawMode.Simple,
            UnityEngine.SpriteDrawMode.Sliced => BattleSquaresSDK.SpriteDrawMode.Sliced,
            UnityEngine.SpriteDrawMode.Tiled => BattleSquaresSDK.SpriteDrawMode.Tiled,
            _ => BattleSquaresSDK.SpriteDrawMode.Simple
        };
        set => spriteRenderer.drawMode = value switch
        {
            BattleSquaresSDK.SpriteDrawMode.Simple => UnityEngine.SpriteDrawMode.Simple,
            BattleSquaresSDK.SpriteDrawMode.Sliced => UnityEngine.SpriteDrawMode.Sliced,
            BattleSquaresSDK.SpriteDrawMode.Tiled => UnityEngine.SpriteDrawMode.Tiled,
            _ => UnityEngine.SpriteDrawMode.Simple
        };
    }

    public System.Numerics.Vector2 Size
    {
        get => cVec2(spriteRenderer.size);
        set => spriteRenderer.size = cVec2(value);
    }


    public BattleSquaresSDK.SpriteTileMode TileMode
    {
        get => spriteRenderer.tileMode switch
        {
            UnityEngine.SpriteTileMode.Continuous => BattleSquaresSDK.SpriteTileMode.Continuous,
            UnityEngine.SpriteTileMode.Adaptive => BattleSquaresSDK.SpriteTileMode.Adaptive,
            _ => BattleSquaresSDK.SpriteTileMode.Continuous
        };
        set => spriteRenderer.tileMode = value switch
        {
            BattleSquaresSDK.SpriteTileMode.Continuous => UnityEngine.SpriteTileMode.Continuous,
            BattleSquaresSDK.SpriteTileMode.Adaptive => UnityEngine.SpriteTileMode.Adaptive,
            _ => UnityEngine.SpriteTileMode.Continuous
        };
    }
}