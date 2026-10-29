namespace Cthangover.Mods.TileRender
{
    using Cthangover.Core.Mods.Consumers;
    /// <summary>Uses the kernel's normal load, catch-up and reverse unload fan-out.</summary>
    public class TileRendererConsumer : ModTypeConsumer<ITileRendererFactory>
    {
        public override string Id => "tile_render_base.renderers";
        protected override void Add(ITileRendererFactory instance, string modId) => TileRendererRegistry.Instance.Register(instance);
        protected override void Remove(ITileRendererFactory instance, string modId) => TileRendererRegistry.Instance.Unregister(instance);
    }
}
