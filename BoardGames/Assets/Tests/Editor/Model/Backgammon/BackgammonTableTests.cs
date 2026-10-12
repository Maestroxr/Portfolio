using System.Collections.Generic;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The backgammon table played in step: who may decide, the dice from the stamps (the same on every device, never a
    /// tie at the opening), the clock finishing a late seat's turn, and two tables fed the same log agreeing to the end.
    /// </summary>
    public class BackgammonTableTests
    {
        private static TableSetup Setup(int firstSeat = 0, bool cube = true, bool openingRoll = true)
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

        [Test]
        public void CreateMakesABackgammonTable()
        {
            BoardTable table = BoardTable.Create(Setup(), 3u);
            Assert.IsInstanceOf<BackgammonTable>(table);
            Assert.AreEqual(GameMode.Backgammon, table.Mode);
            Assert.AreEqual(RuleSet.Backgammon, table.RuleSet);
            Assert.AreEqual(0, table.SeatToAct, "White's seat sends the opening roll");
            var backgammon = (BackgammonTable)table;
            Assert.AreEqual(BackgammonPhase.Opening, backgammon.Backgammon.Phase);
            Assert.IsTrue(backgammon.Backgammon.cube);
            Assert.AreEqual(1, BoardTable.Create(Setup(1), 3u).SeatToAct);
            var plain = new BackgammonTable(Setup(0, false, false), 3u);
            Assert.AreEqual(BackgammonPhase.Rolling, plain.Backgammon.Phase);
            Assert.IsFalse(plain.Backgammon.cube);
            Assert.AreEqual(0, plain.SeatToAct);
        }

        [Test]
        public void RefusesDecisionsOutOfTurn()
        {
            var table = new BackgammonTable(Setup(), 5u);
            uint before = table.Checksum();
            Assert.IsFalse(table.Apply(BoardCommand.Roll(1), 1u));
            Assert.IsFalse(table.Apply(BoardCommand.Double(0), 2u), "no cube before the opening roll");
            Assert.IsFalse(table.Apply(BoardCommand.Play(0, new[] { 12, 7, 7, 6 }), 3u), "nothing to play yet");
            Assert.IsFalse(table.Apply(BoardCommand.Take(0), 4u));
            Assert.AreEqual(before, table.Checksum());
            Assert.IsNull(table.LastCommand);
            BoardCommand roll = BoardCommand.Roll(0);
            Assert.IsTrue(table.Apply(roll, 6u));
            Assert.AreSame(roll, table.LastCommand);
            BackgammonGame game = table.Backgammon;
            Assert.AreEqual(BackgammonPhase.Moving, game.Phase);
            Assert.AreNotEqual(game.die1, game.die2);
            int seat = table.SeatToAct;
            Assert.AreEqual(game.die1 > game.die2 ? 0 : 1, seat);
            int other = 1 - seat;
            Assert.IsFalse(table.Apply(BoardCommand.Roll(seat), 7u), "no second roll");
            int[] path = game.LegalPlays()[0].Path();
            Assert.IsFalse(table.Apply(BoardCommand.Play(other, path), 8u));
            Assert.IsTrue(table.Apply(BoardCommand.Play(seat, path), 9u));
            Assert.AreEqual(other, table.SeatToAct);
            Assert.IsFalse(table.Apply(BoardCommand.Play(other, path), 10u), "roll first");
            Assert.IsTrue(table.Apply(BoardCommand.Double(other), 11u));
            Assert.AreEqual(seat, table.SeatToAct);
            Assert.IsTrue(table.Apply(BoardCommand.Drop(seat), 12u));
            Assert.IsTrue(table.IsOver);
            Assert.AreEqual(other, table.WinnerSeat);
            Assert.AreEqual(-1, table.SeatToAct);
            Assert.IsFalse(table.Apply(BoardCommand.Roll(other), 13u));
        }

        [Test]
        public void TheSameStampsThrowTheSameDice()
        {
            for (uint stamp = 1; stamp <= 300; stamp++)
            {
                var one = new BackgammonTable(Setup(), 1u);
                var two = new BackgammonTable(Setup(), 777u);
                Assert.IsTrue(one.Apply(BoardCommand.Roll(0), stamp));
                Assert.IsTrue(two.Apply(BoardCommand.Roll(0), stamp));
                Assert.AreEqual(one.Backgammon.die1, two.Backgammon.die1);
                Assert.AreEqual(one.Backgammon.die2, two.Backgammon.die2);
                Assert.AreNotEqual(one.Backgammon.die1, one.Backgammon.die2, "the opening roll is never a tie");
                Assert.AreEqual(one.Checksum(), two.Checksum());
            }
        }

        [Test]
        public void ATimeOutFinishesTheTurnAndTakesTheCube()
        {
            var table = new BackgammonTable(Setup(0, true, false), 3u);
            List<BoardCommand> made = table.Timeout(10u);
            Assert.AreEqual(2, made.Count, "a roll and the play");
            Assert.AreEqual(CommandKind.Roll, made[0].Kind);
            Assert.AreEqual(CommandKind.Move, made[1].Kind);
            Assert.AreEqual(1, table.SeatToAct);
            Assert.AreEqual(BackgammonPhase.Rolling, table.Backgammon.Phase);
            Assert.AreEqual(-1, table.Backgammon.cubeOwner, "the clock never doubles");
            Assert.IsTrue(table.Apply(BoardCommand.Double(1), 11u));
            made = table.Timeout(12u);
            Assert.AreEqual(1, made.Count);
            Assert.AreEqual(CommandKind.Take, made[0].Kind);
            Assert.AreEqual(2, table.Backgammon.cubeValue);
            Assert.AreEqual(0, table.Backgammon.cubeOwner);
            Assert.AreEqual(1, table.SeatToAct, "the doubler rolls");

            var twin = new BackgammonTable(Setup(0, true, false), 3u);
            twin.Timeout(10u);
            Assert.IsTrue(twin.Apply(BoardCommand.Double(1), 11u));
            twin.Timeout(12u);
            Assert.AreEqual(table.Checksum(), twin.Checksum());

            var opening = new BackgammonTable(Setup(), 4u);
            made = opening.Timeout(20u);
            Assert.AreEqual(CommandKind.Roll, made[0].Kind);
            Assert.AreEqual(opening.Backgammon.die1 > opening.Backgammon.die2 ? 2 : 1, made.Count, "White plays at once when White won the opening roll");
        }

        [Test]
        public void TwoTablesAgreeThroughAWholeGame()
        {
            for (int g = 0; g < 2; g++)
            {
                var one = new BackgammonTable(Setup(g), 42u + (uint)g);
                var two = new BackgammonTable(Setup(g), 42u + (uint)g);
                var whims = new SeededRandom(5u + (uint)g);
                var stamps = new SeededRandom(9u + (uint)g);
                int steps = 0;
                while (!one.IsOver && steps < 3000)
                {
                    int seat = one.SeatToAct;
                    BoardCommand command = BoardAI.Choose(one.Game.CloneGame(), seat, g, whims);
                    Assert.IsNotNull(command);
                    uint stamp = stamps.Next();
                    Assert.IsTrue(one.Apply(command, stamp), command.ToString());
                    Assert.IsTrue(two.Apply(command, stamp));
                    Assert.AreEqual(one.Checksum(), two.Checksum());
                    steps++;
                    if (steps % 40 == 0)
                    {
                        // Now and then the clock decides for the seat instead.
                        uint late = stamps.Next();
                        List<BoardCommand> a = one.Timeout(late);
                        List<BoardCommand> b = two.Timeout(late);
                        Assert.AreEqual(a.Count, b.Count);
                        Assert.AreEqual(one.Checksum(), two.Checksum());
                    }
                }
                Assert.IsTrue(one.IsOver);
                Assert.GreaterOrEqual(one.WinnerSeat, 0);
                Assert.GreaterOrEqual(one.Game.Points, 1);
                Assert.AreEqual(one.Game.Checksum(), two.Game.Checksum());
            }
        }

        [Test]
        public void ASavedGameGoesOn()
        {
            var table = new BackgammonTable(Setup(), 1u);
            Assert.IsTrue(table.Apply(BoardCommand.Roll(0), 2u));
            BackgammonGame saved = table.Backgammon.Clone();
            var resumed = new BackgammonTable(Setup(), 99u, saved);
            Assert.AreNotSame(saved, resumed.Backgammon);
            Assert.AreEqual(saved.Checksum(), resumed.Backgammon.Checksum());
            Assert.AreEqual(table.SeatToAct, resumed.SeatToAct);
            Assert.IsInstanceOf<BackgammonTable>(BoardTable.Create(Setup(), 1u, saved));
            var checkersSetup = new TableSetup();
            Assert.IsInstanceOf<BackgammonTable>(BoardTable.Create(checkersSetup, 1u, saved), "the saved game decides the table");
        }
    }
}
