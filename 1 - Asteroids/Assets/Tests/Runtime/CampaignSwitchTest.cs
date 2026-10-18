using System.Collections;
using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// Play tests of the mission select with several campaigns per mode: the game gets a copy of its campaign with two more
    /// field campaigns after its own (an open one, a copy of the first one's first two sectors, and one closed behind field
    /// stars) and one more strike campaign; the campaign bar's arrows switch the campaign of the tab shown, its map shows only that campaign's
    /// missions, the selection follows, each tab keeps its own campaign, and the device remembers them.
    /// </summary>
    public class CampaignSwitchTest
    {
        private Campaign originalCampaign;
        private readonly List<Object> made = new List<Object>();
        /// <summary>The field campaigns the game itself has: the test's campaigns come after them.</summary>
        private int fieldArcs;


        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AsteroidsGameManager manager = TestScenes.Manager;
            if (manager != null && originalCampaign != null)
            {
                manager.ReturnToMissionSelect();
                manager.CampaignAsset = originalCampaign;
                manager.Progress?.ResetAll(manager.LevelCount + 16);
                manager.ReturnToMissionSelect();
                yield return null;
            }
            originalCampaign = null;
            foreach (Object item in made)
            {
                if (item != null)
                {
                    Object.Destroy(item);
                }
            }
            made.Clear();
        }


        [UnityTest]
        public IEnumerator TheArrowsSwitchTheCampaignOfTheTabShown()
        {
            yield return LoadWithCampaigns();
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsCampaign campaign = manager.AsteroidsCampaign;
            var ui = (AsteroidsUI)manager.UI;
            CampaignSwitcher bar = ui.campaignSwitcher;
            Assert.NotNull(bar, "The mission select has a campaign bar.");

            manager.SelectMode(MissionMode.Field);
            yield return null;
            Assert.That(manager.ShownArc(MissionMode.Field), Is.Zero, "The first campaign shows first.");
            Assert.That(bar.Shown.Count, Is.EqualTo(fieldArcs + 2));
            Assert.That(bar.Shown.Arc, Is.Zero);
            Assert.That(ShownNodes(ui.missionNodes), Is.EqualTo(campaign.MissionsOf(MissionMode.Field, 0).Count + 1), "Its missions and the endless one.");

            // On past the game's own campaigns to the test's open one.
            manager.StepCampaign(fieldArcs);
            yield return null;
            List<int> second = campaign.MissionsOf(MissionMode.Field, fieldArcs);
            Assert.That(manager.ShownArc(MissionMode.Field), Is.EqualTo(fieldArcs));
            Assert.That(bar.Shown.Arc, Is.EqualTo(fieldArcs));
            Assert.That(bar.Shown.Title, Is.EqualTo("Test Second"));
            Assert.IsTrue(bar.Shown.Open);
            Assert.That(manager.LevelIndex, Is.EqualTo(second[0]), "Switching selects the campaign's next mission to fly.");
            Assert.That(ShownNodes(ui.missionNodes), Is.EqualTo(second.Count), "Only the second campaign's missions, no endless node.");
            Assert.That(ActiveColumns(ui.sectorColumns), Is.EqualTo(campaign.SectorsOf(MissionMode.Field, fieldArcs).Count), "Its sectors' columns only.");
            Assert.That(manager.Progress.ShownCampaign(MissionMode.Field), Is.EqualTo(fieldArcs), "The device remembers the campaign.");

            manager.StepCampaign(1);
            yield return null;
            Assert.That(bar.Shown.Arc, Is.EqualTo(fieldArcs + 1));
            Assert.IsFalse(bar.Shown.Open, "The third campaign waits for field stars.");
            Assert.IsNotEmpty(bar.Shown.LockReason);
            Assert.IsFalse(manager.IsUnlocked(manager.LevelIndex));
            manager.LaunchSelectedMission();
            yield return null;
            Assert.IsFalse(manager.IsMissionActive, "A closed campaign's mission does not launch.");
            Assert.IsTrue(manager.State.Is(BaseGameState.Initialization));

            manager.StepCampaign(1);
            yield return null;
            Assert.That(manager.ShownArc(MissionMode.Field), Is.EqualTo(fieldArcs + 1), "The last campaign has no next one.");

            manager.SelectMode(MissionMode.Strike);
            yield return null;
            Assert.That(bar.Shown.Mode, Is.EqualTo(MissionMode.Strike));
            Assert.That(bar.Shown.Arc, Is.Zero, "The strike tab keeps its own campaign.");
            manager.StepCampaign(1);
            yield return null;
            Assert.That(manager.ShownArc(MissionMode.Strike), Is.EqualTo(1));
            Assert.That(manager.LevelIndex, Is.EqualTo(campaign.FirstMission(MissionMode.Strike, 1)));
            if (ui.strike != null)
            {
                Assert.That(ShownNodes(ui.strike.missionNodes), Is.EqualTo(campaign.MissionsOf(MissionMode.Strike, 1).Count));
            }
            manager.SelectMode(MissionMode.Field);
            yield return null;
            Assert.That(manager.ShownArc(MissionMode.Field), Is.EqualTo(fieldArcs + 1), "Back on the field tab, its campaign is as it was left.");
            Assert.That(manager.Progress.ShownCampaign(MissionMode.Strike), Is.EqualTo(1));
        }


        [UnityTest]
        public IEnumerator TheLastMissionOfACampaignLeadsBackToTheMissionSelect()
        {
            yield return LoadWithCampaigns();
            AsteroidsGameManager manager = TestScenes.Manager;
            AsteroidsCampaign campaign = manager.AsteroidsCampaign;
            List<int> second = campaign.MissionsOf(MissionMode.Field, 1);
            int last = second[second.Count - 1];
            ((IGameController)manager.Controller).PrepareGame(LevelData.Create(last));
            yield return WaitFor(() => manager.IsMissionActive, 6f);
            Assert.IsTrue(manager.IsMissionActive, "The second campaign's last mission started.");
            manager.Objective.Defeat();
            yield return WaitFor(() => manager.State.Is(BaseGameState.Victory), 8f);
            Assert.IsTrue(manager.State.Is(BaseGameState.Victory));
            Assert.That(campaign.NextMission(last), Is.EqualTo(-1), "Nothing comes after it in its campaign.");
            manager.PlayNextMission();
            yield return null;
            Assert.IsTrue(manager.State.Is(BaseGameState.Initialization), "Next goes back to the mission select.");
            Assert.That(manager.ShownArc(MissionMode.Field), Is.EqualTo(1), "Which shows the campaign just flown.");
        }


        /// <summary>Loads the game with its progress reset and a copy of its campaign with more campaigns.</summary>
        private IEnumerator LoadWithCampaigns()
        {
            yield return TestScenes.Load(TestScenes.GameScene);
            AsteroidsGameManager manager = TestScenes.Manager;
            Assert.NotNull(manager, "The game scene has no Asteroids manager.");
            AsteroidsCampaign original = manager.AsteroidsCampaign;
            fieldArcs = original.ArcCount(MissionMode.Field);
            manager.Progress?.ResetAll(manager.LevelCount + 16);
            AsteroidsCampaign copy = Object.Instantiate(original);
            made.Add(copy);
            var sectors = new List<AsteroidsCampaign.Sector>(copy.sectors);
            var arcs = new List<AsteroidsCampaign.Arc>(copy.arcs);
            AddCampaign(original, copy, sectors, arcs, MissionMode.Field, "Test Second", 0, 2);
            AddCampaign(original, copy, sectors, arcs, MissionMode.Field, "Test Third", 99, 1);
            if (original.MissionCountOf(MissionMode.Strike) > 0)
            {
                AddCampaign(original, copy, sectors, arcs, MissionMode.Strike, "Test Strike", 0, 2);
            }
            copy.sectors = sectors.ToArray();
            copy.arcs = arcs.ToArray();
            originalCampaign = original;
            manager.CampaignAsset = copy;
            manager.ReturnToMissionSelect();
            yield return null;
        }


        /// <summary>A campaign of copies of the missions of the first <paramref name="sectorCount"/> sectors of the mode's first campaign.</summary>
        private void AddCampaign(AsteroidsCampaign original, AsteroidsCampaign copy, List<AsteroidsCampaign.Sector> sectors, List<AsteroidsCampaign.Arc> arcs,
            MissionMode mode, string title, int stars, int sectorCount)
        {
            int arc = arcs.FindAll(existing => existing.mode == mode).Count;
            arcs.Add(new AsteroidsCampaign.Arc { title = title, description = "A test campaign.", mode = mode, starsRequired = stars });
            List<int> firstSectors = original.SectorsOf(mode, 0);
            for (int s = 0; s < sectorCount && s < firstSectors.Count; s++)
            {
                AsteroidsCampaign.Sector model = original.GetSector(firstSectors[s]);
                sectors.Add(new AsteroidsCampaign.Sector { title = model.title, theme = model.theme, starsRequired = s * 2, mode = mode, arc = arc });
                foreach (int index in original.MissionsOf(mode, 0))
                {
                    AsteroidsLevel mission = original.Mission(index);
                    if (mission.Sector == firstSectors[s])
                    {
                        AsteroidsLevel clone = Object.Instantiate(mission);
                        clone.sector = sectors.Count - 1;
                        made.Add(clone);
                        copy.LevelList.Add(clone);
                    }
                }
            }
        }


        private static int ShownNodes(MissionNode[] nodes)
        {
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


        /// <summary>Sector columns shown: their headers (the first of each pair).</summary>
        private static int ActiveColumns(GameObject[] columns)
        {
            int shown = 0;
            for (int i = 0; i < columns.Length; i += 2)
            {
                if (columns[i] != null && columns[i].activeInHierarchy)
                {
                    shown++;
                }
            }
            return shown;
        }


        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds)
        {
            float waited = 0f;
            while (!condition() && waited < seconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }
    }
}
