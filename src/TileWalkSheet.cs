namespace Cthangover.Mods.TileRender
{
    using Godot;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// The shared description of a walking sheet (<c>charsets</c> group): configurable animation columns ×
    /// 4 rows of facings, rows in world-facing order Down, Left, Right, Up — the same order as
    /// <c>RpgDirection</c>. A projection may draw another row for a facing
    /// (<see cref="ITileProjection.CharsetRow"/>); the sheet itself never changes. The runtime look of an
    /// actor, its drawing and the editors' preview icons all cut frames through this class, so a sheet
    /// of any pixel size works as long as it divides into its configured columns × 4 equal frames.
    /// Without a sidecar the legacy three-column layout is used.
    /// </summary>
    public static class TileWalkSheet
    {
        /// <summary>Legacy animation frames per facing, used when no layout is supplied.</summary>
        public const int Columns = 3;

        /// <summary>Facings: Down, Left, Right, Up.</summary>
        public const int Rows = 4;

        /// <summary>The standing frame, also the middle of the walk cycle.</summary>
        public const int IdleColumn = 1;

        private static readonly ConditionalWeakTable<Texture2D, TileWalkAnimation> animations = new();
        private static readonly TileWalkAnimation legacy = new();

        /// <summary>Layout associated with a decoded texture, or the legacy default for unconfigured textures.</summary>
        public static TileWalkAnimation Animation(Texture2D texture)
            => texture != null && animations.TryGetValue(texture, out var animation) ? animation : legacy;

        /// <summary>Associates validated metadata with a texture without extending its lifetime.</summary>
        public static void Configure(Texture2D texture, TileWalkAnimation animation)
        {
            animation.Validate();
            animations.Remove(texture);
            animations.Add(texture, animation);
        }

        /// <summary>Pixel size of one frame of a sheet of <paramref name="sheetSize"/> pixels.</summary>
        public static Vector2 FrameSize(Vector2 sheetSize, int columns = Columns) => sheetSize / new Vector2(columns, Rows);

        /// <summary>Pixel region using the texture's own configured layout.</summary>
        public static Rect2 Frame(Texture2D texture, int column, int row)
            => Frame(texture.GetSize(), column, row, Animation(texture).Columns);

        /// <summary>Pixel region of frame (<paramref name="column"/>, <paramref name="row"/>); out-of-range values are clamped into the sheet.</summary>
        public static Rect2 Frame(Vector2 sheetSize, int column, int row, int columns = Columns)
        {
            var frame = FrameSize(sheetSize, columns);
            var cell = new Vector2(Mathf.Clamp(column, 0, columns - 1), Mathf.Clamp(row, 0, Rows - 1));
            return new Rect2(cell * frame, frame);
        }
    }
}
