namespace Cthangover.Mods.TileRender
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Utils;
    using Godot;
    /// <summary>
    /// Owns bounded 32-cell texture chunks. A one-cell halo determines invalidation;
    /// moving a camera or replacing a renderer never re-rasterizes them. After Update,
    /// apply the new snapshot before the next draw: changed textures have been replaced.
    /// </summary>
    public class TileSurfaceCache : IDisposable
    {
        public const int ChunkSize = 32;
        private readonly Dictionary<Vector2I, TileSurfaceChunk> chunks = new();
        private TileSurfaceField previous;
        private Texture2D atlas;
        private byte[] pixels;
        private int atlasWidth;
        public int LastBuiltChunks { get; private set; }
        public IEnumerable<TileSurfaceChunk> Chunks => chunks.Values;
        public long TextureBytes { get; private set; }

        public void Update(TileSurfaceField field, Texture2D source)
        {
            if (!GameEnvironment.IsRuntime) throw new InvalidOperationException("Texture cache requires the Godot runtime.");
            var reset = !ReferenceEquals(atlas, source) || previous == null || previous.Width != field.Width ||
                previous.Height != field.Height || previous.TileSize != field.TileSize || previous.PatchSize != field.PatchSize;
            if (reset)
            {
                Dispose();
                atlas = source;
                using var image = source.GetImage();
                image.Convert(Image.Format.Rgba8);
                pixels = image.GetData();
                atlasWidth = image.GetWidth();
            }
            LastBuiltChunks = 0;
            for (var y = 0; y < field.Height; y += ChunkSize)
            for (var x = 0; x < field.Width; x += ChunkSize)
            {
                var rect = new Rect2I(x, y, Math.Min(ChunkSize, field.Width - x), Math.Min(ChunkSize, field.Height - y));
                if (!reset && !Changed(field, rect)) continue;
                var key = new Vector2I(x, y);
                if (chunks.Remove(key, out var old)) old.Texture.Dispose();
                var found = false;
                for (var cy = y; cy < rect.End.Y && !found; cy++)
                for (var cx = x; cx < rect.End.X && !found; cx++) found = field.Materials[cy * field.Width + cx] >= 0;
                if (!found) continue;
                var bytes = TileSurfaceRasterizer.Render(field, pixels, atlasWidth, rect);
                using var image = Image.CreateFromData(rect.Size.X * field.TileSize, rect.Size.Y * field.TileSize, false, Image.Format.Rgba8, bytes);
                chunks[key] = new TileSurfaceChunk { Cells = rect, Texture = ImageTexture.CreateFromImage(image) };
                LastBuiltChunks++;
            }
            previous = new TileSurfaceField { Width = field.Width, Height = field.Height, TileSize = field.TileSize,
                PatchSize = field.PatchSize, Materials = (int[])field.Materials.Clone(), Diffuse = (bool[])field.Diffuse.Clone() };
            TextureBytes = 0;
            foreach (var chunk in chunks.Values) TextureBytes += (long)chunk.Cells.Size.X * chunk.Cells.Size.Y * field.TileSize * field.TileSize * 4;
        }

        private bool Changed(TileSurfaceField field, Rect2I rect)
        {
            for (var y = Math.Max(0, rect.Position.Y - 1); y < Math.Min(field.Height, rect.End.Y + 1); y++)
            for (var x = Math.Max(0, rect.Position.X - 1); x < Math.Min(field.Width, rect.End.X + 1); x++)
            {
                var i = y * field.Width + x;
                if (field.Materials[i] != previous.Materials[i] || field.Diffuse[i] != previous.Diffuse[i]) return true;
            }
            return false;
        }

        public void Dispose()
        {
            foreach (var chunk in chunks.Values) chunk.Texture.Dispose();
            chunks.Clear(); previous = null; atlas = null; pixels = null; TextureBytes = 0;
        }
    }
}
