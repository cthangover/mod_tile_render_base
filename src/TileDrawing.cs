namespace Cthangover.Mods.TileRender
{
    using Godot;
    /// <summary>Shared UV drawing for runtime and tools. Ground transforms independently of image storage.</summary>
    public static class TileDrawing
    {
        public static Vector2 Project(ITileProjection p, Vector2 point) => p.Project(new Vector3(point.X, point.Y, 0));

        public static Vector2[] CellPolygon(ITileProjection p, Rect2 rect)
        {
            return new[] { Project(p, rect.Position), Project(p, rect.Position + new Vector2(rect.Size.X, 0)),
                Project(p, rect.End), Project(p, rect.Position + new Vector2(0, rect.Size.Y)) };
        }

        public static void Draw(CanvasItem target, TileVisual visual, ITileProjection projection, Color tint)
        {
            if (visual.Texture == null)
                return;
            var placement = projection.Place(visual);
            target.DrawSetTransformMatrix(placement.Transform);
            target.DrawTextureRectRegion(visual.Texture, new Rect2(0, 0, 1, 1), visual.Source, tint);
            target.DrawSetTransformMatrix(Transform2D.Identity);
        }
    }
}
