using System.Collections.Generic;
using Gamebox.Online;

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
    /// The standings of a race, by the rule the base server ranks a finished room with (<see cref="Standings"/>): the
    /// higher score is ahead, the lower seat wins a tie in the order, and equal scores share a place.
    /// </summary>
    public static class RaceStandings
    {
        /// <summary>Sorts the runners by score and gives them their places.</summary>
        public static void Rank(List<Racer> racers)
        {
            int[] places = Standings.Rank(racers, racer => racer.Score, racer => racer.Seat);
            for (int i = 0; i < racers.Count; i++)
            {
                racers[i].Place = places[i];
            }
        }

        /// <summary>What a run is worth so far: a point a meter, and the points of the coins.</summary>
        public static long Score(float distance, int coins, int pointsPerCoin)
        {
            long meters = distance > 0f ? (long)distance : 0;
            return meters + (long)coins * pointsPerCoin;
        }
    }
}
