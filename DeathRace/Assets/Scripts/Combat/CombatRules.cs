using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The numbers of the fighting and the arithmetic on them, apart from the objects that use them, so the weapons,
    /// the health of the cars, the computer drivers and the tests all read one table: how hard each weapon hits, how
    /// a blast falls off, what a crash costs and what a pickup gives.
    /// </summary>
    public static class CombatRules
    {
        // ------------------------------------------------------------------------------------------------
        // Machine gun
        // ------------------------------------------------------------------------------------------------

        public const float GunRoundsPerSecond = 9f;
        /// <summary>
        /// Per round: 12.6 a second nominal, so a full Viper (120) takes ~10 s of hits and a race usually ends with most
        /// cars still running (2.2 wrecked half the field before half distance); mines and rockets stay the big hits.
        /// </summary>
        public const float GunDamage = 1.4f;
        public const float GunRange = 70f;

        /// <summary>Degrees either side of straight ahead a round may stray.</summary>
        public const float GunSpread = 2.2f;

        // ------------------------------------------------------------------------------------------------
        // Mines
        // ------------------------------------------------------------------------------------------------

        public const float MineArmTime = 0.6f;
        public const float MineTriggerRadius = 1.6f;
        public const float MineBlastRadius = 5f;
        public const float MineDamage = 34f;

        /// <summary>Metres per second a mine's blast throws a car at its centre (sideways only).</summary>
        public const float MinePush = 9f;

        /// <summary>Mines lying on the track at once; the oldest goes off when another is dropped.</summary>
        public const int MaxMines = 24;

        // ------------------------------------------------------------------------------------------------
        // Rockets
        // ------------------------------------------------------------------------------------------------

        public const float RocketTopSpeed = 70f;
        public const float RocketAcceleration = 90f;

        /// <summary>Metres per second a rocket leaves the car at, on top of the car's own speed.</summary>
        public const float RocketLaunchSpeed = 18f;

        public const float RocketHomingTime = 2f;

        /// <summary>The whole width of the cone ahead a rocket picks its target in, degrees.</summary>
        public const float RocketCone = 40f;

        public const float RocketSeekRange = 90f;

        /// <summary>Degrees per second a homing rocket turns toward its target.</summary>
        public const float RocketTurnRate = 150f;

        public const float RocketLifetime = 3.5f;
        public const float RocketDamage = 30f;
        public const float RocketBlastRadius = 4f;
        public const float RocketPush = 7f;

        // ------------------------------------------------------------------------------------------------
        // Nitro
        // ------------------------------------------------------------------------------------------------

        public const float NitroSeconds = 2.5f;

        // ------------------------------------------------------------------------------------------------
        // Crashes
        // ------------------------------------------------------------------------------------------------

        /// <summary>Impact speed (m/s along the contact normal) below which a crash does no harm.</summary>
        public const float CrashThreshold = 7f;

        /// <summary>Damage per metre per second of impact speed beyond the threshold.</summary>
        public const float CrashDamagePerSpeed = 1.1f;

        /// <summary>Barriers give a little: a wall hurts this share of what a car hitting as fast would.</summary>
        public const float WallShare = 0.8f;

        /// <summary>How far round from straight ahead (degrees) a contact still counts as a car's front.</summary>
        public const float FrontAngle = 50f;

        /// <summary>Damage per second of scraping along a barrier at <see cref="ScrapeSpeed"/> or faster.</summary>
        public const float ScrapeDamagePerSecond = 2f;

        public const float ScrapeSpeed = 9f;

        /// <summary>Seconds the same car (or the barriers) cannot crash into a car again.</summary>
        public const float CrashCooldown = 0.35f;

        /// <summary>
        /// Seconds after a car was hurt by another that a crash into the barriers still counts as that car's kill: pushed
        /// into the wall is wrecked by whoever pushed.
        /// </summary>
        public const float AssistWindow = 4f;

        // ------------------------------------------------------------------------------------------------
        // Wrecks
        // ------------------------------------------------------------------------------------------------

        public const float WreckBlastRadius = 6f;
        public const float WreckBlastDamage = 12f;
        public const float WreckPush = 6f;

        /// <summary>Below this share of its health a car smokes; below <see cref="BurningAt"/> it smokes black and burns.</summary>
        public const float SmokingAt = 0.5f;

        public const float BurningAt = 0.25f;

        // ------------------------------------------------------------------------------------------------
        // Pickups
        // ------------------------------------------------------------------------------------------------

        public const float PickupRespawn = 9f;
        public const float PickupRadius = 2f;
        public const float RepairShare = 0.25f;
        public const float AmmoShare = 0.5f;
        public const int NitroPerPickup = 1;

        // ------------------------------------------------------------------------------------------------
        // Arithmetic
        // ------------------------------------------------------------------------------------------------

        /// <summary>What one round of a car with <paramref name="stats"/> takes off before the target's armour.</summary>
        public static float RoundDamage(CarStats stats)
        {
            return GunDamage * (stats.GunDamage > 0f ? stats.GunDamage : 1f);
        }

        /// <summary>What a hit of <paramref name="amount"/> takes off a car whose armour lets <paramref name="damageTaken"/> through.</summary>
        public static float AfterArmour(float amount, float damageTaken)
        {
            return Mathf.Max(0f, amount) * Mathf.Max(0f, damageTaken);
        }

        /// <summary>The damage of a crash at <paramref name="impactSpeed"/> m/s along the contact normal (before anything else).</summary>
        public static float CrashDamage(float impactSpeed)
        {
            return Mathf.Max(0f, Mathf.Abs(impactSpeed) - CrashThreshold) * CrashDamagePerSpeed;
        }

        /// <summary>The damage of hitting a barrier at <paramref name="impactSpeed"/>.</summary>
        public static float WallDamage(float impactSpeed)
        {
            return CrashDamage(impactSpeed) * WallShare;
        }

        /// <summary>
        /// What a car takes from a crash into another car: the crash damage, times its own <see cref="CarStats.RamTaken"/>
        /// when it hit with its front (a spiked bumper soaks it), times the other car's <see cref="CarStats.RamDealt"/>
        /// when the other hit with its front (a spiked bumper bites). Armour comes after this.
        /// </summary>
        public static float RamDamage(float impactSpeed, bool ownFront, float ownRamTaken, bool otherFront, float otherRamDealt)
        {
            float damage = CrashDamage(impactSpeed);
            if (ownFront)
            {
                damage *= ownRamTaken;
            }
            if (otherFront)
            {
                damage *= otherRamDealt;
            }
            return damage;
        }

        /// <summary>Whether a point in the direction <paramref name="toPoint"/> from a car facing <paramref name="forward"/> is at its front.</summary>
        public static bool IsFront(Vector2 forward, Vector2 toPoint)
        {
            if (toPoint.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
            {
                return false;
            }
            return Vector2.Angle(forward, toPoint) <= FrontAngle;
        }

        /// <summary>
        /// The damage of a blast of <paramref name="centreDamage"/> and <paramref name="radius"/> on something
        /// <paramref name="distance"/> metres from its centre: all of it at the centre, falling off smoothly to a fifth at
        /// the edge, nothing beyond.
        /// </summary>
        public static float BlastDamage(float centreDamage, float radius, float distance)
        {
            if (radius <= 0f || distance >= radius)
            {
                return 0f;
            }
            float t = Mathf.Clamp01(distance / radius);
            return centreDamage * Mathf.Lerp(1f, 0.2f, t * t * (3f - 2f * t));
        }

        /// <summary>How hard (0 to 1) a blast pushes at <paramref name="distance"/>: hardest at the centre, nothing at the edge.</summary>
        public static float BlastPush(float radius, float distance)
        {
            if (radius <= 0f || distance >= radius)
            {
                return 0f;
            }
            float t = 1f - Mathf.Clamp01(distance / radius);
            return t * t;
        }

        /// <summary>The rounds an ammo pickup puts back for a gun that carries <paramref name="capacity"/>.</summary>
        public static int AmmoPickup(int capacity)
        {
            return Mathf.CeilToInt(Mathf.Max(0, capacity) * AmmoShare);
        }

        /// <summary>The health a repair pickup puts back on a car of <paramref name="maxHealth"/>.</summary>
        public static float RepairPickup(float maxHealth)
        {
            return Mathf.Max(0f, maxHealth) * RepairShare;
        }

        /// <summary>Whether <paramref name="toTarget"/> is inside the cone of <paramref name="coneDegrees"/> (whole width) around <paramref name="forward"/>.</summary>
        public static bool InCone(Vector2 forward, Vector2 toTarget, float coneDegrees)
        {
            if (toTarget.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
            {
                return false;
            }
            return Vector2.Angle(forward, toTarget) <= coneDegrees * 0.5f;
        }

        /// <summary>
        /// Who a hit counts for: the car that dealt it, or nobody when a car hurt itself (its own mine, the blast of its own
        /// rocket).
        /// </summary>
        public static Car Credit(Car victim, Car attacker)
        {
            return attacker != null && attacker != victim ? attacker : null;
        }
    }
}
