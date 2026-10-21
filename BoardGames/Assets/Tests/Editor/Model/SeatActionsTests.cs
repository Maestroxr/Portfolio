using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// What a seat may do at a table (what the HUD and the remote control of a shared screen's room offer): the
    /// opening roll, the cube before a roll, the answer to an offered cube, the checkers of a roll, giving up, and
    /// nothing for the computer, a seat the table has not, or a game that is over.
    /// </summary>
    public class SeatActionsTests
    {
        private static TableSetup Backgammon(int firstSeat = 0, bool cube = true, bool openingRoll = true)
        {
            var setup = new TableSetup
            {
                ruleSet = (int)RuleSet.Backgammon,
                firstSeat = firstSeat,
                doublingCube = cube,
                openingRoll = openingRoll
            };
            setup.seats[0].name = "Ann";
            setup.seats[1].name = "Ben";
            return setup;
        }

        private static TableSetup Chess(bool computerSecond = false)
        {
            var setup = new TableSetup { ruleSet = (int)RuleSet.Chess, firstSeat = 0 };
            setup.seats[0].name = "Ann";
            setup.seats[1].name = computerSecond ? "Ada" : "Ben";
            setup.seats[1].computer = computerSecond;
            setup.seats[1].level = 1;
            return setup;
        }

        [Test]
        public void NothingWithoutATableOrForTheComputer()
        {
            SeatActions none = SeatActions.Of(null, 0);
            Assert.IsFalse(none.ToAct);
            Assert.IsFalse(none.MayResign);
            Assert.IsFalse(none.AnyBackgammonButton);
            Assert.AreEqual("nothing", none.ToString());

            BoardTable table = BoardTable.Create(Chess(true), 3u);
            Assert.IsFalse(SeatActions.Of(table, 1).MayResign, "the computer's seat has no buttons");
            Assert.IsFalse(SeatActions.Of(table, -1).ToAct);
            Assert.IsFalse(SeatActions.Of(table, 2).ToAct);
            Assert.IsTrue(SeatActions.Of(table, 0).ToAct, "the person's seat is to act");
        }

        [Test]
        public void TheOpeningRollIsTheFirstSeatsToMake()
        {
            BoardTable table = BoardTable.Create(Backgammon(), 3u);
            SeatActions first = SeatActions.Of(table, 0);
            Assert.IsTrue(first.ToAct);
            Assert.IsTrue(first.CanRoll);
            Assert.IsFalse(first.CanDouble, "no cube before the opening roll");
            Assert.IsFalse(first.Answering);
            Assert.IsFalse(first.Moving);
            Assert.IsTrue(first.MayResign);
            Assert.IsTrue(first.AnyBackgammonButton);
            Assert.AreEqual("to act, roll", first.ToString());

            SeatActions second = SeatActions.Of(table, 1);
            Assert.IsFalse(second.ToAct);
            Assert.IsFalse(second.CanRoll);
            Assert.IsFalse(second.AnyBackgammonButton);
            Assert.IsTrue(second.MayResign, "the seat that waits may still give up");
            Assert.AreEqual("waiting", second.ToString());
        }

        [Test]
        public void TheCubeMayBeOfferedBeforeRollingAndAnsweredByTheOther()
        {
            var table = new BackgammonTable(Backgammon(0, true, false), 5u);
            Assert.AreEqual(BackgammonPhase.Rolling, table.Backgammon.Phase);
            SeatActions first = SeatActions.Of(table, 0);
            Assert.IsTrue(first.CanRoll);
            Assert.IsTrue(first.CanDouble, "the cube is in the middle");
            Assert.IsFalse(first.Answering);

            Assert.IsTrue(table.Apply(BoardCommand.Double(0), 7u));
            SeatActions offered = SeatActions.Of(table, 1);
            Assert.IsTrue(offered.ToAct);
            Assert.IsTrue(offered.Answering);
            Assert.IsFalse(offered.CanRoll);
            Assert.IsFalse(offered.CanDouble);
            Assert.IsFalse(SeatActions.Of(table, 0).ToAct, "the one who doubled waits for the answer");

            Assert.IsTrue(table.Apply(BoardCommand.Take(1), 8u));
            SeatActions taken = SeatActions.Of(table, 0);
            Assert.IsTrue(taken.CanRoll, "the roll follows the cube");
            Assert.IsFalse(taken.CanDouble, "the cube is the other seat's now");
            Assert.IsFalse(taken.Answering);
        }

        [Test]
        public void AfterTheRollTheSeatMovesItsCheckers()
        {
            var table = new BackgammonTable(Backgammon(0, false, false), 5u);
            Assert.IsTrue(table.Apply(BoardCommand.Roll(0), 6u));
            Assert.AreEqual(BackgammonPhase.Moving, table.Backgammon.Phase);
            SeatActions moving = SeatActions.Of(table, 0);
            Assert.IsTrue(moving.ToAct);
            Assert.IsTrue(moving.Moving);
            Assert.IsFalse(moving.CanRoll);
            Assert.IsFalse(moving.CanDouble, "no cube in this game");
            Assert.IsFalse(moving.AnyBackgammonButton, "the checkers are moved on the board, not with a button");
            Assert.AreEqual("to act, moving", moving.ToString());
        }

        [Test]
        public void NothingOnceTheGameIsOver()
        {
            BoardTable table = BoardTable.Create(Chess(), 3u);
            Assert.IsTrue(SeatActions.Of(table, 1).MayResign);
            Assert.IsTrue(table.Apply(BoardCommand.Resign(1), 9u));
            Assert.IsTrue(table.Game.IsOver);
            for (int seat = 0; seat < 2; seat++)
            {
                SeatActions over = SeatActions.Of(table, seat);
                Assert.IsFalse(over.ToAct, $"seat {seat}");
                Assert.IsFalse(over.MayResign, $"seat {seat}");
                Assert.IsFalse(over.AnyBackgammonButton, $"seat {seat}");
            }
        }

        [Test]
        public void ChessHasNoDice()
        {
            BoardTable table = BoardTable.Create(Chess(), 3u);
            SeatActions white = SeatActions.Of(table, 0);
            Assert.IsTrue(white.ToAct);
            Assert.IsFalse(white.CanRoll);
            Assert.IsFalse(white.CanDouble);
            Assert.IsFalse(white.Answering);
            Assert.IsFalse(white.Moving);
            Assert.IsTrue(white.MayResign);
            Assert.AreEqual("to act", white.ToString());
            Assert.IsFalse(SeatActions.Of(table, 1).ToAct);
            Assert.IsTrue(SeatActions.Of(table, 1).MayResign);
        }
    }
}
