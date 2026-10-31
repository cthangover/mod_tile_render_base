namespace Cthangover.Mods.TileRender
{
    using Godot;

    /// <summary>
    /// The one description of a walking sheet (<c>charsets</c> group): 3 columns of animation frames ×
    /// 4 rows of facings, rows in world-facing order Down, Left, Right, Up — the same order as
    /// <c>RpgDirection</c>. A projection may draw another row for a facing
    /// (<see cref="ITileProjection.CharsetRow"/>); the sheet itself never changes. The runtime look of an
    /// actor, its drawing and the editors' preview icons all cut frames through this class, so a sheet
    /// of any pixel size works as long as it divides into 3×4 equal frames.
    /// </summary>
    public static class TileWalkSheet
    {
        /// <summary>Animation frames per facing.</summary>
        public const int Columns = 3;

        /// <summary>Facings: Down, Left, Right, Up.</summary>
        public const int Rows = 4;

        /// <summary>The standing frame, also the middle of the walk cycle.</summary>
        public const int IdleColumn = 1;

        /// <summary>Pixel size of one frame of a sheet of <paramref name="sheetSize"/> pixels.</summary>
        public static Vector2 FrameSize(Vector2 sheetSize) => sheetSize / new Vector2(Columns, Rows);

        /// <summary>Pixel region of frame (<paramref name="column"/>, <paramref name="row"/>); out-of-range values are clamped into the sheet.</summary>
        public static Rect2 Frame(Vector2 sheetSize, int column, int row)
        {
            var frame = FrameSize(sheetSize);
            var cell = new Vector2(Mathf.Clamp(column, 0, Columns - 1), Mathf.Clamp(row, 0, Rows - 1));
            return new Rect2(cell * frame, frame);
        }
    }
}
