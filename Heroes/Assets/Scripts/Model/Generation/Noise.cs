namespace Portfolio.Heroes
{
    /// <summary>
    /// Smooth value noise in whole numbers (0 to 1000), the same on every device: a hash per lattice point, blended with
    /// an integer smoothstep. Coordinates are the grid's half-cell x (<see cref="HexGrid.X2"/>) and twice the row, which
    /// are nearly square on the hexagon layout.
    /// </summary>
    public static class Noise
    {
        public static int Hash(int x, int y, uint salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) + (uint)(y * 668265263) + salt * 2246822519u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (int)(h % 1001u);
            }
        }

        private static int Smooth(int t, int scale)
        {
            // 1000 * smoothstep(t / scale)
            long f = (long)t * 1000 / scale;
            return (int)(f * f * (3000 - 2 * f) / 1000000);
        }

        public static int Value(int px, int py, int scale, uint salt)
        {
            if (scale <= 1)
            {
                return Hash(px, py, salt);
            }
            int ix = FloorDiv(px, scale);
            int iy = FloorDiv(py, scale);
            int fx = Smooth(px - ix * scale, scale);
            int fy = Smooth(py - iy * scale, scale);
            int a = Hash(ix, iy, salt);
            int b = Hash(ix + 1, iy, salt);
            int c = Hash(ix, iy + 1, salt);
            int d = Hash(ix + 1, iy + 1, salt);
            int top = (a * (1000 - fx) + b * fx) / 1000;
            int bottom = (c * (1000 - fx) + d * fx) / 1000;
            return (top * (1000 - fy) + bottom * fy) / 1000;
        }

        /// <summary>Two octaves: broad shapes with some detail.</summary>
        public static int Fractal(int px, int py, int scale, uint salt)
        {
            int broad = Value(px, py, scale, salt);
            int detail = Value(px, py, System.Math.Max(2, scale / 3), salt + 7919u);
            return (broad * 3 + detail) / 4;
        }

        public static int At(HexGrid grid, int cell, int scale, uint salt, bool fractal = true)
        {
            int px = grid.X2(cell);
            int py = grid.Row(cell) * 2;
            return fractal ? Fractal(px, py, scale, salt) : Value(px, py, scale, salt);
        }

        private static int FloorDiv(int a, int b)
        {
            int q = a / b;
            return (a % b != 0 && (a < 0) != (b < 0)) ? q - 1 : q;
        }
    }
}
