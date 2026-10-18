using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The numbers of what the Iron Tide and Hive Fall campaigns brought to the strike mode beyond the unit table: the
    /// artillery shell's flight, the shield generator's reach, the launches of hive spawners and hangars, the homing of
    /// spores and seeker rockets, and the shape of the new attack patterns (rings, sweeps and barrages). Plain and
    /// deterministic, so the patterns can be tested on their own.
    /// </summary>
    public static class StrikeCampaignRules
    {
        // ------------------------------------------------------------------ artillery

        /// <summary>Seconds an artillery shell flies to the spot it was aimed at (its landing ring shows all the while).</summary>
        public const float ShellFlightTime = 1.6f;

        /// <summary>Radius (m) of a shell's burst.</summary>
        public const float ShellBurstRadius = 1.9f;

        /// <summary>Meters the shells of one barrage land apart, around the aimed spot (the first one on it).</summary>
        public const float BarrageScatter = 2.4f;

        // ------------------------------------------------------------------ shields

        /// <summary>Radius (m) of a shield generator's dome: the ground units whose centre is inside cannot be hurt.</summary>
        public const float ShieldRadius = 6.5f;

        // ------------------------------------------------------------------ launches

        /// <summary>Seconds between the launches of a hive spawner.</summary>
        public const float SpawnerInterval = 5.5f;

        /// <summary>Launches a hive spawner makes before it is spent.</summary>
        public const int SpawnerLaunches = 3;

        /// <summary>Seconds between the aircraft of one launch.</summary>
        public const float LaunchGap = 0.35f;

        // ------------------------------------------------------------------ homing shots

        /// <summary>Degrees per second a spore turns after the nearest pilot, and for how many seconds.</summary>
        public const float SporeHoming = 28f;
        public const float SporeHomingTime = 3.5f;

        /// <summary>Degrees per second a seeker rocket turns after the nearest pilot, and for how many seconds.</summary>
        public const float SeekerHoming = 60f;
        public const float SeekerHomingTime = 1.5f;

        // ------------------------------------------------------------------ patterns

        /// <summary>Degrees either way a sweeping fan swings from straight down.</summary>
        public const float SweepSwing = 55f;

        /// <summary>Swings of a sweeping fan per second (radians of its sine per second).</summary>
        public const float SweepRate = 1.7f;

        /// <summary>Degrees between the shots of a sweeping fan.</summary>
        public const float SweepFan = 11f;


        /// <summary>
        /// The direction of shot <paramref name="index"/> of a ring of <paramref name="count"/>: evenly round, starting
        /// straight down, every other ring (<paramref name="odd"/>) turned by half a step so the rings interleave.
        /// </summary>
        public static Vector2 RingDirection(int index, int count, bool odd)
        {
            count = Mathf.Max(1, count);
            float step = 360f / count;
            return StrikeGun.Rotate(Vector2.down, index * step + (odd ? step * 0.5f : 0f));
        }


        /// <summary>
        /// The direction of shot <paramref name="index"/> of <paramref name="count"/> of a sweeping fan fired after
        /// <paramref name="time"/> seconds (the unit's age, so every client agrees): the fan's middle swings from side to side.
        /// </summary>
        public static Vector2 SweepDirection(int index, int count, float time)
        {
            float middle = SweepSwing * Mathf.Sin(time * SweepRate);
            float offset = count > 1 ? (index - (count - 1) * 0.5f) * SweepFan : 0f;
            return StrikeGun.Rotate(Vector2.down, middle + offset);
        }


        /// <summary>
        /// Where shell <paramref name="index"/> of a barrage aimed at <paramref name="aimed"/> lands: the first on the spot,
        /// the others around it at <see cref="BarrageScatter"/> m, a third of a turn apart.
        /// </summary>
        public static Vector2 BarrageSpot(Vector2 aimed, int index)
        {
            if (index <= 0)
            {
                return aimed;
            }
            float angle = (90f + index * 120f) * Mathf.Deg2Rad;
            return aimed + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * BarrageScatter;
        }


        /// <summary>The velocity that takes a shell from <paramref name="from"/> to <paramref name="to"/> in <see cref="ShellFlightTime"/>.</summary>
        public static Vector2 ShellVelocity(Vector2 from, Vector2 to)
        {
            return (to - from) / ShellFlightTime;
        }


        /// <summary>Whether a ground unit at <paramref name="unit"/> of <paramref name="radius"/> is inside the dome of a generator at <paramref name="generator"/>.</summary>
        public static bool InsideShield(Vector2 generator, Vector2 unit, float radius)
        {
            float reach = ShieldRadius + radius * 0.5f;
            return (unit - generator).sqrMagnitude <= reach * reach;
        }
    }
}
