namespace Cthangover.Mods.TileRender.Tests
{
    using System;
    /// <summary>Registry fixture with no Godot allocation or dependency on an implementation mod.</summary>
    internal class StubTileRendererFactory : ITileRendererFactory
    {
        public string Id { get; }
        public StubTileRendererFactory(string id) { Id = id; }
        public ITileProjection Projection(int size) => throw new NotSupportedException();
        public ITileWorldPresentation Create() => throw new NotSupportedException();
    }
}
