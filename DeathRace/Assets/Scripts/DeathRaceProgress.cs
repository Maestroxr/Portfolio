using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What the device remembers across careers: the stars and best lap of every track, how many tracks the careers
    /// have opened, championships won and a few totals. The career itself is a save (named YAML saves); this is the
    /// record book around it.
    /// </summary>
    public sealed class DeathRaceProgress : CampaignProgress
    {
        public DeathRaceProgress(IStorageStrategy storage, GameType type = GameType.DeathRace) : base(storage, type)
        {
        }

        /// <summary>The best lap of a track in seconds, zero when none was driven.</summary>
        public float BestLap(int track)
        {
            return Value($"BestLap.{track}") / 1000f;
        }

        /// <summary>Keeps <paramref name="seconds"/> when it beats the track's best lap; true when it does.</summary>
        public bool RecordLap(int track, float seconds)
        {
            if (seconds <= 0f)
            {
                return false;
            }
            int milliseconds = Mathf.RoundToInt(seconds * 1000f);
            int best = Value($"BestLap.{track}");
            if (best > 0 && best <= milliseconds)
            {
                return false;
            }
            SetValue($"BestLap.{track}", milliseconds);
            return true;
        }

        /// <summary>How many tracks (from the first) the careers of the first league have opened (kept for older devices).</summary>
        public int CareerTracks => Value("CareerTracks", DeathRaceCampaign.OpenFromStart);

        public void OpenCareerTracks(int count)
        {
            if (count > CareerTracks)
            {
                SetValue("CareerTracks", count);
            }
        }

        /// <summary>The tracks any career has opened, a bit per track index (with the count of the first league's careers).</summary>
        public int CareerTrackMask => Value("CareerTrackMask");

        /// <summary>Whether a career on this device has opened track <paramref name="track"/>.</summary>
        public bool IsCareerTrackOpen(int track)
        {
            return track >= 0 && (track < CareerTracks || (track < 31 && (CareerTrackMask & (1 << track)) != 0));
        }

        /// <summary>Remembers that a career opened <paramref name="track"/> (for quick races and split screen).</summary>
        public void OpenCareerTrack(int track)
        {
            if (track >= 0 && track < 31 && !IsCareerTrackOpen(track))
            {
                SetValue("CareerTrackMask", CareerTrackMask | (1 << track));
            }
        }

        public void OpenCareerTracks(System.Collections.Generic.IEnumerable<int> tracks)
        {
            foreach (int track in tracks)
            {
                OpenCareerTrack(track);
            }
        }

        /// <summary>Championships won in any league.</summary>
        public int ChampionshipsWon => Value("Championships");

        public void WinChampionship()
        {
            SetValue("Championships", ChampionshipsWon + 1);
        }

        /// <summary>Times the champion of <paramref name="league"/> was beaten on this device (the first league's count is <see cref="ChampionshipsWon"/> for older devices).</summary>
        public int LeagueTitles(int league)
        {
            return league == 0 && !HasLeagueTitles ? ChampionshipsWon : Value($"LeagueTitles.{league}");
        }

        /// <summary>Whether any league title was recorded per league (devices from before the leagues only counted championships).</summary>
        private bool HasLeagueTitles => Value("LeagueTitles.Recorded") > 0;

        /// <summary>The champion of <paramref name="league"/> was beaten: counts the title and the championship.</summary>
        public void WinLeague(int league)
        {
            if (!HasLeagueTitles)
            {
                // Carry the championships of an older device over to the first league before counting per league.
                SetValue("LeagueTitles.0", ChampionshipsWon);
                SetValue("LeagueTitles.Recorded", 1);
            }
            SetValue($"LeagueTitles.{league}", Value($"LeagueTitles.{league}") + 1);
            WinChampionship();
        }

        /// <summary>A league opens once the one before it has been won on this device; the first is always open.</summary>
        public bool IsLeagueOpen(int league)
        {
            return league <= 0 || LeagueTitles(league - 1) > 0;
        }

        public int Wrecks => Value("Wrecks");

        public void CountWrecks(int kills)
        {
            if (kills > 0)
            {
                SetValue("Wrecks", Wrecks + kills);
            }
        }

        /// <summary>Forgets everything (the settings panel's reset, tests).</summary>
        public void ResetAll(int trackCount)
        {
            var values = new System.Collections.Generic.List<string>
            {
                "CareerTracks", "CareerTrackMask", "Championships", "Wrecks", "LeagueTitles.Recorded", "LeagueTitles.0", "LeagueTitles.1",
                "LeagueTitles.2"
            };
            for (int i = 0; i < trackCount; i++)
            {
                values.Add($"BestLap.{i}");
            }
            Reset(trackCount, null, values);
        }
    }
}
