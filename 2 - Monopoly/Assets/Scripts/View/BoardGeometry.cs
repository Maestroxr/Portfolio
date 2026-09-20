using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The layout of a square board of 40 spaces in board units (x to the right, y towards the far side, origin in the
    /// middle): GO in the near right corner, spaces running clockwise seen from above, ten per side. Corners are
    /// <see cref="Corner"/> square, the other spaces <see cref="Width"/> wide and <see cref="Corner"/> deep. The board
    /// view, the board texture and the scene builder all place things with it.
    /// </summary>
    public readonly struct BoardGeometry
    {
        public readonly float Corner;
        public readonly float Width;

        public BoardGeometry(float corner, float width)
        {
            Corner = corner;
            Width = width;
        }

        public float Side => 2f * Corner + 9f * Width;

        public float Half => Side * 0.5f;

        /// <summary>Which side a space is on: 0 near, 1 left, 2 far, 3 right. Corners belong to the side they start.</summary>
        public static int SideOf(int space)
        {
            return (space / 10) % 4;
        }

        public static bool IsCorner(int space)
        {
            return space % 10 == 0;
        }

        /// <summary>Rotation of a space's frame in 3D (y up): local z points to the board centre, local x along the side as text reads.</summary>
        public static Quaternion Frame(int space)
        {
            return Quaternion.Euler(0f, 90f * SideOf(space), 0f);
        }

        /// <summary>The unit vector (x, y of the board) from a space's outer edge towards the board centre.</summary>
        public static Vector2 Inward(int space)
        {
            switch (SideOf(space))
            {
                case 0: return new Vector2(0f, 1f);
                case 1: return new Vector2(1f, 0f);
                case 2: return new Vector2(0f, -1f);
                default: return new Vector2(-1f, 0f);
            }
        }

        /// <summary>The unit vector along a space's side, the way its text reads from outside the board.</summary>
        public static Vector2 Along(int space)
        {
            Vector2 inward = Inward(space);
            return new Vector2(inward.y, -inward.x);
        }

        /// <summary>The centre of a space.</summary>
        public Vector2 Center(int space)
        {
            float edge = Half - Corner * 0.5f;
            int k = space % 10;
            float along = k == 0 ? 0f : Half - Corner - (k - 0.5f) * Width;
            switch (SideOf(space))
            {
                case 0: return k == 0 ? new Vector2(edge, -edge) : new Vector2(along, -edge);
                case 1: return k == 0 ? new Vector2(-edge, -edge) : new Vector2(-edge, -along);
                case 2: return k == 0 ? new Vector2(-edge, edge) : new Vector2(-along, edge);
                default: return k == 0 ? new Vector2(edge, edge) : new Vector2(edge, along);
            }
        }

        /// <summary>A space's size: along its side, and its depth.</summary>
        public Vector2 Size(int space)
        {
            return IsCorner(space) ? new Vector2(Corner, Corner) : new Vector2(Width, Corner);
        }

        /// <summary>The axis aligned rectangle a space covers.</summary>
        public Rect Bounds(int space)
        {
            Vector2 center = Center(space);
            Vector2 size = Size(space);
            Vector2 inward = Inward(space);
            Vector2 extent = Mathf.Abs(inward.y) > 0.5f ? new Vector2(size.x, size.y) : new Vector2(size.y, size.x);
            return new Rect(center - extent * 0.5f, extent);
        }

        /// <summary>
        /// A rectangle inside a space, given in the space's own terms: from <paramref name="depthFrom"/> to
        /// <paramref name="depthTo"/> (0 = outer edge, 1 = inner edge) and across <paramref name="alongFrom"/> to
        /// <paramref name="alongTo"/> (0 to 1 in reading order).
        /// </summary>
        public Rect Part(int space, float depthFrom, float depthTo, float alongFrom = 0f, float alongTo = 1f)
        {
            Vector2 center = Center(space);
            Vector2 size = Size(space);
            Vector2 inward = Inward(space);
            Vector2 along = Along(space);
            Vector2 outer = center - inward * (size.y * 0.5f);
            Vector2 left = -along * (size.x * 0.5f);
            Vector2 a = outer + inward * (size.y * depthFrom) + left + along * (size.x * alongFrom);
            Vector2 b = outer + inward * (size.y * depthTo) + left + along * (size.x * alongTo);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>A point inside a space in its own terms (depth 0 outer edge to 1 inner edge, along 0 to 1 in reading order).</summary>
        public Vector2 Point(int space, float depth, float along)
        {
            Vector2 center = Center(space);
            Vector2 size = Size(space);
            return center + Inward(space) * ((depth - 0.5f) * size.y) + Along(space) * ((along - 0.5f) * size.x);
        }

        /// <summary>The space at a point of the board, or -1 off the track.</summary>
        public int SpaceAt(Vector2 point)
        {
            float x = point.x;
            float z = point.y;
            if (Mathf.Abs(x) > Half || Mathf.Abs(z) > Half)
            {
                return -1;
            }
            float inner = Half - Corner;
            bool nearX = x > inner;
            bool farX = x < -inner;
            bool nearZ = z < -inner;
            bool farZ = z > inner;
            if (nearZ && nearX) return 0;
            if (nearZ && farX) return 10;
            if (farZ && farX) return 20;
            if (farZ && nearX) return 30;
            if (nearZ) return 1 + Mathf.Clamp(Mathf.FloorToInt((inner - x) / Width), 0, 8);
            if (farX) return 11 + Mathf.Clamp(Mathf.FloorToInt((z + inner) / Width), 0, 8);
            if (farZ) return 21 + Mathf.Clamp(Mathf.FloorToInt((x + inner) / Width), 0, 8);
            if (nearX) return 31 + Mathf.Clamp(Mathf.FloorToInt((inner - z) / Width), 0, 8);
            return -1;
        }
    }
}
