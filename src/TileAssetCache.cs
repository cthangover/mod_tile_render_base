namespace Cthangover.Mods.TileRender
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Utils;
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
                textures[key] = texture = TextureUtils.LoadFromModGroup(group, path);
            return texture;
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
