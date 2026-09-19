using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>Tracks a mission's objective and describes it for the briefing and the HUD.</summary>
    public class MissionObjective
    {
        public MissionObjective(LevelObjective type, int target, int waveCount, string bossName)
        {
            Type = type;
            Target = Mathf.Max(0, target);
            WaveCount = waveCount;
            BossName = string.IsNullOrEmpty(bossName) ? "the boss" : bossName;
        }

        public LevelObjective Type { get; }

        public int Target { get; }

        public int WaveCount { get; }

        public string BossName { get; }

        public int WavesCleared { get; private set; }

        public int Crystals { get; private set; }

        public float Survived { get; private set; }

        public bool BossDefeated { get; private set; }

        public bool IsComplete
        {
            get
            {
                switch (Type)
                {
                    case LevelObjective.ClearWaves: return WaveCount > 0 && WavesCleared >= WaveCount;
                    case LevelObjective.Survive: return Survived >= Target;
                    case LevelObjective.Collect: return Crystals >= Target;
                    case LevelObjective.Boss: return BossDefeated;
                    default: return false;
                }
            }
        }

        /// <summary>Progress from 0 to 1 for the HUD bar.</summary>
        public float Progress
        {
            get
            {
                switch (Type)
                {
                    case LevelObjective.ClearWaves: return WaveCount > 0 ? Mathf.Clamp01(WavesCleared / (float)WaveCount) : 0f;
                    case LevelObjective.Survive: return Target > 0 ? Mathf.Clamp01(Survived / Target) : 0f;
                    case LevelObjective.Collect: return Target > 0 ? Mathf.Clamp01(Crystals / (float)Target) : 0f;
                    case LevelObjective.Boss: return BossDefeated ? 1f : 0f;
                    default: return 0f;
                }
            }
        }

        public void Survive(float deltaTime)
        {
            Survived += deltaTime;
        }

        public void WaveCleared()
        {
            WavesCleared++;
        }

        public void CrystalCollected()
        {
            Crystals++;
        }

        public void Defeat()
        {
            BossDefeated = true;
        }

        /// <summary>Restores progress from a saved game.</summary>
        public void Restore(int wavesCleared, int crystals, float survived)
        {
            WavesCleared = wavesCleared;
            Crystals = crystals;
            Survived = survived;
        }

        /// <summary>What the mission asks, for the briefing.</summary>
        public string Briefing
        {
            get
            {
                switch (Type)
                {
                    case LevelObjective.ClearWaves: return $"Clear {WaveCount} {(WaveCount == 1 ? "wave" : "waves")} of asteroids";
                    case LevelObjective.Survive: return $"Survive for {FormatTime(Target)}";
                    case LevelObjective.Collect: return $"Collect {Target} crystals";
                    case LevelObjective.Boss: return $"Destroy {BossName}";
                    default: return "Survive as long as you can";
                }
            }
        }

        /// <summary>The objective with its progress, for the HUD.</summary>
        public string Status(int wave)
        {
            switch (Type)
            {
                case LevelObjective.ClearWaves: return $"WAVE {Mathf.Min(Mathf.Max(1, wave), Mathf.Max(1, WaveCount))} / {WaveCount}";
                case LevelObjective.Survive: return $"SURVIVE  {FormatTime(Mathf.Max(0f, Target - Survived))}";
                case LevelObjective.Collect: return $"CRYSTALS  {Mathf.Min(Crystals, Target)} / {Target}";
                case LevelObjective.Boss: return BossDefeated ? "BOSS DEFEATED" : $"DESTROY {BossName.ToUpperInvariant()}";
                default: return $"WAVE {Mathf.Max(1, wave)}";
            }
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
