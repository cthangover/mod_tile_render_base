namespace Cthangover.Mods.TileRender
{
    using Godot;

    /// <summary>One already evaluated actor pose; no event conditions or route definitions cross this boundary.</summary>
    public class TileActorPose
    {
        public string Id { get; set; }
        public Vector2 Position { get; set; }
        public bool Visible { get; set; }
        public TileVisual Look { get; set; }
        public bool Charset { get; set; }
        public int Facing { get; set; }
        public int Column { get; set; } = 1;
        public Color Tint { get; set; } = Colors.White;
    }
}
