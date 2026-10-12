using NUnit.Framework;

namespace Portfolio.Checkers.Tests
{
    /// <summary>The board alone: the squares that are played on, the starting positions of the three variants, the counts.</summary>
    public class BoardTests
    {
        [Test]
        public void OnlyDarkSquaresArePlayable()
        {
            Assert.IsTrue(Board.Playable(0, 0));
            Assert.IsFalse(Board.Playable(0, 1));
            Assert.IsTrue(Board.Playable(1, 1));
            Assert.IsTrue(Board.Playable(7, 7));
            Assert.IsFalse(Board.Playable(7, 0));
            int playable = 0;
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    if (Board.Playable(row, col))
                    {
                        playable++;
                    }
                }
            }
            Assert.AreEqual(50, playable);
        }

        [Test]
        public void IndexRowAndColAgree()
        {
            Assert.AreEqual(0, Board.Index(8, 0, 0));
            Assert.AreEqual(63, Board.Index(8, 7, 7));
            Assert.AreEqual(5, Board.Row(8, 42));
            Assert.AreEqual(2, Board.Col(8, 42));
            Assert.AreEqual(42, Board.Index(8, Board.Row(8, 42), Board.Col(8, 42)));
            Assert.AreEqual(9, Board.Row(10, 97));
            Assert.AreEqual(7, Board.Col(10, 97));
        }

        [TestCase(Variant.English, 8, 12)]
        [TestCase(Variant.Russian, 8, 12)]
        [TestCase(Variant.International, 10, 20)]
        public void InitialPositionFillsTheHomeRows(Variant variant, int size, int men)
        {
            VariantRules rules = VariantRules.Of(variant);
            Board board = Board.Initial(rules);
            Assert.AreEqual(size, board.Size);
            Assert.AreEqual(size * size, board.cells.Length);
            Assert.AreEqual(men, rules.MenPerSide);
            Assert.AreEqual(men, board.Count(Side.Light));
            Assert.AreEqual(men, board.Count(Side.Dark));
            Assert.AreEqual(men, board.Count(Side.Light, false));
            Assert.AreEqual(0, board.Count(Side.Light, true));
            Assert.AreEqual(0, board.Count(Side.Dark, true));
            for (int row = 0; row < size; row++)
            {
                for (int col = 0; col < size; col++)
                {
                    sbyte piece = board[row, col];
                    if (!Board.Playable(row, col))
                    {
                        Assert.AreEqual(Pieces.None, piece, "square {0},{1} is not played on", row, col);
                    }
                    else if (row < rules.RowsOfMen)
                    {
                        Assert.AreEqual(Pieces.LightMan, piece);
                    }
                    else if (row >= size - rules.RowsOfMen)
                    {
                        Assert.AreEqual(Pieces.DarkMan, piece);
                    }
                    else
                    {
                        Assert.AreEqual(Pieces.None, piece);
                    }
                }
            }
        }

        [Test]
        public void CountsTellMenFromKings()
        {
            var board = new Board(8);
            board[0, 0] = Pieces.LightMan;
            board[2, 2] = Pieces.LightKing;
            board[7, 7] = Pieces.DarkKing;
            board[5, 5] = Pieces.DarkMan;
            board[5, 3] = Pieces.DarkMan;
            Assert.AreEqual(2, board.Count(Side.Light));
            Assert.AreEqual(1, board.Count(Side.Light, true));
            Assert.AreEqual(1, board.Count(Side.Light, false));
            Assert.AreEqual(3, board.Count(Side.Dark));
            Assert.AreEqual(2, board.Count(Side.Dark, false));
            Assert.AreEqual(1, board.Count(Side.Dark, true));
        }

        [Test]
        public void CloneIsIndependent()
        {
            Board board = Board.Initial(VariantRules.Of(Variant.English));
            Board copy = board.Clone();
            copy[0, 0] = Pieces.None;
            Assert.AreEqual(Pieces.LightMan, board[0, 0]);
            Assert.AreEqual(Pieces.None, copy[0, 0]);
            Assert.AreEqual(11, copy.Count(Side.Light));
        }

        [Test]
        public void PiecesReadTheSignAndTheRank()
        {
            Assert.AreEqual(Side.Light, Pieces.SideOf(Pieces.LightKing));
            Assert.AreEqual(Side.Dark, Pieces.SideOf(Pieces.DarkMan));
            Assert.IsTrue(Pieces.IsKing(Pieces.DarkKing));
            Assert.IsFalse(Pieces.IsKing(Pieces.DarkMan));
            Assert.IsTrue(Pieces.IsMan(Pieces.LightMan));
            Assert.IsFalse(Pieces.IsMan(Pieces.None));
            Assert.IsFalse(Pieces.Belongs(Pieces.None, Side.Light));
            Assert.IsFalse(Pieces.Belongs(Pieces.None, Side.Dark));
            Assert.IsTrue(Pieces.Belongs(Pieces.DarkKing, Side.Dark));
            Assert.AreEqual(Pieces.DarkMan, Pieces.Man(Side.Dark));
            Assert.AreEqual(Pieces.LightKing, Pieces.King(Side.Light));
        }

        [Test]
        public void VariantRulesAreTheContractsTable()
        {
            VariantRules english = VariantRules.Of(Variant.English);
            Assert.AreEqual(8, english.Size);
            Assert.AreEqual(3, english.RowsOfMen);
            Assert.IsFalse(english.MenCaptureBackward);
            Assert.IsFalse(english.FlyingKings);
            Assert.IsFalse(english.MajorityCapture);
            Assert.AreEqual(Side.Dark, english.FirstMover);
            VariantRules russian = VariantRules.Of(Variant.Russian);
            Assert.IsTrue(russian.MenCaptureBackward);
            Assert.IsTrue(russian.FlyingKings);
            Assert.IsTrue(russian.PromoteDuringCapture);
            Assert.AreEqual(Side.Light, russian.FirstMover);
            VariantRules international = VariantRules.Of(Variant.International);
            Assert.AreEqual(10, international.Size);
            Assert.AreEqual(4, international.RowsOfMen);
            Assert.IsTrue(international.MajorityCapture);
            Assert.IsFalse(international.PromoteDuringCapture);
            Assert.AreEqual(100, international.Squares);
            Assert.AreEqual(50, english.DrawPlies);
            Assert.AreSame(international, VariantRules.Of(7));
            Assert.AreSame(english, VariantRules.Of(-3));
            Assert.AreEqual(3, VariantRules.All.Length);
            Assert.AreEqual("Russian", russian.NameKey);
            Assert.AreEqual("Dark", Sides.NameKey(Side.Dark));
            Assert.AreEqual(Side.Light, Sides.Other(Side.Dark));
        }
    }
}
