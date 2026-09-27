namespace Portfolio.Asteroids
{
    /// <summary>How one strike weapon fires (design section 1.2). Consumables and equipment have a group and a mask only.</summary>
    public struct StrikeWeaponInfo
    {
        public StrikeItem Item;
        public ItemGroup Group;
        /// <summary>The layers its hits reach.</summary>
        public Altitude Mask;
        /// <summary>Seconds between volleys (divided by the hull's fire rate multiplier); 0 for what does not fire.</summary>
        public float Rate;
        /// <summary>Damage per projectile or per zap; for beams the damage per second while on.</summary>
        public float Damage;
        /// <summary>Launch speed of its projectiles (m/s).</summary>
        public float Speed;
        /// <summary>Top speed of its projectiles (m/s); equal to <see cref="Speed"/> when they do not accelerate.</summary>
        public float MaxSpeed;
        /// <summary>Acceleration of its projectiles up to <see cref="MaxSpeed"/> (m/s^2).</summary>
        public float Acceleration;
        /// <summary>Projectiles (or beams) per volley.</summary>
        public int Barrels;
        /// <summary>Side distance between the barrels (m).</summary>
        public float BarrelSpacing;
        /// <summary>The pool of its projectiles (unused for beams and the laser turret).</summary>
        public StrikeShotKind ShotKind;
        /// <summary>Seconds its projectiles fly.</summary>
        public float Lifetime;
        /// <summary>Hit radius of its projectiles (the pulse cannon's wide shockwave).</summary>
        public float ShotRadius;
        /// <summary>A beam from the ship to the first enemy in its column.</summary>
        public bool Beam;
        /// <summary>Seconds a beam is on in every <see cref="Rate"/>.</summary>
        public float BeamOn;
        /// <summary>Half the width of a beam's column (m).</summary>
        public float BeamHalfWidth;
        /// <summary>Hits a random targetable enemy without a projectile (the laser turret).</summary>
        public bool Hitscan;
        /// <summary>Aims at a random targetable enemy (the mini-gun).</summary>
        public bool AutoTrack;
        /// <summary>A blast when the projectile expires (bombs), with its radius and damage (layers: ground).</summary>
        public float BurstRadius;
        public float BurstDamage;
        /// <summary>Its projectiles leave smoke.</summary>
        public bool Smoke;

        public bool Fires => Rate > 0f;
    }


    /// <summary>The firing table of the strike weapons (design section 1.2).</summary>
    public static class StrikeWeaponRules
    {
        /// <summary>Time in which accelerating projectiles reach their top speed; a tuning value, not from Raptor.</summary>
        public const float AccelerationTime = 0.5f;

        /// <summary>The first and last special weapons, in cycling order.</summary>
        public const StrikeItem FirstSpecial = StrikeItem.Dumbfire;
        public const StrikeItem LastSpecial = StrikeItem.TwinLaser;

        /// <summary>The always-on weapons in firing order.</summary>
        public static readonly StrikeItem[] AlwaysOn = { StrikeItem.MachineGun, StrikeItem.PlasmaCannon, StrikeItem.MicroMissiles };

        public static bool IsWeapon(StrikeItem item)
        {
            return item <= StrikeItem.TwinLaser;
        }

        public static bool IsSpecial(StrikeItem item)
        {
            return item >= FirstSpecial && item <= LastSpecial && item != StrikeItem.PowerDisrupter;
        }

        public static ItemGroup Group(StrikeItem item)
        {
            return Info(item).Group;
        }

        public static Altitude Mask(StrikeItem item)
        {
            return Info(item).Mask;
        }

        /// <summary>The firing numbers of <paramref name="item"/>.</summary>
        public static StrikeWeaponInfo Info(StrikeItem item)
        {
            switch (item)
            {
                case StrikeItem.MachineGun:
                    return Gun(item, ItemGroup.AlwaysOn, Altitude.Both, 0.086f, 1f, 30f, 30f, 2, 1.1f, StrikeShotKind.Bullet, 0.9f);
                case StrikeItem.PlasmaCannon:
                    return Gun(item, ItemGroup.AlwaysOn, Altitude.Air, 0.43f, 2f, 9f, 19f, 1, 0f, StrikeShotKind.PlasmaBolt, 1.6f);
                case StrikeItem.MicroMissiles:
                    return Gun(item, ItemGroup.AlwaysOn, Altitude.Both, 0.17f, 2f, 5f, 19f, 2, 0.7f, StrikeShotKind.MicroMissile, 1.6f);
                case StrikeItem.Dumbfire:
                    return Gun(item, ItemGroup.Special, Altitude.Both, 0.43f, 4f, 5f, 28f, 2, 1.4f, StrikeShotKind.Dumbfire, 1.8f);
                case StrikeItem.MiniGun:
                {
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Both, 0.043f, 1f, 24f, 24f, 1, 0f, StrikeShotKind.MiniGunRound, 1.2f);
                    info.AutoTrack = true;
                    return info;
                }
                case StrikeItem.LaserTurret:
                {
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Air, 0.26f, 5f, 0f, 0f, 1, 0f, StrikeShotKind.Bullet, 0f);
                    info.Hitscan = true;
                    return info;
                }
                case StrikeItem.MissilePods:
                {
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Air, 0.21f, 4f, 2f, 37f, 2, 1f, StrikeShotKind.PodMissile, 1.4f);
                    info.Smoke = true;
                    return info;
                }
                case StrikeItem.AirMissiles:
                {
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Air, 0.43f, 4f, 2f, 28f, 2, 1.2f, StrikeShotKind.AirMissile, 1.6f);
                    info.Smoke = true;
                    return info;
                }
                case StrikeItem.GroundMissiles:
                    return Gun(item, ItemGroup.Special, Altitude.Ground, 0.86f, 20f, 2f, 14f, 2, 1.2f, StrikeShotKind.GroundMissile, 2.4f);
                case StrikeItem.Bombs:
                {
                    // Travels up at 6 m/s for 0.8 s without hitting anything, then bursts on the ground.
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Ground, 1.29f, 0f, 6f, 6f, 1, 0f, StrikeShotKind.Bomb, 0.8f);
                    info.BurstRadius = 2.2f;
                    info.BurstDamage = 50f;
                    return info;
                }
                case StrikeItem.PowerDisrupter:
                    return new StrikeWeaponInfo { Item = item, Group = ItemGroup.Special, Mask = Altitude.None, ShotKind = StrikeShotKind.DisrupterOrb };
                case StrikeItem.PulseCannon:
                {
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Both, 0.13f, 5f, 19f, 19f, 1, 0f, StrikeShotKind.Pulse, 1.3f);
                    info.ShotRadius = 0.9f;
                    return info;
                }
                case StrikeItem.Deathray:
                    return Ray(item, 140f, 1, 0f);
                case StrikeItem.TwinLaser:
                    return Ray(item, 233f, 2, 1.6f);
                case StrikeItem.MegaBomb:
                    return new StrikeWeaponInfo { Item = item, Group = ItemGroup.Consumable, Mask = Altitude.Both };
                case StrikeItem.EnergyModule:
                case StrikeItem.PhaseShield:
                    return new StrikeWeaponInfo { Item = item, Group = ItemGroup.Consumable, Mask = Altitude.None };
                default:
                    return new StrikeWeaponInfo { Item = item, Group = ItemGroup.Equipment, Mask = Altitude.None };
            }
        }

        private static StrikeWeaponInfo Gun(StrikeItem item, ItemGroup group, Altitude mask, float rate, float damage, float speed, float maxSpeed,
            int barrels, float spacing, StrikeShotKind kind, float lifetime)
        {
            return new StrikeWeaponInfo
            {
                Item = item,
                Group = group,
                Mask = mask,
                Rate = rate,
                Damage = damage,
                Speed = speed,
                MaxSpeed = maxSpeed,
                Acceleration = maxSpeed > speed ? (maxSpeed - speed) / AccelerationTime : 0f,
                Barrels = barrels,
                BarrelSpacing = spacing,
                ShotKind = kind,
                Lifetime = lifetime,
                ShotRadius = 0.15f
            };
        }

        private static StrikeWeaponInfo Ray(StrikeItem item, float damagePerSecond, int beams, float spacing)
        {
            return new StrikeWeaponInfo
            {
                Item = item,
                Group = ItemGroup.Special,
                Mask = Altitude.Both,
                Rate = 0.3f,
                Damage = damagePerSecond,
                Barrels = beams,
                BarrelSpacing = spacing,
                Beam = true,
                BeamOn = 0.17f,
                BeamHalfWidth = 0.35f
            };
        }
    }
}
