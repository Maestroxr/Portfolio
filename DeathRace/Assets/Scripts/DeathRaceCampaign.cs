using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The tracks of the game in order. A track earns stars by the best place on it (any race but online: three for a
    /// win, two for second, one for third). The first three tracks are open from the start; every other ordinary track
    /// opens when the ordinary track before it has a star (the Showdown's arena is skipped, so a star on Downtown Carnage
    /// opens Refinery Row past the Crucible), or when a career on this device has reached it (every league opens its own
    /// tracks as the player climbs its ladder: <see cref="DeathRaceProgress.IsCareerTrackOpen"/>). The Showdown's arena
    /// opens for quick races once a champion of any league is beaten.
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
            var own = progress as DeathRaceProgress;
            if (IsShowdown(index, this))
            {
                return own != null && own.ChampionshipsWon > 0;
            }
            if (index < OpenFromStart)
            {
                return true;
            }
            int previous = PreviousOrdinary(index, this);
            return previous < 0 || progress.IsCompleted(previous) || (own != null && own.IsCareerTrackOpen(index));
        }

        /// <summary>
        /// Whether level <paramref name="index"/> is a Showdown: the campaign's level says so; without a campaign (or a
        /// level that is not a track's) the track table does.
        /// </summary>
        public static bool IsShowdown(int index, DeathRaceCampaign campaign)
        {
            DeathRaceLevel level = campaign != null ? campaign.Level(index) : null;
            if (level != null)
            {
                return level.Showdown;
            }
            return index >= 0 && index < TrackLayouts.Count && TrackLayouts.All[index].Showdown;
        }

        /// <summary>The ordinary level before <paramref name="index"/> (Showdowns skipped), or -1 when there is none.</summary>
        public static int PreviousOrdinary(int index, DeathRaceCampaign campaign)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (!IsShowdown(i, campaign))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Stars for a place: 3 for a win, 2 for second, 1 for third, none after.</summary>
        public static int StarsFor(int place)
        {
            return place >= 1 && place <= 3 ? 4 - place : 0;
        }
    }
}
