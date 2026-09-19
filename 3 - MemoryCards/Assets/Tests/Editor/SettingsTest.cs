using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.MemoryCards.Tests
{
    public class SettingsTest
    {
        private MemoryCardsSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<MemoryCardsSettings>();
            settings.CardsAmount = 20;
            settings.CardsPerRow = 5;
            settings.FlippedCardsPerMatch = 2;
            settings.TimePerGame = 45f;
            settings.TimeUntilUnflip = 0.8f;
            settings.PreviewTime = 2f;
            settings.Hearts = 4;
            settings.MoveLimit = 30;
            settings.MatchTimeBonus = 1.5f;
            settings.ShuffleEvery = 3;
            settings.Parade = true;
            settings.Bombs = 2;
            settings.Wilds = 0;
            settings.Clocks = 0;
            settings.Peeks = 0;
            settings.Frozen = 3;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void RulesCarryEverySetting()
        {
            RoundRules rules = settings.ToRules();
            Assert.AreEqual(20, rules.Cards);
            Assert.AreEqual(5, rules.Columns);
            Assert.AreEqual(4, rules.Rows);
            Assert.AreEqual(9, rules.Sets);
            Assert.AreEqual(45f, rules.TimeLimit);
            Assert.IsTrue(rules.Parade);
            Assert.AreEqual(3, rules.Frozen);
        }

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            var storage = new TransientStrategy();
            settings.SaveSettings(storage, "Test.");
            var loaded = ScriptableObject.CreateInstance<MemoryCardsSettings>();
            try
            {
                loaded.LoadSettings(storage, "Test.");
                Assert.AreEqual(settings.CardsAmount, loaded.CardsAmount);
                Assert.AreEqual(settings.PreviewTime, loaded.PreviewTime);
                Assert.AreEqual(settings.Hearts, loaded.Hearts);
                Assert.AreEqual(settings.MoveLimit, loaded.MoveLimit);
                Assert.AreEqual(settings.ShuffleEvery, loaded.ShuffleEvery);
                Assert.AreEqual(settings.Parade, loaded.Parade);
                Assert.AreEqual(settings.Bombs, loaded.Bombs);
                Assert.AreEqual(settings.Frozen, loaded.Frozen);
            }
            finally
            {
                Object.DestroyImmediate(loaded);
            }
        }

        [Test]
        public void CopyTakesEveryValue()
        {
            var copy = ScriptableObject.CreateInstance<MemoryCardsSettings>();
            try
            {
                copy.CopySettings(settings);
                Assert.AreEqual(settings.MatchTimeBonus, copy.MatchTimeBonus);
                Assert.AreEqual(settings.Bombs, copy.Bombs);
                Assert.AreEqual(settings.Parade, copy.Parade);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void InvalidSettingsCannotBeSaved()
        {
            settings.Bombs = 1;
            Assert.IsFalse(settings.AreSettingsValid(out _));
            Assert.Throws<GameSettingsException>(() => settings.SaveSettings(new TransientStrategy(), "Test."));
        }

        [Test]
        public void AnimalCountIsCheckedAgainstThePool()
        {
            Assert.IsTrue(settings.AreSettingsValid(9, out _));
            Assert.IsFalse(settings.AreSettingsValid(8, out string message));
            StringAssert.Contains("different animals", message);
        }

        [Test]
        public void LevelModeFollowsTheRules()
        {
            RoundRules rules = settings.ToRules();
            Assert.AreEqual(LevelMode.Parade, MemoryCardsLevel.ModeOf(LevelKind.Campaign, rules));
            rules.Parade = false;
            Assert.AreEqual(LevelMode.TimeAttack, MemoryCardsLevel.ModeOf(LevelKind.Campaign, rules));
            rules.TimeLimit = 0f;
            Assert.AreEqual(LevelMode.Survival, MemoryCardsLevel.ModeOf(LevelKind.Campaign, rules));
            rules.Hearts = 0;
            Assert.AreEqual(LevelMode.MoveLimit, MemoryCardsLevel.ModeOf(LevelKind.Campaign, rules));
            rules.MoveLimit = 0;
            Assert.AreEqual(LevelMode.Classic, MemoryCardsLevel.ModeOf(LevelKind.Campaign, rules));
            Assert.AreEqual(LevelMode.Endless, MemoryCardsLevel.ModeOf(LevelKind.Endless, rules));
            CollectionAssert.Contains(MemoryCardsLevel.Twists(settings.ToRules()), "Bombs");
        }
    }
}
