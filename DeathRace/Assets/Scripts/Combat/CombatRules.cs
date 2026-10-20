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
        // The other guns (Guns holds their table)
        // ------------------------------------------------------------------------------------------------

        /// <summary>The minigun: nearly twice the rate of lighter rounds that stray more; it carries more of them.</summary>
        public const float MinigunRoundsPerSecond = 16f;
        public const float MinigunDamage = 1.0f;
        public const float MinigunRange = 62f;
        public const float MinigunSpread = 4.5f;
        public const float MinigunAmmoFactor = 1.8f;

        /// <summary>
        /// The flamethrower: puffs of fire that hurt everything in a short cone ahead and set it burning. Every puff lands
        /// (no spread, no misses inside the cone, every car in it), so on paper it does less a second than the machine
        /// gun: whole races with the computer's drivers (CombatCheck -full against -plain) had it dealing three times the
        /// machine gun's damage a car at 1.9 a puff and 3 a second of burning, twice at 1.1 and 2; at these it deals
        /// somewhat more than the machine gun, paid for by its short reach.
        /// </summary>
        public const float FlameRoundsPerSecond = 10f;
        public const float FlameDamage = 1f;
        public const float FlameRange = 18f;
        public const float FlameCone = 30f;
        public const float FlameAmmoFactor = 1.2f;

        /// <summary>A car set burning by a flame takes this much a second for <see cref="BurnSeconds"/> (another flame starts it again).</summary>
        public const float BurnDamagePerSecond = 1.5f;
        public const float BurnSeconds = 2f;

        /// <summary>
        /// Seconds between two puffs of burning damage: a burning car loses <see cref="BurnDamagePerSecond"/> in a few
        /// ticks a second rather than a sliver every physics step (one hit event per tick, not fifty).
        /// </summary>
        public const float BurnTick = 0.25f;

        /// <summary>
        /// The plasma cannon: three heavy bolts a second that fly (not hitscan) and burst a little where they land. A bolt
        /// never strays and the computer's drivers lead their targets, so it lands far more often than a round of the
        /// machine gun: 6.5 a bolt dealt three times the machine gun's damage a car in whole races, 4.4 twice; at 4.0
        /// (on paper a little less a second than the machine gun) it deals somewhat more.
        /// </summary>
        public const float PlasmaRoundsPerSecond = 3f;
        public const float PlasmaDamage = 4f;
        public const float PlasmaRange = 90f;
        public const float PlasmaSpeed = 110f;
        public const float PlasmaSplash = 1.5f;
        public const float PlasmaAmmoFactor = 0.35f;

        /// <summary>
        /// The burst of a bolt: the share of the bolt's damage it deals to the cars round the one it hit (the car hit takes
        /// the bolt itself, whole), and the little push of it.
        /// </summary>
        public const float PlasmaSplashShare = 0.4f;
        public const float PlasmaPush = 2.5f;

        // ------------------------------------------------------------------------------------------------
        // Special weapons
        // ------------------------------------------------------------------------------------------------

        /// <summary>An oil slick: its radius, how long it lies, and what a car that drives through it keeps of its grip and steering, and for how long.</summary>
        public const float OilRadius = 4.5f;
        public const float OilLifetime = 14f;
        public const float OilSlickSeconds = 1.4f;
        public const float OilGrip = 0.25f;
        public const float OilSteering = 0.5f;

        /// <summary>The car that poured a slick drives through it unharmed for this long.</summary>
        public const float OilOwnerGrace = 1.5f;

        /// <summary>Slicks lying on the track at once; the oldest dries up when another is poured.</summary>
        public const int MaxOilSlicks = 12;

        /// <summary>The last seconds of a slick's life, in which it fades and thins out (it still slicks until it is gone).</summary>
        public const float OilFade = 2.5f;

        /// <summary>Degrees per second of yaw a car is thrown into when it hits a slick, at <see cref="OilSpinSpeed"/> or faster.</summary>
        public const float OilSpin = 75f;

        public const float OilSpinSpeed = 30f;

        /// <summary>An EMP blast: its radius round the car, how long the cars in it are jammed, what they keep of their top speed, its damage.</summary>
        public const float EmpRadius = 14f;
        public const float EmpJamSeconds = 3f;
        public const float EmpTopSpeed = 0.6f;
        public const float EmpDamage = 5f;

        /// <summary>A mortar shell: how far ahead of the car it lands (more at speed), how long it flies, its blast.</summary>
        public const float MortarRange = 32f;
        public const float MortarFlightSeconds = 1.1f;
        public const float MortarDamage = 40f;
        public const float MortarBlastRadius = 7f;
        public const float MortarPush = 10f;

        /// <summary>How high a shell climbs over the middle of its flight: this share of the way it flies, at least <see cref="MortarMinApex"/> metres.</summary>
        public const float MortarApexShare = 0.3f;

        public const float MortarMinApex = 8f;

        // ------------------------------------------------------------------------------------------------
        // Fittings at work in a race
        // ------------------------------------------------------------------------------------------------

        /// <summary>The field welder: seconds without a hit before it starts, the share of the health it mends a second, and up to what share.</summary>
        public const float WelderDelay = 4f;
        public const float WelderPerSecond = 0.01f;
        public const float WelderLimit = 0.6f;

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

        /// <summary>What one round of the machine gun of a car with <paramref name="stats"/> takes off before the target's armour.</summary>
        public static float RoundDamage(CarStats stats)
        {
            return GunDamage * (stats.GunDamage > 0f ? stats.GunDamage : 1f);
        }

        /// <summary>
        /// What one round of <paramref name="gun"/> (a bullet, a puff of flame, a bolt) of a car with <paramref name="stats"/>
        /// takes off before the target's armour: the gun's damage times the car's (armour-piercing rounds, the gun mount).
        /// </summary>
        public static float RoundDamage(CarStats stats, GunSpec gun)
        {
            return (gun != null ? gun.Damage : GunDamage) * (stats.GunDamage > 0f ? stats.GunDamage : 1f);
        }

        /// <summary>A crash's damage on a car with <paramref name="stats"/> before its armour: a roll cage soaks a share.</summary>
        public static float CrashTaken(float damage, CarStats stats)
        {
            return Mathf.Max(0f, damage) * (stats.CrashTaken > 0f ? stats.CrashTaken : 1f);
        }

        /// <summary>A blast's damage on a car with <paramref name="stats"/> before its armour: blast shielding soaks a share.</summary>
        public static float BlastTaken(float damage, CarStats stats)
        {
            return Mathf.Max(0f, damage) * (stats.BlastTaken > 0f ? stats.BlastTaken : 1f);
        }

        /// <summary>
        /// What a field welder mends in <paramref name="deltaTime"/> on a car of <paramref name="maxHealth"/> with
        /// <paramref name="health"/> left, <paramref name="sinceHit"/> seconds after it was last hurt: nothing for
        /// <see cref="WelderDelay"/> seconds, then <see cref="WelderPerSecond"/> of the maximum a second, never past
        /// <see cref="WelderLimit"/> of it (it patches; it does not rebuild). Nothing on a wreck.
        /// </summary>
        public static float WelderMend(float maxHealth, float health, float sinceHit, float deltaTime)
        {
            if (health <= 0f || maxHealth <= 0f || sinceHit < WelderDelay || deltaTime <= 0f)
            {
                return 0f;
            }
            float room = maxHealth * WelderLimit - health;
            return room <= 1e-4f ? 0f : Mathf.Min(room, maxHealth * WelderPerSecond * deltaTime);
        }

        /// <summary>How far ahead of a car going <paramref name="speed"/> m/s its mortar shell lands: the range, plus the way the car goes while it flies.</summary>
        public static float MortarReach(float speed)
        {
            return MortarRange + Mathf.Max(0f, speed) * MortarFlightSeconds;
        }

        /// <summary>
        /// Where a shell is <paramref name="t"/> (0 to 1) of the way through its flight from <paramref name="from"/> to
        /// <paramref name="to"/> (on the ground): along the line between them, and up on a parabola whose top is
        /// <paramref name="apex"/> metres above it.
        /// </summary>
        public static Vector3 MortarArc(Vector3 from, Vector3 to, float apex, float t)
        {
            t = Mathf.Clamp01(t);
            Vector3 point = Vector3.Lerp(from, to, t);
            point.y = Mathf.Lerp(from.y, to.y, t) + apex * 4f * t * (1f - t);
            return point;
        }

        /// <summary>The top of a shell's flight over <paramref name="distance"/> metres.</summary>
        public static float MortarApex(float distance)
        {
            return Mathf.Max(MortarMinApex, distance * MortarApexShare);
        }

        /// <summary>The share of the top speed a car keeps (an EMP blast's jam).</summary>
        public static float TopSpeedShare(bool jammed)
        {
            return jammed ? EmpTopSpeed : 1f;
        }

        /// <summary>
        /// Whether a flame from <paramref name="origin"/> along <paramref name="forward"/> reaches a point
        /// <paramref name="toTarget"/> away (flat): within <see cref="FlameRange"/> plus <paramref name="slack"/> (the half
        /// size of what it licks at) and inside the cone of <see cref="FlameCone"/>. Barriers are the caller's to check.
        /// </summary>
        public static bool FlameReaches(Vector2 forward, Vector2 toTarget, float slack = 0f, float range = FlameRange, float cone = FlameCone)
        {
            return toTarget.sqrMagnitude <= (range + slack) * (range + slack) && InCone(forward, toTarget, cone);
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
