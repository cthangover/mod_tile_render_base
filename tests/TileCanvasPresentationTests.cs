using Cthangover.Core.Mods.Testing;
using Godot;
using System.Threading.Tasks;
using System.IO;
using Cthangover.Core.Utils;
using Cthangover.Mods.Interface.View;

namespace Cthangover.Mods.TileRender.Tests
{
    /// <summary>Checks actual camera convergence and equal-depth native pixels across updates in both registered projections.</summary>
    public class TileCanvasPresentationTests
    {
        [ModTest("camera/topdown_settles_without_pixel_stalls")]
        static void TopdownSettles(ModTestContext ctx)
        {
            CheckCamera(ctx, "topdown");
        }

        [ModTest("camera/isometric_settles_without_pixel_stalls")]
        static void IsometricSettles(ModTestContext ctx)
        {
            CheckCamera(ctx, "isometric");
        }

        [ModTest("depth/equal_anchor_order_survives_apply_and_remount")]
        static async Task StableTies(ModTestContext ctx)
        {
            ModAssert.True(GameEnvironment.IsRuntime, "depth probe requires the real renderer");
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
                UseHdr2D = ((SceneTree)Engine.GetMainLoop()).Root.UseHdr2D,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            ITileWorldPresentation view = null;
            try
            {
                ((SceneTree)Engine.GetMainLoop()).Root.AddChild(viewport);
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

        private static void CheckCamera(ModTestContext ctx, string renderer)
        {
            var factory = TileRendererRegistry.Instance.Resolve(renderer);
            Vector2? reference = null;
            var fpsValues = new[]
            {
                30,
                60,
                144,
                240
            };
            foreach (var fps in fpsValues)
            {
                using var view = factory.Create();
                ctx.SandboxRoot.AddChild(view.Root);
                view.Mount(new TileRenderScene
                {
                    Size = new Vector2I(256, 256),
                    TileSize = 32,
                    Zoom = 2
                });
                view.Resize(new Vector2(1024, 768));
                var frame = new TileFrame
                {
                    Focus = new Vector2(80, 80)
                };
                view.Present(frame, 0);
                frame.Focus = new Vector2(82.375f, 81.125f);
                var focus = new Vector3(frame.Focus.X, frame.Focus.Y, 0);
                var center = new Vector2(512, 384);
                var previousError = view.ProjectToView(focus).DistanceTo(center);
                for (var step = 0; step < fps / 2; step++)
                {
                    view.Present(frame, 1.0 / fps);
                    var error = view.ProjectToView(focus).DistanceTo(center);
                    if (previousError > 0.2f)
                    {
                        ModAssert.True(error < previousError, renderer + " camera must advance at " + fps + " FPS");
                    }

                    previousError = error;
                    if (step + 1 == fps / 6 && fps % 6 == 0)
                    {
                        if (reference.HasValue)
                        {
                            ModAssert.True(view.Root.Position.DistanceTo(reference.Value) < 0.01f,
                                "equal elapsed time gives equal camera positions");
                        }
                        else
                        {
                            reference = view.Root.Position;
                        }
                    }
                }

                ModAssert.True(previousError < 0.01f, renderer + " centers the fractional focus within 0.5 seconds at " + fps + " FPS");
                var stopped = view.Root.Position;
                for (var step = 0; step < fps * 2; step++)
                {
                    view.Present(frame, 1.0 / fps);
                }

                ModAssert.True(stopped == view.Root.Position, "stationary camera stays exactly still");
                ModAssert.True(view.TryPickGround(center, out var cell), "picking remains valid after camera settles");
                ModAssert.True(new Vector2I(82, 81) == cell, "center picks the focused ground cell");
                frame.Focus = new Vector2(85.25f, 83.625f);
                view.Present(frame, 0);
                ModAssert.True(view.ProjectToView(new Vector3(frame.Focus.X, frame.Focus.Y, 0)).DistanceTo(center) < 0.01f,
                    "zero delta snaps immediately on mount or teleport");
                ctx.Log(renderer + " camera settled within 0.5 seconds at " + fps + " FPS");
            }
        }
    }
}
