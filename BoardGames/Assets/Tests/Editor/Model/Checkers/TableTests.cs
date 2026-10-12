using System.Collections.Generic;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The lockstep table: whose turn it is, what it refuses, and that two tables fed the same log hold the same game,
    /// the clock's default move included.
    /// </summary>
    public class TableTests
    {
        private static TableSetup Setup(Variant variant, int firstSeat = 0)
        {
            var setup = new TableSetup { ruleSet = (int)variant, firstSeat = firstSeat };
            setup.seats[0].name = "Ann";
            setup.seats[1].name = "Ben";
            return setup;
        }

        [Test]
        public void SetupTellsTheSeatsTheirSides()
        {
            TableSetup english = Setup(Variant.English);
            Assert.AreEqual(Side.Dark, english.SideOf(0), "Dark moves first in the English game");
            Assert.AreEqual(Side.Light, english.SideOf(1));
            Assert.AreEqual(0, english.SeatOf(Side.Dark));
            Assert.AreEqual(1, english.SeatOf(Side.Light));
            Assert.AreEqual(2, english.Humans);
            TableSetup russian = Setup(Variant.Russian, 1);
            Assert.AreEqual(Side.Light, russian.SideOf(1));
            Assert.AreEqual(Side.Dark, russian.SideOf(0));
            russian.seats[1].computer = true;
            Assert.AreEqual(1, russian.Humans);
            TableSetup copy = russian.Clone();
            copy.seats[1].computer = false;
            copy.seats[0].name = "Cat";
            Assert.IsTrue(russian.seats[1].computer);
            Assert.AreEqual("Ann", russian.seats[0].name);
        }

        [Test]
        public void TheFirstSeatMovesFirst()
        {
            var table = new CheckersTable(Setup(Variant.English), 1u);
            Assert.AreEqual(0, table.SeatToMove);
            Assert.AreEqual(-1, table.WinnerSeat);
            Assert.IsFalse(table.IsOver);
            Assert.IsNull(table.LastCommand);
            Assert.AreEqual("Ann", table.Setup.seats[0].name);
            var second = new CheckersTable(Setup(Variant.English, 1), 1u);
            Assert.AreEqual(1, second.SeatToMove);
        }

        [Test]
        public void RefusesAMoveOutOfTurnOrOffThePath()
        {
            var table = new CheckersTable(Setup(Variant.English), 1u);
            uint before = table.Checksum();
            Move move = table.Checkers.Legal()[0];
            Assert.IsFalse(table.Apply(BoardCommand.MoveAlong(1, move), 5u));
            Assert.AreEqual(1, table.Applied);
            Assert.IsNull(table.LastCommand);
            Assert.AreEqual(before, table.Checksum());
            Assert.IsFalse(table.Apply(BoardCommand.Parse(0, CommandKind.Move, "0,9"), 6u));
            Assert.IsFalse(table.Apply(null, 7u));
            Assert.AreEqual(3, table.Applied);
            Assert.AreEqual(before, table.Checksum());
            BoardCommand command = BoardCommand.MoveAlong(0, move);
            Assert.IsTrue(table.Apply(command, 8u));
            Assert.AreSame(command, table.LastCommand);
            Assert.AreEqual(1, table.SeatToMove);
            Assert.AreNotEqual(before, table.Checksum());
            Assert.IsFalse(table.Apply(BoardCommand.MoveAlong(9, table.Checkers.Legal()[0]), 9u), "no such seat");
        }

        [Test]
        public void TwoTablesFedTheSameLogAgree()
        {
            var one = new CheckersTable(Setup(Variant.Russian), 42u);
            var two = new CheckersTable(Setup(Variant.Russian), 42u);
            var random = new SystemRandom(3);
            for (uint i = 0; i < 20 && !one.IsOver; i++)
            {
                int seat = one.SeatToMove;
                Move move = CheckersAI.Choose(one.Checkers, 1, random);
                BoardCommand command = BoardCommand.MoveAlong(seat, move);
                BoardCommand copy = BoardCommand.Parse(seat, command.Kind, command.Payload());
                Assert.IsTrue(one.Apply(command, 100u + i));
                Assert.IsTrue(two.Apply(copy, 100u + i));
                Assert.AreEqual(one.Checksum(), two.Checksum());
            }
            Assert.AreEqual(one.Applied, two.Applied);
        }

        [Test]
        public void TimeoutMakesTheSameDefaultMoveOnEveryTable()
        {
            var one = new CheckersTable(Setup(Variant.International), 7u);
            var two = new CheckersTable(Setup(Variant.International), 7u);
            var three = new CheckersTable(Setup(Variant.International), 7u);
            for (uint i = 0; i < 12; i++)
            {
                List<BoardCommand> madeOne = one.Timeout(500u + i);
                List<BoardCommand> madeTwo = two.Timeout(500u + i);
                Assert.AreEqual(1, madeOne.Count);
                Assert.AreEqual(1, madeTwo.Count);
                Assert.AreEqual(madeOne[0].Payload(), madeTwo[0].Payload());
                Assert.AreEqual(madeOne[0].seat, madeTwo[0].seat);
                Assert.AreEqual(CommandKind.Move, madeOne[0].Kind);
                Assert.AreSame(madeOne[0], one.LastCommand);
                Assert.AreEqual(one.Checksum(), two.Checksum());
                // The move the clock made is an ordinary command: a table that gets it from the log keeps up.
                Assert.IsTrue(three.Apply(BoardCommand.Parse(madeOne[0].seat, madeOne[0].Kind, madeOne[0].Payload()), 900u + i));
                Assert.AreEqual(one.Checksum(), three.Checksum());
            }
            Assert.AreEqual(12, one.Applied);
            Assert.AreEqual(12, one.Checkers.ply);
        }

        [Test]
        public void DifferentStampsMayMakeDifferentDefaultMoves()
        {
            // Easy picks at random among its near-best moves: the opening of the International game has many of them.
            var seen = new HashSet<string>();
            for (uint stamp = 1; stamp <= 40; stamp++)
            {
                var table = new CheckersTable(Setup(Variant.International), 7u);
                List<BoardCommand> made = table.Timeout(stamp);
                seen.Add(made[0].Payload());
            }
            Assert.Greater(seen.Count, 1);
        }

        [Test]
        public void ResignEndsTheGameAndTheClockStops()
        {
            var table = new CheckersTable(Setup(Variant.English), 1u);
            BoardCommand resign = BoardCommand.Resign(1);
            Assert.IsTrue(table.Apply(resign, 1u));
            Assert.AreSame(resign, table.LastCommand);
            Assert.IsTrue(table.IsOver);
            Assert.AreEqual(0, table.WinnerSeat);
            Assert.AreEqual(-1, table.SeatToMove);
            Assert.AreEqual(EndReason.Resigned, table.Checkers.Reason);
            Assert.AreEqual(0, table.Timeout(2u).Count);
            Assert.IsNull(table.LastCommand);
            Assert.IsFalse(table.Apply(BoardCommand.Resign(0), 3u));
            Assert.AreEqual(3, table.Applied);
        }

        [Test]
        public void SeatToComputerMarksTheSeat()
        {
            var table = new CheckersTable(Setup(Variant.English), 1u);
            uint before = table.Checksum();
            Assert.IsFalse(table.IsComputer(1));
            Assert.IsTrue(table.HandToComputer(1, 2, 11u));
            Assert.IsTrue(table.IsComputer(1));
            Assert.AreEqual(2, table.LevelOf(1));
            Assert.IsFalse(table.IsComputer(0));
            Assert.AreNotEqual(before, table.Checksum());
            Assert.IsFalse(table.HandToComputer(5, 1, 12u));
            Assert.IsFalse(table.IsComputer(5));
            BoardCommand command = BoardCommand.ToComputer(0, 0);
            Assert.IsTrue(table.Apply(command, 13u));
            Assert.AreSame(command, table.LastCommand);
            Assert.IsTrue(table.IsComputer(0));
            Assert.AreEqual(0, table.LevelOf(0));
            Assert.AreEqual(0, table.Setup.Humans);
            Assert.AreEqual(3, table.Applied);
            Assert.IsTrue(table.HandToComputer(0, 9, 14u));
            Assert.AreEqual(CheckersAI.Levels - 1, table.LevelOf(0), "levels are clamped");
        }

        [Test]
        public void ASavedGameGoesOn()
        {
            var played = new CheckersTable(Setup(Variant.Russian), 5u);
            for (uint i = 0; i < 6; i++)
            {
                Assert.AreEqual(1, played.Timeout(i).Count);
            }
            CheckersGame saved = played.Checkers.Clone();
            TableSetup setup = Setup(Variant.English);
            setup.seats[1].computer = true;
            var resumed = new CheckersTable(setup, 99u, saved);
            Assert.AreEqual((int)Variant.Russian, resumed.Setup.ruleSet, "the save's variant wins over the setup's");
            Assert.AreEqual(6, resumed.Checkers.ply);
            Assert.AreEqual(played.Checkers.Checksum(), resumed.Checkers.Checksum());
            Assert.AreEqual(played.SeatToMove, resumed.SeatToMove);
            Assert.IsTrue(resumed.IsComputer(1));
            Assert.AreEqual(0, resumed.Applied);
            Assert.IsTrue(resumed.Apply(BoardCommand.MoveAlong(resumed.SeatToMove, resumed.Checkers.Legal()[0]), 1u));
            Assert.AreEqual(6, saved.ply, "the table plays on a copy of the save");
        }
    }
}
