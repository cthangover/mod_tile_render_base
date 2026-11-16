using Cthangover.Core.Mods.Testing;
using Godot;
using Cthangover.Core.Utils;

namespace Cthangover.Mods.TileRender.Tests
{
    /// <summary>Checks actual camera convergence across frame rates in both registered projections.</summary>
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
