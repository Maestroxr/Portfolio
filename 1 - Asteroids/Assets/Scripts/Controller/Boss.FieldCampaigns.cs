using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// What the bosses of the field campaigns add (Boss.FieldCampaigns.cs): three movements (circling the middle,
    /// blinking from place to place, holding the middle), six attacks (lightning, lances, sweeping beams, fans of lances,
    /// solar flares, nova orbs), a regeneration that the Hydra uses once its first phase is gone (up to a budget per
    /// phase), and calls the HUD shows as a boss enters a phase. The boss holds still while one of its beams burns. A blinking boss cannot be hurt;
    /// the simulator reports the blink as a warning, so a puppet shrinks away and back the same way.
    /// </summary>
    public partial class Boss
    {
        [Header("Field campaigns")]
        [Tooltip("Hull regained per second after its first phase, once nothing has hit it for a moment (never above the phase it is in).")]
        [SerializeField] internal float regeneration;
        [Tooltip("The most a regenerating boss heals within one phase, as a share of its full hull: keeping up the fire pays off in the end.")]
        [SerializeField] internal float regenerationBudget = 0.15f;
        [Tooltip("Orbit: the size of the circle as a share of the playfield's half size.")]
        [SerializeField] internal float orbitRadius = 0.55f;
        [Tooltip("Blink: seconds between two blinks in the first phase (shorter in the later ones).")]
        [SerializeField] internal float blinkInterval = 5.5f;
        [Tooltip("Blink: seconds the boss is gone.")]
        [SerializeField] internal float blinkTime = 1.1f;
        [Tooltip("Announced as the boss enters its second, third and fourth phase (optional, by phase).")]
        [SerializeField] internal string[] phaseCalls = new string[0];
        [SerializeField] internal AudioClip blinkClip;

        /// <summary>Seconds without a hit before a regenerating boss heals.</summary>
        private const float RegenerationDelay = 4f;

        private float hold;
        private float lastHealth = -1f;
        private float sinceHit;
        private float regenSpark;
        private int regenPhase = -1;
        private float regained;
        private float orbitAngle;
        private float blinkTimer;
        private float blinkClock = -1f;
        private Vector2 blinkTo;
        private bool blinkShown;
        private bool fieldBlinking;

        /// <summary>What the boss shows of the campaign's own states: a blink is a warning.</summary>
        private BodyFlags FieldFlags => blinkClock >= 0f ? BodyFlags.Warning : BodyFlags.None;

        /// <summary>The call of <paramref name="phase"/> (1 to 3) for the HUD, or empty.</summary>
        public string PhaseCall(int phase)
        {
            int index = phase - 1;
            return phaseCalls != null && index >= 0 && index < phaseCalls.Length ? phaseCalls[index] ?? string.Empty : string.Empty;
        }


        /// <summary>Every frame, puppets too: the blink's shrinking, a regenerating boss's healing.</summary>
        private void TickField(float deltaTime)
        {
            hold = Mathf.Max(0f, hold - deltaTime);
            if (state == State.Entering)
            {
                // The first blink comes a while after the entrance.
                blinkTimer = blinkInterval * 0.7f;
                blinkClock = -1f;
                regenPhase = -1;
            }
            float scale = 1f;
            bool blinking = IsPuppet ? fieldBlinking : blinkClock >= 0f;
            if (blinking && state != State.Dying)
            {
                if (!IsPuppet)
                {
                    blinkClock += deltaTime;
                    Blink();
                }
                float progress = IsPuppet ? 0.5f : Mathf.Clamp01(blinkClock / Mathf.Max(0.1f, blinkTime));
                scale = IsPuppet ? 0.05f : Mathf.Max(0.05f, Mathf.Abs(progress * 2f - 1f));
            }
            float current = transform.localScale.x;
            transform.localScale = Vector3.one * (IsPuppet ? Mathf.MoveTowards(current, scale, deltaTime * 3f) : scale);
            if (IsPuppet || state == State.Dying || state == State.Entering)
            {
                lastHealth = Health;
                return;
            }
            if (lastHealth >= 0f && Health < lastHealth - 0.0001f)
            {
                sinceHit = 0f;
            }
            sinceHit += deltaTime;
            if (regeneration > 0f && Phase >= 1 && sinceHit > RegenerationDelay && phaseThresholds != null && Phase - 1 < phaseThresholds.Length)
            {
                if (regenPhase != Phase)
                {
                    regenPhase = Phase;
                    regained = 0f;
                }
                float cap = maxHealth * phaseThresholds[Phase - 1] - 0.01f;
                float heal = Mathf.Min(regeneration * deltaTime, cap - Health, maxHealth * regenerationBudget - regained);
                if (heal > 0f)
                {
                    Health += heal;
                    regained += heal;
                    regenSpark -= deltaTime;
                    if (regenSpark <= 0f)
                    {
                        regenSpark = 0.3f;
                        Field?.Effects?.Spark(Position + Random.insideUnitCircle * radius * 0.8f, new Color(0.45f, 1f, 0.4f), 1.4f);
                    }
                }
            }
            lastHealth = Health;
        }


        /// <summary>The simulator's boss holds still while one of its beams burns.</summary>
        private void HoldField()
        {
            if (hold > 0f && state == State.Fighting)
            {
                Velocity = Vector2.zero;
            }
        }


        /// <summary>The campaign movements.</summary>
        private void MoveField(float deltaTime, Vector2 half)
        {
            switch (movement)
            {
                case BossMovement.Orbit:
                {
                    float rx = Mathf.Max(1f, half.x * orbitRadius);
                    float ry = Mathf.Max(1f, half.y * orbitRadius);
                    orbitAngle += moveSpeed / Mathf.Max(1f, (rx + ry) * 0.5f) * deltaTime * (1f + Phase * 0.25f);
                    var target = new Vector2(Mathf.Cos(orbitAngle) * rx, Mathf.Sin(orbitAngle) * ry);
                    Velocity = (target - Position) * 1.6f;
                    break;
                }
                case BossMovement.Blink:
                {
                    if (blinkClock >= 0f)
                    {
                        Velocity = Vector2.zero;
                        break;
                    }
                    blinkTimer -= deltaTime;
                    if (blinkTimer <= 0f)
                    {
                        StartBlink(half);
                        break;
                    }
                    // Between blinks it drifts slowly about where it appeared.
                    var drift = new Vector2(Mathf.Sin(StateTime * 0.7f), Mathf.Cos(StateTime * 0.5f)) * moveSpeed * 0.4f;
                    Velocity = Vector2.MoveTowards(Velocity, drift, 3f * deltaTime);
                    break;
                }
                default:
                {
                    var anchor = new Vector2(0f, half.y * 0.1f);
                    Velocity = (anchor - Position) * 1.2f + new Vector2(Mathf.Sin(StateTime * 0.4f), 0f) * moveSpeed * 0.2f;
                    break;
                }
            }
        }


        private void StartBlink(Vector2 half)
        {
            blinkClock = 0f;
            blinkShown = false;
            invulnerable = true;
            Velocity = Vector2.zero;
            Vector2 inner = half - Vector2.one * (radius + 1.5f);
            Vector2 best = Position;
            float bestClearance = -1f;
            for (int i = 0; i < 12; i++)
            {
                var candidate = new Vector2(Random.Range(-inner.x, inner.x), Random.Range(-inner.y, inner.y));
                float clearance = Field != null ? Field.ShipClearance(candidate) : 10f;
                float jump = (candidate - Position).magnitude;
                if (clearance > 6f && jump > 6f)
                {
                    best = candidate;
                    break;
                }
                if (clearance > bestClearance)
                {
                    bestClearance = clearance;
                    best = candidate;
                }
            }
            blinkTo = best;
            Field?.Effects?.Telegraph(blinkTo, radius * 1.2f, explosionTint);
            Field?.Effects?.Shockwave(Position, radius * 2.5f, explosionTint);
            Field?.Sounds?.Play(blinkClip, 0.7f, 1f, 0.05f, 0.2f);
        }


        /// <summary>The middle of a blink moves the boss; at the end it can be hurt again.</summary>
        private void Blink()
        {
            if (!blinkShown && blinkClock >= blinkTime * 0.5f)
            {
                blinkShown = true;
                Position = blinkTo;
            }
            if (blinkClock < blinkTime)
            {
                return;
            }
            blinkClock = -1f;
            blinkTimer = blinkInterval * (Phase == 0 ? 1f : Phase == 1 ? 0.75f : 0.55f) * Random.Range(0.85f, 1.15f);
            if (shieldTime <= 0f)
            {
                invulnerable = false;
            }
            Field?.Effects?.Shockwave(Position, radius * 2.5f, explosionTint);
        }


        /// <summary>A puppet follows the simulator's blink: it shrinks away while the boss is gone.</summary>
        private void ShowFieldState(BodyFlags flags)
        {
            bool blinking = BodyCodec.Has(flags, BodyFlags.Warning) && !BodyCodec.Has(flags, BodyFlags.Dying);
            if (blinking == fieldBlinking)
            {
                return;
            }
            fieldBlinking = blinking;
            Field?.Effects?.Shockwave(Position, radius * 2.5f, explosionTint);
            if (blinking)
            {
                Field?.Sounds?.Play(blinkClip, 0.7f, 1f, 0.05f, 0.2f);
                // A blink is no shield: the bubble stays hidden.
                SetShield(false);
            }
        }


        /// <summary>The campaign attacks.</summary>
        private void ExecuteField(BossAttack attack, SpawnService spawner, int count)
        {
            switch (attack.type)
            {
                case BossAttackType.Lightning:
                {
                    for (int i = 0; i < count; i++)
                    {
                        AsteroidsPlayer ship = Field.NearestShip(Position + Random.insideUnitCircle * 8f);
                        Vector2 around = ship != null ? ship.Position + ship.Velocity * 0.6f : Position;
                        Vector2 at = i == 0 ? around : around + Random.insideUnitCircle * attack.spread * 0.1f;
                        spawner.FireEnemyShot(EnemyShotKind.Lightning, Field.Playground != null ? Field.Playground.Clamp(at, 0.5f) : at, Vector2.zero);
                    }
                    break;
                }
                case BossAttackType.Lance:
                case BossAttackType.LanceFan:
                {
                    Vector2 aim = AimAtShip(0f);
                    int lances = attack.type == BossAttackType.Lance ? 1 : count;
                    for (int i = 0; i < lances; i++)
                    {
                        float t = lances == 1 ? 0.5f : i / (lances - 1f);
                        Vector2 direction = Quaternion.Euler(0f, 0f, Mathf.Lerp(-attack.spread, attack.spread, t) * 0.5f) * aim;
                        Beam(spawner, EnemyShotKind.Lance, direction, 1f);
                    }
                    break;
                }
                case BossAttackType.SweepBeam:
                {
                    Vector2 aim = AimAtShip(0f);
                    float turn = Random.value < 0.5f ? 1f : FieldBeam.ClockwiseSpeed;
                    for (int i = 0; i < count; i++)
                    {
                        Beam(spawner, EnemyShotKind.SweepBeam, Quaternion.Euler(0f, 0f, 360f * i / count) * aim, turn);
                    }
                    break;
                }
                case BossAttackType.SolarFlare:
                    for (int i = 0; i < count; i++)
                    {
                        spawner.SpawnFlare((SolarFlare.Side)Random.Range(0, 4));
                    }
                    Field.Sounds?.BossRoar(0.7f);
                    break;
                case BossAttackType.NovaOrbs:
                {
                    Vector2 aim = AimAtShip(0f);
                    for (int i = 0; i < count; i++)
                    {
                        float t = count == 1 ? 0.5f : i / (count - 1f);
                        Vector2 direction = Quaternion.Euler(0f, 0f, Mathf.Lerp(-attack.spread, attack.spread, t) * 0.5f) * aim;
                        spawner.FireEnemyShot(EnemyShotKind.NovaOrb, Position + direction * radius, direction * attack.speed);
                    }
                    Field.Sounds?.EnemyShot();
                    break;
                }
            }
        }


        /// <summary>One beam from the boss's rim; the boss holds still until the longest of its beams is done.</summary>
        private void Beam(SpawnService spawner, EnemyShotKind kind, Vector2 direction, float speed)
        {
            Shot shot = spawner.FireEnemyShot(kind, Position + direction * radius * 0.7f, direction.normalized * speed);
            if (shot is FieldBeam beam)
            {
                hold = Mathf.Max(hold, beam.warningTime + beam.burnTime);
            }
        }
    }
}
