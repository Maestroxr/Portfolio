using System.Linq;
using Gamebox;
using Gamebox.Lockstep;
using NUnit.Framework;

namespace Portfolio.Checkers.Tests
{
    /// <summary>
    /// A game on its way through a named save: the table setup and the position come back from the YAML text with the
    /// same checksum, seats and campaign level, and the restored game plays on.
    /// </summary>
    public class SaveTest
    {
        private static SaveData Played(Variant variant, int plies)
        {
            var data = new SaveData
            {
                setup = new TableSetup { variant = (int)variant, firstSeat = 1, clockSeconds = 60 },
                game = new CheckersGame(variant),
                campaignLevel = 2,
            };
            data.setup.seats[0].name = "Alice";
            data.setup.seats[1].name = "Computer";
            data.setup.seats[1].computer = true;
            data.setup.seats[1].level = 2;
            var random = new SystemRandom(3);
            for (int i = 0; i < plies && !data.game.IsOver; i++)
            {
                Move move = CheckersAI.Choose(data.game, 0, random);
                Assert.IsNotNull(move);
                Assert.IsTrue(data.game.Play(move));
            }
            return data;
        }

        private static SaveData RoundTrip(SaveData data, out SavedGame back)
        {
            string yaml = SavedGame.Of(GameType.Checkers, "Before the king", data, data.campaignLevel, "English, ply 9").ToYaml();
            back = SavedGame.Parse(yaml);
            return back.Read<SaveData>();
        }

        [Test]
        public void AGameComesBackFromItsYamlSaveFile()
        {
            SaveData saved = Played(Variant.English, 9);
            SaveData restored = RoundTrip(saved, out SavedGame back);
            Assert.AreEqual(GameType.Checkers, back.Game);
            Assert.AreEqual("Before the king", back.Name);
            Assert.AreEqual(2, back.Level);
            Assert.AreEqual("English, ply 9", back.Summary);
            Assert.IsNotNull(restored);
            Assert.IsNotNull(restored.game);
            Assert.IsNotNull(restored.setup);
            Assert.AreEqual(saved.game.Checksum(), restored.game.Checksum());
            Assert.AreEqual(saved.game.ply, restored.game.ply);
            Assert.AreEqual(saved.game.sideToMove, restored.game.sideToMove);
            Assert.AreEqual(saved.game.board.size, restored.game.board.size);
            CollectionAssert.AreEqual(saved.game.board.cells, restored.game.board.cells);
            CollectionAssert.AreEqual(saved.game.capturedCount, restored.game.capturedCount);
            Assert.IsNotNull(restored.game.lastMove);
            Assert.IsTrue(saved.game.lastMove.SamePathAs(restored.game.lastMove));
            Assert.AreEqual(2, restored.campaignLevel);
        }

        [Test]
        public void TheSetupOfTheTableComesBackWithIt()
        {
            SaveData saved = Played(Variant.International, 4);
            SaveData restored = RoundTrip(saved, out _);
            Assert.AreEqual((int)Variant.International, restored.setup.variant);
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
            CollectionAssert.AreEqual(saved.game.Legal().Select(move => move.ToString()).ToArray(), restored.game.Legal().Select(move => move.ToString()).ToArray());
            var table = new CheckersTable(restored.setup, restored.game, 99);
            Assert.AreEqual(restored.game.Checksum(), table.Game.Checksum());
            Assert.AreEqual(restored.setup.SeatOf(restored.game.SideToMove), table.SeatToMove);
            Assert.IsTrue(table.IsComputer(1));
            Assert.AreEqual(2, table.LevelOf(1));
            Move next = table.Game.Legal()[0];
            Assert.IsTrue(table.Apply(CheckersCommand.MoveAlong(table.SeatToMove, next), 5));
            Assert.AreEqual(saved.game.ply + 1, table.Game.ply);
        }

        [Test]
        public void AnEmptyAndAFreshSaveAreTakenGracefully()
        {
            var fresh = new SaveData { setup = new TableSetup(), game = new CheckersGame(Variant.English) };
            SaveData restored = RoundTrip(fresh, out _);
            Assert.AreEqual(fresh.game.Checksum(), restored.game.Checksum());
            Assert.AreEqual(-1, restored.campaignLevel);
            // JsonUtility gives a null class field back as an empty one: either way there is no move in it.
            Assert.IsTrue(restored.game.lastMove == null || restored.game.lastMove.path == null || restored.game.lastMove.path.Length == 0, "a fresh game has no last move");
        }
    }
}
