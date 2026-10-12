using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The moves the rules allow, from hand-made positions: steps, mandatory captures and their sequences, the short
    /// English king against the flying ones, the majority rule, no piece jumped twice, and what a man that reaches the
    /// far rank becomes in each variant.
    /// </summary>
    public class MoveGeneratorTests
    {
        private static IReadOnlyList<Move> Legal(CheckersGame game)
        {
            return MoveGenerator.Legal(game.board, game.SideToMove, game.Rules);
        }

        private static string Text(int size, params int[][] squares)
        {
            return string.Join("-", System.Array.ConvertAll(squares, s => Positions.Human(size, s[0], s[1]).ToString()));
        }

        [Test]
        public void OpeningStepsOfBothSides()
        {
            var game = new CheckersGame(Variant.English);
            Assert.AreEqual(Side.Dark, game.SideToMove);
            IReadOnlyList<Move> dark = Legal(game);
            Assert.AreEqual(7, dark.Count);
            foreach (Move move in dark)
            {
                Assert.IsFalse(move.IsCapture);
                Assert.AreEqual(5, Board.Row(8, move.From));
                Assert.AreEqual(4, Board.Row(8, move.To));
            }
            Assert.AreEqual(7, MoveGenerator.Legal(game.board, Side.Light, game.Rules).Count);
            Assert.AreEqual(9, MoveGenerator.Legal(Board.Initial(VariantRules.Of(Variant.International)), Side.Light,
                VariantRules.Of(Variant.International)).Count);
        }

        [Test]
        public void MovesComeOutOrderedByFromThenPath()
        {
            var game = new CheckersGame(Variant.International);
            IReadOnlyList<Move> moves = Legal(game);
            for (int i = 1; i < moves.Count; i++)
            {
                Assert.LessOrEqual(moves[i - 1].From, moves[i].From);
                if (moves[i - 1].From == moves[i].From)
                {
                    Assert.Less(moves[i - 1].To, moves[i].To);
                }
            }
        }

        [Test]
        public void EnglishMenNeverCaptureBackwardRussianMenMust()
        {
            CheckersGame english = Positions.Game(Variant.English, Side.Light, "l 3 3", "d 2 2");
            List<string> moves = Positions.Texts(Legal(english));
            Assert.AreEqual(2, moves.Count);
            Assert.Contains(Text(8, new[] { 3, 3 }, new[] { 4, 2 }), moves);
            Assert.Contains(Text(8, new[] { 3, 3 }, new[] { 4, 4 }), moves);

            CheckersGame russian = Positions.Game(Variant.Russian, Side.Light, "l 3 3", "d 2 2");
            IReadOnlyList<Move> captures = Legal(russian);
            Assert.AreEqual(1, captures.Count);
            Assert.IsTrue(captures[0].IsCapture);
            Assert.AreEqual(Positions.Square(8, 1, 1), captures[0].To);
            Assert.AreEqual(Positions.Square(8, 2, 2), captures[0].captured[0]);
        }

        [Test]
        public void CaptureIsMandatory()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 2 2", "l 0 0", "d 3 3");
            IReadOnlyList<Move> moves = Legal(game);
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(Positions.Square(8, 2, 2), moves[0].From);
            Assert.AreEqual(Positions.Square(8, 4, 4), moves[0].To);
            Assert.AreEqual(1, moves[0].Jumps);
            Assert.IsTrue(MoveGenerator.HasAnyMove(game.board, Side.Light, game.Rules));
        }

        [Test]
        public void MultiJumpIsOneMoveAndAnySequenceMayBeChosen()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 0 0", "d 1 1", "d 3 3");
            IReadOnlyList<Move> moves = Legal(game);
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(2, moves[0].Jumps);
            CollectionAssert.AreEqual(new[] { Positions.Square(8, 0, 0), Positions.Square(8, 2, 2), Positions.Square(8, 4, 4) }, moves[0].path);
            CollectionAssert.AreEqual(new[] { Positions.Square(8, 1, 1), Positions.Square(8, 3, 3) }, moves[0].captured);
            Assert.AreEqual("1x19x37", moves[0].ToString());

            // A fork after the first jump: both branches are legal, the shorter one too (no majority rule in English).
            CheckersGame fork = Positions.Game(Variant.English, Side.Light, "l 0 0", "d 1 1", "d 3 3", "d 3 1", "d 5 5");
            List<string> texts = Positions.Texts(Legal(fork));
            Assert.AreEqual(2, texts.Count);
            Assert.Contains("1x19x33", texts);
            Assert.Contains("1x19x37x55", texts);
        }

        [Test]
        public void EnglishKingIsShort()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "L 3 3");
            List<string> moves = Positions.Texts(Legal(game));
            Assert.AreEqual(4, moves.Count);
            Assert.Contains(Text(8, new[] { 3, 3 }, new[] { 4, 4 }), moves);
            Assert.Contains(Text(8, new[] { 3, 3 }, new[] { 2, 2 }), moves);
            Assert.Contains(Text(8, new[] { 3, 3 }, new[] { 2, 4 }), moves);
            Assert.Contains(Text(8, new[] { 3, 3 }, new[] { 4, 2 }), moves);

            CheckersGame capture = Positions.Game(Variant.English, Side.Light, "L 0 0", "d 2 2");
            Assert.AreEqual(0, Legal(capture).Count(m => m.IsCapture), "a short king does not capture at a distance");
            CheckersGame adjacent = Positions.Game(Variant.English, Side.Light, "L 3 3", "d 2 2");
            IReadOnlyList<Move> backward = Legal(adjacent);
            Assert.AreEqual(1, backward.Count);
            Assert.AreEqual(Positions.Square(8, 1, 1), backward[0].To);
        }

        [Test]
        public void FlyingKingSlidesAndCapturesAtADistance()
        {
            CheckersGame slide = Positions.Game(Variant.Russian, Side.Light, "L 3 3");
            Assert.AreEqual(13, Legal(slide).Count);

            CheckersGame capture = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 3 3");
            IReadOnlyList<Move> moves = Legal(capture);
            Assert.AreEqual(4, moves.Count);
            foreach (Move move in moves)
            {
                Assert.AreEqual(1, move.Jumps);
                Assert.AreEqual(Positions.Square(8, 3, 3), move.captured[0]);
                Assert.Greater(Board.Row(8, move.To), 3);
            }

            // An enemy with no empty square behind it: nothing to take, only slides.
            CheckersGame blocked = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 3 3", "d 4 4");
            Assert.AreEqual(0, Legal(blocked).Count(m => m.IsCapture));
        }

        [Test]
        public void MajorityRuleKeepsOnlyTheLongestCaptures()
        {
            CheckersGame international = Positions.Game(Variant.International, Side.Light, "l 2 2", "d 3 3", "d 5 5", "d 3 1");
            IReadOnlyList<Move> moves = Legal(international);
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(2, moves[0].Jumps);
            Assert.AreEqual(Positions.Square(10, 6, 6), moves[0].To);

            CheckersGame russian = Positions.Game(Variant.Russian, Side.Light, "l 2 2", "d 3 3", "d 5 5", "d 3 1");
            Assert.AreEqual(2, Legal(russian).Count, "without the majority rule the single jump is legal too");
        }

        [Test]
        public void NoPieceIsJumpedTwiceAndTakenPiecesStayUntilTheEnd()
        {
            // A ring of five around a flying king: it can take them all going round, but not the first one again.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 1 1", "d 3 1", "d 5 1", "d 5 3", "d 3 3");
            IReadOnlyList<Move> moves = Legal(game);
            int longest = 0;
            foreach (Move move in moves)
            {
                var seen = new HashSet<int>(move.captured);
                Assert.AreEqual(move.captured.Length, seen.Count, "a piece was jumped twice in " + move);
                Assert.LessOrEqual(move.Jumps, 5);
                Assert.AreNotEqual(Positions.Square(8, 0, 0), move.To, "the king cannot come home over the first piece it took");
                longest = System.Math.Max(longest, move.Jumps);
            }
            Assert.AreEqual(5, longest);

            // Under the majority rule only the full round remains, and the ring stands until the move is applied.
            CheckersGame majority = Positions.Game(Variant.International, Side.Light, "L 0 0", "d 1 1", "d 3 1", "d 5 1", "d 5 3", "d 3 3");
            IReadOnlyList<Move> rounds = Legal(majority);
            Assert.Greater(rounds.Count, 0);
            foreach (Move move in rounds)
            {
                Assert.AreEqual(5, move.Jumps);
            }
            Board after = majority.board.Clone();
            MoveGenerator.Apply(after, rounds[0], majority.Rules);
            Assert.AreEqual(0, after.Count(Side.Dark));
            Assert.AreEqual(1, after.Count(Side.Light, true));
        }

        [Test]
        public void EnglishManStopsWhenCrownedRussianManGoesOnAsAKing()
        {
            // The man at 4,6 is there to leave the Russian king one landing square after its second jump.
            CheckersGame english = Positions.Game(Variant.English, Side.Light, "l 5 1", "d 6 2", "d 6 4", "l 4 6");
            IReadOnlyList<Move> stop = Legal(english);
            Assert.AreEqual(1, stop.Count);
            Assert.AreEqual(1, stop[0].Jumps);
            Assert.IsTrue(stop[0].promotes);
            Assert.AreEqual(Positions.Square(8, 7, 3), stop[0].To);
            Assert.IsTrue(english.Play(stop[0]));
            Assert.AreEqual(Pieces.LightKing, english.board[7, 3]);

            CheckersGame russian = Positions.Game(Variant.Russian, Side.Light, "l 5 1", "d 6 2", "d 6 4", "l 4 6");
            IReadOnlyList<Move> onward = Legal(russian);
            Assert.AreEqual(1, onward.Count);
            Assert.AreEqual(2, onward[0].Jumps);
            Assert.IsTrue(onward[0].promotes);
            Assert.AreEqual(Positions.Square(8, 5, 5), onward[0].To);
            Assert.IsTrue(russian.Play(onward[0]));
            Assert.AreEqual(Pieces.LightKing, russian.board[5, 5]);
            Assert.AreEqual(0, russian.board.Count(Side.Dark));
        }

        [Test]
        public void InternationalManIsCrownedOnlyWhenTheSequenceEndsOnTheFarRank()
        {
            CheckersGame through = Positions.Game(Variant.International, Side.Light, "l 7 1", "d 8 2", "d 8 4");
            IReadOnlyList<Move> moves = Legal(through);
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(2, moves[0].Jumps);
            Assert.IsFalse(moves[0].promotes);
            Assert.AreEqual(Positions.Square(10, 7, 5), moves[0].To);
            Assert.IsTrue(through.Play(moves[0]));
            Assert.AreEqual(Pieces.LightMan, through.board[7, 5]);

            CheckersGame ends = Positions.Game(Variant.International, Side.Light, "l 7 1", "d 8 2");
            IReadOnlyList<Move> crown = Legal(ends);
            Assert.AreEqual(1, crown.Count);
            Assert.IsTrue(crown[0].promotes);
            Assert.IsTrue(ends.Play(crown[0]));
            Assert.AreEqual(Pieces.LightKing, ends.board[9, 3]);
        }

        [Test]
        public void PlainStepOntoTheFarRankPromotes()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Dark, "d 1 1", "l 7 7");
            IReadOnlyList<Move> moves = Legal(game);
            Assert.AreEqual(2, moves.Count);
            foreach (Move move in moves)
            {
                Assert.IsTrue(move.promotes);
                Assert.AreEqual(0, Board.Row(8, move.To));
            }
            Assert.IsTrue(game.Play(moves[0]));
            Assert.AreEqual(1, game.board.Count(Side.Dark, true));
            Assert.AreEqual(0, game.board.Count(Side.Dark, false));
        }

        [Test]
        public void ApplyMovesThePieceAndRemovesTheTaken()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 0 0", "d 1 1", "d 3 3");
            Move move = Legal(game)[0];
            MoveGenerator.Apply(game.board, move, game.Rules);
            Assert.AreEqual(Pieces.None, game.board[0, 0]);
            Assert.AreEqual(Pieces.None, game.board[1, 1]);
            Assert.AreEqual(Pieces.None, game.board[3, 3]);
            Assert.AreEqual(Pieces.LightMan, game.board[4, 4]);
        }

        [Test]
        public void ABlockedSideHasNoMove()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 0 0", "d 1 1", "d 2 2");
            Assert.AreEqual(0, Legal(game).Count);
            Assert.IsFalse(MoveGenerator.HasAnyMove(game.board, Side.Light, game.Rules));
            Assert.IsTrue(MoveGenerator.HasAnyMove(game.board, Side.Dark, game.Rules));
        }

        [TestCase(Variant.English, 11)]
        [TestCase(Variant.Russian, 13)]
        [TestCase(Variant.International, 17)]
        public void HasAnyMoveAgreesWithLegalAlongAGame(Variant variant, int seed)
        {
            var game = new CheckersGame(variant);
            var random = new Gamebox.Lockstep.SystemRandom(seed);
            while (!game.IsOver && game.ply < 120)
            {
                IReadOnlyList<Move> legal = game.Legal();
                Assert.AreEqual(legal.Count > 0, MoveGenerator.HasAnyMove(game.board, game.SideToMove, game.Rules));
                Assert.IsTrue(game.Play(legal[random.Range(0, legal.Count)]));
            }
        }
    }

    internal static class MoveListExtensions
    {
        public static int Count(this IReadOnlyList<Move> moves, System.Func<Move, bool> test)
        {
            int count = 0;
            foreach (Move move in moves)
            {
                if (test(move))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
