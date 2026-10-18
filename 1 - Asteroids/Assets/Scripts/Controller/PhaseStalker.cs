using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// A phase stalker: it never flies to its prey, it blinks there. A ring of light warns where it will appear; it
    /// fades in a few meters from its prey, fires a fan of bolts, hangs there for a moment and fades out again to pick
    /// its next spot. Out of phase it cannot be hurt and hurts nothing, so the moment it shows is the moment to shoot.
    /// The simulator reports it as shielded while it is out of phase and as warning while it is about to appear, which is
    /// all a puppet needs to fade and to show the ring.
    /// </summary>
    public class PhaseStalker : CampaignEnemy
    {
        private enum Phase
        {
            Arriving,
            Shown,
            Leaving
        }

        [SerializeField] internal float warningTime = 0.8f;
        [SerializeField] internal float fadeTime = 0.3f;
        [SerializeField] internal float shownTime = 2.6f;
        [Tooltip("How far from its prey the stalker appears.")]
        [SerializeField] internal Vector2 range = new Vector2(4.5f, 7f);
        [SerializeField] internal int fan = 3;
        [SerializeField] internal float fanSpread = 34f;
        [Tooltip("Seconds after appearing before the stalker fires its fan.")]
        [SerializeField] internal float fireDelay = 0.55f;
        [SerializeField] internal Color blinkTint = new Color(0.75f, 0.45f, 1f);

        private Phase phase;
        private float phaseTime;
        private bool fired;
        private float shown;
        private bool warning;

        internal override bool IsTelegraphing => !IsPuppet && phase == Phase.Arriving && phaseTime < warningTime;

        /// <summary>Out of phase it hurts nothing it touches.</summary>
        public override float ContactDamage => shown > 0.5f ? base.ContactDamage : 0f;


        public override void OnSpawned()
        {
            base.OnSpawned();
            shown = 0f;
            warning = false;
            transform.localScale = Vector3.one * 0.01f;
            if (!IsPuppet)
            {
                Arrive(true);
            }
        }


        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);
            if (!InPlay)
            {
                return;
            }
            if (!IsPuppet)
            {
                Think(deltaTime);
            }
            // Fade by the state the enemy shows: out of phase (shielded) it shrinks away, in phase it grows back.
            float wanted = Invulnerable ? 0f : 1f;
            shown = Mathf.MoveTowards(shown, wanted, deltaTime / Mathf.Max(0.05f, fadeTime));
            transform.localScale = Vector3.one * Mathf.Max(0.01f, Mathf.SmoothStep(0f, 1f, shown));
            if (shown > 0.5f && Prey(out Vector2 at, out _))
            {
                Face(OffsetTo(at), deltaTime, 400f);
            }
        }


        private void Think(float deltaTime)
        {
            phaseTime += deltaTime;
            switch (phase)
            {
                case Phase.Arriving:
                    if (phaseTime >= warningTime)
                    {
                        phase = Phase.Shown;
                        phaseTime = 0f;
                        fired = false;
                        Invulnerable = false;
                        Velocity = Random.insideUnitCircle * 0.6f;
                    }
                    break;
                case Phase.Shown:
                    if (!fired && phaseTime >= fireDelay)
                    {
                        fired = true;
                        FireFan();
                    }
                    if (phaseTime >= shownTime)
                    {
                        phase = Phase.Leaving;
                        phaseTime = 0f;
                        Invulnerable = true;
                        PlayCue(0.4f, 1.2f);
                        Field?.Effects?.Spark(Position, blinkTint, 1.6f);
                    }
                    break;
                case Phase.Leaving:
                    if (phaseTime >= fadeTime)
                    {
                        Arrive(false);
                    }
                    break;
            }
        }


        /// <summary>Picks the next spot near the prey and starts the warning there.</summary>
        private void Arrive(bool first)
        {
            phase = Phase.Arriving;
            phaseTime = 0f;
            Invulnerable = true;
            Velocity = Vector2.zero;
            if (Prey(out Vector2 at, out Vector2 moving) && Field != null && Field.Playground != null)
            {
                Vector2 around = Random.insideUnitCircle.normalized * Random.Range(range.x, range.y);
                Vector2 spot = at + moving * (warningTime + fadeTime) * 0.5f + around;
                Vector2 half = Field.Playground.HalfSize - Vector2.one * (radius + 0.6f);
                Position = new Vector2(Mathf.Clamp(spot.x, -half.x, half.x), Mathf.Clamp(spot.y, -half.y, half.y));
            }
            else if (first && Field != null && Field.Playground != null)
            {
                Position = Field.Playground.RandomPointAwayFrom(Field.ShipClearance, 5f, 2f);
            }
            Field?.Effects?.Telegraph(Position, radius * 1.6f, blinkTint);
            PlayCue(0.45f, first ? 1f : 0.9f);
        }


        private void FireFan()
        {
            if (!Prey(out Vector2 at, out Vector2 moving))
            {
                return;
            }
            Vector2 aim = Lead(at, moving, shotSpeed);
            int count = Mathf.Max(1, fan);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (count - 1f);
                Fire(Quaternion.Euler(0f, 0f, Mathf.Lerp(-fanSpread, fanSpread, t) * 0.5f + Random.Range(-aimError, aimError)) * aim);
            }
        }


        /// <summary>A puppet's warning: the ring where the stalker it stands for is about to appear.</summary>
        internal override void ShowWarning(bool on)
        {
            if (on && !warning && Field != null)
            {
                Field.Effects?.Telegraph(Position, radius * 1.6f, blinkTint);
                PlayCue(0.45f);
            }
            warning = on;
        }
    }
}
