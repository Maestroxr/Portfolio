using System.Collections.Generic;

namespace Portfolio.EndlessRunner
{
    /// <summary>A runner of a race, as the race HUD and the results show it.</summary>
    public sealed class Racer
    {
        /// <summary>The seat of the runner in the room.</summary>
        public int Seat;

        /// <summary>Place at the start line, from 0: the order of the seats when the race began.</summary>
        public int Slot;

        public string Name = string.Empty;

        /// <summary>The runner played at this device.</summary>
        public bool Local;

        public float Distance;

        /// <summary>Coin value of the pieces the server gave the runner.</summary>
        public int Coins;

        /// <summary>What the runner is worth right now, which is what the places go by.</summary>
        public long Score;

        /// <summary>The score the server has for the runner: reported now and then during the run, final when the race is over.</summary>
        public long Reported;

        /// <summary>The run is over: across the line, or out of hearts.</summary>
        public bool Done;

        /// <summary>Rank by score, from 1; equal scores share a place. Set by <see cref="RaceStandings.Rank"/>.</summary>
        public int Place;
    }


    /// <summary>
    /// The standings of a race, by the rule the base server ranks a finished room with: the higher score is ahead, the
    /// lower seat wins a tie in the order, and equal scores share a place.
    /// </summary>
    public static class RaceStandings
    {
        /// <summary>Sorts the runners by score and gives them their places.</summary>
        public static void Rank(List<Racer> racers)
        {
            racers.Sort((a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : a.Seat.CompareTo(b.Seat));
            int place = 0;
            for (int i = 0; i < racers.Count; i++)
            {
                if (i == 0 || racers[i - 1].Score != racers[i].Score)
                {
                    place = i + 1;
                }
                racers[i].Place = place;
            }
        }

        /// <summary>What a run is worth so far: a point a meter, and the points of the coins.</summary>
        public static long Score(float distance, int coins, int pointsPerCoin)
        {
            long meters = distance > 0f ? (long)distance : 0;
            return meters + (long)coins * pointsPerCoin;
        }

        /// <summary>"1st", "2nd", "3rd", "4th"...</summary>
        public static string Ordinal(int place)
        {
            int lastTwo = place % 100;
            if (lastTwo >= 11 && lastTwo <= 13)
            {
                return place + "th";
            }
            switch (place % 10)
            {
                case 1: return place + "st";
                case 2: return place + "nd";
                case 3: return place + "rd";
                default: return place + "th";
            }
        }
    }
}
