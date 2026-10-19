using System;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A car's weapons: the machine gun and its rounds, the mines and rockets it carries, and its nitro charges. The car
    /// hands it the driver's input every physics step (<see cref="Tick"/>). The gun is hitscan: each round flies straight
    /// out of the muzzle (a little spread) to the first car or barrier within its range, drawn as a tracer with a flash
    /// and sparks. Mines drop behind (<see cref="Mine"/>), rockets launch forward (<see cref="Rocket"/>), a nitro charge
    /// burns for a couple of seconds with flames from the exhausts. What is carried and spent is a
    /// <see cref="WeaponStock"/>; what each weapon does is in <see cref="CombatRules"/>.
    /// </summary>
    public class CarWeapons : MonoBehaviour
    {
        private const float MuzzleHeight = 0.75f;

        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        private WeaponStock stock = new WeaponStock(100, 0, 0, 0);
        private Car car;
        private System.Random random = new System.Random(1);
        private Transform fallbackMuzzle;
        private RaceEffect[] flames = new RaceEffect[0];
        private bool disarmed;
        private bool emptyGun;
        private bool nitroWasHeld;
        private int hitCount;

        public int Ammo { get; protected set; }
        public int AmmoCapacity { get; protected set; }
        public int Mines { get; protected set; }
        public int Rockets { get; protected set; }
        public int NitroCharges { get; protected set; }

        /// <summary>Whether a nitro charge is burning.</summary>
        public bool NitroActive { get; protected set; }

        /// <summary>Whether the trigger is held with rounds left (online poses, the HUD, the gun's sound).</summary>
        public bool Firing { get; protected set; }

        public int MinesUsed { get; protected set; }
        public int RocketsUsed { get; protected set; }
        public int NitroUsed { get; protected set; }

        /// <summary>Rounds fired this race, and how many of them hit a car.</summary>
        public int RoundsFired => stock.RoundsFired;

        public int RoundsHit => hitCount;

        /// <summary>The share of the burning nitro charge left, 0 to 1 (0 when none burns).</summary>
        public float NitroLeft01 => stock.NitroLeft / CombatRules.NitroSeconds;

        public float GunRange => CombatRules.GunRange;

        /// <summary>A round left the muzzle: where from, where it stopped, the car it hit (or null).</summary>
        public event Action<Vector3, Vector3, Car> Fired;

        /// <summary>A mine was dropped (online: an action for the room).</summary>
        public event Action<Mine> MineDropped;

        /// <summary>A rocket was fired (online: an action for the room).</summary>
        public event Action<Rocket> RocketFired;

        /// <summary>Called by the factory once the car is set up: the loadout says what the car carries.</summary>
        public virtual void Setup(Car car, CarLoadout loadout)
        {
            this.car = car;
            int capacity = car != null ? car.Stats.AmmoCapacity : CarStats.Of(loadout).AmmoCapacity;
            stock = new WeaponStock(capacity, loadout?.mines ?? 0, loadout?.rockets ?? 0, loadout?.nitro ?? 0);
            int seed = car != null && car.Race != null && car.Race.Setup != null ? car.Race.Setup.Seed : 0;
            int seat = car != null && car.Entrant != null ? car.Entrant.Seat : 0;
            random = new System.Random(unchecked(seed * 7919 + seat * 104729 + 17));
            disarmed = false;
            hitCount = 0;
            Sync();
        }

        private RaceEffects Effects => car != null && car.Race != null ? car.Race.Effects : null;

        private RaceAudio Audio => car != null && car.Race != null && car.Race.Audio != null ? car.Race.Audio : RaceAudio.Current;

        private DeathRaceContent Content => car != null && car.Race != null && car.Race.Effects != null ? car.Race.Effects.Content : null;

        /// <summary>One physics step with the driver's input.</summary>
        public virtual void Tick(CarInput input, float deltaTime)
        {
            if (car == null)
            {
                car = GetComponent<Car>();
            }
            bool live = !disarmed && (car == null || !car.IsWrecked) && (car == null || car.Race == null || car.Race.IsRunning);
            Gun(live && input.Fire, deltaTime);
            if (live && input.DropMine)
            {
                if (stock.TakeMine())
                {
                    DropMine();
                }
                else
                {
                    Empty();
                }
            }
            if (live && input.FireRocket)
            {
                if (stock.TakeRocket())
                {
                    FireRocket();
                }
                else
                {
                    Empty();
                }
            }
            Nitro(live && input.Nitro, deltaTime);
            Sync();
        }

        // ------------------------------------------------------------------------------------------------
        // The machine gun
        // ------------------------------------------------------------------------------------------------

        private void Gun(bool held, float deltaTime)
        {
            int rounds = stock.Fire(held, deltaTime);
            Firing = held && stock.Ammo > 0;
            for (int i = 0; i < rounds; i++)
            {
                FireRound();
            }
            if (held && stock.Ammo == 0 && rounds == 0)
            {
                if (!emptyGun)
                {
                    emptyGun = true;
                    Empty();
                }
            }
            else if (!held)
            {
                emptyGun = false;
            }
        }

        /// <summary>Where the rounds leave from: the model's muzzle, or the middle of the bonnet.</summary>
        public Transform Muzzle
        {
            get
            {
                if (car != null && car.View != null && car.View.muzzle != null)
                {
                    return car.View.muzzle;
                }
                if (fallbackMuzzle == null)
                {
                    fallbackMuzzle = new GameObject("Muzzle").transform;
                    fallbackMuzzle.SetParent(transform, false);
                    float length = car != null && car.Loadout != null ? car.Loadout.Spec.Size.z : 4.2f;
                    fallbackMuzzle.localPosition = new Vector3(0f, MuzzleHeight, length * 0.5f + 0.1f);
                }
                return fallbackMuzzle;
            }
        }

        private void FireRound()
        {
            Transform muzzle = Muzzle;
            Vector3 origin = muzzle.position;
            origin.y = Mathf.Clamp(origin.y, 0.5f, 1.1f);
            float spread = ((float)random.NextDouble() * 2f - 1f) * CombatRules.GunSpread;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            Vector3 direction = Quaternion.Euler(0f, spread, 0f) * forward.normalized;
            Vector3 end = origin + direction * CombatRules.GunRange;
            Car victim = null;
            if (Nearest(origin, direction, out RaycastHit hit))
            {
                end = hit.point;
                victim = hit.collider.GetComponentInParent<Car>();
                Vector3 normal = new Vector3(hit.normal.x, 0f, hit.normal.z);
                if (victim != null)
                {
                    hitCount++;
                    // Whether it hurts is the victim's to judge (online, only the device that drives a car hurts it).
                    if (victim.Health != null && !victim.IsWrecked)
                    {
                        victim.Health.TakeDamage(new DamageInfo(CombatRules.RoundDamage(car != null ? car.Stats : CarStats.Of(null)), DamageSource.Gun, car, hit.point, direction));
                    }
                    Effects?.Sparks(hit.point, Vector3.Reflect(direction, normal.sqrMagnitude > 0.0001f ? normal.normalized : -direction), 1f);
                    if (random.NextDouble() < 0.3)
                    {
                        Audio?.Ricochet(hit.point);
                    }
                }
                else
                {
                    Effects?.Sparks(hit.point, normal, 0.6f);
                    if (random.NextDouble() < 0.2)
                    {
                        Audio?.Ricochet(hit.point);
                    }
                }
            }
            RaceEffects effects = Effects;
            if (effects != null)
            {
                effects.Tracer(origin, end);
                effects.Muzzle(muzzle);
            }
            Audio?.Gun(origin);
            Fired?.Invoke(origin, end, victim);
        }

        /// <summary>The first car or barrier along the round's way, not this car itself.</summary>
        private bool Nearest(Vector3 origin, Vector3 direction, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(origin, direction, Hits, CombatRules.GunRange, DeathRaceLayers.ShotMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.collider == null || hit.distance >= best)
                {
                    continue;
                }
                Car owner = hit.collider.GetComponentInParent<Car>();
                if (owner != null && owner == car)
                {
                    continue;
                }
                best = hit.distance;
                nearest = hit;
                found = true;
            }
            return found;
        }

        // ------------------------------------------------------------------------------------------------
        // Mines and rockets
        // ------------------------------------------------------------------------------------------------

        /// <summary>Drops a mine behind the car (the stock was already taken).</summary>
        private void DropMine()
        {
            Vector3 at;
            if (car != null && car.View != null && car.View.mineDrop != null)
            {
                at = car.View.mineDrop.position;
            }
            else
            {
                float length = car != null && car.Loadout != null ? car.Loadout.Spec.Size.z : 4.2f;
                at = transform.TransformPoint(new Vector3(0f, 0f, -length * 0.5f - 0.9f));
            }
            Mine mine = Mine.Spawn(car != null ? car.Race : null, Content, at, transform.rotation, car, MinesUsed + 1);
            Audio?.MineDrop(at);
            MineDropped?.Invoke(mine);
        }

        /// <summary>Fires a rocket ahead (the stock was already taken).</summary>
        private void FireRocket()
        {
            Vector3 at;
            if (car != null && car.View != null && car.View.rocketMount != null)
            {
                at = car.View.rocketMount.position;
            }
            else
            {
                at = transform.TransformPoint(new Vector3(0f, 1.2f, 0.4f));
            }
            float speed = Mathf.Max(0f, car != null ? car.Speed : 0f) + CombatRules.RocketLaunchSpeed;
            Rocket rocket = Rocket.Launch(car != null ? car.Race : null, Content, at, transform.forward, speed, car);
            Effects?.Muzzle(rocket.transform);
            Audio?.RocketLaunch(at);
            RocketFired?.Invoke(rocket);
        }

        // ------------------------------------------------------------------------------------------------
        // Nitro
        // ------------------------------------------------------------------------------------------------

        private void Nitro(bool held, float deltaTime)
        {
            bool wasBurning = stock.NitroActive;
            bool lit = stock.Nitro(held, deltaTime);
            if (held && !nitroWasHeld && !lit && !stock.NitroActive)
            {
                Empty();
            }
            nitroWasHeld = held;
            if (lit)
            {
                Audio?.Nitro(transform.position);
                car?.Raise(new CarEvent(CarEventKind.Nitro, stock.NitroCharges));
            }
            if (stock.NitroActive != wasBurning || lit)
            {
                ShowNitro(stock.NitroActive);
            }
        }

        private void ShowNitro(bool burning)
        {
            if (car != null && car.Body != null)
            {
                car.Body.Nitro = burning;
            }
            if (car != null && car.View != null)
            {
                car.View.SetNitro(burning);
            }
            if (burning)
            {
                if (flames.Length == 0 || flames[0] == null || !flames[0].Playing)
                {
                    LightFlames();
                }
            }
            else
            {
                foreach (RaceEffect flame in flames)
                {
                    if (flame != null)
                    {
                        flame.Stop();
                    }
                }
                flames = new RaceEffect[0];
            }
        }

        /// <summary>A flame out of each exhaust, pointing back along the car whatever way the exhaust is turned.</summary>
        private void LightFlames()
        {
            RaceEffects effects = Effects;
            if (effects == null)
            {
                return;
            }
            Transform[] exhausts = car != null && car.View != null && car.View.exhausts != null && car.View.exhausts.Length > 0 ? car.View.exhausts : null;
            if (exhausts == null)
            {
                float length = car != null && car.Loadout != null ? car.Loadout.Spec.Size.z : 4.2f;
                flames = new[]
                {
                    effects.Loop(EffectKind.NitroFlame, transform, new Vector3(-0.45f, 0.45f, -length * 0.5f), Quaternion.LookRotation(Vector3.back)),
                    effects.Loop(EffectKind.NitroFlame, transform, new Vector3(0.45f, 0.45f, -length * 0.5f), Quaternion.LookRotation(Vector3.back))
                };
                return;
            }
            flames = new RaceEffect[exhausts.Length];
            Quaternion back = Quaternion.LookRotation(-transform.forward, Vector3.up);
            for (int i = 0; i < exhausts.Length; i++)
            {
                if (exhausts[i] != null)
                {
                    flames[i] = effects.Loop(EffectKind.NitroFlame, exhausts[i], Vector3.zero, Quaternion.Inverse(exhausts[i].rotation) * back);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Stock
        // ------------------------------------------------------------------------------------------------

        /// <summary>Something was used with nothing left: the driver hears a dry click.</summary>
        private void Empty()
        {
            car?.Raise(new CarEvent(CarEventKind.Empty));
            if (car != null && car.IsHuman)
            {
                Audio?.Deny();
            }
        }

        /// <summary>Adds rounds (a pickup), up to the capacity; returns how many were added.</summary>
        public virtual int AddAmmo(int rounds)
        {
            int added = stock.AddAmmo(rounds);
            Sync();
            return added;
        }

        public virtual void AddNitro(int charges)
        {
            stock.AddNitro(charges);
            Sync();
        }

        public void AddMines(int mines)
        {
            stock.AddMines(mines);
            Sync();
        }

        public void AddRockets(int rockets)
        {
            stock.AddRockets(rockets);
            Sync();
        }

        /// <summary>The car was wrecked: nothing fires any more.</summary>
        public virtual void Disarm()
        {
            disarmed = true;
            stock.Douse();
            Firing = false;
            ShowNitro(false);
            Sync();
        }

        private void Sync()
        {
            Ammo = stock.Ammo;
            AmmoCapacity = stock.AmmoCapacity;
            Mines = stock.Mines;
            Rockets = stock.Rockets;
            NitroCharges = stock.NitroCharges;
            NitroActive = stock.NitroActive;
            MinesUsed = stock.MinesUsed;
            RocketsUsed = stock.RocketsUsed;
            NitroUsed = stock.NitroUsed;
        }

        private void OnDestroy()
        {
            foreach (RaceEffect flame in flames)
            {
                if (flame != null)
                {
                    flame.Stop();
                }
            }
        }
    }
}
