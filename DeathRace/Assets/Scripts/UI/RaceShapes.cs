using Gamebox.UI;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Small white shapes the windows and the HUD tint and draw where a game's font may have no glyph (★, ▲, a padlock):
    /// drawn once in code (<see cref="OverlayKit.ShapeSprite"/>) and kept for the whole run, so a rebuilt window costs
    /// nothing.
    /// </summary>
    public static class RaceShapes
    {
        private static Sprite star, locked, up, down, dot, ring, flag, rounded;

        /// <summary>A white rounded rectangle to slice (the panels of the HUD).</summary>
        public static Sprite Rounded => rounded != null ? rounded : rounded = OverlayKit.RoundedSprite(32, 11);

        /// <summary>A five-pointed star.</summary>
        public static Sprite Star => star != null ? star : star = OverlayKit.ShapeSprite("RaceStar", InStar);

        /// <summary>A padlock, for what is not open yet.</summary>
        public static Sprite Lock => locked != null ? locked : locked = OverlayKit.ShapeSprite("RaceLock", InLock);

        /// <summary>A triangle pointing up (a driver who climbed the ladder).</summary>
        public static Sprite Up => up != null ? up : up = OverlayKit.ShapeSprite("RaceUp", (x, y) => y > -0.3f && y < 0.32f && Mathf.Abs(x) < (0.32f - y) * 0.72f);

        /// <summary>A triangle pointing down (a driver who dropped).</summary>
        public static Sprite Down => down != null ? down : down = OverlayKit.ShapeSprite("RaceDown", (x, y) => y < 0.3f && y > -0.32f && Mathf.Abs(x) < (0.32f + y) * 0.72f);

        /// <summary>A round dot (a level pip, a car on the map).</summary>
        public static Sprite Dot => dot != null ? dot : dot = OverlayKit.ShapeSprite("RaceDot", (x, y) => x * x + y * y < 0.2f);

        /// <summary>A ring (a level still to buy).</summary>
        public static Sprite Ring => ring != null ? ring : ring = OverlayKit.ShapeSprite("RaceRing", (x, y) =>
        {
            float r = x * x + y * y;
            return r < 0.2f && r > 0.11f;
        });

        /// <summary>A chequered flag on its pole.</summary>
        public static Sprite Flag => flag != null ? flag : flag = OverlayKit.ShapeSprite("RaceFlag", InFlag);

        private static bool InStar(float x, float y)
        {
            // Ten corners, alternating between the points (radius 0.48) and the inner corners (0.2), the first pointing up.
            const int corners = 10;
            bool inside = false;
            for (int i = 0, j = corners - 1; i < corners; j = i++)
            {
                Vector2 a = Corner(i);
                Vector2 b = Corner(j);
                if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static Vector2 Corner(int index)
        {
            float radius = index % 2 == 0 ? 0.48f : 0.2f;
            float angle = Mathf.PI * 0.5f + index * Mathf.PI / 5f;
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius - 0.03f);
        }

        private static bool InLock(float x, float y)
        {
            bool body = Mathf.Abs(x) < 0.3f && y > -0.42f && y < 0.04f;
            float dx = x, dy = y - 0.04f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            bool shackle = dy > 0f && r > 0.14f && r < 0.24f;
            bool hole = Mathf.Abs(x) < 0.05f && y > -0.26f && y < -0.1f;
            return (body && !hole) || shackle;
        }

        private static bool InFlag(float x, float y)
        {
            bool pole = x > -0.42f && x < -0.34f && y > -0.46f && y < 0.44f;
            bool cloth = x > -0.34f && x < 0.42f && y > -0.02f && y < 0.44f;
            bool white = (Mathf.FloorToInt((x + 0.34f) / 0.19f) + Mathf.FloorToInt((y + 0.02f) / 0.115f)) % 2 == 0;
            return pole || (cloth && white) || (cloth && (y < 0.01f || y > 0.41f || x > 0.39f));
        }
    }
}
