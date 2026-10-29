namespace Cthangover.Mods.TileRender.Tests
{
    using Cthangover.Core.Mods.Testing;
    /// <summary>Explicit selection, ambiguity and owner-sensitive unregister never depend on discovery order.</summary>
    public class TileRendererRegistryTests
    {
        private readonly TileRendererRegistry registry = new();
        private readonly StubTileRendererFactory square = new("square");
        private void Missing() => registry.Resolve("missing");
        private void Ambiguous() => registry.Resolve(null);
        private void Duplicate() => registry.Register(square);

        [ModTest("registry/selection_collision_and_unload")]
        static void SelectionCollisionAndUnload(ModTestContext ctx)
        {
            var test = new TileRendererRegistryTests();
            test.registry.Register(test.square);
            ModAssert.True(ReferenceEquals(test.square, test.registry.Resolve(null)), "single implementation may be inferred");
            ModAssert.Throws(test.Missing, "explicit missing ID must not fall back", true);
            ModAssert.Throws(test.Duplicate, "duplicate ID is rejected", true);
            var iso = new StubTileRendererFactory("iso");
            test.registry.Register(iso);
            ModAssert.Throws(test.Ambiguous, "multiple implementations require selection", true);
            ModAssert.True(ReferenceEquals(iso, test.registry.Resolve("iso")), "explicit selection");
            test.registry.Unregister(new StubTileRendererFactory("iso"));
            ModAssert.True(ReferenceEquals(iso, test.registry.Resolve("iso")), "a different owner cannot delete a factory");
            test.registry.Unregister(iso);
            ModAssert.True(ReferenceEquals(test.square, test.registry.Resolve(null)), "remaining implementation stays available");
        }
    }
}
