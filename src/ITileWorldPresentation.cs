namespace Cthangover.Mods.TileRender
{
    using System;
    using Godot;

    /// <summary>A replaceable view. No operation may advance gameplay or write a save.</summary>
    public interface ITileWorldPresentation : IDisposable
    {
        /// <summary>Host attaches this root; the presentation owns all its descendants.</summary>
        Node2D Root { get; }
        /// <summary>Material shared by static and dynamic artwork, exposed for diagnostics.</summary>
        ShaderMaterial SharedMaterial { get; }
        /// <summary>Number of computed surface textures actually submitted to static draw chunks.</summary>
        int SurfaceChunkCount { get; }
        /// <summary>Projected static extent before camera scaling, for scrollable editor canvases.</summary>
        Rect2 ContentBounds { get; }
        /// <summary>Verifies the visible actors inherit the same material as the terrain.</summary>
        bool ActorsShareMaterial { get; }
        /// <summary>Builds static geometry from resolved visual data.</summary>
        void Mount(TileRenderScene scene);
        /// <summary>Applies a newer snapshot while reusing unaffected GPU textures and cached commands.</summary>
        void Apply(TileSceneChanges changes);
        /// <summary>Copies poses, camera focus and lighting without ticking simulation.</summary>
        void Present(TileFrame frame, double delta);
        /// <summary>Sets the visible host rectangle.</summary>
        void Resize(Vector2 size);
        /// <summary>Coordinates are relative to the host, including camera displacement.</summary>
        bool TryPickGround(Vector2 localPoint, out Vector2I cell);
        /// <summary>Projects a cell point into host coordinates.</summary>
        Vector2 ProjectToView(Vector3 point);
        /// <summary>Releases map resources; safe to call more than once.</summary>
        void Unmount();
    }
}
