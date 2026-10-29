namespace Cthangover.Mods.TileRender
{
    using System;
    using Godot;
    /// <summary>
    /// Pure CPU reference compositor. A region samples neighbours from the full field,
    /// with global patch phase and noise, so chunk borders equal a monolithic render.
    /// </summary>
    public static class TileSurfaceRasterizer
    {
        public static byte[] Render(TileSurfaceField field, byte[] atlas, int atlasWidth, Rect2I cells)
        {
            var size = field.TileSize;
            var patch = field.PatchSize;
            if (size <= 0 || patch <= 0 || atlasWidth < patch || atlas.Length < atlasWidth * patch * 4 ||
                field.Materials.Length != field.Width * field.Height || field.Diffuse.Length != field.Materials.Length ||
                cells.Position.X < 0 || cells.Position.Y < 0 || cells.End.X > field.Width || cells.End.Y > field.Height)
                throw new ArgumentException("Invalid surface field, atlas or region.");
            var width = cells.Size.X * size;
            var height = cells.Size.Y * size;
            var output = new byte[width * height * 4];
            var radius = Math.Min(8f, size / 4f);
            var homogeneous = new bool[cells.Size.X * cells.Size.Y];
            if (size >= 8)
                for (var cy = cells.Position.Y; cy < cells.End.Y; cy++)
                for (var cx = cells.Position.X; cx < cells.End.X; cx++)
                    homogeneous[(cy - cells.Position.Y) * cells.Size.X + cx - cells.Position.X] = Homogeneous(field, cx, cy, patch, atlasWidth);
            for (var oy = 0; oy < height; oy++)
            for (var ox = 0; ox < width; ox++)
            {
                var x = ox + cells.Position.X * size;
                var y = oy + cells.Position.Y * size;
                var cx = x / size;
                var cy = y / size;
                var own = cy * field.Width + cx;
                if (field.Materials[own] < 0) continue;
                // Equal source samples stay equal under any positive blend weights.
                // Most terrain interiors can copy the patch without a 9-tap kernel.
                if (homogeneous[(cy - cells.Position.Y) * cells.Size.X + cx - cells.Position.X])
                {
                    var src = ((y % patch) * atlasWidth + field.Materials[own] * patch + x % patch) * 4;
                    var dst = (oy * width + ox) * 4;
                    output[dst] = atlas[src]; output[dst + 1] = atlas[src + 1];
                    output[dst + 2] = atlas[src + 2]; output[dst + 3] = atlas[src + 3];
                    continue;
                }
                var noise = (float)(Math.Sin((x + 0.5) * 0.23) * Math.Cos((y + 0.5) * 0.19) * 1.5);
                float red = 0, green = 0, blue = 0, alpha = 0, total = 0;
                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var nx = cx + dx;
                    var ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= field.Width || ny >= field.Height) continue;
                    var index = ny * field.Width + nx;
                    var material = field.Materials[index];
                    if (material < 0 || ((dx != 0 || dy != 0) && (!field.Diffuse[own] || !field.Diffuse[index]))) continue;
                    if ((material + 1) * patch > atlasWidth) throw new ArgumentException("Surface index is outside the atlas.");
                    var px = x + 0.5f + noise;
                    var py = y + 0.5f + noise;
                    var distanceX = Math.Min(px - nx * size, (nx + 1) * size - px);
                    var distanceY = Math.Min(py - ny * size, (ny + 1) * size - py);
                    var weight = Smooth(0.5f + distanceX / (2 * radius)) * Smooth(0.5f + distanceY / (2 * radius));
                    if (weight <= 0) continue;
                    var offset = ((y % patch) * atlasWidth + material * patch + x % patch) * 4;
                    red += atlas[offset] * weight; green += atlas[offset + 1] * weight;
                    blue += atlas[offset + 2] * weight; alpha += atlas[offset + 3] * weight; total += weight;
                }
                var dest = (oy * width + ox) * 4;
                output[dest] = (byte)Math.Clamp((int)Math.Round(red / total), 0, 255);
                output[dest + 1] = (byte)Math.Clamp((int)Math.Round(green / total), 0, 255);
                output[dest + 2] = (byte)Math.Clamp((int)Math.Round(blue / total), 0, 255);
                output[dest + 3] = (byte)Math.Clamp((int)Math.Round(alpha / total), 0, 255);
            }
            return output;
        }

        private static bool Homogeneous(TileSurfaceField field, int x, int y, int patch, int atlasWidth)
        {
            var own = y * field.Width + x;
            var material = field.Materials[own];
            if (material < 0 || (material + 1) * patch > atlasWidth) return false;
            if (!field.Diffuse[own]) return true;
            for (var ny = Math.Max(0, y - 1); ny <= Math.Min(field.Height - 1, y + 1); ny++)
            for (var nx = Math.Max(0, x - 1); nx <= Math.Min(field.Width - 1, x + 1); nx++)
            {
                var i = ny * field.Width + nx;
                if (field.Materials[i] >= 0 && field.Diffuse[i] && field.Materials[i] != material) return false;
            }
            return true;
        }

        private static float Smooth(float value)
        { value = Math.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
    }
}
