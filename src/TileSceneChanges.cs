namespace Cthangover.Mods.TileRender
{
    /// <summary>Versioned replacement of prepared visual data; implementations retain unchanged draw chunks.</summary>
    public class TileSceneChanges
    {
        public long BaseRevision { get; set; }
        public TileRenderScene Scene { get; set; }
    }
}
