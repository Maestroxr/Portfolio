using Gamebox;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class StrikeCampaignTest
    {
        private readonly TemporaryObjects objects = new TemporaryObjects();
        private AsteroidsCampaign campaign;
        private AsteroidsProgress progress;

        /// <summary>
        /// Four field missions in two sectors (the second needs 4 field stars), the endless mission, then three strike
        /// missions in two strike sectors (the second needs 3 strike stars): the campaign's layout in small.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            campaign = objects.Asset<AsteroidsCampaign>();
            campaign.sectors = new[]
            {
                new AsteroidsCampaign.Sector { title = "One", starsRequired = 0, mode = MissionMode.Field },
                new AsteroidsCampaign.Sector { title = "Two", starsRequired = 4, mode = MissionMode.Field },
                new AsteroidsCampaign.Sector { title = "Flats", starsRequired = 0, mode = MissionMode.Strike },
                new AsteroidsCampaign.Sector { title = "Delta", starsRequired = 3, mode = MissionMode.Strike }
            };
            campaign.endlessAfter = 2;
            for (int i = 0; i < 4; i++)
            {
                AsteroidsLevel level = objects.Asset<AsteroidsLevel>();
                level.sector = i / 2;
                level.objective = LevelObjective.ClearWaves;
                campaign.LevelList.Add(level);
            }
            AsteroidsLevel endless = objects.Asset<AsteroidsLevel>();
            endless.objective = LevelObjective.Endless;
            endless.sector = 2;
            campaign.LevelList.Add(endless);
            for (int i = 0; i < 3; i++)
            {
                StrikeLevel level = objects.Asset<StrikeLevel>();
                level.sector = i < 2 ? 2 : 3;
                campaign.LevelList.Add(level);
            }
            progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
        }

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }


        [Test]
        public void ModesAreCountedApart()
        {
            Assert.That(campaign.ModeOf(0), Is.EqualTo(MissionMode.Field));
            Assert.That(campaign.ModeOf(4), Is.EqualTo(MissionMode.Field), "The endless mission is a field mission.");
            Assert.That(campaign.ModeOf(5), Is.EqualTo(MissionMode.Strike));
            Assert.That(campaign.MissionCountOf(MissionMode.Field), Is.EqualTo(5));
            Assert.That(campaign.MissionCountOf(MissionMode.Strike), Is.EqualTo(3));
            Assert.That(campaign.SectorCountOf(MissionMode.Field), Is.EqualTo(2));
            Assert.That(campaign.SectorCountOf(MissionMode.Strike), Is.EqualTo(2));
            Assert.That(campaign.FirstMission(MissionMode.Field), Is.EqualTo(0));
            Assert.That(campaign.FirstMission(MissionMode.Strike), Is.EqualTo(5));
            Assert.That(campaign.FirstSectorOf(MissionMode.Strike), Is.EqualTo(2));
            Assert.That(campaign.MaxStarsOf(MissionMode.Field), Is.EqualTo(12));
            Assert.That(campaign.MaxStarsOf(MissionMode.Strike), Is.EqualTo(9));
            Assert.That(campaign.MaxStars, Is.EqualTo(21), "MaxStars still counts every mode.");
            Assert.That(campaign.EndlessIndex, Is.EqualTo(4), "Field indices do not move.");
            Assert.That(campaign.SectorOf(4), Is.Null, "The endless mission's sector index is a strike sector, not its own.");
            Assert.That(campaign.SectorOf(5).title, Is.EqualTo("Flats"));
        }


        [Test]
        public void TheFirstStrikeMissionIsOpenFromTheStart()
        {
            Assert.IsTrue(campaign.IsUnlocked(0, progress));
            Assert.IsTrue(campaign.IsUnlocked(5, progress), "The first strike mission needs nothing.");
            Assert.IsFalse(campaign.IsUnlocked(6, progress));
            progress.RecordLevel(5, 1, 1000);
            Assert.IsTrue(campaign.IsUnlocked(6, progress));
            Assert.IsFalse(campaign.IsUnlocked(1, progress), "A strike mission does not open field missions.");
            campaign.sectors[2].starsRequired = 5;
            Assert.IsTrue(campaign.IsUnlocked(5, new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids)), "Even behind a gate.");
        }


        [Test]
        public void StrikeStarsNeverOpenFieldGatesAndTheOtherWayRound()
        {
            for (int i = 0; i < 4; i++)
            {
                progress.RecordLevel(i, 3, 100);
            }
            progress.RecordLevel(5, 1, 100);
            progress.RecordLevel(6, 1, 100);
            Assert.That(campaign.TotalStars(progress), Is.EqualTo(14), "TotalStars still counts every mode.");
            Assert.That(campaign.TotalStars(progress, MissionMode.Field), Is.EqualTo(12));
            Assert.That(campaign.TotalStars(progress, MissionMode.Strike), Is.EqualTo(2));
            Assert.That(campaign.StarsRequired(7), Is.EqualTo(3));
            Assert.IsFalse(campaign.IsUnlocked(7, progress), "Field stars do not count for a strike gate.");
            progress.RecordLevel(6, 2, 100);
            Assert.IsTrue(campaign.IsUnlocked(7, progress));

            var other = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
            other.RecordLevel(0, 1, 100);
            other.RecordLevel(1, 1, 100);
            for (int i = 5; i < 8; i++)
            {
                other.RecordLevel(i, 3, 100);
            }
            Assert.IsFalse(campaign.IsUnlocked(2, other), "Strike stars do not count for a field gate.");
            other.RecordLevel(1, 3, 100);
            Assert.IsTrue(campaign.IsUnlocked(2, other));
        }


        [Test]
        public void OnlyFieldMissionsOpenTheEndlessMission()
        {
            progress.RecordLevel(5, 3, 100);
            progress.RecordLevel(6, 3, 100);
            progress.RecordLevel(7, 3, 100);
            Assert.IsFalse(campaign.IsUnlocked(4, progress));
            progress.RecordLevel(0, 1, 100);
            progress.RecordLevel(1, 1, 100);
            Assert.IsTrue(campaign.IsUnlocked(4, progress));
        }


        [Test]
        public void NextAndPreviousStayInsideTheMode()
        {
            Assert.That(campaign.NextMission(0), Is.EqualTo(1));
            Assert.That(campaign.NextMission(3), Is.EqualTo(-1), "The last field mission leads nowhere, not into strike.");
            Assert.That(campaign.NextMission(5), Is.EqualTo(6));
            Assert.That(campaign.NextMission(7), Is.EqualTo(-1));
            progress.RecordLevel(3, 1, 100);
            Assert.IsTrue(campaign.IsUnlocked(5, progress));
            Assert.IsFalse(campaign.IsUnlocked(6, progress), "The mission before a strike mission is a strike mission.");
        }


        [Test]
        public void ThePilotRoundTripsThroughTheProgress()
        {
            Assert.IsFalse(progress.HasPilot);
            StrikeLoadout fresh = progress.LoadPilot();
            Assert.That(fresh.Money, Is.EqualTo(StrikeRules.NewPilotMoney), "A new pilot when none is saved.");
            Assert.That(fresh.Energy, Is.EqualTo(75f));

            StrikeLoadout pilot = StrikeLoadout.NewPilot();
            pilot.Money = 9999999;
            pilot.Energy = 42.5f;
            pilot.PhaseShields = 3;
            pilot.ShieldPoints = 61.25f;
            pilot.Megabombs = 4;
            pilot.Difficulty = StrikeDifficulty.Elite;
            pilot.SetCount(StrikeItem.PlasmaCannon, 1);
            pilot.SetCount(StrikeItem.AirMissiles, 3);
            pilot.SetCount(StrikeItem.TwinLaser, 1);
            pilot.SetCount(StrikeItem.IonScanner, 1);
            pilot.Special = StrikeItem.TwinLaser;
            progress.SavePilot(pilot);
            Assert.IsTrue(progress.HasPilot);

            StrikeLoadout loaded = progress.LoadPilot();
            Assert.That(loaded, Is.Not.SameAs(pilot));
            Assert.That(loaded.Money, Is.EqualTo(9999999));
            Assert.That(loaded.Energy, Is.EqualTo(42.5f));
            Assert.That(loaded.PhaseShields, Is.EqualTo(3));
            Assert.That(loaded.ShieldPoints, Is.EqualTo(61.25f));
            Assert.That(loaded.Megabombs, Is.EqualTo(4));
            Assert.That(loaded.Difficulty, Is.EqualTo(StrikeDifficulty.Elite));
            Assert.That(loaded.Special, Is.EqualTo(StrikeItem.TwinLaser));
            Assert.IsTrue(loaded.HasScanner);
            for (var item = StrikeItem.MachineGun; item <= StrikeItem.IonScanner; item++)
            {
                Assert.That(loaded.Count(item), Is.EqualTo(pilot.Count(item)), item.ToString());
            }

            progress.RecordLevel(0, 2, 100);
            progress.RecordLevel(5, 3, 100);
            progress.ResetStrike(campaign);
            Assert.IsFalse(progress.HasPilot);
            Assert.That(progress.LoadPilot().Money, Is.EqualTo(StrikeRules.NewPilotMoney));
            Assert.That(progress.Stars(5), Is.Zero, "The strike stars are forgotten.");
            Assert.That(progress.Stars(0), Is.EqualTo(2), "The field stars stay.");

            progress.SavePilot(pilot);
            progress.ResetPilot();
            Assert.IsFalse(progress.HasPilot);
            progress.SavePilot(pilot);
            progress.ResetAll(campaign.Count);
            Assert.IsFalse(progress.HasPilot, "Resetting everything forgets the pilot.");
            Assert.That(progress.Stars(0), Is.Zero);
        }


        [Test]
        public void ALoadedPilotIsKeptInsideTheRules()
        {
            var pilot = new StrikeLoadout { Money = 12345, Energy = 60f, Special = StrikeItem.Deathray };
            pilot.SetCount(StrikeItem.MachineGun, 0);
            progress.SavePilot(pilot);
            StrikeLoadout loaded = progress.LoadPilot();
            Assert.That(loaded.Count(StrikeItem.MachineGun), Is.EqualTo(1), "The machine gun is never missing.");
            Assert.That(loaded.Special, Is.EqualTo(StrikeItem.MachineGun), "An unowned special is not selected.");
            Assert.That(loaded.Money, Is.EqualTo(12345));
        }


        [Test]
        public void AStrikeLevelIsValidOnlyWithEverythingItNeeds()
        {
            StrikeLevel level = ValidLevel();
            Assert.IsTrue(level.IsLevelValid(out string message), message);
            Assert.That(level.Mode, Is.EqualTo(MissionMode.Strike));
            Assert.That(level.Length, Is.EqualTo(19 * 20f - 20f));

            level.bossAt = 330f;
            Assert.IsFalse(level.IsLevelValid(out message), "The terrain must reach 40 m past the boss.");
            level.bossAt = 300f;

            level.events[0].money = 1000;
            Assert.IsFalse(level.IsLevelValid(out message), "Money pickups have fixed values.");
            level.events[0].money = StrikeRules.Isotopes;

            level.events[0].bonus = StrikeItem.TwinLaser;
            Assert.IsFalse(level.IsLevelValid(out message), "The twin laser is too dear to be a pickup.");
            level.events[0].bonus = StrikeItem.AirMissiles;

            level.events[1].unit = StrikeUnit.Depot;
            Assert.IsFalse(level.IsLevelValid(out message), "A depot always drops something.");
            level.events[1].money = StrikeRules.SmallArms;
            Assert.IsTrue(level.IsLevelValid(out message), message);

            level.events[1].at = 320f;
            Assert.IsFalse(level.IsLevelValid(out message), "An event after the boss never spawns.");
            level.events[1].at = 60f;

            level.segments[1].kind = TerrainKind.Sea;
            Assert.IsFalse(level.IsLevelValid(out message), "The theme has no sea.");
            level.segments[1].kind = TerrainKind.Road;

            level.boss = objects.Component<Boss>("Field boss");
            Assert.IsFalse(level.IsLevelValid(out message), "A strike level needs a strike boss.");
            level.boss = objects.Component<StrikeBoss>("Strike boss");

            level.terrain = null;
            Assert.IsFalse(level.IsLevelValid(out message), "A strike level needs a theme.");
        }


        private StrikeLevel ValidLevel()
        {
            StrikeLevel level = objects.Asset<StrikeLevel>();
            var serialized = new SerializedObject(level);
            serialized.FindProperty("<Settings>k__BackingField").objectReferenceValue = objects.Asset<AsteroidSettings>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            StrikeTheme theme = objects.Asset<StrikeTheme>();
            theme.tiles = new[]
            {
                new StrikeTheme.TileSet { kind = TerrainKind.Plain, variants = new[] { objects.Component<TerrainTile>("Plain") } },
                new StrikeTheme.TileSet { kind = TerrainKind.Road, variants = new[] { objects.Component<TerrainTile>("Road") } }
            };
            level.terrain = theme;
            level.segments = new[]
            {
                new TerrainSegment { kind = TerrainKind.Plain, tiles = 10 },
                new TerrainSegment { kind = TerrainKind.Road, tiles = 9 }
            };
            level.events = new[]
            {
                new StrikeEvent { at = 20f, unit = StrikeUnit.Dart, money = StrikeRules.Isotopes },
                new StrikeEvent { at = 60f, unit = StrikeUnit.Turret }
            };
            level.bossAt = 300f;
            level.boss = objects.Component<StrikeBoss>("Boss");
            return level;
        }
    }
}
