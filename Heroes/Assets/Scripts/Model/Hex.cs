using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>The six sides of a pointy topped hexagon, clockwise from the right.</summary>
    public enum HexSide
    {
        East = 0,
        SouthEast = 1,
        SouthWest = 2,
        West = 3,
        NorthWest = 4,
        NorthEast = 5
    }

    /// <summary>
    /// The hexagonal grid of a map, laid out the way Terrain Grid System 2 lays out a hexagonal grid with pointy topped
    /// hexagons and "even layout" off: cells are numbered row by row from the bottom (row 0 lies at the lowest z), a row
    /// runs along x, and every odd row is shifted half a cell to the left. So a cell index of the rules is the cell
    /// index of the grid on the terrain, and the adventure map and every battle share one grid.
    ///
    /// Distances and lines work in axial coordinates: q = column + (row - (row &amp; 1)) / 2, r = -row.
    /// </summary>
    [Serializable]
    public sealed class HexGrid
    {
        public int columns;
        public int rows;

        public HexGrid()
        {
        }

        public HexGrid(int columns, int rows)
        {
            this.columns = columns;
            this.rows = rows;
        }

        public int Count => columns * rows;

        public int Index(int column, int row)
        {
            return row * columns + column;
        }

        public int Column(int index)
        {
            return index % columns;
        }

        public int Row(int index)
        {
            return index / columns;
        }

        public bool Contains(int column, int row)
        {
            return column >= 0 && row >= 0 && column < columns && row < rows;
        }

        public bool Valid(int index)
        {
            return index >= 0 && index < columns * rows;
        }

        /// <summary>The cell beyond <paramref name="side"/> of <paramref name="index"/>, or -1 off the grid.</summary>
        public int Neighbor(int index, HexSide side)
        {
            int column = Column(index);
            int row = Row(index);
            bool odd = (row & 1) == 1;
            switch (side)
            {
                case HexSide.East:
                    column += 1;
                    break;
                case HexSide.West:
                    column -= 1;
                    break;
                case HexSide.NorthEast:
                    column += odd ? 0 : 1;
                    row += 1;
                    break;
                case HexSide.NorthWest:
                    column += odd ? -1 : 0;
                    row += 1;
                    break;
                case HexSide.SouthEast:
                    column += odd ? 0 : 1;
                    row -= 1;
                    break;
                case HexSide.SouthWest:
                    column += odd ? -1 : 0;
                    row -= 1;
                    break;
            }
            return Contains(column, row) ? Index(column, row) : -1;
        }

        /// <summary>The cells next to <paramref name="index"/>, in side order, skipping the ones off the grid.</summary>
        public void Neighbors(int index, List<int> into)
        {
            into.Clear();
            for (int side = 0; side < 6; side++)
            {
                int next = Neighbor(index, (HexSide)side);
                if (next >= 0)
                {
                    into.Add(next);
                }
            }
        }

        public List<int> Neighbors(int index)
        {
            var list = new List<int>(6);
            Neighbors(index, list);
            return list;
        }

        public bool Adjacent(int a, int b)
        {
            return a != b && Distance(a, b) == 1;
        }

        public static int AxialQ(int column, int row)
        {
            return column + (row - (row & 1)) / 2;
        }

        public static int AxialR(int row)
        {
            return -row;
        }

        public int Distance(int a, int b)
        {
            int ra = Row(a), rb = Row(b);
            int dq = AxialQ(Column(a), ra) - AxialQ(Column(b), rb);
            int dr = AxialR(ra) - AxialR(rb);
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }

        /// <summary>The cell at axial (q, r), or -1 off the grid.</summary>
        public int FromAxial(int q, int r)
        {
            int row = -r;
            int column = q - (row - (row & 1)) / 2;
            return Contains(column, row) ? Index(column, row) : -1;
        }

        /// <summary>Every cell within <paramref name="radius"/> steps of <paramref name="center"/>, the center first.</summary>
        public List<int> Disk(int center, int radius)
        {
            var cells = new List<int>();
            int cq = AxialQ(Column(center), Row(center));
            int cr = AxialR(Row(center));
            cells.Add(center);
            for (int ring = 1; ring <= radius; ring++)
            {
                AddRing(cq, cr, ring, cells);
            }
            return cells;
        }

        /// <summary>The cells exactly <paramref name="radius"/> steps from <paramref name="center"/>.</summary>
        public List<int> Ring(int center, int radius)
        {
            var cells = new List<int>();
            if (radius == 0)
            {
                cells.Add(center);
                return cells;
            }
            AddRing(AxialQ(Column(center), Row(center)), AxialR(Row(center)), radius, cells);
            return cells;
        }

        private static readonly int[] DirQ = { 1, 0, -1, -1, 0, 1 };
        private static readonly int[] DirR = { 0, 1, 1, 0, -1, -1 };

        private void AddRing(int cq, int cr, int radius, List<int> into)
        {
            // Start at the south west corner of the ring and walk its six sides.
            int q = cq + DirQ[4] * radius;
            int r = cr + DirR[4] * radius;
            for (int side = 0; side < 6; side++)
            {
                for (int step = 0; step < radius; step++)
                {
                    int cell = FromAxial(q, r);
                    if (cell >= 0)
                    {
                        into.Add(cell);
                    }
                    q += DirQ[side];
                    r += DirR[side];
                }
            }
        }

        /// <summary>
        /// The x of a cell center in half cell widths (whole numbers, for the rules: no floating point differences between
        /// devices). Neighbors in a row are 2 apart, the rows above and below are shifted by 1.
        /// </summary>
        public int X2(int index)
        {
            int row = Row(index);
            return 2 * Column(index) + 2 - (row & 1);
        }

        /// <summary>Four times the squared distance between two cell centers, in circumradius units: 3 dx2^2 + 9 drow^2.</summary>
        public int Distance2x4(int a, int b)
        {
            int dx = X2(a) - X2(b);
            int dy = Row(a) - Row(b);
            return 3 * dx * dx + 9 * dy * dy;
        }

        /// <summary>Position of a cell center in units of the hexagon's circumradius, x to the right and y up (z in the world).</summary>
        public static void Center(int column, int row, out double x, out double y)
        {
            const double width = 1.7320508075688772; // sqrt(3)
            x = width * (column + 1 - 0.5 * (row & 1));
            y = 1.0 + 1.5 * row;
        }

        public void Center(int index, out double x, out double y)
        {
            Center(Column(index), Row(index), out x, out y);
        }

        /// <summary>Width and height of the whole grid in units of the circumradius.</summary>
        public void Size(out double width, out double height)
        {
            width = 1.7320508075688772 * (columns + 0.5);
            height = 2.0 + 1.5 * (rows - 1);
        }

        /// <summary>The cell whose hexagon contains the point (x, y) in circumradius units, or -1 off the grid.</summary>
        public int CellAt(double x, double y)
        {
            // A point lies in the hexagon of the nearest cell center: try the few centers around the estimate.
            const double width = 1.7320508075688772;
            int rowGuess = (int)Math.Round((y - 1.0) / 1.5);
            int best = -1;
            double bestDistance = double.MaxValue;
            for (int row = rowGuess - 1; row <= rowGuess + 1; row++)
            {
                if (row < 0 || row >= rows)
                {
                    continue;
                }
                int columnGuess = (int)Math.Round(x / width - 1.0 + 0.5 * (row & 1));
                for (int column = columnGuess - 1; column <= columnGuess + 1; column++)
                {
                    if (column < 0 || column >= columns)
                    {
                        continue;
                    }
                    Center(column, row, out double cx, out double cy);
                    double d = (cx - x) * (cx - x) + (cy - y) * (cy - y);
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        best = Index(column, row);
                    }
                }
            }
            // Every point of a hexagon is within one circumradius of its center.
            return bestDistance <= 1.0 ? best : -1;
        }

        /// <summary>The side of <paramref name="from"/> that faces <paramref name="to"/> (a neighbor), or East when they are not neighbors.</summary>
        public HexSide SideTowards(int from, int to)
        {
            for (int side = 0; side < 6; side++)
            {
                if (Neighbor(from, (HexSide)side) == to)
                {
                    return (HexSide)side;
                }
            }
            return HexSide.East;
        }
    }
}
