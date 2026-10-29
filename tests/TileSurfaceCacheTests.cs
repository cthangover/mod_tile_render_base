namespace Cthangover.Mods.TileRender.Tests
{
    using System.Linq;
    using Cthangover.Core.Mods.Testing;
    using Godot;
    /// <summary>One edit rebuilds its affected halo, and cached GPU resources have explicit ownership.</summary>
    public class TileSurfaceCacheTests
    {
        private static Texture2D ThirdTexture(TileSurfaceCache cache)
        {
            foreach (var chunk in cache.Chunks) if (chunk.Cells.Position.X == 64) return chunk.Texture;
            return null;
        }

        [ModTest("surfaces/cache_reuses_unchanged_chunks_and_invalidates_halo")]
        static void CacheReusesUnchangedChunksAndInvalidatesHalo(ModTestContext ctx)
        {
            var pixels = new byte[16 * 8 * 4];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = 255;
            using var image = Image.CreateFromData(16, 8, false, Image.Format.Rgba8, pixels);
            using var texture = ImageTexture.CreateFromImage(image);
            using var cache = new TileSurfaceCache();
            var field = new TileSurfaceField { Width = 65, Height = 3, TileSize = 4, PatchSize = 8,
                Materials = new int[65 * 3], Diffuse = Enumerable.Repeat(true, 65 * 3).ToArray() };
            cache.Update(field, texture);
            ModAssert.Equal(3, cache.LastBuiltChunks, "cold build has three chunks");
            var last = ThirdTexture(cache);
            cache.Update(field, texture);
            ModAssert.Equal(0, cache.LastBuiltChunks, "camera/unchanged data generate no pixels");
            field.Materials[31] = 1;
            cache.Update(field, texture);
            ModAssert.Equal(2, cache.LastBuiltChunks, "an edit beside a chunk boundary invalidates both neighbours");
            ModAssert.True(ReferenceEquals(last, ThirdTexture(cache)), "unaffected third texture is preserved");
            field.Materials[0] = 1;
            cache.Update(field, texture);
            ModAssert.Equal(1, cache.LastBuiltChunks, "interior edit regenerates only one chunk");
            cache.Dispose();
            ModAssert.Equal(0, cache.TextureBytes, "explicit disposal releases the cache's budget");
        }
    }
}
