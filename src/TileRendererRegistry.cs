namespace Cthangover.Mods.TileRender
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cthangover.Core.Utils;

    /// <summary>Mod-owned factories. Selection is never cached across games and an explicit missing ID never falls back.</summary>
    public class TileRendererRegistry
    {
        public static TileRendererRegistry Instance { get; } = new();
        private readonly Dictionary<string, ITileRendererFactory> factories = new(StringComparer.Ordinal);
        /// <summary>Sent before the owner's assembly unloads, so live hosts release its nodes and delegates.</summary>
        public event Action<string> Removing;
        public IReadOnlyList<string> Ids => factories.Keys.OrderBy(IdOrder).ToArray();
        private static string IdOrder(string id) => id;

        public void Register(ITileRendererFactory factory)
        {
            if (factory == null || string.IsNullOrWhiteSpace(factory.Id))
                throw new ArgumentException("Renderer requires an ID.");
            if (!factories.TryAdd(factory.Id, factory))
                throw new InvalidOperationException($"Duplicate tile renderer '{factory.Id}'.");
        }

        public void Unregister(ITileRendererFactory factory)
        {
            if (factory == null || !factories.TryGetValue(factory.Id, out var current) || !ReferenceEquals(current, factory))
                return;
            if (Removing != null)
                foreach (Action<string> handler in Removing.GetInvocationList())
                    try { handler(factory.Id); }
                    catch (Exception ex) { GameLogger.Log("RPG", "renderer cleanup: " + ex.Message, LogLevel.Error); }
            factories.Remove(factory.Id);
        }

        public ITileRendererFactory Resolve(string id)
        {
            if (!string.IsNullOrEmpty(id) && factories.TryGetValue(id, out var factory))
                return factory;
            if (string.IsNullOrEmpty(id) && factories.Count == 1)
                return factories.Values.First();
            throw new InvalidOperationException($"Tile renderer '{id ?? "<choose>"}' unavailable or ambiguous. Available: {string.Join(", ", Ids)}");
        }
    }
}
