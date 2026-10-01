using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>What the mission select shows about one mission.</summary>
    public struct MissionSummary
    {
        public int Index;
        public int Number;
        public string Title;
        public string Description;
        public string Introduces;
        public string Objective;
        public int Sector;
        public string SectorTitle;
        public Color Accent;
        public bool Unlocked;
        public string LockReason;
        public int Stars;
        public int BestScore;
        public int ScoreGoal;
        public bool Endless;
        public int BestWave;
        public bool Boss;
    }


    /// <summary>What the results screen shows about a finished mission.</summary>
    public struct MissionResult
    {
        public string Title;
        public bool Victory;
        public bool Endless;
        public int Stars;
        public bool Flawless;
        public bool ScoreGoalReached;
        public int Score;
        public int ScoreGoal;
        public int LifeBonus;
        public bool NewBest;
        public int Kills;
        public float Accuracy;
        public int MaxCombo;
        public int Crystals;
        public float Time;
        public int Wave;
        public int BestWave;
        public bool HasNext;
        public bool NextLocked;
        public string NextLockReason;
        /// <summary>A mission flown with other pilots: no stars and records, but everybody's scores.</summary>
        public bool Coop;
        /// <summary>A mission flown by several pilots at this device: no stars and records, everybody's scores, and a retry.</summary>
        public bool Local;
        /// <summary>The pilots of a shared or local mission by place, a line each.</summary>
        public string Standings;
    }


    /// <summary>Everything the HUD shows during a mission, refreshed every frame.</summary>
    public struct HudState
    {
        public int Score;
        public int Multiplier;
        /// <summary>Share of the combo window left (0 to 1).</summary>
        public float ComboTime;
        public string Objective;
        public float ObjectiveProgress;
        public float Hull;
        public float Shield;
        public int Lives;
        public WeaponType Weapon;
        public int WeaponLevel;
        public int Bombs;
        /// <summary>0 when the dash is ready, 1 right after dashing.</summary>
        public float DashRecharge;
        public bool BossActive;
        public string BossName;
        public float BossHealth;
    }


    /// <summary>What the hangar shows about one ship.</summary>
    public struct HangarShip
    {
        public int Index;
        public string Name;
        public string Description;
        public bool Unlocked;
        public int StarsToUnlock;
        public bool Selected;
        /// <summary>0 to 1 ratings for the bars: speed, handling, hull, fire rate.</summary>
        public float Speed;
        public float Handling;
        public float Hull;
        public float FireRate;
    }


    /// <summary>What the strike tab of the mission select shows about one strike mission.</summary>
    public struct StrikeMissionSummary
    {
        public int Index;
        /// <summary>1 to 9 within the strike campaign.</summary>
        public int Number;
        public string Title;
        public string Description;
        /// <summary>The index of the mission's sector in the campaign (4 to 6).</summary>
        public int Sector;
        public string SectorTitle;
        public Color Accent;
        public bool Unlocked;
        public string LockReason;
        public int Stars;
        /// <summary>The most money one flight earned.</summary>
        public int BestMoney;
        public string BossName;
        /// <summary>Strike stars needed to enter the mission's sector.</summary>
        public int SectorStars;
    }


    /// <summary>Everything the strike HUD shows during a mission, refreshed every frame.</summary>
    public struct StrikeHudState
    {
        /// <summary>The pilot's wallet before the mission.</summary>
        public int Wallet;
        /// <summary>The money earned in this mission so far.</summary>
        public int MissionMoney;
        /// <summary>Energy, 0 to 1.</summary>
        public float Energy;
        /// <summary>The phase shield in use, 0 to 1.</summary>
        public float Shield;
        public int PhaseShields;
        public int Megabombs;
        /// <summary>0 when the megabomb is ready, 1 right after one went off.</summary>
        public float MegabombCooldown;
        public StrikeItem Special;
        /// <summary>How far the mission got toward the boss, 0 to 1.</summary>
        public float Progress;
        public bool BossActive;
        public string BossName;
        public float BossHealth;
        /// <summary>The boss bar shows the health only with the Ion Scanner.</summary>
        public bool HasScanner;
        public bool ShieldLow;
    }


    /// <summary>What the strike results screen shows about a finished strike mission.</summary>
    public struct StrikeResult
    {
        public string Title;
        public bool Victory;
        public int Stars;
        public bool KillStar;
        public bool DamageStar;
        /// <summary>The money earned in the mission (paid into the wallet on a win).</summary>
        public int Money;
        /// <summary>The wallet after the mission.</summary>
        public int Wallet;
        public int HostilesEntered;
        public int HostilesDestroyed;
        public float DamageTaken;
        public float Time;
        /// <summary>The mission's best money so far was beaten.</summary>
        public bool NewBest;
        public bool HasNext;
        public bool NextLocked;
        public string NextLockReason;
        /// <summary>A mission flown with other pilots: no Supply Room button, everybody's money.</summary>
        public bool Coop;
        /// <summary>A mission flown by several pilots at this device: no stars or Supply Room button, everybody's money, a retry.</summary>
        public bool Local;
        /// <summary>The pilots of a shared or local mission by place, a line each.</summary>
        public string Standings;
    }
}
