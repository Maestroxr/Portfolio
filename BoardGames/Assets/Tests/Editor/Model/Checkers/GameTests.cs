using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// A game from start to end: who moves, how it is won, resigned, drawn or called off, what the last move and the
    /// legal moves tell, and that a copy and a checksum are what a game in step needs.
    /// </summary>
    public class GameTests
    {
        [Test]
        public void StartsWithTheFirstMoverAndNoLastMove()
        {
            var english = new CheckersGame(Variant.English);
            Assert.AreEqual(Side.Dark, english.SideToMove);
            Assert.IsNull(english.lastMove);
            Assert.AreEqual(0, english.ply);
            Assert.IsFalse(english.IsOver);
            Assert.IsNull(english.Winner);
            Assert.AreEqual(GameResult.Playing, english.Result);
            Assert.AreEqual(EndReason.None, english.Reason);
            Assert.AreEqual(Side.Light, new CheckersGame(Variant.Russian).SideToMove);
            Assert.AreEqual(Side.Light, new CheckersGame(Variant.International).SideToMove);
        }

        [Test]
        public void WinByLeavingTheOtherSideWithoutAMove()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Dark, "l 0 0", "d 1 1", "d 3 3");
            Move block = game.FindLegal(new[] { Positions.Square(8, 3, 3), Positions.Square(8, 2, 2) });
            Assert.IsNotNull(block);
            Assert.IsTrue(game.Play(block));
            Assert.IsTrue(game.IsOver);
            Assert.AreEqual(GameResult.DarkWins, game.Result);
            Assert.AreEqual(EndReason.NoMoves, game.Reason);
            Assert.AreEqual(Side.Dark, game.Winner);
            Assert.AreEqual(0, game.Legal().Count);
            Assert.IsFalse(game.Play(Move.Plain(0, 9)));
        }

        [Test]
        public void ResignGivesTheGameToTheOtherSide()
        {
            var game = new CheckersGame(Variant.Russian);
            Assert.IsTrue(game.Resign(Side.Light));
            Assert.AreEqual(GameResult.DarkWins, game.Result);
            Assert.AreEqual(EndReason.Resigned, game.Reason);
            Assert.IsFalse(game.Resign(Side.Dark), "nothing to resign once the game is over");
            Assert.AreEqual(0, game.Legal().Count);
            Assert.IsFalse(game.Play(Move.Plain(Positions.Square(8, 2, 0), Positions.Square(8, 3, 1))));
        }

        [Test]
        public void FiftyQuietPliesDraw()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "L 0 0", "D 7 7");
            game.quietPlies = VariantRules.DrawPliesCount - 1;
            Assert.IsTrue(game.Play(game.Legal()[0]));
            Assert.AreEqual(GameResult.Draw, game.Result);
            Assert.AreEqual(EndReason.DrawByPlies, game.Reason);
            Assert.IsNull(game.Winner);
            Assert.IsTrue(game.IsOver);
        }

        [Test]
        public void AManMovingOrACaptureResetsTheQuietPlies()
        {
            CheckersGame men = Positions.Game(Variant.English, Side.Light, "l 0 0", "D 7 7");
            men.quietPlies = 10;
            Assert.IsTrue(men.Play(men.Legal()[0]));
            Assert.AreEqual(0, men.quietPlies);

            CheckersGame kings = Positions.Game(Variant.English, Side.Light, "L 0 0", "D 7 7");
            kings.quietPlies = 10;
            Assert.IsTrue(kings.Play(kings.Legal()[0]));
            Assert.AreEqual(11, kings.quietPlies);

            CheckersGame capture = Positions.Game(Variant.English, Side.Light, "L 0 0", "d 1 1", "D 7 7");
            capture.quietPlies = 10;
            Assert.IsTrue(capture.Play(capture.Legal()[0]));
            Assert.AreEqual(0, capture.quietPlies);
            Assert.AreEqual(1, capture.capturedCount[(int)Side.Light]);
            Assert.AreEqual(0, capture.capturedCount[(int)Side.Dark]);
        }

        [Test]
        public void LastMoveIsTheMovePlayedAndTheTurnPasses()
        {
            var game = new CheckersGame(Variant.English);
            Move first = game.Legal()[0];
            Assert.IsTrue(game.Play(first));
            Assert.IsTrue(game.lastMove.SamePathAs(first));
            Assert.AreEqual(Side.Light, game.SideToMove);
            Assert.AreEqual(1, game.ply);
            Assert.AreEqual(0, game.quietPlies);
        }

        [Test]
        public void FindLegalAndLegalFromReadThePosition()
        {
            var game = new CheckersGame(Variant.English);
            int from = Positions.Square(8, 5, 1);
            IReadOnlyList<Move> ofPiece = game.LegalFrom(from);
            Assert.AreEqual(2, ofPiece.Count);
            Assert.AreEqual(0, game.LegalFrom(Positions.Square(8, 7, 1)).Count, "a man behind the front cannot move");
            Assert.AreEqual(0, game.LegalFrom(Positions.Square(8, 0, 0)).Count, "the other side is not to move");
            Assert.IsNotNull(game.FindLegal(new[] { from, Positions.Square(8, 4, 0) }));
            Assert.IsNull(game.FindLegal(new[] { from, Positions.Square(8, 3, 1) }));
            Assert.IsNull(game.FindLegal(new[] { from }));
            Assert.IsNull(game.FindLegal(null));
            Assert.IsFalse(game.Play(Move.Plain(from, Positions.Square(8, 3, 1))));
            Assert.IsFalse(game.Play(null));
        }

        [Test]
        public void PlayRefusesAPathThatIsNotLegal()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 2 2", "l 0 0", "d 3 3");
            Assert.IsFalse(game.Play(Move.Plain(Positions.Square(8, 0, 0), Positions.Square(8, 1, 1))), "a capture is mandatory");
            Assert.AreEqual(0, game.ply);
            Assert.IsTrue(game.Play(game.Legal()[0]));
        }

        [Test]
        public void CloneIsIndependent()
        {
            var game = new CheckersGame(Variant.International);
            uint before = game.Checksum();
            CheckersGame copy = game.Clone();
            Assert.AreEqual(before, copy.Checksum());
            Assert.IsTrue(copy.Play(copy.Legal()[0]));
            Assert.AreEqual(before, game.Checksum());
            Assert.AreEqual(0, game.ply);
            Assert.AreEqual(1, copy.ply);
            Assert.AreNotEqual(before, copy.Checksum());
            Assert.AreEqual(Side.Light, game.SideToMove);
            Assert.AreEqual(Side.Dark, copy.SideToMove);
        }

        [Test]
        public void ChecksumFollowsTheGame()
        {
            var one = new CheckersGame(Variant.Russian);
            var two = new CheckersGame(Variant.Russian);
            Assert.AreEqual(one.Checksum(), two.Checksum());
            var seen = new HashSet<uint> { one.Checksum() };
            for (int i = 0; i < 6; i++)
            {
                Move move = one.Legal()[0];
                Assert.IsTrue(one.Play(move));
                Assert.IsTrue(two.Play(two.FindLegal(move.path)));
                Assert.AreEqual(one.Checksum(), two.Checksum());
                Assert.IsTrue(seen.Add(one.Checksum()), "every ply folds to a new number");
            }
            Assert.IsTrue(two.Resign(Side.Light));
            Assert.AreNotEqual(one.Checksum(), two.Checksum());
        }

        [Test]
        public void CallOffEndsTheGameAsTold()
        {
            var drawn = new CheckersGame(Variant.English);
            drawn.CallOff(null);
            Assert.AreEqual(GameResult.Draw, drawn.Result);
            Assert.AreEqual(EndReason.CalledOff, drawn.Reason);
            Assert.IsTrue(drawn.IsOver);
            var won = new CheckersGame(Variant.English);
            won.CallOff(Side.Light);
            Assert.AreEqual(GameResult.LightWins, won.Result);
            Assert.AreEqual(Side.Light, won.Winner);
            Assert.AreEqual(EndReason.CalledOff, won.Reason);
        }

        [Test]
        public void MaterialCountsMenAndKings()
        {
            Assert.AreEqual(1200, new CheckersGame(Variant.English).Material(Side.Light));
            Assert.AreEqual(2000, new CheckersGame(Variant.International).Material(Side.Dark));
            CheckersGame english = Positions.Game(Variant.English, Side.Light, "L 0 0", "l 2 2", "D 7 7");
            Assert.AreEqual(350, english.Material(Side.Light));
            Assert.AreEqual(250, english.Material(Side.Dark));
            CheckersGame russian = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "l 2 2");
            Assert.AreEqual(400, russian.Material(Side.Light));
        }

        [Test]
        public void VariantNumberIsClamped()
        {
            var game = new CheckersGame((Variant)9);
            Assert.AreEqual(Variant.International, game.Rules.Variant);
            Assert.AreEqual((int)Variant.International, game.variant);
            Assert.AreEqual(10, game.board.Size);
        }
    }
}
