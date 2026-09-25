using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.MemoryCards.Tests
{
    public class CampaignTest
    {
        private readonly TemporaryObjects objects = new TemporaryObjects();
        private MemoryCardsCampaign campaign;
        private MemoryCardsProgress progress;

        [SetUp]
        public void SetUp()
        {
            campaign = objects.Asset<MemoryCardsCampaign>();
            campaign.worlds = new[]
            {
                new MemoryCardsCampaign.World { title = "Farm", starsRequired = 0 },
                new MemoryCardsCampaign.World { title = "Jungle", starsRequired = 4 },
                new MemoryCardsCampaign.World { title = "Carnival", starsRequired = 0 }
            };
            campaign.endlessAfter = 2;
            for (int i = 0; i < 4; i++)
            {
                MemoryCardsLevel level = objects.Asset<MemoryCardsLevel>();
                level.world = i / 2;
                campaign.LevelList.Add(level);
            }
            MemoryCardsLevel endless = objects.Asset<MemoryCardsLevel>();
            endless.kind = LevelKind.Endless;
            endless.world = 2;
            campaign.LevelList.Add(endless);
            MemoryCardsLevel freePlay = objects.Asset<MemoryCardsLevel>();
            freePlay.kind = LevelKind.FreePlay;
            freePlay.world = 2;
            campaign.LevelList.Add(freePlay);
            progress = new MemoryCardsProgress(new TransientStrategy(), GameType.MemoryCards);
        }

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }

        [Test]
        public void FirstLevelIsOpenAndTheNextOpensWhenCompleted()
        {
            Assert.IsTrue(campaign.IsUnlocked(0, progress));
            Assert.IsFalse(campaign.IsUnlocked(1, progress));
            progress.RecordLevel(0, 1, 500);
            Assert.IsTrue(campaign.IsUnlocked(1, progress));
        }

        [Test]
        public void WorldsWaitForEnoughStars()
        {
            progress.RecordLevel(0, 1, 100);
            progress.RecordLevel(1, 2, 100);
            Assert.IsFalse(campaign.IsUnlocked(2, progress), "3 stars are not enough for the second world");
            Assert.IsFalse(campaign.IsWorldUnlocked(1, progress));
            progress.RecordLevel(1, 3, 100);
            Assert.IsTrue(campaign.IsUnlocked(2, progress));
            Assert.IsTrue(campaign.IsWorldUnlocked(1, progress));
            Assert.AreEqual(4, campaign.StarsRequired(3));
        }

        [Test]
        public void EndlessOpensAfterEnoughLevelsAndFreePlayIsAlwaysOpen()
        {
            Assert.IsFalse(campaign.IsUnlocked(4, progress));
            Assert.IsTrue(campaign.IsUnlocked(5, progress));
            progress.RecordLevel(0, 1, 100);
            progress.RecordLevel(1, 1, 100);
            Assert.IsTrue(campaign.IsUnlocked(4, progress));
        }

        [Test]
        public void OnlyCampaignLevelsEarnStars()
        {
            Assert.AreEqual(12, campaign.MaxStars);
            progress.RecordLevel(0, 3, 100);
            progress.RecordLevel(4, 3, 100);
            Assert.AreEqual(3, campaign.TotalStars(progress));
            Assert.AreEqual(1, campaign.NextLevel(0));
            Assert.AreEqual(-1, campaign.NextLevel(3), "the endless run and free play are not next levels");
            Assert.AreEqual(4, campaign.IndexOf(LevelKind.Endless));
            CollectionAssert.AreEqual(new[] { 4, 5 }, campaign.LevelsOf(2));
        }

        [Test]
        public void WithoutProgressEverythingIsOpen()
        {
            for (int i = 0; i < campaign.Count; i++)
            {
                Assert.IsTrue(campaign.IsUnlocked(i, null));
            }
        }

        [Test]
        public void ProgressKeepsRecordsAndCounters()
        {
            Assert.IsTrue(progress.RecordEndless(3, 4000));
            Assert.IsFalse(progress.RecordEndless(2, 3000));
            Assert.AreEqual(3, progress.EndlessBestBoards);
            Assert.AreEqual(4000, progress.EndlessBestScore);
            progress.RecordGame(5, 3);
            progress.RecordGame(4, 2);
            Assert.AreEqual(9, progress.SetsFound);
            Assert.AreEqual(3, progress.BestCombo);
            progress.SelectedWorld = 1;
            Assert.AreEqual(1, progress.SelectedWorld);
            progress.ResetAll(campaign.Count);
            Assert.AreEqual(0, progress.EndlessBestScore);
            Assert.AreEqual(0, progress.SetsFound);
            Assert.AreEqual(0, progress.SelectedWorld);
        }

        [Test]
        public void ValidateReportsLevelsWithoutSettings()
        {
            List<string> problems = campaign.Validate();
            Assert.AreEqual(4, problems.Count, string.Join("\n", problems));
        }
    }
}
