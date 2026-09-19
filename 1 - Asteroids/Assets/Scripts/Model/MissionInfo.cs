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
}
