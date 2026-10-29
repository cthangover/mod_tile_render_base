namespace Cthangover.Mods.TileRender
{
    using Godot;
    /// <summary>Cosmetic world-space accents and the exact interaction cell supplied by the simulation.</summary>
    public partial class TileCanvasEffects : Node2D
    {
        private TileRenderScene scene;
        private ITileProjection projection;
        private Vector2? target;
        private double elapsed;
        public TileCanvasEffects(TileRenderScene scene, ITileProjection projection)
        { this.scene = scene; this.projection = projection; }
        public void Present(Vector2? cell, double delta)
        { target = cell; elapsed += delta; QueueRedraw(); }
        public void Release() { scene = null; projection = null; }
        public void SetScene(TileRenderScene snapshot) { scene = snapshot; QueueRedraw(); }
        public override void _Draw()
        {
            var time = (float)elapsed;
            if (target.HasValue)
            {
                var points = TileDrawing.CellPolygon(projection, new Rect2(target.Value + Vector2.One * 0.06f, Vector2.One * 0.88f));
                var color = new Color(1f, 0.83f, 0.48f, 0.65f + 0.2f * Mathf.Sin(time * 3));
                for (var i = 0; i < 4; i++)
                {
                    DrawLine(points[i], points[i].Lerp(points[(i + 1) % 4], 0.2f), color);
                    DrawLine(points[i], points[i].Lerp(points[(i + 3) % 4], 0.2f), color);
                }
            }
            if (scene.Atmosphere != "woodland") return;
            for (var i = 0; i < 36; i++)
            {
                var point = new Vector2(Mathf.PosMod(i * 4.29f + time * (2 + i % 3) / 32, scene.Size.X),
                    Mathf.PosMod(i * 2.61f + time * 0.044f + Mathf.Sin(time * 0.4f + i) * 0.22f, scene.Size.Y));
                var pixel = TileDrawing.Project(projection, point);
                DrawLine(pixel, pixel + new Vector2(2, 1), new Color(0.77f, 0.78f, 0.4f, 0.32f));
            }
            for (var i = 0; i < scene.Water.Count; i += 2)
            {
                var point = TileDrawing.Project(projection, scene.Water[i] + new Vector2(0.3f, 0.3f));
                DrawLine(point, point + new Vector2(6, 0), new Color(0.63f, 0.87f, 0.84f, 0.1f + 0.1f * Mathf.Sin(time + i)));
            }
        }
    }
}
