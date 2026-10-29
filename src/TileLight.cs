namespace Cthangover.Mods.TileRender
{
    using Godot;
    /// <summary>Position and radius are logical cell units in both projections.</summary>
    public class TileLight
    {
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Color Color { get; set; }
    }
}
