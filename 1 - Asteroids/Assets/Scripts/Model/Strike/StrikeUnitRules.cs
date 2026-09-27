using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>The numbers of one strike unit at toughness 1 (design section 1.4); the prefab builders copy them.</summary>
    public struct StrikeUnitInfo
    {
        public StrikeUnit Unit;
        /// <summary>Air units can be rammed and cast shadows; ground units are flown over.</summary>
        public Altitude Layer;
        public float Health;
        public float Radius;
        /// <summary>Money for destroying it (its score).</summary>
        public int Bounty;
        /// <summary>Flight speed of an air unit (m/s); ground units drive at their event's speed and have 0 here.</summary>
        public float Speed;
        public AttackPattern Pattern;
        /// <summary>A second pattern used on every other volley (the bomber's rockets); None for one pattern.</summary>
        public AttackPattern AlternatePattern;
        /// <summary>Volleys per attack.</summary>
        public int Burst;
        /// <summary>Seconds between the volleys of a burst.</summary>
        public float BurstGap;
        /// <summary>Seconds between attacks.</summary>
        public float Interval;
        /// <summary>Projectiles per volley: barrels firing together, or the shots of a spread.</summary>
        public int Shots;
        public EnemyShotKind ShotKind;
        /// <summary>The kind of the alternate pattern's shots.</summary>
        public EnemyShotKind AlternateShotKind;
        /// <summary>Seconds of warning before it attacks or arrives (the laser tower's glow, the interceptor's warning).</summary>
        public float Telegraph;
        /// <summary>A turret that aims at the nearest pilot.</summary>
        public bool AimsTurret;
        /// <summary>Explodes when destroyed and damages the ground units around it.</summary>
        public bool Explodes;
        public float BlastRadius;
        public float BlastDamage;
        /// <summary>Always drops its event's pickup (the transport and the depot).</summary>
        public bool AlwaysDrops;
        /// <summary>Size of its explosion effect.</summary>
        public float ExplosionScale;
        /// <summary>Size of the crater or rubble a ground unit leaves (0 for air units).</summary>
        public float CraterSize;

        public bool IsAir => Layer == Altitude.Air;
    }


    /// <summary>What a projectile of an enemy shot kind does (a prefab value: guests only know the kind).</summary>
    public struct EnemyShotInfo
    {
        public EnemyShotKind Kind;
        public float Damage;
        /// <summary>Launch speed (m/s).</summary>
        public float Speed;
        /// <summary>Top speed after accelerating (m/s); equal to <see cref="Speed"/> for shots that do not accelerate.</summary>
        public float MaxSpeed;
        /// <summary>Acceleration up to <see cref="MaxSpeed"/> (m/s^2).</summary>
        public float Acceleration;
        /// <summary>Seconds the shot flies (0: until it leaves the screen).</summary>
        public float Lifetime;
        /// <summary>Side-to-side wobble (m), 0 for none.</summary>
        public float Wobble;
        /// <summary>Leaves a smoke trail.</summary>
        public bool Smoke;
    }


    /// <summary>
    /// The table of the strike units (design section 1.4) and of the enemy shot kinds. Enemy shots deal one more than
    /// Raptor's on purpose (our hit circle is about four times smaller than its 32 px box).
    /// </summary>
    public static class StrikeUnitRules
    {
        private static StrikeUnitInfo[] table;

        /// <summary>Every unit, air first, in enum order.</summary>
        public static StrikeUnit[] All =>
            new[]
            {
                StrikeUnit.Dart, StrikeUnit.Hornet, StrikeUnit.Bomber, StrikeUnit.Gunship, StrikeUnit.Interceptor, StrikeUnit.Kamikaze,
                StrikeUnit.Transport, StrikeUnit.Turret, StrikeUnit.Flak, StrikeUnit.Tank, StrikeUnit.Truck, StrikeUnit.Bunker,
                StrikeUnit.FuelTank, StrikeUnit.Radar, StrikeUnit.Gunboat, StrikeUnit.Hut, StrikeUnit.LaserTower, StrikeUnit.Depot,
                StrikeUnit.Crate
            };

        /// <summary>Number of air units (their pool indices are 0 to AirCount - 1).</summary>
        public const int AirCount = 7;

        /// <summary>Number of ground units (their pool indices are unit - 16, 0 to GroundCount - 1).</summary>
        public const int GroundCount = 12;

        /// <summary>The value of the first ground unit.</summary>
        public const int FirstGround = 16;

        public static bool IsAir(StrikeUnit unit)
        {
            return (int)unit < FirstGround;
        }

        public static bool IsGround(StrikeUnit unit)
        {
            return (int)unit >= FirstGround;
        }

        /// <summary>Index of the unit's pool: the value for air units, value - 16 for ground units.</summary>
        public static int PoolIndex(StrikeUnit unit)
        {
            return IsAir(unit) ? (int)unit : (int)unit - FirstGround;
        }

        /// <summary>The numbers of <paramref name="unit"/>.</summary>
        public static StrikeUnitInfo Info(StrikeUnit unit)
        {
            table ??= Build();
            foreach (StrikeUnitInfo info in table)
            {
                if (info.Unit == unit)
                {
                    return info;
                }
            }
            return new StrikeUnitInfo { Unit = unit, Layer = Altitude.Ground, Health = 1f, Radius = 1f, Burst = 1, Shots = 1, ExplosionScale = 1f };
        }

        public static string Title(StrikeUnit unit)
        {
            switch (unit)
            {
                case StrikeUnit.FuelTank: return "Fuel Tank";
                case StrikeUnit.LaserTower: return "Laser Tower";
                default: return unit.ToString();
            }
        }

        /// <summary>The numbers of an enemy shot kind (the field kinds keep their prefab values; listed for completeness).</summary>
        public static EnemyShotInfo Shot(EnemyShotKind kind)
        {
            switch (kind)
            {
                case EnemyShotKind.Plasma:
                    return new EnemyShotInfo { Kind = kind, Damage = 16f, Speed = 23f, MaxSpeed = 23f };
                case EnemyShotKind.Flak:
                    return new EnemyShotInfo { Kind = kind, Damage = 2f, Speed = 2f, MaxSpeed = 14f, Acceleration = 20f };
                case EnemyShotKind.Bolt:
                    return new EnemyShotInfo { Kind = kind, Damage = 3f, Speed = 7f, MaxSpeed = 14f, Acceleration = 12f };
                case EnemyShotKind.Rocket:
                    return new EnemyShotInfo { Kind = kind, Damage = 5f, Speed = 5f, MaxSpeed = 23f, Acceleration = 22f, Smoke = true };
                case EnemyShotKind.Beam:
                    return new EnemyShotInfo { Kind = kind, Damage = 40f, Lifetime = 0.17f };
                case EnemyShotKind.SkyMine:
                    return new EnemyShotInfo { Kind = kind, Damage = 16f, Speed = 2.3f, MaxSpeed = 2.3f, Lifetime = 6.4f, Wobble = 0.6f };
                default:
                    return new EnemyShotInfo { Kind = kind, Damage = 10f, Speed = 9f, MaxSpeed = 9f };
            }
        }

        private static StrikeUnitInfo[] Build()
        {
            return new[]
            {
                Air(StrikeUnit.Dart, 4f, 0.9f, 150, 11f, AttackPattern.Aimed, 1, 0f, 1.8f, 1, EnemyShotKind.Flak),
                Air(StrikeUnit.Hornet, 8f, 1f, 300, 9f, AttackPattern.Down, 1, 0f, 1.2f, 2, EnemyShotKind.Bolt),
                With(Air(StrikeUnit.Bomber, 60f, 2.2f, 1200, 5f, AttackPattern.Angled, 1, 0f, 1.6f, 2, EnemyShotKind.Bolt),
                    info => { info.AlternatePattern = AttackPattern.Rockets; info.AlternateShotKind = EnemyShotKind.Rocket; return info; }),
                Air(StrikeUnit.Gunship, 40f, 1.6f, 900, 6f, AttackPattern.AimedBurst, 3, 0.15f, 2.2f, 1, EnemyShotKind.Flak),
                With(Air(StrikeUnit.Interceptor, 10f, 1f, 400, 16f, AttackPattern.Down, 1, 0f, 0.8f, 2, EnemyShotKind.Bolt),
                    info => { info.Telegraph = 1f; return info; }),
                Air(StrikeUnit.Kamikaze, 3f, 0.8f, 100, 12f, AttackPattern.None, 0, 0f, 0f, 0, EnemyShotKind.Flak),
                With(Air(StrikeUnit.Transport, 220f, 3.2f, 2500, 3.5f, AttackPattern.Spread, 1, 0f, 2.5f, 5, EnemyShotKind.Bolt),
                    info => { info.AlwaysDrops = true; return info; }),

                With(Ground(StrikeUnit.Turret, 15f, 1.1f, 500, AttackPattern.Aimed, 1, 0f, 1.6f, 1, EnemyShotKind.Flak),
                    info => { info.AimsTurret = true; return info; }),
                Ground(StrikeUnit.Flak, 25f, 1.3f, 750, AttackPattern.Rockets, 2, 0.25f, 2.4f, 1, EnemyShotKind.Rocket),
                With(Ground(StrikeUnit.Tank, 20f, 1.2f, 600, AttackPattern.Aimed, 1, 0f, 2f, 1, EnemyShotKind.Flak),
                    info => { info.AimsTurret = true; return info; }),
                Ground(StrikeUnit.Truck, 5f, 0.9f, 250, AttackPattern.None, 0, 0f, 0f, 0, EnemyShotKind.Flak),
                Ground(StrikeUnit.Bunker, 60f, 1.8f, 1500, AttackPattern.Spread, 1, 0f, 2.2f, 3, EnemyShotKind.Bolt),
                With(Ground(StrikeUnit.FuelTank, 10f, 1.2f, 400, AttackPattern.None, 0, 0f, 0f, 0, EnemyShotKind.Flak),
                    info => { info.Explodes = true; info.BlastRadius = 3.5f; info.BlastDamage = 12f; return info; }),
                Ground(StrikeUnit.Radar, 20f, 1.3f, 1000, AttackPattern.None, 0, 0f, 0f, 0, EnemyShotKind.Flak),
                Ground(StrikeUnit.Gunboat, 25f, 1.4f, 800, AttackPattern.Aimed, 1, 0f, 1.8f, 2, EnemyShotKind.Flak),
                Ground(StrikeUnit.Hut, 8f, 1.2f, 200, AttackPattern.None, 0, 0f, 0f, 0, EnemyShotKind.Flak),
                With(Ground(StrikeUnit.LaserTower, 50f, 1.4f, 1800, AttackPattern.Laser, 1, 0f, 3f, 1, EnemyShotKind.Beam),
                    info => { info.Telegraph = 0.6f; return info; }),
                With(Ground(StrikeUnit.Depot, 80f, 2.6f, 2000, AttackPattern.None, 0, 0f, 0f, 0, EnemyShotKind.Flak),
                    info => { info.Explodes = true; info.BlastRadius = 4.5f; info.BlastDamage = 20f; info.AlwaysDrops = true; return info; }),
                Ground(StrikeUnit.Crate, 3f, 0.8f, 50, AttackPattern.None, 0, 0f, 0f, 0, EnemyShotKind.Flak)
            };
        }

        private static StrikeUnitInfo Air(StrikeUnit unit, float health, float radius, int bounty, float speed, AttackPattern pattern,
            int burst, float burstGap, float interval, int shots, EnemyShotKind kind)
        {
            return new StrikeUnitInfo
            {
                Unit = unit,
                Layer = Altitude.Air,
                Health = health,
                Radius = radius,
                Bounty = bounty,
                Speed = speed,
                Pattern = pattern,
                Burst = burst,
                BurstGap = burstGap,
                Interval = interval,
                Shots = shots,
                ShotKind = kind,
                AlternateShotKind = kind,
                ExplosionScale = Mathf.Max(0.8f, radius * 1.2f)
            };
        }

        private static StrikeUnitInfo Ground(StrikeUnit unit, float health, float radius, int bounty, AttackPattern pattern,
            int burst, float burstGap, float interval, int shots, EnemyShotKind kind)
        {
            StrikeUnitInfo info = Air(unit, health, radius, bounty, 0f, pattern, burst, burstGap, interval, shots, kind);
            info.Layer = Altitude.Ground;
            info.CraterSize = radius * 1.5f;
            return info;
        }

        private static StrikeUnitInfo With(StrikeUnitInfo info, System.Func<StrikeUnitInfo, StrikeUnitInfo> change)
        {
            return change(info);
        }
    }
}
