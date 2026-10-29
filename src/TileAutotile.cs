namespace Cthangover.Mods.TileRender
{
    using System;
    using Godot;
    /// <summary>Projection-independent A2 quarter selection and phase addressing.</summary>
    public static class TileAutotile
    {
        public static Vector2I[] Quarters(bool up, bool down, bool left, bool right,
            bool upLeft, bool upRight, bool downLeft, bool downRight)
        {
            return new[]
            {
                Quarter(up, left, upLeft, new Vector2I(2, 0), left ? 2 : 0, up ? 4 : 2),
                Quarter(up, right, upRight, new Vector2I(3, 0), right ? 1 : 3, up ? 4 : 2),
                Quarter(down, left, downLeft, new Vector2I(2, 1), left ? 2 : 0, down ? 3 : 5),
                Quarter(down, right, downRight, new Vector2I(3, 1), right ? 1 : 3, down ? 3 : 5),
            };
        }

        public static Rect2 VariantRect(Vector2I imageSize, int tileSize, Vector2I cell)
        {
            var width = Math.Max(1, tileSize) * 2;
            var height = Math.Max(1, tileSize) * 3;
            var columns = Math.Max(1, imageSize.X / width);
            var rows = Math.Max(1, imageSize.Y / height);
            var x = (cell.X % columns + columns) % columns;
            var y = (cell.Y % rows + rows) % rows;
            return new Rect2(x * width, y * height, width, height);
        }

        private static Vector2I Quarter(bool sideA, bool sideB, bool corner, Vector2I inner, int column, int row)
        {
            if (sideA && sideB && !corner)
                return inner;
            return new Vector2I(column, row);
        }

    }
}
