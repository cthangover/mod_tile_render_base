namespace Cthangover.Mods.TileRender
{
    using Godot;

    /// <summary>Maps cell-space geometry to pixels; it has no knowledge of RPG rules or artwork.</summary>
    public interface ITileProjection
    {
        /// <summary>Projection-owned image placement policy; the backend never switches coordinate systems.</summary>
        TileVisualPlacement Place(TileVisual visual);
        /// <summary>Source-pixel scale, independent of camera zoom.</summary>
        float Unit { get; }
        /// <summary>Projects ground coordinates and optional elevation in cells.</summary>
        Vector2 Project(Vector3 point);
        /// <summary>Inverts ground projection before flooring to a cell.</summary>
        Vector2 Unproject(Vector2 point);
        /// <summary>Maps world facing (down/left/right/up) to a four-row charset.</summary>
        int CharsetRow(int facing);
    }
}
