using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The tracks of the game in order. A track earns stars by the best place on it (any race but online: three for a
    /// win, two for second, one for third). The first three tracks are open from the start; every other one opens when
    /// the track before it has a star, or when the career has reached it (the career opens tracks as the player climbs
    /// the ladder). The Showdown's arena opens for quick races once the champion is beaten.
    /// </summary>
    [CreateAssetMenu(fileName = "DeathRaceCampaign", menuName = "Death Race/Campaign", order = 3)]
    public sealed class DeathRaceCampaign : Campaign
    {
        public const int OpenFromStart = 3;

        public DeathRaceLevel Level(int index)
        {
            return this[index] as DeathRaceLevel;
        }

        public override bool IsUnlocked(int index, CampaignProgress progress)
        {
            if (index < 0 || index >= Count)
            {
                return false;
            }
            if (progress == null)
            {
                return true;
            }
            DeathRaceLevel level = Level(index);
            var own = progress as DeathRaceProgress;
            if (level != null && level.Showdown)
            {
                return own != null && own.ChampionshipsWon > 0;
            }
            if (index < OpenFromStart)
            {
                return true;
            }
            return progress.IsCompleted(index - 1) || (own != null && own.CareerTracks > index);
        }

        /// <summary>Stars for a place: 3 for a win, 2 for second, 1 for third, none after.</summary>
        public static int StarsFor(int place)
        {
            return place >= 1 && place <= 3 ? 4 - place : 0;
        }
    }
}
