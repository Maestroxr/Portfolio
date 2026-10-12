using System.Collections.Generic;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The adversarial review of the rules engine: hand-made positions where the legal moves are worked out from the
    /// rules themselves (flying king landings and continuations, pieces taken but still standing, rings, promotion in
    /// each variant, the majority rule, the quiet-ply clock, ambiguous paths, the table's refusals and the computer's
    /// choices), plus the move generator against an independent, slow reference generator over random positions and
    /// random games of all three variants. Tests marked Ignore are review findings: they fail against the current
    /// code and say what the rule asks for.
    /// </summary>
    public class ReviewTests
    {
        private const sbyte Taken = 100;

        private static List<string> Texts(CheckersGame game)
        {
            return Positions.Texts(game.Legal());
        }

        // ---------------------------------------------------------------- flying kings: landings and continuations

        [Test]
        public void RussianFlyingKingLandsOnEveryEmptySquareBeyondTheVictimUpToTheNextPiece()
        {
            // The king on 0,0 takes 2,2 and may land on 3,3, 4,4 or 5,5; its own man on 6,6 stops the slide there.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 2 2", "l 6 6");
            CollectionAssert.AreEqual(new[] { "1x28", "1x37", "1x46" }, Texts(game));
            foreach (Move move in game.Legal())
            {
                CollectionAssert.AreEqual(new[] { Positions.Square(8, 2, 2) }, move.captured);
                Assert.IsFalse(move.promotes);
            }
        }

        [Test]
        public void RussianKingMayNotStopWhileItCanStillCapture()
        {
            // 0,0 x 1,1 can only land on 2,2 (own man on 3,3); from there 3,1 must be taken as well: 1x19 alone is no move.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 1 1", "l 3 3", "d 3 1");
            CollectionAssert.AreEqual(new[] { "1x19x33" }, Texts(game));
        }

        [Test]
        public void RussianKingMustLandWhereItCanGoOnCapturing()
        {
            // The king on 1 takes the man on 19 and may land on 28, 37, 46, 55 or 64; only from 46 can it go on (the man
            // on 39, with 32 free behind it). A capture goes on while another capture is possible, so stopping on 28, 37,
            // 55 or 64 leaves a capture on the board: the only legal move is 1x46x32.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 2 2", "d 4 6");
            List<string> moves = Texts(game);
            Assert.Contains("1x46x32", moves);
            Assert.AreEqual(new[] { "1x46x32" }, moves.ToArray(), "a king may not stop where it could still capture: " + string.Join(" ", moves));
        }

        [Test]
        public void RussianManCrownedMidCaptureMustLandWhereTheKingCanGoOn()
        {
            // 5,1 x 6,2 lands on 7,3 and is crowned; the king takes 6,4 and may land on 5,5, 4,6 or 3,7. Only from 5,5 is
            // there another capture (4,4, with 3,3 .. 0,0 free behind it), so the legal moves are the four through 5,5.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "l 5 1", "d 6 2", "d 6 4", "d 4 4");
            List<string> moves = Texts(game);
            CollectionAssert.AreEquivalent(new[] { "42x60x46x28", "42x60x46x19", "42x60x46x10", "42x60x46x1" }, moves);
            foreach (Move move in game.Legal())
            {
                Assert.AreEqual(3, move.Jumps);
                Assert.IsTrue(move.promotes);
            }
        }

        [Test]
        public void InternationalKingLandingIsSettledByTheMajorityRule()
        {
            // The same shape on the big board: the majority rule alone keeps the two-piece sequences only (the king
            // takes 4,6 from 5,5 and may land on 3,7, 2,8 or 1,9).
            CheckersGame game = Positions.Game(Variant.International, Side.Light, "L 0 0", "d 2 2", "d 4 6");
            IReadOnlyList<Move> moves = game.Legal();
            Assert.AreEqual(3, moves.Count, string.Join(" ", Positions.Texts(moves)));
            foreach (Move move in moves)
            {
                Assert.AreEqual(2, move.Jumps);
                Assert.AreEqual(Positions.Square(10, 5, 5), move.path[1]);
            }
        }

        // ---------------------------------------------------------------- pieces taken but still standing

        [Test]
        public void RussianKingNeverJumpsThePieceItHasJustTaken()
        {
            // After 0,0 x 1,1 the king stands on 2,2 .. 7,7 with the taken man behind it: no way back over it.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 1 1");
            CollectionAssert.AreEqual(new[] { "1x19", "1x28", "1x37", "1x46", "1x55", "1x64" }, Texts(game));
            foreach (Move move in game.Legal())
            {
                CollectionAssert.AreEqual(new[] { Positions.Square(8, 1, 1) }, move.captured);
            }
        }

        [Test]
        public void ATakenPieceStillBlocksTheKingUntilTheSequenceEnds()
        {
            // The Turkish strike: 2,2 x 3,3 lands on 4,4 (own man on 5,5). Looking back from 4,4 the taken man on 3,3
            // is still there, so the man on 1,1 (0,0 free behind it) cannot be reached in the same move. Taking 1,1
            // first is a separate single capture.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 2 2", "d 3 3", "l 5 5", "d 1 1");
            CollectionAssert.AreEqual(new[] { "19x1", "19x37" }, Texts(game));
            Assert.IsTrue(game.Play(game.FindLegal(new[] { Positions.Square(8, 2, 2), Positions.Square(8, 4, 4) })));
            Assert.AreEqual(Pieces.DarkMan, game.board[1, 1], "the shielded man is still on the board");
            Assert.AreEqual(Pieces.None, game.board[3, 3]);
            Assert.AreEqual(Pieces.LightKing, game.board[4, 4]);
            Assert.IsFalse(game.IsOver);
        }

        [Test]
        public void AKingMaySlideOverItsVacatedOriginToCaptureOn()
        {
            // 3,3 x 4,4 -> 5,5, x 4,6 -> 3,7, x 2,6 -> 1,5, then over 2,4 and the empty origin 3,3 to take 4,2 and land
            // on 5,1 or 6,0: four pieces in one move.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 3 3", "d 4 4", "l 6 6", "d 4 6", "d 2 6", "d 4 2");
            List<string> moves = Texts(game);
            Assert.Contains("28x46x32x14x42", moves);
            Assert.Contains("28x46x32x14x49", moves);
        }

        [Test]
        public void AManMayComeHomeRoundARingOfFour()
        {
            // Russian men capture backward: the man on 2,2 takes 3,3 / 5,3 / 5,1 / 3,1 and lands back on its own square.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "l 2 2", "d 3 3", "d 5 3", "d 5 1", "d 3 1");
            IReadOnlyList<Move> moves = game.Legal();
            Assert.AreEqual(2, moves.Count, string.Join(" ", Positions.Texts(moves)));
            foreach (Move move in moves)
            {
                Assert.AreEqual(4, move.Jumps);
                Assert.AreEqual(Positions.Square(8, 2, 2), move.To);
                Assert.IsFalse(move.promotes);
            }
            Assert.IsTrue(game.Play(moves[0]));
            Assert.AreEqual(Pieces.LightMan, game.board[2, 2]);
            Assert.AreEqual(0, game.board.Count(Side.Dark));
            Assert.IsTrue(game.IsOver);
            Assert.AreEqual(GameResult.LightWins, game.Result);
        }

        [Test]
        public void EnglishKingGoesRoundARingAndComesHome()
        {
            // Short jumps round the same ring, either way round; the king ends where it started and Dark has nothing left.
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "L 2 2", "d 3 3", "d 5 3", "d 5 1", "d 3 1");
            CollectionAssert.AreEqual(new[] { "19x33x51x37x19", "19x37x51x33x19" }, Texts(game));
            foreach (Move move in game.Legal())
            {
                Assert.AreEqual(4, move.Jumps);
                Assert.IsFalse(move.promotes);
            }
            Assert.IsTrue(game.Play(game.Legal()[0]));
            Assert.AreEqual(Pieces.LightKing, game.board[2, 2]);
            Assert.AreEqual(0, game.board.Count(Side.Dark));
            Assert.AreEqual(GameResult.LightWins, game.Result);
            Assert.AreEqual(EndReason.NoMoves, game.Reason);
        }

        // ---------------------------------------------------------------- promotion in each variant

        [Test]
        public void RussianManCrownedMidCaptureFliesOn()
        {
            // 5,1 x 6,2 lands on 7,3 and is crowned; the new king takes 5,5 at a distance (6,4 empty) and lands on 4,6
            // or 3,7: two moves, both promoting, neither available to a man.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "l 5 1", "d 6 2", "d 5 5");
            List<string> moves = Texts(game);
            CollectionAssert.AreEquivalent(new[] { "42x60x39", "42x60x32" }, moves);
            foreach (Move move in game.Legal())
            {
                Assert.IsTrue(move.promotes);
            }
            Assert.IsTrue(game.Play(game.Legal()[0]));
            Assert.AreEqual(1, game.board.Count(Side.Light, true));
            Assert.AreEqual(0, game.board.Count(Side.Dark));
        }

        [Test]
        public void InternationalManOnTheBackRankMayNotGoOnAsAKing()
        {
            // 7,1 x 8,2 lands on 9,3; the man on 7,5 is a king's capture (8,4 empty) but not a man's: the sequence ends
            // on the back rank and the man is crowned there.
            CheckersGame game = Positions.Game(Variant.International, Side.Light, "l 7 1", "d 8 2", "d 7 5");
            IReadOnlyList<Move> moves = game.Legal();
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(1, moves[0].Jumps);
            Assert.IsTrue(moves[0].promotes);
            Assert.AreEqual(Positions.Square(10, 9, 3), moves[0].To);
        }

        [Test]
        public void InternationalManIsCrownedWhenTheSequenceLeavesAndReturnsToTheBackRank()
        {
            // 7,1 x 8,2 -> 9,3 (not crowned yet), x 8,4 backward -> 7,5, x 8,6 -> 9,7: it ends on the back rank, so it is
            // crowned there, and not before.
            CheckersGame game = Positions.Game(Variant.International, Side.Light, "l 7 1", "d 8 2", "d 8 4", "d 8 6");
            CollectionAssert.AreEqual(new[] { "72x94x76x98" }, Texts(game));
            Move move = game.Legal()[0];
            Assert.AreEqual(3, move.Jumps);
            Assert.IsTrue(move.promotes);
            Assert.IsTrue(game.Play(move));
            Assert.AreEqual(Pieces.LightKing, game.board[9, 7]);
            Assert.AreEqual(0, game.board.Count(Side.Dark));
        }

        [Test]
        public void EnglishManCrownedByACaptureStopsEvenWhenAKingCouldGoOn()
        {
            // 5,1 x 6,2 lands on 7,3; a king there could take 6,4 (5,5 free), a crowned man may not.
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 5 1", "d 6 2", "d 6 4");
            IReadOnlyList<Move> moves = game.Legal();
            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(1, moves[0].Jumps);
            Assert.IsTrue(moves[0].promotes);
            Assert.IsTrue(game.Play(moves[0]));
            Assert.AreEqual(Pieces.LightKing, game.board[7, 3]);
            Assert.AreEqual(Pieces.DarkMan, game.board[6, 4]);
        }

        // ---------------------------------------------------------------- English kings, the majority rule, mandatory capture

        [Test]
        public void EnglishKingCapturesBackwardWithinOneSequenceWhereAManStops()
        {
            // 2,2 x 3,3 -> 4,4, then back over 3,5 to 2,6: one move for a king; a man takes 3,3 and stops.
            CheckersGame king = Positions.Game(Variant.English, Side.Light, "L 2 2", "d 3 3", "d 3 5");
            CollectionAssert.AreEqual(new[] { "19x37x23" }, Texts(king));
            Assert.AreEqual(2, king.Legal()[0].Jumps);
            Assert.IsFalse(king.Legal()[0].promotes);

            CheckersGame man = Positions.Game(Variant.English, Side.Light, "l 2 2", "d 3 3", "d 3 5");
            CollectionAssert.AreEqual(new[] { "19x37" }, Texts(man));
        }

        [Test]
        public void InternationalMajorityCountsPiecesAKingIsOne()
        {
            // Two men (5,3 then 7,1) outrank one king (5,5): only the two-piece sequence is legal, and it ends on row 8.
            CheckersGame game = Positions.Game(Variant.International, Side.Light, "l 4 4", "d 5 3", "d 7 1", "D 5 5");
            CollectionAssert.AreEqual(new[] { "45x63x81" }, Texts(game));
            Assert.AreEqual(2, game.Legal()[0].Jumps);
            Assert.IsFalse(game.Legal()[0].promotes);
        }

        [Test]
        public void InternationalSequencesOfEqualLengthAreAllFree()
        {
            CheckersGame game = Positions.Game(Variant.International, Side.Light, "l 4 4", "d 5 3", "d 7 1", "d 5 5", "d 7 7");
            CollectionAssert.AreEqual(new[] { "45x63x81", "45x67x89" }, Texts(game));
        }

        [Test]
        public void ACaptureIsMandatoryOverEverySlideAndStep()
        {
            // The king has twelve slides and the man two steps; the one capture (3,3 x 6,6 -> 7,7) is the only move.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 3 3", "d 6 6", "l 1 1");
            CollectionAssert.AreEqual(new[] { "28x64" }, Texts(game));
            Assert.AreEqual(0, game.LegalFrom(Positions.Square(8, 1, 1)).Count);
        }

        // ---------------------------------------------------------------- the end of a game

        [Test]
        public void ASideWhoseFlyingKingAndManAreBothBlockedLosesWhenItsTurnComes()
        {
            // Light's king on 0,0 is shut in by its own man on 1,1, which has no step (2,0 and 2,2 taken), no capture
            // (0,0 and 3,3 occupied, 0,2 only reachable backward and empty). Dark slides its king off 0,2 and wins.
            CheckersGame game = Positions.Game(Variant.Russian, Side.Dark, "L 0 0", "l 1 1", "d 2 0", "d 2 2", "d 3 3", "D 0 2");
            IReadOnlyList<Move> dark = game.Legal();
            Assert.AreEqual(7, dark.Count, string.Join(" ", Positions.Texts(dark)));
            foreach (Move move in dark)
            {
                Assert.IsFalse(move.IsCapture);
            }
            Move slide = game.FindLegal(new[] { Positions.Square(8, 0, 2), Positions.Square(8, 1, 3) });
            Assert.IsNotNull(slide);
            Assert.IsTrue(game.Play(slide));
            Assert.IsTrue(game.IsOver);
            Assert.AreEqual(GameResult.DarkWins, game.Result);
            Assert.AreEqual(EndReason.NoMoves, game.Reason);
            Assert.AreEqual(Side.Dark, game.Winner);
            Assert.AreEqual(0, MoveGenerator.Legal(game.board, Side.Light, game.Rules).Count);
            Assert.IsFalse(MoveGenerator.HasAnyMove(game.board, Side.Light, game.Rules));
        }

        [Test]
        public void QuietPliesCountBothSidesAndResetOnAManStepOrAnyCapture()
        {
            // The light man steps along the home rank's edge (0,4 -> 1,5), out of the kings' way.
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "L 0 0", "l 0 4", "D 7 7");
            game.quietPlies = 10;
            Step(game, 0, 0, 1, 1);
            Assert.AreEqual(11, game.quietPlies, "a king's step is quiet");
            Step(game, 7, 7, 6, 6);
            Assert.AreEqual(12, game.quietPlies);
            Step(game, 0, 4, 1, 5);
            Assert.AreEqual(0, game.quietPlies, "a man's step resets the count");
            Step(game, 6, 6, 5, 5);
            Assert.AreEqual(1, game.quietPlies);
            Step(game, 1, 1, 2, 2);
            Assert.AreEqual(2, game.quietPlies);
            Step(game, 5, 5, 4, 4);
            Assert.AreEqual(3, game.quietPlies);
            Step(game, 2, 2, 3, 3);
            Assert.AreEqual(4, game.quietPlies);
            Assert.AreEqual(new[] { "37x19" }, Texts(game).ToArray(), "the dark king must take the light one");
            Step(game, 4, 4, 2, 2);
            Assert.AreEqual(0, game.quietPlies, "a king's capture resets the count");
            Assert.AreEqual(8, game.ply);
            Assert.AreEqual(1, game.capturedCount[(int)Side.Dark]);
            Assert.IsFalse(game.IsOver);
        }

        [Test]
        public void DrawComesOnTheFiftiethQuietPlyAndNotBefore()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "L 0 0", "D 7 7");
            game.quietPlies = VariantRules.DrawPliesCount - 2;
            Step(game, 0, 0, 1, 1);
            Assert.AreEqual(VariantRules.DrawPliesCount - 1, game.quietPlies);
            Assert.IsFalse(game.IsOver);
            Step(game, 7, 7, 6, 6);
            Assert.AreEqual(VariantRules.DrawPliesCount, game.quietPlies);
            Assert.IsTrue(game.IsOver);
            Assert.AreEqual(GameResult.Draw, game.Result);
            Assert.AreEqual(EndReason.DrawByPlies, game.Reason);
            Assert.IsNull(game.Winner);
        }

        [Test]
        public void FiftiethQuietPlyDrawsOnlyWhenTheGameIsNotAlreadyWon()
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "L 0 0", "D 7 7");
            game.quietPlies = VariantRules.DrawPliesCount - 1;
            Assert.IsTrue(game.Play(game.Legal()[0]));
            Assert.AreEqual(GameResult.Draw, game.Result);
            Assert.AreEqual(EndReason.DrawByPlies, game.Reason);
            Assert.AreEqual(0, game.Legal().Count);
        }

        private static void Step(CheckersGame game, int fromRow, int fromCol, int toRow, int toCol)
        {
            int size = game.board.Size;
            Move move = game.FindLegal(new[] { Positions.Square(size, fromRow, fromCol), Positions.Square(size, toRow, toCol) });
            Assert.IsNotNull(move, fromRow + "," + fromCol + " -> " + toRow + "," + toCol + " among " + string.Join(" ", Texts(game)));
            Assert.IsTrue(game.Play(move));
        }

        // ---------------------------------------------------------------- paths

        [Test]
        public void FindLegalTellsTwoJumpPathsToTheSameSquareApart()
        {
            // 2,2 reaches 6,2 over 3,1 and 5,1 or over 3,3 and 5,3: the path names the pieces taken.
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 2 2", "d 3 1", "d 3 3", "d 5 1", "d 5 3");
            Assert.AreEqual(2, game.Legal().Count, string.Join(" ", Texts(game)));
            int from = Positions.Square(8, 2, 2);
            int to = Positions.Square(8, 6, 2);
            Move left = game.FindLegal(new[] { from, Positions.Square(8, 4, 0), to });
            Move right = game.FindLegal(new[] { from, Positions.Square(8, 4, 4), to });
            Assert.IsNotNull(left);
            Assert.IsNotNull(right);
            CollectionAssert.AreEqual(new[] { Positions.Square(8, 3, 1), Positions.Square(8, 5, 1) }, left.captured);
            CollectionAssert.AreEqual(new[] { Positions.Square(8, 3, 3), Positions.Square(8, 5, 3) }, right.captured);
            Assert.IsNull(game.FindLegal(new[] { from, to }), "from and to alone do not name a double jump");
            Assert.IsNull(game.FindLegal(new[] { from, Positions.Square(8, 4, 0) }), "a sequence cannot be cut short");
            Assert.IsNull(game.FindLegal(new[] { from, Positions.Square(8, 4, 0), to, from }));
            Assert.IsTrue(game.Play(left));
            Assert.AreEqual(Pieces.None, game.board[3, 1]);
            Assert.AreEqual(Pieces.None, game.board[5, 1]);
            Assert.AreEqual(Pieces.DarkMan, game.board[3, 3]);
            Assert.AreEqual(Pieces.DarkMan, game.board[5, 3]);
            Assert.AreEqual(Pieces.LightMan, game.board[6, 2]);
        }

        [Test]
        public void MovesComeOutByFromThenPathShorterPrefixFirst()
        {
            CheckersGame ring = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 1 1", "d 3 1", "d 5 1", "d 5 3", "d 3 3", "L 7 7", "d 6 4");
            AssertOrdered(ring.Legal());
            var random = new SystemRandom(31);
            foreach (Variant variant in VariantRules.All)
            {
                VariantRules rules = VariantRules.Of(variant);
                for (int n = 0; n < 40; n++)
                {
                    Board board = Scatter(rules, random);
                    AssertOrdered(MoveGenerator.Legal(board, Side.Light, rules));
                    AssertOrdered(MoveGenerator.Legal(board, Side.Dark, rules));
                }
            }
        }

        private static void AssertOrdered(IReadOnlyList<Move> moves)
        {
            for (int i = 1; i < moves.Count; i++)
            {
                Assert.Less(ComparePaths(moves[i - 1], moves[i]), 0, moves[i - 1] + " before " + moves[i]);
            }
        }

        private static int ComparePaths(Move a, Move b)
        {
            int shared = System.Math.Min(a.path.Length, b.path.Length);
            for (int i = 0; i < shared; i++)
            {
                if (a.path[i] != b.path[i])
                {
                    return a.path[i].CompareTo(b.path[i]);
                }
            }
            return a.path.Length.CompareTo(b.path.Length);
        }

        // ---------------------------------------------------------------- the table and its commands

        private static TableSetup Setup(Variant variant, int firstSeat = 0)
        {
            var setup = new TableSetup { ruleSet = (int)variant, firstSeat = firstSeat };
            setup.seats[0].name = "Ann";
            setup.seats[1].name = "Ben";
            return setup;
        }

        [Test]
        public void TableRefusesTheOtherSidesPathAndAnySeatOutOfTurn()
        {
            var table = new CheckersTable(Setup(Variant.English), 3u);
            uint before = table.Checksum();
            Move lightMove = MoveGenerator.Legal(table.Checkers.board, Side.Light, table.Checkers.Rules)[0];
            Move darkMove = table.Checkers.Legal()[0];
            Assert.AreEqual(0, table.SeatToMove, "seat 0 plays Dark, who opens the English game");
            Assert.IsFalse(table.Apply(BoardCommand.MoveAlong(0, lightMove), 1u), "the seat to move cannot move the other side's man");
            Assert.IsFalse(table.Apply(BoardCommand.MoveAlong(1, lightMove), 2u), "out of turn");
            Assert.IsFalse(table.Apply(BoardCommand.MoveAlong(1, darkMove), 3u), "out of turn, even along a legal path");
            Assert.IsFalse(table.Apply(BoardCommand.Parse(0, CommandKind.Move, "99,98"), 4u), "squares off the board are refused, not thrown");
            Assert.IsFalse(table.Apply(BoardCommand.Parse(0, CommandKind.Move, "0,9"), 5u));
            Assert.AreEqual(5, table.Applied);
            Assert.IsNull(table.LastCommand);
            Assert.AreEqual(before, table.Checksum());
            Assert.AreEqual(0, table.Checkers.ply);
            Assert.IsTrue(table.Apply(BoardCommand.MoveAlong(0, darkMove), 6u));
            Assert.AreEqual(1, table.SeatToMove);
            Move darkAgain = MoveGenerator.Legal(table.Checkers.board, Side.Dark, table.Checkers.Rules)[0];
            Assert.IsFalse(table.Apply(BoardCommand.MoveAlong(0, darkAgain), 7u), "Dark may not move twice");
            Assert.IsNull(table.LastCommand);
            Assert.AreEqual(1, table.Checkers.ply);
        }

        [Test]
        public void TwoTablesAgreeOverALogWithMovesAComputerSeatAndAResignation()
        {
            var one = new CheckersTable(Setup(Variant.Russian), 42u);
            var two = new CheckersTable(Setup(Variant.Russian), 42u);
            var random = new SystemRandom(3);
            uint stamp = 100u;
            for (int i = 0; i < 6; i++)
            {
                Relay(one, two, BoardCommand.MoveAlong(one.SeatToMove, CheckersAI.Choose(one.Checkers, 1, random)), stamp++);
            }
            Relay(one, two, BoardCommand.ToComputer(1, 2), stamp++);
            Assert.IsTrue(two.IsComputer(1));
            Assert.AreEqual(2, two.LevelOf(1));
            for (int i = 0; i < 4; i++)
            {
                Relay(one, two, BoardCommand.MoveAlong(one.SeatToMove, CheckersAI.Choose(one.Checkers, 1, random)), stamp++);
            }
            Relay(one, two, BoardCommand.Resign(0), stamp++);
            Assert.IsTrue(two.IsOver);
            Assert.AreEqual(1, two.WinnerSeat);
            Assert.AreEqual(-1, two.SeatToMove);
            Assert.AreEqual(0, two.Timeout(stamp).Count);
            Assert.AreEqual(one.Checkers.ply, two.Checkers.ply);
            Assert.AreEqual(10, two.Checkers.ply);
        }

        private static void Relay(CheckersTable one, CheckersTable two, BoardCommand command, uint stamp)
        {
            BoardCommand copy = BoardCommand.Parse(command.seat, command.Kind, command.Payload());
            Assert.IsNotNull(copy, command.Kind + " " + command.Payload());
            Assert.IsTrue(one.Apply(command, stamp));
            Assert.IsTrue(two.Apply(copy, stamp));
            Assert.AreEqual(one.Checksum(), two.Checksum(), "after " + command.Kind + " " + command.Payload());
        }

        [Test]
        public void DefaultMoveFollowsTheStampWhateverTheRoomSeed()
        {
            var one = new CheckersTable(Setup(Variant.International), 1u);
            var two = new CheckersTable(Setup(Variant.International), 999u);
            for (uint stamp = 77u; stamp < 83u; stamp++)
            {
                List<BoardCommand> madeOne = one.Timeout(stamp);
                List<BoardCommand> madeTwo = two.Timeout(stamp);
                Assert.AreEqual(1, madeOne.Count);
                Assert.AreEqual(1, madeTwo.Count);
                Assert.AreEqual(madeOne[0].Payload(), madeTwo[0].Payload(), "stamp " + stamp);
                Assert.AreEqual(madeOne[0].seat, madeTwo[0].seat);
                Assert.AreEqual(one.Checksum(), two.Checksum());
                Assert.IsNotNull(one.Checkers.lastMove);
                Assert.IsTrue(one.Checkers.lastMove.PathIs(madeOne[0].path));
            }
        }

        [Test]
        public void AFinishedSaveMakesAFinishedTable()
        {
            var saved = new CheckersGame(Variant.English);
            Assert.IsTrue(saved.Resign(Side.Light));
            var table = new CheckersTable(Setup(Variant.English), 5u, saved);
            Assert.IsTrue(table.IsOver);
            Assert.AreEqual(-1, table.SeatToMove);
            Assert.AreEqual(0, table.WinnerSeat, "seat 0 plays Dark, who was resigned to");
            Assert.AreEqual(0, table.Timeout(1u).Count);
            Assert.IsFalse(table.Apply(BoardCommand.Parse(0, CommandKind.Move, "40,33"), 2u));
            Assert.IsFalse(table.Apply(BoardCommand.Resign(1), 3u));
        }

        [TestCase("1,2,")]
        [TestCase(",1,2")]
        [TestCase("1;2")]
        [TestCase("1,2\n")]
        [TestCase("1\t,2")]
        [TestCase("0x1,9")]
        [TestCase("9,1e1")]
        [TestCase(" 1,9")]
        [TestCase("1,9 ")]
        [TestCase("1,,")]
        [TestCase("1,100")]
        [TestCase("2147483648,1")]
        public void ParseRefusesJunkMovePayloads(string payload)
        {
            Assert.IsNull(BoardCommand.Parse(0, CommandKind.Move, payload));
        }

        [TestCase("2,")]
        [TestCase(" 1")]
        [TestCase("1 ")]
        [TestCase("0x1")]
        [TestCase("1.0")]
        [TestCase("1\n")]
        public void ParseRefusesJunkLevels(string payload)
        {
            Assert.IsNull(BoardCommand.Parse(0, CommandKind.SeatToComputer, payload));
        }

        [TestCase(" ")]
        [TestCase("0")]
        [TestCase(",")]
        public void ParseRefusesAResignationWithAPayload(string payload)
        {
            Assert.IsNull(BoardCommand.Parse(0, CommandKind.Resign, payload));
        }

        // ---------------------------------------------------------------- the computer

        [TestCase(Variant.English, 301)]
        [TestCase(Variant.Russian, 302)]
        [TestCase(Variant.International, 303)]
        public void ComputerAlwaysChoosesALegalMoveAndTakesWhenItMust(Variant variant, int seed)
        {
            VariantRules rules = VariantRules.Of(variant);
            var random = new SystemRandom(seed);
            int tried = 0;
            for (int n = 0; tried < 15 && n < 200; n++)
            {
                var game = new CheckersGame(variant);
                game.board = Scatter(rules, random);
                game.sideToMove = (int)(n % 2 == 0 ? Side.Light : Side.Dark);
                IReadOnlyList<Move> legal = game.Legal();
                if (legal.Count < 2)
                {
                    continue;
                }
                tried++;
                int level = tried % 5 == 0 ? 2 : tried % 2;
                Move chosen = CheckersAI.Choose(game.Clone(), level, new SystemRandom(n));
                Assert.IsNotNull(chosen, "position " + n + " level " + level);
                bool found = false;
                foreach (Move move in legal)
                {
                    found |= move.SamePathAs(chosen);
                }
                Assert.IsTrue(found, "position " + n + " level " + level + ": " + chosen + " is not among " + string.Join(" ", Positions.Texts(legal)));
                if (legal[0].IsCapture)
                {
                    Assert.IsTrue(chosen.IsCapture, "a capture was on and the computer did not take");
                }
                Assert.IsTrue(game.Play(chosen));
            }
            Assert.AreEqual(15, tried);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ComputerTakesWithTheFlyingKingRatherThanSliding(int level)
        {
            CheckersGame game = Positions.Game(Variant.Russian, Side.Light, "L 3 3", "d 6 6", "l 1 1", "d 7 1");
            Move move = CheckersAI.Choose(game, level, new SystemRandom(level));
            Assert.IsNotNull(move);
            Assert.IsTrue(move.IsCapture);
            Assert.AreEqual(Positions.Square(8, 6, 6), move.captured[0]);
            Assert.AreEqual(Positions.Square(8, 7, 7), move.To);
        }

        // ---------------------------------------------------------------- the generator against a reference

        [TestCase(Variant.English, 101)]
        [TestCase(Variant.Russian, 102)]
        [TestCase(Variant.International, 103)]
        public void LegalAgreesWithTheReferenceOnRandomPositions(Variant variant, int seed)
        {
            VariantRules rules = VariantRules.Of(variant);
            var random = new SystemRandom(seed);
            for (int n = 0; n < 1500; n++)
            {
                Board board = Scatter(rules, random);
                foreach (Side side in new[] { Side.Light, Side.Dark })
                {
                    Compare(board, side, rules, "scatter " + n);
                }
            }
        }

        [TestCase(Variant.English, 201)]
        [TestCase(Variant.Russian, 202)]
        [TestCase(Variant.International, 203)]
        public void LegalAgreesWithTheReferenceAlongRandomGames(Variant variant, int seed)
        {
            var random = new SystemRandom(seed);
            for (int g = 0; g < 12; g++)
            {
                var game = new CheckersGame(variant);
                while (!game.IsOver && game.ply < 200)
                {
                    Compare(game.board, game.SideToMove, game.Rules, "game " + g + " ply " + game.ply);
                    IReadOnlyList<Move> legal = game.Legal();
                    Assert.IsTrue(game.Play(legal[random.Range(0, legal.Count)]));
                }
            }
        }

        private static void Compare(Board board, Side side, VariantRules rules, string where)
        {
            List<string> expected = Reference(board, side, rules);
            List<string> actual = Encode(MoveGenerator.Legal(board, side, rules));
            expected.Sort(string.CompareOrdinal);
            actual.Sort(string.CompareOrdinal);
            if (!SameLists(expected, actual))
            {
                Assert.Fail(where + " " + side + " on\n" + Draw(board) + "\nexpected: " + string.Join(" ", expected)
                    + "\nactual:   " + string.Join(" ", actual));
            }
            Assert.AreEqual(expected.Count > 0, MoveGenerator.HasAnyMove(board, side, rules), where + " HasAnyMove " + side + " on\n" + Draw(board));
        }

        private static bool SameLists(List<string> a, List<string> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Board Scatter(VariantRules rules, SystemRandom random)
        {
            var board = new Board(rules.Size);
            int density = 20 + random.Range(0, 50);
            for (int row = 0; row < rules.Size; row++)
            {
                for (int col = 0; col < rules.Size; col++)
                {
                    if (!Board.Playable(row, col) || random.Range(0, 100) >= density)
                    {
                        continue;
                    }
                    Side side = random.Range(0, 2) == 0 ? Side.Light : Side.Dark;
                    bool king = random.Range(0, 100) < 35 || rules.IsCrowningRow(side, row);
                    board[row, col] = king ? Pieces.King(side) : Pieces.Man(side);
                }
            }
            return board;
        }

        private static string Draw(Board board)
        {
            var text = new System.Text.StringBuilder();
            for (int row = board.size - 1; row >= 0; row--)
            {
                for (int col = 0; col < board.size; col++)
                {
                    sbyte piece = board[row, col];
                    text.Append(piece == Pieces.LightMan ? 'l' : piece == Pieces.LightKing ? 'L' : piece == Pieces.DarkMan ? 'd'
                        : piece == Pieces.DarkKing ? 'D' : Board.Playable(row, col) ? '.' : ' ');
                }
                text.Append('\n');
            }
            return text.ToString();
        }

        private static List<string> Encode(IReadOnlyList<Move> moves)
        {
            var texts = new List<string>();
            foreach (Move move in moves)
            {
                texts.Add(Encode(move.path, move.captured, move.promotes));
            }
            return texts;
        }

        private static string Encode(IList<int> path, IList<int> captured, bool promotes)
        {
            return string.Join(",", path) + "|" + string.Join(",", captured) + "|" + (promotes ? "K" : "");
        }

        // The reference: the same rules written the slow and obvious way, on a copy of the cells where the mover's
        // origin is cleared and the pieces taken so far are marked, so they block a slide and cannot be jumped again.
        // A flying king lands only where it can go on capturing when some landing square allows that (review finding 1,
        // also pinned by the hand-made Russian positions above).
        private static List<string> Reference(Board board, Side side, VariantRules rules)
        {
            var results = new List<string>();
            var counts = new List<int>();
            int size = board.size;
            for (int square = 0; square < board.cells.Length; square++)
            {
                sbyte piece = board.cells[square];
                if (!Pieces.Belongs(piece, side))
                {
                    continue;
                }
                var cells = (sbyte[])board.cells.Clone();
                cells[square] = Pieces.None;
                var path = new List<int> { square };
                var captured = new List<int>();
                RefJumps(cells, size, side, rules, square, Pieces.IsKing(piece), Pieces.IsMan(piece), path, captured, results, counts);
            }
            if (results.Count == 0)
            {
                RefPlain(board, side, rules, results);
                return results;
            }
            if (rules.MajorityCapture)
            {
                int most = 0;
                foreach (int count in counts)
                {
                    most = System.Math.Max(most, count);
                }
                var longest = new List<string>();
                for (int i = 0; i < results.Count; i++)
                {
                    if (counts[i] == most)
                    {
                        longest.Add(results[i]);
                    }
                }
                return longest;
            }
            return results;
        }

        private static void RefPlain(Board board, Side side, VariantRules rules, List<string> results)
        {
            int size = board.size;
            int forward = VariantRules.Forward(side);
            for (int square = 0; square < board.cells.Length; square++)
            {
                sbyte piece = board.cells[square];
                if (!Pieces.Belongs(piece, side))
                {
                    continue;
                }
                bool king = Pieces.IsKing(piece);
                int row = square / size;
                int col = square % size;
                for (int dr = -1; dr <= 1; dr += 2)
                {
                    for (int dc = -1; dc <= 1; dc += 2)
                    {
                        if (!king && dr != forward)
                        {
                            continue;
                        }
                        int reach = king && rules.FlyingKings ? size : 1;
                        for (int step = 1; step <= reach; step++)
                        {
                            int r = row + dr * step;
                            int c = col + dc * step;
                            if (r < 0 || r >= size || c < 0 || c >= size || board[r, c] != Pieces.None)
                            {
                                break;
                            }
                            bool promotes = !king && rules.IsCrowningRow(side, r);
                            results.Add(Encode(new[] { square, r * size + c }, new int[0], promotes));
                        }
                    }
                }
            }
        }

        private static void RefJumps(sbyte[] cells, int size, Side side, VariantRules rules, int square, bool king,
            bool startedAsMan, List<int> path, List<int> captured, List<string> results, List<int> counts)
        {
            bool any = false;
            int row = square / size;
            int col = square % size;
            int forward = VariantRules.Forward(side);
            Side enemy = Sides.Other(side);
            for (int dr = -1; dr <= 1; dr += 2)
            {
                for (int dc = -1; dc <= 1; dc += 2)
                {
                    if (!king && dr != forward && !rules.MenCaptureBackward)
                    {
                        continue;
                    }
                    int reach = king && rules.FlyingKings ? size : 1;
                    int victim = -1;
                    for (int step = 1; step <= reach; step++)
                    {
                        int r = row + dr * step;
                        int c = col + dc * step;
                        if (r < 0 || r >= size || c < 0 || c >= size)
                        {
                            break;
                        }
                        sbyte there = cells[r * size + c];
                        if (there == Pieces.None)
                        {
                            continue;
                        }
                        if (there != Taken && Pieces.Belongs(there, enemy))
                        {
                            victim = r * size + c;
                        }
                        break;
                    }
                    if (victim < 0)
                    {
                        continue;
                    }
                    int victimRow = victim / size;
                    int victimCol = victim % size;
                    // Every empty square beyond the piece is a landing square; when any of them allows a further jump, a
                    // capture goes on from there, so the others are no stops.
                    var landings = new List<int>();
                    var goesOn = new List<int>();
                    sbyte before = cells[victim];
                    cells[victim] = Taken;
                    for (int step = 1; step <= reach; step++)
                    {
                        int r = victimRow + dr * step;
                        int c = victimCol + dc * step;
                        if (r < 0 || r >= size || c < 0 || c >= size || cells[r * size + c] != Pieces.None)
                        {
                            break;
                        }
                        int square2 = r * size + c;
                        landings.Add(square2);
                        bool crowned2 = king || (rules.PromoteDuringCapture && rules.IsCrowningRow(side, r));
                        if (RefHasJump(cells, size, side, rules, square2, crowned2))
                        {
                            goesOn.Add(square2);
                        }
                    }
                    cells[victim] = before;
                    foreach (int landing in goesOn.Count > 0 ? goesOn : landings)
                    {
                        int r = landing / size;
                        any = true;
                        bool crowned = king || (rules.PromoteDuringCapture && rules.IsCrowningRow(side, r));
                        sbyte was = cells[victim];
                        cells[victim] = Taken;
                        path.Add(landing);
                        captured.Add(victim);
                        RefJumps(cells, size, side, rules, landing, crowned, startedAsMan, path, captured, results, counts);
                        captured.RemoveAt(captured.Count - 1);
                        path.RemoveAt(path.Count - 1);
                        cells[victim] = was;
                    }
                }
            }
            if (!any && captured.Count > 0)
            {
                bool promotes = startedAsMan && (king || rules.IsCrowningRow(side, row));
                results.Add(Encode(path, captured, promotes));
                counts.Add(captured.Count);
            }
        }

        /// <summary>Whether a piece standing on <paramref name="square"/> could jump from there (the taken pieces block and cannot be jumped).</summary>
        private static bool RefHasJump(sbyte[] cells, int size, Side side, VariantRules rules, int square, bool king)
        {
            int row = square / size;
            int col = square % size;
            int forward = VariantRules.Forward(side);
            Side enemy = Sides.Other(side);
            for (int dr = -1; dr <= 1; dr += 2)
            {
                for (int dc = -1; dc <= 1; dc += 2)
                {
                    if (!king && dr != forward && !rules.MenCaptureBackward)
                    {
                        continue;
                    }
                    int reach = king && rules.FlyingKings ? size : 1;
                    for (int step = 1; step <= reach; step++)
                    {
                        int r = row + dr * step;
                        int c = col + dc * step;
                        if (r < 0 || r >= size || c < 0 || c >= size)
                        {
                            break;
                        }
                        sbyte there = cells[r * size + c];
                        if (there == Pieces.None)
                        {
                            continue;
                        }
                        if (there != Taken && Pieces.Belongs(there, enemy))
                        {
                            int lr = r + dr;
                            int lc = c + dc;
                            if (lr >= 0 && lr < size && lc >= 0 && lc < size && cells[lr * size + lc] == Pieces.None)
                            {
                                return true;
                            }
                        }
                        break;
                    }
                }
            }
            return false;
        }
    }
}
