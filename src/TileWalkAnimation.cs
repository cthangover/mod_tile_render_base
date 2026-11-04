namespace Cthangover.Mods.TileRender
{
    using System;

    /// <summary>
    /// Per-charset layout and playback, read from its .walk.json sidecar. Frame indices are zero based;
    /// the four direction rows retain their shared world-facing order. SpeedScale changes poses only.
    /// </summary>
    public class TileWalkAnimation
    {
        public int Columns { get; set; } = 3;
        public int IdleColumn { get; set; } = 1;
        public int[] WalkFrames { get; set; }
        public float SpeedScale { get; set; } = 1f;

        /// <summary>Rejects invalid layouts before they can crop or animate a texture.</summary>
        public void Validate()
        {
            if (Columns < 1 || IdleColumn < 0 || IdleColumn >= Columns)
                throw new ArgumentException("Columns must be positive and IdleColumn must be inside the sheet.");
            if (!float.IsFinite(SpeedScale) || SpeedScale <= 0)
                throw new ArgumentException("SpeedScale must be finite and positive.");
            if (WalkFrames == null)
                return;
            if (WalkFrames.Length == 0)
                throw new ArgumentException("WalkFrames must contain at least one frame.");
            foreach (var frame in WalkFrames)
                if (frame < 0 || frame >= Columns)
                    throw new ArgumentException("WalkFrames contains a frame outside the sheet.");
        }

        /// <summary>
        /// Selects a pose from the simulation's continuous walk clock. Unconfigured three-column sheets
        /// retain 0,1,2,1; other layouts play all columns in order unless WalkFrames overrides that order.
        /// </summary>
        public int Column(double animationTime, bool moving)
        {
            if (!moving)
                return IdleColumn;
            var count = WalkFrames?.Length ?? (Columns == 3 ? 4 : Columns);
            var phase = (int)(Math.Max(0, animationTime) * SpeedScale % count);
            return WalkFrames != null ? WalkFrames[phase] : Columns == 3 && phase == 3 ? 1 : phase;
        }
    }
}
