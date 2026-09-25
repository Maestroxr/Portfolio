using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The rules of a shared mission that are not the server's: what a host can set for a room and what the pilots
    /// look like. The room keeps the settings as "key=value;key=value"; the values are read here.
    /// </summary>
    public static class CoopRules
    {
        public const string LivesKey = "lives";
        public const string MissionsKey = "missions";

        public const int MinPilots = 2;
        public const int MaxPilots = 4;

        public const int DefaultLives = 3;
        public const int MinLives = 1;
        public const int MaxLives = 9;

        /// <summary>Distance of the pilots from the middle of the playfield at the start.</summary>
        public const float StartRadius = 3f;

        /// <summary>The ships a host can give every pilot, as the lobby offers them.</summary>
        public static readonly string[] LivesChoices = { "1", "2", "3", "5", "7" };

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

        /// <summary>The colour of a seat: the halo of the ship, its name and its line on the HUD.</summary>
        public static Color SeatColor(int seat)
        {
            return SeatColors[((seat % SeatColors.Length) + SeatColors.Length) % SeatColors.Length];
        }
    }
}
