using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// A battle fought on the map itself (<see cref="BattleStyle.OnTheMap"/>): the field is the block of map cells
    /// around the place the armies met, its hexagons drawn on the terrain by the map's own grid, and the camera over the
    /// map frames it and holds still until the battle is over. The heroes and the guard standing on the field, and the
    /// trees at the edges of its open ground, make room for the stacks while it lasts, and the shroud over the land not
    /// yet explored lifts off it.
    /// </summary>
    public sealed class MapBattlefield : IBattlefield
    {
        /// <summary>How many degrees further down than over the map the camera looks at a battle.</summary>
        private const float Steeper = 16f;

        private readonly MapView map;
        private readonly CameraRig rig;
        private readonly Transform root;
        private readonly Dictionary<int, Color> painted = new Dictionary<int, Color>();
        private readonly List<GameObject> hidden = new List<GameObject>();
        private BattleState battle;
        private Vector3 savedGoal;
        private float savedDistance;
        private bool savedLock;
        private PlayerState savedViewer;
        private bool open;
        private int marked = -1;

        public MapBattlefield(MapView map, CameraRig rig, Transform root)
        {
            this.map = map;
            this.rig = rig;
            this.root = root;
        }

        public Camera Camera => rig != null && rig.View != null ? rig.View : Camera.main;

        public Transform Root => root;

        public float CellWidth => map.Layout.CellWidth;

        public float UnitScale => 1f;

        public bool IsDragging => rig != null && rig.IsDragging;

        public Vector3 Point(int cell)
        {
            return map.Point(cell);
        }

        /// <summary>
        /// The cell of the field whose middle, as the camera sees it, is nearest the pointer: the ground of the map rises
        /// and falls, and a cell behind a crag or a tree is still picked where it lies rather than where the crag is.
        /// </summary>
        public int CellAt(Vector3 screen, out Vector3 ground)
        {
            ground = default;
            Camera view = Camera;
            if (view == null || battle == null)
            {
                return -1;
            }
            int best = -1;
            float closest = float.MaxValue;
            var pointer = new Vector2(screen.x, screen.y);
            foreach (int cell in battle.cells)
            {
                Vector3 seen = view.WorldToScreenPoint(map.Point(cell));
                if (seen.z <= 0f)
                {
                    continue;
                }
                float distance = (new Vector2(seen.x, seen.y) - pointer).sqrMagnitude;
                if (distance < closest)
                {
                    closest = distance;
                    best = cell;
                }
            }
            if (best < 0)
            {
                return -1;
            }
            // No farther than a cell's own size on the screen from its middle: off the field is off it.
            Vector3 middle = map.Point(best);
            Vector3 center = view.WorldToScreenPoint(middle);
            Vector3 side = view.WorldToScreenPoint(middle + view.transform.right * map.Layout.Radius);
            float reach = Mathf.Max(4f, (new Vector2(side.x, side.y) - new Vector2(center.x, center.y)).magnitude * 1.05f);
            if (closest > reach * reach)
            {
                return -1;
            }
            // Where the pointer is on the level of that cell, for which side of it a blow comes from.
            Ray ray = view.ScreenPointToRay(screen);
            var level = new Plane(Vector3.up, middle);
            ground = level.Raycast(ray, out float enter) ? ray.GetPoint(enter) : middle;
            return best;
        }

        public Vector3 Facing(HexSide side)
        {
            return map.Facing((int)side);
        }

        public void HeroSpot(int side, out Vector3 position, out Vector3 facing)
        {
            bool left = side == 0 ? battle == null || battle.attackerLeft : battle != null && !battle.attackerLeft;
            Vector3 center = battle != null ? map.Point(battle.center) : Vector3.zero;
            // Past the end of the field, toward its back row, as the heroes of old stood at the corners of theirs.
            float x = center.x + (left ? -1f : 1f) * (BattleState.HalfWidth + 1.3f) * CellWidth;
            float z = center.z + BattleState.HalfHeight * 1.5f * map.Layout.Radius * 0.6f;
            position = new Vector3(x, 0f, z);
            Terrain terrain = map.Terrain;
            if (terrain != null && terrain.terrainData != null)
            {
                // Never past the edge of the map, for a battle fought at its rim.
                Vector3 low = terrain.transform.position + new Vector3(1.5f, 0f, 1.5f);
                Vector3 high = terrain.transform.position + terrain.terrainData.size - new Vector3(1.5f, 0f, 1.5f);
                position.x = Mathf.Clamp(position.x, low.x, high.x);
                position.z = Mathf.Clamp(position.z, low.z, high.z);
            }
            position.y = terrain != null ? terrain.SampleHeight(position) + terrain.transform.position.y : center.y;
            facing = left ? Vector3.right : Vector3.left;
        }

        /// <summary>
        /// Clears the field for the stacks (the heroes and the monster standing on it step aside, the shroud over the land
        /// not yet explored lifts off it) and frames it inside <paramref name="viewport"/>, the part of the screen the
        /// battle bar leaves free; the camera holds still until <see cref="Close"/> takes it back where it was. Around
        /// the field the map shows what <paramref name="seenBy"/> has explored, the player at this device who fights
        /// (in a hot seat not always the one the map was last shown to); null leaves the shroud as it is.
        /// </summary>
        public void Open(BattleState shown, Rect viewport, PlayerState seenBy = null)
        {
            battle = shown;
            open = true;
            hidden.Clear();
            FogOfWar fog = map.Fog;
            if (fog != null)
            {
                savedViewer = fog.Viewer;
                if (seenBy != null)
                {
                    fog.Show(seenBy);
                }
                fog.Lift(Uncovered());
            }
            var cells = new HashSet<int>(shown.cells);
            foreach (HeroView hero in map.Heroes)
            {
                if (hero != null && hero.gameObject.activeSelf && cells.Contains(hero.Cell))
                {
                    Hide(hero.gameObject);
                }
            }
            ObjectView monster = shown.monsterObject >= 0 ? map.Object(shown.monsterObject) : null;
            if (monster != null)
            {
                Hide(monster.gameObject);
            }
            map.ClearTrees(shown);
            if (rig != null)
            {
                savedGoal = rig.Goal;
                savedDistance = rig.GoalDistance;
                savedLock = rig.Locked;
                rig.Locked = true;
                // Steeper than over the map, to look past the hills and woods around the field down into it.
                rig.Frame(Corners(), viewport, map.Point(shown.center).y, Steeper);
            }
        }

        /// <summary>Puts back what stepped aside and the camera where it was before the battle.</summary>
        public void Close()
        {
            if (!open)
            {
                return;
            }
            open = false;
            HideGrid();
            ClearPaint();
            Mark(-1, Color.clear);
            foreach (GameObject thing in hidden)
            {
                if (thing != null)
                {
                    thing.SetActive(true);
                }
            }
            hidden.Clear();
            map.RestoreTrees();
            FogOfWar fog = map.Fog;
            if (fog != null)
            {
                fog.Lift(null);
                fog.Show(savedViewer);
            }
            savedViewer = null;
            if (rig != null)
            {
                rig.Locked = savedLock;
                rig.Focus(savedGoal, savedDistance, 3f);
            }
        }

        /// <summary>
        /// The cells the shroud lifts off while the battle lasts: the field and a ring around it (the shroud's soft edge
        /// would creep over the stacks at its rim), and where the heroes stand past its ends.
        /// </summary>
        private HashSet<int> Uncovered()
        {
            HexGrid grid = map.Layout.Grid;
            var cells = new HashSet<int>();
            void Around(int cell)
            {
                cells.Add(cell);
                foreach (int next in grid.Neighbors(cell))
                {
                    cells.Add(next);
                }
            }
            foreach (int cell in battle.cells)
            {
                Around(cell);
            }
            for (int side = 0; side < 2; side++)
            {
                HeroSpot(side, out Vector3 spot, out _);
                int at = map.Layout.CellAt(spot);
                if (at >= 0)
                {
                    Around(at);
                }
            }
            return cells;
        }

        private void Hide(GameObject thing)
        {
            thing.SetActive(false);
            hidden.Add(thing);
        }

        /// <summary>
        /// The corners of the block of the field and the places of its heroes, at the height of the middle of the field,
        /// for the camera to take in: the height of the woods and crags on and around it would push the camera back from
        /// the troops, or tip the picture off them.
        /// </summary>
        private List<Vector3> Corners()
        {
            float ground = map.Point(battle.center).y;
            float left = float.MaxValue, right = float.MinValue, near = float.MaxValue, far = float.MinValue;
            foreach (int cell in battle.cells)
            {
                Vector3 point = map.Point(cell);
                left = Mathf.Min(left, point.x);
                right = Mathf.Max(right, point.x);
                near = Mathf.Min(near, point.z);
                far = Mathf.Max(far, point.z);
            }
            var points = new List<Vector3>
            {
                new Vector3(left, ground, near), new Vector3(right, ground, near),
                new Vector3(left, ground + 2f, far), new Vector3(right, ground + 2f, far)
            };
            for (int side = 0; side < 2; side++)
            {
                // Only where a hero stands: the empty corner of a wandering army is not worth the room.
                if (battle.HeroOf(side) >= 0)
                {
                    HeroSpot(side, out Vector3 spot, out _);
                    points.Add(new Vector3(spot.x, ground + 2.5f, spot.z));
                }
            }
            return points;
        }

        public void ShowGrid()
        {
            if (battle != null && map.Grid != null)
            {
                float radius = (BattleState.HalfWidth + 1.5f) * CellWidth;
                map.Grid.ShowBattle(battle.cells, map.Point(battle.center), radius);
            }
        }

        public void HideGrid()
        {
            map.Grid?.HideBattle();
        }

        public void Paint(int cell, Color color)
        {
            painted[cell] = color;
            if (cell != marked)
            {
                map.Grid?.Paint(cell, color);
            }
        }

        public void Unpaint(int cell)
        {
            painted.Remove(cell);
            if (cell != marked)
            {
                map.Grid?.Unpaint(cell);
            }
        }

        public void ClearPaint()
        {
            painted.Clear();
            map.Grid?.ClearPaint();
            marked = -1;
        }

        /// <summary>The grid of the map has no rings: the cell under the pointer is painted over, and given back its paint after.</summary>
        public void Mark(int cell, Color color)
        {
            if (map.Grid == null)
            {
                return;
            }
            if (marked >= 0 && marked != cell)
            {
                if (painted.TryGetValue(marked, out Color under))
                {
                    map.Grid.Paint(marked, under);
                }
                else
                {
                    map.Grid.Unpaint(marked);
                }
            }
            marked = cell;
            if (cell >= 0)
            {
                Color over = color;
                if (painted.TryGetValue(cell, out Color under))
                {
                    over = Color.Lerp(under, color, 0.6f);
                    over.a = Mathf.Max(under.a, color.a);
                }
                map.Grid.Paint(cell, over);
            }
        }

        /// <summary>On the map a tower stands on the town itself, which stays as it is when the tower falls.</summary>
        public void TowerFell(int cell)
        {
        }
    }
}
