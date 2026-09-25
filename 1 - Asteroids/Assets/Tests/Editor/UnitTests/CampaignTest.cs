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
    }
}
