using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{
    public static class AssetCreator
    {
        internal delegate ITexture2D CreateTextureDelegate(string PngPath, TextureWrapMode wrapMode, FilterMode filterMode);
        internal static CreateTextureDelegate createTextureDelegate;
        public static ITexture2D CreateTexture(string PngPath, TextureWrapMode wrapMode, FilterMode filterMode)
        {
            return createTextureDelegate(PngPath, wrapMode, filterMode);
        }
        internal delegate ISprite CreateSpriteDelegate(ITexture2D texture, int PixelsPerUnit);
        internal static CreateSpriteDelegate createSpriteDelegate;
        public static ISprite CreateSprite(ITexture2D texture, int PixelsPerUnit)
        {
            return createSpriteDelegate(texture, PixelsPerUnit);
        }

        public static (ISprite, ITexture2D) CreateSprite(string PngPath, int PixelsPerUnit, TextureWrapMode wrapMode, FilterMode filterMode)
        {
            ITexture2D texture = CreateTexture(PngPath, wrapMode, filterMode);
            ISprite sprite = CreateSprite(texture, PixelsPerUnit);
            return (sprite, texture);
        }
        internal delegate IMaterial CreateMaterialDelegate(IMaterial source = null, IShader shader = null);
        internal static CreateMaterialDelegate createMaterialDelegate;
        public static IMaterial CreateMaterial(IMaterial source = null, IShader shader = null) => createMaterialDelegate(source, shader);
        internal delegate IShader CreateShaderDelegate(string assetBundlePath, string shaderName);
        internal static CreateShaderDelegate createShaderDelegate;
        public static IShader CreateShader(string shaderPath, string internalShaderName) => createShaderDelegate(shaderPath, internalShaderName);
        internal delegate IMesh CreateMeshDelegate();
        internal static CreateMeshDelegate createMeshDelegate;
        public static IMesh CreateMesh() => createMeshDelegate();
    }

    public interface ITexture2D : IDestroyable
    {
        int Width { get; }
        int Height { get; }
        string Name { get; }
        TextureFormat Format { get; }
        TextureWrapMode WrapMode { get; set; }
        FilterMode FilterMode { get; set; }
        bool IsReadable { get; }
    }

    public interface ISprite : IDestroyable
    {
        int Width { get; }
        int Height { get; }
        System.Numerics.Vector2 Pivot { get; }
        System.Numerics.Vector4 Border { get; }
        ITexture2D Texture { get; }
        string Name { get; }
        float PixelsPerUnit { get; }
    }

    public interface IMaterial : IDestroyable
    {
        bool HasProperty(string name);

        void SetColor(string name, System.Numerics.Vector4 color);
        System.Numerics.Vector4 GetColor(string name);

        void SetFloat(string name, float value);
        float GetFloat(string name);

        void SetVector(string name, System.Numerics.Vector4 value);
        System.Numerics.Vector4 GetVector(string name);

        void SetTexture(string name, ITexture2D texture);
        ITexture2D GetTexture(string name);

        void SetInt(string name, int value);
        int GetInt(string name);

        void EnableKeyword(string keyword);
        void DisableKeyword(string keyword);
        bool IsKeywordEnabled(string keyword);

        string ShaderName { get; }
        int RenderQueue { get; set; }
    }

    public interface IShader
    {
        string Name { get; }
        int PropertyCount { get; }
        string GetPropertyName(int index);
        PropertyType GetPropertyType(int index);
        bool IsSupported { get; }
    }

    public interface IMesh : IDestroyable
    {
        string Name { get; set; }
        int VertexCount { get; }
        int TriangleCount { get; }
        System.Numerics.Vector3[] Vertices { get; set; }
        System.Numerics.Vector3[] Normals { get; set; }
        System.Numerics.Vector2[] UV { get; set; }
        int[] Triangles { get; set; }
        void RecalculateNormals();
        void RecalculateBounds();
    }

    public interface IMeshRenderer
    {
        bool Enabled { get; set; }
        IMaterial Material { get; set; }
        IMaterial[] Materials { get; set; }
        IMaterial SharedMaterial { get; set; }
        int SortingOrder { get; set; }
        string SortingLayerName { get; set; }

        IMesh Mesh { get; set; } // <-- Expose mesh
    }

    public interface IDestroyable
    {
        void Destroy();
    }

    public enum TextureFormat
    {
        RGBA32,
        ARGB32,
        RGB24,
        Alpha8,
        Unknown
    }

    public enum PropertyType
    {
        Color,
        Vector,
        Float,
        Range,
        Texture,
        Int
    }

    public enum FilterMode
    {
        Point,
        Bilinear,
        Trilinear
    }
    public enum TextureWrapMode
    {
        Repeat,
        Clamp,
        Mirror,
        MirrorOnce,
    }

    public struct Colors
    {
        public static readonly Vector4 Red = new Vector4(1f, 0f, 0f, 1f);
        public static readonly Vector4 Green = new Vector4(0f, 1f, 0f, 1f);
        public static readonly Vector4 Blue = new Vector4(0f, 0f, 1f, 1f);
        public static readonly Vector4 Yellow = new Vector4(1f, 1f, 0f, 1f);
        public static readonly Vector4 Cyan = new Vector4(0f, 1f, 1f, 1f);
        public static readonly Vector4 Magenta = new Vector4(1f, 0f, 1f, 1f);
        public static readonly Vector4 White = new Vector4(1f, 1f, 1f, 1f);
        public static readonly Vector4 Black = new Vector4(0f, 0f, 0f, 1f);
        public static readonly Vector4 Gray = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        public static readonly Vector4 DarkGray = new Vector4(0.25f, 0.25f, 0.25f, 1f);
        public static readonly Vector4 LightGray = new Vector4(0.75f, 0.75f, 0.75f, 1f);
        public static readonly Vector4 Orange = new Vector4(1f, 0.5f, 0f, 1f);
        public static readonly Vector4 Brown = new Vector4(0.6f, 0.3f, 0f, 1f);
        public static readonly Vector4 Purple = new Vector4(0.5f, 0f, 0.5f, 1f);
        public static readonly Vector4 Pink = new Vector4(1f, 0.75f, 0.8f, 1f);
        public static readonly Vector4 Lime = new Vector4(0.75f, 1f, 0f, 1f);
        public static readonly Vector4 Olive = new Vector4(0.5f, 0.5f, 0f, 1f);
        public static readonly Vector4 Teal = new Vector4(0f, 0.5f, 0.5f, 1f);
        public static readonly Vector4 Navy = new Vector4(0f, 0f, 0.5f, 1f);
        public static readonly Vector4 Maroon = new Vector4(0.5f, 0f, 0f, 1f);
        public static readonly Vector4 Coral = new Vector4(1f, 0.5f, 0.31f, 1f);
        public static readonly Vector4 Gold = new Vector4(1f, 0.84f, 0f, 1f);
        public static readonly Vector4 Silver = new Vector4(0.75f, 0.75f, 0.75f, 1f);
        public static readonly Vector4 Violet = new Vector4(0.93f, 0.51f, 0.93f, 1f);
        public static readonly Vector4 Indigo = new Vector4(0.29f, 0f, 0.51f, 1f);
        public static readonly Vector4 Turquoise = new Vector4(0.25f, 0.88f, 0.82f, 1f);
        public static readonly Vector4 Salmon = new Vector4(0.98f, 0.5f, 0.45f, 1f);
        public static readonly Vector4 Beige = new Vector4(0.96f, 0.96f, 0.86f, 1f);
        public static readonly Vector4 Mint = new Vector4(0.6f, 1f, 0.6f, 1f);
        public static readonly Vector4 Peach = new Vector4(1f, 0.85f, 0.7f, 1f);
        public static readonly Vector4 Lavender = new Vector4(0.9f, 0.9f, 0.98f, 1f);
        public static readonly Vector4 Chocolate = new Vector4(0.82f, 0.41f, 0.12f, 1f);
    }

}
