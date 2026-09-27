using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The numbers of the strike mode that are not a unit's or a weapon's own (design sections 1.1 to 1.3). Raptor's
    /// 200 px screen height is 20 m here and its 23.3 ticks per second give px/tick x 2.33 = m/s.
    /// </summary>
    public static class StrikeRules
    {
        // ------------------------------------------------------------------ playfield and scroll

        /// <summary>Half the size of the strike playfield: exactly the co-op size of the server (35.56 x 20 m).</summary>
        public static readonly Vector2 HalfSize = new Vector2(10f * 16f / 9f, 10f);

        /// <summary>Meters per second the ground scrolls down (a unit crosses the screen in about 8.3 s).</summary>
        public const float ScrollSpeed = 2.4f;

        /// <summary>Depth (z, away from the camera) of the ground plane.</summary>
        public const float GroundDepth = 1.2f;

        /// <summary>Ground units appear this far above the top edge, so they never pop in.</summary>
        public const float SpawnLead = 4f;

        /// <summary>Length of a terrain tile along the level (m).</summary>
        public const float TileLength = 20f;

        /// <summary>Width of a terrain tile (m): wider than the playfield, so it covers any screen.</summary>
        public const float TileWidth = 64f;

        /// <summary>Distance before the boss over which the scroll eases to rest.</summary>
        public const float BossEaseDistance = 3.6f;

        /// <summary>Where the drop shadows of air units and ships fall, relative to the body (on the ground depth).</summary>
        public static readonly Vector2 ShadowOffset = new Vector2(0.9f, -1.2f);

        /// <summary>One tick of the original game in seconds (23.3 ticks per second).</summary>
        public const float Tick = 1f / 23.3f;

        // ------------------------------------------------------------------ the ship

        /// <summary>Seconds the ship takes to reach its top speed from standing.</summary>
        public const float ShipAccelerationTime = 0.35f;

        /// <summary>An axis without input halves its speed every this many seconds.</summary>
        public const float ShipHalfLife = Tick;

        /// <summary>Seconds of invulnerability at the start of a mission (never after a hit).</summary>
        public const float StartInvulnerability = 1.5f;

        /// <summary>Top speed of the ship in m/s for a hull's <see cref="PlayerSettings.MovementSpeed"/>.</summary>
        public static float ShipMaxSpeed(float movementSpeed)
        {
            return 17f * Mathf.Clamp(movementSpeed / 11f, 0.85f, 1.2f);
        }

        /// <summary>Top speed of the ship in m/s with <paramref name="hull"/> (the base speed without one).</summary>
        public static float ShipMaxSpeed(PlayerSettings hull)
        {
            return ShipMaxSpeed(hull != null ? hull.MovementSpeed : 11f);
        }

        // ------------------------------------------------------------------ energy and shields

        public const int MaxEnergy = 100;

        /// <summary>A mission launched with less energy starts with this much (free), so a pilot is never stuck.</summary>
        public const int MinLaunchEnergy = 25;

        /// <summary>Seconds without firing for one point of energy (never on Elite).</summary>
        public const float EnergyRegenInterval = 4.1f;

        public const int EnergyRegenAmount = 1;

        /// <summary>Energy an energy pickup gives; at full energy a quarter of it goes to the current phase shield.</summary>
        public const int EnergyPickup = 25;

        /// <summary>Points of one phase shield.</summary>
        public const float PhaseShieldPoints = 100f;

        public const int MaxPhaseShields = 5;

        /// <summary>At or below this energy with no phase shield, every hit also destroys a weapon.</summary>
        public const float LowEnergy = 10f;

        /// <summary>Damage the mission may cost (phase shields and energy, after multipliers) for the damage star.</summary>
        public const float StarDamage = 30f;

        /// <summary>Share of the hostiles that entered the screen to destroy for the kill star.</summary>
        public const float StarKillShare = 0.7f;

        /// <summary>Multiplier of the damage the pilot takes on <paramref name="difficulty"/> (before the hull's).</summary>
        public static float DamageTaken(StrikeDifficulty difficulty)
        {
            return difficulty == StrikeDifficulty.Rookie ? 0.5f : 1f;
        }

        /// <summary>Whether energy regenerates while not firing on <paramref name="difficulty"/>.</summary>
        public static bool RegeneratesEnergy(StrikeDifficulty difficulty)
        {
            return difficulty != StrikeDifficulty.Elite;
        }

        // ------------------------------------------------------------------ ramming

        /// <summary>Seconds between two ramming hits while a pilot overlaps an air unit.</summary>
        public const float RamInterval = Tick;

        /// <summary>Damage an air unit takes per ramming hit (a Collision by the player).</summary>
        public const float RamUnitDamage = 16f;

        /// <summary>Damage the pilot takes per ramming hit from a unit of <paramref name="diameter"/> (2 x its radius).</summary>
        public static float RamPilotDamage(float diameter)
        {
            return 2.5f * Mathf.Min(diameter, 2.4f);
        }

        // ------------------------------------------------------------------ megabomb

        public const float MegabombCooldown = 2.6f;

        /// <summary>Damage of the megabomb to every targetable enemy and to every boss piece.</summary>
        public const float MegabombDamage = 50f;

        public const int MaxMegabombs = 5;

        // ------------------------------------------------------------------ bosses and the end of a mission

        /// <summary>Multiplier of boss and part health for <paramref name="difficulty"/> and the number of pilots.</summary>
        public static float BossHealthScale(StrikeDifficulty difficulty, int pilots)
        {
            float level = difficulty == StrikeDifficulty.Rookie ? 0.5f : 1f;
            return level * (1f + 0.5f * (Mathf.Max(1, pilots) - 1));
        }

        /// <summary>Multiplier of the shots in a boss's bursts on <paramref name="difficulty"/>.</summary>
        public static float BossBurstScale(StrikeDifficulty difficulty)
        {
            return difficulty == StrikeDifficulty.Rookie ? 0.75f : 1f;
        }

        /// <summary>Seconds of free flight after the boss dies, with the pickup pull.</summary>
        public const float FreeFlightTime = 2f;

        /// <summary>Range (m) from which pickups come to the ship during the free flight.</summary>
        public const float EndPickupPull = 40f;

        /// <summary>Seconds of the fly-off (the ship centres and climbs out of the top).</summary>
        public const float FlyOffTime = 2.2f;

        // ------------------------------------------------------------------ air units

        /// <summary>Seconds a hovering (Repeat) air unit stays before it leaves.</summary>
        public const float RepeatLeaveTime = 12f;

        /// <summary>Dive speed of a kamikaze (m/s).</summary>
        public const float KamikazeDiveSpeed = 18f;

        /// <summary>Share of the destroyed air units that drop a credit orb.</summary>
        public const float CreditOrbChance = 0.5f;

        // ------------------------------------------------------------------ money

        /// <summary>The most money a pilot can hold.</summary>
        public const int WalletCap = 9999999;

        public const int CreditOrb = 50;
        public const int SmallArms = 35200;
        public const int Isotopes = 55700;
        public const int Thaelite = 76000;
        public const int FusionCore = 93800;
        public const int FreyliumOre = 122500;

        /// <summary>The money values a level event may drop (0 = none).</summary>
        public static readonly int[] MoneyValues = { 0, CreditOrb, SmallArms, Isotopes, Thaelite, FusionCore, FreyliumOre };

        /// <summary>Only items priced at or below this may be placed as pickups.</summary>
        public const int MaxPickupPrice = 145200;

        /// <summary>Whether <paramref name="money"/> is one of the <see cref="MoneyValues"/>.</summary>
        public static bool IsMoneyValue(int money)
        {
            return System.Array.IndexOf(MoneyValues, money) >= 0;
        }

        /// <summary>Adds <paramref name="amount"/> to <paramref name="wallet"/> without passing the cap.</summary>
        public static int AddToWallet(int wallet, int amount)
        {
            long total = (long)wallet + amount;
            return (int)System.Math.Max(0L, System.Math.Min(WalletCap, total));
        }

        // ------------------------------------------------------------------ a new pilot

        public const int NewPilotMoney = 10000;
        public const int NewPilotEnergy = 75;
        public const StrikeDifficulty NewPilotDifficulty = StrikeDifficulty.Veteran;
    }
}
