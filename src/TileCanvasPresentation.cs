namespace Cthangover.Mods.TileRender
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Mods.Resolvers;
    using Godot;

    /// <summary>
    /// Shared canvas backend: projections live in separate implementation mods.
    /// Static ground is chunked, whole props and actors share ground-anchor sorting.
    /// Camera and light transforms are derived from the same projection used for picking.
    /// </summary>
    public partial class TileCanvasPresentation : Node2D, ITileWorldPresentation
    {
        private ITileRendererFactory factory;
        private ITileProjection projection;
        private TileRenderScene scene;
        private Node2D actors;
        private Node2D[] layers;
        private readonly Dictionary<(int Layer, int X, int Y), TileCanvasLayer> chunks = new();
        private readonly List<TileCanvasSprite> props = new();
        private readonly HashSet<string> seenActors = new();
        private readonly List<string> removedActors = new();
        public int LastChangedDrawChunks { get; private set; }
        private readonly Dictionary<string, TileCanvasSprite> sprites = new();
        private readonly Vector2[] lightPositions = new Vector2[16];
        private readonly Color[] lightColors = new Color[16];
        private readonly float[] lightRadii = new float[16];
        private Vector2 viewport;
        private Rect2 bounds;
        private TileCanvasEffects effects;
        public Node2D Root => this;
        public ShaderMaterial SharedMaterial { get; private set; }
        public int SurfaceChunkCount { get; private set; }
        public Rect2 ContentBounds => bounds;
        /// <summary>Weak diagnostic only; does not retain implementation code during unload verification.</summary>
        public WeakReference ProjectionLifetime() => new(projection);
        public bool ActorsShareMaterial
        {
            get
            {
                if (actors == null || actors.Material != SharedMaterial || sprites.Count == 0) return false;
                foreach (var sprite in sprites.Values) if (!sprite.UseParentMaterial) return false;
                return true;
            }
        }
        public TileCanvasPresentation(ITileRendererFactory factory) { this.factory = factory; }

        public void Mount(TileRenderScene snapshot)
        {
            Unmount();
            scene = snapshot;
            projection = factory.Projection(scene.TileSize);
            Name = "TilePresentation";
            TextureFilter = TextureFilterEnum.Nearest;
            Scale = Vector2.One * scene.Zoom;
            var corners = TileDrawing.CellPolygon(projection, new Rect2(Vector2.Zero, scene.Size));
            bounds = new Rect2(corners[0], Vector2.Zero);
            foreach (var point in corners) bounds = bounds.Expand(point);
            SharedMaterial = new ShaderMaterial { Shader = Shaders.Resolve("tile_lit") };
            layers = new Node2D[3];
            layers[0] = new Node2D { Name = "Back", Material = SharedMaterial };
            layers[1] = new Node2D { Name = "Main", Material = SharedMaterial };
            layers[2] = new Node2D { Name = "Front", Material = SharedMaterial };
            AddChild(layers[0]);
            AddChild(layers[1]);
            actors = new Node2D { Name = "Actors", YSortEnabled = true, Material = SharedMaterial };
            AddChild(actors);
            AddChild(layers[2]);
            RebuildStatic();
            effects = new TileCanvasEffects(scene, projection);
            AddChild(effects);
        }

        public void Apply(TileSceneChanges changes)
        {
            if (scene == null || changes.BaseRevision != scene.Revision || changes.Scene.Revision <= scene.Revision)
                throw new InvalidOperationException("Stale tile presentation delta.");
            var next = changes.Scene;
            if (next.Size != scene.Size || next.TileSize != scene.TileSize || next.Zoom != scene.Zoom)
            { Mount(next); return; }
            scene = next;
            RebuildStatic();
            effects.SetScene(scene);
        }

        private void RebuildStatic()
        {
            var corners = TileDrawing.CellPolygon(projection, new Rect2(Vector2.Zero, scene.Size));
            bounds = new Rect2(corners[0], Vector2.Zero);
            foreach (var point in corners) bounds = bounds.Expand(point);
            foreach (var prop in props) { prop.Release(); prop.GetParent()?.RemoveChild(prop); prop.Free(); }
            props.Clear();
            SurfaceChunkCount = 0;
            LastChangedDrawChunks = 0;
            var nextChunks = new Dictionary<(int Layer, int X, int Y), List<TileVisual>>();
            foreach (var visual in scene.Visuals)
            {
                if (!string.IsNullOrEmpty(visual.Only) && visual.Only != factory.Id) continue;
                var placement = projection.Place(visual);
                bounds = bounds.Merge(placement.Bounds);
                if (placement.DepthSorted)
                {
                    var sprite = new TileCanvasSprite(projection) { UseParentMaterial = true };
                    (visual.Layer == 2 ? layers[2] : actors).AddChild(sprite);
                    sprite.SetStatic(visual);
                    props.Add(sprite);
                }
                else
                {
                    var key = (visual.Layer, (int)visual.Bounds.Position.X / 32, (int)visual.Bounds.Position.Y / 32);
                    if (!nextChunks.TryGetValue(key, out var list)) nextChunks[key] = list = new List<TileVisual>();
                    list.Add(visual);
                    if (visual.Id?.StartsWith("surface/") == true && visual.Texture != null) SurfaceChunkCount++;
                }
            }
            var obsolete = new List<(int Layer, int X, int Y)>();
            foreach (var pair in chunks) if (!nextChunks.ContainsKey(pair.Key)) obsolete.Add(pair.Key);
            foreach (var key in obsolete)
            {
                var chunk = chunks[key];
                chunk.Release(); chunk.GetParent()?.RemoveChild(chunk); chunk.Free(); chunks.Remove(key);
                LastChangedDrawChunks++;
            }
            foreach (var pair in nextChunks)
            {
                if (!chunks.TryGetValue(pair.Key, out var chunk))
                {
                    chunk = new TileCanvasLayer(projection) { UseParentMaterial = true };
                    layers[pair.Key.Layer].AddChild(chunk);
                    chunks[pair.Key] = chunk;
                }
                else if (chunk.Matches(pair.Value)) continue;
                chunk.Replace(pair.Value);
                LastChangedDrawChunks++;
            }
        }

        public void Resize(Vector2 size) => viewport = size;

        public void Present(TileFrame frame, double delta)
        {
            if (scene == null) return;
            seenActors.Clear();
            foreach (var pose in frame.Actors)
            {
                if (!sprites.TryGetValue(pose.Id, out var sprite))
                {
                    sprite = new TileCanvasSprite(projection) { UseParentMaterial = true };
                    actors.AddChild(sprite);
                    sprites[pose.Id] = sprite;
                }
                seenActors.Add(pose.Id);
                sprite.SetPose(pose);
            }
            removedActors.Clear();
            foreach (var pair in sprites) if (!seenActors.Contains(pair.Key)) removedActors.Add(pair.Key);
            foreach (var id in removedActors)
            {
                var sprite = sprites[id];
                sprite.Release(); sprite.GetParent()?.RemoveChild(sprite); sprite.Free(); sprites.Remove(id);
            }
            var focus = TileDrawing.Project(projection, frame.Focus) * Scale.X;
            var target = new Vector2(Axis(viewport.X, bounds.Position.X * Scale.X, bounds.End.X * Scale.X, focus.X),
                Axis(viewport.Y, bounds.Position.Y * Scale.Y, bounds.End.Y * Scale.Y, focus.Y));
            Position = delta <= 0 ? target : Position.Lerp(target, 1f - Mathf.Exp(-(float)delta * 14f)).Round();
            UpdateLight(frame);
            effects.Present(frame.InteractionCell, delta);
        }

        private void UpdateLight(TileFrame frame)
        {
            SharedMaterial.SetShaderParameter("time_weights", frame.TimeWeights);
            SharedMaterial.SetShaderParameter("use_ambient", frame.Ambient.HasValue);
            SharedMaterial.SetShaderParameter("ambient_color", frame.Ambient ?? Colors.White);
            var world = GlobalTransform * new Transform2D(TileDrawing.Project(projection, Vector2.Right),
                TileDrawing.Project(projection, Vector2.Down), Vector2.Zero);
            var inverse = world.AffineInverse();
            SharedMaterial.SetShaderParameter("cell_x", new Vector2(inverse.X.X, inverse.Y.X));
            SharedMaterial.SetShaderParameter("cell_y", new Vector2(inverse.X.Y, inverse.Y.Y));
            SharedMaterial.SetShaderParameter("cell_offset", inverse.Origin);
            var count = Math.Min(16, frame.Lights.Count);
            for (var i = 0; i < count; i++)
            {
                lightPositions[i] = frame.Lights[i].Position;
                lightColors[i] = frame.Lights[i].Color;
                lightRadii[i] = frame.Lights[i].Radius;
            }
            SharedMaterial.SetShaderParameter("light_count", count);
            SharedMaterial.SetShaderParameter("light_positions", lightPositions);
            SharedMaterial.SetShaderParameter("light_colors", lightColors);
            SharedMaterial.SetShaderParameter("light_radii", lightRadii);
        }

        public bool TryPickGround(Vector2 localPoint, out Vector2I cell)
        {
            cell = default;
            if (scene == null) return false;
            var point = projection.Unproject(Transform.AffineInverse() * localPoint);
            cell = new Vector2I(Mathf.FloorToInt(point.X), Mathf.FloorToInt(point.Y));
            return cell.X >= 0 && cell.Y >= 0 && cell.X < scene.Size.X && cell.Y < scene.Size.Y;
        }

        public Vector2 ProjectToView(Vector3 point) => Transform * projection.Project(point);

        public void Unmount()
        {
            foreach (var child in GetChildren()) { ReleaseReferences(child); RemoveChild(child); child.Free(); }
            sprites.Clear();
            props.Clear();
            chunks.Clear();
            layers = null;
            SurfaceChunkCount = 0;
            actors = null;
            effects = null;
            scene = null;
            projection = null;
            SharedMaterial?.Dispose();
            SharedMaterial = null;
        }

        private static void ReleaseReferences(Node node)
        {
            foreach (var child in node.GetChildren()) ReleaseReferences(child);
            if (node is TileCanvasLayer layer) layer.Release();
            if (node is TileCanvasSprite sprite) sprite.Release();
            if (node is TileCanvasEffects accents) accents.Release();
        }

        void IDisposable.Dispose()
        {
            if (!GodotObject.IsInstanceValid(this)) return;
            Unmount();
            factory = null;
            GetParent()?.RemoveChild(this);
            Free();
        }

        private static float Axis(float screen, float start, float end, float focus)
            => Mathf.Round(end - start <= screen ? (screen - start - end) / 2f
                : Mathf.Clamp(screen / 2f - focus, screen - end, -start));
    }
}
