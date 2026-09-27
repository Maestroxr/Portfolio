using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The rules of a shared mission that are not the server's: what a host can set for a room (the ships per pilot of
    /// an asteroid field mission, the difficulty of a planet strike) and what the pilots look like and where they start.
    /// The room keeps the settings as "key=value;key=value"; the values are read here.
    /// </summary>
    public static class CoopRules
    {
        public const string LivesKey = "lives";
        public const string MissionsKey = "missions";
        public const string DifficultyKey = "difficulty";

        public const int MinPilots = 2;
        public const int MaxPilots = 4;

        public const int DefaultLives = 3;
        public const int MinLives = 1;
        public const int MaxLives = 9;

        /// <summary>Distance of the pilots from the middle of the playfield at the start.</summary>
        public const float StartRadius = 3f;

        /// <summary>Strike: metres between the pilots of the row they start in.</summary>
        public const float RowSpacing = 5f;

        /// <summary>Strike: height of the starting row above the bottom edge of the playfield.</summary>
        public const float RowHeight = 3.5f;

        /// <summary>Strike: the difficulty of a room when the option is missing or not one (Veteran).</summary>
        public const StrikeDifficulty DefaultDifficulty = StrikeDifficulty.Veteran;

        /// <summary>The ships a host can give every pilot, as the lobby offers them.</summary>
        public static readonly string[] LivesChoices = { "1", "2", "3", "5", "7" };

        /// <summary>The strike difficulties a host can pick, as the lobby offers them (the StrikeDifficulty numbers).</summary>
        public static readonly string[] DifficultyChoices = { "0", "1", "2" };

        /// <summary>What the lobby calls the difficulties.</summary>
        public static readonly string[] DifficultyLabels = { "Rookie", "Veteran", "Elite" };

        private static readonly Color[] SeatColors =
        {
            new Color(0.35f, 0.9f, 1f),
            new Color(1f, 0.62f, 0.25f),
            new Color(0.5f, 1f, 0.55f),
            new Color(1f, 0.5f, 0.9f)
        };

        /// <summary>Ships per pilot in the options of a room; the default when the option is missing or not a number.</summary>
        public static int Lives(string options)
        {
            return Mathf.Clamp(RoomOptions.Number(options, LivesKey, DefaultLives), MinLives, MaxLives);
        }

        /// <summary>The strike difficulty in the options of a room; Veteran when the option is missing or not one.</summary>
        public static StrikeDifficulty Difficulty(string options)
        {
            int value = RoomOptions.Number(options, DifficultyKey, (int)DefaultDifficulty);
            return value >= (int)StrikeDifficulty.Rookie && value <= (int)StrikeDifficulty.Elite ? (StrikeDifficulty)value : DefaultDifficulty;
        }

        /// <summary>What the lobby and the room list call a strike difficulty.</summary>
        public static string DifficultyTitle(StrikeDifficulty difficulty)
        {
            int index = (int)difficulty;
            return index >= 0 && index < DifficultyLabels.Length ? DifficultyLabels[index] : difficulty.ToString();
        }

        /// <summary>
        /// What the lobby calls mission number <paramref name="number"/> of its kind: "3. Title" for an asteroid field
        /// mission, "Title (endless)", or "Strike 2. Title" for a planet strike mission.
        /// </summary>
        public static string LevelTitle(AsteroidsLevel mission, int number)
        {
            if (mission == null)
            {
                return $"Mission {number}";
            }
            if (mission.Mode == MissionMode.Strike)
            {
                return $"Strike {number}. {mission.Title}";
            }
            return mission.IsEndless ? $"{mission.Title} (endless)" : $"{number}. {mission.Title}";
        }

        /// <summary>The colour of a seat: the halo of the ship, its name and its line on the HUD.</summary>
        public static Color SeatColor(int seat)
        {
            return SeatColors[((seat % SeatColors.Length) + SeatColors.Length) % SeatColors.Length];
        }
    }
}
