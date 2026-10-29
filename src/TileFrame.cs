namespace Cthangover.Mods.TileRender
{
    using System.Collections.Generic;
    using Godot;
    /// <summary>Reusable frame storage; populated by the game, read synchronously by the presentation.</summary>
    public class TileFrame
    {
        public List<TileActorPose> Actors { get; } = new();
        public List<TileLight> Lights { get; } = new();
        public Vector2 Focus { get; set; }
        public Vector2? InteractionCell { get; set; }
        public Vector4 TimeWeights { get; set; } = new(0, 1, 0, 0);
        public Color? Ambient { get; set; }
    }
}
