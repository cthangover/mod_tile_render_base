namespace Cthangover.Mods.TileRender
{
    using System.Collections.Generic;
    using Godot;
    /// <summary>Cached draw commands for one static map chunk. Rebuilt only when the scene changes.</summary>
    public partial class TileCanvasLayer : Node2D
    {
        private ITileProjection projection;
        private readonly List<TileVisual> visuals = new();
        public TileCanvasLayer(ITileProjection projection) { this.projection = projection; }
        public void Add(TileVisual visual) => visuals.Add(visual);
        public bool Matches(IReadOnlyList<TileVisual> other)
        {
            if (visuals.Count != other.Count) return false;
            for (var i = 0; i < visuals.Count; i++)
            {
                var a = visuals[i]; var b = other[i];
                if (!ReferenceEquals(a.Texture, b.Texture) || a.Source != b.Source || a.Bounds != b.Bounds || a.Tint != b.Tint) return false;
                if ((a.WorldQuad == null) != (b.WorldQuad == null)) return false;
                if (a.WorldQuad != null)
                {
                    if (a.WorldQuad.Length != b.WorldQuad.Length) return false;
                    for (var p = 0; p < a.WorldQuad.Length; p++) if (a.WorldQuad[p] != b.WorldQuad[p]) return false;
                }
            }
            return true;
        }
        public void Replace(IEnumerable<TileVisual> other) { visuals.Clear(); visuals.AddRange(other); QueueRedraw(); }
        public void Release() { projection = null; visuals.Clear(); }
        public override void _Draw()
        {
            foreach (var visual in visuals) TileDrawing.Draw(this, visual, projection, visual.Tint);
        }
    }
}
