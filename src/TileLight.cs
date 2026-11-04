namespace Cthangover.Mods.TileRender
{
    using Godot;
    /// <summary>Source centers use stable map coordinates. ViewSpace selects circular falloff on the projected artwork; portable light uses viewport pixels.</summary>
    public class TileLight
    {
        /// <summary>Use a circular falloff in the rendered 2D image; the center stays in stable map coordinates for camera movement.</summary>
        public bool ViewSpace { get; set; }
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Color Color { get; set; }
    }
}
