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

        private TileVisual shown;

        private bool shownShadow;

        private Vector2? visibilityCell;

        /// <summary>Borrows the presentation's projection; the parent tree owns the created drawing node.</summary>
        public TileCanvasSprite(ITileProjection projection)
        {
            this.projection = projection;
        }

        /// <summary>Detaches borrowed graphics before the owning presentation frees this node and its resources.</summary>
        public void Release()
        {
            projection = null;
            visual = null;
            shown = null;
            staticPlacement = null;
        }

        /// <summary>Installs a borrowed immutable static look, including its independent visibility sample.</summary>
        public void SetStatic(TileVisual image)
        {
            visual = image;
            Modulate = image.Tint;
            source = image.Source;
            staticPlacement = projection.Place(image);
            Position = staticPlacement.Anchor;
            SetVisibilityCell(image.VisibilityCell);
            SetQuadVisibility(image);
            QueueRedraw();
        }

        /// <summary>Copies an evaluated pose without advancing simulation or retaining the reusable frame itself.</summary>
        public void SetPose(TileActorPose pose)
        {
            Visible = pose.Visible;
            Modulate = pose.Tint;
            visual = pose.Look;
            SetVisibilityCell(visual?.VisibilityCell is Vector2 sample ? pose.Position + sample
                : pose.Charset ? pose.Position + Vector2.One / 2 : null);
            if (visual == null)
            {
                if (shown != null)
                {
                    shown = null;
                    QueueRedraw();
                }

                return;
            }

            // Only walking characters move to the projection's stand point; a dynamic prop keeps the anchor its static twin uses.
            var anchor = pose.Charset ? projection.ActorAnchor(visual.Anchor) : visual.Anchor;
            Position = TileDrawing.Project(projection, pose.Position + anchor);
            var next = visual.Source;
            shadow = pose.Charset;
            if (pose.Charset && visual.Texture != null)
            {
                next = TileWalkSheet.Frame(visual.Texture, pose.Column, projection.CharsetRow(pose.Facing));
            }

            if (source != next || visual != shown || shadow != shownShadow)
            {
                source = next;
                shown = visual;
                shownShadow = shadow;
                QueueRedraw();
            }
        }

        /// <summary>Draws borrowed artwork with its static placement or the last evaluated pose; no simulation runs here.</summary>
        public override void _Draw()
        {
            if (visual?.Texture == null)
            {
                return;
            }

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

        private void SetVisibilityCell(Vector2? cell)
        {
            if (visibilityCell == cell)
            {
                return;
            }

            visibilityCell = cell;
            SetInstanceShaderParameter("visibility_anchor_enabled", cell.HasValue);
            SetInstanceShaderParameter("visibility_anchor", cell ?? Vector2.Zero);
        }

        private void SetQuadVisibility(TileVisual image)
        {
            var enabled = false;
            if (!image.VisibilityCell.HasValue && image.WorldQuad is { Length: 4 } && image.Texture != null)
            {
                var origin = projection.Project(image.WorldQuad[0]);
                var x = projection.Project(image.WorldQuad[1]) - origin;
                var y = projection.Project(image.WorldQuad[3]) - origin;
                enabled = staticPlacement.Transform.Origin.IsEqualApprox(origin)
                    && staticPlacement.Transform.X.IsEqualApprox(x) && staticPlacement.Transform.Y.IsEqualApprox(y);
                if (enabled)
                {
                    var first = new Vector2(image.WorldQuad[0].X, image.WorldQuad[0].Y);
                    var right = new Vector2(image.WorldQuad[1].X, image.WorldQuad[1].Y);
                    var bottom = new Vector2(image.WorldQuad[3].X, image.WorldQuad[3].Y);
                    var size = image.Texture.GetSize();
                    SetInstanceShaderParameter("visibility_uv_origin", first);
                    SetInstanceShaderParameter("visibility_uv_x", right - first);
                    SetInstanceShaderParameter("visibility_uv_y", bottom - first);
                    SetInstanceShaderParameter("visibility_uv_region", new Vector4(image.Source.Position.X / size.X,
                        image.Source.Position.Y / size.Y, image.Source.Size.X / size.X, image.Source.Size.Y / size.Y));
                }
            }

            SetInstanceShaderParameter("visibility_uv_enabled", enabled);
        }
    }
}
