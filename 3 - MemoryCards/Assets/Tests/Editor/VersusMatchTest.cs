using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.MemoryCards.Tests
{
    public class VersusMatchTest
    {
        private static MemoryRound Round(string layout, int matchSize = 2)
        {
            Deal deal = Boards.Make(layout);
            return new MemoryRound(VersusMatch.RulesFor(Boards.Rules(deal.Cards.Count, 4, matchSize)), deal);
        }

        private static VersusMatch Match(int players, float turnSeconds = 20f)
        {
            var names = new List<string>();
            for (int i = 0; i < players; i++)
            {
                names.Add($"P{i + 1}");
            }
            return new VersusMatch(names, turnSeconds);
        }

        /// <summary>Flips a card for the player whose turn it is, as the manager does.</summary>
        private static VersusOutcome Flip(MemoryRound round, VersusMatch match, int card)
        {
            return match.Apply(round.Flip(round.Cards[card]), round);
        }

        [Test]
        public void ASetScoresForThePlayerAndKeepsTheTurn()
        {
            MemoryRound round = Round("0 1 0 1");
            VersusMatch match = Match(2);
            Assert.IsTrue(Flip(round, match, 0).KeepsTurn);
            VersusOutcome outcome = Flip(round, match, 2);
            Assert.IsTrue(outcome.KeepsTurn);
            Assert.AreEqual(MemoryRound.PointsPerPair, outcome.Points);
            Assert.AreEqual(0, match.Current);
            Assert.AreEqual(MemoryRound.PointsPerPair, match.Seats[0].Score);
            Assert.AreEqual(1, match.Seats[0].Sets);
            Assert.AreEqual(0, match.Seats[1].Score);
        }

        [Test]
        public void AMistakePassesTheTurnOnceItIsTurnedBack()
        {
            MemoryRound round = Round("0 1 0 1");
            VersusMatch match = Match(3);
            Flip(round, match, 0);
            VersusOutcome outcome = Flip(round, match, 1);
            Assert.IsFalse(outcome.KeepsTurn);
            Assert.IsTrue(outcome.PassesAfterMistake);
            // The cards still show: the turn is the first player's until they are turned back.
            Assert.AreEqual(0, match.Current);
            Assert.AreEqual(1, match.Seats[0].Mistakes);
            round.HideMismatch();
            match.PassTurn();
            Assert.AreEqual(1, match.Current);
            Assert.AreEqual(2, match.TurnNumber);
        }

        [Test]
        public void SetsInARowOfOnePlayerBuildTheCombo()
        {
            MemoryRound round = Round("0 1 0 1 2 2 3 3");
            VersusMatch match = Match(2);
            Flip(round, match, 0);
            Flip(round, match, 2);
            Flip(round, match, 1);
            VersusOutcome second = Flip(round, match, 3);
            Assert.AreEqual(MemoryRound.PointsPerPair * 2, second.Points);
            Assert.AreEqual(MemoryRound.PointsPerPair * 3, match.Seats[0].Score);
            Assert.AreEqual(2, match.Seats[0].BestCombo);
        }

        [Test]
        public void TheComboEndsWithTheTurn()
        {
            MemoryRound round = Round("0 1 0 1 2 2 3 3");
            VersusMatch match = Match(2);
            Flip(round, match, 0);
            Flip(round, match, 2);
            Flip(round, match, 1);
            Flip(round, match, 4);
            round.HideMismatch();
            match.PassTurn();
            Flip(round, match, 4);
            VersusOutcome outcome = Flip(round, match, 5);
            Assert.AreEqual(1, outcome.Seat);
            Assert.AreEqual(MemoryRound.PointsPerPair, outcome.Points);
        }

        [Test]
        public void ABombTakesNoMoreThanThePlayerHasAndPassesTheTurn()
        {
            MemoryRound round = Round("0 B 0 1 1 2 2 3 3");
            VersusMatch match = Match(2);
            Flip(round, match, 0);
            Flip(round, match, 2);
            Flip(round, match, 3);
            Flip(round, match, 5);
            round.HideMismatch();
            match.PassTurn();
            // The board has points on it, the second player has none to lose.
            VersusOutcome outcome = Flip(round, match, 1);
            Assert.AreEqual(0, outcome.Points);
            Assert.IsFalse(outcome.KeepsTurn);
            Assert.AreEqual(0, match.Current);
            Assert.AreEqual(0, match.Seats[1].Score);
            Assert.AreEqual(MemoryRound.PointsPerPair, match.Seats[0].Score);
        }

        [Test]
        public void ABombCostsPointsOfThePlayerWhoHitIt()
        {
            MemoryRound round = Round("0 B 0 1 1 2 2 3 3");
            VersusMatch match = Match(2);
            Flip(round, match, 0);
            Flip(round, match, 2);
            VersusOutcome outcome = Flip(round, match, 1);
            Assert.AreEqual(-MemoryRound.BombPoints, outcome.Points);
            Assert.AreEqual(MemoryRound.PointsPerPair - MemoryRound.BombPoints, match.Seats[0].Score);
            Assert.AreEqual(1, match.Current);
        }

        [Test]
        public void TheTurnClockRunsOutOnce()
        {
            VersusMatch match = Match(2, 10f);
            Assert.IsFalse(match.Tick(9.5f));
            Assert.IsTrue(match.Tick(1f));
            Assert.IsFalse(match.Tick(1f));
            match.PassTurn();
            Assert.AreEqual(10f, match.TurnLeft);
            Assert.AreEqual(1, match.Current);
        }

        [Test]
        public void ASetGivesTheWholeTurnBack()
        {
            MemoryRound round = Round("0 1 0 1");
            VersusMatch match = Match(2, 10f);
            match.Tick(6f);
            Flip(round, match, 0);
            Assert.AreEqual(4f, match.TurnLeft, 0.001f);
            Flip(round, match, 2);
            Assert.AreEqual(10f, match.TurnLeft, 0.001f);
        }

        [Test]
        public void ATurnWithoutALimitNeverRunsOut()
        {
            VersusMatch match = Match(2, 0f);
            Assert.IsFalse(match.HasTurnLimit);
            Assert.IsFalse(match.Tick(1000f));
        }

        [Test]
        public void ThePlayerWhoLeftIsSkipped()
        {
            VersusMatch match = Match(3);
            match.Seats[1].Playing = false;
            match.PassTurn();
            Assert.AreEqual(2, match.Current);
            match.PassTurn();
            Assert.AreEqual(0, match.Current);
        }

        [Test]
        public void RunningOutOfTimeTurnsTheCardsBackAndEndsTheCombo()
        {
            MemoryRound round = Round("0 1 0 1 2 2 3 3");
            round.Flip(round.Cards[0]);
            round.Flip(round.Cards[2]);
            round.Flip(round.Cards[1]);
            List<MemoryCard> hidden = round.HideRevealed();
            Assert.AreEqual(1, hidden.Count);
            Assert.AreEqual(CardState.Hidden, round.Cards[1].State);
            Assert.AreEqual(CardState.Matched, round.Cards[0].State);
            Assert.AreEqual(0, round.Combo);
            Assert.AreEqual(0, round.Revealed.Count);
            // The card can be flipped again, by the next player.
            Assert.AreEqual(FlipOutcome.Revealed, round.Flip(round.Cards[1]).Outcome);
        }

        [Test]
        public void TheStandingsGoByScoreThenSets()
        {
            VersusMatch match = Match(3);
            match.Seats[0].Score = 200;
            match.Seats[1].Score = 300;
            match.Seats[2].Score = 300;
            match.Seats[2].Sets = 1;
            List<VersusSeat> standings = match.Standings();
            Assert.AreEqual(2, standings[0].Seat);
            Assert.AreEqual(1, standings[1].Seat);
            Assert.AreEqual(0, standings[2].Seat);
            Assert.AreEqual(1, match.Winners().Count);
        }

        [Test]
        public void WhoeverLeftCannotWin()
        {
            VersusMatch match = Match(3);
            match.Seats[0].Score = 900;
            match.Seats[0].Playing = false;
            match.Seats[1].Score = 100;
            match.Seats[2].Score = 300;
            List<VersusSeat> standings = match.Standings();
            Assert.AreEqual(2, standings[0].Seat);
            Assert.AreEqual(1, standings[1].Seat);
            Assert.AreEqual(0, standings[2].Seat);
            Assert.AreEqual(1, match.Winners().Count);
            Assert.AreEqual(2, match.Winners()[0].Seat);
        }

        [Test]
        public void EqualResultsAreADraw()
        {
            VersusMatch match = Match(2);
            match.Seats[0].Score = 300;
            match.Seats[1].Score = 300;
            Assert.AreEqual(2, match.Winners().Count);
        }

        [Test]
        public void ASharedBoardDropsWhatLimitsASinglePlayer()
        {
            var board = new RoundRules
            {
                Cards = 20, Columns = 5, MatchSize = 2, TimeLimit = 90f, Hearts = 3, MoveLimit = 30, ShuffleEvery = 4, Parade = true,
                MatchTimeBonus = 3f, Clocks = 2, Peeks = 1, Wilds = 1, Bombs = 2, Frozen = 3, PreviewTime = 4f
            };
            RoundRules shared = VersusMatch.RulesFor(board);
            Assert.AreEqual(0f, shared.TimeLimit);
            Assert.AreEqual(0, shared.Hearts);
            Assert.AreEqual(0, shared.MoveLimit);
            Assert.AreEqual(0, shared.ShuffleEvery);
            Assert.IsFalse(shared.Parade);
            Assert.AreEqual(0, shared.Clocks);
            Assert.AreEqual(3, shared.Peeks);
            // The board itself stays, and so does the number of sets.
            Assert.AreEqual(board.Cards, shared.Cards);
            Assert.AreEqual(board.Sets, shared.Sets);
            Assert.AreEqual(board.Wilds, shared.Wilds);
            Assert.AreEqual(board.Bombs, shared.Bombs);
            Assert.AreEqual(board.Frozen, shared.Frozen);
            Assert.AreEqual(board.PreviewTime, shared.PreviewTime);
            Assert.AreEqual(90f, board.TimeLimit, "the rules of the level are left alone");
        }

        [Test]
        public void AVersusGameHasTwoToFourPlayers()
        {
            Assert.Throws<System.ArgumentException>(() => Match(1));
            Assert.Throws<System.ArgumentException>(() => Match(5));
            Assert.DoesNotThrow(() => Match(4));
        }

        [Test]
        public void ABombTakesAHalfFinishedSetBackWithTheTurn()
        {
            MemoryRound round = Round("0 B 0 1 1");
            VersusMatch match = Match(2);
            Flip(round, match, 0);
            FlipResult bomb = round.Flip(round.Cards[1]);
            VersusOutcome outcome = match.Apply(bomb, round);
            Assert.IsTrue(outcome.PassesNow);
            Assert.AreEqual(1, match.Current);
            Assert.AreEqual(1, bomb.FlippedBack.Count, "the card the player had face up went back down");
            Assert.AreEqual(CardState.Hidden, round.Cards[0].State);
            Assert.AreEqual(0, round.Revealed.Count, "the next player starts with a clean board");
            Assert.AreEqual(0, round.Combo);
        }

        [Test]
        public void TwoWildCardsScoreButCompleteNoSet()
        {
            MemoryRound round = Round("W W 0 0");
            VersusMatch match = Match(2);
            Flip(round, match, 0);
            VersusOutcome outcome = Flip(round, match, 1);
            Assert.AreEqual(MemoryRound.WildBonus * 2, outcome.Points);
            Assert.IsFalse(outcome.SetCompleted);
            Assert.IsTrue(outcome.KeepsTurn);
            Assert.AreEqual(0, match.Seats[0].Sets);
            Assert.AreEqual(0, round.MatchedSets);
        }

        [Test]
        public void JudgingWithoutATableIsTheSameRuleTheServerPlaysBy()
        {
            MemoryRound round = Round("0 B 0 1 1");
            VersusOutcome waiting = VersusMatch.Judge(round.Flip(round.Cards[0]), 30, round);
            Assert.IsTrue(waiting.KeepsTurn);
            Assert.IsFalse(waiting.FreshClock, "a card waiting for its set does not restart the clock");
            FlipResult bomb = round.Flip(round.Cards[1]);
            VersusOutcome outcome = VersusMatch.Judge(bomb, 30, round);
            Assert.AreEqual(-30, outcome.Points, "a bomb takes no more than the player has");
            Assert.IsTrue(outcome.PassesNow);
            Assert.AreEqual(1, bomb.FlippedBack.Count);

            MemoryRound other = Round("0 1 0 1");
            other.Flip(other.Cards[0]);
            VersusOutcome set = VersusMatch.Judge(other.Flip(other.Cards[2]), 0, other);
            Assert.IsTrue(set.FreshClock);
            Assert.IsTrue(set.SetCompleted);
            Assert.AreEqual(MemoryRound.PointsPerPair, set.Points);
        }

        [Test]
        public void EqualScoresWithMoreSetsAreNotADraw()
        {
            VersusMatch match = Match(2);
            match.Seats[0].Score = 300;
            match.Seats[0].Sets = 2;
            match.Seats[1].Score = 300;
            match.Seats[1].Sets = 3;
            Assert.AreEqual(1, match.Winners().Count);
            Assert.AreEqual(1, match.Winners()[0].Seat);
            Assert.Less(VersusMatch.Compare(match.Seats[1], match.Seats[0]), 0, "the order the server ranks the room by");
            Assert.IsFalse(VersusMatch.SharePlace(match.Seats[0], match.Seats[1]));
            Assert.IsTrue(VersusMatch.SharePlace(match.Seats[0], new VersusSeat { Score = 300, Sets = 2 }));
        }
    }
}
