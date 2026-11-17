namespace Cthangover.Mods.TileRender
{
    using Godot;

    /// <summary>A resolved image region in cell space. Texture pixels are never gameplay geometry.</summary>
    public class TileVisual
    {
        /// <summary>Null preserves legacy ordering. SortAnchor affects depth only; it never moves the image.</summary>
        public TileDrawOrder? DrawOrder { get; set; }

        /// <summary>Optional cell-space depth point; geometry and the visibility sample remain independent.</summary>
        public Vector2? SortAnchor { get; set; }

        /// <summary>Stable presentation identity used to reuse draw nodes when authored geometry changes.</summary>
        public string Id { get; set; }

        /// <summary>Borrowed decoded image; the scene, builder or cache owns its lifetime.</summary>
        public Texture2D Texture { get; set; }

        /// <summary>Pixel rectangle in Texture, independent of the cell-space destination.</summary>
        public Rect2 Source { get; set; }

        /// <summary>Cell-space destination before projection, scaling and camera displacement.</summary>
        public Rect2 Bounds { get; set; }

        /// <summary>Ground anchor used to position and sort upright artwork.</summary>
        public Vector2 Anchor { get; set; }

        /// <summary>Fallback draw stratum when no explicit DrawOrder is supplied.</summary>
        public int Layer { get; set; }

        /// <summary>Whether the image stands on Anchor instead of lying on the ground plane.</summary>
        public bool Upright { get; set; }

        /// <summary>Artwork modulation before the shared daylight and optional visibility material.</summary>
        public Color Tint { get; set; } = Colors.White;

        /// <summary>
        /// Optional cell-space sample for whole upright images. Null keeps per-pixel sampling, including room and ground art;
        /// poses add their position to this local point. It does not move or sort the image.
        /// </summary>
        public Vector2? VisibilityCell { get; set; }

        /// <summary>
        /// Optional explicit world plane, TL/TR/BR/BL in world cell coordinates; with Upright it is depth-sorted at Anchor.
        /// </summary>
        public Vector3[] WorldQuad { get; set; }

        /// <summary>Only this projection uses the visual (empty means both).</summary>
        public string Only { get; set; }
    }
}
