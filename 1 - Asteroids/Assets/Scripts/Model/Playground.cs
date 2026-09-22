using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The wrapping playfield on the plane z = 0: as high as <see cref="halfHeight"/> times two and as wide as the
    /// camera shows. Whatever flies out of one side comes back in on the other.
    /// </summary>
    public class Playground : MonoBehaviour
    {
        [SerializeField] internal Camera view;
        [Tooltip("Half the height of the playfield in meters; the camera is placed so that it shows exactly this much.")]
        [SerializeField] internal float halfHeight = 10f;

        [field: SerializeField]
        public Vector3 Middle { get; private set; }

        [field: SerializeField, Tooltip("Extra room beyond the visible edge before something wraps around.")]
        public Vector3 Margin { get; private set; } = new Vector3(0.2f, 0.2f, 0f);

        private Vector2? fixedHalfSize;

        public float Aspect => view != null && view.aspect > 0.1f ? view.aspect : 16f / 9f;

        public float HalfHeight => halfHeight;

        /// <summary>
        /// Half the width and height of the playfield: what the camera shows, or the size it was fixed to for a mission
        /// shared with other pilots, whose screens differ.
        /// </summary>
        public Vector2 HalfSize => fixedHalfSize ?? new Vector2(halfHeight * Aspect, halfHeight);

        /// <summary>Whether the playfield has a size of its own instead of the camera's (see <see cref="Fix"/>).</summary>
        public bool IsFixed => fixedHalfSize.HasValue;

        /// <summary>Gives the playfield a size that does not depend on the screen; the camera has to fit it into view.</summary>
        public void Fix(Vector2 halfSize)
        {
            fixedHalfSize = new Vector2(Mathf.Max(1f, halfSize.x), Mathf.Max(1f, halfSize.y));
        }

        /// <summary>The playfield is as wide as the camera shows again.</summary>
        public void Release()
        {
            fixedHalfSize = null;
        }

        /// <summary>Full size of the visible playfield (kept from the original, where it came from a mesh).</summary>
        public Vector3 Size => HalfSize * 2f;

        public Rect Bounds
        {
            get
            {
                Vector2 half = HalfSize;
                return new Rect((Vector2)Middle - half, half * 2f);
            }
        }

        /// <summary>
        /// Where something of <paramref name="radius"/> at <paramref name="position"/> belongs: unchanged while any part of
        /// it could still be on screen, moved to just outside the opposite edge once it has fully left.
        /// </summary>
        public Vector2 Wrap(Vector2 position, float radius)
        {
            Vector2 half = HalfSize + (Vector2)Margin + Vector2.one * radius;
            Vector2 local = position - (Vector2)Middle;
            if (local.x > half.x)
            {
                local.x -= half.x * 2f;
            }
            else if (local.x < -half.x)
            {
                local.x += half.x * 2f;
            }
            if (local.y > half.y)
            {
                local.y -= half.y * 2f;
            }
            else if (local.y < -half.y)
            {
                local.y += half.y * 2f;
            }
            return local + (Vector2)Middle;
        }

        /// <summary>Whether something of <paramref name="radius"/> has fully left the playfield (plus <paramref name="extra"/>).</summary>
        public bool IsOutside(Vector2 position, float radius, float extra = 0f)
        {
            Vector2 half = HalfSize + (Vector2)Margin + Vector2.one * (radius + extra);
            Vector2 local = position - (Vector2)Middle;
            return Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y;
        }

        /// <summary>Whether the point is on screen, <paramref name="inset"/> meters away from the edges.</summary>
        public bool IsInside(Vector2 position, float inset = 0f)
        {
            Vector2 half = HalfSize - Vector2.one * inset;
            Vector2 local = position - (Vector2)Middle;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y;
        }

        /// <summary>Shortest offset from <paramref name="from"/> to <paramref name="to"/> across the wrapping edges.</summary>
        public Vector2 Delta(Vector2 from, Vector2 to)
        {
            Vector2 size = HalfSize * 2f + (Vector2)Margin * 2f;
            Vector2 delta = to - from;
            if (delta.x > size.x * 0.5f)
            {
                delta.x -= size.x;
            }
            else if (delta.x < -size.x * 0.5f)
            {
                delta.x += size.x;
            }
            if (delta.y > size.y * 0.5f)
            {
                delta.y -= size.y;
            }
            else if (delta.y < -size.y * 0.5f)
            {
                delta.y += size.y;
            }
            return delta;
        }

        /// <summary>
        /// How far something of <paramref name="radius"/> travels before it is back where it was: it wraps once it has
        /// fully left, so bigger things go further around. Two positions of the same body are compared with this.
        /// </summary>
        public Vector2 WrapPeriod(float radius)
        {
            return (HalfSize + (Vector2)Margin + Vector2.one * radius) * 2f;
        }

        /// <summary>
        /// A point just outside a random edge for something of <paramref name="radius"/> to enter from, and the direction
        /// into the playfield from there (aimed loosely at the middle).
        /// </summary>
        public Vector2 EdgePoint(float radius, out Vector2 inward, float roll1, float roll2, float roll3)
        {
            Vector2 half = HalfSize + Vector2.one * (radius + 0.1f);
            Vector2 point;
            int side = Mathf.Min(3, Mathf.FloorToInt(roll1 * 4f));
            float along = roll2 * 2f - 1f;
            switch (side)
            {
                case 0: point = new Vector2(-half.x, along * half.y); break;
                case 1: point = new Vector2(half.x, along * half.y); break;
                case 2: point = new Vector2(along * half.x, -half.y); break;
                default: point = new Vector2(along * half.x, half.y); break;
            }
            Vector2 aim = new Vector2((roll3 - 0.5f) * HalfSize.x, (roll2 - 0.5f) * HalfSize.y);
            inward = (aim - point).normalized;
            return point + (Vector2)Middle;
        }

        public Vector2 EdgePoint(float radius, out Vector2 inward)
        {
            return EdgePoint(radius, out inward, Random.value, Random.value, Random.value);
        }

        /// <summary>A random point on screen at least <paramref name="minDistance"/> from <paramref name="avoid"/>.</summary>
        public Vector2 RandomPointAwayFrom(Vector2 avoid, float minDistance, float inset = 1f)
        {
            return RandomPointAwayFrom(candidate => Delta(avoid, candidate).magnitude, minDistance, inset);
        }

        /// <summary>
        /// A random point on screen that <paramref name="clearance"/> puts at least <paramref name="minDistance"/> away
        /// from what has to be avoided (several ships, for one); the best of a dozen tries otherwise.
        /// </summary>
        public Vector2 RandomPointAwayFrom(System.Func<Vector2, float> clearance, float minDistance, float inset = 1f)
        {
            Vector2 half = HalfSize - Vector2.one * inset;
            Vector2 best = (Vector2)Middle;
            float bestDistance = -1f;
            for (int i = 0; i < 12; i++)
            {
                var candidate = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y)) + (Vector2)Middle;
                float distance = clearance(candidate);
                if (distance >= minDistance)
                {
                    return candidate;
                }
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
