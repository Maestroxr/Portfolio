using System;
using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The Asteroids campaign: the missions of the base <see cref="Campaign"/> grouped into sectors. On top of the base
    /// rule (a mission opens once the one before it is completed) every sector is gated behind a number of stars, and
    /// the endless mission opens once the first sector's boss is beaten.
    ///
    /// The campaign holds two kinds of missions (<see cref="MissionMode"/>): the asteroid field missions and the endless
    /// one first, then the planet strike missions appended after them, with their sectors appended after the field
    /// sectors. Each mode is a campaign of its own: the previous and next missions, the star gates and the stars they
    /// count stay inside the mode, and only field missions count toward the endless mission. <see cref="TotalStars(CampaignProgress)"/>
    /// and <see cref="MaxStars"/> still count every mode (the hangar's unlocks use them).
    /// </summary>
    [CreateAssetMenu(fileName = "AsteroidsCampaign", menuName = "Asteroids/Campaign", order = 3)]
    public class AsteroidsCampaign : Campaign
    {
        [Serializable]
        public class Sector
        {
            public string title;
            public SectorTheme theme;
            [Tooltip("Stars of the sector's mode needed to enter the sector.")]
            public int starsRequired;
            [Tooltip("The kind of missions in the sector.")]
            public MissionMode mode;
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

        /// <summary>The sector of the mission at <paramref name="index"/>, or null when there is none of the mission's mode.</summary>
        public Sector SectorOf(int index)
        {
            AsteroidsLevel mission = Mission(index);
            Sector sector = mission != null ? GetSector(mission.Sector) : null;
            return sector != null && sector.mode == mission.Mode ? sector : null;
        }

        /// <summary>The kind of the mission at <paramref name="index"/> (the endless mission is a field mission).</summary>
        public MissionMode ModeOf(int index)
        {
            AsteroidsLevel mission = Mission(index);
            return mission != null ? mission.Mode : MissionMode.Field;
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

        /// <summary>The stars the missions of <paramref name="mode"/> can earn.</summary>
        public int MaxStarsOf(MissionMode mode)
        {
            int missions = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Counts(i, mode))
                {
                    missions++;
                }
            }
            return missions * MaxStarsPerLevel;
        }

        /// <summary>Missions of <paramref name="mode"/>, the endless one counted as a field mission.</summary>
        public int MissionCountOf(MissionMode mode)
        {
            int missions = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Mission(i) != null && Mission(i).Mode == mode)
                {
                    missions++;
                }
            }
            return missions;
        }

        /// <summary>Sectors of <paramref name="mode"/>.</summary>
        public int SectorCountOf(MissionMode mode)
        {
            int count = 0;
            for (int i = 0; i < SectorCount; i++)
            {
                if (sectors[i] != null && sectors[i].mode == mode)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Index of the first sector of <paramref name="mode"/>, or -1.</summary>
        public int FirstSectorOf(MissionMode mode)
        {
            for (int i = 0; i < SectorCount; i++)
            {
                if (sectors[i] != null && sectors[i].mode == mode)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Index of the first campaign mission of <paramref name="mode"/> (never the endless one), or -1.</summary>
        public int FirstMission(MissionMode mode)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Counts(i, mode))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Stars of the mission's own mode needed before the mission at <paramref name="index"/> opens; zero when only the
        /// mission before counts.
        /// </summary>
        public int StarsRequired(int index)
        {
            AsteroidsLevel mission = Mission(index);
            if (mission == null || mission.IsEndless)
            {
                return 0;
            }
            Sector sector = SectorOf(index);
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
            if (index == FirstMission(mission.Mode))
            {
                // The first mission of every mode is open from the start.
                return true;
            }
            int previous = PreviousMission(index);
            bool previousDone = previous < 0 || progress.IsCompleted(previous);
            return previousDone && TotalStars(progress, mission.Mode) >= StarsRequired(index);
        }

        /// <summary>The stars of every campaign mission of every mode.</summary>
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

        /// <summary>The stars of the campaign missions of <paramref name="mode"/>.</summary>
        public int TotalStars(CampaignProgress progress, MissionMode mode)
        {
            if (progress == null)
            {
                return 0;
            }
            int total = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Counts(i, mode))
                {
                    total += progress.Stars(i);
                }
            }
            return total;
        }

        /// <summary>The campaign mission of the same mode after <paramref name="index"/>, skipping the endless one; -1 after the last.</summary>
        public int NextMission(int index)
        {
            MissionMode mode = ModeOf(index);
            for (int i = index + 1; i < Count; i++)
            {
                if (Counts(i, mode))
                {
                    return i;
                }
            }
            return -1;
        }

        private int PreviousMission(int index)
        {
            MissionMode mode = ModeOf(index);
            for (int i = index - 1; i >= 0; i--)
            {
                if (Counts(i, mode))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>Field missions completed, for the endless mission (strike missions never count).</summary>
        private int CompletedMissions(CampaignProgress progress)
        {
            int done = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Counts(i, MissionMode.Field) && progress.IsCompleted(i))
                {
                    done++;
                }
            }
            return done;
        }

        /// <summary>Whether the mission at <paramref name="index"/> is a campaign mission (not the endless one) of <paramref name="mode"/>.</summary>
        private bool Counts(int index, MissionMode mode)
        {
            AsteroidsLevel mission = Mission(index);
            return mission != null && !mission.IsEndless && mission.Mode == mode;
        }
    }
}
