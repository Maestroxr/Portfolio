using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A boss's laser (<see cref="EnemyShotKind.Lance"/> and <see cref="EnemyShotKind.SweepBeam"/>): it stands where it was
    /// fired and reaches <see cref="length"/> meters along its direction. A thin flickering line warns for
    /// <see cref="warningTime"/>, then the beam burns for <see cref="burnTime"/>, every ship it touches losing hull
    /// continuously (<see cref="Shot.Damage"/> per second). A lance stays put; a sweep beam turns through
    /// <see cref="sweep"/> degrees while it burns.
    ///
    /// The beam never moves: its velocity only carries its direction and, for a sweep, the way it turns (a speed of 1
    /// turns it counterclockwise, 2 clockwise), so a puppet elsewhere draws the same beam from the row that announced it.
    /// Every client burns its own ships; the simulator burns the freighter or station.
    /// </summary>
    public class FieldBeam : Shot, IBurnsAllies
    {
        /// <summary>The speed of a sweep beam that turns clockwise (one that turns counterclockwise has speed 1).</summary>
        public const float ClockwiseSpeed = 2f;

        [SerializeField] internal float warningTime = 0.75f;
        [SerializeField] internal float burnTime = 0.7f;
        [SerializeField] internal float length = 34f;
        [SerializeField] internal float halfWidth = 0.42f;
        [Tooltip("Degrees the beam turns while it burns: 0 for a lance.")]
        [SerializeField] internal float sweep;
        [Tooltip("The beam: a quad one unit square, stretched along the direction from the origin.")]
        [SerializeField] internal Transform beam;
        [SerializeField] internal Renderer beamRenderer;
        [Tooltip("The glow at the origin.")]
        [SerializeField] internal Transform source;
        [SerializeField] internal AudioClip chargeClip;
        [SerializeField] internal AudioClip fireClip;

        private static MaterialPropertyBlock block;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private Color beamColor = Color.white;
        private bool burning;
        private bool sounded;

        internal override bool StopsOnHit => false;

        /// <summary>Whether the beam burns now (after its warning).</summary>
        public bool IsBurning => burning;

        /// <summary>Whether a sweep beam turns clockwise.</summary>
        public bool Clockwise => Velocity.magnitude >= (1f + ClockwiseSpeed) * 0.5f;

        /// <summary>The way the beam points now.</summary>
        public Vector2 Direction
        {
            get
            {
                Vector2 start = Velocity.sqrMagnitude > 0.0001f ? Velocity.normalized : Vector2.down;
                if (sweep <= 0f)
                {
                    return start;
                }
                float turn = Clockwise ? -1f : 1f;
                float progress = Mathf.Clamp01((Age - warningTime) / Mathf.Max(0.05f, burnTime));
                return Quaternion.Euler(0f, 0f, turn * sweep * (progress - 0.5f)) * start;
            }
        }


        protected override void Awake()
        {
            base.Awake();
            alignToVelocity = false;
            if (beamRenderer != null && beamRenderer.sharedMaterial != null && beamRenderer.sharedMaterial.HasProperty(ColorId))
            {
                beamColor = beamRenderer.sharedMaterial.GetColor(ColorId);
            }
        }


        public override void OnSpawned()
        {
            if (lifetime <= 0f)
            {
                lifetime = warningTime + burnTime + 0.1f;
            }
            base.OnSpawned();
            burning = false;
            sounded = false;
        }


        public override void Tick(float deltaTime)
        {
            // The beam stays where it was fired: its velocity is only its direction.
            Vector2 at = Position;
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            Position = at;
            if (!sounded)
            {
                sounded = true;
                Field?.Sounds?.Play(chargeClip, 0.5f, 1f, 0.05f, 0.1f);
            }
            bool wasBurning = burning;
            burning = Age >= warningTime && Age < warningTime + burnTime;
            if (burning && !wasBurning)
            {
                Field?.Sounds?.Play(fireClip, 0.7f, 1f, 0.05f, 0.1f);
                Field?.CameraRig?.Shake(0.2f);
            }
            Draw();
            if (!burning || Field == null)
            {
                return;
            }
            for (int i = 0; i < Field.LocalShipCount; i++)
            {
                AsteroidsPlayer ship = Field.LocalShip(i);
                if (ship == null || !ship.IsAlive || !Touches(ship.Position, ship.Radius))
                {
                    continue;
                }
                Vector2 side = new Vector2(-Direction.y, Direction.x);
                Vector2 away = Vector2.Dot(ship.Position - Position, side) >= 0f ? side : -side;
                ship.TakeDamage(new DamageInfo(damage * deltaTime, away, ship.Position, DamageSource.Enemy, false, this), true);
            }
        }


        private void Draw()
        {
            Vector2 direction = Direction;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            float end = warningTime + burnTime;
            bool shown = Age < end;
            if (beam != null)
            {
                beam.gameObject.SetActive(shown);
                float width = burning ? halfWidth * 2.4f * (0.9f + 0.1f * Mathf.Sin(Age * 60f)) : 0.1f;
                beam.localPosition = new Vector3(0f, length * 0.5f, 0f);
                beam.localScale = new Vector3(width, length, 1f);
            }
            if (source != null)
            {
                source.gameObject.SetActive(shown);
                float grow = burning ? 1.6f : Mathf.Clamp01(Age / Mathf.Max(0.05f, warningTime));
                source.localScale = Vector3.one * (0.6f + grow);
            }
            if (beamRenderer != null)
            {
                float alpha = burning ? 1f : 0.35f + 0.35f * Mathf.Sin(Age * 30f);
                block ??= new MaterialPropertyBlock();
                block.Clear();
                block.SetColor(ColorId, new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a * alpha));
                beamRenderer.SetPropertyBlock(block);
            }
        }


        /// <summary>Whether a circle meets the burning beam: the segment from the origin along the direction, as wide as the beam.</summary>
        internal override bool Touches(Vector2 center, float size)
        {
            if (!burning)
            {
                return false;
            }
            Vector2 direction = Direction;
            float along = Mathf.Clamp(Vector2.Dot(center - Position, direction), 0f, length);
            Vector2 nearest = Position + direction * along;
            float reach = halfWidth + size;
            return (center - nearest).sqrMagnitude <= reach * reach;
        }


        /// <summary>A freighter or station in the beam burns as a ship does.</summary>
        public float Burn(SpaceBody target, float deltaTime)
        {
            return Touches(target.Position, target.Radius) ? damage * deltaTime : 0f;
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            burning = false;
            if (beamRenderer != null)
            {
                beamRenderer.SetPropertyBlock(null);
            }
        }
    }
}
