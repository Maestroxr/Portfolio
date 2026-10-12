using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The move generator against the published perft numbers (positions a number of moves deep) of the starting
    /// position and the well known test positions, which between them hold every special move and most of the traps:
    /// castling through and out of check, en passant that uncovers a check, promotions with capture, pins.
    /// </summary>
    public class ChessPerftTests
    {
        public const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
        public const string Position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        public const string Position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
        public const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

        [TestCase(0, 1L)]
        [TestCase(1, 20L)]
        [TestCase(2, 400L)]
        [TestCase(3, 8902L)]
        [TestCase(4, 197281L)]
        public void StartingPosition(int depth, long nodes)
        {
            Assert.AreEqual(nodes, ChessMoveGenerator.Perft(ChessGame.NewGame(), depth));
        }

        [TestCase(1, 48L)]
        [TestCase(2, 2039L)]
        [TestCase(3, 97862L)]
        public void KiwipetePosition(int depth, long nodes)
        {
            Assert.AreEqual(nodes, ChessMoveGenerator.Perft(ChessGame.FromFen(Kiwipete), depth));
        }

        [TestCase(1, 14L)]
        [TestCase(2, 191L)]
        [TestCase(3, 2812L)]
        [TestCase(4, 43238L)]
        public void EndgamePosition3(int depth, long nodes)
        {
            Assert.AreEqual(nodes, ChessMoveGenerator.Perft(ChessGame.FromFen(Position3), depth));
        }

        [TestCase(1, 6L)]
        [TestCase(2, 264L)]
        [TestCase(3, 9467L)]
        public void PromotionsPosition4(int depth, long nodes)
        {
            Assert.AreEqual(nodes, ChessMoveGenerator.Perft(ChessGame.FromFen(Position4), depth));
        }

        [TestCase(1, 44L)]
        [TestCase(2, 1486L)]
        [TestCase(3, 62379L)]
        public void CastlingAndPromotionPosition5(int depth, long nodes)
        {
            Assert.AreEqual(nodes, ChessMoveGenerator.Perft(ChessGame.FromFen(Position5), depth));
        }

        [Test]
        public void LegalMovesAgreeWithPerftOne()
        {
            foreach (string fen in new[] { ChessNotation.StartFen, Kiwipete, Position3, Position4, Position5 })
            {
                ChessGame game = ChessGame.FromFen(fen);
                Assert.AreEqual(ChessMoveGenerator.Perft(game, 1), game.Legal().Count, fen);
            }
        }

        /// <summary>The deeper counts, to run by hand after a change to the generator (seconds, not milliseconds).</summary>
        [Explicit]
        [TestCase(Kiwipete, 4, 4085603L)]
        [TestCase(Position3, 5, 674624L)]
        [TestCase(Position4, 4, 422333L)]
        [TestCase(Position5, 4, 2103487L)]
        [TestCase(ChessNotation.StartFen, 5, 4865609L)]
        public void DeeperCounts(string fen, int depth, long nodes)
        {
            Assert.AreEqual(nodes, ChessMoveGenerator.Perft(ChessGame.FromFen(fen), depth));
        }
    }
}
