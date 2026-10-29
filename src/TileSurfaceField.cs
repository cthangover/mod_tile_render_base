namespace Cthangover.Mods.TileRender
{
    /// <summary>Projection-independent material IDs (-1 means no surface) and blending flags in row-major order.</summary>
    public class TileSurfaceField
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileSize { get; set; }
        public int PatchSize { get; set; }
        public int[] Materials { get; set; }
        public bool[] Diffuse { get; set; }
    }
}
