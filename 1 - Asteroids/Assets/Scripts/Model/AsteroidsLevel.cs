using System;
using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// One wave of a mission: the large asteroids it starts with and the hazards that arrive while it lasts. A wave of
    /// a clearing mission ends when its asteroids and enemies are gone; in a survival mission it ends after
    /// <see cref="duration"/> seconds.
    /// </summary>
    [Serializable]
    public class WaveSpec
    {
        [Tooltip("Announced when the wave starts; empty announces \"Wave n\".")]
        public string title;

        [Header("Large asteroids at the start of the wave")]
        public int rocks;
        public int ores;
        public int magma;
        public int ice;
        public int crystals;

        [Header("Hazards spread over the wave")]
        public int mines;
        public int bombs;
        public int comets;
        public int wells;
        public int saucers;
        public int wasps;
        public int pods;

        [Tooltip("Seconds between two hazards.")]
        public float hazardInterval = 6f;

        [Tooltip("Seconds the wave lasts in survival missions.")]
        public float duration = 30f;

        public int AsteroidCount => rocks + ores + magma + ice + crystals;

        public int HazardCount => mines + bombs + comets + wells + saucers + wasps + pods;

        public int Count(AsteroidKind kind)
        {
            switch (kind)
            {
                case AsteroidKind.Ore: return ores;
                case AsteroidKind.Magma: return magma;
                case AsteroidKind.Ice: return ice;
                case AsteroidKind.Crystal: return crystals;
                default: return rocks;
            }
        }

        public int Count(HazardKind kind)
        {
            switch (kind)
            {
                case HazardKind.Mine: return mines;
                case HazardKind.ClusterBomb: return bombs;
                case HazardKind.Comet: return comets;
                case HazardKind.GravityWell: return wells;
                case HazardKind.Saucer: return saucers;
                case HazardKind.Wasp: return wasps;
                default: return pods;
            }
        }

        public WaveSpec Copy()
        {
            return (WaveSpec)MemberwiseClone();
        }

        /// <summary>
        /// Wave <paramref name="number"/> (1 based) of the endless mission: more and tougher asteroids every wave, new
        /// hazards joining as the waves go by.
        /// </summary>
        public static WaveSpec Endless(int number, System.Random random)
        {
            int n = Mathf.Max(1, number);
            var wave = new WaveSpec { hazardInterval = Mathf.Max(2.5f, 6.5f - n * 0.2f), duration = 45f };
            int budget = 3 + n / 2 + n / 5;
            wave.rocks = Mathf.Max(1, budget - Mathf.Min(budget - 1, n / 3));
            int special = budget - wave.rocks;
            for (int i = 0; i < special; i++)
            {
                int pick = random.Next(Mathf.Min(4, 1 + n / 3));
                switch (pick)
                {
                    case 0: wave.ores++; break;
                    case 1: wave.magma++; break;
                    case 2: wave.ice++; break;
                    default: wave.crystals++; break;
                }
            }
            wave.pods = n % 2 == 0 ? 1 : 0;
            wave.mines = n >= 2 ? random.Next(0, 2 + n / 4) : 0;
            wave.comets = n >= 3 ? random.Next(0, 1 + n / 5) : 0;
            wave.saucers = n >= 4 ? random.Next(0, 1 + n / 6) : 0;
            wave.wasps = n >= 6 ? random.Next(0, 2 + n / 5) : 0;
            wave.bombs = n >= 7 ? random.Next(0, 1 + n / 7) : 0;
            wave.wells = n >= 8 && random.NextDouble() < 0.35 ? 1 : 0;
            return wave;
        }
    }


    /// <summary>
    /// A mission of the Asteroids campaign: the sector it is flown in, what it asks for, the waves it throws at the
    /// ship, the boss that ends it, what the rocks and supply pods drop and the score that earns the third star.
    /// </summary>
    [CreateAssetMenu(fileName = "AsteroidsLevel", menuName = "Asteroids/Level", order = 2)]
    public class AsteroidsLevel : GameLevel
    {
        [field: SerializeField]
        public AsteroidSettings Settings { get; private set; }

        [SerializeField, TextArea] internal string description;
        [Tooltip("What the mission introduces, shown in the mission briefing.")]
        [SerializeField] internal string introduces;
        [Tooltip("Tips shown one after the other while the mission starts.")]
        [SerializeField] internal string[] hints = new string[0];
        [Tooltip("Wording of the tips for touch play, by position in Hints; empty entries keep the tip as it is.")]
        [SerializeField] internal string[] touchHints = new string[0];
        [SerializeField] internal int sector;
        [SerializeField] internal SectorTheme theme;
        [Tooltip("Endless missions move on to the next of these themes every Waves Per Theme waves.")]
        [SerializeField] internal SectorTheme[] themeRotation = new SectorTheme[0];
        [SerializeField] internal int wavesPerTheme = 5;
        [SerializeField] internal LevelObjective objective;
        [Tooltip("Crystals to collect or seconds to survive, depending on the objective.")]
        [SerializeField] internal int objectiveTarget;
        [SerializeField] internal WaveSpec[] waves = new WaveSpec[0];
        [Tooltip("Arrives after the last wave of a boss mission.")]
        [SerializeField] internal Boss boss;
        [Tooltip("Endless missions send these bosses every Boss Every waves, in turn.")]
        [SerializeField] internal Boss[] bossRotation = new Boss[0];
        [SerializeField] internal int bossEvery = 10;
        [SerializeField] internal Loot asteroidLoot;
        [SerializeField, Range(0f, 1f)] internal float lootChance = 0.06f;
        [SerializeField] internal Loot podLoot;
        [Tooltip("Score that earns the third star.")]
        [SerializeField] internal int scoreGoal = 10000;
        [Tooltip("Speed multiplier of this mission's asteroids, on top of the settings.")]
        [SerializeField] internal float speedMultiplier = 1f;

        public string Description => description;
        public string Introduces => introduces;
        public string[] Hints => hints ?? new string[0];

        public int HintCount => hints != null ? hints.Length : 0;

        /// <summary>Tip <paramref name="index"/>, in its touch wording when there is one and <paramref name="touch"/>.</summary>
        public string Hint(int index, bool touch)
        {
            if (touch && touchHints != null && index < touchHints.Length && !string.IsNullOrEmpty(touchHints[index]))
            {
                return touchHints[index];
            }
            return hints[index];
        }
        public int Sector => sector;
        public SectorTheme Theme => theme;
        public LevelObjective Objective => objective;
        public int ObjectiveTarget => objectiveTarget;
        public WaveSpec[] Waves => waves ?? new WaveSpec[0];
        public Boss BossPrefab => boss;
        public Loot AsteroidLoot => asteroidLoot;
        public float LootChance => lootChance;
        public Loot PodLoot => podLoot;
        public int ScoreGoal => scoreGoal;
        public float SpeedMultiplier => speedMultiplier;
        public bool IsEndless => objective == LevelObjective.Endless;

        /// <summary>The sector theme of the wave <paramref name="waveNumber"/> (1 based): fixed, or rotating when endless.</summary>
        public SectorTheme ThemeForWave(int waveNumber)
        {
            if (!IsEndless || themeRotation == null || themeRotation.Length == 0 || wavesPerTheme <= 0)
            {
                return theme;
            }
            int index = (Mathf.Max(1, waveNumber) - 1) / wavesPerTheme % themeRotation.Length;
            return themeRotation[index] != null ? themeRotation[index] : theme;
        }

        /// <summary>The boss of an endless wave, or null when the wave has none.</summary>
        public Boss BossForWave(int waveNumber)
        {
            if (!IsEndless || bossRotation == null || bossRotation.Length == 0 || bossEvery <= 0 || waveNumber % bossEvery != 0)
            {
                return null;
            }
            return bossRotation[(waveNumber / bossEvery - 1) % bossRotation.Length];
        }

        public override bool IsLevelValid(out string message)
        {
            if (Settings == null)
            {
                message = "The level has no asteroid settings.";
                return false;
            }
            if (theme == null)
            {
                message = "The level has no sector theme.";
                return false;
            }
            if (!IsEndless && Waves.Length == 0)
            {
                message = "The level has no waves.";
                return false;
            }
            if (objective == LevelObjective.Boss && boss == null)
            {
                message = "The boss level has no boss.";
                return false;
            }
            if ((objective == LevelObjective.Collect || objective == LevelObjective.Survive) && objectiveTarget <= 0)
            {
                message = $"The {objective} objective needs a positive target.";
                return false;
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
