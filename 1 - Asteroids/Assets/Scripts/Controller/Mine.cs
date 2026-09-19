using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A proximity mine. It drifts quietly with a slow cyan pulse until the ship comes within its trigger range; then
    /// it arms, turns red, beeps faster and faster, creeps toward the ship and blows up. Shooting it from a safe
    /// distance sets it off early, which also breaks every rock caught in the blast.
    /// </summary>
    public class Mine : Explodable
    {
        [SerializeField] internal float triggerRange = 3.4f;
        [SerializeField] internal float fuseTime = 1.25f;
        [SerializeField] internal float creepSpeed = 1.6f;
        [Tooltip("Glowing core that pulses and blinks.")]
        [SerializeField] internal Renderer beacon;
        [Tooltip("Ring on the ground showing the blast radius while armed.")]
        [SerializeField] internal Transform rangeRing;
        [SerializeField, ColorUsage(false, true)] internal Color idleColor = new Color(0.2f, 1.6f, 2.4f);
        [SerializeField, ColorUsage(false, true)] internal Color armedColor = new Color(3f, 0.25f, 0.15f);

        private static MaterialPropertyBlock block;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private float fuse;
        private float blinkTimer;
        private bool blinkOn;

        public bool IsArmed { get; private set; }


        public override void OnSpawned()
        {
            base.OnSpawned();
            IsArmed = false;
            fuse = 0f;
            blinkTimer = 0f;
            SetBeacon(idleColor);
            if (rangeRing != null)
            {
                rangeRing.gameObject.SetActive(false);
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            AsteroidsPlayer player = Field != null ? Field.Player : null;
            if (!IsArmed)
            {
                float pulse = 0.55f + 0.45f * Mathf.Sin(Age * 3f);
                SetBeacon(idleColor * pulse);
                if (player != null && player.IsAlive && Field.Playground != null &&
                    Field.Playground.Delta(Position, player.Position).sqrMagnitude < triggerRange * triggerRange)
                {
                    Arm();
                }
                return;
            }
            fuse -= deltaTime;
            if (player != null && player.IsAlive && Field.Playground != null)
            {
                Vector2 toShip = Field.Playground.Delta(Position, player.Position);
                Velocity = Vector2.MoveTowards(Velocity, toShip.normalized * creepSpeed, deltaTime * 4f);
            }
            float interval = Mathf.Lerp(0.06f, 0.3f, Mathf.Clamp01(fuse / fuseTime));
            blinkTimer -= deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkTimer = interval;
                blinkOn = !blinkOn;
                if (blinkOn && Field.Sounds != null)
                {
                    Field.Sounds.MineBeep(1f - Mathf.Clamp01(fuse / fuseTime));
                }
            }
            SetBeacon(blinkOn ? armedColor : armedColor * 0.15f);
            if (rangeRing != null)
            {
                float size = BlastRadius * 2f;
                rangeRing.localScale = new Vector3(size, size, 1f) * (1f + 0.04f * Mathf.Sin(Age * 30f));
            }
            if (fuse <= 0f)
            {
                Detonate(false);
            }
        }


        public void Arm()
        {
            if (IsArmed)
            {
                return;
            }
            IsArmed = true;
            fuse = fuseTime;
            blinkTimer = 0f;
            if (rangeRing != null)
            {
                rangeRing.gameObject.SetActive(true);
                rangeRing.rotation = Quaternion.identity;
            }
        }


        private void SetBeacon(Color color)
        {
            if (beacon == null)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(ColorId, color);
            beacon.SetPropertyBlock(block);
        }
    }
}
