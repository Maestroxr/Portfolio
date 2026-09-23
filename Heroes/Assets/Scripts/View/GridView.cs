using System.Collections.Generic;
using TGS;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The hexagonal grid of the map, drawn on the terrain by Terrain Grid System 2. It stays hidden while heroes travel
    /// and shows only while a battle is fought, and only on the battlefield: the other cells are made invisible and the
    /// lines fade out toward the edge of the field. It also paints cells: where a troop can go, what it can strike, the
    /// area of a spell.
    /// </summary>
    public sealed class GridView : MonoBehaviour
    {
        [SerializeField] private TerrainGridSystem grid;
        [SerializeField] private Color lineColor = new Color(1f, 0.86f, 0.45f, 0.75f);
        [SerializeField] private Transform fadeCenter;

        private readonly HashSet<int> painted = new HashSet<int>();
        private readonly List<int> hidden = new List<int>();
        private HexLayout layout;
        private bool battle;

        public TerrainGridSystem Grid => grid;

        public bool Ready { get; private set; }

        /// <summary>Lays the grid over a freshly built terrain: one cell of the grid per cell of the map.</summary>
        public void Setup(Terrain terrain, HexLayout mapLayout)
        {
            layout = mapLayout;
            if (grid == null)
            {
                GameObject prefab = Resources.Load<GameObject>("Prefabs/TerrainGridSystem");
                if (prefab == null)
                {
                    Debug.LogError("Heroes: the Terrain Grid System prefab is missing (Assets/TerrainGridSystem).");
                    return;
                }
                grid = Instantiate(prefab, transform).GetComponent<TerrainGridSystem>();
            }
            if (fadeCenter == null)
            {
                fadeCenter = new GameObject("Grid Fade Center").transform;
                fadeCenter.SetParent(transform, false);
            }
            painted.Clear();
            hidden.Clear();
            battle = false;
            grid.gridTopology = GridTopology.Hexagonal;
            grid.pointyTopHexagons = true;
            grid.evenLayout = false;
            grid.columnCount = layout.Grid.columns;
            grid.rowCount = layout.Grid.rows;
            grid.regularHexagons = true;
            grid.regularHexagonsWidth = layout.Radius * 2f;
            grid.showTerritories = false;
            grid.highlightMode = HighlightMode.None;
            grid.cellBorderColor = lineColor;
            grid.cellCustomBorderThickness = true;
            grid.cellBorderThickness = 1.6f;
            grid.gridElevation = 0.04f;
            grid.gridCameraOffset = 0.05f;
            grid.circularFadeEnabled = true;
            grid.circularFadeTarget = fadeCenter;
            grid.circularFadeDistance = 30f;
            grid.circularFadeFallOff = 6f;
            grid.respectOtherUI = true;
            grid.terrainObject = terrain.gameObject;
            grid.showCells = false;
            grid.Redraw();
            Ready = true;
        }

        /// <summary>World position of a cell center on the grid of Terrain Grid System (for checking it lines up with the rules).</summary>
        public Vector3 CellPosition(int cell)
        {
            return Ready ? grid.CellGetPosition(cell) : Vector3.zero;
        }

        /// <summary>Draws the grid on the battlefield only, fading toward its edges.</summary>
        public void ShowBattle(List<int> cells, Vector3 center, float radius)
        {
            if (!Ready)
            {
                return;
            }
            var inside = new HashSet<int>(cells);
            hidden.Clear();
            for (int cell = 0; cell < layout.Grid.Count; cell++)
            {
                if (!inside.Contains(cell))
                {
                    hidden.Add(cell);
                }
            }
            grid.CellSetVisible(hidden, false);
            fadeCenter.position = center;
            grid.circularFadeDistance = radius;
            grid.circularFadeFallOff = radius * 0.3f;
            grid.showCells = true;
            grid.Redraw();
            battle = true;
        }

        public void HideBattle()
        {
            if (!Ready || !battle)
            {
                return;
            }
            ClearPaint();
            grid.showCells = false;
            grid.CellSetVisible(hidden, true);
            hidden.Clear();
            grid.Redraw();
            battle = false;
        }

        public void Paint(int cell, Color color)
        {
            if (!Ready || cell < 0)
            {
                return;
            }
            grid.CellToggleRegionSurface(cell, true, color);
            painted.Add(cell);
        }

        public void Paint(IEnumerable<int> cells, Color color)
        {
            foreach (int cell in cells)
            {
                Paint(cell, color);
            }
        }

        public void Unpaint(int cell)
        {
            if (Ready && painted.Remove(cell))
            {
                grid.CellHideRegionSurface(cell);
            }
        }

        public void ClearPaint()
        {
            if (!Ready)
            {
                return;
            }
            foreach (int cell in painted)
            {
                grid.CellHideRegionSurface(cell);
            }
            painted.Clear();
        }
    }
}
