using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The strike weapons of the local ship: one fire button fires every always-on weapon the pilot owns plus the selected
    /// special, cycle selects the next owned special, and the megabomb clears the screen. Projectiles go through
    /// <see cref="SpawnService.FireStrikeShot"/>; beams and the laser turret's zaps hit directly
    /// (<see cref="SpaceField.FirstInColumn"/>, <see cref="SpaceField.RandomTarget"/>) and show through the ship's
    /// <see cref="PlayerBeam"/>. The <see cref="Loadout"/> (the working copy of the pilot) is the source of truth.
    ///
    /// The ship steps it once a frame (<see cref="Update"/>): the reload times run down, the dumbfire missiles that were
    /// dropped ignite, and while the fire button is held every weapon whose reload is over fires (<see cref="Fire"/>).
    /// Reload times are the rates of <see cref="StrikeWeaponRules"/> divided by the hull's fire rate multiplier (beams and
    /// zaps included).
    /// </summary>
    public class StrikeGunnery
    {
        /// <summary>Seconds a dumbfire missile drops sideways before it ignites.</summary>
        public const float DumbfireDropTime = 0.25f;

        /// <summary>Seconds after a volley during which the gunnery counts as firing.</summary>
        public const float FiringHold = 0.2f;

        /// <summary>The most volleys one weapon fires in a single frame (a long frame with a fast weapon).</summary>
        private const int MaxVolleysPerFrame = 4;

        private struct Dropping
        {
            public Shot Shot;
            public Vector2 DropVelocity;
            public float Drift;
            public float Time;
        }

        private readonly float[] reloads = new float[(int)StrikeItem.TwinLaser + 1];
        private readonly List<Dropping> dropping = new List<Dropping>(16);
        private readonly float[] beamDamage = new float[2];
        private readonly System.Random random;
        private AsteroidsPlayer lastShip;
        private StrikeItem lastSpecial;
        private float sinceVolley = float.MaxValue;
        private float beamClock;
        private float beamTick;
        private bool volleyThisFrame;

        /// <summary>The working copy of the pilot the mission flies with.</summary>
        public StrikeLoadout Loadout { get; }

        /// <summary>Whether a volley was fired recently (energy does not regenerate while firing).</summary>
        public bool IsFiring => sinceVolley < FiringHold;

        /// <summary>Whether a volley (a beam's included) was fired in the last <see cref="Update"/>: restarts the energy regeneration.</summary>
        public bool FiredThisFrame => volleyThisFrame;

        /// <summary>The selected special; <see cref="StrikeItem.MachineGun"/> when none.</summary>
        public StrikeItem Special => Loadout != null ? Loadout.Special : StrikeItem.MachineGun;

        /// <summary>Whether a beam weapon is held (the pose's beam bit for the other pilots).</summary>
        public bool BeamHeld { get; private set; }

        /// <summary>Which beam weapon is held (the pose's beam kind): 0 none, else the value of the item minus 11.</summary>
        public int BeamKind { get; private set; }

        /// <summary>Whether the held beam is in the on part of its cycle this frame.</summary>
        public bool BeamOn { get; private set; }

        /// <summary>Seconds until the megabomb can be used again.</summary>
        public float MegabombCooldown { get; private set; }

        /// <summary>A hit at low energy destroyed a weapon ("WEAPON DESTROYED").</summary>
        public event Action<StrikeItem> WeaponLost;

        /// <summary>The selected special changed (cycled, bought, collected or lost).</summary>
        public event Action SpecialChanged;

        /// <summary>A megabomb went off.</summary>
        public event Action MegabombFired;


        public StrikeGunnery(StrikeLoadout loadout, int seed = 7919)
        {
            Loadout = loadout;
            random = new System.Random(seed);
            lastSpecial = Special;
            if (Loadout != null)
            {
                Loadout.Changed += OnLoadoutChanged;
            }
        }


        /// <summary>Stops listening to the loadout (the ship flies another mission).</summary>
        internal void Detach()
        {
            if (Loadout != null)
            {
                Loadout.Changed -= OnLoadoutChanged;
            }
        }


        /// <summary>The beam kind of the pose for <paramref name="item"/>: 1 the deathray, 2 the twin laser, 0 anything else.</summary>
        public static int BeamKindOf(StrikeItem item)
        {
            return item == StrikeItem.Deathray || item == StrikeItem.TwinLaser ? (int)item - 11 : 0;
        }


        /// <summary>The beam weapon of a pose's beam kind (see <see cref="BeamKindOf"/>); MachineGun for none.</summary>
        public static StrikeItem BeamOfKind(int kind)
        {
            return kind == 1 ? StrikeItem.Deathray : kind == 2 ? StrikeItem.TwinLaser : StrikeItem.MachineGun;
        }


        /// <summary>
        /// Seconds between volleys of <paramref name="item"/> with a hull whose fire rate multiplier is
        /// <paramref name="fireRate"/>; 0 for what does not fire.
        /// </summary>
        public static float Interval(StrikeItem item, float fireRate)
        {
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            return info.Fires ? info.Rate / Mathf.Max(0.1f, fireRate) : 0f;
        }


        /// <summary>
        /// Whether a beam of <paramref name="item"/> held for <paramref name="heldTime"/> seconds is in the on part of its
        /// cycle: on for <see cref="StrikeWeaponInfo.BeamOn"/> of every interval (the first part of each).
        /// </summary>
        public static bool IsBeamOn(StrikeItem item, float heldTime, float fireRate)
        {
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            if (!info.Beam)
            {
                return false;
            }
            float interval = Interval(item, fireRate);
            float on = Mathf.Min(info.BeamOn, interval * 0.9f);
            return Mathf.Repeat(heldTime, interval) < on;
        }


        /// <summary>
        /// One frame of the weapons (<paramref name="deltaTime"/> seconds): the reloads and the megabomb cooldown run down,
        /// dropped dumbfire missiles ignite, and with <paramref name="fire"/> held the weapons fire; otherwise beams go out.
        /// </summary>
        public void Update(AsteroidsPlayer ship, float deltaTime, bool fire)
        {
            lastShip = ship;
            volleyThisFrame = false;
            MegabombCooldown = Mathf.Max(0f, MegabombCooldown - deltaTime);
            IgniteDumbfires(deltaTime);
            if (fire)
            {
                Fire(ship, deltaTime);
            }
            else
            {
                for (int i = 0; i < reloads.Length; i++)
                {
                    reloads[i] = Mathf.Max(0f, reloads[i] - deltaTime);
                }
                StopBeams(ship);
            }
            sinceVolley = volleyThisFrame ? 0f : sinceVolley + deltaTime;
        }


        /// <summary>One frame with the fire button held (<paramref name="deltaTime"/> seconds): every weapon whose rate allows fires.</summary>
        public void Fire(AsteroidsPlayer ship, float deltaTime)
        {
            if (ship == null || Loadout == null)
            {
                return;
            }
            lastShip = ship;
            float fireRate = ship.PlayerSettings != null ? ship.PlayerSettings.FireRateMultiplier : 1f;
            int shots = 0;
            foreach (StrikeItem item in StrikeWeaponRules.AlwaysOn)
            {
                if (Loadout.Count(item) > 0)
                {
                    shots += FireWeapon(ship, item, fireRate, deltaTime);
                }
            }
            StrikeItem special = Special;
            bool beam = false;
            if (StrikeWeaponRules.IsSpecial(special) && Loadout.Count(special) > 0)
            {
                if (StrikeWeaponRules.Info(special).Beam)
                {
                    beam = true;
                    FireBeam(ship, special, fireRate, deltaTime);
                }
                else
                {
                    shots += FireWeapon(ship, special, fireRate, deltaTime);
                }
            }
            if (!beam)
            {
                StopBeams(ship);
            }
            if (shots > 0)
            {
                volleyThisFrame = true;
                ship.NoteVolley(shots);
            }
        }


        /// <summary>Selects the next owned special (enum order, wrapping).</summary>
        public void Cycle()
        {
            if (Loadout == null)
            {
                return;
            }
            StrikeItem before = Special;
            Loadout.CycleSpecial();
            if (Special != before)
            {
                lastShip?.Field?.Sounds?.Click();
            }
            RaiseIfSpecialChanged();
        }


        /// <summary>Sets off a megabomb unless it cools down or none is left (a press during the cooldown spends nothing).</summary>
        public void Megabomb(AsteroidsPlayer ship)
        {
            if (ship == null || Loadout == null || MegabombCooldown > 0f)
            {
                return;
            }
            SpaceField field = ship.Field;
            if (field == null || Loadout.Megabombs <= 0 || !Loadout.UseMegabomb())
            {
                field?.Sounds?.Denied();
                return;
            }
            MegabombCooldown = StrikeRules.MegabombCooldown;
            // The strike form of the nova: every enemy shot on screen cleared, 50 to every targetable enemy and every boss
            // piece, no push. On a guest the simulator is told through the ship signal and deals the damage there.
            field.Nova(ship.Position, StrikeRules.MegabombDamage, StrikeRules.MegabombDamage, ship.HitSeat);
            field.Effects?.MegabombFlash();
            field.Sounds?.Megabomb();
            field.CameraRig?.Shake(1f);
            field.CameraRig?.Pulse(1f);
            MegabombFired?.Invoke();
        }


        /// <summary>Hides the beams and zaps and silences the beam (the ship was destroyed, or the mission ended).</summary>
        public void Stop(AsteroidsPlayer ship)
        {
            StopBeams(ship);
            dropping.Clear();
            sinceVolley = float.MaxValue;
            if (ship != null && ship.Beam != null)
            {
                ship.Beam.HideAll();
            }
        }


        /// <summary>Tells the listeners that a weapon was lost (the pilot's damage rules call it).</summary>
        internal void NotifyWeaponLost(StrikeItem item)
        {
            WeaponLost?.Invoke(item);
            RaiseIfSpecialChanged();
        }


        private void OnLoadoutChanged()
        {
            RaiseIfSpecialChanged();
        }


        /// <summary>Raises <see cref="SpecialChanged"/> once for every change of the selected special, whatever made it.</summary>
        private void RaiseIfSpecialChanged()
        {
            if (Special != lastSpecial)
            {
                lastSpecial = Special;
                SpecialChanged?.Invoke();
            }
        }


        // ------------------------------------------------------------------ projectiles and zaps

        /// <summary>Fires the volleys of <paramref name="item"/> its reload allows this frame; returns the projectiles fired.</summary>
        private int FireWeapon(AsteroidsPlayer ship, StrikeItem item, float fireRate, float deltaTime)
        {
            int index = (int)item;
            float interval = Interval(item, fireRate);
            if (interval <= 0f)
            {
                return 0;
            }
            reloads[index] -= deltaTime;
            int shots = 0;
            int volleys = 0;
            while (reloads[index] <= 0f && volleys < MaxVolleysPerFrame)
            {
                reloads[index] += interval;
                volleys++;
                shots += Volley(ship, item);
            }
            if (reloads[index] < 0f)
            {
                reloads[index] = 0f;
            }
            return shots;
        }


        /// <summary>One volley of <paramref name="item"/>; returns the projectiles fired (a zap counts one).</summary>
        private int Volley(AsteroidsPlayer ship, StrikeItem item)
        {
            SpaceField field = ship.Field;
            if (field == null)
            {
                return 0;
            }
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            if (info.Hitscan)
            {
                return Zap(ship, info);
            }
            SpawnService spawner = field.Spawner;
            if (spawner == null)
            {
                return 0;
            }
            Vector2 nose = Nose(ship);
            int fired = 0;
            for (int b = 0; b < info.Barrels; b++)
            {
                float side = (b - (info.Barrels - 1) * 0.5f) * info.BarrelSpacing;
                Vector2 origin = nose + new Vector2(side, item == StrikeItem.MachineGun ? -0.2f : -0.35f);
                Vector2 velocity = LaunchVelocity(field, info, origin, side, out Altitude reach);
                Shot shot = spawner.FireStrikeShot(info.ShotKind, origin, velocity, info.Damage, reach, ship);
                if (shot == null)
                {
                    continue;
                }
                fired++;
                if (info.Lifetime > 0f)
                {
                    shot.Lifetime = info.Lifetime;
                }
                if (item == StrikeItem.Dumbfire)
                {
                    float drift = ((float)random.NextDouble() * 2f - 1f) * 0.08f;
                    dropping.Add(new Dropping { Shot = shot, DropVelocity = velocity, Drift = drift, Time = 0f });
                }
            }
            if (fired > 0)
            {
                VolleyFeedback(ship, item, nose);
            }
            return fired;
        }


        /// <summary>The launch velocity of one projectile of <paramref name="info"/> fired from <paramref name="origin"/>.</summary>
        private Vector2 LaunchVelocity(SpaceField field, StrikeWeaponInfo info, Vector2 origin, float side, out Altitude reach)
        {
            reach = info.Mask;
            switch (info.Item)
            {
                case StrikeItem.Dumbfire:
                {
                    // Drops out sideways (away from the centre) and a little down; ignites after DumbfireDropTime.
                    float outward = side < 0f ? -1f : 1f;
                    float spread = 1f + (float)random.NextDouble() * 1.5f;
                    return new Vector2(outward * spread / DumbfireDropTime, -2f);
                }
                case StrikeItem.MiniGun:
                {
                    Shootable target = field.RandomTarget(info.Mask, (float)random.NextDouble());
                    if (target == null)
                    {
                        return Vector2.up * info.Speed;
                    }
                    Vector2 aim = target.Position + new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * target.Radius;
                    Vector2 direction = aim - origin;
                    return direction.sqrMagnitude > 0.0001f ? direction.normalized * info.Speed : Vector2.up * info.Speed;
                }
                case StrikeItem.Bombs:
                    // Nothing is hit while the bomb falls; it bursts on the ground when its lifetime runs out.
                    reach = Altitude.None;
                    return Vector2.up * info.Speed;
                default:
                    return Vector2.up * info.Speed;
            }
        }


        /// <summary>The laser turret: an instant hit on a random targetable air enemy; nothing without one.</summary>
        private int Zap(AsteroidsPlayer ship, StrikeWeaponInfo info)
        {
            SpaceField field = ship.Field;
            Shootable target = field.RandomTarget(info.Mask, (float)random.NextDouble());
            if (target == null)
            {
                return 0;
            }
            Vector2 from = ship.Position + new Vector2(0f, 0.3f);
            Vector2 to = target.Position;
            Vector2 direction = to - from;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            if (ship.Beam != null)
            {
                ship.Beam.ShowZap(from, to);
            }
            field.Sounds?.LaserZap();
            target.TakeHit(new DamageInfo(info.Damage, direction, to - direction * target.Radius * 0.5f, DamageSource.PlayerShot, true) { Seat = ship.HitSeat });
            return 1;
        }


        private static void VolleyFeedback(AsteroidsPlayer ship, StrikeItem item, Vector2 nose)
        {
            SpaceField field = ship.Field;
            ShipVisuals visuals = ship.visuals;
            switch (item)
            {
                case StrikeItem.MachineGun:
                    if (visuals != null)
                    {
                        visuals.MuzzleFlash(new Color(1f, 0.85f, 0.45f));
                    }
                    field.Sounds?.MachineGun();
                    break;
                case StrikeItem.MiniGun:
                    field.Sounds?.MachineGun();
                    break;
                case StrikeItem.PlasmaCannon:
                case StrikeItem.PulseCannon:
                    if (visuals != null)
                    {
                        visuals.MuzzleFlash(new Color(0.5f, 0.85f, 1f));
                    }
                    field.Effects?.Muzzle(nose);
                    break;
                case StrikeItem.Bombs:
                    field.Sounds?.BombDrop();
                    break;
                case StrikeItem.MicroMissiles:
                    break;
                default:
                    field.Sounds?.StrikeMissileLaunch();
                    break;
            }
        }


        /// <summary>Dumbfire missiles fall sideways for <see cref="DumbfireDropTime"/>, then ignite upward with a small drift.</summary>
        private void IgniteDumbfires(float deltaTime)
        {
            if (dropping.Count == 0)
            {
                return;
            }
            float launch = StrikeWeaponRules.Info(StrikeItem.Dumbfire).Speed;
            for (int i = dropping.Count - 1; i >= 0; i--)
            {
                Dropping missile = dropping[i];
                if (missile.Shot == null || !missile.Shot.InPlay)
                {
                    dropping.RemoveAt(i);
                    continue;
                }
                missile.Time += deltaTime;
                if (missile.Time < DumbfireDropTime)
                {
                    // Holds the drop course, slowing down (the shot would speed up along it otherwise).
                    missile.Shot.Velocity = missile.DropVelocity * (1f - missile.Time / DumbfireDropTime * 0.6f);
                    dropping[i] = missile;
                    continue;
                }
                missile.Shot.Velocity = new Vector2(missile.Drift, 1f).normalized * launch;
                dropping.RemoveAt(i);
            }
        }


        // ------------------------------------------------------------------ beams

        /// <summary>A beam weapon held: on for part of every interval, damaging the first enemy in each beam's column.</summary>
        private void FireBeam(AsteroidsPlayer ship, StrikeItem item, float fireRate, float deltaTime)
        {
            SpaceField field = ship.Field;
            StrikeWeaponInfo info = StrikeWeaponRules.Info(item);
            float interval = Interval(item, fireRate);
            bool wasOn = BeamOn;
            int kind = BeamKindOf(item);
            if (!BeamHeld || BeamKind != kind)
            {
                beamClock = 0f;
                beamTick = 0f;
                beamDamage[0] = beamDamage[1] = 0f;
                wasOn = false;
            }
            else
            {
                beamClock += deltaTime;
            }
            BeamHeld = true;
            BeamKind = kind;
            BeamOn = IsBeamOn(item, beamClock, fireRate);
            // Every cycle of the beam counts as a volley (it restarts the energy regeneration).
            if (BeamOn && !wasOn)
            {
                volleyThisFrame = true;
                ship.NoteVolley(info.Barrels);
            }
            volleyThisFrame |= BeamOn;
            PlayerBeam beam = ship.Beam;
            Vector2 position = ship.Position;
            Vector2 gun = GunPoint(ship);
            float top = field != null && field.Playground != null ? field.Playground.Top + 1f : position.y + 25f;
            bool dealt = false;
            if (BeamOn)
            {
                beamTick += deltaTime;
                dealt = beamTick >= StrikeRules.Tick;
            }
            for (int b = 0; b < 2; b++)
            {
                if (b >= info.Barrels)
                {
                    if (beam != null)
                    {
                        beam.ShowBeam(b, position, position, false);
                    }
                    continue;
                }
                Vector2 from = PlayerBeam.BeamOrigin(item, b, position, gun);
                Shootable target = field != null ? field.FirstInColumn(from.x, info.BeamHalfWidth, from.y, Altitude.Both) : null;
                Vector2 to = target != null ? new Vector2(from.x, Mathf.Max(from.y, target.Position.y - target.Radius * 0.5f)) : new Vector2(from.x, top);
                if (beam != null)
                {
                    beam.ShowBeam(b, from, to, BeamOn);
                }
                if (!BeamOn)
                {
                    continue;
                }
                // The damage is dealt once per tick of the original game (6 a tick for the deathray, 10 for each twin
                // laser), so a beam on a puppet sends a hit report a tick instead of one a frame.
                beamDamage[b] += info.Damage * deltaTime;
                if (dealt && target != null && beamDamage[b] > 0f)
                {
                    target.TakeHit(new DamageInfo(beamDamage[b], Vector2.up, to, DamageSource.PlayerShot, true) { Seat = ship.HitSeat });
                    field.Effects?.Spark(to, new Color(1f, 0.5f, 0.9f), 0.7f);
                }
                if (dealt)
                {
                    beamDamage[b] = 0f;
                }
            }
            if (dealt)
            {
                beamTick = 0f;
            }
            if (beam != null)
            {
                beam.Hum(field != null ? field.Sounds : null, true);
            }
        }


        private void StopBeams(AsteroidsPlayer ship)
        {
            if (!BeamHeld && !BeamOn)
            {
                return;
            }
            BeamHeld = false;
            BeamOn = false;
            BeamKind = 0;
            beamClock = 0f;
            beamTick = 0f;
            beamDamage[0] = beamDamage[1] = 0f;
            PlayerBeam beam = ship != null ? ship.Beam : null;
            if (beam != null)
            {
                beam.ShowBeam(0, Vector2.zero, Vector2.zero, false);
                beam.ShowBeam(1, Vector2.zero, Vector2.zero, false);
                beam.Hum(ship.Field != null ? ship.Field.Sounds : null, false);
            }
        }


        // ------------------------------------------------------------------ geometry

        private static Vector2 GunPoint(AsteroidsPlayer ship)
        {
            return ship.PlayerSettings != null ? ship.PlayerSettings.GunPoint : new Vector2(0f, 0.8f);
        }


        /// <summary>The ship's nose (the nose always points up in strike).</summary>
        private static Vector2 Nose(AsteroidsPlayer ship)
        {
            return ship.Position + new Vector2(0f, GunPoint(ship).y);
        }
    }
}
