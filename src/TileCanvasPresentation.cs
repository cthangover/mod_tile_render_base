using System;
using System.Collections.Generic;
using Cthangover.Core.Mods.Resolvers;
using Godot;

namespace Cthangover.Mods.TileRender
{
    /// <summary>
    /// Shared canvas backend: projections live in separate implementation mods.
    /// Static ground is chunked, whole props and actors share ground-anchor sorting.
    /// Camera and light transforms are derived from the same projection used for picking.
    /// In the game (<see cref = "TileFrame.CameraOnArtwork"/>) the camera stops at the drawn artwork, not at the empty grid,
    /// so a map drawn as one picture never scrolls into black; editors frame the whole grid.
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

        private readonly List<string> actorOrder = new();

        private readonly HashSet<string> seenActors = new();

        private readonly List<string> removedActors = new();

        /// <summary>
        /// Replaced or removed ground chunks in the last mount/delta; unchanged cached chunks do not increase this count.
        /// </summary>
        public int LastChangedDrawChunks { get; private set; }

        private readonly Dictionary<string, TileCanvasSprite> sprites = new();

        private readonly Vector2[] lightPositions = new Vector2[16];

        private readonly Color[] lightColors = new Color[16];

        private readonly float[] lightRadii = new float[16];

        private readonly int[] lightSpaces = new int[16];

        private Vector2 viewport;

        private Rect2 bounds;
        // Where the camera may go: the union of the placed visuals (the cell polygon when none is placed).
        private Rect2 cameraBounds;

        private TileCanvasEffects effects;

        /// <summary>Borrowed attachment root; the presentation owns its descendants and detaches/frees them on disposal.</summary>
        public Node2D Root
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Borrowed lighting material shared by terrain and actors; the presentation disposes it after native draw
        /// references retire.
        /// </summary>
        public ShaderMaterial SharedMaterial { get; private set; }
        /// <summary>
        /// Surface textures submitted through static draw chunks; upright props and actor textures are counted separately
        /// by callers.
        /// </summary>
        public int SurfaceChunkCount { get; private set; }

        /// <summary>Projected map/artwork bounds before outer host scaling, used by editor fitting and camera limits.</summary>
        public Rect2 ContentBounds
        {
            get
            {
                return bounds;
            }
        }

        /// <summary>Weak diagnostic only; does not retain implementation code during unload verification.</summary>
        public WeakReference ProjectionLifetime()
        {
            return new(projection);
        }

        /// <summary>
        /// True only with presented actors whose native sprites inherit the terrain lighting material; false for an empty
        /// actor set.
        /// </summary>
        public bool ActorsShareMaterial
        {
            get
            {
                if (actors == null || actors.Material != SharedMaterial || sprites.Count == 0)
                {
                    return false;
                }

                foreach (var sprite in sprites.Values)
                {
                    if (!sprite.UseParentMaterial)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Stores the projection factory for this owned native view; callers create and operate it on the Godot main
        /// thread.
        /// </summary>
        public TileCanvasPresentation(ITileRendererFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Replaces native presentation for a borrowed snapshot, preserving its source textures and owning only renderer
        /// nodes/materials.
        /// </summary>
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
            foreach (var point in corners)
            {
                bounds = bounds.Expand(point);
            }

            SharedMaterial = new ShaderMaterial
            {
                Shader = Shaders.Resolve("tile_lit")
            };
            layers = new Node2D[3];
            layers[0] = new Node2D
            {
                Name = "Back",
                Material = SharedMaterial
            };
            layers[1] = new Node2D
            {
                Name = "Main",
                Material = SharedMaterial
            };
            layers[2] = new Node2D
            {
                Name = "Front",
                Material = SharedMaterial
            };
            AddChild(layers[0]);
            AddChild(layers[1]);
            actors = new Node2D
            {
                Name = "Actors",
                YSortEnabled = true,
                Material = SharedMaterial
            };
            AddChild(actors);
            AddChild(layers[2]);
            RebuildStatic();
            effects = new TileCanvasEffects(scene, projection);
            AddChild(effects);
        }

        /// <summary>
        /// Rejects stale revisions, remounts changed map geometry and otherwise retains unchanged chunks; equal-depth order
        /// remains stable.
        /// </summary>
        public void Apply(TileSceneChanges changes)
        {
            if (scene == null || changes.BaseRevision != scene.Revision || changes.Scene.Revision <= scene.Revision)
            {
                throw new InvalidOperationException("Stale tile presentation delta.");
            }

            var next = changes.Scene;
            if (next.Size != scene.Size || next.TileSize != scene.TileSize || next.Zoom != scene.Zoom)
            {
                Mount(next);
                return;
            }

            scene = next;
            RebuildStatic();
            effects.SetScene(scene);
        }

        private void RebuildStatic()
        {
            var corners = TileDrawing.CellPolygon(projection, new Rect2(Vector2.Zero, scene.Size));
            bounds = new Rect2(corners[0], Vector2.Zero);
            foreach (var point in corners)
            {
                bounds = bounds.Expand(point);
            }

            var cellBounds = bounds;
            var artworkBounds = new Rect2();
            var anyPlaced = false;
            foreach (var prop in props)
            {
                prop.Release();
                prop.GetParent()?.RemoveChild(prop);
                prop.Free();
            }

            props.Clear();
            SurfaceChunkCount = 0;
            LastChangedDrawChunks = 0;
            var nextChunks = new Dictionary<(int Layer, int X, int Y), List<TileVisual>>();
            foreach (var visual in scene.Visuals)
            {
                if (!string.IsNullOrEmpty(visual.Only) && visual.Only != factory.Id)
                {
                    continue;
                }

                var placement = projection.Place(visual);
                bounds = bounds.Merge(placement.Bounds);
                artworkBounds = anyPlaced ? artworkBounds.Merge(placement.Bounds) : placement.Bounds;
                anyPlaced = true;
                if (placement.DepthSorted || visual.DrawOrder.HasValue)
                {
                    var sprite = new TileCanvasSprite(projection)
                    {
                        UseParentMaterial = true
                    };
                    var parent = visual.DrawOrder == TileDrawOrder.BelowCharacters ? layers[0]
                        : visual.DrawOrder == TileDrawOrder.AboveCharacters ? layers[2]
                        : visual.DrawOrder == TileDrawOrder.ByPosition ? actors : visual.Layer == 2 ? layers[2] : actors;
                    parent.AddChild(sprite);
                    sprite.SetStatic(visual);
                    props.Add(sprite);
                }
                else
                {
                    var key = (visual.Layer, (int)visual.Bounds.Position.X / 32, (int)visual.Bounds.Position.Y / 32);
                    if (!nextChunks.TryGetValue(key, out var list))
                    {
                        nextChunks[key] = list = new List<TileVisual>();
                    }

                    list.Add(visual);
                    if (visual.Id?.StartsWith("surface/") == true && visual.Texture != null)
                    {
                        SurfaceChunkCount++;
                    }
                }
            }

            cameraBounds = anyPlaced ? artworkBounds : cellBounds;
            var obsolete = new List<(int Layer, int X, int Y)>();
            foreach (var pair in chunks)
            {
                if (!nextChunks.ContainsKey(pair.Key))
                {
                    obsolete.Add(pair.Key);
                }
            }

            foreach (var key in obsolete)
            {
                var chunk = chunks[key];
                chunk.Release();
                chunk.GetParent()?.RemoveChild(chunk);
                chunk.Free();
                chunks.Remove(key);
                LastChangedDrawChunks++;
            }

            foreach (var pair in nextChunks)
            {
                if (!chunks.TryGetValue(pair.Key, out var chunk))
                {
                    chunk = new TileCanvasLayer(projection)
                    {
                        UseParentMaterial = true
                    };
                    layers[pair.Key.Layer].AddChild(chunk);
                    chunks[pair.Key] = chunk;
                }
                else if (chunk.Matches(pair.Value))
                {
                    continue;
                }

                chunk.Replace(pair.Value);
                LastChangedDrawChunks++;
            }
            ReorderDepthChildren();
        }

        /// <summary>
        /// Updates the available camera rectangle; the next Present computes its position without changing gameplay
        /// coordinates.
        /// </summary>
        public void Resize(Vector2 size)
        {
            viewport = size;
        }

        /// <summary>
        /// Copies evaluated poses and lighting, preserving static-before-actor ties and frame pose order; delta smooths
        /// only the camera.
        /// </summary>
        public void Present(TileFrame frame, double delta)
        {
            if (scene == null)
            {
                return;
            }

            seenActors.Clear();
            foreach (var pose in frame.Actors)
            {
                if (!sprites.TryGetValue(pose.Id, out var sprite))
                {
                    sprite = new TileCanvasSprite(projection)
                    {
                        UseParentMaterial = true
                    };
                    actors.AddChild(sprite);
                    sprites[pose.Id] = sprite;
                }

                seenActors.Add(pose.Id);
                sprite.SetPose(pose);
            }

            removedActors.Clear();
            foreach (var pair in sprites)
            {
                if (!seenActors.Contains(pair.Key))
                {
                    removedActors.Add(pair.Key);
                }
            }

            foreach (var id in removedActors)
            {
                var sprite = sprites[id];
                sprite.Release();
                sprite.GetParent()?.RemoveChild(sprite);
                sprite.Free();
                sprites.Remove(id);
            }

            RestoreDepthOrder(frame);
            var stand = TileDrawing.Project(projection, frame.Focus);
            // The player is never lost when it walks outside the artwork.
            var area = frame.CameraOnArtwork ? cameraBounds.Expand(stand) : bounds;
            var focus = stand * Scale.X;
            var target = new Vector2(Axis(viewport.X, area.Position.X * Scale.X, area.End.X * Scale.X, focus.X),
                Axis(viewport.Y, area.Position.Y * Scale.Y, area.End.Y * Scale.Y, focus.Y));
            // Keep subpixel precision throughout the chase: feeding rounded pixels back
            // into interpolation stalls short movements at high frame rates.
            Position = delta <= 0 ? target : Position.Lerp(target, 1f - Mathf.Exp(-(float)delta * 20f));
            if (Position.DistanceSquaredTo(target) <= 0.01f)
            {
                Position = target;
            }

            UpdateLight(frame);
            effects.Present(frame.InteractionCell, delta);
        }

        private void RestoreDepthOrder(TileFrame frame)
        {
            var changed = actorOrder.Count != frame.Actors.Count;
            if (!changed)
            {
                for (var index = 0; index < actorOrder.Count; index++)
                {
                    if (actorOrder[index] != frame.Actors[index].Id)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            actorOrder.Clear();
            foreach (var pose in frame.Actors)
            {
                actorOrder.Add(pose.Id);
            }

            ReorderDepthChildren();
        }

        private void ReorderDepthChildren()
        {
            // Equal Y positions use sibling order. Rebuilding props preserves authored part order,
            // then the last presented pose order, instead of placing new props in front of existing actors.
            var next = 0;
            foreach (var prop in props)
            {
                if (prop.GetParent() == actors)
                {
                    actors.MoveChild(prop, next++);
                }
            }

            foreach (var id in actorOrder)
            {
                actors.MoveChild(sprites[id], next++);
            }
        }

        private void UpdateLight(TileFrame frame)
        {
            SharedMaterial.SetShaderParameter("portable_position", frame.PortableLight?.Position ?? new Vector2(-1000, -1000));
            SharedMaterial.SetShaderParameter("portable_color", frame.PortableLight?.Color ?? new Color(0, 0, 0, 0));
            SharedMaterial.SetShaderParameter("portable_radius", frame.PortableLight?.Radius ?? 0);
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
                var light = frame.Lights[i];
                lightSpaces[i] = light.ViewSpace ? 1 : 0;
                lightPositions[i] = light.ViewSpace ? GlobalTransform * TileDrawing.Project(projection, light.Position) : light.Position;
                lightColors[i] = light.Color;
                lightRadii[i] = light.ViewSpace ? light.Radius * projection.Unit * GlobalTransform.X.Length() : light.Radius;
            }

            SharedMaterial.SetShaderParameter("light_spaces", lightSpaces);
            SharedMaterial.SetShaderParameter("light_count", count);
            SharedMaterial.SetShaderParameter("light_positions", lightPositions);
            SharedMaterial.SetShaderParameter("light_colors", lightColors);
            SharedMaterial.SetShaderParameter("light_radii", lightRadii);
        }

        /// <summary>Inverts the same projected camera transform as drawing, returning only cells inside the mounted map.</summary>
        public bool TryPickGround(Vector2 localPoint, out Vector2I cell)
        {
            cell = default;
            if (scene == null)
            {
                return false;
            }

            var point = projection.Unproject(Transform.AffineInverse() * localPoint);
            cell = new Vector2I(Mathf.FloorToInt(point.X), Mathf.FloorToInt(point.Y));
            return cell.X >= 0 && cell.Y >= 0 && cell.X < scene.Size.X && cell.Y < scene.Size.Y;
        }

        /// <summary>Projects cell coordinates and elevation through the current camera for picking and diagnostic overlays.</summary>
        public Vector2 ProjectToView(Vector3 point)
        {
            return Transform * projection.Project(point);
        }

        /// <summary>
        /// Drops native draw references before freeing owned children/material; borrowed snapshot textures remain with
        /// their caller.
        /// </summary>
        public void Unmount()
        {
            foreach (var child in GetChildren())
            {
                ReleaseReferences(child);
                RemoveChild(child);
                child.Free();
            }

            sprites.Clear();
            props.Clear();
            actorOrder.Clear();
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
            foreach (var child in node.GetChildren())
            {
                ReleaseReferences(child);
            }

            if (node is TileCanvasLayer layer)
            {
                layer.Release();
            }

            if (node is TileCanvasSprite sprite)
            {
                sprite.Release();
            }

            if (node is TileCanvasEffects accents)
            {
                accents.Release();
            }
        }

        /// <summary>
        /// Detaches the owned root and retires native children before releasing the factory reference; repeated disposal is
        /// safe.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (!GodotObject.IsInstanceValid(this))
            {
                return;
            }

            Unmount();
            factory = null;
            GetParent()?.RemoveChild(this);
            Free();
        }

        private static float Axis(float screen, float start, float end, float focus)
        {
            return end - start <= screen ? (screen - start - end) / 2f : Mathf.Clamp(screen / 2f - focus, screen - end, -start);
        }
    }
}
