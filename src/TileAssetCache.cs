namespace Cthangover.Mods.TileRender
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Utils;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Resolvers;
    using System.IO;
    using System.Text.Json;
    using Godot;
    /// <summary>File-group lookup preserves mod overrides and ZIP providers. Instances own decoded textures and regions.</summary>
    public class TileAssetCache : IDisposable
    {
        private readonly Dictionary<string, Texture2D> textures = new();
        public Texture2D Resolve(string group, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var key = group + "/" + path;
            if (!textures.TryGetValue(key, out var texture))
            {
                textures[key] = texture = TextureUtils.LoadFromModGroup(group, path);
                if (group == "charsets" && texture != null)
                    ConfigureWalk(texture, path);
            }
            return texture;
        }

        private static void ConfigureWalk(Texture2D texture, string path)
        {
            var normalized = ModPath.Normalize(path).TrimStart('/');
            if (normalized.StartsWith("charsets/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("charsets/".Length);
            if (Path.HasExtension(normalized))
                normalized = normalized.Substring(0, normalized.Length - Path.GetExtension(normalized).Length);
            var files = Files.Collect("charsets");
            if (files == null || !files.TryGetValue(normalized + ".walk.json", out var entry))
                return;
            try
            {
                var json = ModManager.Instance.ReadFileText(entry.ModId, entry.FullPath);
                var animation = JsonSerializer.Deserialize<TileWalkAnimation>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (animation == null)
                    throw new ArgumentException("Walking sheet metadata is null.");
                animation.Validate();
                if (texture.GetWidth() % animation.Columns != 0 || texture.GetHeight() % TileWalkSheet.Rows != 0)
                    throw new ArgumentException("Walking sheet size must divide into Columns by four rows.");
                TileWalkSheet.Configure(texture, animation);
            }
            catch (Exception error)
            {
                GameLogger.Log("RPG", $"walking sheet '{path}': invalid .walk.json: {error.Message}", LogLevel.Error);
            }
        }
        public Texture2D Region(string key, Texture2D atlas, Rect2 region)
        {
            key = "region/" + key;
            if (!textures.TryGetValue(key, out var texture)) textures[key] = texture = new AtlasTexture { Atlas = atlas, Region = region };
            return texture;
        }
        public void Dispose()
        {
            foreach (var texture in new HashSet<Texture2D>(textures.Values)) texture?.Dispose();
            textures.Clear();
        }
    }
}
