using BattleSquaresSDK;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using static UnityVecToSystemVec;

public class AssetWrappers
{
    internal sealed class SpriteWrapper : ISprite
    {
        internal readonly Sprite sprite;

        public string Name => sprite != null ? sprite.name : string.Empty;

        public int Width => sprite != null ? sprite.rect.width > 0 ? (int)sprite.rect.width : 0 : 0;

        public int Height => sprite != null ? sprite.rect.height > 0 ? (int)sprite.rect.height : 0 : 0;

        public System.Numerics.Vector2 Pivot => sprite != null
            ? cVec2(new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height))
            : cVec2(Vector2.zero);

        public System.Numerics.Vector4 Border => sprite != null
            ? cVec4(sprite.border)
            : System.Numerics.Vector4.Zero;

        public float PixelsPerUnit => sprite != null ? sprite.pixelsPerUnit : 100f;

        public ITexture2D Texture { get; }

        internal SpriteWrapper(Sprite sourceSprite)
        {
            if (sourceSprite == null) throw new ArgumentNullException(nameof(sourceSprite));

            sprite = sourceSprite;

            if (sprite.texture != null) Texture = new Texture2DWrapper(sprite.texture);
        }

        public void Destroy()
        {
            if (sprite != null) UnityEngine.Object.Destroy(sprite);
        }
    }

    internal sealed class Texture2DWrapper : ITexture2D
    {
        internal readonly Texture2D texture;

        public string Name => texture != null ? texture.name : string.Empty;
        public int Width => texture != null ? texture.width : 0;
        public int Height => texture != null ? texture.height : 0;
        public bool IsReadable => texture != null && texture.isReadable;

        public BattleSquaresSDK.TextureFormat Format
        {
            get
            {
                if (texture == null) return BattleSquaresSDK.TextureFormat.Unknown;

                return texture.format switch
                {
                    UnityEngine.TextureFormat.RGBA32 => BattleSquaresSDK.TextureFormat.RGBA32,
                    UnityEngine.TextureFormat.ARGB32 => BattleSquaresSDK.TextureFormat.ARGB32,
                    UnityEngine.TextureFormat.RGB24 => BattleSquaresSDK.TextureFormat.RGB24,
                    UnityEngine.TextureFormat.Alpha8 => BattleSquaresSDK.TextureFormat.Alpha8,
                    _ => BattleSquaresSDK.TextureFormat.Unknown
                };
            }
        }

        public BattleSquaresSDK.TextureWrapMode WrapMode
        {
            get
            {
                if (texture == null) return BattleSquaresSDK.TextureWrapMode.Repeat;

                return texture.wrapMode switch
                {
                    UnityEngine.TextureWrapMode.Repeat => BattleSquaresSDK.TextureWrapMode.Repeat,
                    UnityEngine.TextureWrapMode.Clamp => BattleSquaresSDK.TextureWrapMode.Clamp,
                    UnityEngine.TextureWrapMode.Mirror => BattleSquaresSDK.TextureWrapMode.Mirror,
                    UnityEngine.TextureWrapMode.MirrorOnce => BattleSquaresSDK.TextureWrapMode.MirrorOnce,
                    _ => BattleSquaresSDK.TextureWrapMode.Repeat
                };
            }
            set
            {
                if (texture == null) return;

                texture.wrapMode = value switch
                {
                    BattleSquaresSDK.TextureWrapMode.Repeat => UnityEngine.TextureWrapMode.Repeat,
                    BattleSquaresSDK.TextureWrapMode.Clamp => UnityEngine.TextureWrapMode.Clamp,
                    BattleSquaresSDK.TextureWrapMode.Mirror => UnityEngine.TextureWrapMode.Mirror,
                    BattleSquaresSDK.TextureWrapMode.MirrorOnce => UnityEngine.TextureWrapMode.MirrorOnce,
                    _ => UnityEngine.TextureWrapMode.Repeat
                };
            }
        }

        public BattleSquaresSDK.FilterMode FilterMode
        {
            get
            {
                if (texture == null) return BattleSquaresSDK.FilterMode.Point;

                return texture.filterMode switch
                {
                    UnityEngine.FilterMode.Point => BattleSquaresSDK.FilterMode.Point,
                    UnityEngine.FilterMode.Bilinear => BattleSquaresSDK.FilterMode.Bilinear,
                    UnityEngine.FilterMode.Trilinear => BattleSquaresSDK.FilterMode.Trilinear,
                    _ => BattleSquaresSDK.FilterMode.Point
                };
            }
            set
            {
                if (texture == null) return;

                texture.filterMode = value switch
                {
                    BattleSquaresSDK.FilterMode.Point => UnityEngine.FilterMode.Point,
                    BattleSquaresSDK.FilterMode.Bilinear => UnityEngine.FilterMode.Bilinear,
                    BattleSquaresSDK.FilterMode.Trilinear => UnityEngine.FilterMode.Trilinear,
                    _ => UnityEngine.FilterMode.Point
                };
            }
        }

        internal Texture2DWrapper(Texture2D tex)
        {
            if (tex == null) throw new ArgumentNullException(nameof(tex));
            texture = tex;
        }

        public void Destroy()
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }
    }

    internal sealed class MaterialWrapper : IMaterial
    {
        internal readonly Material material;
        public string ShaderName => material?.shader != null ? material.shader.name : string.Empty;

        public int RenderQueue
        {
            get => material != null ? material.renderQueue : 0;
            set { if (material != null) material.renderQueue = value; }
        }

        internal MaterialWrapper(Shader shader, Material clone = null)
        {
            if (shader == null)
                throw new ArgumentNullException(nameof(shader));

            material = clone != null
                ? UnityEngine.Object.Instantiate(clone)
                : new Material(shader);

            material.shader = shader;
        }

        internal MaterialWrapper(Material source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            material = UnityEngine.Object.Instantiate(source);
        }

        public bool HasProperty(string name) => material.HasProperty(name);

        private void ValidateProperty(string name)
        {
            if (!material.HasProperty(name))
                throw new InvalidOperationException($"Shader '{ShaderName}' does not define property '{name}'.");
        }

        public System.Numerics.Vector4 GetColor(string name)
        {
            ValidateProperty(name);
            return cVec4(material.GetColor(name));
        }

        public void SetColor(string name, System.Numerics.Vector4 color)
        {
            ValidateProperty(name);
            material.SetColor(name, cVec4(color));
        }

        public float GetFloat(string name)
        {
            ValidateProperty(name);
            return material.GetFloat(name);
        }

        public void SetFloat(string name, float value)
        {
            ValidateProperty(name);
            material.SetFloat(name, value);
        }

        public System.Numerics.Vector4 GetVector(string name)
        {
            ValidateProperty(name);
            return cVec4(material.GetVector(name));
        }

        public void SetVector(string name, System.Numerics.Vector4 value)
        {
            ValidateProperty(name);
            material.SetVector(name, cVec4(value));
        }

        public void SetTexture(string name, ITexture2D texture)
        {
            ValidateProperty(name);
            var wrapper = texture as Texture2DWrapper;
            material.SetTexture(name, wrapper?.texture);
        }

        public ITexture2D GetTexture(string name)
        {
            ValidateProperty(name);
            var tex = material.GetTexture(name) as Texture2D;
            return tex != null ? new Texture2DWrapper(tex) : null;
        }

        public void SetInt(string name, int value)
        {
            ValidateProperty(name);
            material.SetInt(name, value);
        }

        public int GetInt(string name)
        {
            ValidateProperty(name);
            return material.GetInt(name);
        }

        public void EnableKeyword(string keyword) => material.EnableKeyword(keyword);

        public void DisableKeyword(string keyword) => material.DisableKeyword(keyword);

        public bool IsKeywordEnabled(string keyword) => material.IsKeywordEnabled(keyword);

        public void Destroy()
        {
            if (material != null) UnityEngine.Object.Destroy(material);
        }
    }

    internal sealed class ShaderWrapper : IShader
    {
        internal readonly Shader shader;

        public string Name => shader != null ? shader.name : string.Empty;
        public int PropertyCount => shader != null ? shader.GetPropertyCount() : 0;
        public bool IsSupported => shader != null && shader.isSupported;

        internal ShaderWrapper(Shader sourceShader)
        {
            if (sourceShader == null) throw new ArgumentNullException(nameof(sourceShader));
            shader = sourceShader;
        }

        public string GetPropertyName(int index)
        {
            if (shader == null || index < 0 || index >= PropertyCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return shader.GetPropertyName(index);
        }

        public PropertyType GetPropertyType(int index)
        {
            if (shader == null || index < 0 || index >= PropertyCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return shader.GetPropertyType(index) switch
            {
                UnityEngine.Rendering.ShaderPropertyType.Color => PropertyType.Color,
                UnityEngine.Rendering.ShaderPropertyType.Vector => PropertyType.Vector,
                UnityEngine.Rendering.ShaderPropertyType.Float => PropertyType.Float,
                UnityEngine.Rendering.ShaderPropertyType.Range => PropertyType.Range,
                UnityEngine.Rendering.ShaderPropertyType.Texture => PropertyType.Texture,
                UnityEngine.Rendering.ShaderPropertyType.Int => PropertyType.Int,
                _ => PropertyType.Float
            };
        }
    }

    internal sealed class MeshWrapper : IMesh
    {
        internal readonly Mesh mesh;

        public string Name
        {
            get => mesh != null ? mesh.name : string.Empty;
            set { if (mesh != null) mesh.name = value; }
        }

        public int VertexCount => mesh != null ? mesh.vertexCount : 0;
        public int TriangleCount => mesh != null ? mesh.triangles.Length / 3 : 0;

        public System.Numerics.Vector3[] Vertices
        {
            get => mesh != null ? mesh.vertices.Select(v => cVec3(v)).ToArray() : Array.Empty<System.Numerics.Vector3>();
            set { if (mesh != null) mesh.vertices = value.Select(v => cVec3(v)).ToArray(); }
        }

        public System.Numerics.Vector3[] Normals
        {
            get => mesh != null ? mesh.normals.Select(n => cVec3(n)).ToArray() : Array.Empty<System.Numerics.Vector3>();
            set { if (mesh != null) mesh.normals = value.Select(n => cVec3(n)).ToArray(); }
        }

        public System.Numerics.Vector2[] UV
        {
            get => mesh != null ? mesh.uv.Select(u => cVec2(u)).ToArray() : Array.Empty<System.Numerics.Vector2>();
            set { if (mesh != null) mesh.uv = value.Select(u => cVec2(u)).ToArray(); }
        }

        public int[] Triangles
        {
            get => mesh != null ? mesh.triangles : Array.Empty<int>();
            set { if (mesh != null) mesh.triangles = value; }
        }

        internal MeshWrapper(Mesh sourceMesh = null)
        {
            mesh = sourceMesh != null ? UnityEngine.Object.Instantiate(sourceMesh) : new Mesh();
        }

        public void RecalculateNormals()
        {
            mesh?.RecalculateNormals();
        }

        public void RecalculateBounds()
        {
            mesh?.RecalculateBounds();
        }

        public void Destroy()
        {
            if (mesh != null) UnityEngine.Object.Destroy(mesh);
        }
    }

    internal sealed class MeshRendererWrapper : IMeshRenderer
    {
        internal readonly MeshRenderer renderer;

        public MeshRendererWrapper(MeshRenderer sourceRenderer)
        {
            renderer = sourceRenderer ?? throw new ArgumentNullException(nameof(sourceRenderer));
        }

        public bool Enabled
        {
            get => renderer != null && renderer.enabled;
            set { if (renderer != null) renderer.enabled = value; }
        }

        public IMaterial Material
        {
            get => renderer != null && renderer.material != null ? new MaterialWrapper(renderer.material) : null;
            set
            {
                if (renderer != null && value is MaterialWrapper wrapper)
                    renderer.material = wrapper.material;
            }
        }

        public IMaterial[] Materials
        {
            get => renderer != null
                ? renderer.materials.Select(m => new MaterialWrapper(m) as IMaterial).ToArray()
                : Array.Empty<IMaterial>();
            set
            {
                if (renderer != null && value != null)
                    renderer.materials = value.OfType<MaterialWrapper>().Select(w => w.material).ToArray();
            }
        }

        public IMaterial SharedMaterial
        {
            get => renderer != null && renderer.sharedMaterial != null ? new MaterialWrapper(renderer.sharedMaterial) : null;
            set
            {
                if (renderer != null && value is MaterialWrapper wrapper)
                    renderer.sharedMaterial = wrapper.material;
            }
        }

        public int SortingOrder
        {
            get => renderer != null ? renderer.sortingOrder : 0;
            set { if (renderer != null) renderer.sortingOrder = value; }
        }

        public string SortingLayerName
        {
            get => renderer != null ? renderer.sortingLayerName : string.Empty;
            set { if (renderer != null) renderer.sortingLayerName = value; }
        }

        // ---- Mesh property (interop with IMesh) ----

        public IMesh Mesh
        {
            get
            {
                if (renderer == null || renderer.GetComponent<MeshFilter>() == null) return null;

                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null) return null;

                return new MeshWrapper(mesh);
            }
            set
            {
                if (renderer == null) return;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null)
                    filter = renderer.gameObject.AddComponent<MeshFilter>();

                if (value is MeshWrapper wrapper)
                    filter.mesh = wrapper.mesh;
            }
        }
    }
}
