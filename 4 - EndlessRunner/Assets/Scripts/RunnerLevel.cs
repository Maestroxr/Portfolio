using System;
using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>What the track generator may put on a level's track.</summary>
    [Flags]
    public enum TrackFeatures
    {
        None = 0,
        Hurdles = 1 << 0,
        Blocks = 1 << 1,
        Barriers = 1 << 2,
        Ramps = 1 << 3,
        PlatformChains = 1 << 4,
        Chasms = 1 << 5,
        JumpPads = 1 << 6,
        MovingCarts = 1 << 7,
        PowerUps = 1 << 8,
        Gems = 1 << 9,
        All = (1 << 10) - 1
    }


    /// <summary>
    /// A level of the Endless Runner campaign: the run settings, the world it is played in, how long the track is and
    /// which obstacles the generator builds it from. The same seed always produces the same track, so a level can be
    /// learned. A level without a length is endless and rotates through its themes.
    /// </summary>
    [CreateAssetMenu(fileName = "RunnerLevel", menuName = "Endless Runner/Level", order = 2)]
    public class RunnerLevel : GameLevel
    {
        [field: SerializeField]
        public RunnerSettings Settings { get; private set; }

        [SerializeField, TextArea] internal string description;
        [SerializeField] internal RunnerTheme theme;
        [Tooltip("Endless levels switch to the next of these themes every Theme Length meters.")]
        [SerializeField] internal RunnerTheme[] themeRotation = new RunnerTheme[0];
        [SerializeField] internal float themeLength = 600f;
        [Tooltip("Distance to the finish line in meters. Zero makes the level endless.")]
        [SerializeField] internal float length = 500f;
        [SerializeField] internal int seed = 1;
        [SerializeField, Range(0f, 1f)] internal float startDifficulty;
        [SerializeField, Range(0f, 1f)] internal float endDifficulty = 0.5f;
        [SerializeField] internal TrackFeatures features = TrackFeatures.Hurdles | TrackFeatures.Blocks;
        [Tooltip("Features introduced by this level: the track shows each of them early, with a hint when hints are on.")]
        [SerializeField] internal TrackFeatures spotlight;
        [SerializeField] internal PowerUpType[] powerUps = new PowerUpType[0];
        [Tooltip("Share of the level's coins needed for the coin star.")]
        [SerializeField, Range(0.1f, 1f)] internal float coinGoal = 0.6f;
        [SerializeField] internal bool showHints;

        public string Description => description;
        public RunnerTheme Theme => theme;
        public float Length => length;
        public bool IsEndless => length <= 0f;
        public int Seed => seed;
        public TrackFeatures Features => features;
        public TrackFeatures Spotlight => spotlight;
        public PowerUpType[] PowerUps => powerUps ?? new PowerUpType[0];
        public float CoinGoal => coinGoal;
        public bool ShowHints => showHints;

        public bool Has(TrackFeatures feature)
        {
            return (features & feature) == feature;
        }

        /// <summary>The theme of the track at <paramref name="z"/>: fixed for campaign levels, rotating when endless.</summary>
        public RunnerTheme ThemeAt(float z)
        {
            if (!IsEndless || themeRotation == null || themeRotation.Length == 0 || themeLength <= 0f)
            {
                return theme;
            }
            int index = Mathf.FloorToInt(Mathf.Max(0f, z) / themeLength) % themeRotation.Length;
            return themeRotation[index] != null ? themeRotation[index] : theme;
        }

        /// <summary>Difficulty (0 to 1) at <paramref name="z"/>; endless levels reach the end value after 2.5 km.</summary>
        public float DifficultyAt(float z)
        {
            float span = IsEndless ? 2500f : Mathf.Max(1f, length);
            return Mathf.Lerp(startDifficulty, endDifficulty, Mathf.Clamp01(z / span));
        }

        public override bool IsLevelValid(out string message)
        {
            if (Settings == null)
            {
                message = "The level has no runner settings.";
                return false;
            }
            if (theme == null)
            {
                message = "The level has no theme.";
                return false;
            }
            if (!IsEndless && length < 60f)
            {
                message = $"The level is {length} m long; it needs at least 60 m.";
                return false;
            }
            return Settings.AreSettingsValid(out message);
        }
    }
}
