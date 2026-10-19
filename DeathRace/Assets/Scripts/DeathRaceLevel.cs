using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// One track of the campaign: which layout it is (an index into the track table, the same as the level's own
    /// index), its land and its laps. The layout itself (the shape, the width, where the pickups lie) is code, so the
    /// level only names it; Title is the track's name, an English key of the localization table.
    /// </summary>
    [CreateAssetMenu(fileName = "DeathRaceLevel", menuName = "Death Race/Level", order = 2)]
    public sealed class DeathRaceLevel : GameLevel
    {
        /// <summary>The index of the track's layout in <c>TrackLayouts.All</c>.</summary>
        [field: SerializeField]
        public int Track { get; private set; }

        [field: SerializeField]
        public Biome Biome { get; private set; }

        [field: SerializeField]
        public int Laps { get; private set; } = 4;

        /// <summary>The race against the champion: offered by the career only, never in a quick race until it is won.</summary>
        [field: SerializeField]
        public bool Showdown { get; private set; }

        [field: SerializeField, TextArea(2, 4)]
        public string Description { get; private set; }

        public override bool IsLevelValid(out string message)
        {
            if (Track < 0)
            {
                message = $"Track {Track} is not a track";
                return false;
            }
            if (Laps < DeathRaceSettings.MinLaps || Laps > DeathRaceSettings.MaxLaps)
            {
                message = $"Laps {Laps} has to be between {DeathRaceSettings.MinLaps} and {DeathRaceSettings.MaxLaps}";
                return false;
            }
            if (string.IsNullOrEmpty(Title))
            {
                message = "A track needs a name";
                return false;
            }
            message = "OK";
            return true;
        }

#if UNITY_EDITOR
        /// <summary>For the content builder.</summary>
        public void Configure(int track, Biome biome, int laps, bool showdown, string description)
        {
            Track = track;
            Biome = biome;
            Laps = laps;
            Showdown = showdown;
            Description = description;
        }
#endif
    }
}
