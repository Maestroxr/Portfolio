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
        Crystal = 4
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
        Missiles = 3
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
        Drones = 3
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
        Endless = 4
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
        SupplyPod = 6
    }


    /// <summary>The projectiles enemies, bombs and crystals fire at the ship.</summary>
    public enum EnemyShotKind
    {
        Plasma = 0,
        Shrapnel = 1,
        Shard = 2,
        Missile = 3,
        Acid = 4
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

        public DamageInfo(float amount, Vector2 direction, Vector2 point, DamageSource source, bool byPlayer, Shot shot = null)
        {
            Amount = amount;
            Direction = direction;
            Point = point;
            Source = source;
            ByPlayer = byPlayer;
            Shot = shot;
        }
    }


    /// <summary>The numbers behind every kind and size of asteroid.</summary>
    public static class AsteroidRules
    {
        public static float Radius(AsteroidKind kind, AsteroidSize size)
        {
            float radius = size == AsteroidSize.Large ? 1.55f : size == AsteroidSize.Medium ? 0.95f : 0.55f;
            switch (kind)
            {
                case AsteroidKind.Ice: return radius * 1.05f;
                case AsteroidKind.Crystal: return radius * 1.1f;
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
                default: return new Color(1f, 0.7f, 0.4f);
            }
        }
    }
}
