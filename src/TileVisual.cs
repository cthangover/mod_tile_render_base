namespace Cthangover.Mods.TileRender
{
    using Godot;

    /// <summary>A resolved image region in cell space. Texture pixels are never gameplay geometry.</summary>
    public class TileVisual
    {
        public string Id { get; set; }
        public Texture2D Texture { get; set; }
        public Rect2 Source { get; set; }
        public Rect2 Bounds { get; set; }
        public Vector2 Anchor { get; set; }
        public int Layer { get; set; }
        public bool Upright { get; set; }
        public Color Tint { get; set; } = Colors.White;
        /// <summary>Optional explicit world plane, TL/TR/BR/BL in world cell coordinates; with Upright it is depth-sorted at Anchor.</summary>
        public Vector3[] WorldQuad { get; set; }
        /// <summary>Only this projection uses the visual (empty means both).</summary>
        public string Only { get; set; }
    }
}
