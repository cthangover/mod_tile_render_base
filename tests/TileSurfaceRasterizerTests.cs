namespace Cthangover.Mods.TileRender.Tests
{
    using Cthangover.Core.Mods.Testing;
    using Godot;
    /// <summary>Chunk edges, world phase and diagonal blending must match the full reference texture byte-for-byte.</summary>
    public class TileSurfaceRasterizerTests
    {
        [ModTest("surfaces/strength_preserves_legacy_and_controls_transition_width")]
        static void StrengthControlsWidth(ModTestContext ctx)
        {
            var field = new TileSurfaceField { Width = 2, Height = 1, TileSize = 32, PatchSize = 16,
                Materials = new[] { 0, 1 }, Diffuse = new[] { true, true } };
            var atlas = new byte[32 * 16 * 4];
            for (var y = 0; y < 16; y++) for (var x = 0; x < 32; x++)
            { atlas[(y * 32 + x) * 4 + (x < 16 ? 0 : 2)] = 255; atlas[(y * 32 + x) * 4 + 3] = 255; }
            var rect = new Rect2I(0, 0, 2, 1);
            var legacy = TileSurfaceRasterizer.Render(field, atlas, 32, rect);
            var previousMixed = 0;
            foreach (var strength in new[] { 1, 2, 3 })
            {
                field.DiffuseStrength = new[] { strength, strength };
                var full = TileSurfaceRasterizer.Render(field, atlas, 32, rect);
                if (strength == 2) ModAssert.True(System.MemoryExtensions.SequenceEqual<byte>(legacy, full), "2 preserves legacy pixels exactly");
                var mixed = 0;
                for (var i = 0; i < full.Length; i += 4) if (full[i] > 0 && full[i + 2] > 0) mixed++;
                ModAssert.True(mixed > previousMixed, "higher strength widens the transition");
                previousMixed = mixed;
                for (var cx = 0; cx < 2; cx++)
                {
                    var part = TileSurfaceRasterizer.Render(field, atlas, 32, new Rect2I(cx, 0, 1, 1));
                    for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) for (var c = 0; c < 4; c++)
                        ModAssert.Equal(full[(y * 64 + cx * 32 + x) * 4 + c], part[(y * 32 + x) * 4 + c], "strength keeps chunk seams identical");
                }
                field.Diffuse[1] = false;
                var hard = TileSurfaceRasterizer.Render(field, atlas, 32, rect);
                ModAssert.Equal((byte)255, hard[31 * 4], "disabled diffusion keeps the left edge red");
                ModAssert.Equal((byte)255, hard[32 * 4 + 2], "disabled diffusion keeps the right edge blue");
                field.Diffuse[1] = true;
            }
        }

        [ModTest("surfaces/strength_keeps_small_hard_cells_opaque")]
        static void SmallHardCells(ModTestContext ctx)
        {
            var atlas = new byte[8 * 8 * 4];
            System.Array.Fill(atlas, (byte)255);
            foreach (var strength in new[] { 1, 2, 3 })
            {
                var field = new TileSurfaceField { Width = 16, Height = 16, TileSize = 4, PatchSize = 8,
                    Materials = new int[256], Diffuse = new bool[256], DiffuseStrength = new int[256] };
                System.Array.Fill(field.DiffuseStrength, strength);
                var pixels = TileSurfaceRasterizer.Render(field, atlas, 8, new Rect2I(0, 0, 16, 16));
                foreach (var sample in pixels) ModAssert.Equal((byte)255, sample, "disabling diffusion never leaves holes at any strength");
            }
        }

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
