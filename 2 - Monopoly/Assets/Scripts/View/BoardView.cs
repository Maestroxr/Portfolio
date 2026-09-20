using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The 3D board: where every space lies, where tokens stand on it, and the pieces that change during a match (owner
    /// tags, mortgage stamps, houses and hotels, highlights). The board is square, centred on its transform, with GO in
    /// the near right corner; spaces run clockwise seen from above, ten per side. Corners are <see cref="corner"/> wide,
    /// the other spaces <see cref="width"/> wide and <see cref="corner"/> deep.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private float corner = 1.6f;
        [SerializeField] private float width = 1f;
        [SerializeField] private float surface = 0.3f;
        [SerializeField] private List<Tile> tiles = new List<Tile>();
        [SerializeField] private Material[] ownerMaterials = new Material[4];
        [SerializeField] private BuildingPool housePool;
        [SerializeField] private BuildingPool hotelPool;
        [Tooltip("Share of the depth of a street taken by its colour bar.")]
        [SerializeField] private float barShare = 0.24f;

        private readonly HashSet<int> pulsing = new HashSet<int>();
        private int hovered = -1;

        public float Corner => corner;
        public float Width => width;
        public float Surface => surface;
        public float Side => 2f * corner + 9f * width;
        public int Count => tiles.Count;
        public IReadOnlyList<Tile> Tiles => tiles;

        public Tile Tile(int space)
        {
            return space >= 0 && space < tiles.Count ? tiles[space] : null;
        }

        // ------------------------------------------------------------------ geometry (board local, y up, z away)

        public static int SideOf(int space)
        {
            return BoardGeometry.SideOf(space);
        }

        public static bool IsCorner(int space)
        {
            return BoardGeometry.IsCorner(space);
        }

        /// <summary>Rotation of a space's frame: local z points to the board centre, local x along the side as text reads.</summary>
        public static Quaternion Frame(int space)
        {
            return BoardGeometry.Frame(space);
        }

        public BoardGeometry Geometry => new BoardGeometry(corner, width);

        /// <summary>The centre of a space on the board surface, in board space.</summary>
        public Vector3 LocalCenter(int space)
        {
            Vector2 p = Geometry.Center(space);
            return new Vector3(p.x, surface, p.y);
        }

        public Vector3 Center(int space)
        {
            return transform.TransformPoint(LocalCenter(space));
        }

        /// <summary>The size of a space: along its side and its depth.</summary>
        public Vector2 SpaceSize(int space)
        {
            return IsCorner(space) ? new Vector2(corner, corner) : new Vector2(width, corner);
        }

        /// <summary>The middle of a street's colour bar, where its houses stand, in board space.</summary>
        public Vector3 LocalBarCenter(int space)
        {
            float depth = corner * barShare;
            return LocalCenter(space) + Frame(space) * new Vector3(0f, 0f, corner * 0.5f - depth * 0.5f);
        }

        public float BarDepth => corner * barShare;

        /// <summary>
        /// Where a token stands: <paramref name="slot"/> of <paramref name="count"/> tokens on the space, jailed tokens
        /// in the cell of the jail corner and visitors around it.
        /// </summary>
        public Vector3 TokenSpot(int space, int slot, int count, bool jailed)
        {
            Quaternion frame = Frame(space);
            Vector3 center = LocalCenter(space);
            Vector3 offset;
            if (IsCorner(space))
            {
                if (space % 40 == 10)
                {
                    // Jail: the cell is the inner part of the corner, visitors stand on the two outer strips.
                    var toCenter = new Vector3(-Mathf.Sign(center.x), 0f, -Mathf.Sign(center.z));
                    if (jailed)
                    {
                        Vector2 cell = Grid(slot, count, 0.2f);
                        offset = toCenter * (corner * 0.16f) + new Vector3(cell.x, 0f, cell.y);
                    }
                    else
                    {
                        float t = count <= 1 ? 0.5f : slot / (float)(count - 1);
                        float strip = corner * 0.37f;
                        offset = t < 0.5f
                            ? new Vector3(toCenter.x * Mathf.Lerp(-strip, strip * 0.8f, t * 2f), 0f, -toCenter.z * strip)
                            : new Vector3(-toCenter.x * strip, 0f, toCenter.z * Mathf.Lerp(-strip * 0.6f, strip * 0.8f, (t - 0.5f) * 2f));
                    }
                    return transform.TransformPoint(center + offset);
                }
                Vector2 g = Grid(slot, count, 0.34f);
                offset = frame * new Vector3(g.x, 0f, g.y);
            }
            else
            {
                Vector2 g = Grid(slot, count, 0.22f);
                offset = frame * new Vector3(g.x, 0f, g.y * 1.2f - corner * 0.1f);
            }
            return transform.TransformPoint(center + offset);
        }

        /// <summary>Offsets of up to four tokens sharing a spot: one in the middle, two side by side, then a square.</summary>
        private static Vector2 Grid(int slot, int count, float spacing)
        {
            if (count <= 1)
            {
                return Vector2.zero;
            }
            if (count == 2)
            {
                return new Vector2(slot == 0 ? -spacing : spacing, 0f);
            }
            int x = slot % 2;
            int y = slot / 2;
            return new Vector2(x == 0 ? -spacing : spacing, y == 0 ? spacing : -spacing);
        }

        /// <summary>The space under a point of the board surface (board space), or -1 off the track.</summary>
        public int SpaceAtLocal(Vector3 local)
        {
            return Geometry.SpaceAt(new Vector2(local.x, local.z));
        }

        /// <summary>The space a ray (from the camera through the pointer) hits, or -1.</summary>
        public int Raycast(Ray ray)
        {
            var plane = new Plane(transform.up, transform.TransformPoint(new Vector3(0f, surface, 0f)));
            if (!plane.Raycast(ray, out float distance))
            {
                return -1;
            }
            return SpaceAtLocal(transform.InverseTransformPoint(ray.GetPoint(distance)));
        }

        // ------------------------------------------------------------------ pieces of the match

        public Material OwnerMaterial(int seat)
        {
            return seat >= 0 && seat < ownerMaterials.Length ? ownerMaterials[seat] : null;
        }

        public void SetOwner(int space, int seat)
        {
            Tile tile = Tile(space);
            if (tile != null)
            {
                tile.SetOwner(seat >= 0 ? OwnerMaterial(seat) : null);
            }
        }

        public void SetMortgaged(int space, bool mortgaged)
        {
            Tile(space)?.SetMortgaged(mortgaged);
        }

        /// <summary>Shows <paramref name="houses"/> houses (5: a hotel) on a street, popping new buildings in when animated.</summary>
        public void SetBuildings(int space, int houses, bool animate)
        {
            Tile(space)?.SetBuildings(houses, housePool, hotelPool, animate);
        }

        /// <summary>Clears every owner tag, stamp, building and highlight (a new match).</summary>
        public void ResetPieces()
        {
            foreach (Tile tile in tiles)
            {
                tile.SetOwner(null);
                tile.SetMortgaged(false);
                tile.SetBuildings(0, housePool, hotelPool, false);
                tile.SetHighlight(false, Color.white);
            }
            pulsing.Clear();
            hovered = -1;
        }

        /// <summary>Makes a space glow (a landing, a selection, a destination to pick).</summary>
        public void Highlight(int space, Color color, bool pulse = true)
        {
            Tile tile = Tile(space);
            if (tile == null)
            {
                return;
            }
            tile.SetHighlight(true, color);
            if (pulse)
            {
                pulsing.Add(space);
            }
        }

        public void ClearHighlight(int space)
        {
            Tile(space)?.SetHighlight(false, Color.white);
            pulsing.Remove(space);
        }

        public void ClearHighlights()
        {
            foreach (Tile tile in tiles)
            {
                tile.SetHighlight(false, Color.white);
            }
            pulsing.Clear();
            hovered = -1;
        }

        /// <summary>A soft glow under the pointer, for boards that take a pick.</summary>
        public void Hover(int space, Color color)
        {
            if (space == hovered)
            {
                return;
            }
            if (hovered >= 0 && !pulsing.Contains(hovered))
            {
                Tile(hovered)?.SetHighlight(false, Color.white);
            }
            hovered = space;
            if (space >= 0 && !pulsing.Contains(space))
            {
                Tile(space)?.SetHighlight(true, color);
            }
        }

        private void Update()
        {
            if (pulsing.Count == 0)
            {
                return;
            }
            float glow = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (int space in pulsing)
            {
                Tile(space)?.SetHighlightStrength(glow);
            }
        }
    }
}
