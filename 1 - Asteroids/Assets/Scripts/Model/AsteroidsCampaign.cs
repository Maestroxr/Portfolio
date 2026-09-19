using System;
using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The Asteroids campaign: the missions of the base <see cref="Campaign"/> grouped into sectors. On top of the base
    /// rule (a mission opens once the one before it is completed) every sector is gated behind a number of stars, and
    /// the endless mission opens once the first sector's boss is beaten.
    /// </summary>
    [CreateAssetMenu(fileName = "AsteroidsCampaign", menuName = "Asteroids/Campaign", order = 3)]
    public class AsteroidsCampaign : Campaign
    {
        [Serializable]
        public class Sector
        {
            public string title;
            public SectorTheme theme;
            [Tooltip("Stars from the whole campaign needed to enter the sector.")]
            public int starsRequired;
        }

        [SerializeField] internal Sector[] sectors = new Sector[0];
        [Tooltip("Missions to complete before the endless mission opens.")]
        [SerializeField] internal int endlessAfter = 3;

        public int SectorCount => sectors != null ? sectors.Length : 0;

        public Sector GetSector(int index)
        {
            return sectors != null && index >= 0 && index < sectors.Length ? sectors[index] : null;
        }

        public AsteroidsLevel Mission(int index)
        {
            return this[index] as AsteroidsLevel;
        }

        /// <summary>Index of the endless mission, or -1.</summary>
        public int EndlessIndex
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    if (Mission(i) != null && Mission(i).IsEndless)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        /// <summary>Stars are earned on campaign missions only; the endless mission keeps a record instead.</summary>
        public override int MaxStars
        {
            get
            {
                int missions = 0;
                for (int i = 0; i < Count; i++)
                {
                    if (Mission(i) != null && !Mission(i).IsEndless)
                    {
                        missions++;
                    }
                }
                return missions * MaxStarsPerLevel;
            }
        }

        /// <summary>Stars needed before the mission at <paramref name="index"/> opens; zero when only the mission before counts.</summary>
        public int StarsRequired(int index)
        {
            AsteroidsLevel mission = Mission(index);
            if (mission == null || mission.IsEndless)
            {
                return 0;
            }
            Sector sector = GetSector(mission.Sector);
            return sector != null ? sector.starsRequired : 0;
        }

        public override bool IsUnlocked(int index, CampaignProgress progress)
        {
            AsteroidsLevel mission = Mission(index);
            if (mission == null)
            {
                return false;
            }
            if (progress == null)
            {
                return true;
            }
            if (mission.IsEndless)
            {
                return CompletedMissions(progress) >= endlessAfter;
            }
            int previous = PreviousMission(index);
            bool previousDone = previous < 0 || progress.IsCompleted(previous);
            return previousDone && TotalStars(progress) >= StarsRequired(index);
        }

        public override int TotalStars(CampaignProgress progress)
        {
            if (progress == null)
            {
                return 0;
            }
            int total = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Mission(i) != null && !Mission(i).IsEndless)
                {
                    total += progress.Stars(i);
                }
            }
            return total;
        }

        /// <summary>The campaign mission after <paramref name="index"/>, skipping the endless one; -1 after the last.</summary>
        public int NextMission(int index)
        {
            for (int i = index + 1; i < Count; i++)
            {
                if (Mission(i) != null && !Mission(i).IsEndless)
                {
                    return i;
                }
            }
            return -1;
        }

        private int PreviousMission(int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (Mission(i) != null && !Mission(i).IsEndless)
                {
                    return i;
                }
            }
            return -1;
        }

        private int CompletedMissions(CampaignProgress progress)
        {
            int done = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Mission(i) != null && !Mission(i).IsEndless && progress.IsCompleted(i))
                {
                    done++;
                }
            }
            return done;
        }
    }
}
