using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Where the cells of the map lie in the world: the grid is centered on the terrain, every cell a pointy topped
    /// hexagon of <see cref="Radius"/> meters, laid out like Terrain Grid System lays out its hexagonal grid (the rules'
    /// <see cref="HexGrid"/>), so the grid drawn on the terrain and the cells of the rules match one to one.
    /// </summary>
    public sealed class HexLayout
    {
        /// <summary>Circumradius of a cell: 1.2 m, a hexagon 2.4 m from point to point and 2.08 m across.</summary>
        public const float DefaultRadius = 1.2f;

        public HexLayout(HexGrid grid, float radius, float margin)
        {
            Grid = grid;
            Radius = radius;
            grid.Size(out double width, out double height);
            GridSize = new Vector2((float)width * radius, (float)height * radius);
            Margin = margin;
            TerrainSize = new Vector2(GridSize.x + margin * 2f, GridSize.y + margin * 2f);
            Origin = new Vector2(margin, margin);
        }

        public HexGrid Grid { get; }

        public float Radius { get; }

        /// <summary>Width (x) and depth (z) of the grid in meters.</summary>
        public Vector2 GridSize { get; }

        public Vector2 TerrainSize { get; }

        public float Margin { get; }

        /// <summary>The bottom left corner of the grid, relative to the terrain's corner.</summary>
        public Vector2 Origin { get; }

        /// <summary>World position of the terrain's corner (the terrain sits at the world origin).</summary>
        public Vector3 TerrainPosition => Vector3.zero;

        public float CellWidth => Radius * 1.7320508f;

        /// <summary>The center of a cell on the ground plane (y = 0).</summary>
        public Vector3 Flat(int cell)
        {
            Grid.Center(cell, out double x, out double y);
            return new Vector3(Origin.x + (float)x * Radius, 0f, Origin.y + (float)y * Radius);
        }

        /// <summary>The cell under a world position, or -1.</summary>
        public int CellAt(Vector3 world)
        {
            return Grid.CellAt((world.x - Origin.x) / Radius, (world.z - Origin.y) / Radius);
        }

        /// <summary>Position in grid units (circumradii) of a world position, for sampling the map.</summary>
        public Vector2 GridPoint(float worldX, float worldZ)
        {
            return new Vector2((worldX - Origin.x) / Radius, (worldZ - Origin.y) / Radius);
        }

        public Bounds GridBounds(float height)
        {
            var center = new Vector3(Origin.x + GridSize.x * 0.5f, height * 0.5f, Origin.y + GridSize.y * 0.5f);
            return new Bounds(center, new Vector3(GridSize.x, height, GridSize.y));
        }
    }
}
