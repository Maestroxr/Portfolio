using System;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A car's health: hits (guns, mines, rockets, rams, walls) take it down through the car's armour, pickups and the
    /// garage put it back; at zero the car is wrecked. Crashes are worked out here from the body's collisions: a crash
    /// into another car hurts both by the speed of the impact (a spiked bumper bites harder and soaks more), a barrier
    /// hurts by how hard it was hit, and grinding along one throws sparks. Smoke and fire show how hurt a car is; a wreck
    /// blows up with a blast that shakes the views nearby, leaves a scorch mark and burns for a while. The numbers are
    /// <see cref="CombatRules"/>'s, the arithmetic <see cref="HealthModel"/>'s.
    /// </summary>
    public class CarHealth : MonoBehaviour
    {
        private const float WreckBurnSeconds = 14f;

        private HealthModel model;
        private Car car;
        private Car lastAttacker;
        private RaceEffect smoke;
        private RaceEffect fire;
        private int stage = -1;
        private float scrapeTimer;
        private float grindTimer;
        private float scrapedAt = -1f;
        private float wreckBurn;
        private bool wreckSmokeOnly;

        public float Health { get; protected set; }
        public float Max { get; protected set; } = 100f;
        public float Health01 => Max > 0f ? Mathf.Clamp01(Health / Max) : 0f;

        /// <summary>Whether the car has no health left.</summary>
        public bool IsDead => Health <= 0f;

        /// <summary>The last hit that landed (after armour).</summary>
        public DamageInfo LastHit { get; private set; }

        /// <summary>The damage taken this race, after armour.</summary>
        public float DamageTaken { get; private set; }

        /// <summary>
        /// Whether this car's own hits are judged here. A car of a person on another device is judged on theirs (online
        /// races set it false on remote cars): it still shows smoke and sparks, but takes no damage of its own.
        /// </summary>
        public bool Authoritative { get; set; } = true;

        /// <summary>A hit landed (after armour).</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>Health was put back (a pickup), how much.</summary>
        public event Action<float> Repaired;

        /// <summary>Called by the factory: the car, and the health it starts the race with (0 to 1 of its maximum).</summary>
        public virtual void Setup(Car car, float startHealth01)
        {
            if (this.car != null && this.car.Body != null)
            {
                this.car.Body.Collided -= OnCollided;
            }
            this.car = car;
            CarStats stats = car != null ? car.Stats : CarStats.Of(null);
            model = new HealthModel(stats.MaxHealth > 0f ? stats.MaxHealth : 100f, startHealth01, stats.DamageTaken > 0f ? stats.DamageTaken : 1f);
            Max = model.Max;
            Health = model.Health;
            DamageTaken = 0f;
            lastAttacker = null;
            stage = -1;
            if (car != null && car.Body != null)
            {
                car.Body.Collided += OnCollided;
            }
            if (car != null)
            {
                SkidMarks skids = GetComponent<SkidMarks>();
                if (skids == null)
                {
                    skids = gameObject.AddComponent<SkidMarks>();
                }
                skids.Setup(car);
            }
            RefreshLook();
        }

        private RaceEffects Effects => car != null && car.Race != null ? car.Race.Effects : null;

        private RaceAudio Audio => car != null && car.Race != null && car.Race.Audio != null ? car.Race.Audio : RaceAudio.Current;

        private float Now => car != null && car.Race != null ? car.Race.Time : Time.time;

        private int Seat => car != null && car.Entrant != null ? car.Entrant.Seat : HealthModel.Nobody;

        private static int SeatOf(Car other)
        {
            return other != null && other.Entrant != null ? other.Entrant.Seat : HealthModel.Nobody;
        }

        /// <summary>
        /// Takes a hit: <paramref name="info"/>'s amount before armour. The armour takes its share, the same crash cannot land
        /// twice within a moment, and a car whose health runs out is wrecked.
        /// </summary>
        public virtual void TakeDamage(DamageInfo info)
        {
            if (model == null)
            {
                Setup(car != null ? car : GetComponent<Car>(), 1f);
            }
            // A car past the finish line is out of the fight: its place is safe.
            if (!Authoritative || model.Dead || (car != null && (car.IsWrecked || (car.Racer != null && car.Racer.Finished))))
            {
                return;
            }
            float now = Now;
            int seat = Seat;
            int attacker = SeatOf(info.Attacker);
            float taken = model.Apply(info.Amount, info.Source, attacker, seat, now);
            if (taken <= 0f)
            {
                return;
            }
            if (info.Attacker != null && info.Attacker != car)
            {
                lastAttacker = info.Attacker;
            }
            Health = model.Health;
            DamageTaken += taken;
            DamageInfo landed = info.WithAmount(taken);
            LastHit = landed;
            Damaged?.Invoke(landed);
            if (car != null)
            {
                car.Raise(new CarEvent(CarEventKind.Damaged, Mathf.Max(1, Mathf.RoundToInt(taken)), CombatRules.Credit(car, info.Attacker)));
                if (car.IsHuman && info.Source == DamageSource.Gun)
                {
                    // Every round that lands on a person's car jolts their view a little: being shot is felt.
                    Effects?.Shake(transform.position, 0.12f);
                }
            }
            RefreshLook();
            if (model.Dead)
            {
                int killer = model.KillerOf(attacker, seat, now);
                DamageInfo cause = landed;
                cause.Attacker = killer == HealthModel.Nobody ? null : killer == attacker ? info.Attacker : lastAttacker;
                Wreck(cause);
            }
        }

        /// <summary>Puts back up to <paramref name="amount"/> health.</summary>
        public virtual void Repair(float amount)
        {
            if (model == null)
            {
                Setup(car != null ? car : GetComponent<Car>(), 1f);
            }
            float added = model.Repair(amount);
            Health = model.Health;
            if (added > 0f)
            {
                Repaired?.Invoke(added);
                RefreshLook();
            }
        }

        /// <summary>
        /// The health a person on another device says their car has (online): shown as it is, the smoke follows, and at
        /// zero the car blows up here too.
        /// </summary>
        public void ShowHealth(float health01, Car attacker = null)
        {
            if (model == null)
            {
                Setup(car != null ? car : GetComponent<Car>(), 1f);
            }
            model.Set(Max * Mathf.Clamp01(health01));
            Health = model.Health;
            RefreshLook();
            if (model.Dead && car != null && !car.IsWrecked)
            {
                Wreck(new DamageInfo(0f, DamageSource.Explosion, attacker, transform.position, Vector3.zero));
            }
        }

        protected void RaiseDamaged(DamageInfo info)
        {
            Damaged?.Invoke(info);
        }

        // ------------------------------------------------------------------------------------------------
        // Crashes
        // ------------------------------------------------------------------------------------------------

        /// <summary>The body hit something: another car (a ram) or a barrier.</summary>
        private void OnCollided(Collision collision, float impactSpeed)
        {
            if (collision == null || car == null || car.IsWrecked || collision.contactCount == 0)
            {
                return;
            }
            Collider other = collision.collider;
            if (other == null)
            {
                return;
            }
            ContactPoint contact = collision.GetContact(0);
            Vector3 point = contact.point;
            Car otherCar = other.GetComponentInParent<Car>();
            if (otherCar != null && otherCar != car && !otherCar.IsWrecked)
            {
                Rammed(otherCar, point, contact.normal, impactSpeed);
            }
            else if (other.gameObject.layer == DeathRaceLayers.Wall || (otherCar != null && otherCar.IsWrecked))
            {
                // A wreck is an obstacle like a barrier: nobody's ram.
                HitWall(point, contact.normal, impactSpeed);
            }
        }

        private void Rammed(Car other, Vector3 point, Vector3 normal, float impactSpeed)
        {
            Vector2 toPoint = new Vector2(point.x, point.z) - car.Position2D;
            Vector2 fromOther = new Vector2(point.x, point.z) - other.Position2D;
            bool ownFront = CombatRules.IsFront(car.Forward2D, toPoint);
            bool otherFront = CombatRules.IsFront(other.Forward2D, fromOther);
            float damage = CombatRules.RamDamage(impactSpeed, ownFront, car.Stats.RamTaken, otherFront, other.Stats.RamDealt);
            // Both cars hear of the crash; one of them plays it, so it shows once.
            bool shows = PlaysCrashWith(other) || other.IsWrecked;
            if (shows && impactSpeed > 3f)
            {
                float strength = Mathf.Clamp01(impactSpeed / 25f);
                Effects?.Sparks(point + Vector3.up * 0.5f, new Vector3(normal.x, 0f, normal.z), 0.8f + strength * 1.2f);
                Audio?.Impact(point, strength);
                if (damage > 6f)
                {
                    Effects?.Shake(point, Mathf.Clamp01(damage / 40f) * 0.5f);
                }
            }
            if (damage > 0f)
            {
                TakeDamage(new DamageInfo(damage, DamageSource.Ram, other, point, new Vector3(normal.x, 0f, normal.z)));
            }
        }

        private void HitWall(Vector3 point, Vector3 normal, float impactSpeed)
        {
            float damage = CombatRules.WallDamage(impactSpeed);
            if (impactSpeed > 4f)
            {
                float strength = Mathf.Clamp01(impactSpeed / 25f);
                Effects?.Sparks(point + Vector3.up * 0.5f, new Vector3(normal.x, 0f, normal.z), 0.6f + strength * 1.4f);
                Audio?.Impact(point, strength);
                if (damage > 6f && car.IsHuman)
                {
                    Effects?.Shake(point, Mathf.Clamp01(damage / 40f) * 0.45f);
                }
            }
            if (damage > 0f)
            {
                TakeDamage(new DamageInfo(damage, DamageSource.Wall, null, point, new Vector3(normal.x, 0f, normal.z)));
            }
        }

        /// <summary>Whether this car (not <paramref name="other"/>) shows the crash of the two: the one of the lower seat.</summary>
        private bool PlaysCrashWith(Car other)
        {
            int own = Seat;
            int theirs = SeatOf(other);
            return own != theirs ? own < theirs : car.GetHashCode() < other.GetHashCode();
        }

        /// <summary>Grinding along a barrier or another car: sparks, the scrape, and a little damage from the barrier.</summary>
        private void OnCollisionStay(Collision collision)
        {
            if (car == null || car.IsWrecked || collision.contactCount == 0)
            {
                return;
            }
            Rigidbody body = car.Body != null ? car.Body.Rigidbody : null;
            if (body == null)
            {
                return;
            }
            ContactPoint contact = collision.GetContact(0);
            Vector3 normal = new Vector3(contact.normal.x, 0f, contact.normal.z);
            Vector3 velocity = body.linearVelocity;
            if (collision.rigidbody != null)
            {
                velocity -= collision.rigidbody.linearVelocity;
            }
            Vector3 along = velocity - Vector3.Project(velocity, normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward);
            along.y = 0f;
            float slide = along.magnitude;
            if (slide < 5f)
            {
                return;
            }
            bool wall = collision.gameObject.layer == DeathRaceLayers.Wall;
            float dt = Time.fixedDeltaTime;
            if (wall)
            {
                // A barrier is many boxes: touching two at a corner is still one scrape this step.
                if (Mathf.Approximately(scrapedAt, Time.fixedTime))
                {
                    return;
                }
                scrapedAt = Time.fixedTime;
                scrapeTimer -= dt;
                if (scrapeTimer <= 0f)
                {
                    scrapeTimer = Mathf.Lerp(0.09f, 0.04f, Mathf.InverseLerp(5f, 30f, slide));
                    Effects?.Sparks(contact.point + Vector3.up * 0.4f, -along.normalized + normal * 0.6f, Mathf.InverseLerp(5f, 30f, slide) + 0.4f);
                    Audio?.Scrape(contact.point, Mathf.InverseLerp(5f, 30f, slide));
                }
                float share = Mathf.InverseLerp(5f, CombatRules.ScrapeSpeed, slide);
                ScrapeDamage(CombatRules.ScrapeDamagePerSecond * share * dt, contact.point);
                return;
            }
            Car other = collision.collider != null ? collision.collider.GetComponentInParent<Car>() : null;
            if (other != null && PlaysCrashWith(other))
            {
                grindTimer -= dt;
                if (grindTimer <= 0f)
                {
                    grindTimer = 0.08f;
                    Effects?.Sparks(contact.point + Vector3.up * 0.5f, -along.normalized, 0.5f);
                }
            }
        }

        /// <summary>The steady wear of a scrape: a little every step, so the crash cooldown does not hold it back.</summary>
        private void ScrapeDamage(float amount, Vector3 point)
        {
            if (amount <= 0f || model == null || model.Dead || !Authoritative)
            {
                return;
            }
            float taken = model.Wear(amount);
            if (taken <= 0f)
            {
                return;
            }
            Health = model.Health;
            DamageTaken += taken;
            RefreshLook();
            if (model.Dead)
            {
                int killer = model.KillerOf(HealthModel.Nobody, Seat, Now);
                Wreck(new DamageInfo(taken, DamageSource.Wall, killer == HealthModel.Nobody ? null : lastAttacker, point, Vector3.zero));
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Looks
        // ------------------------------------------------------------------------------------------------

        /// <summary>The smoke, fire and dents follow the health.</summary>
        private void RefreshLook()
        {
            if (car != null && car.View != null)
            {
                car.View.SetDamage(Health01);
            }
            if (car != null && car.IsWrecked)
            {
                return;
            }
            int now = Health > 0f ? HealthModel.DamageStage(Health01) : 2;
            if (now == stage)
            {
                if (smoke != null)
                {
                    smoke.SetRate(Mathf.Lerp(1.6f, 0.7f, Health01 / CombatRules.SmokingAt));
                }
                return;
            }
            stage = now;
            StopLoop(ref smoke);
            StopLoop(ref fire);
            RaceEffects effects = Effects;
            if (effects == null || stage == 0)
            {
                return;
            }
            Vector3 engine = EnginePoint();
            // Bigger than authored: the smoke has to read from forty metres up, behind a car doing a hundred.
            smoke = effects.Loop(stage >= 2 ? EffectKind.BlackSmoke : EffectKind.Smoke, transform, engine, Quaternion.identity, stage >= 2 ? 1.7f : 1.5f);
            if (stage >= 2)
            {
                fire = effects.Loop(EffectKind.Fire, transform, engine + Vector3.down * 0.2f, Quaternion.identity, 0.9f);
            }
        }

        /// <summary>Where the smoke comes out: the bonnet, a little above the body.</summary>
        private Vector3 EnginePoint()
        {
            Vector3 size = car != null && car.Loadout != null ? CarSpecs.Of(car.Loadout.model).Size : new Vector3(2f, 1.4f, 4.2f);
            return new Vector3(0f, size.y + 0.1f, size.z * 0.28f);
        }

        /// <summary>Stops a loop; what it already puffed out stays in the world and fades there.</summary>
        private static void StopLoop(ref RaceEffect effect)
        {
            if (effect != null)
            {
                effect.Stop();
            }
            effect = null;
        }

        // ------------------------------------------------------------------------------------------------
        // The wreck
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// The end of the car: the big explosion, a blast that hurts and throws the cars nearby (the kill of what it wrecks
        /// counts for whoever wrecked this one), a shake for the people near it, a scorch mark, and then the race's wreck of
        /// the car (<see cref="Car.Wreck"/>: it stops and is out). It keeps burning a while.
        /// </summary>
        protected virtual void Wreck(DamageInfo cause)
        {
            if (car == null || car.IsWrecked)
            {
                return;
            }
            Health = 0f;
            Vector3 at = transform.position;
            RaceEffects effects = Effects;
            effects?.Explosion(at + Vector3.up * 0.8f, 2.4f);
            effects?.Scorch(at, 1.6f);
            effects?.Shake(at, 0.85f);
            Audio?.Explosion(at, true);
            Blast.Explode(car.Race, at, CombatRules.WreckBlastRadius, CombatRules.WreckBlastDamage, CombatRules.WreckPush, DamageSource.Explosion, cause.Attacker, car);
            car.Wreck(cause);
            if (car.View != null)
            {
                car.View.SetDamage(0f);
            }
            StopLoop(ref smoke);
            StopLoop(ref fire);
            if (effects != null)
            {
                Vector3 engine = EnginePoint();
                smoke = effects.Loop(EffectKind.BlackSmoke, transform, engine, Quaternion.identity, 2f);
                fire = effects.Loop(EffectKind.Fire, transform, engine + Vector3.down * 0.3f, Quaternion.identity, 1.3f);
            }
            wreckBurn = WreckBurnSeconds;
            wreckSmokeOnly = false;
        }

        private void Update()
        {
            if (wreckBurn <= 0f)
            {
                return;
            }
            wreckBurn -= Time.deltaTime;
            if (!wreckSmokeOnly && wreckBurn < WreckBurnSeconds * 0.45f)
            {
                // The fire dies down; the wreck smoulders.
                wreckSmokeOnly = true;
                StopLoop(ref fire);
            }
            if (wreckBurn <= 0f)
            {
                StopLoop(ref smoke);
            }
        }

        private void OnDestroy()
        {
            if (car != null && car.Body != null)
            {
                car.Body.Collided -= OnCollided;
            }
        }
    }
}
