namespace Cthangover.Mods.TileRender.Tests
{
    using Cthangover.Core.Mods.Testing;
    using Godot;
    using System;
    using System.Text.Json;
    using Cthangover.Core.Mods;
    using Cthangover.Core.Mods.Resolvers;

    /// <summary>Legacy and per-texture layouts, playback speed, idle poses and metadata validation.</summary>
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

        [ModTest("walk_sheet/configuration_roundtrip_and_playback")]
        static void Configuration(ModTestContext ctx)
        {
            var original = new TileWalkAnimation { Columns = 7, IdleColumn = 3,
                WalkFrames = new[] { 0, 1, 2, 3, 4, 5, 6, 3 }, SpeedScale = 0.8f };
            var restored = JsonSerializer.Deserialize<TileWalkAnimation>(JsonSerializer.Serialize(original));
            restored.Validate();
            ModAssert.Equal(7, restored.Columns);
            ModAssert.Equal(3, restored.IdleColumn);
            ModAssert.CloseTo(0.8, restored.SpeedScale);
            for (var i = 0; i < original.WalkFrames.Length; i++)
            {
                ModAssert.Equal(original.WalkFrames[i], restored.WalkFrames[i]);
                ModAssert.Equal(original.WalkFrames[i], restored.Column((i + 0.1) / 0.8, true));
            }
            ModAssert.Equal(0, restored.Column(1, true), "first transition now takes 1.25 old frame intervals");
            ModAssert.Equal(1, restored.Column(1.3, true));
            ModAssert.Equal(3, restored.Column(99, false), "idle is independent of walk phase");
            var legacy = new TileWalkAnimation();
            ModAssert.Equal(1, legacy.Column(3.1, true), "legacy cycle still returns to its standing pose");
            var sequential = new TileWalkAnimation { Columns = 6, IdleColumn = 0 };
            ModAssert.Equal(5, sequential.Column(5.1, true));
            ModAssert.Equal(0, sequential.Column(6.1, true));
        }

        [ModTest("walk_sheet/different_layouts_on_different_textures")]
        static void TextureLayouts(ModTestContext ctx)
        {
            using var oldImage = Image.CreateEmpty(96, 128, false, Image.Format.Rgba8);
            using var newImage = Image.CreateEmpty(160, 128, false, Image.Format.Rgba8);
            using var oldTexture = ImageTexture.CreateFromImage(oldImage);
            using var newTexture = ImageTexture.CreateFromImage(newImage);
            TileWalkSheet.Configure(newTexture, new TileWalkAnimation { Columns = 5, IdleColumn = 2 });
            ModAssert.True(TileWalkSheet.Frame(oldTexture, 1, 3) == new Rect2(32, 96, 32, 32));
            ModAssert.True(TileWalkSheet.Frame(newTexture, 4, 3) == new Rect2(128, 96, 32, 32));
            ModAssert.Equal(1, TileWalkSheet.Animation(oldTexture).Column(10, false));
            ModAssert.Equal(2, TileWalkSheet.Animation(newTexture).Column(10, false));
        }

        [ModTest("walk_sheet/reject_invalid_metadata")]
        static void InvalidMetadata(ModTestContext ctx)
        {
            var invalid = new[] {
                new TileWalkAnimation { Columns = 0 }, new TileWalkAnimation { IdleColumn = 3 },
                new TileWalkAnimation { WalkFrames = Array.Empty<int>() },
                new TileWalkAnimation { WalkFrames = new[] { -1 } },
                new TileWalkAnimation { WalkFrames = new[] { 3 } },
                new TileWalkAnimation { SpeedScale = 0 }, new TileWalkAnimation { SpeedScale = float.NaN } };
            foreach (var animation in invalid)
            {
                var rejected = false;
                try { animation.Validate(); } catch (ArgumentException) { rejected = true; }
                ModAssert.True(rejected, "invalid layout must be rejected");
            }
        }

        [ModTest("walk_sheet/mounted_sidecars_reach_texture_consumers")]
        static void MountedSidecars(ModTestContext ctx)
        {
            using var cache = new TileAssetCache();
            foreach (var pair in Files.Collect("charsets"))
            {
                if (!pair.Key.EndsWith(".walk.json", StringComparison.Ordinal))
                    continue;
                var path = pair.Key.Substring(0, pair.Key.Length - ".walk.json".Length);
                var json = ModManager.Instance.ReadFileText(pair.Value.ModId, pair.Value.FullPath);
                var expected = JsonSerializer.Deserialize<TileWalkAnimation>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                expected.Validate();
                var texture = cache.Resolve("charsets", path);
                ModAssert.NotNull(texture, path + " texture");
                var actual = TileWalkSheet.Animation(texture);
                ModAssert.Equal(expected.Columns, actual.Columns, path + " columns");
                ModAssert.Equal(expected.IdleColumn, actual.IdleColumn, path + " idle");
                ModAssert.CloseTo(expected.SpeedScale, actual.SpeedScale, what: path + " speed");
                ModAssert.Equal(0, texture.GetWidth() % actual.Columns, path + " equal frame widths");
                ModAssert.Equal(0, texture.GetHeight() % TileWalkSheet.Rows, path + " equal row heights");
                for (var phase = 0; phase < (expected.WalkFrames?.Length ?? expected.Columns); phase++)
                    ModAssert.Equal(expected.Column(phase / (double)expected.SpeedScale + 0.01, true),
                        actual.Column(phase / (double)expected.SpeedScale + 0.01, true), path + " playback");
            }
        }
    }
}
