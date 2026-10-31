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

        /// <summary>Optional per-cell strengths, 1–3; absent data uses level 2 (the original blend).</summary>
        public int[] DiffuseStrength { get; set; }

        /// <summary>Normalizes author data and supports fields created before adjustable diffusion.</summary>
        public int StrengthAt(int index) => DiffuseStrength == null ? 2 : System.Math.Clamp(DiffuseStrength[index], 1, 3);
    }
}
