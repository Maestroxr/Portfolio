using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A laser tower's beam: the column from its gun down to the bottom edge. It never stops on contact; while it touches
    /// the local ship it burns it continuously (<see cref="EnemyShotKind.Beam"/>: its damage over <see cref="burnTime"/>,
    /// 40 over 0.17 s). On the simulator it moves with its gun and only the simulator expires it; every client burns only
    /// its own ship, for the beam's time at most: a puppet burns for what its simulator's beam had left, then waits dark
    /// for the exit.
    /// </summary>
    public class EnemyBeam : Shot
    {
        [Header("Beam")]
        [Tooltip("Half the width of the column (m).")]
        [SerializeField] internal float halfWidth = 0.35f;
        [Tooltip("Seconds the beam burns: the prefab damage is spread over them.")]
        [SerializeField] internal float burnTime = 0.17f;
        [Tooltip("Stretched from the gun down past the bottom edge: a model one meter tall, centred on its pivot (optional).")]
        [SerializeField] internal Transform column;

        /// <summary>Meters the column reaches below the bottom edge.</summary>
        private const float Overhang = 1f;

        /// <summary>The age at which the beam stops burning: its lifetime, or the time a puppet's simulator had left.</summary>
        private float burnEnd;

        internal override bool StopsOnHit => false;

        /// <summary>Half the width of the column.</summary>
        public float HalfWidth => halfWidth;


        protected override void Awake()
        {
            base.Awake();
            // The column hangs straight down whatever its gun's motion.
            alignToVelocity = false;
        }


        public override void OnSpawned()
        {
            if (lifetime <= 0f)
            {
                lifetime = burnTime;
            }
            base.OnSpawned();
            burnEnd = lifetime;
            transform.rotation = Quaternion.identity;
            ShowColumn(true);
            Stretch();
        }


        /// <summary>
        /// A puppet burns for <paramref name="seconds"/> from now (the time its simulator's beam had left), then only waits
        /// for the simulator's exit: a late exit must not burn the ship here any longer.
        /// </summary>
        internal void BurnFor(float seconds)
        {
            burnEnd = Age + Mathf.Max(0f, seconds);
        }


        /// <summary>Whether the beam still burns (a puppet past its burn waits, dark, for its exit).</summary>
        public bool IsBurning => burnEnd <= 0f || Age < burnEnd;


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay || Field == null)
            {
                return;
            }
            float burn = BurnSeconds(Age, deltaTime, burnEnd);
            if (burn <= 0f)
            {
                ShowColumn(false);
                return;
            }
            Stretch();
            AsteroidsPlayer ship = Field.Player;
            if (ship == null || !ship.IsAlive || !Touches(ship.Position, ship.Radius) || burnTime <= 0f)
            {
                return;
            }
            ship.TakeDamage(new DamageInfo(damage / burnTime * burn, Vector2.down, ship.Position, DamageSource.Enemy, false, this), true);
        }


        /// <summary>
        /// Seconds of the tick of <paramref name="deltaTime"/> that ended at <paramref name="age"/> in which the beam burned:
        /// none past <paramref name="burnEnd"/>, so every client burns its ship for the beam's time at most (no end: all).
        /// </summary>
        internal static float BurnSeconds(float age, float deltaTime, float burnEnd)
        {
            if (burnEnd <= 0f)
            {
                return deltaTime;
            }
            return Mathf.Clamp(burnEnd - (age - deltaTime), 0f, deltaTime);
        }


        /// <summary>Shows or hides the beam's model (its visual, else its column; never the body itself).</summary>
        private void ShowColumn(bool show)
        {
            Transform shown = visual != null ? visual : column;
            if (shown != null && shown != transform && shown.gameObject.activeSelf != show)
            {
                shown.gameObject.SetActive(show);
            }
        }


        /// <summary>The column from the beam down to the bottom edge.</summary>
        internal override bool Touches(Vector2 center, float radius)
        {
            return InColumn(Position, halfWidth, center, radius);
        }


        /// <summary>
        /// Whether a circle of <paramref name="radius"/> at <paramref name="center"/> meets the column of half width
        /// <paramref name="halfWidth"/> that hangs down from <paramref name="top"/>.
        /// </summary>
        public static bool InColumn(Vector2 top, float halfWidth, Vector2 center, float radius)
        {
            return Mathf.Abs(center.x - top.x) <= halfWidth + radius && center.y - radius <= top.y;
        }


        /// <summary>Stretches the column from the gun to just below the bottom edge.</summary>
        private void Stretch()
        {
            if (column == null)
            {
                return;
            }
            Playground playground = Field != null ? Field.Playground : null;
            float bottom = playground != null ? playground.Bottom : -StrikeRules.HalfSize.y;
            float length = Mathf.Max(0.1f, Position.y - bottom + Overhang);
            Vector3 scale = column.localScale;
            column.localScale = new Vector3(scale.x, length, scale.z);
            column.localPosition = new Vector3(column.localPosition.x, -length * 0.5f, column.localPosition.z);
        }
    }
}
