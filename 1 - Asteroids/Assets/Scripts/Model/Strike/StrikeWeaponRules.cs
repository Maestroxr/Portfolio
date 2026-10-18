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
        /// <summary>The armory's weapons: the cone (degrees) its barrels fan over (the swarm rockets).</summary>
        public float Spread;
        /// <summary>How many targets one projectile passes through (the rail gun's slug: everything in its line).</summary>
        public int Pierce;
        /// <summary>Degrees per second its projectiles turn toward a target (the swarm rockets); 0 flies straight.</summary>
        public float Homing;
        /// <summary>How many more targets the lightning of a hit jumps to (the arc lightning), how far, and the damage of the first jump.</summary>
        public int Chains;
        public float ChainRange;
        public float ChainDamage;
        /// <summary>
        /// What its projectile bursts into (the flak shell's shards, the cluster canister's bomblets, the napalm canister's
        /// fire): how many, of which kind, and their damage (a bomblet's blast, the fire's burn per tick).
        /// </summary>
        public int Fragments;
        public StrikeShotKind FragmentKind;
        public float FragmentDamage;
        /// <summary>The flak shell bursts when an aircraft comes this close (m).</summary>
        public float FuseRadius;

        public bool Fires => Rate > 0f;
    }


    /// <summary>The firing table of the strike weapons (design section 1.2).</summary>
    public static class StrikeWeaponRules
    {
        /// <summary>Time in which accelerating projectiles reach their top speed; a tuning value, not from Raptor.</summary>
        public const float AccelerationTime = 0.5f;

        /// <summary>The first and last special weapons of the first block (3 to 13); the armory's follow in <see cref="Specials"/>.</summary>
        public const StrikeItem FirstSpecial = StrikeItem.Dumbfire;
        public const StrikeItem LastSpecial = StrikeItem.TwinLaser;

        /// <summary>The first and last of the armory's special weapons (18 to 23).</summary>
        public const StrikeItem FirstArmorySpecial = StrikeItem.FlakCannon;
        public const StrikeItem LastArmorySpecial = StrikeItem.RailGun;

        /// <summary>The always-on weapons in firing order.</summary>
        public static readonly StrikeItem[] AlwaysOn = { StrikeItem.MachineGun, StrikeItem.PlasmaCannon, StrikeItem.MicroMissiles };

        /// <summary>Every special weapon in cycling order: the first block in enum order, then the armory's.</summary>
        public static readonly StrikeItem[] Specials = BuildSpecials();

        public static bool IsWeapon(StrikeItem item)
        {
            return item <= StrikeItem.TwinLaser || IsSpecial(item);
        }

        public static bool IsSpecial(StrikeItem item)
        {
            return item >= FirstSpecial && item <= LastSpecial && item != StrikeItem.PowerDisrupter
                || item >= FirstArmorySpecial && item <= LastArmorySpecial;
        }

        private static StrikeItem[] BuildSpecials()
        {
            var specials = new System.Collections.Generic.List<StrikeItem>();
            for (var item = FirstSpecial; item <= LastArmorySpecial; item++)
            {
                if (IsSpecial(item))
                {
                    specials.Add(item);
                }
            }
            return specials.ToArray();
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
                case StrikeItem.FlakCannon:
                {
                    // Shells that burst into six shards when an aircraft comes within 1.8 m, or at the end of their climb.
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Air, 0.65f, 3f, 22f, 22f, 2, 0.9f, StrikeShotKind.FlakShell, 0.85f);
                    info.ShotRadius = 0.25f;
                    info.Fragments = 6;
                    info.FragmentKind = StrikeShotKind.FlakShard;
                    info.FragmentDamage = 2f;
                    info.FuseRadius = 1.8f;
                    return info;
                }
                case StrikeItem.ClusterBombs:
                {
                    // Climbs like a bomb, then scatters six bomblets that burst on the ground around it (the burst is a bomblet's).
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Ground, 1.5f, 0f, 6f, 6f, 1, 0f, StrikeShotKind.ClusterShell, 0.85f);
                    info.Fragments = 6;
                    info.FragmentKind = StrikeShotKind.Bomblet;
                    info.FragmentDamage = 22f;
                    info.BurstRadius = 1.7f;
                    info.BurstDamage = 22f;
                    return info;
                }
                case StrikeItem.Napalm:
                {
                    // Climbs like a bomb, then leaves a patch of ground (the burst radius) burning: 8 a tick to every ground unit in it.
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Ground, 1.25f, 0f, 7f, 7f, 1, 0f, StrikeShotKind.NapalmCanister, 0.7f);
                    info.Fragments = 1;
                    info.FragmentKind = StrikeShotKind.NapalmFire;
                    info.FragmentDamage = 8f;
                    info.BurstRadius = 2f;
                    return info;
                }
                case StrikeItem.SwarmRockets:
                {
                    // Six small rockets fanned over 70 degrees that find their own targets.
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Both, 0.9f, 3.5f, 12f, 26f, 6, 0.42f, StrikeShotKind.SwarmRocket, 1.6f);
                    info.Spread = 70f;
                    info.Homing = 260f;
                    info.Smoke = true;
                    return info;
                }
                case StrikeItem.ArcLightning:
                {
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Both, 0.32f, 6f, 40f, 40f, 1, 0f, StrikeShotKind.ArcBolt, 0.6f);
                    info.ShotRadius = 0.3f;
                    info.Chains = 4;
                    info.ChainRange = 6f;
                    info.ChainDamage = 4.5f;
                    return info;
                }
                case StrikeItem.RailGun:
                {
                    // A slug that goes through everything in its line, the parts of a boss included.
                    StrikeWeaponInfo info = Gun(item, ItemGroup.Special, Altitude.Both, 0.75f, 55f, 80f, 80f, 1, 0f, StrikeShotKind.RailSlug, 0.35f);
                    info.ShotRadius = 0.35f;
                    info.Pierce = 99;
                    return info;
                }
                case StrikeItem.MegaBomb:
                    return new StrikeWeaponInfo { Item = item, Group = ItemGroup.Consumable, Mask = Altitude.Both };
                case StrikeItem.EnergyModule:
                case StrikeItem.PhaseShield:
                case StrikeItem.RepairKit:
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
                ShotRadius = 0.15f,
                Pierce = 1
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
