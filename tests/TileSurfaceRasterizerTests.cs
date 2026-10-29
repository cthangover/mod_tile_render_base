namespace Cthangover.Mods.TileRender.Tests
{
    using Cthangover.Core.Mods.Testing;
    using Godot;
    /// <summary>Chunk edges, world phase and diagonal blending must match the full reference texture byte-for-byte.</summary>
    public class TileSurfaceRasterizerTests
    {
        [ModTest("surfaces/homogeneous_field_preserves_every_patch_byte")]
        static void HomogeneousPatch(ModTestContext ctx)
        {
            var atlas = new byte[16 * 16 * 4];
            for (var i = 0; i < atlas.Length; i++) atlas[i] = (byte)(i * 73 % 256);
            foreach (var size in new[] { 8, 32 })
            foreach (var diffuse in new[] { false, true })
            {
                var field = new TileSurfaceField { Width = 3, Height = 2, TileSize = size, PatchSize = 16,
                    Materials = new int[6], Diffuse = new[] { diffuse, diffuse, diffuse, diffuse, diffuse, diffuse } };
                var pixels = TileSurfaceRasterizer.Render(field, atlas, 16, new Rect2I(0, 0, 3, 2));
                for (var y = 0; y < 2 * size; y++) for (var x = 0; x < 3 * size; x++) for (var c = 0; c < 4; c++)
                    ModAssert.Equal(atlas[((y % 16) * 16 + x % 16) * 4 + c], pixels[(y * 3 * size + x) * 4 + c], "exact source sample including alpha");
            }
        }
        [ModTest("surfaces/chunks_equal_monolithic_pixels")]
        static void ChunksEqualMonolithicPixels(ModTestContext ctx)
        {
            var field = new TileSurfaceField { Width = 4, Height = 3, TileSize = 8, PatchSize = 16,
                Materials = new[] { 0, 0, 1, 1, 0, -1, 1, 1, 1, 0, 0, 1 },
                Diffuse = new[] { true, true, true, true, true, true, true, false, true, true, true, true } };
            var atlas = new byte[32 * 16 * 4];
            for (var y = 0; y < 16; y++) for (var x = 0; x < 32; x++)
            { var i = (y * 32 + x) * 4; atlas[i] = (byte)(x * 7); atlas[i + 1] = (byte)(y * 15); atlas[i + 3] = 255; }
            var full = TileSurfaceRasterizer.Render(field, atlas, 32, new Rect2I(0, 0, 4, 3));
            for (var cy = 0; cy < 3; cy++) for (var cx = 0; cx < 4; cx++)
            {
                var part = TileSurfaceRasterizer.Render(field, atlas, 32, new Rect2I(cx, cy, 1, 1));
                for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++) for (var c = 0; c < 4; c++)
                    ModAssert.Equal(full[((cy * 8 + y) * 32 + cx * 8 + x) * 4 + c], part[(y * 8 + x) * 4 + c], "chunk including halo and global patch phase");
            }
        }
    }
}
