using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A sentry: an armoured gun platform that warps in and drifts slowly through the sector. Its turret turns toward what
    /// it guards against (a ship, or the freighter or station of the mission), its muzzle glows while it charges, and it
    /// fires a burst of bolts. The charge is the warning: the simulator reports it, so every pilot sees the glow.
    /// </summary>
    public class Sentry : CampaignEnemy
    {
        [SerializeField] internal Transform turret;
        [Tooltip("Glows while the sentry charges a burst.")]
        [SerializeField] internal Renderer charge;
        [SerializeField] internal int burst = 3;
        [SerializeField] internal float burstGap = 0.13f;
        [SerializeField] internal float chargeTime = 0.75f;
        [SerializeField] internal float turnSpeed = 140f;
        [SerializeField] internal float drift = 0.45f;
        [SerializeField, ColorUsage(false, true)] internal Color chargeColor = new Color(3f, 1.1f, 0.3f);

        private static MaterialPropertyBlock block;
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private float charging;
        private int burstLeft;
        private float burstTimer;
        private Vector2 aim = Vector2.up;
        private bool warning;

        internal override bool IsTelegraphing => charging > 0f;


        public override void OnSpawned()
        {
            base.OnSpawned();
            charging = 0f;
            burstLeft = 0;
            warning = false;
            aim = Vector2.up;
            Spin = new Vector3(0f, 0f, Random.Range(-12f, 12f));
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (Prey(out Vector2 at, out Vector2 moving))
            {
                aim = Vector2.Lerp(aim, Lead(at, moving, shotSpeed), Mathf.Min(1f, deltaTime * 6f)).normalized;
            }
            if (turret != null)
            {
                float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f;
                turret.rotation = Quaternion.RotateTowards(turret.rotation, Quaternion.Euler(0f, 0f, angle), turnSpeed * deltaTime);
            }
            if (!IsPuppet)
            {
                Think(deltaTime);
            }
            float glow = IsPuppet ? (warning ? 1f : 0f) : charging > 0f ? 1f - charging / Mathf.Max(0.01f, chargeTime) : 0f;
            ShowCharge(glow);
        }


        private void Think(float deltaTime)
        {
            if (Velocity.sqrMagnitude > drift * drift)
            {
                Velocity = Vector2.MoveTowards(Velocity, Velocity.normalized * drift, deltaTime);
            }
            if (burstLeft > 0)
            {
                burstTimer -= deltaTime;
                if (burstTimer <= 0f)
                {
                    burstTimer = burstGap;
                    burstLeft--;
                    Fire(Quaternion.Euler(0f, 0f, Random.Range(-aimError, aimError)) * aim);
                }
                return;
            }
            if (charging > 0f)
            {
                charging -= deltaTime;
                if (charging <= 0f)
                {
                    charging = 0f;
                    // At a freighter or station it fires a shorter burst and waits longer for the next.
                    burstLeft = HuntsAlly ? Mathf.Max(1, burst - 1) : Mathf.Max(1, burst);
                    burstTimer = 0f;
                    if (HuntsAlly)
                    {
                        fireTimer += fireInterval * 0.6f;
                    }
                }
                return;
            }
            if (ReadyToFire(deltaTime))
            {
                charging = chargeTime;
                PlayCue(0.45f);
            }
        }


        internal override void ShowWarning(bool on)
        {
            if (on && !warning)
            {
                PlayCue(0.45f);
            }
            warning = on;
        }


        private void ShowCharge(float amount)
        {
            if (charge == null)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            block.Clear();
            block.SetColor(ColorId, chargeColor * (0.15f + 0.85f * amount * (0.75f + 0.25f * Mathf.Sin(Age * 40f))));
            charge.SetPropertyBlock(block);
        }
    }
}
