namespace Cthangover.Mods.TileRender
{
    using Godot;
    /// <summary>Projection output, consumed without knowing a coordinate system: an affine quad and optional sort anchor.</summary>
    public class TileVisualPlacement
    {
        public Transform2D Transform { get; private set; }
        public Rect2 Bounds { get; private set; }
        public Vector2 Anchor { get; private set; }
        public bool DepthSorted { get; private set; }
        public static TileVisualPlacement FromQuad(Vector2[] points, Vector2 anchor, bool depthSorted)
        {
            var bounds = new Rect2(points[0], Vector2.Zero);
            foreach (var point in points) bounds = bounds.Expand(point);
            return new TileVisualPlacement { Transform = new Transform2D(points[1] - points[0], points[3] - points[0], points[0]),
                Bounds = bounds, Anchor = anchor, DepthSorted = depthSorted };
        }
    }
}
