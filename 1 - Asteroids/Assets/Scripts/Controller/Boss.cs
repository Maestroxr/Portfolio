using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Portfolio.Asteroids
{
    public enum BossMovement
    {
        /// <summary>Drifts and wraps like a giant asteroid.</summary>
        Drift = 0,
        /// <summary>Patrols along the top of the playfield.</summary>
        HoverTop = 1,
        /// <summary>Roams between random points.</summary>
        Wander = 2
    }


    public enum BossAttackType
    {
        RingShot = 0,
        SpreadShot = 1,
        AimedBurst = 2,
        SpawnMinions = 3,
        MissileSalvo = 4,
        DropMines = 5,
        Charge = 6
    }


    public enum MinionKind
    {
        Asteroid = 0,
        Wasp = 1,
        Saucer = 2,
        Mine = 3
    }


    /// <summary>One attack in a boss's repertoire.</summary>
    [Serializable]
    public class BossAttack
    {
        public BossAttackType type;
        [Tooltip("Seconds before the boss uses this attack again.")]
        public float cooldown = 4f;
        [Tooltip("Shots, minions or mines per use.")]
        public int count = 8;
        [Tooltip("Width of a spread in degrees.")]
        public float spread = 50f;
        public float speed = 8f;
        [Tooltip("First phase (0, 1 or 2) in which the boss uses this attack.")]
        public int fromPhase;
        public MinionKind minion;
        public AsteroidKind asteroidKind;
        public EnemyShotKind shot = EnemyShotKind.Plasma;
    }


    /// <summary>
    /// The boss at the end of a sector. It makes an entrance, then moves (drift, patrol or roam) and cycles through its
    /// attacks: rings and fans of shots, aimed bursts, homing missiles, mines, minions and charges. At two thirds and
    /// one third of its hull it enters a new phase: it shields up for a moment, roars, and attacks faster with more
    /// of its repertoire. It goes down in a chain of explosions.
    /// </summary>
    public class Boss : Shootable
    {
        private enum State
        {
            Entering,
            Fighting,
            Telegraphing,
            Charging,
            Dying
        }

        [SerializeField] internal string displayName = "Boss";
        [SerializeField] internal BossMovement movement = BossMovement.HoverTop;
        [SerializeField] internal float moveSpeed = 2.5f;
        [Tooltip("Height of the patrol line as a fraction of the playfield's half height.")]
        [SerializeField] internal float hoverHeight = 0.55f;
        [SerializeField] internal BossAttack[] attacks = new BossAttack[0];
        [SerializeField] internal float[] phaseThresholds = { 0.66f, 0.33f };
        [Tooltip("Seconds between two attacks in the first phase.")]
        [SerializeField] internal float attackGap = 1.3f;
        [SerializeField] internal bool facesShip;
        [SerializeField] internal Transform[] spinners = new Transform[0];
        [SerializeField] internal float spinSpeed = 40f;
        [SerializeField] internal GameObject shield;
        [SerializeField] internal Color explosionTint = new Color(1f, 0.6f, 0.25f);
        [SerializeField] internal float entrySeconds = 2.5f;

        private State state;
        private float stateTime;
        private float[] cooldowns = new float[0];
        private float gap;
        private float shieldTime;
        private Vector2 wanderTarget;
        private Vector2 chargeDirection;
        private float nextBlast;
        private int burstLeft;
        private float burstTimer;
        private BossAttack burstAttack;
        private float ringOffset;

        public string DisplayName => displayName;

        public int Phase { get; private set; }

        public bool IsDying => state == State.Dying;

        public bool HasEntered => state != State.Entering;

        /// <summary>What shows of the boss's state, for the puppets that stand for it in a shared mission.</summary>
        internal BodyFlags StateFlags
        {
            get
            {
                BodyFlags flags = BodyFlags.None;
                if (state == State.Entering)
                {
                    flags |= BodyFlags.Entering;
                }
                if (state == State.Dying)
                {
                    flags |= BodyFlags.Dying;
                }
                if (invulnerable && state != State.Dying)
                {
                    flags |= BodyFlags.Shielded;
                }
                return BodyCodec.WithPhase(flags, Phase);
            }
        }

        /// <summary>The boss moved into a new phase (1 or 2).</summary>
        public event Action<Boss, int> PhaseChanged;

        /// <summary>The death sequence finished; the boss is about to leave play.</summary>
        public event Action<Boss> Defeated;

        private float PhaseFactor => Phase == 0 ? 1f : Phase == 1 ? 0.78f : 0.6f;


        public override void OnSpawned()
        {
            base.OnSpawned();
            state = State.Entering;
            stateTime = 0f;
            Phase = 0;
            invulnerable = true;
            wraps = false;
            gap = 1.5f;
            shieldTime = 0f;
            burstLeft = 0;
            cooldowns = new float[attacks != null ? attacks.Length : 0];
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i] = Random.Range(0.5f, 2f);
            }
            Playground playground = Field != null ? Field.Playground : null;
            float top = playground != null ? playground.HalfSize.y : 10f;
            Position = new Vector2(0f, top + radius + 1f);
            Velocity = Vector2.zero;
            SetShield(true);
        }


        public override void Tick(float deltaTime)
        {
            stateTime += deltaTime;
            foreach (Transform spinner in spinners)
            {
                if (spinner != null)
                {
                    spinner.localRotation = Quaternion.Euler(0f, 0f, spinSpeed * (1f + Phase * 0.5f) * deltaTime) * spinner.localRotation;
                }
            }
            if (IsPuppet)
            {
                // A puppet goes where the simulator's boss goes; only its death throes are played here.
                if (state == State.Dying)
                {
                    Dying(deltaTime);
                }
                base.Tick(deltaTime);
                return;
            }
            switch (state)
            {
                case State.Entering:
                    Enter(deltaTime);
                    break;
                case State.Fighting:
                    Move(deltaTime);
                    Attack(deltaTime);
                    break;
                case State.Telegraphing:
                    Velocity *= Mathf.Clamp01(1f - 4f * deltaTime);
                    if (stateTime > 0.7f)
                    {
                        state = State.Charging;
                        stateTime = 0f;
                        Velocity = chargeDirection * moveSpeed * 5f;
                    }
                    break;
                case State.Charging:
                    if (stateTime > 0.75f)
                    {
                        state = State.Fighting;
                        stateTime = 0f;
                        Velocity *= 0.2f;
                    }
                    break;
                case State.Dying:
                    Dying(deltaTime);
                    break;
            }
            if (shieldTime > 0f)
            {
                shieldTime -= deltaTime;
                if (shieldTime <= 0f && state != State.Entering && state != State.Dying)
                {
                    invulnerable = false;
                    SetShield(false);
                }
            }
            if (facesShip && state != State.Dying)
            {
                AsteroidsPlayer ship = Field != null ? Field.NearestShip(Position) : null;
                Vector2 look = ship != null ? ship.Position - Position : Vector2.down;
                float angle = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, 0f, angle), 70f * deltaTime);
            }
            base.Tick(deltaTime);
            KeepInside();
        }


        private void Enter(float deltaTime)
        {
            Playground playground = Field != null ? Field.Playground : null;
            float height = playground != null ? playground.HalfSize.y : 10f;
            var target = new Vector2(0f, movement == BossMovement.HoverTop ? height * hoverHeight : height * 0.25f);
            Position = Vector2.Lerp(Position, target, 1f - Mathf.Exp(-1.6f * deltaTime));
            Velocity = Vector2.zero;
            if (stateTime >= entrySeconds)
            {
                state = State.Fighting;
                stateTime = 0f;
                invulnerable = false;
                SetShield(false);
                wraps = movement == BossMovement.Drift;
                wanderTarget = Position;
                if (movement == BossMovement.Drift)
                {
                    Velocity = Random.insideUnitCircle.normalized * moveSpeed;
                }
            }
        }


        private void Move(float deltaTime)
        {
            Playground playground = Field != null ? Field.Playground : null;
            Vector2 half = playground != null ? playground.HalfSize : new Vector2(17f, 10f);
            switch (movement)
            {
                case BossMovement.HoverTop:
                {
                    float x = Mathf.Sin(stateTime * moveSpeed / Mathf.Max(1f, half.x) * 1.4f) * (half.x - radius - 1f);
                    var target = new Vector2(x, half.y * hoverHeight);
                    Velocity = (target - Position) * 1.5f;
                    break;
                }
                case BossMovement.Wander:
                    if ((wanderTarget - Position).sqrMagnitude < 1f || stateTime > 6f)
                    {
                        stateTime = 0f;
                        wanderTarget = new Vector2(Random.Range(-half.x + radius + 1f, half.x - radius - 1f), Random.Range(-half.y + radius + 1f, half.y - radius - 1f));
                    }
                    Velocity = Vector2.MoveTowards(Velocity, (wanderTarget - Position).normalized * moveSpeed, 3f * deltaTime);
                    break;
                default:
                    if (Velocity.sqrMagnitude < moveSpeed * moveSpeed * 0.25f)
                    {
                        Velocity = Velocity.sqrMagnitude > 0.01f ? Velocity.normalized * moveSpeed : Random.insideUnitCircle.normalized * moveSpeed;
                    }
                    break;
            }
        }


        private void KeepInside()
        {
            if (wraps || Field == null || Field.Playground == null || state == State.Entering || state == State.Dying)
            {
                return;
            }
            Vector2 half = Field.Playground.HalfSize - Vector2.one * (radius * 0.6f);
            Vector2 position = Position;
            if (Mathf.Abs(position.x) > half.x || Mathf.Abs(position.y) > half.y)
            {
                Position = new Vector2(Mathf.Clamp(position.x, -half.x, half.x), Mathf.Clamp(position.y, -half.y, half.y));
                if (state == State.Charging)
                {
                    state = State.Fighting;
                    stateTime = 0f;
                    Velocity = Vector2.zero;
                    if (Field.CameraRig != null)
                    {
                        Field.CameraRig.Shake(0.5f);
                    }
                }
            }
        }


        private void Attack(float deltaTime)
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i] -= deltaTime;
            }
            if (burstLeft > 0)
            {
                burstTimer -= deltaTime;
                if (burstTimer <= 0f)
                {
                    burstTimer = 0.14f;
                    burstLeft--;
                    FireShot(burstAttack.shot, AimAtShip(4f), burstAttack.speed);
                }
                return;
            }
            gap -= deltaTime;
            if (gap > 0f || attacks == null || attacks.Length == 0)
            {
                return;
            }
            int start = Random.Range(0, attacks.Length);
            for (int n = 0; n < attacks.Length; n++)
            {
                int i = (start + n) % attacks.Length;
                BossAttack attack = attacks[i];
                if (attack == null || attack.fromPhase > Phase || cooldowns[i] > 0f)
                {
                    continue;
                }
                Execute(attack);
                cooldowns[i] = attack.cooldown * PhaseFactor;
                gap = attackGap * PhaseFactor;
                return;
            }
        }


        private void Execute(BossAttack attack)
        {
            SpawnService spawner = Field != null ? Field.Spawner : null;
            if (spawner == null)
            {
                return;
            }
            int count = Mathf.Max(1, attack.count + Phase * Mathf.Max(1, attack.count / 4));
            switch (attack.type)
            {
                case BossAttackType.RingShot:
                    ringOffset += 360f / count * 0.5f;
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (ringOffset + 360f * i / count) * Mathf.Deg2Rad;
                        FireShot(attack.shot, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), attack.speed);
                    }
                    break;
                case BossAttackType.SpreadShot:
                {
                    Vector2 aim = AimAtShip(0f);
                    for (int i = 0; i < count; i++)
                    {
                        float t = count == 1 ? 0.5f : i / (count - 1f);
                        FireShot(attack.shot, Quaternion.Euler(0f, 0f, Mathf.Lerp(-attack.spread, attack.spread, t) * 0.5f) * aim, attack.speed);
                    }
                    break;
                }
                case BossAttackType.AimedBurst:
                    burstAttack = attack;
                    burstLeft = count;
                    burstTimer = 0f;
                    break;
                case BossAttackType.MissileSalvo:
                {
                    Vector2 aim = AimAtShip(0f);
                    for (int i = 0; i < count; i++)
                    {
                        float t = count == 1 ? 0.5f : i / (count - 1f);
                        Vector2 direction = Quaternion.Euler(0f, 0f, Mathf.Lerp(-70f, 70f, t)) * aim;
                        spawner.FireEnemyShot(EnemyShotKind.Missile, Position + direction * radius * 0.8f, direction * attack.speed);
                    }
                    Field.Sounds?.MissileLaunch();
                    break;
                }
                case BossAttackType.SpawnMinions:
                    for (int i = 0; i < count; i++)
                    {
                        spawner.SpawnMinion(attack.minion, attack.asteroidKind, Position + Random.insideUnitCircle.normalized * (radius + 0.8f));
                    }
                    Field.Sounds?.BossRoar(0.6f);
                    break;
                case BossAttackType.DropMines:
                    for (int i = 0; i < count; i++)
                    {
                        spawner.SpawnMinion(MinionKind.Mine, attack.asteroidKind, Position + Random.insideUnitCircle.normalized * (radius + 1.2f));
                    }
                    break;
                case BossAttackType.Charge:
                    state = State.Telegraphing;
                    stateTime = 0f;
                    chargeDirection = AimAtShip(0f);
                    Field.Sounds?.BossRoar(0.8f);
                    Field.Effects?.Telegraph(Position, radius, explosionTint);
                    break;
            }
        }


        private Vector2 AimAtShip(float errorDegrees)
        {
            AsteroidsPlayer ship = Field != null ? Field.NearestShip(Position) : null;
            Vector2 direction = ship != null ? (ship.Position - Position).normalized : Vector2.down;
            return Quaternion.Euler(0f, 0f, Random.Range(-errorDegrees, errorDegrees)) * direction;
        }


        private void FireShot(EnemyShotKind kind, Vector2 direction, float speed)
        {
            if (Field == null || Field.Spawner == null)
            {
                return;
            }
            Field.Spawner.FireEnemyShot(kind, Position + direction * radius * 0.85f, direction * speed);
            Field.Sounds?.EnemyShot();
        }


        public override bool TakeHit(DamageInfo hit)
        {
            if (state == State.Entering || state == State.Dying)
            {
                return false;
            }
            bool destroyed = base.TakeHit(hit);
            if (destroyed || IsPuppet || phaseThresholds == null || Phase >= phaseThresholds.Length)
            {
                return destroyed;
            }
            if (HealthFraction <= phaseThresholds[Phase])
            {
                Phase++;
                invulnerable = true;
                shieldTime = 1.4f;
                SetShield(true);
                burstLeft = 0;
                Field?.Sounds?.BossRoar(1f);
                Field?.CameraRig?.Shake(0.6f);
                Field?.Effects?.Shockwave(Position, radius * 3f, explosionTint);
                PhaseChanged?.Invoke(this, Phase);
            }
            return false;
        }


        public override void OnRammed(AsteroidsPlayer player, Vector2 direction, bool dashing)
        {
            // Too big to be hurt by the ship flying into it.
        }


        protected override void Die(DamageInfo hit)
        {
            if (state == State.Dying)
            {
                return;
            }
            Exit = ExitReason.Destroyed;
            ExitSeat = hit.ByPlayer ? hit.Seat : null;
            state = State.Dying;
            stateTime = 0f;
            nextBlast = 0f;
            Velocity *= 0.2f;
            SetShield(false);
            Field?.NotifyDestroyed(this, hit);
            Field?.Sounds?.BossRoar(1.2f);
        }


        /// <summary>The simulator reports the state of the boss a puppet stands for: its entrance, shield, phase and death.</summary>
        internal void ShowState(BodyFlags flags)
        {
            bool dying = BodyCodec.Has(flags, BodyFlags.Dying);
            if (dying && state != State.Dying)
            {
                state = State.Dying;
                stateTime = 0f;
                nextBlast = 0f;
                Field?.Sounds?.BossRoar(1.2f);
            }
            else if (!dying)
            {
                state = BodyCodec.Has(flags, BodyFlags.Entering) ? State.Entering : State.Fighting;
            }
            bool shielded = BodyCodec.Has(flags, BodyFlags.Shielded) && !dying;
            invulnerable = shielded || dying;
            SetShield(shielded);
            int phase = BodyCodec.Phase(flags);
            if (phase > Phase)
            {
                Phase = phase;
                Field?.Sounds?.BossRoar(1f);
                Field?.CameraRig?.Shake(0.6f);
                Field?.Effects?.Shockwave(Position, radius * 3f, explosionTint);
                PhaseChanged?.Invoke(this, Phase);
            }
        }


        /// <summary>The boss a puppet stands for went up: the last explosion plays here too, and the kill counts for whoever made it.</summary>
        internal override void PlayDestroyed(DamageInfo hit)
        {
            if (!InPlay)
            {
                return;
            }
            FinalBlast();
            Field?.NotifyDestroyed(this, hit);
            Defeated?.Invoke(this);
            Despawn();
        }


        private void Dying(float deltaTime)
        {
            Velocity *= Mathf.Clamp01(1f - deltaTime);
            nextBlast -= deltaTime;
            if (nextBlast <= 0f)
            {
                nextBlast = Random.Range(0.1f, 0.2f);
                Vector2 at = Position + Random.insideUnitCircle * radius;
                Field?.Effects?.Explosion(at, Random.Range(0.8f, 1.6f), explosionTint);
                Field?.Sounds?.Explosion(0.8f);
                Field?.CameraRig?.Shake(0.25f);
            }
            if (visual != null)
            {
                visual.localPosition = Random.insideUnitSphere * 0.15f;
            }
            if (stateTime >= 2.2f && !IsPuppet)
            {
                FinalBlast();
                SpawnService spawner = Field != null ? Field.Spawner : null;
                if (spawner != null)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        spawner.SpawnCrystal(Position + Random.insideUnitCircle * radius, Random.insideUnitCircle * 4f);
                    }
                }
                Defeated?.Invoke(this);
                Despawn();
            }
        }


        private void FinalBlast()
        {
            Field?.Effects?.Explosion(Position, radius * 1.6f, explosionTint);
            Field?.Effects?.Shockwave(Position, radius * 6f, explosionTint);
            Field?.Sounds?.Explosion(1.5f);
            Field?.CameraRig?.Shake(1f);
        }


        private void SetShield(bool on)
        {
            if (shield != null)
            {
                shield.SetActive(on);
            }
        }


        protected override void OnDespawned()
        {
            base.OnDespawned();
            PhaseChanged = null;
            Defeated = null;
            if (visual != null)
            {
                visual.localPosition = Vector3.zero;
            }
            if (Pool == null && this != null)
            {
                // Bosses are not pooled: one is built for each fight.
                Destroy(gameObject);
            }
        }
    }
}
