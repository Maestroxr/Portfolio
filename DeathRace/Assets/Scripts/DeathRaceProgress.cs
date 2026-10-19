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

        /// <summary>How many tracks (from the first) the careers have opened.</summary>
        public int CareerTracks => Value("CareerTracks", DeathRaceCampaign.OpenFromStart);

        public void OpenCareerTracks(int count)
        {
            if (count > CareerTracks)
            {
                SetValue("CareerTracks", count);
            }
        }

        public int ChampionshipsWon => Value("Championships");

        public void WinChampionship()
        {
            SetValue("Championships", ChampionshipsWon + 1);
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
            var values = new System.Collections.Generic.List<string> { "CareerTracks", "Championships", "Wrecks" };
            for (int i = 0; i < trackCount; i++)
            {
                values.Add($"BestLap.{i}");
            }
            Reset(trackCount, null, values);
        }
    }
}
