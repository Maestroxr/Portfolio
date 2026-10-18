using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The campaign steps of the tour. <c>campaigns</c> shows every campaign of both modes on the mission select the way the
    /// campaign bar switches them (each selects its next mission to fly) and pictures each, then the details of a mission
    /// the campaign still keeps locked, and goes back to the campaign the tab showed before. <c>field:c&lt;n&gt;</c>,
    /// <c>strike:c&lt;n&gt;</c> and <c>boss:c&lt;n&gt;</c> fly missions of campaign n of a mode (0 = the first) by their number in
    /// it: <c>:&lt;number&gt;</c> (1 = its first, the default) or <c>:all</c> for every one of them in order.
    /// </summary>
    public partial class AsteroidsTour
    {
        /// <summary>Whether a step's argument names a campaign ("c1").</summary>
        private static bool IsCampaignArgument(string argument)
        {
            return argument.Length > 1 && argument[0] == 'c' && char.IsDigit(argument[1]);
        }


        /// <summary>Flies missions of the campaign the step names (<c>field:c1:3</c>, <c>strike:c2:all</c>, <c>boss:c0</c>).</summary>
        private IEnumerator CampaignMissions(MissionMode mode, string[] parts, bool boss)
        {
            AsteroidsCampaign campaign = manager.AsteroidsCampaign;
            int arc = ParseInt(parts[1].Substring(1), 0);
            List<int> missions = campaign != null ? campaign.MissionsOf(mode, arc) : new List<int>();
            if (missions.Count == 0)
            {
                Note($"no {mode} campaign {arc} (the mode has {(campaign != null ? campaign.ArcCount(mode) : 0)})");
                yield break;
            }
            string which = parts.Length > 2 ? parts[2] : "1";
            var numbers = new List<int>();
            if (which == "all")
            {
                for (int n = 1; n <= missions.Count; n++)
                {
                    numbers.Add(n);
                }
            }
            else
            {
                numbers.Add(ParseInt(which, 1));
            }
            Note($"campaign {mode} {arc} \"{AsteroidsGameManager.CampaignTitle(campaign, mode, arc)}\": {missions.Count} missions, flying {string.Join(" ", numbers)}");
            foreach (int number in numbers)
            {
                if (number < 1 || number > missions.Count)
                {
                    Note($"no mission {number} in {mode} campaign {arc} (it has {missions.Count})");
                    continue;
                }
                int index = missions[number - 1];
                if (mode == MissionMode.Field)
                {
                    yield return Field(index);
                }
                else
                {
                    yield return FlyStrike(index, boss ? $"c{arc}boss{number}" : $"c{arc}m{number}", boss);
                }
            }
        }


        /// <summary>
        /// The mission select of every campaign of both modes: switched to like the campaign bar does, checked against what
        /// the bar and the map show, and pictured; then a locked mission's details where the campaign has one.
        /// </summary>
        private IEnumerator Campaigns()
        {
            yield return ToMenu();
            AsteroidsCampaign campaign = manager.AsteroidsCampaign;
            var screens = manager.UI as AsteroidsUI;
            if (campaign == null || screens == null)
            {
                Note("campaigns: no campaign or no AsteroidsUI");
                yield break;
            }
            foreach (MissionMode mode in new[] { MissionMode.Field, MissionMode.Strike })
            {
                string tab = mode == MissionMode.Strike ? "strike" : "field";
                if (campaign.MissionCountOf(mode) == 0)
                {
                    Note($"campaigns: the {tab} tab has no missions");
                    continue;
                }
                manager.SelectMode(mode);
                yield return Wait(0.8f);
                int before = manager.ShownArc(mode);
                int count = campaign.ArcCount(mode);
                for (int arc = 0; arc < count; arc++)
                {
                    manager.SelectCampaign(mode, arc);
                    yield return Wait(1.2f);
                    CampaignSummary shown = manager.CampaignSummaryOf(mode);
                    List<int> missions = campaign.MissionsOf(mode, arc);
                    int nodes = ShownNodes(screens, mode);
                    Note($"campaigns: {tab} {arc + 1}/{count} \"{shown.Title}\" stars {shown.Stars}/{shown.MaxStars}, open {shown.Open}, " +
                         $"selected {manager.LevelIndex} ({(manager.Mission != null ? manager.Mission.Title : "-")}), missions {missions.Count}, " +
                         $"sectors {campaign.SectorsOf(mode, arc).Count}, nodes shown {nodes}" + (shown.Open ? string.Empty : $", lock: {shown.LockReason}"));
                    CampaignSummary bar = screens.campaignSwitcher != null ? screens.campaignSwitcher.Shown : default;
                    int expected = missions.Count + (mode == MissionMode.Field && arc == 0 && campaign.EndlessIndex >= 0 ? 1 : 0);
                    if (manager.ShownArc(mode) != arc || bar.Arc != arc || bar.Mode != mode || !missions.Contains(manager.LevelIndex) || nodes != expected)
                    {
                        errors++;
                        Note($"FAIL campaigns: {tab} campaign {arc} shows arc {manager.ShownArc(mode)} (bar {bar.Mode} {bar.Arc}), " +
                             $"selected {manager.LevelIndex}, nodes {nodes} of {expected}");
                    }
                    yield return Shot($"campaigns_{tab}_c{arc}");
                    int locked = missions.FindLast(index => !manager.IsUnlocked(index));
                    if (locked >= 0)
                    {
                        manager.SelectMission(locked);
                        yield return Wait(0.6f);
                        yield return Shot($"campaigns_{tab}_c{arc}_locked");
                    }
                }
                manager.SelectCampaign(mode, before);
                yield return Wait(0.5f);
            }
            manager.SelectMode(MissionMode.Field);
            yield return Wait(0.5f);
        }


        /// <summary>The mission nodes the map of <paramref name="mode"/>'s tab shows.</summary>
        private static int ShownNodes(AsteroidsUI screens, MissionMode mode)
        {
            MissionNode[] nodes = mode == MissionMode.Strike ? screens.strike != null ? screens.strike.missionNodes : new MissionNode[0] : screens.missionNodes;
            int shown = 0;
            foreach (MissionNode node in nodes)
            {
                if (node != null && node.gameObject.activeInHierarchy)
                {
                    shown++;
                }
            }
            return shown;
        }
    }
}
