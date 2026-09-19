using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    public class DealerTest
    {
        [Test]
        public void DealsCompleteSetsOfDifferentAnimals()
        {
            RoundRules rules = Boards.Rules(24, 6, 3);
            Deal deal = Dealer.Create(rules, 12, new Random(1));
            Assert.AreEqual(24, deal.Cards.Count);
            Assert.AreEqual(8, deal.Animals.Count);
            Assert.AreEqual(8, deal.Animals.Distinct().Count());
            foreach (IGrouping<int, MemoryCard> set in deal.Cards.GroupBy(card => card.Animal))
            {
                Assert.AreEqual(3, set.Count());
            }
            Assert.IsTrue(deal.Animals.All(animal => animal >= 0 && animal < 12));
        }

        [Test]
        public void CardsKnowTheirIdAndSlot()
        {
            Deal deal = Dealer.Create(Boards.Rules(16), 10, new Random(2));
            for (int i = 0; i < deal.Cards.Count; i++)
            {
                Assert.AreEqual(i, deal.Cards[i].Id);
                Assert.AreEqual(i, deal.Cards[i].Slot);
                Assert.AreEqual(CardState.Hidden, deal.Cards[i].State);
            }
        }

        [Test]
        public void SpecialAndFrozenCardsAreDealt()
        {
            RoundRules rules = Boards.Rules(24, 6);
            rules.Bombs = 2;
            rules.Wilds = 1;
            rules.Clocks = 1;
            rules.Peeks = 2;
            rules.Frozen = 5;
            Deal deal = Dealer.Create(rules, 30, new Random(3));
            Assert.AreEqual(2, deal.Cards.Count(card => card.Kind == CardKind.Bomb));
            Assert.AreEqual(1, deal.Cards.Count(card => card.Kind == CardKind.Wild));
            Assert.AreEqual(1, deal.Cards.Count(card => card.Kind == CardKind.Clock));
            Assert.AreEqual(2, deal.Cards.Count(card => card.Kind == CardKind.Peek));
            Assert.AreEqual(9, deal.Animals.Count);
            Assert.AreEqual(5, deal.Cards.Count(card => card.Frozen));
            Assert.IsTrue(deal.Cards.Where(card => card.Frozen).All(card => card.IsAnimal));
            Assert.IsTrue(deal.Cards.Where(card => !card.IsAnimal).All(card => card.Animal == -1));
        }

        [Test]
        public void ParadeListsEveryAnimalOnce()
        {
            RoundRules rules = Boards.Rules(20, 5);
            rules.Parade = true;
            Deal deal = Dealer.Create(rules, 10, new Random(4));
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 10).ToList(), deal.Parade);
            Assert.IsEmpty(Dealer.Create(Boards.Rules(20, 5), 10, new Random(4)).Parade);
        }

        [Test]
        public void DealsDifferWithTheRandomSeed()
        {
            List<int> first = Dealer.Create(Boards.Rules(20, 5), 30, new Random(10)).Cards.Select(card => card.Animal).ToList();
            List<int> again = Dealer.Create(Boards.Rules(20, 5), 30, new Random(10)).Cards.Select(card => card.Animal).ToList();
            List<int> other = Dealer.Create(Boards.Rules(20, 5), 30, new Random(11)).Cards.Select(card => card.Animal).ToList();
            CollectionAssert.AreEqual(first, again);
            CollectionAssert.AreNotEqual(first, other);
        }

        [Test]
        public void RefusesRulesItCannotDeal()
        {
            Assert.Throws<ArgumentException>(() => Dealer.Create(Boards.Rules(20, 5), 5, new Random(1)), "10 pairs from 5 animals");
            Assert.Throws<ArgumentException>(() => Dealer.Create(Boards.Rules(15, 5), 30, new Random(1)), "odd card count for pairs");
            Assert.Throws<ArgumentException>(() => Dealer.Create(Boards.Rules(18, 4), 30, new Random(1)), "rows not full");
        }

        [Test]
        public void ValidationExplainsWhatIsWrong()
        {
            RoundRules rules = Boards.Rules(16);
            Assert.IsTrue(rules.IsValid(8, out _));
            rules.Bombs = 1;
            Assert.IsFalse(rules.IsValid(8, out string message));
            StringAssert.Contains("multiple of cards per match", message);
            rules.Bombs = 2;
            Assert.IsTrue(rules.IsValid(7, out _));
            rules.MoveLimit = 3;
            Assert.IsFalse(rules.IsValid(7, out message));
            StringAssert.Contains("move limit", message);
        }

        [Test]
        public void EveryEndlessBoardCanBeDealt()
        {
            for (int board = 1; board <= 40; board++)
            {
                RoundRules rules = EndlessRules.ForBoard(board);
                Assert.IsTrue(rules.IsValid(30, out string message), $"board {board}: {message}");
                Assert.DoesNotThrow(() => Dealer.Create(rules, 30, new Random(board)));
                Assert.Greater(EndlessRules.ClearBonus(board), 0f);
            }
        }
    }
}
