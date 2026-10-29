namespace Cthangover.Mods.TileRender
{
    using System.Collections.Generic;
    using Godot;

    /// <summary>Static presentation snapshot, prepared by a game adapter. Owns only explicitly listed transient textures.</summary>
    public class TileRenderScene
    {
        public Vector2I Size { get; set; }
        public long Revision { get; set; }
        public int TileSize { get; set; } = 32;
        public float Zoom { get; set; } = 2;
        public string Atmosphere { get; set; }
        public List<Vector2> Water { get; } = new();
        public List<TileVisual> Visuals { get; } = new();
        public List<Texture2D> OwnedTextures { get; } = new();
        public List<string> Warnings { get; } = new();

        /// <summary>Shared file-group textures are borrowed, never freed by the snapshot.</summary>
        public void Release()
        {
            foreach (var texture in OwnedTextures)
                texture.Dispose();
            OwnedTextures.Clear();
            Visuals.Clear();
        }
    }
}
