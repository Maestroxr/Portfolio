using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class CampaignTest
    {
        private readonly TemporaryObjects objects = new TemporaryObjects();
        private AsteroidsCampaign campaign;
        private AsteroidsProgress progress;

        [SetUp]
        public void SetUp()
        {
            campaign = objects.Asset<AsteroidsCampaign>();
            campaign.sectors = new[]
            {
                new AsteroidsCampaign.Sector { title = "One", starsRequired = 0 },
                new AsteroidsCampaign.Sector { title = "Two", starsRequired = 4 }
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
            campaign.LevelList.Add(endless);
            progress = new AsteroidsProgress(new TransientStrategy(), GameType.Asteroids);
        }

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }

        [Test]
        public void FirstMissionIsOpenAndTheNextOpensWhenCompleted()
        {
            Assert.IsTrue(campaign.IsUnlocked(0, progress));
            Assert.IsFalse(campaign.IsUnlocked(1, progress));
            progress.RecordLevel(0, 1, 500);
            Assert.IsTrue(campaign.IsUnlocked(1, progress));
        }

        [Test]
        public void SectorsNeedStars()
        {
            progress.RecordLevel(0, 1, 100);
            progress.RecordLevel(1, 1, 100);
            Assert.IsFalse(campaign.IsUnlocked(2, progress), "Two stars are not enough for the second sector.");
            progress.RecordLevel(1, 3, 100);
            Assert.That(campaign.TotalStars(progress), Is.EqualTo(4));
            Assert.IsTrue(campaign.IsUnlocked(2, progress), "Four stars open the second sector.");
            Assert.That(campaign.StarsRequired(2), Is.EqualTo(4));
        }

        [Test]
        public void EndlessOpensAfterEnoughMissionsAndEarnsNoStars()
        {
            int endless = campaign.EndlessIndex;
            Assert.That(endless, Is.EqualTo(4));
            Assert.IsFalse(campaign.IsUnlocked(endless, progress));
            progress.RecordLevel(0, 1, 100);
            progress.RecordLevel(1, 1, 100);
            Assert.IsTrue(campaign.IsUnlocked(endless, progress));
            Assert.That(campaign.MaxStars, Is.EqualTo(12), "Four campaign missions with three stars each.");
            Assert.That(campaign.NextMission(3), Is.EqualTo(-1), "The endless mission is not the next mission.");
        }

        [Test]
        public void ProgressKeepsTheBestResult()
        {
            Assert.IsTrue(progress.RecordLevel(0, 2, 1000));
            Assert.IsFalse(progress.RecordLevel(0, 1, 500), "A worse run is not a new best.");
            Assert.That(progress.Stars(0), Is.EqualTo(2));
            Assert.That(progress.BestScore(0), Is.EqualTo(1000));
            Assert.IsTrue(progress.RecordEndless(7, 12000));
            Assert.IsFalse(progress.RecordEndless(5, 8000));
            Assert.That(progress.EndlessBestWave, Is.EqualTo(7));
            progress.SelectedShip = 2;
            Assert.That(progress.SelectedShip, Is.EqualTo(2));
            progress.ResetAll(campaign.Count);
            Assert.That(progress.Stars(0), Is.Zero);
            Assert.That(progress.EndlessBestScore, Is.Zero);
            Assert.That(progress.SelectedShip, Is.Zero);
        }

        [Test]
        public void SettingsValidateAndRoundTrip()
        {
            var settings = objects.Asset<AsteroidSettings>();
            Assert.IsTrue(settings.AreSettingsValid(out _));
            settings.Lives = 0;
            Assert.IsFalse(settings.AreSettingsValid(out _));
            settings.Lives = 5;
            settings.AsteroidSpeed = 1.5f;
            settings.AsteroidExplosionRadius = 4f;
            var storage = new TransientStrategy();
            settings.SaveSettings(storage, "Test.");
            var loaded = objects.Asset<AsteroidSettings>();
            loaded.LoadSettings(storage, "Test.");
            Assert.That(loaded.Lives, Is.EqualTo(5));
            Assert.That(loaded.AsteroidSpeed, Is.EqualTo(1.5f));
            Assert.That(loaded.AsteroidExplosionRadius, Is.EqualTo(4f));
            var copy = objects.Asset<AsteroidSettings>();
            copy.CopySettings(settings);
            Assert.That(copy.Lives, Is.EqualTo(5));
            settings.AsteroidSpeed = 99f;
            Assert.Throws<GameSettingsException>(() => settings.SaveSettings(storage, "Test."));
        }

        // ------------------------------------------------------------------ several campaigns of a mode

        [Test]
        public void ACampaignWithoutArcsIsOneCampaign()
        {
            Assert.That(campaign.ArcCount(MissionMode.Field), Is.EqualTo(1), "Sectors without an arc are the first campaign.");
            Assert.That(campaign.MissionsOf(MissionMode.Field, 0), Is.EqualTo(new[] { 0, 1, 2, 3 }), "The endless mission is no campaign mission.");
            Assert.That(campaign.MaxStarsOf(MissionMode.Field, 0), Is.EqualTo(12));
            Assert.That(campaign.FirstMission(MissionMode.Field, 0), Is.EqualTo(0));
            Assert.That(campaign.ArcOf(4), Is.Zero, "The endless mission goes with the first campaign.");
            Assert.IsTrue(campaign.IsArcOpen(MissionMode.Field, 0, progress));
            Assert.IsNull(campaign.GetArc(MissionMode.Field, 0), "Nothing is listed.");
            Assert.That(campaign.ArcCount(MissionMode.Strike), Is.Zero, "No strike missions, no strike campaign.");
        }

        [Test]
        public void CampaignsAreNumberedAndCountedApart()
        {
            AddCampaigns();
            Assert.That(campaign.ArcCount(MissionMode.Field), Is.EqualTo(3));
            Assert.That(campaign.GetArc(MissionMode.Field, 1).title, Is.EqualTo("Second"));
            Assert.IsNull(campaign.GetArc(MissionMode.Field, 3));
            Assert.That(campaign.ArcOf(5), Is.EqualTo(1));
            Assert.That(campaign.ArcOf(8), Is.EqualTo(2));
            Assert.That(campaign.MissionsOf(MissionMode.Field, 1), Is.EqualTo(new[] { 5, 6, 7 }));
            Assert.That(campaign.SectorsOf(MissionMode.Field, 1), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(campaign.MaxStarsOf(MissionMode.Field, 2), Is.EqualTo(6));
            Assert.That(campaign.MaxStarsOf(MissionMode.Field), Is.EqualTo(27), "The mode-wide calls count every campaign of the mode.");
            Assert.That(campaign.FirstMission(MissionMode.Field, 1), Is.EqualTo(5));
            Assert.That(campaign.FirstMission(MissionMode.Field), Is.EqualTo(0));
            Assert.That(campaign.EndlessIndex, Is.EqualTo(4), "No older index moved.");
            progress.RecordLevel(0, 3, 100);
            progress.RecordLevel(5, 2, 100);
            Assert.That(campaign.TotalStars(progress, MissionMode.Field, 0), Is.EqualTo(3));
            Assert.That(campaign.TotalStars(progress, MissionMode.Field, 1), Is.EqualTo(2));
            Assert.That(campaign.TotalStars(progress, MissionMode.Field), Is.EqualTo(5));
        }

        [Test]
        public void TheFirstMissionOfAnOpenCampaignIsUnlocked()
        {
            AddCampaigns();
            Assert.IsTrue(campaign.IsUnlocked(5, progress), "The second campaign is open from the start.");
            Assert.IsFalse(campaign.IsUnlocked(6, progress), "Its second mission waits for its first.");
            Assert.That(campaign.ArcStarsRequired(MissionMode.Field, 2), Is.EqualTo(5));
            Assert.IsFalse(campaign.IsArcOpen(MissionMode.Field, 2, progress));
            Assert.IsFalse(campaign.IsUnlocked(8, progress), "The third campaign waits for 5 stars of the mode.");
            progress.RecordLevel(0, 3, 100);
            progress.RecordLevel(5, 2, 100);
            Assert.IsTrue(campaign.IsArcOpen(MissionMode.Field, 2, progress), "Stars of every campaign of the mode count.");
            Assert.IsTrue(campaign.IsUnlocked(8, progress));
        }

        [Test]
        public void GatesAndNextMissionsStayInsideTheirCampaign()
        {
            AddCampaigns();
            Assert.That(campaign.NextMission(3), Is.EqualTo(-1), "The last mission of the first campaign leads nowhere, not into the second.");
            Assert.That(campaign.NextMission(5), Is.EqualTo(6));
            Assert.That(campaign.NextMission(7), Is.EqualTo(-1));
            Assert.That(campaign.NextMission(8), Is.EqualTo(9));
            for (int i = 0; i < 4; i++)
            {
                progress.RecordLevel(i, 3, 100);
            }
            Assert.IsFalse(campaign.IsUnlocked(6, progress), "The mission before it is the second campaign's first, not the first campaign's last.");
            progress.RecordLevel(5, 1, 100);
            progress.RecordLevel(6, 1, 100);
            Assert.That(campaign.StarsRequired(7), Is.EqualTo(3));
            Assert.IsFalse(campaign.IsUnlocked(7, progress), "The gate counts the stars of its own campaign, not the first campaign's 12.");
            progress.RecordLevel(6, 2, 100);
            Assert.IsTrue(campaign.IsUnlocked(7, progress));
            Assert.IsTrue(campaign.IsUnlocked(campaign.EndlessIndex, progress), "Field missions of any campaign open the endless mission.");
        }

        [Test]
        public void TheShownCampaignIsRememberedPerMode()
        {
            var storage = new TransientStrategy();
            var saved = new AsteroidsProgress(storage, GameType.Asteroids);
            Assert.That(saved.ShownCampaign(MissionMode.Field), Is.Zero, "The first campaign until another was shown.");
            saved.SetShownCampaign(MissionMode.Field, 2);
            saved.SetShownCampaign(MissionMode.Strike, 1);
            var again = new AsteroidsProgress(storage, GameType.Asteroids);
            Assert.That(again.ShownCampaign(MissionMode.Field), Is.EqualTo(2));
            Assert.That(again.ShownCampaign(MissionMode.Strike), Is.EqualTo(1), "Each mode keeps its own.");
            again.ResetAll(campaign.Count);
            Assert.That(again.ShownCampaign(MissionMode.Field), Is.Zero);
            Assert.That(again.ShownCampaign(MissionMode.Strike), Is.Zero);
        }

        /// <summary>
        /// Two more field campaigns after the endless mission: the second (missions 5-7: sector "Three", then "Four" behind 3
        /// stars of that campaign) open from the start, the third (missions 8-9, sector "Five") behind 5 stars of the mode.
        /// </summary>
        private void AddCampaigns()
        {
            campaign.sectors = new[]
            {
                campaign.sectors[0],
                campaign.sectors[1],
                new AsteroidsCampaign.Sector { title = "Three", starsRequired = 0, arc = 1 },
                new AsteroidsCampaign.Sector { title = "Four", starsRequired = 3, arc = 1 },
                new AsteroidsCampaign.Sector { title = "Five", starsRequired = 0, arc = 2 }
            };
            campaign.arcs = new[]
            {
                new AsteroidsCampaign.Arc { title = "First", mode = MissionMode.Field },
                new AsteroidsCampaign.Arc { title = "Second", mode = MissionMode.Field },
                new AsteroidsCampaign.Arc { title = "Third", mode = MissionMode.Field, starsRequired = 5 }
            };
            foreach (int sector in new[] { 2, 2, 3, 4, 4 })
            {
                AsteroidsLevel level = objects.Asset<AsteroidsLevel>();
                level.sector = sector;
                level.objective = LevelObjective.ClearWaves;
                campaign.LevelList.Add(level);
            }
        }
    }
}
