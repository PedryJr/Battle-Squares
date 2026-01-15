using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp")]
namespace BattleSquaresSDK
{



    public interface ISpriteRenderer
    {
        void OnDestroy();

        void SetSprite(ISprite sprite);
        ISprite GetSprite();

        IMaterial Material { get; set; }
        IMaterial SharedMaterial { get; set; }

        System.Numerics.Vector4 Color { get; set; }

        int SortingOrder { get; set; }
        string SortingLayerName { get; set; }

        bool FlipX { get; set; }
        bool FlipY { get; set; }

        SpriteDrawMode DrawMode { get; set; }
        System.Numerics.Vector2 Size { get; set; }

        SpriteTileMode TileMode { get; set; }
    }

    public enum SpriteDrawMode
    {
        Simple,
        Sliced,
        Tiled,
    }

    public enum SpriteTileMode
    {
        Continuous,
        Adaptive,
    }

    public class SpriteRendererComponent : ComponentBase, ISpriteRenderer
    {

        internal ISpriteRenderer component;
        public SpriteRendererComponent() => integrationType = IntegrationType.SpriteRenderer;

        public IMaterial Material { get => component.Material; set => component.Material = value; }
        public IMaterial SharedMaterial { get => component.SharedMaterial; set => component.SharedMaterial = value; }
        public Vector4 Color { get => component.Color; set => component.Color = value; }
        public int SortingOrder { get => component.SortingOrder; set => component.SortingOrder = value; }
        public string SortingLayerName { get => component.SortingLayerName; set => component.SortingLayerName = value; }
        public bool FlipX { get => component.FlipX; set => component.FlipX = value; }
        public bool FlipY { get => component.FlipY; set => component.FlipY = value; }
        public SpriteDrawMode DrawMode { get => component.DrawMode; set => component.DrawMode = value; }
        public Vector2 Size { get => component.Size; set => component.Size = value; }
        public SpriteTileMode TileMode { get => component.TileMode; set => component.TileMode = value; }

        public ISprite GetSprite() => component.GetSprite(); 
        public override void OnDestroy() => component.OnDestroy(); 
        public void SetSprite(ISprite sprite) => component.SetSprite(sprite);
    }
}