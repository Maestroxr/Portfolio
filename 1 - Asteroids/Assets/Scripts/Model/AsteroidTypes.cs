using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>What an asteroid is made of, which decides how it breaks.</summary>
    public enum AsteroidKind
    {
        /// <summary>Plain rock: splits in two.</summary>
        Rock = 0,
        /// <summary>Metal-rich rock: tougher, drops crystals.</summary>
        Ore = 1,
        /// <summary>Molten rock: explodes and sets off everything around it.</summary>
        Magma = 2,
        /// <summary>Ice: shatters into a spray of fast shards.</summary>
        Ice = 3,
        /// <summary>Void crystal: releases shards that hunt the ship.</summary>
        Crystal = 4,
        /// <summary>Armoured iron: shots do half damage, blasts crack it.</summary>
        Metal = 5,
        /// <summary>Living rock: splits in three, and its small fragments grow back when left alone.</summary>
        Hydra = 6,
        /// <summary>Wreckage of dead ships: holds salvage; a wreck with a fuel tank explodes.</summary>
        Wreck = 7
    }


    public enum AsteroidSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }


    public enum WeaponType
    {
        Blaster = 0,
        Laser = 1,
        Scatter = 2,
        Missiles = 3,
        /// <summary>The Arc Caster: a spark that chains lightning to the targets around the one it hits.</summary>
        Arc = 4,
        /// <summary>The Rail Driver: a slow, very fast slug that goes through everything in its line.</summary>
        Rail = 5,
        /// <summary>The Flak Burster: shells that burst into shrapnel near a target or at the end of their range.</summary>
        Flak = 6,
        /// <summary>The Ion Wave: a slow crescent that grows as it flies and passes through what it hits.</summary>
        Wave = 7
    }


    /// <summary>Timed power-ups.</summary>
    public enum PowerUpType
    {
        /// <summary>Doubles the fire rate.</summary>
        Overdrive = 0,
        /// <summary>Pulls crystals and pickups to the ship.</summary>
        Magnet = 1,
        /// <summary>Slows everything but the ship.</summary>
        Chrono = 2,
        /// <summary>Two drones orbit the ship and fire at the nearest target.</summary>
        Drones = 3,
        /// <summary>Phase Rounds: every projectile passes through two more targets.</summary>
        Piercing = 4,
        /// <summary>Aegis Barrier: the shield takes every hit and the hull none.</summary>
        Aegis = 5
    }


    /// <summary>What a mission asks for.</summary>
    public enum LevelObjective
    {
        /// <summary>Destroy every asteroid of every wave.</summary>
        ClearWaves = 0,
        /// <summary>Stay alive until the timer runs out.</summary>
        Survive = 1,
        /// <summary>Collect a number of crystals.</summary>
        Collect = 2,
        /// <summary>Destroy the sector's boss.</summary>
        Boss = 3,
        /// <summary>Waves without end; the score and the wave reached are the result.</summary>
        Endless = 4,
        /// <summary>Bring a number of freighters across the sector, one after the other; losing one fails the mission.</summary>
        Escort = 5,
        /// <summary>Keep a station standing until the timer runs out.</summary>
        Defend = 6,
        /// <summary>Destroy a number of marked enemies.</summary>
        Hunt = 7
    }


    /// <summary>Everything a wave can throw at the ship besides asteroids.</summary>
    public enum HazardKind
    {
        Mine = 0,
        ClusterBomb = 1,
        Comet = 2,
        GravityWell = 3,
        Saucer = 4,
        Wasp = 5,
        SupplyPod = 6,
        /// <summary>A wall of fire from the dying star sweeps across the sector after a warning.</summary>
        SolarFlare = 7,
        /// <summary>A drifting ion cloud: it slows the ship, drains its shield and calls lightning.</summary>
        IonStorm = 8,
        /// <summary>An armoured gun platform.</summary>
        Sentry = 9,
        /// <summary>A heavy ship that crosses the sector sowing mines.</summary>
        Minelayer = 10,
        /// <summary>An enemy that blinks out and in around its prey.</summary>
        Stalker = 11,
        /// <summary>A raider fighter that goes for freighters and stations first.</summary>
        Corsair = 12
    }


    /// <summary>The projectiles enemies, bombs and crystals fire at the ship.</summary>
    public enum EnemyShotKind
    {
        Plasma = 0,
        Shrapnel = 1,
        Shard = 2,
        Missile = 3,
        Acid = 4,
        /// <summary>Strike: small aimed shots that speed up.</summary>
        Flak = 5,
        /// <summary>Strike: straight bolts that speed up.</summary>
        Bolt = 6,
        /// <summary>Strike: rockets with a smoke trail.</summary>
        Rocket = 7,
        /// <summary>Strike: a laser tower's beam (<see cref="EnemyBeam"/>).</summary>
        Beam = 8,
        /// <summary>Strike: a mine that drifts down with a wobble (a shot: not shootable, the megabomb clears it).</summary>
        SkyMine = 9,
        /// <summary>A lightning strike: a ring warns where it lands, then it burns what stands there (<see cref="ZoneShot"/>).</summary>
        Lightning = 10,
        /// <summary>A straight laser that warns with a thin line, then burns along it (<see cref="FieldBeam"/>).</summary>
        Lance = 11,
        /// <summary>A laser that warns, then sweeps an arc as it burns (<see cref="FieldBeam"/>).</summary>
        SweepBeam = 12,
        /// <summary>A slow orb that bursts into a ring of plasma when its time runs out (<see cref="NovaOrb"/>).</summary>
        NovaOrb = 13,
        /// <summary>Strike: an artillery shell that bursts where it was aimed, its landing spot marked (<see cref="EnemyShell"/>).</summary>
        Shell = 14,
        /// <summary>Strike: a slow spore of the Hive that drifts after the nearest pilot for a while.</summary>
        Spore = 15,
        /// <summary>Strike: a thin crystal needle that speeds up.</summary>
        Needle = 16,
        /// <summary>Strike: a rocket that turns after the nearest pilot for a while.</summary>
        Seeker = 17
    }


    /// <summary>The two kinds of mission of the campaign (serialized in the sectors): append only.</summary>
    public enum MissionMode
    {
        /// <summary>The asteroid field: a wrapping playfield, waves, lives.</summary>
        Field = 0,
        /// <summary>The planet strike: a vertical scroller over the ground with a boss at the end.</summary>
        Strike = 1
    }


    /// <summary>What caused a hit, which decides who gets the points.</summary>
    public enum DamageSource
    {
        PlayerShot,
        Explosion,
        Collision,
        Comet,
        Nova,
        Hazard,
        Enemy
    }


    /// <summary>One hit on a <see cref="Shootable"/> or on the ship.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public Vector2 Direction;
        public Vector2 Point;
        public DamageSource Source;
        public Shot Shot;
        /// <summary>Whether the kill counts for the player (their shots, their nova, chains they set off).</summary>
        public bool ByPlayer;
        /// <summary>
        /// In a shared mission: the seat of the pilot on another device the hit counts for; in local co-op the seat of the
        /// wingman whose hit it is (<see cref="AsteroidsPlayer.HitSeat"/>); null when it counts for the player's own ship.
        /// </summary>
        public int? Seat;

        public DamageInfo(float amount, Vector2 direction, Vector2 point, DamageSource source, bool byPlayer, Shot shot = null)
        {
            Amount = amount;
            Direction = direction;
            Point = point;
            Source = source;
            ByPlayer = byPlayer;
            Shot = shot;
            Seat = null;
        }
    }


    /// <summary>The numbers behind every kind and size of asteroid.</summary>
    public static partial class AsteroidRules
    {
        public static float Radius(AsteroidKind kind, AsteroidSize size)
        {
            float radius = size == AsteroidSize.Large ? 1.55f : size == AsteroidSize.Medium ? 0.95f : 0.55f;
            switch (kind)
            {
                case AsteroidKind.Ice: return radius * 1.05f;
                case AsteroidKind.Crystal: return radius * 1.1f;
                case AsteroidKind.Hydra: return radius * 1.05f;
                default: return radius;
            }
        }

        public static float Health(AsteroidKind kind, AsteroidSize size)
        {
            int index = (int)size;
            switch (kind)
            {
                case AsteroidKind.Ore: return new[] { 2f, 4f, 7f }[index];
                case AsteroidKind.Ice: return new[] { 1f, 2f, 3f }[index];
                case AsteroidKind.Crystal: return new[] { 2f, 3f, 6f }[index];
                case AsteroidKind.Metal: return new[] { 3f, 6f, 10f }[index];
                case AsteroidKind.Hydra: return new[] { 1f, 2f, 3f }[index];
                case AsteroidKind.Wreck: return new[] { 2f, 3f, 5f }[index];
                default: return new[] { 1f, 2f, 4f }[index];
            }
        }

        /// <summary>Points for destroying one; the small ones are the hardest to hit and are worth the most.</summary>
        public static int Score(AsteroidKind kind, AsteroidSize size)
        {
            int points = size == AsteroidSize.Small ? 100 : size == AsteroidSize.Medium ? 50 : 20;
            float factor;
            switch (kind)
            {
                case AsteroidKind.Ore: factor = 1.5f; break;
                case AsteroidKind.Magma: factor = 1.5f; break;
                case AsteroidKind.Ice: factor = 1.2f; break;
                case AsteroidKind.Crystal: factor = 2f; break;
                case AsteroidKind.Metal: factor = 2f; break;
                case AsteroidKind.Hydra: factor = 1.4f; break;
                case AsteroidKind.Wreck: factor = 1.3f; break;
                default: factor = 1f; break;
            }
            return Mathf.RoundToInt(points * factor / 5f) * 5;
        }

        /// <summary>Drift speed range in meters per second before the level's speed multiplier.</summary>
        public static Vector2 SpeedRange(AsteroidSize size)
        {
            switch (size)
            {
                case AsteroidSize.Large: return new Vector2(0.9f, 1.7f);
                case AsteroidSize.Medium: return new Vector2(1.6f, 2.6f);
                default: return new Vector2(2.4f, 3.6f);
            }
        }

        /// <summary>Hull damage the ship takes when it flies into one.</summary>
        public static float ContactDamage(AsteroidKind kind, AsteroidSize size)
        {
            float damage = size == AsteroidSize.Large ? 34f : size == AsteroidSize.Medium ? 24f : 14f;
            return kind == AsteroidKind.Magma ? damage * 1.25f : damage;
        }

        /// <summary>How many fragments one breaks into; <paramref name="roll"/> is a random number in [0, 1).</summary>
        public static int SplitCount(AsteroidKind kind, AsteroidSize size, float roll)
        {
            if (size == AsteroidSize.Small)
            {
                return 0;
            }
            if (kind == AsteroidKind.Ice)
            {
                return size == AsteroidSize.Large ? 4 : 3;
            }
            if (kind == AsteroidKind.Hydra)
            {
                return size == AsteroidSize.Large ? 3 : roll < 0.3f ? 3 : 2;
            }
            if (size == AsteroidSize.Large && roll < 0.3f)
            {
                return 3;
            }
            return 2;
        }

        /// <summary>Size of the fragments: one size down, ice shatters straight into small shards.</summary>
        public static AsteroidSize SplitSize(AsteroidKind kind, AsteroidSize size)
        {
            if (kind == AsteroidKind.Ice || size == AsteroidSize.Small)
            {
                return AsteroidSize.Small;
            }
            return size - 1;
        }

        /// <summary>Crystals an ore asteroid drops when destroyed.</summary>
        public static int CrystalDrops(AsteroidKind kind, AsteroidSize size)
        {
            if (kind == AsteroidKind.Ore)
            {
                return (int)size + 1;
            }
            if (kind == AsteroidKind.Wreck)
            {
                // Salvage: the big pieces of a wreck hold some.
                return (int)size;
            }
            return 0;
        }

        /// <summary>Homing shards a void crystal releases when destroyed.</summary>
        public static int ShardCount(AsteroidKind kind, AsteroidSize size)
        {
            if (kind != AsteroidKind.Crystal)
            {
                return 0;
            }
            return (int)size + 2;
        }

        public static string Title(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Ore: return "Ore Rock";
                case AsteroidKind.Magma: return "Magma Rock";
                case AsteroidKind.Ice: return "Ice Rock";
                case AsteroidKind.Crystal: return "Void Crystal";
                case AsteroidKind.Metal: return "Iron Rock";
                case AsteroidKind.Hydra: return "Hydra Rock";
                case AsteroidKind.Wreck: return "Wreckage";
                default: return "Rock";
            }
        }

        /// <summary>Colour of the debris and the flash when one breaks.</summary>
        public static Color Tint(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Ore: return new Color(1f, 0.82f, 0.35f);
                case AsteroidKind.Magma: return new Color(1f, 0.45f, 0.12f);
                case AsteroidKind.Ice: return new Color(0.55f, 0.9f, 1f);
                case AsteroidKind.Crystal: return new Color(0.85f, 0.35f, 1f);
                case AsteroidKind.Metal: return new Color(0.75f, 0.82f, 0.95f);
                case AsteroidKind.Hydra: return new Color(0.45f, 1f, 0.35f);
                case AsteroidKind.Wreck: return new Color(1f, 0.6f, 0.3f);
                default: return new Color(1f, 0.7f, 0.4f);
            }
        }
    }
}
