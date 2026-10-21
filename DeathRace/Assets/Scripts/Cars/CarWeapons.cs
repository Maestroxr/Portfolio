using System;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// A car's weapons: the gun on its bonnet and its rounds, the mines and rockets it carries, its nitro charges and its
    /// special weapons. The car hands it the driver's input every physics step (<see cref="Tick"/>).
    ///
    /// The gun is the one of the car's stats (<see cref="Guns"/>), firing at its rate (the gun mount's faster) for its
    /// damage (armour-piercing rounds and the mount's harder): the machine gun and the minigun are hitscan, each round
    /// flying straight out of the muzzle (with the gun's spread) to the first car or barrier within its range, drawn as a
    /// tracer (the minigun's thinner and quicker) with a flash and sparks; the flamethrower sends a puff of fire every
    /// round that hurts every car in a short cone ahead that no barrier hides and sets it burning
    /// (<see cref="CarStatus.Burn"/>), a jet of flame roaring from the muzzle while it fires; the plasma cannon fires
    /// glowing bolts that fly (<see cref="PlasmaBolt"/>) and burst where they hit.
    ///
    /// Mines drop behind (<see cref="Mine"/>), rockets launch forward (<see cref="Rocket"/>), a nitro charge burns for a
    /// couple of seconds (longer with the nitro system) with flames from the exhausts. The special button fires the
    /// special armed, or the next one there is (<see cref="NextSpecial"/>): an oil slick poured behind
    /// (<see cref="OilSlick"/>), an EMP blast round the car (<see cref="EmpBlast"/>), a mortar shell lobbed ahead along the
    /// track (<see cref="MortarShell"/>); each raises its event with what it made (online: an action for the room). A
    /// jammed car (<see cref="CarStatus.IsJammed"/>) fires nothing and its burning charge goes out. What is carried and
    /// spent is a <see cref="WeaponStock"/>; what each weapon does is in <see cref="CombatRules"/>.
    /// </summary>
    public class CarWeapons : MonoBehaviour
    {
        private const float MuzzleHeight = 0.75f;

        /// <summary>The minigun's tracers: thinner and quicker than the machine gun's (0.34 m, 0.09 s).</summary>
        private const float MinigunTracerWidth = 0.2f;

        private const float MinigunTracerSeconds = 0.06f;

        /// <summary>How far behind the car's tail an oil slick is poured (to its middle).</summary>
        private const float OilBehind = 2.2f;

        private const float FlameVolume = 0.7f;

        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        private WeaponStock stock = new WeaponStock(100, 0, 0, 0);
        private GunSpec gun = Guns.Of(GunKind.MachineGun);
        private Car car;
        private System.Random random = new System.Random(1);
        private Transform fallbackMuzzle;
        private RaceEffect[] flames = new RaceEffect[0];
        private RaceEffect flameJet;
        private AudioSource flameSound;
        private bool flameOn;
        private bool disarmed;
        private bool emptyGun;
        private bool nitroWasHeld;
        private int hitCount;
        private int roundsShown;

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

        /// <summary>Rounds fired this race, and how many of them hit a car (a puff of flame counts once for every car it licks).</summary>
        public int RoundsFired => stock.RoundsFired;

        public int RoundsHit => hitCount;

        /// <summary>The share of the burning nitro charge left, 0 to 1 (0 when none burns).</summary>
        public float NitroLeft01 => stock.NitroLeft / Mathf.Max(0.01f, stock.NitroSeconds);

        /// <summary>The gun on the bonnet.</summary>
        public GunKind Gun => gun.Kind;

        /// <summary>Everything about the gun on the bonnet: its rate, damage, reach, spread, how its rounds travel.</summary>
        public GunSpec GunSpec => gun;

        public float GunRange => gun.Range;

        /// <summary>How many rounds the gun fires a second (the gun mount's faster).</summary>
        public float RoundsPerSecond => stock.RoundsPerSecond;

        /// <summary>What one round takes off before the target's armour (armour-piercing rounds and the gun mount included).</summary>
        public float RoundDamage => CombatRules.RoundDamage(car != null ? car.Stats : CarStats.Of(null), gun);

        /// <summary>Whether an EMP blast has the car jammed: nothing fires.</summary>
        public bool Jammed => car != null && car.Status != null && car.Status.IsJammed;

        /// <summary>A round left the muzzle: where from, where it stopped (or would), the car it hit (or null).</summary>
        public event Action<Vector3, Vector3, Car> Fired;

        /// <summary>A mine was dropped (online: an action for the room).</summary>
        public event Action<Mine> MineDropped;

        /// <summary>A rocket was fired (online: an action for the room).</summary>
        public event Action<Rocket> RocketFired;

        /// <summary>An oil slick was poured (online: an action for the room).</summary>
        public event Action<OilSlick> OilDropped;

        /// <summary>An EMP blast went off round the car (online: an action for the room).</summary>
        public event Action<EmpBlast> EmpFired;

        /// <summary>A mortar shell was lobbed (online: an action for the room).</summary>
        public event Action<MortarShell> MortarFired;

        private SpecialKind armedSpecial;

        /// <summary>The special weapon the button fires next (the armed one, or the next with any left); null when none are left.</summary>
        public SpecialKind? NextSpecial => stock.NextSpecial(armedSpecial);

        /// <summary>The special weapon armed (fired first while there is any of it).</summary>
        public SpecialKind ArmedSpecial => armedSpecial;

        /// <summary>How many of <paramref name="kind"/> are left.</summary>
        public int SpecialsLeft(SpecialKind kind)
        {
            return stock.SpecialsLeft(kind);
        }

        public int SpecialsUsed(SpecialKind kind)
        {
            return stock.SpecialsUsed(kind);
        }

        /// <summary>How many special weapons of every kind are left.</summary>
        public int SpecialsCarried => stock.SpecialsLeft(SpecialKind.Oil) + stock.SpecialsLeft(SpecialKind.Emp) + stock.SpecialsLeft(SpecialKind.Mortar);

        /// <summary>For the weapons work and the online race: raise the events of the special weapons.</summary>
        protected void RaiseOilDropped(OilSlick slick) => OilDropped?.Invoke(slick);

        protected void RaiseEmpFired(EmpBlast blast) => EmpFired?.Invoke(blast);

        protected void RaiseMortarFired(MortarShell shell) => MortarFired?.Invoke(shell);

        /// <summary>Called by the factory once the car is set up: the loadout says what the car carries.</summary>
        public virtual void Setup(Car car, CarLoadout loadout)
        {
            this.car = car;
            int capacity = car != null ? car.Stats.AmmoCapacity : CarStats.Of(loadout).AmmoCapacity;
            CarStats stats = car != null ? car.Stats : CarStats.Of(loadout);
            gun = Guns.Of(stats.Gun);
            stock = new WeaponStock(capacity, loadout?.mines ?? 0, loadout?.rockets ?? 0, loadout?.nitro ?? 0, loadout?.oil ?? 0, loadout?.emp ?? 0,
                loadout?.mortar ?? 0, gun.RoundsPerSecond * (stats.GunRate > 0f ? stats.GunRate : 1f), stats.NitroSeconds);
            armedSpecial = loadout != null ? loadout.special : SpecialKind.Oil;
            int seed = car != null && car.Race != null && car.Race.Setup != null ? car.Race.Setup.Seed : 0;
            int seat = car != null && car.Entrant != null ? car.Entrant.Seat : 0;
            random = new System.Random(unchecked(seed * 7919 + seat * 104729 + 17));
            disarmed = false;
            hitCount = 0;
            ShowFlame(false);
            Sync();
        }

        private RaceEffects Effects => car != null && car.Race != null ? car.Race.Effects : null;

        private RaceAudio Audio => car != null && car.Race != null && car.Race.Audio != null ? car.Race.Audio : RaceAudio.Current;

        private DeathRaceContent Content => car != null && car.Race != null && car.Race.Effects != null ? car.Race.Effects.Content : null;

        private Race Race => car != null ? car.Race : null;

        /// <summary>One physics step with the driver's input.</summary>
        public virtual void Tick(CarInput input, float deltaTime)
        {
            if (car == null)
            {
                car = GetComponent<Car>();
            }
            bool jammed = Jammed;
            if (jammed && stock.NitroActive)
            {
                // The pulse kills the electrics: a burning charge goes out with them.
                stock.Douse();
                ShowNitro(false);
            }
            bool live = !disarmed && !jammed && (car == null || !car.IsWrecked) && (car == null || car.Race == null || car.Race.IsRunning);
            Trigger(live && input.Fire, deltaTime);
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
            if (live && input.Special)
            {
                FireSpecial();
            }
            Nitro(live && input.Nitro, deltaTime);
            Sync();
        }

        // ------------------------------------------------------------------------------------------------
        // The gun
        // ------------------------------------------------------------------------------------------------

        private void Trigger(bool held, float deltaTime)
        {
            int rounds = stock.Fire(held, deltaTime);
            Firing = held && stock.Ammo > 0;
            for (int i = 0; i < rounds; i++)
            {
                FireRound();
            }
            ShowFlame(Firing && gun.IsFlame);
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

        /// <summary>The muzzle's point, at the height rounds fly (over the road, under the barriers' tops).</summary>
        private Vector3 MuzzlePoint(Transform muzzle)
        {
            Vector3 origin = muzzle.position;
            origin.y = Mathf.Clamp(origin.y, 0.5f, 1.1f);
            return origin;
        }

        private Vector3 FlatForward
        {
            get
            {
                Vector3 forward = transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
            }
        }

        private void FireRound()
        {
            if (gun.IsFlame)
            {
                FirePuff();
            }
            else if (gun.IsProjectile)
            {
                FireBolt();
            }
            else
            {
                FireBullet();
            }
        }

        /// <summary>A round of the machine gun or the minigun: straight to the first car or barrier, at once.</summary>
        private void FireBullet()
        {
            Transform muzzle = Muzzle;
            Vector3 origin = MuzzlePoint(muzzle);
            float spread = ((float)random.NextDouble() * 2f - 1f) * gun.Spread;
            Vector3 direction = Quaternion.Euler(0f, spread, 0f) * FlatForward;
            Vector3 end = origin + direction * gun.Range;
            Car victim = null;
            if (Nearest(origin, direction, gun.Range, out RaycastHit hit))
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
                        victim.Health.TakeDamage(new DamageInfo(RoundDamage, DamageSource.Gun, car, hit.point, direction));
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
            bool minigun = gun.Kind == GunKind.Minigun;
            roundsShown++;
            if (effects != null)
            {
                if (minigun)
                {
                    effects.Tracer(origin, end, MinigunTracerWidth, MinigunTracerSeconds);
                    // Sixteen rounds a second: a flash for every other one is a steady blaze already.
                    if (roundsShown % 2 == 0)
                    {
                        effects.Muzzle(muzzle);
                    }
                }
                else
                {
                    effects.Tracer(origin, end);
                    effects.Muzzle(muzzle);
                }
            }
            if (minigun)
            {
                Audio?.Minigun(origin);
            }
            else
            {
                Audio?.Gun(origin);
            }
            Fired?.Invoke(origin, end, victim);
        }

        /// <summary>The first car or barrier along the round's way within <paramref name="range"/>, not this car itself.</summary>
        private bool Nearest(Vector3 origin, Vector3 direction, float range, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(origin, direction, Hits, range, DeathRaceLayers.ShotMask, QueryTriggerInteraction.Ignore);
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

        /// <summary>
        /// A puff of the flamethrower: every running car in the cone ahead within reach (measured to the nearest point of
        /// its body) that no barrier hides takes the puff's damage and catches fire.
        /// </summary>
        private void FirePuff()
        {
            Vector3 origin = MuzzlePoint(Muzzle);
            Vector3 forward = FlatForward;
            Vector2 from = new Vector2(origin.x, origin.z);
            Vector2 ahead = new Vector2(forward.x, forward.z);
            float damage = RoundDamage;
            Car first = null;
            float firstDistance = float.MaxValue;
            foreach (Car other in Targeting.CarsOf(Race))
            {
                if (other == null || other == car || other.IsWrecked || (Race != null && other.Race != null && other.Race != Race))
                {
                    continue;
                }
                Vector3 nearest = other.Body != null && other.Body.Collider != null && other.Body.Collider.enabled
                    ? other.Body.Collider.ClosestPoint(origin)
                    : other.transform.position;
                Vector2 toNearest = new Vector2(nearest.x, nearest.z) - from;
                Vector2 toMiddle = other.Position2D - from;
                bool reaches = CombatRules.FlameReaches(ahead, toNearest, 0f, gun.Range, gun.Cone) ||
                               CombatRules.FlameReaches(ahead, toMiddle, 0f, gun.Range, gun.Cone);
                if (!reaches)
                {
                    continue;
                }
                Vector3 target = new Vector3(nearest.x, origin.y, nearest.z);
                if (Physics.Linecast(origin, target, DeathRaceLayers.WallMask, QueryTriggerInteraction.Ignore))
                {
                    // A barrier between: the fire licks the barrier, not the car behind it.
                    continue;
                }
                hitCount++;
                Vector3 direction = new Vector3(toMiddle.x, 0f, toMiddle.y);
                direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : forward;
                // Whether it hurts and burns is the victim's to judge (online, only the device that drives a car does).
                other.Health?.TakeDamage(new DamageInfo(damage, DamageSource.Flame, car, nearest, direction));
                other.Status?.Burn(car);
                if (toMiddle.sqrMagnitude < firstDistance)
                {
                    firstDistance = toMiddle.sqrMagnitude;
                    first = other;
                }
            }
            Fired?.Invoke(origin, origin + forward * gun.Range, first);
        }

        /// <summary>A bolt of the plasma cannon, flying straight out at the cannon's speed on top of the car's own.</summary>
        private void FireBolt()
        {
            Transform muzzle = Muzzle;
            Vector3 origin = MuzzlePoint(muzzle);
            Vector3 direction = FlatForward;
            float speed = gun.ProjectileSpeed + Mathf.Max(0f, car != null ? car.Speed : 0f);
            PlasmaBolt.Launch(Race, Content, origin, direction, speed, car, RoundDamage, gun.Range, victim => hitCount++);
            Effects?.Muzzle(muzzle);
            Audio?.PlasmaShot(origin);
            Fired?.Invoke(origin, origin + direction * gun.Range, null);
        }

        /// <summary>The flamethrower's jet and its roar, on while it fires.</summary>
        private void ShowFlame(bool on)
        {
            if (on == flameOn && (!on || (flameJet != null && flameJet.Playing)))
            {
                return;
            }
            flameOn = on;
            if (on)
            {
                RaceEffects effects = Effects;
                if (effects != null && (flameJet == null || !flameJet.Playing))
                {
                    flameJet = effects.Loop(EffectKind.FlameJet, Muzzle, Vector3.zero, Quaternion.identity, 1f);
                }
                RaceAudio audio = Audio;
                if (flameSound == null && audio != null && audio.Sounds != null)
                {
                    flameSound = audio.LoopOn(transform, audio.Sounds.flame, "Flamethrower");
                }
                if (flameSound != null)
                {
                    flameSound.volume = audio != null ? audio.LoopVolume(FlameVolume) : FlameVolume;
                    if (!flameSound.isPlaying)
                    {
                        flameSound.time = 0f;
                        flameSound.Play();
                    }
                }
            }
            else
            {
                if (flameJet != null)
                {
                    flameJet.Stop();
                    flameJet = null;
                }
                if (flameSound != null && flameSound.isPlaying)
                {
                    flameSound.Stop();
                }
            }
        }

        private void Update()
        {
            // The physics steps stop with the game; the flamethrower's roar has to be paused by hand.
            if (flameSound == null || !flameOn)
            {
                return;
            }
            if (Time.timeScale <= 0f && flameSound.isPlaying)
            {
                flameSound.Pause();
            }
            else if (Time.timeScale > 0f && !flameSound.isPlaying)
            {
                flameSound.UnPause();
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Mines and rockets
        // ------------------------------------------------------------------------------------------------

        /// <summary>Where things are dropped behind the car: the model's mine drop, or behind the tail.</summary>
        private Vector3 DropPoint(float further)
        {
            float length = car != null && car.Loadout != null ? car.Loadout.Spec.Size.z : 4.2f;
            if (car != null && car.View != null && car.View.mineDrop != null)
            {
                return car.View.mineDrop.position - FlatForward * further;
            }
            return transform.TransformPoint(new Vector3(0f, 0f, -length * 0.5f - 0.9f - further));
        }

        /// <summary>Drops a mine behind the car (the stock was already taken).</summary>
        private void DropMine()
        {
            Vector3 at = DropPoint(0f);
            Mine mine = Mine.Spawn(Race, Content, at, transform.rotation, car, MinesUsed + 1);
            Audio?.MineDrop(at);
            MineDropped?.Invoke(mine);
        }

        /// <summary>Where rockets and shells leave from: the model's rocket mount, or the roof.</summary>
        private Vector3 RoofPoint()
        {
            if (car != null && car.View != null && car.View.rocketMount != null)
            {
                return car.View.rocketMount.position;
            }
            return transform.TransformPoint(new Vector3(0f, 1.2f, 0.4f));
        }

        /// <summary>Fires a rocket ahead (the stock was already taken).</summary>
        private void FireRocket()
        {
            Vector3 at = RoofPoint();
            float speed = Mathf.Max(0f, car != null ? car.Speed : 0f) + CombatRules.RocketLaunchSpeed;
            Rocket rocket = Rocket.Launch(Race, Content, at, transform.forward, speed, car);
            Effects?.Muzzle(rocket.transform);
            Audio?.RocketLaunch(at);
            RocketFired?.Invoke(rocket);
        }

        // ------------------------------------------------------------------------------------------------
        // Special weapons
        // ------------------------------------------------------------------------------------------------

        /// <summary>The special button: the special armed, or the next one there is; a dry click when none are left.</summary>
        private void FireSpecial()
        {
            SpecialKind? next = NextSpecial;
            if (!next.HasValue || !stock.TakeSpecial(next.Value))
            {
                Empty();
                return;
            }
            switch (next.Value)
            {
                case SpecialKind.Oil:
                    PourOil();
                    break;
                case SpecialKind.Emp:
                    FireEmp();
                    break;
                default:
                    FireMortar();
                    break;
            }
        }

        /// <summary>Pours a slick behind the car (the stock was already taken).</summary>
        private void PourOil()
        {
            Vector3 at = DropPoint(OilBehind);
            OilSlick slick = OilSlick.Spawn(Race, Content, at, transform.rotation, car, stock.SpecialsUsed(SpecialKind.Oil));
            RaiseOilDropped(slick);
        }

        /// <summary>Sets off a pulse round the car (the stock was already taken).</summary>
        private void FireEmp()
        {
            EmpBlast blast = EmpBlast.Fire(Race, Content, transform.position, car);
            RaiseEmpFired(blast);
        }

        /// <summary>Lobs a shell ahead (the stock was already taken).</summary>
        private void FireMortar()
        {
            MortarShell shell = MortarShell.Launch(Race, Content, RoofPoint(), MortarAim(), car);
            RaiseMortarFired(shell);
        }

        /// <summary>
        /// Where a shell fired now comes down: <see cref="CombatRules.MortarReach"/> ahead along the track (at the car's own
        /// place across the road, kept off the verges), so it lands on the road even round a bend; straight ahead off a
        /// track (a test).
        /// </summary>
        public Vector3 MortarAim()
        {
            float reach = CombatRules.MortarReach(car != null ? car.Speed : 0f);
            Track track = Race != null ? Race.Track : null;
            if (car != null && track != null && track.Path != null)
            {
                float distance = car.TrackDistance + reach;
                float half = track.HalfWidthAt(distance);
                float offset = Mathf.Clamp(car.TrackOffset, -half * 0.6f, half * 0.6f);
                Vector2 point = track.Path.PointAt(distance) + track.Path.NormalAt(distance) * offset;
                return new Vector3(point.x, 0f, point.y);
            }
            Vector3 ahead = transform.position + FlatForward * reach;
            return new Vector3(ahead.x, 0f, ahead.z);
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

        public void AddSpecials(SpecialKind kind, int count)
        {
            stock.AddSpecials(kind, count);
            Sync();
        }

        /// <summary>
        /// Sets the stock outright: what a car taken over from another device mid-race carries, as reckoned from the room's
        /// log (<see cref="HandedOverStock"/>). The rounds and the nitro charges spent so far stay counted.
        /// </summary>
        public void Restock(HandedOverStock.Stock carried)
        {
            stock.Set(carried.Ammo, carried.Mines, carried.Rockets, carried.Nitro, carried.Oil, carried.Emp, carried.Mortar);
            Sync();
        }

        /// <summary>The car was wrecked: nothing fires any more.</summary>
        public virtual void Disarm()
        {
            disarmed = true;
            stock.Douse();
            Firing = false;
            ShowNitro(false);
            ShowFlame(false);
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
            if (flameJet != null)
            {
                flameJet.Stop();
            }
        }
    }
}
