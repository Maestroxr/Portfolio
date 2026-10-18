using System;
using System.Collections.Generic;
using Gamebox;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The plain rules of the mission select's campaigns: which mission a campaign selects when the player switches to it,
    /// a mission's number and place on its campaign's map, and the size of the map that holds every campaign of a mode.
    /// The mission select shows one campaign of a mode at a time (<see cref="AsteroidsCampaign.Arc"/>); its sector map has
    /// a column per sector and a node per mission of the sector, laid out for the largest campaign of the mode, so a
    /// smaller campaign leaves some columns and nodes unused.
    /// </summary>
    public static class CampaignSelect
    {
        /// <summary>
        /// The mission campaign <paramref name="arc"/> of <paramref name="mode"/> selects when the player switches to it: the
        /// next one to fly (its first open mission without stars), else its first mission; -1 when it has none.
        /// </summary>
        public static int NextToFly(AsteroidsCampaign campaign, MissionMode mode, int arc, CampaignProgress progress)
        {
            if (campaign == null)
            {
                return -1;
            }
            List<int> missions = campaign.MissionsOf(mode, arc);
            foreach (int index in missions)
            {
                if (campaign.IsUnlocked(index, progress) && (progress == null || progress.Stars(index) == 0))
                {
                    return index;
                }
            }
            return missions.Count > 0 ? missions[0] : -1;
        }

        /// <summary>
        /// The mission the mission select suggests when it first shows campaign <paramref name="arc"/>: the first open
        /// mission without stars, else the last open one, else the campaign's first; -1 when it has none.
        /// </summary>
        public static int Suggested(AsteroidsCampaign campaign, MissionMode mode, int arc, CampaignProgress progress)
        {
            if (campaign == null)
            {
                return -1;
            }
            List<int> missions = campaign.MissionsOf(mode, arc);
            int suggested = missions.Count > 0 ? missions[0] : -1;
            foreach (int index in missions)
            {
                if (!campaign.IsUnlocked(index, progress))
                {
                    continue;
                }
                suggested = index;
                if (progress == null || progress.Stars(index) == 0)
                {
                    break;
                }
            }
            return suggested;
        }

        /// <summary>
        /// The number of the mission at <paramref name="index"/> within its campaign (1 = the campaign's first); 0 for the
        /// endless mission and anything that is not a campaign mission.
        /// </summary>
        public static int Number(AsteroidsCampaign campaign, int index)
        {
            AsteroidsLevel mission = campaign != null ? campaign.Mission(index) : null;
            if (mission == null || mission.IsEndless)
            {
                return 0;
            }
            return campaign.MissionsOf(mission.Mode, campaign.ArcOf(index)).IndexOf(index) + 1;
        }

        /// <summary>
        /// Where the mission at <paramref name="index"/> sits on its campaign's map: the place of its sector among the
        /// campaign's sectors (the column) and its own place among the campaign missions of that sector (the row); (-1, -1)
        /// for a mission without a sector of its mode (the endless one).
        /// </summary>
        public static (int sector, int position) Place(AsteroidsCampaign campaign, int index)
        {
            AsteroidsLevel mission = campaign != null ? campaign.Mission(index) : null;
            if (mission == null || mission.IsEndless || campaign.SectorOf(index) == null)
            {
                return (-1, -1);
            }
            int arc = campaign.ArcOf(index);
            int sector = campaign.SectorsOf(mission.Mode, arc).IndexOf(mission.Sector);
            int position = 0;
            foreach (int other in campaign.MissionsOf(mission.Mode, arc))
            {
                if (other == index)
                {
                    break;
                }
                if (campaign.Mission(other).Sector == mission.Sector)
                {
                    position++;
                }
            }
            return (sector, position);
        }

        /// <summary>
        /// The map that holds every campaign of <paramref name="mode"/>: the most sectors one campaign has and the most
        /// campaign missions one sector has (at least 1 and 1). A campaign whose missions have no sectors counts as one
        /// column of all its missions.
        /// </summary>
        public static (int sectors, int perSector) Layout(AsteroidsCampaign campaign, MissionMode mode)
        {
            int columns = 1;
            int rows = 1;
            if (campaign == null)
            {
                return (columns, rows);
            }
            int arcs = campaign.ArcCount(mode);
            for (int arc = 0; arc < arcs; arc++)
            {
                List<int> sectors = campaign.SectorsOf(mode, arc);
                columns = Math.Max(columns, sectors.Count);
                int unplaced = 0;
                var counts = new Dictionary<int, int>();
                foreach (int index in campaign.MissionsOf(mode, arc))
                {
                    int sector = campaign.Mission(index).Sector;
                    if (sectors.Contains(sector))
                    {
                        counts.TryGetValue(sector, out int count);
                        counts[sector] = count + 1;
                        rows = Math.Max(rows, count + 1);
                    }
                    else
                    {
                        unplaced++;
                    }
                }
                rows = Math.Max(rows, unplaced);
            }
            return (columns, rows);
        }

        /// <summary>
        /// The node of a map of <paramref name="columns"/> columns of <paramref name="perColumn"/> nodes (numbered column by
        /// column) for every place of <paramref name="places"/> (<see cref="Place"/>): a place on the map gets its own node,
        /// one that is not on it (or whose node is taken) the first node left free; -1 once every node is taken.
        /// </summary>
        public static int[] Nodes(IList<(int sector, int position)> places, int columns, int perColumn)
        {
            int count = places != null ? places.Count : 0;
            var nodes = new int[count];
            int size = Math.Max(0, columns) * Math.Max(0, perColumn);
            var taken = new bool[size];
            for (int i = 0; i < count; i++)
            {
                (int sector, int position) = places[i];
                int node = sector >= 0 && sector < columns && position >= 0 && position < perColumn ? sector * perColumn + position : -1;
                if (node >= 0 && taken[node])
                {
                    node = -1;
                }
                nodes[i] = node;
                if (node >= 0)
                {
                    taken[node] = true;
                }
            }
            int free = 0;
            for (int i = 0; i < count; i++)
            {
                if (nodes[i] >= 0)
                {
                    continue;
                }
                while (free < size && taken[free])
                {
                    free++;
                }
                if (free < size)
                {
                    nodes[i] = free;
                    taken[free] = true;
                }
            }
            return nodes;
        }

        /// <summary><paramref name="arc"/> kept inside the campaigns <paramref name="mode"/> has (0 when it has none).</summary>
        public static int ClampArc(AsteroidsCampaign campaign, MissionMode mode, int arc)
        {
            int count = campaign != null ? campaign.ArcCount(mode) : 0;
            return count > 0 ? Math.Max(0, Math.Min(count - 1, arc)) : 0;
        }
    }
}
