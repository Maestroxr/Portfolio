using System.Linq;
using Gamebox;
using Gamebox.Lockstep;
using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// A game of each of the three on its way through a named save: the table setup and the position come back from the
    /// YAML text with the same checksum, seats, options and campaign level, the save says which game it holds, and the
    /// restored game plays on.
    /// </summary>
    public class SaveTest
    {
        private static SaveData Played(Variant variant, int plies)
        {
            var data = new SaveData
            {
                setup = new TableSetup { ruleSet = (int)variant, firstSeat = 1, clockSeconds = 60 },
                checkers = new CheckersGame(variant),
                campaignLevel = 2,
            };
            data.setup.seats[0].name = "Alice";
            data.setup.seats[1].name = "Computer";
            data.setup.seats[1].computer = true;
            data.setup.seats[1].level = 2;
            var random = new SystemRandom(3);
            for (int i = 0; i < plies && !data.checkers.IsOver; i++)
            {
                Move move = CheckersAI.Choose(data.checkers, 0, random);
                Assert.IsNotNull(move);
                Assert.IsTrue(data.checkers.Play(move));
            }
            return data;
        }

        private static SaveData RoundTrip(SaveData data, out SavedGame back)
        {
            string yaml = SavedGame.Of(GameType.BoardGames, "Before the king", data, data.campaignLevel, "English, ply 9").ToYaml();
            back = SavedGame.Parse(yaml);
            return back.Read<SaveData>();
        }

        [Test]
        public void AGameComesBackFromItsYamlSaveFile()
        {
            SaveData saved = Played(Variant.English, 9);
            SaveData restored = RoundTrip(saved, out SavedGame back);
            Assert.AreEqual(GameType.BoardGames, back.Game);
            Assert.AreEqual("Before the king", back.Name);
            Assert.AreEqual(2, back.Level);
            Assert.AreEqual("English, ply 9", back.Summary);
            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.checkers);
            Assert.IsNotNull(restored.setup);
            Assert.AreEqual(saved.checkers.Checksum(), restored.checkers.Checksum());
            Assert.AreEqual(saved.checkers.ply, restored.checkers.ply);
            Assert.AreEqual(saved.checkers.sideToMove, restored.checkers.sideToMove);
            Assert.AreEqual(saved.checkers.board.size, restored.checkers.board.size);
            CollectionAssert.AreEqual(saved.checkers.board.cells, restored.checkers.board.cells);
            CollectionAssert.AreEqual(saved.checkers.capturedCount, restored.checkers.capturedCount);
            Assert.IsNotNull(restored.checkers.lastMove);
            Assert.IsTrue(saved.checkers.lastMove.SamePathAs(restored.checkers.lastMove));
            Assert.AreEqual(2, restored.campaignLevel);
        }

        [Test]
        public void TheSetupOfTheTableComesBackWithIt()
        {
            SaveData saved = Played(Variant.International, 4);
            SaveData restored = RoundTrip(saved, out _);
            Assert.AreEqual((int)Variant.International, restored.setup.ruleSet);
            Assert.AreEqual(1, restored.setup.firstSeat);
            Assert.AreEqual(60, restored.setup.clockSeconds);
            Assert.AreEqual(2, restored.setup.seats.Length);
            Assert.AreEqual("Alice", restored.setup.seats[0].name);
            Assert.IsFalse(restored.setup.seats[0].computer);
            Assert.AreEqual("Computer", restored.setup.seats[1].name);
            Assert.IsTrue(restored.setup.seats[1].computer);
            Assert.AreEqual(2, restored.setup.seats[1].level);
            Assert.AreEqual(1, restored.setup.Humans);
            Assert.AreEqual(saved.setup.SideOf(0), restored.setup.SideOf(0));
            Assert.AreEqual(saved.setup.SideOf(1), restored.setup.SideOf(1));
        }

        [Test]
        public void ARestoredGamePlaysOnLikeTheOriginal()
        {
            SaveData saved = Played(Variant.Russian, 6);
            SaveData restored = RoundTrip(saved, out _);
            CollectionAssert.AreEqual(saved.checkers.Legal().Select(move => move.ToString()).ToArray(), restored.checkers.Legal().Select(move => move.ToString()).ToArray());
            var table = new CheckersTable(restored.setup, 99, restored.checkers);
            Assert.AreEqual(restored.checkers.Checksum(), table.Checkers.Checksum());
            Assert.AreEqual(restored.setup.SeatOf(restored.checkers.SideToMove), table.SeatToMove);
            Assert.IsTrue(table.IsComputer(1));
            Assert.AreEqual(2, table.LevelOf(1));
            Move next = table.Checkers.Legal()[0];
            Assert.IsTrue(table.Apply(BoardCommand.MoveAlong(table.SeatToMove, next), 5));
            Assert.AreEqual(saved.checkers.ply + 1, table.Checkers.ply);
        }

        [Test]
        public void AnEmptyAndAFreshSaveAreTakenGracefully()
        {
            var fresh = new SaveData { setup = new TableSetup(), checkers = new CheckersGame(Variant.English) };
            SaveData restored = RoundTrip(fresh, out _);
            Assert.AreEqual(fresh.checkers.Checksum(), restored.checkers.Checksum());
            Assert.AreEqual(-1, restored.campaignLevel);
            // JsonUtility gives a null class field back as an empty one: either way there is no move in it.
            Assert.IsTrue(restored.checkers.lastMove == null || restored.checkers.lastMove.path == null || restored.checkers.lastMove.path.Length == 0, "a fresh game has no last move");
            Assert.IsTrue(restored.IsWhole());
            Assert.AreSame(restored.checkers, restored.Game);
        }

        /// <summary>Plays <paramref name="decisions"/> commands of the Easy computer on <paramref name="table"/>, a stamp each.</summary>
        private static void PlayOn(BoardTable table, int decisions)
        {
            var random = new SystemRandom(11);
            for (int i = 0; i < decisions && !table.IsOver; i++)
            {
                BoardCommand command = BoardAI.Choose(table.Game, table.SeatToAct, 0, random);
                Assert.IsNotNull(command, $"decision {i}");
                Assert.IsTrue(table.Apply(command, (uint)(100 + i)), $"decision {i}: {command}");
            }
        }

        private static TableSetup SetupOf(RuleSet ruleSet)
        {
            var setup = new TableSetup { ruleSet = (int)ruleSet, firstSeat = 1, clockSeconds = 30, doublingCube = false, openingRoll = true };
            setup.seats[0].name = "Alice";
            setup.seats[1].name = "Bob";
            setup.seats[1].computer = true;
            setup.seats[1].level = 2;
            return setup;
        }

        [Test]
        public void AChessGameComesBackFromItsYamlSaveFile()
        {
            var table = new ChessTable(SetupOf(RuleSet.Chess), 7);
            PlayOn(table, 7);
            SaveData saved = SaveData.Of(table.Setup, table.Game, -1);
            Assert.IsNotNull(saved.chess);
            Assert.IsNull(saved.checkers);
            Assert.IsNull(saved.backgammon);
            Assert.AreNotSame(table.Chess, saved.chess, "the save holds a copy");
            SaveData restored = RoundTrip(saved, out SavedGame back);
            Assert.AreEqual(-1, back.Level, "the save of a game outside the campaign");
            Assert.IsTrue(restored.IsWhole());
            Assert.AreEqual(GameMode.Chess, restored.setup.Mode);
            Assert.IsInstanceOf<ChessGame>(restored.Game);
            Assert.AreEqual(table.Chess.Checksum(), restored.chess.Checksum());
            Assert.AreEqual(table.Chess.ToFen(), restored.chess.ToFen());
            Assert.AreEqual(table.Chess.ply, restored.chess.ply);
            CollectionAssert.AreEqual(table.Chess.positions, restored.chess.positions);
            Assert.IsNotNull(restored.chess.LastMove);
            Assert.IsTrue(table.Chess.LastMove.SameAs(restored.chess.LastMove));
            Assert.AreEqual(1, restored.setup.firstSeat);
            Assert.AreEqual("Bob", restored.setup.seats[1].name);

            // The restored game plays on exactly like the original.
            var again = (ChessTable)BoardTable.Create(restored.setup, 7, restored.Game);
            Assert.AreEqual(table.Chess.Checksum(), again.Chess.Checksum());
            Assert.AreEqual(table.SeatToAct, again.SeatToAct);
            ChessMove next = again.Chess.Legal()[0];
            Assert.IsTrue(again.Apply(BoardCommand.Play(again.SeatToAct, next.Path()), 5));
            Assert.IsTrue(table.Apply(BoardCommand.Play(table.SeatToAct, next.Path()), 5));
            Assert.AreEqual(table.Chess.Checksum(), again.Chess.Checksum());
        }

        [Test]
        public void ABackgammonGameComesBackWithItsDiceAndCube()
        {
            TableSetup setup = SetupOf(RuleSet.Backgammon);
            setup.doublingCube = true;
            var table = new BackgammonTable(setup, 9);
            PlayOn(table, 9);
            SaveData saved = SaveData.Of(table.Setup, table.Game, 10);
            SaveData restored = RoundTrip(saved, out SavedGame back);
            Assert.AreEqual(10, back.Level);
            Assert.AreEqual(10, restored.campaignLevel);
            Assert.IsTrue(restored.IsWhole());
            Assert.IsInstanceOf<BackgammonGame>(restored.Game);
            BackgammonGame original = table.Backgammon;
            BackgammonGame copy = restored.backgammon;
            Assert.AreEqual(original.Checksum(), copy.Checksum());
            CollectionAssert.AreEqual(original.board, copy.board);
            CollectionAssert.AreEqual(original.bar, copy.bar);
            CollectionAssert.AreEqual(original.off, copy.off);
            Assert.AreEqual(original.phase, copy.phase);
            Assert.AreEqual(original.die1, copy.die1);
            Assert.AreEqual(original.die2, copy.die2);
            Assert.AreEqual(original.cube, copy.cube);
            Assert.AreEqual(original.cubeValue, copy.cubeValue);
            Assert.AreEqual(original.cubeOwner, copy.cubeOwner);
            Assert.AreEqual(original.Turn, copy.Turn);
            Assert.IsTrue(restored.setup.doublingCube);
            Assert.IsTrue(restored.setup.openingRoll);

            // The same stamps throw the same dice on the original and the restored table.
            var again = (BackgammonTable)BoardTable.Create(restored.setup, 9, restored.Game);
            Assert.AreEqual(original.Checksum(), again.Backgammon.Checksum());
            var random = new SystemRandom(4);
            for (int i = 0; i < 6 && !table.IsOver; i++)
            {
                BoardCommand command = BoardAI.Choose(table.Game, table.SeatToAct, 0, random);
                Assert.IsTrue(table.Apply(command, (uint)(500 + i)));
                Assert.IsTrue(again.Apply(command, (uint)(500 + i)), $"the restored table takes {command}");
                Assert.AreEqual(table.Backgammon.Checksum(), again.Backgammon.Checksum(), $"after {command}");
            }
        }

        [Test]
        public void ASaveIsWholeOnlyWithTheGameOfItsRuleSet()
        {
            var chess = new SaveData { setup = SetupOf(RuleSet.Chess), checkers = new CheckersGame(Variant.English) };
            Assert.IsFalse(chess.IsWhole(), "a chess save without its chess game");
            Assert.IsNull(chess.Game);
            chess.chess = new ChessGame();
            Assert.IsFalse(chess.IsWhole(), "an empty chess game (what JsonUtility gives back for a missing one) has no kings");
            chess.chess = ChessGame.NewGame();
            Assert.IsTrue(chess.IsWhole());
            Assert.AreSame(chess.chess, chess.Game);
            chess.setup.firstSeat = 2;
            Assert.IsFalse(chess.IsWhole(), "the first seat is one of the two");
            chess.setup.firstSeat = 1;
            Assert.IsTrue(chess.IsWhole());
            chess.chess.cells = new sbyte[10];
            Assert.IsFalse(chess.IsWhole(), "a chess board has 64 squares");
            var backgammon = new SaveData { setup = SetupOf(RuleSet.Backgammon), backgammon = new BackgammonGame() };
            Assert.IsFalse(backgammon.IsWhole(), "an empty backgammon board has no checkers");
            backgammon.backgammon = BackgammonGame.NewGame(true, true, Side.Light);
            Assert.IsTrue(backgammon.IsWhole());
            backgammon.backgammon.board = new int[3];
            Assert.IsFalse(backgammon.IsWhole(), "a backgammon board has 24 points");
            var oneSeat = new SaveData { setup = SetupOf(RuleSet.EnglishDraughts), checkers = new CheckersGame(Variant.English) };
            oneSeat.setup.seats = new[] { new TableSeat() };
            Assert.IsFalse(oneSeat.IsWhole(), "a table has two seats");
            Assert.IsFalse(new SaveData().IsWhole());
        }
    }
}
