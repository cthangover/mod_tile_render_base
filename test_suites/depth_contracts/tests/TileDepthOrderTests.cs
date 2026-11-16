using System;
using System.IO;
using System.Threading.Tasks;
using Cthangover.Core.Mods.Testing;
using Cthangover.Core.Utils;
using Cthangover.Mods.Interface.View;
using Godot;

namespace Cthangover.Mods.TileRender.Tests
{
    /// <summary>
    /// Native pixel ordering across apply/remount requires the scheduled windowed renderer, independently of headless data tests.
    /// </summary>
    public sealed class TileDepthOrderTests
    {
        [ModTest("depth/equal_anchor_order_survives_apply_and_remount")]
        static async Task StableTies(ModTestContext ctx)
        {
            ModAssert.True(GameEnvironment.IsRuntime, "depth probe requires the real renderer");
            ModAssert.False(DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase),
                "pixel ordering requires the registered windowed tile_depth_order launch");
            var projections = new[]
            {
                "topdown",
                "isometric",
            };
            foreach (var renderer in projections)
            {
                await CheckStableTie(ctx, renderer);
            }
        }

        private static async Task CheckStableTie(ModTestContext ctx, string renderer)
        {
            using var red = ProbeTexture(Colors.Red);
            using var green = ProbeTexture(Colors.Lime);
            var viewport = new SubViewport
            {
                Size = new Vector2I(128, 128),
                Disable3D = true,
                TransparentBg = true,
                UseHdr2D = ((SceneTree)Godot.Engine.GetMainLoop()).Root.UseHdr2D,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            ITileWorldPresentation view = null;
            try
            {
                ((SceneTree)Godot.Engine.GetMainLoop()).Root.AddChild(viewport);
                view = TileRendererRegistry.Instance.Resolve(renderer).Create();
                viewport.AddChild(view.Root);
                view.Resize(new Vector2(128, 128));
                var first = ProbeScene(red, 1);
                var frame = new TileFrame
                {
                    Focus = Vector2.One,
                    Ambient = Colors.White,
                };
                frame.Actors.Add(new TileActorPose
                {
                    Id = "walker",
                    Visible = true,
                    Position = Vector2.One,
                    Look = new TileVisual
                    {
                        Texture = green,
                        Source = new Rect2(0, 0, 16, 16),
                        Bounds = new Rect2(-0.5f, -0.5f, 1, 1),
                        Anchor = Vector2.Zero,
                    },
                });
                view.Mount(first);
                view.Present(frame, 0);
                await CheckGreen(viewport, view, ctx, renderer + "_initial");
                var next = ProbeScene(red, 2);
                view.Apply(new TileSceneChanges
                {
                    BaseRevision = 1,
                    Scene = next,
                });
                await CheckGreen(viewport, view, ctx, renderer + "_immediately_after_apply");
                view.Present(frame, 0);
                await CheckGreen(viewport, view, ctx, renderer + "_after_apply");
                view.Mount(next);
                view.Present(frame, 0);
                await CheckGreen(viewport, view, ctx, renderer + "_after_remount");
            }
            finally
            {
                view?.Dispose();
                viewport.Free();
            }
        }

        private static TileRenderScene ProbeScene(Texture2D texture, long revision)
        {
            var scene = new TileRenderScene
            {
                Revision = revision,
                Size = new Vector2I(2, 2),
                TileSize = 32,
                Zoom = 1,
            };
            scene.Visuals.Add(new TileVisual
            {
                Id = "prop#body",
                Texture = texture,
                Source = new Rect2(0, 0, 16, 16),
                Bounds = new Rect2(0, 0, 2, 2),
                Anchor = Vector2.One,
                Upright = true,
                DrawOrder = TileDrawOrder.ByPosition,
            });
            return scene;
        }

        private static ImageTexture ProbeTexture(Color color)
        {
            using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
            image.Fill(color);
            return ImageTexture.CreateFromImage(image);
        }

        private static async Task CheckGreen(SubViewport viewport, ITileWorldPresentation view, ModTestContext ctx, string stage)
        {
            await viewport.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await viewport.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var point = (Vector2I)view.ProjectToView(new Vector3(1, 1, 0));
            using var pixels = ViewportCapture.Take(viewport);
            ModAssert.NotNull(pixels, "depth probe capture succeeds");
            var color = pixels.GetPixel(point.X, point.Y);
            ctx.Log(stage + " equal-depth pixel=" + color);
            var output = System.Environment.GetEnvironmentVariable("ISO2D_DEPTH_SHOTS");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                ModAssert.True(ViewportCapture.SavePng(viewport, Path.Combine(output, stage + ".png")),
                    "depth capture writes through the shared pipeline");
            }

            ModAssert.True(color.G > 0.9f && color.R < 0.1f,
                stage + ": the equal-depth actor keeps the same order after replacement and reload");
        }

    }
}
