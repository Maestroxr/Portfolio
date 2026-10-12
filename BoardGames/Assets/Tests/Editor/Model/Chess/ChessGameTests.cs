using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The rules of chess as the game keeps them: positions in and out of FEN, the notation of the moves played,
    /// castling, en passant and promotion, and every way a game ends by itself (checkmate, stalemate, repetition, the
    /// fifty move rule, insufficient material), giving up and calling off; copies, checksums and material.
    /// </summary>
    public class ChessGameTests
    {
        /// <summary>"e4" to its square number.</summary>
        public static int Sq(string name)
        {
            return ChessGame.Square(name[0] - 'a', name[1] - '1');
        }

        /// <summary>Plays the moves written in algebraic notation, each of which must be legal.</summary>
        public static ChessGame Play(ChessGame game, params string[] moves)
        {
            foreach (string san in moves)
            {
                ChessMove move = game.FindSan(san);
                Assert.IsNotNull(move, "not legal: " + san + " in " + game.ToFen());
                Assert.IsTrue(game.Play(move), san);
            }
            return game;
        }

        [Test]
        public void NewGameIsTheStartingPosition()
        {
            ChessGame game = ChessGame.NewGame();
            Assert.AreEqual(ChessNotation.StartFen, game.ToFen());
            Assert.AreEqual(RuleSet.Chess, game.RuleSet);
            Assert.AreEqual(Side.Light, game.SideToMove);
            Assert.AreEqual(Side.Light, game.SideToAct);
            Assert.AreEqual(15, game.castling);
            Assert.AreEqual(-1, game.enPassant);
            Assert.AreEqual(20, game.Legal().Count);
            Assert.AreEqual(2, game.positions.Count, "the first position's key");
            Assert.IsFalse(game.IsOver);
            Assert.IsFalse(game.InCheck);
            Assert.IsNull(game.Winner);
            Assert.AreEqual(0, game.Points);
            Assert.AreEqual(0, game.Turn);
            Assert.IsNull(game.LastMove);
            Assert.AreEqual(4000, game.Material(Side.Light));
            Assert.AreEqual(4000, game.Material(Side.Dark));
            Assert.AreEqual(Sq("e1"), game.KingSquare(Side.Light));
            Assert.AreEqual(Sq("e8"), game.KingSquare(Side.Dark));
            Assert.AreEqual(ChessPieces.Make(Side.Dark, ChessPieceKind.Queen), game[Sq("d8")]);
            Assert.AreEqual(ChessPieces.None, game[64]);
        }

        [TestCase(ChessNotation.StartFen)]
        [TestCase(ChessPerftTests.Kiwipete)]
        [TestCase(ChessPerftTests.Position3)]
        [TestCase(ChessPerftTests.Position4)]
        [TestCase(ChessPerftTests.Position5)]
        [TestCase("rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3")]
        [TestCase("4k3/8/8/8/8/8/8/R3K2R b K - 12 40")]
        public void FenRoundTrips(string fen)
        {
            ChessGame game = ChessGame.FromFen(fen);
            Assert.AreEqual(fen, game.ToFen());
            Assert.AreEqual(fen, ChessGame.FromFen(game.ToFen()).ToFen());
            Assert.AreEqual(game.Checksum(), ChessGame.FromFen(fen).Checksum());
        }

        [Test]
        public void FenFollowsTheMoves()
        {
            ChessGame game = ChessGame.NewGame();
            Play(game, "e4");
            Assert.AreEqual("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1", game.ToFen());
            Play(game, "c5");
            Assert.AreEqual("rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 2", game.ToFen());
            Play(game, "Nf3");
            Assert.AreEqual("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2", game.ToFen());
            Assert.AreEqual(3, game.ply);
            Assert.AreEqual(2, game.fullmove);
            Assert.AreEqual(1, game.halfmoveClock);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("8/8/8 w - - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR x KQkq - 0 1")]
        [TestCase("rnbqkbnr/ppppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/ppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNX w KQkq - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkx - 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq e9 0 1")]
        [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - x 1")]
        [TestCase("8/8/8/8/8/8/8/8 w - - 0 1")]
        [TestCase("4k3/8/8/8/8/8/8/3KK3 w - - 0 1")]
        [TestCase("P3k3/8/8/8/8/8/8/4K3 w - - 0 1")]
        [TestCase("4k3/8/8/8/8/8/8/4K3 w - e3 0 1")]
        [TestCase("4k3/8/8/8/8/8/8/4K2r b - - 0 1")]
        public void BadFensThrow(string fen)
        {
            Assert.Throws<FormatException>(() => ChessGame.FromFen(fen));
        }

        [Test]
        public void FenDropsRightsThePiecesDoNotAllow()
        {
            ChessGame game = ChessGame.FromFen("4k3/8/8/8/8/8/8/4K2R w KQkq - 0 1");
            Assert.AreEqual(ChessGame.CastleWhiteKing, game.castling);
            Assert.AreEqual("4k3/8/8/8/8/8/8/4K2R w K - 0 1", game.ToFen());
            ChessGame noPawn = ChessGame.FromFen("4k3/8/8/8/8/8/8/4K2R w K e6 0 1");
            Assert.AreEqual(-1, noPawn.enPassant, "no pawn passed over e6");
            Assert.AreEqual("4k3/8/8/8/8/8/8/4K2R w K - 0 1", noPawn.ToFen());
        }

        [Test]
        public void NotationOfCastlingAndPieces()
        {
            ChessGame game = Play(ChessGame.NewGame(), "e4", "e5", "Nf3", "Nc6", "Bb5", "a6", "Ba4", "Nf6", "O-O");
            Assert.AreEqual("O-O", game.LastMove.san);
            Assert.IsTrue(game.LastMove.IsCastle);
            Assert.AreEqual(ChessPieces.Make(Side.Light, ChessPieceKind.King), game[Sq("g1")]);
            Assert.AreEqual(ChessPieces.Make(Side.Light, ChessPieceKind.Rook), game[Sq("f1")]);
            Assert.AreEqual(ChessPieces.None, game[Sq("h1")]);
            Assert.AreEqual(ChessGame.CastleBlackKing | ChessGame.CastleBlackQueen, game.castling);
            Assert.AreEqual(Sq("h1"), game.LastMove.RookFrom);
            Assert.AreEqual(Sq("f1"), game.LastMove.RookTo);

            ChessGame queenSide = ChessGame.FromFen("r3k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            ChessMove castle = queenSide.FindLegal(Sq("e1"), Sq("c1"));
            Assert.IsNotNull(castle);
            Assert.AreEqual("O-O-O", queenSide.SanOf(castle));
            Assert.AreEqual(castle, queenSide.FindSan("0-0-0"));
            Assert.IsTrue(queenSide.Play(castle));
            Assert.AreEqual(ChessPieces.Make(Side.Light, ChessPieceKind.Rook), queenSide[Sq("d1")]);
            Assert.AreEqual(ChessPieces.None, queenSide[Sq("a1")]);
        }

        [Test]
        public void NotationTellsPiecesApart()
        {
            ChessGame knights = ChessGame.FromFen("4k3/8/8/8/8/2N3N1/8/4K3 w - - 0 1");
            Assert.AreEqual("Nce4", knights.SanOf(knights.FindLegal(Sq("c3"), Sq("e4"))));
            Assert.AreEqual("Nge4", knights.SanOf(knights.FindLegal(Sq("g3"), Sq("e4"))));
            Assert.AreEqual("Nge2", knights.SanOf(knights.FindLegal(Sq("g3"), Sq("e2"))), "both reach e2 too");
            Assert.AreEqual("Na4", knights.SanOf(knights.FindLegal(Sq("c3"), Sq("a4"))));
            Assert.AreEqual(Sq("g3"), knights.FindSan("Nge4").from);

            ChessGame rooks = ChessGame.FromFen("4k3/8/8/R7/8/8/8/R3K3 w - - 0 1");
            Assert.AreEqual("R1a3", rooks.SanOf(rooks.FindLegal(Sq("a1"), Sq("a3"))));
            Assert.AreEqual("R5a3", rooks.SanOf(rooks.FindLegal(Sq("a5"), Sq("a3"))));
            Assert.AreEqual("Rb5", rooks.SanOf(rooks.FindLegal(Sq("a5"), Sq("b5"))));

            ChessGame queens = ChessGame.FromFen("4k3/8/8/8/8/Q7/8/Q1Q1K3 w - - 0 1");
            Assert.AreEqual("Qa1b2", queens.SanOf(queens.FindLegal(Sq("a1"), Sq("b2"))));
            Assert.AreEqual("Qcb2", queens.SanOf(queens.FindLegal(Sq("c1"), Sq("b2"))));
            Assert.AreEqual("Q3b2", queens.SanOf(queens.FindLegal(Sq("a3"), Sq("b2"))));
            Assert.AreEqual("", queens.SanOf(new ChessMove { from = Sq("a1"), to = Sq("h1") }), "blocked by the king");
        }

        [Test]
        public void EnPassantOnlyRightAfterTheDoubleStep()
        {
            const string fen = "rnbqkbnr/ppp1p1pp/8/3pPp2/8/8/PPPP1PPP/RNBQKBNR w KQkq f6 0 3";
            ChessGame game = ChessGame.FromFen(fen);
            ChessMove take = game.FindLegal(Sq("e5"), Sq("f6"));
            Assert.IsNotNull(take);
            Assert.IsTrue(take.IsEnPassant);
            Assert.AreEqual(Sq("f5"), take.captured);
            Assert.IsNull(game.FindLegal(Sq("e5"), Sq("d6")), "d5 moved two moves ago");
            Assert.IsTrue(game.Play(take));
            Assert.AreEqual("exf6", game.LastMove.san);
            Assert.AreEqual(ChessPieces.None, game[Sq("f5")]);
            Assert.AreEqual(new List<int> { -1 }, game.captured);
            Assert.AreEqual("rnbqkbnr/ppp1p1pp/5P2/3p4/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 3", game.ToFen());

            ChessGame late = Play(ChessGame.FromFen(fen), "Nf3", "Nc6");
            Assert.IsNull(late.FindLegal(Sq("e5"), Sq("f6")));
        }

        [Test]
        public void PromotionChoicesAreFourMoves()
        {
            ChessGame game = ChessGame.FromFen("4k3/P7/8/8/8/8/8/4K3 w - - 0 1");
            IReadOnlyList<ChessMove> moves = game.LegalFrom(Sq("a7"));
            Assert.AreEqual(4, moves.Count);
            var kinds = new List<int>();
            foreach (ChessMove move in moves)
            {
                Assert.AreEqual(Sq("a8"), move.to);
                Assert.IsTrue(move.IsPromotion);
                kinds.Add(move.promotion);
            }
            CollectionAssert.AreEquivalent(new[] { 2, 3, 4, 5 }, kinds);
            Assert.IsTrue(game.NeedsPromotion(Sq("a7"), Sq("a8")));
            Assert.IsFalse(game.NeedsPromotion(Sq("e1"), Sq("e2")));
            Assert.IsNull(game.FindLegal(Sq("a7"), Sq("a8")), "the piece must be named");
            Assert.IsNull(game.FindLegal(new[] { Sq("a7"), Sq("a8") }));
            ChessMove knight = game.FindLegal(new[] { Sq("a7"), Sq("a8"), (int)ChessPieceKind.Knight });
            Assert.IsNotNull(knight);
            Assert.IsTrue(game.Play(knight));
            Assert.AreEqual(ChessPieces.Make(Side.Light, ChessPieceKind.Knight), game[Sq("a8")]);
            Assert.AreEqual("a8=N", game.LastMove.san);
            Assert.AreEqual(320, game.Material(Side.Light));
        }

        [Test]
        public void PromotionWithCaptureAndCheck()
        {
            ChessGame game = ChessGame.FromFen("r3k3/1P6/8/8/8/8/8/4K3 w - - 0 1");
            Play(game, "bxa8=Q+");
            Assert.AreEqual("bxa8=Q+", game.LastMove.san);
            Assert.IsTrue(game.LastMove.GivesCheck);
            Assert.IsFalse(game.LastMove.GivesMate);
            Assert.IsTrue(game.InCheck);
            Assert.AreEqual(new List<int> { -4 }, game.captured);
            ChessGame knight = ChessGame.FromFen("8/1P1k4/8/8/8/8/8/4K3 w - - 0 1");
            Assert.AreEqual("b8=N+", knight.SanOf(knight.FindLegal(Sq("b7"), Sq("b8"), (int)ChessPieceKind.Knight)));
            Assert.AreEqual("b8=Q", knight.SanOf(knight.FindLegal(Sq("b7"), Sq("b8"), (int)ChessPieceKind.Queen)));
        }

        [Test]
        public void CastlingRefusedThroughOutOfAndIntoCheck()
        {
            ChessGame through = ChessGame.FromFen("4kr2/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            Assert.IsNull(through.FindLegal(Sq("e1"), Sq("g1")), "f1 is attacked");
            Assert.IsNotNull(through.FindLegal(Sq("e1"), Sq("c1")));
            ChessGame outOf = ChessGame.FromFen("3k4/8/8/4r3/8/8/8/R3K2R w KQ - 0 1");
            Assert.IsTrue(outOf.InCheck);
            Assert.IsNull(outOf.FindLegal(Sq("e1"), Sq("g1")));
            Assert.IsNull(outOf.FindLegal(Sq("e1"), Sq("c1")));
            ChessGame into = ChessGame.FromFen("4k1r1/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            Assert.IsNull(into.FindLegal(Sq("e1"), Sq("g1")), "g1 is attacked");
            Assert.IsNotNull(into.FindLegal(Sq("e1"), Sq("c1")));
            ChessGame rookPasses = ChessGame.FromFen("1r2k3/8/8/8/8/8/8/R3K2R w KQ - 0 1");
            Assert.IsNotNull(rookPasses.FindLegal(Sq("e1"), Sq("c1")), "only the rook crosses the attacked b1");
            Assert.IsNotNull(rookPasses.FindLegal(Sq("e1"), Sq("g1")));
            ChessGame blocked = ChessGame.FromFen("4k3/8/8/8/8/8/8/RN2K1NR w KQ - 0 1");
            Assert.IsNull(blocked.FindLegal(Sq("e1"), Sq("g1")));
            Assert.IsNull(blocked.FindLegal(Sq("e1"), Sq("c1")));
        }

        [Test]
        public void CastlingRightsGoWithTheRookAndTheKing()
        {
            ChessGame game = Play(ChessGame.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1"), "Rh2", "Ra7", "Rh1", "Ra8");
            Assert.AreEqual(ChessGame.CastleWhiteQueen | ChessGame.CastleBlackKing, game.castling);
            Assert.IsNull(game.FindLegal(Sq("e1"), Sq("g1")), "the h1 rook has moved");
            Assert.IsNotNull(game.FindLegal(Sq("e1"), Sq("c1")));
            Play(game, "Kd1");
            Assert.AreEqual(ChessGame.CastleBlackKing, game.castling);
            ChessGame taken = Play(ChessGame.FromFen("r3k2r/8/8/8/8/8/6b1/R3K2R b KQkq - 0 1"), "Bxh1");
            Assert.AreEqual(ChessGame.CastleWhiteQueen | ChessGame.CastleBlackKing | ChessGame.CastleBlackQueen, taken.castling);
        }

        [Test]
        public void FoolsMateIsCheckmate()
        {
            ChessGame game = Play(ChessGame.NewGame(), "f3", "e5", "g4", "Qh4#");
            Assert.AreEqual("Qh4#", game.LastMove.san);
            Assert.IsTrue(game.LastMove.GivesMate);
            Assert.IsTrue(game.LastMove.GivesCheck);
            Assert.IsTrue(game.IsOver);
            Assert.AreEqual(GameResult.DarkWins, game.Result);
            Assert.AreEqual(EndReason.Checkmate, game.Reason);
            Assert.AreEqual(Side.Dark, game.Winner);
            Assert.AreEqual(1, game.Points);
            Assert.IsTrue(game.InCheck);
            Assert.AreEqual(0, game.Legal().Count);
            Assert.IsTrue(ChessMoveGenerator.IsAttacked(game, game.KingSquare(Side.Light), Side.Dark));
            Assert.IsFalse(game.Play(new ChessMove { from = Sq("e2"), to = Sq("e4") }), "over");
            Assert.IsFalse(game.Resign(Side.Light), "over");
        }

        [Test]
        public void StalemateIsADraw()
        {
            ChessGame game = Play(ChessGame.FromFen("7k/8/6K1/8/8/8/5Q2/8 w - - 0 1"), "Qf7");
            Assert.AreEqual("Qf7", game.LastMove.san);
            Assert.AreEqual(GameResult.Draw, game.Result);
            Assert.AreEqual(EndReason.Stalemate, game.Reason);
            Assert.IsNull(game.Winner);
            Assert.AreEqual(0, game.Points);
            Assert.IsFalse(game.InCheck);
        }

        [Test]
        public void ThirdRepetitionIsADraw()
        {
            ChessGame game = Play(ChessGame.NewGame(), "Nf3", "Nf6", "Ng1", "Ng8", "Nf3", "Nf6", "Ng1");
            Assert.IsFalse(game.IsOver);
            Assert.AreEqual(2, game.RepetitionCount());
            Play(game, "Ng8");
            Assert.AreEqual(3, game.RepetitionCount());
            Assert.AreEqual(GameResult.Draw, game.Result);
            Assert.AreEqual(EndReason.Repetition, game.Reason);
            Assert.AreEqual(18, game.positions.Count);
        }

        [Test]
        public void AnEnPassantNobodyCanTakeDoesNotMakeANewPosition()
        {
            ChessGame stepped = ChessGame.FromFen("4k3/8/8/8/4P3/8/8/4K3 b - e3 0 1");
            ChessGame plain = ChessGame.FromFen("4k3/8/8/8/4P3/8/8/4K3 b - - 0 1");
            Assert.AreEqual(Sq("e3"), stepped.enPassant);
            Assert.AreEqual(plain.positions, stepped.positions);
            ChessGame takeable = ChessGame.FromFen("4k3/8/8/8/3pP3/8/8/4K3 b - e3 0 1");
            ChessGame notTakeable = ChessGame.FromFen("4k3/8/8/8/3pP3/8/8/4K3 b - - 0 1");
            Assert.AreNotEqual(notTakeable.positions, takeable.positions);
        }

        [Test]
        public void HundredQuietPliesAreADraw()
        {
            ChessGame game = ChessGame.FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 98 80");
            Play(game, "Ra2");
            Assert.IsFalse(game.IsOver);
            Play(game, "Kd7");
            Assert.AreEqual(100, game.halfmoveClock);
            Assert.AreEqual(GameResult.Draw, game.Result);
            Assert.AreEqual(EndReason.FiftyMoves, game.Reason);

            ChessGame mate = Play(ChessGame.FromFen("7k/8/6K1/8/8/8/8/R7 w - - 99 80"), "Ra8#");
            Assert.AreEqual(EndReason.Checkmate, mate.Reason, "a mate on the hundredth ply still wins");
            Assert.AreEqual(GameResult.LightWins, mate.Result);

            ChessGame pawn = Play(ChessGame.FromFen("4k3/8/8/8/8/8/4P3/R3K3 w - - 60 80"), "e4");
            Assert.AreEqual(0, pawn.halfmoveClock);
            Assert.AreEqual(2, pawn.positions.Count, "no earlier position can stand again");
        }

        [Test]
        public void InsufficientMaterialIsADraw()
        {
            ChessGame game = Play(ChessGame.FromFen("4k3/8/8/8/8/8/3q4/4K3 w - - 0 1"), "Kxd2");
            Assert.AreEqual(GameResult.Draw, game.Result);
            Assert.AreEqual(EndReason.InsufficientMaterial, game.Reason);
            Assert.IsTrue(ChessGame.FromFen("4k3/8/8/8/8/8/8/2B1K3 w - - 0 1").InsufficientMaterial(), "K+B v K");
            Assert.IsTrue(ChessGame.FromFen("4k3/8/8/8/8/8/8/1N2K3 w - - 0 1").InsufficientMaterial(), "K+N v K");
            Assert.IsTrue(ChessGame.FromFen("4kb2/8/8/8/8/8/8/2B1K3 w - - 0 1").InsufficientMaterial(), "bishops on dark squares");
            Assert.IsFalse(ChessGame.FromFen("4k1b1/8/8/8/8/8/8/2B1K3 w - - 0 1").InsufficientMaterial(), "bishops of both colours");
            Assert.IsFalse(ChessGame.FromFen("4k3/8/8/8/8/8/8/1NN1K3 w - - 0 1").InsufficientMaterial(), "two knights");
            Assert.IsFalse(ChessGame.FromFen("4k3/8/8/8/8/8/4P3/4K3 w - - 0 1").InsufficientMaterial(), "a pawn");
            Assert.IsFalse(ChessGame.FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 0 1").InsufficientMaterial(), "a rook");
            ChessGame loaded = ChessGame.FromFen("4k3/8/8/8/8/8/8/2B1K3 w - - 0 1");
            Assert.IsTrue(loaded.IsOver, "a dead position is over as soon as it is set up");
            Assert.AreEqual(EndReason.InsufficientMaterial, loaded.Reason);
        }

        [Test]
        public void ResignAndCallOff()
        {
            ChessGame game = ChessGame.NewGame();
            Assert.IsTrue(game.Resign(Side.Light));
            Assert.AreEqual(GameResult.DarkWins, game.Result);
            Assert.AreEqual(EndReason.Resigned, game.Reason);
            Assert.AreEqual(0, game.Legal().Count);
            Assert.IsFalse(game.Resign(Side.Dark));
            ChessGame drawn = ChessGame.NewGame();
            drawn.CallOff(null);
            Assert.AreEqual(GameResult.Draw, drawn.Result);
            Assert.AreEqual(EndReason.CalledOff, drawn.Reason);
            ChessGame won = ChessGame.NewGame();
            won.CallOff(Side.Light);
            Assert.AreEqual(Side.Light, won.Winner);
            Assert.AreEqual(1, won.Points);
        }

        [Test]
        public void FindLegalByTheCommandNumbers()
        {
            ChessGame game = ChessGame.NewGame();
            ChessMove e4 = game.FindLegal(new[] { Sq("e2"), Sq("e4"), 0 });
            Assert.IsNotNull(e4);
            Assert.AreSame(e4, game.FindLegal(new[] { Sq("e2"), Sq("e4") }));
            Assert.AreSame(e4, game.FindLegal(Sq("e2"), Sq("e4")));
            Assert.AreEqual(ChessMove.DoublePush, e4.flags & ChessMove.DoublePush);
            Assert.IsNull(game.FindLegal(new[] { Sq("e2"), Sq("e4"), 0, 1 }));
            Assert.IsNull(game.FindLegal(new[] { Sq("e2") }));
            Assert.IsNull(game.FindLegal((int[])null));
            Assert.IsNull(game.FindLegal(new[] { Sq("e2"), Sq("e5"), 0 }));
            Assert.IsNull(game.FindLegal(new[] { Sq("e2"), Sq("e4"), (int)ChessPieceKind.Queen }));
            Assert.IsNull(game.FindLegal(new[] { Sq("e7"), Sq("e5"), 0 }), "not White's piece");
            Assert.AreEqual(2, game.LegalFrom(Sq("e2")).Count);
            Assert.AreEqual(0, game.LegalFrom(Sq("e1")).Count);
            Assert.AreEqual(0, game.LegalFrom(Sq("e4")).Count);
        }

        [Test]
        public void PlayTakesOnlyLegalMovesAndFillsThemIn()
        {
            ChessGame game = ChessGame.NewGame();
            uint before = game.Checksum();
            Assert.IsFalse(game.Play(null));
            Assert.IsFalse(game.Play(new ChessMove { from = Sq("e2"), to = Sq("e5") }));
            Assert.IsFalse(game.Play(new ChessMove { from = Sq("e7"), to = Sq("e5") }));
            Assert.AreEqual(before, game.Checksum());
            var move = new ChessMove { from = Sq("e2"), to = Sq("e4") };
            Assert.IsTrue(game.Play(move));
            Assert.AreEqual("e4", move.san);
            Assert.AreEqual(ChessPieces.Make(Side.Light, ChessPieceKind.Pawn), move.piece);
            Assert.AreEqual(ChessMove.DoublePush, move.flags);
            Assert.IsFalse(move.IsCapture);
            Assert.AreNotSame(move, game.LastMove);
            Assert.AreEqual("e4", game.LastMove.san);
            Assert.AreEqual("e2e4", game.LastMove.Uci);
            Assert.AreEqual(Side.Dark, game.SideToMove);
            game.lastMove = new ChessMove();
            Assert.IsNull(game.LastMove, "a save without a last move loads an empty one");
        }

        [Test]
        public void CopiesAreIndependent()
        {
            ChessGame game = Play(ChessGame.NewGame(), "e4", "d5", "exd5");
            ChessGame copy = game.Clone();
            Assert.AreEqual(game.ToFen(), copy.ToFen());
            Assert.AreEqual(game.Checksum(), copy.Checksum());
            Assert.AreEqual("exd5", copy.LastMove.san);
            Play(copy, "Qxd5", "Nc3");
            Assert.AreEqual("rnbqkbnr/ppp1pppp/8/3P4/8/8/PPPP1PPP/RNBQKBNR b KQkq - 0 2", game.ToFen());
            Assert.AreEqual(new List<int> { -1 }, game.captured);
            Assert.AreEqual(2, copy.captured.Count);
            Assert.AreEqual(2, game.positions.Count);
            Assert.AreEqual("exd5", game.LastMove.san);
            Assert.AreNotEqual(game.Checksum(), copy.Checksum());
            Assert.IsInstanceOf<ChessGame>(game.CloneGame());
            Assert.AreEqual(game.Checksum(), game.CloneGame().Checksum());
        }

        [Test]
        public void ChecksumChangesWithEveryMoveAndMatchesEqualGames()
        {
            ChessGame one = ChessGame.NewGame();
            ChessGame two = ChessGame.NewGame();
            Assert.AreEqual(one.Checksum(), two.Checksum());
            var seen = new HashSet<uint> { one.Checksum() };
            foreach (string san in new[] { "d4", "Nf6", "c4", "e6", "Nc3", "Bb4", "Qc2", "O-O", "a3", "Bxc3+", "Qxc3" })
            {
                Play(one, san);
                Play(two, san);
                Assert.AreEqual(one.Checksum(), two.Checksum(), san);
                Assert.IsTrue(seen.Add(one.Checksum()), "a new checksum after " + san);
            }
            Assert.AreNotEqual(ChessGame.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1").Checksum(),
                ChessGame.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w Kkq - 0 1").Checksum(), "castling rights count");
        }

        [Test]
        public void MaterialCountsThePiecesLeft()
        {
            ChessGame game = Play(ChessGame.NewGame(), "e4", "d5", "exd5", "Qxd5", "Nc3", "Qxa2", "Rxa2");
            Assert.AreEqual(4000 - 100 - 100, game.Material(Side.Light));
            Assert.AreEqual(4000 - 100 - 900, game.Material(Side.Dark));
            Assert.AreEqual(new List<int> { -1, 1, 1, -5 }, game.captured);
        }

        [Test]
        public void SanOfLeavesTheGameAsItWas()
        {
            ChessGame game = Play(ChessGame.NewGame(), "e4", "e5", "Qh5", "Nc6", "Bc4", "Nf6");
            uint before = game.Checksum();
            ChessMove mate = game.FindLegal(Sq("h5"), Sq("f7"));
            Assert.AreEqual("Qxf7#", game.SanOf(mate));
            Assert.AreEqual(before, game.Checksum());
            Assert.IsFalse(game.IsOver);
            Assert.IsTrue(game.Play(mate));
            Assert.AreEqual(EndReason.Checkmate, game.Reason);
            Assert.AreEqual(Side.Light, game.Winner);
        }
    }
}
