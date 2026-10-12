using System.Collections.Generic;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The chess table played in step: White's seat moves first, a move is taken only from the seat to move and only
    /// when it is legal, two tables fed the same log hold the same game, and the clock's default move comes out the
    /// same on every table. A chess move travels as three numbers.
    /// </summary>
    public class ChessTableTests
    {
        private static TableSetup Setup(int firstSeat = 0)
        {
            var setup = new TableSetup { ruleSet = (int)RuleSet.Chess, firstSeat = firstSeat };
            setup.seats[0].name = "Ann";
            setup.seats[1].name = "Ben";
            return setup;
        }

        private static BoardCommand Move(int seat, string from, string to, int promotion = 0)
        {
            return BoardCommand.Play(seat, new[] { ChessGameTests.Sq(from), ChessGameTests.Sq(to), promotion });
        }

        [Test]
        public void WhiteMovesFirstFromTheFirstSeat()
        {
            var table = new ChessTable(Setup(), 1u);
            Assert.AreEqual(0, table.SeatToMove);
            Assert.AreEqual(GameMode.Chess, table.Mode);
            Assert.AreEqual(RuleSet.Chess, table.RuleSet);
            Assert.AreSame(table.Chess, table.Game);
            Assert.AreEqual(ChessNotation.StartFen, table.Chess.ToFen());
            Assert.AreEqual(-1, table.WinnerSeat);
            var second = new ChessTable(Setup(1), 1u);
            Assert.AreEqual(1, second.SeatToMove);
            Assert.AreEqual(Side.Light, second.Setup.SideOf(1));
            TableSetup draughts = Setup();
            draughts.ruleSet = (int)RuleSet.RussianDraughts;
            Assert.AreEqual((int)RuleSet.Chess, new ChessTable(draughts, 1u).Setup.ruleSet, "a chess table plays chess");
        }

        [Test]
        public void RefusesMovesOutOfTurnOrAgainstTheRules()
        {
            var table = new ChessTable(Setup(), 3u);
            uint before = table.Checksum();
            Assert.IsFalse(table.Apply(Move(1, "e2", "e4"), 10u), "Black's seat");
            Assert.IsFalse(table.Apply(Move(0, "e2", "e5"), 11u));
            Assert.IsFalse(table.Apply(Move(0, "e7", "e5"), 12u));
            Assert.IsFalse(table.Apply(BoardCommand.Parse(0, CommandKind.Move, "12,28,0,0"), 13u));
            Assert.IsFalse(table.Apply(BoardCommand.Roll(0), 14u));
            Assert.IsFalse(table.Apply(null, 15u));
            Assert.AreEqual(6, table.Applied);
            Assert.IsNull(table.LastCommand);
            Assert.AreEqual(before, table.Checksum());
            BoardCommand e4 = Move(0, "e2", "e4");
            Assert.IsTrue(table.Apply(e4, 16u));
            Assert.AreSame(e4, table.LastCommand);
            Assert.AreEqual(1, table.SeatToMove);
            Assert.AreNotEqual(before, table.Checksum());
            Assert.AreEqual("e4", table.Chess.LastMove.san);
        }

        [Test]
        public void APromotionNamesItsPiece()
        {
            var table = new ChessTable(Setup(), 3u, ChessGame.FromFen("4k3/P7/8/8/8/8/8/4K3 w - - 0 1"));
            Assert.IsFalse(table.Apply(BoardCommand.Play(0, new[] { ChessGameTests.Sq("a7"), ChessGameTests.Sq("a8") }), 1u));
            Assert.IsFalse(table.Apply(Move(0, "a7", "a8"), 2u));
            Assert.IsTrue(table.Apply(Move(0, "a7", "a8", (int)ChessPieceKind.Rook), 3u));
            Assert.AreEqual(ChessPieces.Make(Side.Light, ChessPieceKind.Rook), table.Chess[ChessGameTests.Sq("a8")]);
        }

        [Test]
        public void TwoTablesFedTheSameLogAgree()
        {
            var one = new ChessTable(Setup(), 42u);
            var two = new ChessTable(Setup(), 42u);
            var random = new SeededRandom(3);
            for (uint i = 0; i < 30 && !one.IsOver; i++)
            {
                int seat = one.SeatToMove;
                BoardCommand command = ChessAI.Choose(one.Chess, seat, 0, random);
                Assert.IsNotNull(command);
                BoardCommand copy = BoardCommand.Parse(seat, command.Kind, command.Payload());
                Assert.IsTrue(one.Apply(command, 100u + i));
                Assert.IsTrue(two.Apply(copy, 100u + i));
                Assert.AreEqual(one.Checksum(), two.Checksum());
                Assert.AreEqual(one.Chess.ToFen(), two.Chess.ToFen());
            }
            Assert.AreEqual(one.Applied, two.Applied);
        }

        [Test]
        public void TimeoutMakesTheSameDefaultMoveOnEveryTable()
        {
            var one = new ChessTable(Setup(1), 7u);
            var two = new ChessTable(Setup(1), 7u);
            var three = new ChessTable(Setup(1), 7u);
            for (uint i = 0; i < 12; i++)
            {
                int seat = one.SeatToMove;
                List<BoardCommand> madeOne = one.Timeout(500u + i);
                List<BoardCommand> madeTwo = two.Timeout(500u + i);
                Assert.AreEqual(1, madeOne.Count);
                Assert.AreEqual(1, madeTwo.Count);
                Assert.AreEqual(seat, madeOne[0].seat);
                Assert.AreEqual(madeOne[0].Payload(), madeTwo[0].Payload());
                Assert.AreEqual(CommandKind.Move, madeOne[0].Kind);
                Assert.AreSame(madeOne[0], one.LastCommand);
                Assert.AreEqual(one.Checksum(), two.Checksum());
                // The move the clock made is an ordinary command: a table that gets it from the log keeps up.
                Assert.IsTrue(three.Apply(BoardCommand.Parse(madeOne[0].seat, madeOne[0].Kind, madeOne[0].Payload()), 900u + i));
                Assert.AreEqual(one.Checksum(), three.Checksum());
            }
            Assert.AreEqual(12, one.Chess.ply);
        }

        [Test]
        public void ResigningEndsTheGame()
        {
            var table = new ChessTable(Setup(), 5u);
            Assert.IsTrue(table.Apply(Move(0, "d2", "d4"), 1u));
            Assert.IsTrue(table.Apply(BoardCommand.Resign(1), 2u));
            Assert.IsTrue(table.IsOver);
            Assert.AreEqual(0, table.WinnerSeat);
            Assert.AreEqual(-1, table.SeatToAct);
            Assert.AreEqual(EndReason.Resigned, table.Chess.Reason);
            Assert.IsFalse(table.Apply(Move(1, "d7", "d5"), 3u));
            Assert.AreEqual(0, table.Timeout(4u).Count);
        }

        [Test]
        public void CreateMakesAChessTableAndGoesOnWithASave()
        {
            Assert.IsInstanceOf<ChessTable>(BoardTable.Create(Setup(), 5u));
            ChessGame saved = ChessGameTests.Play(ChessGame.NewGame(), "e4", "c5", "Nf3");
            TableSetup other = Setup();
            other.ruleSet = (int)RuleSet.EnglishDraughts;
            BoardTable table = BoardTable.Create(other, 5u, saved);
            var chess = table as ChessTable;
            Assert.IsNotNull(chess, "the save decides the game");
            Assert.AreNotSame(saved, chess.Chess);
            Assert.AreEqual(saved.ToFen(), chess.Chess.ToFen());
            Assert.AreEqual(saved.Checksum(), chess.Chess.Checksum());
            Assert.AreEqual(1, table.SeatToAct, "Black to move");
        }

        [Test]
        public void TheComputerTakesASeatOver()
        {
            var table = new ChessTable(Setup(), 9u);
            Assert.IsTrue(table.Apply(BoardCommand.ToComputer(1, 2), 1u));
            Assert.IsTrue(table.IsComputer(1));
            Assert.AreEqual(2, table.LevelOf(1));
            Assert.IsTrue(table.Apply(Move(0, "e2", "e4"), 2u));
            BoardCommand reply = BoardAI.Choose(table.Game, 1, 1, new SeededRandom(4));
            Assert.IsNotNull(reply);
            Assert.AreEqual(1, reply.seat);
            Assert.AreEqual(3, reply.path.Length);
            Assert.IsTrue(table.Apply(reply, 3u));
        }

        [Test]
        public void AChessMoveTravelsAsThreeNumbers()
        {
            ChessGame game = ChessGame.NewGame();
            ChessMove e4 = game.FindLegal(ChessGameTests.Sq("e2"), ChessGameTests.Sq("e4"));
            BoardCommand command = BoardCommand.Play(0, e4.Path());
            Assert.AreEqual("12,28,0", command.Payload());
            BoardCommand back = BoardCommand.Parse(0, CommandKind.Move, command.Payload());
            Assert.AreEqual(CommandKind.Move, back.Kind);
            CollectionAssert.AreEqual(new[] { 12, 28, 0 }, back.path);
            Assert.AreSame(e4, game.FindLegal(back.path));
            ChessGame promote = ChessGame.FromFen("4k3/P7/8/8/8/8/8/4K3 w - - 0 1");
            ChessMove queen = promote.FindLegal(ChessGameTests.Sq("a7"), ChessGameTests.Sq("a8"), (int)ChessPieceKind.Queen);
            Assert.AreEqual("48,56,5", BoardCommand.Play(1, queen.Path()).Payload());
            Assert.AreSame(queen, promote.FindLegal(BoardCommand.Parse(1, CommandKind.Move, "48,56,5").path));
        }
    }
}
