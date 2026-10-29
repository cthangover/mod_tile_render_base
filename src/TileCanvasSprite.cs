namespace Cthangover.Mods.TileRender
{
    using Godot;
    /// <summary>Upright image sorted at its ground anchor. A pose update never advances movement or animation.</summary>
    public partial class TileCanvasSprite : Node2D
    {
        private ITileProjection projection;
        private TileVisual visual;
        private Rect2 source;
        private bool shadow;
        private TileVisualPlacement staticPlacement;
        public TileCanvasSprite(ITileProjection projection) { this.projection = projection; }
        public void Release() { projection = null; visual = null; shown = null; staticPlacement = null; }

        public void SetStatic(TileVisual image)
        {
            visual = image;
            Modulate = image.Tint;
            source = image.Source;
            staticPlacement = projection.Place(image);
            Position = staticPlacement.Anchor;
            QueueRedraw();
        }

        public void SetPose(TileActorPose pose)
        {
            Visible = pose.Visible;
            Modulate = pose.Tint;
            visual = pose.Look;
            if (visual == null)
            {
                if (shown != null) { shown = null; QueueRedraw(); }
                return;
            }
            Position = TileDrawing.Project(projection, pose.Position + visual.Anchor);
            var next = visual.Source;
            shadow = pose.Charset;
            if (pose.Charset && visual.Texture != null)
            {
                var frame = visual.Texture.GetSize() / new Vector2(3, 4);
                next = new Rect2(new Vector2(pose.Column, projection.CharsetRow(pose.Facing)) * frame, frame);
            }
            if (source != next || visual != shown || shadow != shownShadow)
            {
                source = next;
                shown = visual;
                shownShadow = shadow;
                QueueRedraw();
            }
        }
        private TileVisual shown;
        private bool shownShadow;

        public override void _Draw()
        {
            if (visual?.Texture == null) return;
            if (staticPlacement != null)
            {
                var transform = staticPlacement.Transform;
                transform.Origin -= Position;
                DrawSetTransformMatrix(transform);
                DrawTextureRectRegion(visual.Texture, new Rect2(0, 0, 1, 1), source, Colors.White);
                DrawSetTransformMatrix(Transform2D.Identity);
                return;
            }
            var unit = projection.Unit;
            if (shadow)
            {
                DrawSetTransform(new Vector2(0, -3), 0, new Vector2(1, 0.32f));
                DrawCircle(Vector2.Zero, unit * 0.28f, new Color(0.04f, 0.09f, 0.1f, 0.28f));
                DrawSetTransform(Vector2.Zero);
            }
            DrawTextureRectRegion(visual.Texture, new Rect2((visual.Bounds.Position - visual.Anchor) * unit,
                visual.Bounds.Size * unit), source, Colors.White);
        }
    }
}
