using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    public class MemoryRoundTest
    {
        private static MemoryRound Round(string layout, RoundRules rules = null, params int[] parade)
        {
            Deal deal = Boards.Make(layout, parade);
            rules ??= Boards.Rules(deal.Cards.Count);
            return new MemoryRound(rules, deal);
        }

        [Test]
        public void MatchingPairCompletesASetAndScores()
        {
            MemoryRound round = Round("0 1 0 1");
            Assert.AreEqual(FlipOutcome.Revealed, round.Flip(round.Cards[0]).Outcome);
            FlipResult result = round.Flip(round.Cards[2]);
            Assert.AreEqual(FlipOutcome.Matched, result.Outcome);
            Assert.AreEqual(2, result.Cards.Count);
            Assert.AreEqual(CardState.Matched, round.Cards[0].State);
            Assert.AreEqual(CardState.Matched, round.Cards[2].State);
            Assert.AreEqual(1, round.MatchedSets);
            Assert.AreEqual(MemoryRound.PointsPerPair, round.Score);
            Assert.AreEqual(1, round.Moves);
        }

        [Test]
        public void ClearingEveryPairEndsTheRound()
        {
            MemoryRound round = Round("0 1 0 1");
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[2]);
            round.Flip(round.Cards[1]);
            round.Flip(round.Cards[3]);
            Assert.AreEqual(RoundEnd.Cleared, round.End);
            Assert.IsTrue(round.IsCleared);
            Assert.AreEqual(FlipOutcome.Ignored, round.Flip(round.Cards[0]).Outcome);
        }

        [Test]
        public void MismatchCountsAMistakeAndFlipsBack()
        {
            MemoryRound round = Round("0 1 0 1");
            round.Flip(round.Cards[0]);
            FlipResult result = round.Flip(round.Cards[1]);
            Assert.AreEqual(FlipOutcome.Mismatched, result.Outcome);
            Assert.IsTrue(round.MismatchShowing);
            Assert.AreEqual(1, round.Mistakes);
            Assert.AreEqual(1, round.Moves);
            Assert.AreEqual(0, round.Combo);
            Assert.AreEqual(2, round.HideMismatch().Count);
            Assert.AreEqual(CardState.Hidden, round.Cards[0].State);
            Assert.AreEqual(CardState.Hidden, round.Cards[1].State);
            Assert.IsFalse(round.MismatchShowing);
        }

        [Test]
        public void TappingAnotherCardFlipsAShowingMismatchBackFirst()
        {
            MemoryRound round = Round("0 1 0 1");
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[1]);
            FlipResult result = round.Flip(round.Cards[2]);
            Assert.AreEqual(2, result.FlippedBack.Count);
            Assert.AreEqual(FlipOutcome.Revealed, result.Outcome);
            Assert.AreEqual(CardState.Revealed, round.Cards[2].State);
            Assert.AreEqual(CardState.Hidden, round.Cards[0].State);
        }

        [Test]
        public void ConsecutiveMatchesMultiplyThePoints()
        {
            MemoryRound round = Round("0 0 1 1 2 2 3 3", Boards.Rules(8));
            round.Flip(round.Cards[0]);
            Assert.AreEqual(100, round.Flip(round.Cards[1]).Points);
            round.Flip(round.Cards[2]);
            Assert.AreEqual(200, round.Flip(round.Cards[3]).Points);
            round.Flip(round.Cards[4]);
            FlipResult third = round.Flip(round.Cards[5]);
            Assert.AreEqual(300, third.Points);
            Assert.AreEqual(3, third.Combo);
            Assert.AreEqual(3, round.BestCombo);
        }

        [Test]
        public void TripletsNeedThreeCards()
        {
            MemoryRound round = Round("0 0 0 1 1 1", Boards.Rules(6, 3, 3));
            round.Flip(round.Cards[0]);
            Assert.AreEqual(FlipOutcome.Revealed, round.Flip(round.Cards[1]).Outcome);
            FlipResult result = round.Flip(round.Cards[2]);
            Assert.AreEqual(FlipOutcome.Matched, result.Outcome);
            Assert.AreEqual(MemoryRound.PointsPerTriple, result.Points);
            Assert.AreEqual(2, round.Sets);
        }

        [Test]
        public void TripletMismatchHappensAsSoonAsTwoAnimalsDiffer()
        {
            MemoryRound round = Round("0 0 1 1 0 1", Boards.Rules(6, 3, 3));
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[1]);
            Assert.AreEqual(FlipOutcome.Mismatched, round.Flip(round.Cards[2]).Outcome);
            Assert.AreEqual(3, round.HideMismatch().Count);
        }

        [Test]
        public void WildCardMatchesEveryCardOfTheAnimal()
        {
            RoundRules rules = Boards.Rules(8);
            rules.Wilds = 1;
            rules.Cards = 7;
            rules.Columns = 7;
            MemoryRound round = Round("0 1 W 0 2 1 2", rules);
            round.Flip(round.Cards[2]);
            FlipResult result = round.Flip(round.Cards[1]);
            Assert.AreEqual(FlipOutcome.WildMatched, result.Outcome);
            Assert.AreEqual(CardState.Matched, round.Cards[5].State, "the hidden partner is matched too");
            Assert.AreEqual(3, result.Cards.Count);
            Assert.AreEqual(1, round.MatchedSets);
            Assert.AreEqual(MemoryRound.PointsPerPair + MemoryRound.WildBonus, result.Points);
        }

        [Test]
        public void WildCardAfterAnAnimalAlsoMatches()
        {
            MemoryRound round = Round("0 1 0 1 W");
            round.Flip(round.Cards[1]);
            FlipResult result = round.Flip(round.Cards[4]);
            Assert.AreEqual(FlipOutcome.WildMatched, result.Outcome);
            Assert.AreEqual(CardState.Matched, round.Cards[3].State);
            Assert.AreEqual(CardState.Hidden, round.Cards[0].State);
        }

        [Test]
        public void BombCostsAHeartTimeAndPoints()
        {
            RoundRules rules = Boards.Rules(5);
            rules.Hearts = 3;
            rules.TimeLimit = 30f;
            rules.BombPenalty = 5f;
            MemoryRound round = Round("0 0 1 1 B", rules);
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[1]);
            FlipResult result = round.Flip(round.Cards[4]);
            Assert.AreEqual(FlipOutcome.Bomb, result.Outcome);
            Assert.AreEqual(CardState.Spent, round.Cards[4].State);
            Assert.AreEqual(2, round.HeartsLeft);
            Assert.IsTrue(result.HeartLost);
            Assert.AreEqual(25f, round.Clock, 0.001f);
            Assert.AreEqual(-MemoryRound.BombPoints, result.Points);
            Assert.AreEqual(0, round.Combo);
        }

        [Test]
        public void BombAddsTimeWithoutATimeLimit()
        {
            MemoryRound round = Round("0 0 B 1 1");
            round.Tick(10f);
            FlipResult result = round.Flip(round.Cards[2]);
            Assert.AreEqual(round.Rules.BombPenalty, result.TimeChange, 0.001f);
            Assert.AreEqual(10f + round.Rules.BombPenalty, round.Clock, 0.001f);
            Assert.AreEqual(0, round.Score, "the score never drops below zero");
        }

        [Test]
        public void ClockAddsTimeToACountdownAndTakesItOffTheCount()
        {
            RoundRules timed = Boards.Rules(3);
            timed.TimeLimit = 20f;
            MemoryRound countdown = Round("0 0 C", timed);
            countdown.Flip(countdown.Cards[2]);
            Assert.AreEqual(20f + timed.ClockBonus, countdown.Clock, 0.001f);

            MemoryRound countUp = Round("0 0 C");
            countUp.Tick(30f);
            countUp.Flip(countUp.Cards[2]);
            Assert.AreEqual(30f - countUp.Rules.ClockBonus, countUp.Clock, 0.001f);
        }

        [Test]
        public void SpecialCardsDoNotBreakASetInProgress()
        {
            MemoryRound round = Round("0 C 0 P");
            round.Flip(round.Cards[0]);
            Assert.AreEqual(FlipOutcome.Clock, round.Flip(round.Cards[1]).Outcome);
            Assert.AreEqual(FlipOutcome.Peek, round.Flip(round.Cards[3]).Outcome);
            Assert.AreEqual(FlipOutcome.Matched, round.Flip(round.Cards[2]).Outcome);
            Assert.IsTrue(round.IsCleared, "special cards need not be flipped to clear the board");
        }

        [Test]
        public void FrozenCardCracksBeforeItFlips()
        {
            MemoryRound round = Round("0* 0 1 1");
            Assert.AreEqual(FlipOutcome.Cracked, round.Flip(round.Cards[0]).Outcome);
            Assert.AreEqual(CardState.Hidden, round.Cards[0].State);
            Assert.IsFalse(round.Cards[0].Frozen);
            Assert.AreEqual(FlipOutcome.Revealed, round.Flip(round.Cards[0]).Outcome);
            Assert.AreEqual(0, round.Moves, "cracking ice is free");
        }

        [Test]
        public void ParadeOnlyAcceptsTheAnimalItAsksFor()
        {
            RoundRules rules = Boards.Rules(4);
            rules.Parade = true;
            MemoryRound round = Round("0 1 0 1", rules, 1, 0);
            Assert.AreEqual(1, round.ParadeTarget);
            round.Flip(round.Cards[0]);
            FlipResult wrong = round.Flip(round.Cards[2]);
            Assert.AreEqual(FlipOutcome.WrongOrder, wrong.Outcome);
            Assert.AreEqual(1, round.Mistakes);
            round.HideMismatch();
            round.Flip(round.Cards[1]);
            Assert.AreEqual(FlipOutcome.Matched, round.Flip(round.Cards[3]).Outcome);
            Assert.AreEqual(0, round.ParadeTarget);
        }

        [Test]
        public void RunningOutOfHeartsEndsTheRound()
        {
            RoundRules rules = Boards.Rules(4);
            rules.Hearts = 1;
            MemoryRound round = Round("0 1 0 1", rules);
            round.Flip(round.Cards[0]);
            FlipResult result = round.Flip(round.Cards[1]);
            Assert.IsTrue(result.HeartLost);
            Assert.AreEqual(RoundEnd.OutOfHearts, round.End);
        }

        [Test]
        public void RunningOutOfMovesEndsTheRound()
        {
            RoundRules rules = Boards.Rules(4);
            rules.MoveLimit = 2;
            MemoryRound round = Round("0 1 0 1", rules);
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[1]);
            Assert.AreEqual(RoundEnd.None, round.End);
            round.HideMismatch();
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[1]);
            Assert.AreEqual(RoundEnd.OutOfMoves, round.End);
            Assert.AreEqual(0, round.MovesLeft);
        }

        [Test]
        public void ClearingWithTheLastMoveStillWins()
        {
            RoundRules rules = Boards.Rules(4);
            rules.MoveLimit = 2;
            MemoryRound round = Round("0 1 0 1", rules);
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[2]);
            round.Flip(round.Cards[1]);
            round.Flip(round.Cards[3]);
            Assert.AreEqual(RoundEnd.Cleared, round.End);
        }

        [Test]
        public void CountdownRunsOutOfTime()
        {
            RoundRules rules = Boards.Rules(4);
            rules.TimeLimit = 3f;
            MemoryRound round = Round("0 1 0 1", rules);
            round.Tick(2f);
            Assert.AreEqual(1f, round.Clock, 0.001f);
            round.Tick(1.5f);
            Assert.AreEqual(RoundEnd.OutOfTime, round.End);
            Assert.AreEqual(0f, round.Clock);
        }

        [Test]
        public void MatchTimeBonusExtendsTheCountdown()
        {
            RoundRules rules = Boards.Rules(4);
            rules.TimeLimit = 10f;
            rules.MatchTimeBonus = 2f;
            MemoryRound round = Round("0 0 1 1", rules);
            round.Flip(round.Cards[0]);
            FlipResult result = round.Flip(round.Cards[1]);
            Assert.AreEqual(2f, result.TimeChange, 0.001f);
            Assert.AreEqual(12f, round.Clock, 0.001f);
        }

        [Test]
        public void ShuffleIsAskedForAfterTheSetNumberOfMistakes()
        {
            RoundRules rules = Boards.Rules(6, 3);
            rules.ShuffleEvery = 2;
            MemoryRound round = Round("0 1 2 0 1 2", rules);
            round.Flip(round.Cards[0]);
            Assert.IsFalse(round.Flip(round.Cards[1]).Shuffle);
            round.HideMismatch();
            round.Flip(round.Cards[0]);
            Assert.IsTrue(round.Flip(round.Cards[2]).Shuffle);
        }

        [Test]
        public void ShuffleMovesOnlyHiddenCardsAndKeepsSlotsUnique()
        {
            MemoryRound round = Round("0 1 2 3 0 1 2 3", Boards.Rules(8));
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[4]);
            var moved = round.Shuffle(new System.Random(7));
            Assert.Greater(moved.Count, 0);
            Assert.AreEqual(0, round.Cards[0].Slot, "matched cards stay put");
            Assert.AreEqual(4, round.Cards[4].Slot);
            var slots = new System.Collections.Generic.HashSet<int>();
            foreach (MemoryCard card in round.Cards)
            {
                Assert.IsTrue(slots.Add(card.Slot));
                Assert.AreSame(card, round.CardInSlot(card.Slot));
            }
        }

        [Test]
        public void FinishBonusRewardsWhatIsLeft()
        {
            RoundRules rules = Boards.Rules(2, 2);
            rules.TimeLimit = 20f;
            rules.Hearts = 3;
            MemoryRound round = Round("0 0", rules);
            round.Tick(4.5f);
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[1]);
            int expected = 15 * MemoryRound.PointsPerSecondLeft + 3 * MemoryRound.PointsPerHeartLeft;
            Assert.AreEqual(expected, round.FinishBonus());
            int before = round.Score;
            round.ApplyFinishBonus();
            Assert.AreEqual(before + expected, round.Score);
        }

        [Test]
        public void StarGoalsCountMistakesAndTheThirdGoal()
        {
            var goals = new StarGoals { maxMistakes = 0, third = StarGoal.Combo, thirdTarget = 2 };
            MemoryRound perfect = Round("0 0 1 1");
            perfect.Flip(perfect.Cards[0]);
            perfect.Flip(perfect.Cards[1]);
            perfect.Flip(perfect.Cards[2]);
            perfect.Flip(perfect.Cards[3]);
            Assert.AreEqual(3, goals.Stars(perfect));

            MemoryRound sloppy = Round("0 1 0 1");
            sloppy.Flip(sloppy.Cards[0]);
            sloppy.Flip(sloppy.Cards[1]);
            sloppy.HideMismatch();
            sloppy.Flip(sloppy.Cards[0]);
            sloppy.Flip(sloppy.Cards[2]);
            sloppy.Flip(sloppy.Cards[1]);
            sloppy.Flip(sloppy.Cards[3]);
            Assert.AreEqual(2, goals.Stars(sloppy), "cleared with a x2 combo but one mistake");
            Assert.AreEqual(0, goals.Stars(Round("0 0")));
        }

        [Test]
        public void TimeGoalReadsTheClockOfTheRound()
        {
            var goals = new StarGoals { maxMistakes = 5, third = StarGoal.Time, thirdTarget = 10 };
            RoundRules timed = Boards.Rules(2, 2);
            timed.TimeLimit = 30f;
            MemoryRound countdown = Round("0 0", timed);
            countdown.Tick(15f);
            countdown.Flip(countdown.Cards[0]);
            countdown.Flip(countdown.Cards[1]);
            Assert.IsTrue(goals.ThirdStar(countdown), "15 s left is at least 10 s left");

            MemoryRound countUp = Round("0 0");
            countUp.Tick(15f);
            countUp.Flip(countUp.Cards[0]);
            countUp.Flip(countUp.Cards[1]);
            Assert.IsFalse(goals.ThirdStar(countUp), "15 s is not within 10 s");
        }

        [Test]
        public void RestoreBringsBackTheCountersAndHidesRevealedCards()
        {
            Deal deal = Boards.Make("0 1 0 1");
            deal.Cards[0].State = CardState.Matched;
            deal.Cards[2].State = CardState.Matched;
            deal.Cards[1].State = CardState.Revealed;
            var round = new MemoryRound(Boards.Rules(4), deal);
            round.Restore(1, 300, 2, 3, 4, 5, 0, 12f, 20f, 0, 1, 0);
            Assert.AreEqual(1, round.MatchedSets);
            Assert.AreEqual(300, round.Score);
            Assert.AreEqual(2, round.Combo);
            Assert.AreEqual(12f, round.Clock);
            Assert.AreEqual(CardState.Hidden, round.Cards[1].State);
            round.Flip(round.Cards[1]);
            Assert.AreEqual(FlipOutcome.Matched, round.Flip(round.Cards[3]).Outcome);
            Assert.IsTrue(round.IsCleared);
        }
    }
}
