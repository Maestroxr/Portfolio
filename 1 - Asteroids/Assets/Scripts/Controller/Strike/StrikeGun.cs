using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The attack clock of a strike unit or boss part (design 1.4): every <see cref="Interval"/> seconds an attack of
    /// <see cref="Burst"/> volleys <see cref="BurstGap"/> seconds apart, alternating with a second pattern when there is
    /// one (the bomber's rockets), after a warning of <see cref="Telegraph"/> seconds when it has one (the laser tower's
    /// glow). It runs only while its unit may fire (on screen); leaving the screen breaks off an attack. It only says
    /// when a volley goes and which pattern it is; the unit fires it (<see cref="Enemy"/>'s strike volley). Plain C#, so
    /// the pure parts (the fan of a volley) can be tested on their own.
    /// </summary>
    public sealed class StrikeGun
    {
        private float timer;
        private float gapTimer;
        private int volleysLeft;
        private float warning;
        private bool alternateNext;

        public AttackPattern Pattern { get; private set; }

        /// <summary>The pattern of every other attack; None for one pattern.</summary>
        public AttackPattern Alternate { get; private set; }

        public EnemyShotKind Kind { get; private set; }

        public EnemyShotKind AlternateKind { get; private set; }

        /// <summary>Volleys per attack.</summary>
        public int Burst { get; private set; } = 1;

        /// <summary>Seconds between the volleys of an attack.</summary>
        public float BurstGap { get; private set; }

        /// <summary>Seconds between attacks.</summary>
        public float Interval { get; private set; }

        /// <summary>Seconds of warning before each attack (0 for none).</summary>
        public float Telegraph { get; private set; }

        /// <summary>The pattern of the attack under way (or the last one).</summary>
        public AttackPattern CurrentPattern { get; private set; }

        /// <summary>The shot kind of the attack under way (or the last one).</summary>
        public EnemyShotKind CurrentKind { get; private set; }

        /// <summary>Whether the gun fires at all.</summary>
        public bool IsArmed => Pattern != AttackPattern.None && Interval > 0f;

        /// <summary>Whether the gun is warning of its next attack (show the glow).</summary>
        public bool IsWarning => warning > 0f;


        /// <summary>Sets what the gun fires and how often; the clock starts again with <see cref="Restart"/>.</summary>
        public void Configure(AttackPattern pattern, EnemyShotKind kind, int burst, float burstGap, float interval,
            AttackPattern alternate = AttackPattern.None, EnemyShotKind alternateKind = EnemyShotKind.Plasma, float telegraph = 0f)
        {
            Pattern = pattern;
            Kind = kind;
            Burst = Mathf.Max(1, burst);
            BurstGap = Mathf.Max(0f, burstGap);
            Interval = interval;
            Alternate = alternate;
            AlternateKind = alternateKind;
            Telegraph = Mathf.Max(0f, telegraph);
            CurrentPattern = pattern;
            CurrentKind = kind;
        }


        /// <summary>Starts the clock: the first attack comes after <paramref name="firstDelay"/> seconds of firing time.</summary>
        public void Restart(float firstDelay)
        {
            timer = Mathf.Max(0f, firstDelay);
            gapTimer = 0f;
            volleysLeft = 0;
            warning = 0f;
            alternateNext = false;
            CurrentPattern = Pattern;
            CurrentKind = Kind;
        }


        /// <summary>
        /// Advances the clock by <paramref name="deltaTime"/> while <paramref name="canFire"/>. True when a volley goes now;
        /// <see cref="CurrentPattern"/> and <see cref="CurrentKind"/> say what it is.
        /// </summary>
        public bool Tick(float deltaTime, bool canFire)
        {
            if (!IsArmed)
            {
                return false;
            }
            if (!canFire)
            {
                volleysLeft = 0;
                warning = 0f;
                return false;
            }
            if (volleysLeft > 0)
            {
                gapTimer -= deltaTime;
                if (gapTimer > 0f)
                {
                    return false;
                }
                volleysLeft--;
                gapTimer += BurstGap;
                return true;
            }
            if (warning > 0f)
            {
                warning -= deltaTime;
                if (warning > 0f)
                {
                    return false;
                }
                warning = 0f;
                return StartBurst();
            }
            timer -= deltaTime;
            if (timer > 0f)
            {
                return false;
            }
            timer = Mathf.Max(timer + Interval, 0f);
            bool alternate = alternateNext && Alternate != AttackPattern.None;
            CurrentPattern = alternate ? Alternate : Pattern;
            CurrentKind = alternate ? AlternateKind : Kind;
            alternateNext = !alternateNext;
            if (Telegraph > 0f)
            {
                warning = Telegraph;
                return false;
            }
            return StartBurst();
        }


        private bool StartBurst()
        {
            volleysLeft = Burst - 1;
            gapTimer = BurstGap;
            return true;
        }


        // ------------------------------------------------------------------ the shape of a volley (pure)

        /// <summary>Whether the shots of <paramref name="pattern"/> fly at the nearest pilot (else straight down the screen).</summary>
        public static bool Aims(AttackPattern pattern)
        {
            switch (pattern)
            {
                case AttackPattern.Aimed:
                case AttackPattern.AimedBurst:
                case AttackPattern.Rockets:
                case AttackPattern.HeavyPlasma:
                    return true;
                default:
                    return false;
            }
        }


        /// <summary>
        /// Degrees shot <paramref name="index"/> of <paramref name="count"/> turns away from the volley's direction: Angled
        /// fans from -45 to +45, Spread over up to +-60 (15 degrees apart); the other patterns fly side by side.
        /// </summary>
        public static float FanAngle(AttackPattern pattern, int index, int count)
        {
            if (count <= 1)
            {
                return 0f;
            }
            float t = Mathf.Clamp01(index / (count - 1f));
            switch (pattern)
            {
                case AttackPattern.Angled:
                    return Mathf.Lerp(-45f, 45f, t);
                case AttackPattern.Spread:
                {
                    float half = Mathf.Min(60f, 15f * (count - 1));
                    return Mathf.Lerp(-half, half, t);
                }
                default:
                    return 0f;
            }
        }


        /// <summary>
        /// Sideways offset (m) of shot <paramref name="index"/> of <paramref name="count"/> leaving a unit of
        /// <paramref name="radius"/> without muzzles: side by side across 70% of it, centred.
        /// </summary>
        public static float Lateral(int index, int count, float radius)
        {
            if (count <= 1)
            {
                return 0f;
            }
            return Mathf.Lerp(-0.35f, 0.35f, index / (count - 1f)) * radius;
        }


        /// <summary>Rotates <paramref name="direction"/> by <paramref name="degrees"/> (counterclockwise on screen).</summary>
        public static Vector2 Rotate(Vector2 direction, float degrees)
        {
            if (Mathf.Approximately(degrees, 0f))
            {
                return direction;
            }
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
        }


        /// <summary>The direction of a heading in degrees (0 = up the screen, 90 = left).</summary>
        public static Vector2 HeadingDirection(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(radians), Mathf.Cos(radians));
        }


        /// <summary>The heading in degrees (0 = up the screen, 90 = left) of <paramref name="direction"/>.</summary>
        public static float HeadingOf(Vector2 direction)
        {
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        }
    }
}
