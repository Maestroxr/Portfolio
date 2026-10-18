using System;
using System.Collections.Generic;
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
    ///
    /// Each mode holds one or more campaigns of its own (<see cref="Arc"/>: the first is the one every mode started with,
    /// the others come after it). A sector belongs to one campaign of its mode (<see cref="Sector.arc"/>), and a mission to
    /// the campaign of its sector. The previous and next missions, the star gates and the stars they count stay inside the
    /// campaign; the first mission of every campaign opens with the campaign (<see cref="Arc.starsRequired"/>). Missions of
    /// later campaigns are appended after everything that was there before, so no older index moves (progress keys and
    /// online room levels are indices).
    /// </summary>
    [CreateAssetMenu(fileName = "AsteroidsCampaign", menuName = "Asteroids/Campaign", order = 3)]
    public class AsteroidsCampaign : Campaign
    {
        [Serializable]
        public class Sector
        {
            public string title;
            public SectorTheme theme;
            [Tooltip("Stars of the sector's campaign needed to enter the sector.")]
            public int starsRequired;
            [Tooltip("The kind of missions in the sector.")]
            public MissionMode mode;
            [Tooltip("The campaign of the sector's mode the sector belongs to: 0 is the first campaign of the mode.")]
            public int arc;
        }

        /// <summary>One campaign of a mode: its name and story for the mission select, and what opens it.</summary>
        [Serializable]
        public class Arc
        {
            [Tooltip("The campaign's name, shown on the mission select (in capitals there).")]
            public string title;
            [Tooltip("A line or two about the campaign for the mission select.")]
            [TextArea] public string description;
            [Tooltip("The kind of missions of the campaign; the campaigns of a mode are numbered in the order they are listed.")]
            public MissionMode mode;
            [Tooltip("Stars of the mode (every campaign of it) needed before the campaign's first mission opens; 0 opens it from the start.")]
            public int starsRequired;
        }

        [SerializeField] internal Sector[] sectors = new Sector[0];
        [Tooltip("The campaigns of both modes; a mode's campaigns are numbered in list order (the first of a mode is 0).")]
        [SerializeField] internal Arc[] arcs = new Arc[0];
        [Tooltip("Missions to complete before the endless mission opens.")]
        [SerializeField] internal int endlessAfter = 3;

        public int SectorCount => sectors != null ? sectors.Length : 0;

        public Sector GetSector(int index)
        {
            return sectors != null && index >= 0 && index < sectors.Length ? sectors[index] : null;
        }

        // ------------------------------------------------------------------ the campaigns of a mode

        /// <summary>
        /// The campaigns <paramref name="mode"/> has: as many as are listed for it, and at least as many as its sectors name
        /// (a mode with missions and nothing listed has one).
        /// </summary>
        public int ArcCount(MissionMode mode)
        {
            int count = 0;
            if (arcs != null)
            {
                foreach (Arc arc in arcs)
                {
                    if (arc != null && arc.mode == mode)
                    {
                        count++;
                    }
                }
            }
            for (int i = 0; i < SectorCount; i++)
            {
                if (sectors[i] != null && sectors[i].mode == mode)
                {
                    count = Math.Max(count, sectors[i].arc + 1);
                }
            }
            if (count == 0 && MissionCountOf(mode) > 0)
            {
                count = 1;
            }
            return count;
        }

        /// <summary>Campaign <paramref name="arc"/> (0 = the first) of <paramref name="mode"/>, or null when none is listed.</summary>
        public Arc GetArc(MissionMode mode, int arc)
        {
            if (arcs == null || arc < 0)
            {
                return null;
            }
            int number = 0;
            foreach (Arc entry in arcs)
            {
                if (entry == null || entry.mode != mode)
                {
                    continue;
                }
                if (number == arc)
                {
                    return entry;
                }
                number++;
            }
            return null;
        }

        /// <summary>The campaign of its mode the mission at <paramref name="index"/> belongs to (the endless mission and a mission without a sector: 0).</summary>
        public int ArcOf(int index)
        {
            Sector sector = SectorOf(index);
            return sector != null ? Math.Max(0, sector.arc) : 0;
        }

        /// <summary>The campaign missions (never the endless one) of campaign <paramref name="arc"/> of <paramref name="mode"/>, in order.</summary>
        public List<int> MissionsOf(MissionMode mode, int arc)
        {
            var missions = new List<int>();
            for (int i = 0; i < Count; i++)
            {
                if (Counts(i, mode, arc))
                {
                    missions.Add(i);
                }
            }
            return missions;
        }

        /// <summary>The sectors of campaign <paramref name="arc"/> of <paramref name="mode"/>, as sector indices in order.</summary>
        public List<int> SectorsOf(MissionMode mode, int arc)
        {
            var list = new List<int>();
            for (int i = 0; i < SectorCount; i++)
            {
                if (sectors[i] != null && sectors[i].mode == mode && sectors[i].arc == arc)
                {
                    list.Add(i);
                }
            }
            return list;
        }

        /// <summary>The stars the missions of campaign <paramref name="arc"/> of <paramref name="mode"/> can earn.</summary>
        public int MaxStarsOf(MissionMode mode, int arc)
        {
            return MissionsOf(mode, arc).Count * MaxStarsPerLevel;
        }

        /// <summary>The stars of the missions of campaign <paramref name="arc"/> of <paramref name="mode"/>.</summary>
        public int TotalStars(CampaignProgress progress, MissionMode mode, int arc)
        {
            if (progress == null)
            {
                return 0;
            }
            int total = 0;
            foreach (int index in MissionsOf(mode, arc))
            {
                total += progress.Stars(index);
            }
            return total;
        }

        /// <summary>Index of the first mission of campaign <paramref name="arc"/> of <paramref name="mode"/> (never the endless one), or -1.</summary>
        public int FirstMission(MissionMode mode, int arc)
        {
            List<int> missions = MissionsOf(mode, arc);
            return missions.Count > 0 ? missions[0] : -1;
        }

        /// <summary>Stars of <paramref name="mode"/> (every campaign of it) needed to open campaign <paramref name="arc"/>.</summary>
        public int ArcStarsRequired(MissionMode mode, int arc)
        {
            Arc entry = GetArc(mode, arc);
            return entry != null ? Math.Max(0, entry.starsRequired) : 0;
        }

        /// <summary>Whether campaign <paramref name="arc"/> of <paramref name="mode"/> is open: its first mission can be flown.</summary>
        public bool IsArcOpen(MissionMode mode, int arc, CampaignProgress progress)
        {
            return progress == null || TotalStars(progress, mode) >= ArcStarsRequired(mode, arc);
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
        /// Stars of the mission's own campaign needed before the mission at <paramref name="index"/> opens (the gate of its
        /// sector); zero when only the mission before counts.
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
            int arc = ArcOf(index);
            if (index == FirstMission(mission.Mode, arc))
            {
                // The first mission of every campaign opens with the campaign (the first campaign of a mode from the start).
                return IsArcOpen(mission.Mode, arc, progress);
            }
            int previous = PreviousMission(index);
            bool previousDone = previous < 0 || progress.IsCompleted(previous);
            return previousDone && TotalStars(progress, mission.Mode, arc) >= StarsRequired(index);
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

        /// <summary>
        /// The campaign mission of the same mode and campaign after <paramref name="index"/>, skipping the endless one; -1
        /// after the last (the last mission of a campaign leads nowhere, not into the next campaign).
        /// </summary>
        public int NextMission(int index)
        {
            MissionMode mode = ModeOf(index);
            int arc = ArcOf(index);
            for (int i = index + 1; i < Count; i++)
            {
                if (Counts(i, mode, arc))
                {
                    return i;
                }
            }
            return -1;
        }

        private int PreviousMission(int index)
        {
            MissionMode mode = ModeOf(index);
            int arc = ArcOf(index);
            for (int i = index - 1; i >= 0; i--)
            {
                if (Counts(i, mode, arc))
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

        /// <summary>Whether the mission at <paramref name="index"/> is a campaign mission of campaign <paramref name="arc"/> of <paramref name="mode"/>.</summary>
        private bool Counts(int index, MissionMode mode, int arc)
        {
            return Counts(index, mode) && ArcOf(index) == arc;
        }
    }
}
