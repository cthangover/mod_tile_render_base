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
        /// <summary>
        /// Maps a cell-relative actor anchor to the point the actor stands on. Looks are anchored at the bottom centre
        /// of the actor's cell (x + 0.5, y + 1), right for square cells but on the lower-left edge of an isometric
        /// diamond; each projection moves it to the ground point of its own cell so the character stands in that cell.
        /// </summary>
        Vector2 ActorAnchor(Vector2 anchor);
        /// <summary>Maps world facing (down/left/right/up) to a four-row charset.</summary>
        int CharsetRow(int facing);
    }
}
