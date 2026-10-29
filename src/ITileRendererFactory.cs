namespace Cthangover.Mods.TileRender
{
    /// <summary>Discovered through ModTypeConsumer; constructors must not allocate Godot objects.</summary>
    public interface ITileRendererFactory
    {
        /// <summary>Stable collection setting, such as topdown or isometric.</summary>
        string Id { get; }
        /// <summary>Creates a pure projection for tools, picking and previews.</summary>
        ITileProjection Projection(int tileSize);
        /// <summary>Creates one independently owned presentation, on the main thread.</summary>
        ITileWorldPresentation Create();
    }
}
