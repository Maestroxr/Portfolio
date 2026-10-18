using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// An ion storm: a crackling cloud that drifts through the sector for a while. A ship inside it is dragged down and
    /// its shield drains away, and the storm calls lightning (<see cref="ZoneShot"/>) on what is inside it, a ring
    /// warning a moment before each strike. Like a black hole it swells as it opens and shrinks as it closes. Every client
    /// drags and drains its own ships; only the simulator calls the lightning, which arrives elsewhere as enemy shots.
    /// </summary>
    public class IonStorm : SpaceBody
    {
        [SerializeField] internal float growTime = 1.5f;
        [Tooltip("How much of a ship's speed the storm takes away per second.")]
        [SerializeField] internal float drag = 1.4f;
        [Tooltip("Shield points a ship inside loses per second.")]
        [SerializeField] internal float shieldDrain = 30f;
        [Tooltip("Seconds between two lightning strikes.")]
        [SerializeField] internal float strikeInterval = 1.15f;
        [SerializeField] internal float drift = 0.7f;
        [Tooltip("Parts of the cloud that turn, each its own way.")]
        [SerializeField] internal Transform[] swirls = new Transform[0];
        [SerializeField] internal float swirlSpeed = 25f;
        [Tooltip("Arcs that flicker on and off inside the cloud.")]
        [SerializeField] internal Renderer[] arcs = new Renderer[0];
        [SerializeField] internal AudioSource crackle;

        private Vector3 restScale = Vector3.one;
        private float strikeTimer;
        private float flicker;

        /// <summary>0 while opening and closing, 1 at full strength.</summary>
        public float Strength01 { get; private set; }


        private void Awake()
        {
            if (visual != null)
            {
                restScale = visual.localScale;
            }
        }


        public override void OnSpawned()
        {
            base.OnSpawned();
            wraps = true;
            pulledByGravity = false;
            Strength01 = 0f;
            strikeTimer = strikeInterval * 1.5f;
            if (visual != null)
            {
                visual.localScale = Vector3.zero;
            }
            if (crackle != null)
            {
                crackle.Play();
            }
        }


        /// <summary>The simulator's storm sets off on a slow drift.</summary>
        public void Begin(Vector2 at)
        {
            Position = at;
            Velocity = Random.insideUnitCircle.normalized * drift;
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            float open = Mathf.Clamp01(Age / growTime);
            float close = lifetime > 0f ? Mathf.Clamp01((lifetime - Age) / growTime) : 1f;
            Strength01 = Mathf.SmoothStep(0f, 1f, Mathf.Min(open, close));
            if (visual != null)
            {
                visual.localScale = restScale * Strength01;
            }
            for (int i = 0; i < swirls.Length; i++)
            {
                if (swirls[i] != null)
                {
                    float speed = swirlSpeed * (i % 2 == 0 ? 1f : -1.4f);
                    swirls[i].localRotation = Quaternion.Euler(0f, 0f, speed * deltaTime) * swirls[i].localRotation;
                }
            }
            flicker -= deltaTime;
            if (flicker <= 0f)
            {
                flicker = Random.Range(0.05f, 0.18f);
                for (int i = 0; i < arcs.Length; i++)
                {
                    if (arcs[i] != null)
                    {
                        arcs[i].enabled = Strength01 > 0.3f && Random.value < 0.45f;
                        arcs[i].transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                    }
                }
            }
            if (crackle != null)
            {
                crackle.volume = 0.35f * Strength01;
            }
            if (Field == null || Strength01 < 0.5f)
            {
                return;
            }
            for (int i = 0; i < Field.LocalShipCount; i++)
            {
                AsteroidsPlayer ship = Field.LocalShip(i);
                if (ship == null || !ship.IsAlive || !Inside(ship.Position, ship.Radius))
                {
                    continue;
                }
                ship.Push(-ship.Velocity * Mathf.Min(1f, drag * deltaTime));
                if (ship.Shield > 0f)
                {
                    ship.RestoreShield(-shieldDrain * deltaTime);
                }
            }
            if (!IsPuppet)
            {
                CallLightning(deltaTime);
            }
        }


        /// <summary>Whether a circle at <paramref name="center"/> is inside the cloud (across the wrapping edges).</summary>
        public bool Inside(Vector2 center, float size)
        {
            Vector2 offset = Field != null && Field.Playground != null ? Field.Playground.Delta(Position, center) : center - Position;
            float reach = radius * Strength01 + size * 0.5f;
            return offset.sqrMagnitude <= reach * reach;
        }


        private void CallLightning(float deltaTime)
        {
            strikeTimer -= deltaTime;
            if (strikeTimer > 0f || Field.Spawner == null)
            {
                return;
            }
            strikeTimer = strikeInterval * Random.Range(0.75f, 1.25f);
            // A ship inside draws the lightning; otherwise it strikes somewhere in the cloud.
            Vector2 at = Position + Random.insideUnitCircle * radius * 0.8f;
            AsteroidsPlayer ship = Field.NearestShip(Position);
            if (ship != null && Inside(ship.Position, 0f) && Random.value < 0.7f)
            {
                at = ship.Position + ship.Velocity * 0.5f + Random.insideUnitCircle * 0.6f;
            }
            Field.Spawner.FireEnemyShot(EnemyShotKind.Lightning, at, Vector2.zero);
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            if (crackle != null)
            {
                crackle.Stop();
            }
        }
    }
}
