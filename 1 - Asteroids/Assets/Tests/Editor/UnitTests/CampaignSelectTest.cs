using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The mission select's campaign rules (<see cref="CampaignSelect"/>, and the map placement and mission line of
    /// <see cref="AsteroidsUI"/>): the mission a campaign selects, a mission's number and place on its campaign's map, the
    /// map that holds the largest campaign, and the names the lobby gives missions with several campaigns.
    /// </summary>
    public class CampaignSelectTest
    {
        private readonly TemporaryObjects objects = new TemporaryObjects();
        private AsteroidsCampaign campaign;
        private AsteroidsProgress progress;

        /// <summary>
        /// Two field campaigns and one strike campaign. Field 0: sector A (missions 0, 1), sector B behind 2 stars (2, 3, 4),
        /// then the endless mission 5. Field 1, behind 3 field stars: C (6), D (7, 8), E (9). Strike 0: S (10, 11).
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            campaign = objects.Asset<AsteroidsCampaign>();
            campaign.sectors = new[]
            {
                new AsteroidsCampaign.Sector { title = "A", mode = MissionMode.Field },
                new AsteroidsCampaign.Sector { title = "B", starsRequired = 2, mode = MissionMode.Field },
                new AsteroidsCampaign.Sector { title = "C", mode = MissionMode.Field, arc = 1 },
                new AsteroidsCampaign.Sector { title = "D", mode = MissionMode.Field, arc = 1 },
                new AsteroidsCampaign.Sector { title = "E", mode = MissionMode.Field, arc = 1 },
                new AsteroidsCampaign.Sector { title = "S", mode = MissionMode.Strike }
            };
            campaign.arcs = new[]
            {
                new AsteroidsCampaign.Arc { title = "First", mode = MissionMode.Field },
                new AsteroidsCampaign.Arc { title = "Shadow", mode = MissionMode.Strike },
                new AsteroidsCampaign.Arc { title = "Second", mode = MissionMode.Field, starsRequired = 3 }
            };
            campaign.endlessAfter = 2;
            foreach (int sector in new[] { 0, 0, 1, 1, 1 })
            {
                Add(objects.Asset<AsteroidsLevel>(), sector);
            }
            AsteroidsLevel endless = objects.Asset<AsteroidsLevel>();
            endless.objective = LevelObjective.Endless;
            campaign.LevelList.Add(endless);
            foreach (int sector in new[] { 2, 3, 3, 4 })
            {
                Add(objects.Asset<AsteroidsLevel>(), sector);
            }
            Add(objects.Asset<StrikeLevel>(), 5);
            Add(objects.Asset<StrikeLevel>(), 5);
            progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
        }

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }

        private void Add(AsteroidsLevel level, int sector)
        {
            level.sector = sector;
            if (!(level is StrikeLevel))
            {
                level.objective = LevelObjective.ClearWaves;
            }
            campaign.LevelList.Add(level);
        }


        [Test]
        public void SwitchingSelectsTheNextMissionToFly()
        {
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Field, 0, progress), Is.EqualTo(0), "Nothing flown: the first mission.");
            progress.RecordLevel(0, 1, 100);
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Field, 0, progress), Is.EqualTo(1));
            progress.RecordLevel(1, 1, 100);
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Field, 0, progress), Is.EqualTo(2), "Two stars opened sector B.");
            for (int i = 2; i < 5; i++)
            {
                progress.RecordLevel(i, 2, 100);
            }
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Field, 0, progress), Is.EqualTo(0), "Every mission has stars: the first again.");
            Assert.That(CampaignSelect.Suggested(campaign, MissionMode.Field, 0, progress), Is.EqualTo(4), "The first visit suggests the last open one then.");
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Strike, 0, progress), Is.EqualTo(10));
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Strike, 3, progress), Is.EqualTo(-1), "No such campaign.");
        }


        [Test]
        public void AClosedCampaignSelectsItsFirstMission()
        {
            Assert.IsFalse(campaign.IsArcOpen(MissionMode.Field, 1, progress));
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Field, 1, progress), Is.EqualTo(6));
            Assert.That(CampaignSelect.Suggested(campaign, MissionMode.Field, 1, progress), Is.EqualTo(6));
            progress.RecordLevel(0, 3, 100);
            Assert.IsTrue(campaign.IsUnlocked(6, progress), "Three field stars open the second campaign.");
            progress.RecordLevel(6, 1, 100);
            Assert.That(CampaignSelect.NextToFly(campaign, MissionMode.Field, 1, progress), Is.EqualTo(7));
        }


        [Test]
        public void MissionsAreNumberedWithinTheirCampaign()
        {
            Assert.That(CampaignSelect.Number(campaign, 0), Is.EqualTo(1));
            Assert.That(CampaignSelect.Number(campaign, 4), Is.EqualTo(5));
            Assert.That(CampaignSelect.Number(campaign, 5), Is.Zero, "The endless mission has no number.");
            Assert.That(CampaignSelect.Number(campaign, 6), Is.EqualTo(1), "The second campaign counts from 1 again.");
            Assert.That(CampaignSelect.Number(campaign, 9), Is.EqualTo(4));
            Assert.That(CampaignSelect.Number(campaign, 11), Is.EqualTo(2));
            Assert.That(CampaignSelect.Number(campaign, 99), Is.Zero);
        }


        [Test]
        public void AMissionsPlaceIsItsSectorsColumnAndItsRowInIt()
        {
            Assert.That(CampaignSelect.Place(campaign, 0), Is.EqualTo((0, 0)));
            Assert.That(CampaignSelect.Place(campaign, 3), Is.EqualTo((1, 1)));
            Assert.That(CampaignSelect.Place(campaign, 4), Is.EqualTo((1, 2)));
            Assert.That(CampaignSelect.Place(campaign, 5), Is.EqualTo((-1, -1)), "The endless mission has its own node.");
            Assert.That(CampaignSelect.Place(campaign, 6), Is.EqualTo((0, 0)), "The second campaign's map starts at its own first column.");
            Assert.That(CampaignSelect.Place(campaign, 8), Is.EqualTo((1, 1)));
            Assert.That(CampaignSelect.Place(campaign, 9), Is.EqualTo((2, 0)));
            Assert.That(CampaignSelect.Place(campaign, 11), Is.EqualTo((0, 1)));
        }


        [Test]
        public void TheMapHoldsTheLargestCampaignOfTheMode()
        {
            Assert.That(CampaignSelect.Layout(campaign, MissionMode.Field), Is.EqualTo((3, 3)), "Three sectors (the second campaign), three missions (sector B).");
            Assert.That(CampaignSelect.Layout(campaign, MissionMode.Strike), Is.EqualTo((1, 2)));
            Assert.That(CampaignSelect.Layout(null, MissionMode.Field), Is.EqualTo((1, 1)));
            Assert.That(CampaignSelect.ClampArc(campaign, MissionMode.Field, 7), Is.EqualTo(1));
            Assert.That(CampaignSelect.ClampArc(campaign, MissionMode.Strike, -3), Is.Zero);
            Assert.That(CampaignSelect.ClampArc(null, MissionMode.Strike, 2), Is.Zero);
        }


        [Test]
        public void PlacesFindTheirNodesAndTheRestTheFreeOnes()
        {
            var places = new List<(int, int)> { (0, 0), (0, 1), (1, 0), (1, 1), (1, 2) };
            Assert.That(CampaignSelect.Nodes(places, 3, 3), Is.EqualTo(new[] { 0, 1, 3, 4, 5 }));
            places.Add((5, 0));
            places.Add((0, 0));
            Assert.That(CampaignSelect.Nodes(places, 3, 3), Is.EqualTo(new[] { 0, 1, 3, 4, 5, 2, 6 }), "Off the map or taken: the first free node.");
            Assert.That(CampaignSelect.Nodes(places, 1, 3), Is.EqualTo(new[] { 0, 1, 2, -1, -1, -1, -1 }), "A full map places nothing more.");
            Assert.That(CampaignSelect.Nodes(null, 3, 3), Is.Empty);
        }


        [Test]
        public void TheMapPutsEachSummaryOnItsNode()
        {
            var nodes = new MissionNode[7];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = objects.Component<MissionNode>($"Node{i}");
            }
            nodes[6].endlessIcon = objects.Keep(new UnityEngine.GameObject("Infinity"));
            var summaries = new List<MissionSummary>
            {
                new MissionSummary { Index = 6, SectorSlot = 0, SectorPosition = 0 },
                new MissionSummary { Index = 7, SectorSlot = 1, SectorPosition = 0 },
                new MissionSummary { Index = 8, SectorSlot = 1, SectorPosition = 1 },
                new MissionSummary { Index = 5, Endless = true, SectorSlot = -1 }
            };
            MissionSummary?[] placed = AsteroidsUI.PlaceOnMap(summaries, nodes, 3, 2);
            Assert.That(placed[0].Value.Index, Is.EqualTo(6));
            Assert.IsNull(placed[1], "Sector C has one mission: its second node stays empty.");
            Assert.That(placed[2].Value.Index, Is.EqualTo(7));
            Assert.That(placed[3].Value.Index, Is.EqualTo(8));
            Assert.IsNull(placed[4]);
            Assert.That(placed[6].Value.Index, Is.EqualTo(5), "The endless mission goes to the node with the endless badge.");
        }


        [Test]
        public void TheMissionLineNamesTheCampaign()
        {
            Assert.That(AsteroidsUI.MissionLine("Nova Frontier", "Ember Drift", 2), Is.EqualTo("NOVA FRONTIER  -  EMBER DRIFT  -  MISSION 2"));
            Assert.That(AsteroidsUI.MissionLine(null, "Kepler Belt", 1), Is.EqualTo("KEPLER BELT  -  MISSION 1"), "Without a campaign, as before.");
        }
    }
}
