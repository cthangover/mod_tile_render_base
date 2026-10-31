namespace Cthangover.Mods.TileRender.Tests
{
    using Cthangover.Core.Mods.Testing;
    using Godot;

    /// <summary>The single 3×4 walking-sheet layout: frame size and frame regions for any sheet size.</summary>
    public class TileWalkSheetTests
    {
        [ModTest("walk_sheet/frames_of_any_sheet_size")]
        static void Frames(ModTestContext ctx)
        {
            ModAssert.True(TileWalkSheet.FrameSize(new Vector2(96, 128)) == new Vector2(32, 32), "3 columns × 4 rows of 32 px");
            ModAssert.True(TileWalkSheet.FrameSize(new Vector2(144, 256)) == new Vector2(48, 64), "frames need not be square");
            ModAssert.True(TileWalkSheet.Frame(new Vector2(96, 128), TileWalkSheet.IdleColumn, 3) == new Rect2(32, 96, 32, 32), "idle frame of the Up row");
            ModAssert.True(TileWalkSheet.Frame(new Vector2(96, 128), 0, 0) == new Rect2(0, 0, 32, 32), "first frame");
            ModAssert.True(TileWalkSheet.Frame(new Vector2(96, 128), 5, -1) == new Rect2(64, 0, 32, 32), "out-of-range frames clamp into the sheet");
        }
    }
}
