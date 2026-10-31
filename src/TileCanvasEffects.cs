namespace Cthangover.Mods.TileRender
{
    using Godot;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Cthangover.Mods.Effects.Atmosphere;
    /// <summary>Owns global and painted atmosphere hosts. Each mask shares one particle budget, regardless of cell count.</summary>
    public partial class TileCanvasEffects : Node2D
    {
        private TileRenderScene scene;
        private ITileProjection projection;
        private Vector2? target;
        private double elapsed;
        private readonly List<AtmosphereView> hosts = new();
        public TileCanvasEffects(TileRenderScene scene, ITileProjection projection)
        { this.projection = projection; SetScene(scene); }
        public void Present(Vector2? cell, double delta)
        { target = cell; elapsed += delta; foreach (var host in hosts) host.Advance(delta); QueueRedraw(); }
        public void Release()
        {
            foreach (var host in hosts) { host.Release(); RemoveChild(host); host.Free(); }
            hosts.Clear(); scene = null;
        }
        public void SetScene(TileRenderScene snapshot)
        {
            scene = snapshot;
            var placements = new List<AtmospherePlacement>();
            if (!string.IsNullOrEmpty(scene.Atmosphere)) placements.Add(new AtmospherePlacement { Profile = scene.Atmosphere });
            placements.AddRange(scene.Effects ?? new List<AtmospherePlacement>());
            for (var i = hosts.Count; i < placements.Count; i++) { var host = new AtmosphereView(); AddChild(host); hosts.Add(host); }
            while (hosts.Count > placements.Count) { var host = hosts[^1]; hosts.RemoveAt(hosts.Count - 1); host.Release(); RemoveChild(host); host.Free(); }
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var cells = placement.Cells?.Where(c => c?.Length == 2 && c[0] >= 0 && c[1] >= 0 && c[0] < scene.Size.X && c[1] < scene.Size.Y)
                    .Select(c => new Vector2(c[0], c[1])).Distinct().ToList();
                hosts[i].Configure(placement.Profile, new AtmosphereContext {
                    Bounds = new Rect2(Vector2.Zero, scene.Size), Project = projection.Project, SpawnCells = cells,
                    Regions = new Dictionary<string, IReadOnlyList<Vector2>> { ["water"] = cells ?? scene.Water, ["painted"] = cells ?? scene.Water }
                });
            }
            QueueRedraw();
        }
        public override void _Draw()
        {
            if (!target.HasValue || projection == null) return;
            var points = TileDrawing.CellPolygon(projection, new Rect2(target.Value + Vector2.One * 0.06f, Vector2.One * 0.88f));
            var color = new Color(1f, 0.83f, 0.48f, 0.65f + 0.2f * Mathf.Sin((float)elapsed * 3));
            for (var i = 0; i < 4; i++) {
                DrawLine(points[i], points[i].Lerp(points[(i + 1) % 4], 0.2f), color);
                DrawLine(points[i], points[i].Lerp(points[(i + 3) % 4], 0.2f), color);
            }
        }
    }
}
