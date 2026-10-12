using System.Collections.Generic;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The rules of backgammon: the starting position, entering from the bar, hitting, using as many dice as possible
    /// (the larger when only one fits), doubles, bearing off, a roll that allows nothing, the opening roll, and the
    /// step by step input of a play (NextMoves, IsCompletePlay, FindPlay, Preview); the generator checked against a plain
    /// reference of the rules in played out games.
    /// </summary>
    public class BackgammonRulesTests
    {
        private const Side White = Side.Light;
        private const Side Black = Side.Dark;

        private static BackgammonGame Game(Side toAct, int die1, int die2, params string[] checkers)
        {
            return BackgammonPositions.Game(toAct, die1, die2, checkers);
        }

        private static int[] Path(Side side, params int[] ownPoints)
        {
            return BackgammonPositions.Path(side, ownPoints);
        }

        private static CheckerMove Move(Side side, int from, int to, int die)
        {
            return BackgammonPositions.Move(side, from, to, die);
        }

        private static int Index(Side side, int ownPoint)
        {
            return BackgammonGame.PointIndex(side, ownPoint);
        }

        [Test]
        public void StartingPositionAndPipCounts()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, true, White);
            Assert.AreEqual(BackgammonPhase.Opening, game.Phase);
            Assert.AreEqual(White, game.SideToAct);
            Assert.AreEqual(RuleSet.Backgammon, game.RuleSet);
            Assert.AreEqual(167, game.PipCount(White));
            Assert.AreEqual(167, game.PipCount(Black));
            Assert.AreEqual(2, game.CheckersAt(23, White));
            Assert.AreEqual(5, game.CheckersAt(12, White));
            Assert.AreEqual(3, game.CheckersAt(7, White));
            Assert.AreEqual(5, game.CheckersAt(5, White));
            Assert.AreEqual(2, game.CheckersAt(0, Black));
            Assert.AreEqual(5, game.CheckersAt(11, Black));
            Assert.AreEqual(3, game.CheckersAt(16, Black));
            Assert.AreEqual(5, game.CheckersAt(18, Black));
            Assert.AreEqual(0, game.CheckersAt(0, White));
            Assert.AreEqual(0, game.CheckersAt(24, White), "off the board");
            int checkers = 0;
            foreach (int n in game.board)
            {
                checkers += n < 0 ? -n : n;
            }
            Assert.AreEqual(30, checkers);
            Assert.AreEqual(1, game.cubeValue);
            Assert.AreEqual(-1, game.cubeOwner);
            Assert.IsTrue(game.cube);
            Assert.IsFalse(game.IsOver);
            Assert.AreEqual(0, game.Points);
            Assert.AreEqual(0, game.Turn);
            Assert.AreEqual(-1, game.lastSide);
            Assert.AreEqual(0, game.DiceToPlay().Length);
            Assert.AreEqual(0, game.LegalPlays().Count);
            Assert.AreEqual(game.Material(White), game.Material(Black));
            Assert.AreEqual(-1, BackgammonGame.Direction(White));
            Assert.AreEqual(1, BackgammonGame.Direction(Black));
            Assert.AreEqual(5, BackgammonGame.PointIndex(White, 6));
            Assert.AreEqual(18, BackgammonGame.PointIndex(Black, 6));
            Assert.AreEqual(6, BackgammonGame.OwnPoint(Black, 18));
            Assert.AreEqual(25, BackgammonGame.OwnPoint(White, BackgammonGame.Bar));
            Assert.AreEqual(0, BackgammonGame.OwnPoint(White, BackgammonGame.Off));

            BackgammonGame plain = BackgammonGame.NewGame(false, false, Black);
            Assert.AreEqual(BackgammonPhase.Rolling, plain.Phase);
            Assert.AreEqual(Black, plain.SideToAct);
            Assert.IsFalse(plain.cube);
        }

        [Test]
        public void EnteringFromTheBarComesFirst()
        {
            // Black holds its 1 to 5-points: White's checker on the bar enters only with the 6 (on White's 19-point).
            BackgammonGame game = Game(White, 6, 3, "w 25 1", "w 13 14", "b 1 2", "b 2 2", "b 3 2", "b 4 2", "b 5 2", "b 13 5");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.AreEqual(2, plays.Count);
            foreach (BackgammonPlay play in plays)
            {
                Assert.AreEqual(2, play.Count);
                Assert.AreEqual(new CheckerMove(BackgammonGame.Bar, Index(White, 19), 6), play.moves[0]);
                Assert.IsTrue(play.moves[0].FromBar);
            }
            Assert.IsNotNull(game.FindPlay(Path(White, 25, 19, 13, 10)));
            Assert.IsNotNull(game.FindPlay(Path(White, 25, 19, 19, 16)));
            BackgammonPlay reordered = game.FindPlay(Path(White, 13, 10, 25, 19));
            Assert.IsNotNull(reordered, "the same checkers in an order that can be played");
            Assert.IsTrue(reordered.moves[0].FromBar);
            Assert.IsNull(game.FindPlay(Path(White, 25, 22, 13, 7)), "the 3 does not enter");
            Assert.IsNull(game.FindPlay(Path(White, 13, 7, 13, 10)), "the checker on the bar first");
            IReadOnlyList<CheckerMove> next = game.NextMoves(new CheckerMove[0]);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(Move(White, 25, 19, 6), next[0]);
        }

        [Test]
        public void TwoOnTheBarWithOneEntryOpenPlayOneDie()
        {
            BackgammonGame game = Game(White, 6, 3, "w 25 2", "w 13 13", "b 1 2", "b 2 2", "b 3 2", "b 4 2", "b 5 2", "b 13 5");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.AreEqual(1, plays.Count);
            Assert.AreEqual(1, plays[0].Count);
            Assert.AreEqual(Move(White, 25, 19, 6), plays[0].moves[0]);
            Assert.IsTrue(game.Play(Path(White, 25, 19)));
            Assert.AreEqual(1, game.bar[0]);
            Assert.AreEqual(Black, game.SideToAct);
        }

        [Test]
        public void AClosedBoardPassesTheTurnAtOnce()
        {
            BackgammonGame game = Game(White, 0, 0, "w 25 1", "w 13 14", "b 1 2", "b 2 2", "b 3 2", "b 4 2", "b 5 2", "b 6 2", "b 13 3");
            Assert.AreEqual(BackgammonPhase.Rolling, game.Phase);
            int turn = game.Turn;
            Assert.IsTrue(game.Roll(5, 2));
            Assert.AreEqual(BackgammonPhase.Rolling, game.Phase);
            Assert.AreEqual(Black, game.SideToAct);
            Assert.AreEqual((int)White, game.lastSide);
            Assert.AreEqual(0, game.lastPlay.Length);
            Assert.AreEqual(turn + 1, game.Turn);
            Assert.AreEqual(5, game.die1, "the dice stay for the board to show");
            Assert.AreEqual(2, game.die2);
            Assert.AreEqual(0, game.LegalPlays().Count);
            Assert.IsFalse(game.Play(Path(White, 13, 8, 13, 11)));
        }

        [Test]
        public void HittingSendsALoneCheckerToTheBar()
        {
            // A black blot on White's 10-point, two black checkers on White's 11-point.
            BackgammonGame game = Game(White, 3, 2, "w 13 15", "b 15 1", "b 14 2", "b 6 12");
            Assert.IsNull(game.FindPlay(Path(White, 13, 11, 13, 10)), "a point held by two may not be landed on");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.AreEqual(1, plays.Count);
            int blackPips = game.PipCount(Black);
            int[] path = Path(White, 13, 10, 10, 8);
            Assert.IsTrue(game.Play(path));
            Assert.AreEqual(1, game.bar[1]);
            Assert.AreEqual(0, game.CheckersAt(Index(White, 10), Black));
            Assert.AreEqual(1, game.CheckersAt(Index(White, 8), White));
            Assert.AreEqual(blackPips + 10, game.PipCount(Black));
            Assert.AreEqual(Black, game.SideToAct);
            Assert.AreEqual(BackgammonPhase.Rolling, game.Phase);
            CollectionAssert.AreEqual(path, game.lastPlay);
            Assert.AreEqual((int)White, game.lastSide);
            // Black must now enter before anything else.
            Assert.IsTrue(game.Roll(4, 1));
            foreach (BackgammonPlay play in game.LegalPlays())
            {
                Assert.IsTrue(play.moves[0].FromBar);
            }
        }

        [Test]
        public void OnlyTheOrderThatUsesBothDiceIsLegal()
        {
            // White's last checker outside, on its 10-point: a 4 first runs into the black point on 6; a 2 first goes on.
            BackgammonGame game = Game(White, 4, 2, "w 10 1", "w 1 14", "b 19 2", "b 4 13");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.AreEqual(1, plays.Count);
            Assert.AreEqual(2, plays[0].Count);
            Assert.AreEqual(Move(White, 10, 8, 2), plays[0].moves[0]);
            Assert.AreEqual(Move(White, 8, 4, 4), plays[0].moves[1]);
            Assert.IsNull(game.FindPlay(Path(White, 10, 8)), "both dice must be used");
            Assert.IsFalse(game.IsCompletePlay(new[] { Move(White, 10, 8, 2) }));
            BackgammonPlay reordered = game.FindPlay(Path(White, 8, 4, 10, 8));
            Assert.IsNotNull(reordered);
            Assert.AreEqual(Move(White, 10, 8, 2), reordered.moves[0]);
            IReadOnlyList<CheckerMove> next = game.NextMoves(new CheckerMove[0]);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(Move(White, 10, 8, 2), next[0]);
            Assert.IsTrue(game.IsCompletePlay(plays[0].moves));
        }

        [Test]
        public void WhenOnlyOneDieCanBePlayedThatIsThePlay()
        {
            // 10/6 with the 4; then the 2 has nowhere to go (4 is held, the checkers on 1 may not bear off past the one
            // on 6). A 2 first runs into the black point on 8.
            BackgammonGame game = Game(White, 4, 2, "w 10 1", "w 1 14", "b 17 2", "b 21 2", "b 2 11");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.AreEqual(1, plays.Count);
            Assert.AreEqual(1, plays[0].Count);
            Assert.AreEqual(Move(White, 10, 6, 4), plays[0].moves[0]);
            Assert.IsTrue(game.IsCompletePlay(new[] { Move(White, 10, 6, 4) }));
            Assert.AreEqual(0, game.NextMoves(new[] { Move(White, 10, 6, 4) }).Count);
            Assert.IsTrue(game.Play(Path(White, 10, 6)));
        }

        [Test]
        public void WhenEitherDieButNotBothCanBePlayedTheLargerMustBe()
        {
            // 10/4 with the 6 or 10/8 with the 2, never both: the black point on 2 stops each second move.
            BackgammonGame game = Game(White, 2, 6, "w 10 1", "w 1 14", "b 23 2", "b 3 13");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.AreEqual(1, plays.Count);
            Assert.AreEqual(1, plays[0].Count);
            Assert.AreEqual(Move(White, 10, 4, 6), plays[0].moves[0]);
            Assert.IsNull(game.FindPlay(Path(White, 10, 8)), "the smaller die may not be played");
            Assert.IsFalse(game.IsCompletePlay(new[] { Move(White, 10, 8, 2) }));
            IReadOnlyList<CheckerMove> next = game.NextMoves(new CheckerMove[0]);
            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(Move(White, 10, 4, 6), next[0]);
            CollectionAssert.AreEqual(BackgammonPositions.ReferenceResults(game), BackgammonPositions.Results(game));
        }

        [Test]
        public void DoublesPlayFourMoves()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(game.Roll(6, 6));
            Assert.IsTrue(game.IsDouble);
            CollectionAssert.AreEqual(new[] { 6, 6, 6, 6 }, game.DiceToPlay());
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.Greater(plays.Count, 1);
            foreach (BackgammonPlay play in plays)
            {
                Assert.AreEqual(4, play.Count);
            }
            Assert.IsNotNull(game.FindPlay(Path(White, 24, 18, 24, 18, 13, 7, 13, 7)));
            Assert.IsNull(game.FindPlay(Path(White, 24, 18, 24, 18, 13, 7)), "all four");
            Assert.IsNull(game.FindPlay(Path(White, 24, 18, 18, 12, 13, 7, 13, 7)), "Black holds White's 12-point");
        }

        [Test]
        public void ABlockedDoublePlaysWhatItCan()
        {
            // 2-2: 10/8/6, then the black point on 4 stops the rest (and nothing may bear off past the checker on 6).
            BackgammonGame game = Game(White, 2, 2, "w 10 1", "w 1 14", "b 21 2", "b 3 13");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.AreEqual(1, plays.Count);
            Assert.AreEqual(2, plays[0].Count);
            Assert.IsNotNull(game.FindPlay(Path(White, 10, 8, 8, 6)));
            Assert.IsNull(game.FindPlay(Path(White, 10, 8)));
        }

        [Test]
        public void BearingOffWithExactDice()
        {
            BackgammonGame game = Game(White, 6, 3, "w 6 2", "w 3 1", "b 6 15");
            Assert.IsTrue(game.Play(Path(White, 6, 0, 3, 0)));
            Assert.AreEqual(14, game.off[0]);
            Assert.AreEqual(1, game.CheckersAt(Index(White, 6), White));
            Assert.IsFalse(game.IsOver);
        }

        [Test]
        public void ALargerDieBearsOffOnlyFromTheHighestPoint()
        {
            BackgammonGame game = Game(White, 6, 1, "w 5 1", "w 2 1", "b 6 15");
            foreach (CheckerMove move in game.NextMoves(new CheckerMove[0]))
            {
                Assert.IsFalse(move.from == Index(White, 2) && move.BearsOff, "the checker on 5 is higher");
            }
            Assert.IsNull(game.FindPlay(Path(White, 2, 0, 5, 4)));
            Assert.IsNotNull(game.FindPlay(Path(White, 5, 0, 2, 1)));
            BackgammonPlay down = game.FindPlay(Path(White, 5, 4, 4, 0));
            Assert.IsNotNull(down, "5/4, then the 6 from the highest point");
            Assert.AreEqual(6, down.moves[1].die);

            BackgammonGame last = Game(White, 6, 5, "w 4 1", "w 2 1", "b 6 15");
            Assert.IsTrue(last.Play(Path(White, 4, 0, 2, 0)));
            Assert.IsTrue(last.IsOver);
            Assert.AreEqual(GameResult.LightWins, last.Result);
            Assert.AreEqual(EndReason.BorneOff, last.Reason);
            Assert.AreEqual(White, last.Winner);
            Assert.AreEqual(BackgammonPhase.Over, last.Phase);
        }

        [Test]
        public void NoBearingOffWithACheckerOutside()
        {
            BackgammonGame game = Game(White, 5, 2, "w 7 1", "w 2 1", "b 6 15");
            foreach (CheckerMove move in game.NextMoves(new CheckerMove[0]))
            {
                Assert.IsFalse(move.BearsOff);
            }
            Assert.AreEqual(1, game.LegalPlays().Count, "7/5/off and 7/2/off leave the same position: one play");
            Assert.IsNotNull(game.FindPlay(Path(White, 7, 5, 5, 0)));
            Assert.IsNotNull(game.FindPlay(Path(White, 7, 2, 2, 0)));
            Assert.IsNull(game.FindPlay(Path(White, 2, 0, 7, 5)), "after 7/5 the 5 must bear off from 5");
        }

        [Test]
        public void AHitCheckerComingBackStopsTheBearOff()
        {
            BackgammonGame game = Game(White, 6, 5, "w 25 1", "w 6 2", "b 12 15");
            IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
            Assert.Greater(plays.Count, 0);
            foreach (BackgammonPlay play in plays)
            {
                Assert.IsTrue(play.moves[0].FromBar);
                foreach (CheckerMove move in play.moves)
                {
                    Assert.IsFalse(move.BearsOff, play.ToString());
                }
            }
        }

        [Test]
        public void TheOpeningRollGivesTheHigherDieBothNumbers()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, true, White);
            Assert.IsFalse(game.Roll(4, 4), "a tie is thrown again");
            Assert.IsFalse(game.Roll(0, 3));
            Assert.IsFalse(game.Roll(7, 1));
            Assert.AreEqual(BackgammonPhase.Opening, game.Phase);
            Assert.AreEqual(0, game.Turn);
            Assert.IsTrue(game.Roll(2, 5));
            Assert.AreEqual(Black, game.SideToAct, "Black threw the 5");
            Assert.AreEqual(BackgammonPhase.Moving, game.Phase);
            Assert.AreEqual(2, game.die1, "die1 is White's");
            Assert.AreEqual(5, game.die2, "die2 is Black's");
            CollectionAssert.AreEqual(new[] { 2, 5 }, game.DiceToPlay());
            Assert.IsFalse(game.Roll(3, 1), "no second roll");
            foreach (BackgammonPlay play in game.LegalPlays())
            {
                Assert.AreEqual(2, play.Count);
            }
            Assert.IsTrue(game.Play(Path(Black, 13, 8, 13, 11)));
            Assert.AreEqual(White, game.SideToAct);
            Assert.AreEqual(BackgammonPhase.Rolling, game.Phase);
            Assert.AreEqual((int)Black, game.lastSide);
            Assert.AreEqual(2, game.Turn);

            BackgammonGame other = BackgammonGame.NewGame(false, true, White);
            Assert.IsTrue(other.Roll(6, 1));
            Assert.AreEqual(White, other.SideToAct);
            Assert.IsTrue(other.Play(Path(White, 13, 7, 8, 7)), "the bar point");
            Assert.AreEqual(2, other.CheckersAt(Index(White, 7), White));
        }

        [Test]
        public void StepByStepBuildsExactlyTheLegalPlays()
        {
            var games = new List<BackgammonGame>();
            for (int a = 1; a <= 6; a++)
            {
                for (int b = a; b <= 6; b++)
                {
                    BackgammonGame start = BackgammonGame.NewGame(true, false, White);
                    start.Roll(a, b);
                    games.Add(start);
                }
            }
            games.Add(Game(White, 6, 3, "w 25 1", "w 13 14", "b 1 2", "b 2 2", "b 3 2", "b 4 2", "b 5 2", "b 13 5"));
            games.Add(Game(White, 2, 6, "w 10 1", "w 1 14", "b 23 2", "b 3 13"));
            games.Add(Game(White, 6, 1, "w 5 1", "w 2 1", "b 6 15"));
            games.Add(Game(White, 5, 2, "w 7 1", "w 2 1", "b 6 15"));
            games.Add(Game(White, 3, 3, "w 6 3", "w 5 3", "w 4 3", "w 3 3", "w 2 2", "w 1 1", "b 18 1", "b 6 14"));
            games.Add(Game(Black, 4, 4, "b 25 1", "b 8 4", "b 6 10", "w 22 2", "w 6 13"));
            foreach (BackgammonGame game in games)
            {
                var reached = new SortedSet<string>(System.StringComparer.Ordinal);
                foreach (List<CheckerMove> sequence in BackgammonPositions.StepSequences(game))
                {
                    Assert.IsTrue(game.IsCompletePlay(sequence), BackgammonPositions.Key(game));
                    BackgammonPlay play = game.FindPlay(BackgammonPositions.PathOf(sequence));
                    Assert.IsNotNull(play);
                    reached.Add(BackgammonPositions.Key(game.Preview(sequence)));
                    Assert.AreEqual(BackgammonPositions.Key(game.Preview(sequence)), BackgammonPositions.Key(game.Preview(play.moves)));
                }
                CollectionAssert.AreEqual(BackgammonPositions.Results(game), new List<string>(reached), BackgammonPositions.Key(game));
                foreach (BackgammonPlay play in game.LegalPlays())
                {
                    Assert.IsNotNull(game.FindPlay(play.Path()));
                    Assert.IsTrue(game.IsCompletePlay(play.moves));
                }
            }
        }

        [Test]
        public void TheGeneratorAgreesWithThePlainRulesInPlayedOutGames()
        {
            var random = new SeededRandom(7);
            int compared = 0;
            for (int g = 0; g < 12; g++)
            {
                BackgammonGame game = BackgammonGame.NewGame(false, false, g % 2 == 0 ? White : Black);
                for (int t = 0; t < 600 && !game.IsOver; t++)
                {
                    int a = random.Range(1, 7);
                    int b = random.Range(1, 7);
                    Assert.IsTrue(game.Roll(a, b));
                    if (game.Phase != BackgammonPhase.Moving)
                    {
                        continue;
                    }
                    List<string> expected = BackgammonPositions.ReferenceResults(game);
                    CollectionAssert.AreEqual(expected, BackgammonPositions.Results(game), BackgammonPositions.Key(game) + " roll " + a + "-" + b);
                    compared++;
                    IReadOnlyList<BackgammonPlay> plays = game.LegalPlays();
                    BackgammonPlay chosen = plays[random.Range(0, plays.Count)];
                    Assert.IsTrue(game.Play(chosen.Path()), chosen.ToString());
                }
                Assert.IsTrue(game.IsOver);
            }
            Assert.Greater(compared, 300);
        }

        [Test]
        public void FindPlayAcceptsAnotherOrderOfTheSameCheckers()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(game.Roll(6, 1));
            BackgammonPlay one = game.FindPlay(Path(White, 13, 7, 8, 7));
            BackgammonPlay two = game.FindPlay(Path(White, 8, 7, 13, 7));
            Assert.IsNotNull(one);
            Assert.IsNotNull(two);
            Assert.AreEqual(6, one.moves[0].die);
            Assert.AreEqual(1, two.moves[0].die);
            Assert.IsNull(game.FindPlay(Path(White, 13, 7)), "one checker only");
            Assert.IsNull(game.FindPlay(new[] { 12, 6, 7 }), "odd numbers");
            Assert.IsNull(game.FindPlay(new[] { 12, 24, 7, 6 }), "a move onto the bar");
            Assert.IsNull(game.FindPlay(new[] { 25, 6, 7, 6 }), "a move from off");
            Assert.IsNull(game.FindPlay(null));
            Assert.IsTrue(game.Play(Path(White, 8, 7, 13, 7)));
            CollectionAssert.AreEqual(two.Path(), game.lastPlay);
        }

        [Test]
        public void NextMovesFollowAPartialPlay()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(game.Roll(3, 1));
            IReadOnlyList<CheckerMove> first = game.NextMoves(new CheckerMove[0]);
            Assert.Greater(first.Count, 2);
            var done = new List<CheckerMove> { Move(White, 8, 5, 3) };
            IReadOnlyList<CheckerMove> second = game.NextMoves(done);
            Assert.IsTrue(ContainsPath(second, Move(White, 6, 5, 1)));
            foreach (CheckerMove move in second)
            {
                Assert.AreEqual(1, move.die, "only the 1 is left");
            }
            CollectionAssert.AreEqual(new[] { 1 }, game.DiceLeft(done));
            done.Add(Move(White, 6, 5, 1));
            Assert.IsTrue(game.IsCompletePlay(done));
            Assert.AreEqual(0, game.NextMoves(done).Count, "a whole play");
            Assert.AreEqual(0, game.NextMoves(new[] { Move(White, 8, 4, 4) }).Count, "no die of 4");
            Assert.AreEqual(0, game.NextMoves(new[] { Move(White, 13, 10, 3), Move(White, 13, 12, 1), Move(White, 6, 5, 1) }).Count);
            Assert.IsFalse(game.IsCompletePlay(new[] { Move(White, 8, 5, 3) }));
        }

        [Test]
        public void PreviewShowsAPlayInProgress()
        {
            BackgammonGame game = Game(White, 3, 2, "w 13 15", "b 15 1", "b 14 2", "b 6 12");
            string before = BackgammonPositions.Key(game);
            uint sum = game.Checksum();
            BackgammonGame shown = game.Preview(new[] { Move(White, 13, 10, 3) });
            Assert.AreEqual(14, shown.CheckersAt(Index(White, 13), White));
            Assert.AreEqual(1, shown.CheckersAt(Index(White, 10), White));
            Assert.AreEqual(1, shown.bar[1], "the blot is hit");
            Assert.AreEqual(game.Phase, shown.Phase);
            Assert.AreEqual(game.Turn, shown.Turn);
            Assert.AreEqual(game.die1, shown.die1);
            Assert.AreEqual(before, BackgammonPositions.Key(game), "the game itself is unchanged");
            Assert.AreEqual(sum, game.Checksum());
            BackgammonGame blocked = game.Preview(new[] { Move(White, 13, 11, 2) });
            Assert.AreEqual(before, BackgammonPositions.Key(blocked), "a move onto a held point is not shown");
            CollectionAssert.AreEqual(new[] { 2 }, game.DiceLeft(new[] { Move(White, 13, 10, 3) }));
            Assert.AreEqual(before, BackgammonPositions.Key(game.Preview(null)));
        }

        [Test]
        public void CloneIsIndependentAndTheChecksumFollowsEveryDecision()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, true, White);
            BackgammonGame copy = game.Clone();
            Assert.AreEqual(game.Checksum(), copy.Checksum());
            var seen = new HashSet<uint> { game.Checksum() };
            Assert.IsTrue(game.Roll(5, 3));
            Assert.IsTrue(seen.Add(game.Checksum()));
            Assert.AreNotEqual(game.Checksum(), copy.Checksum());
            Assert.IsTrue(copy.Roll(5, 3));
            Assert.AreEqual(game.Checksum(), copy.Checksum());
            Assert.IsTrue(game.Play(Path(White, 8, 3, 6, 3)));
            Assert.IsTrue(seen.Add(game.Checksum()));
            Assert.IsTrue(game.Double(Black));
            Assert.IsTrue(seen.Add(game.Checksum()));
            Assert.IsTrue(game.Take(White));
            Assert.IsTrue(seen.Add(game.Checksum()));
            BackgammonGame later = game.Clone();
            later.board[0] = 0;
            later.bar[1] = 2;
            later.lastPlay[0] = 3;
            Assert.AreEqual(-2, game.board[0]);
            Assert.AreEqual(0, game.bar[1]);
            Assert.AreEqual(Index(White, 8), game.lastPlay[0]);
            Assert.AreNotEqual(game.Checksum(), later.Checksum());
            IBoardGame boxed = game.CloneGame();
            Assert.IsInstanceOf<BackgammonGame>(boxed);
            Assert.AreEqual(game.Checksum(), boxed.Checksum());
        }

        [Test]
        public void APlayTravelsAsACommand()
        {
            BackgammonGame game = Game(White, 6, 5, "w 25 1", "w 6 2", "b 12 15");
            BackgammonPlay play = game.LegalPlays()[0];
            BoardCommand command = BoardCommand.Play(0, play.Path());
            BoardCommand parsed = BoardCommand.Parse(0, CommandKind.Move, command.Payload());
            Assert.IsNotNull(parsed);
            CollectionAssert.AreEqual(play.Path(), parsed.path);
            Assert.AreEqual(BackgammonGame.Bar, parsed.path[0]);
            Assert.IsTrue(game.Play(parsed.path));
            foreach (CommandKind kind in new[] { CommandKind.Roll, CommandKind.Double, CommandKind.Take, CommandKind.Drop })
            {
                BoardCommand simple = BoardCommand.Parse(1, kind, "");
                Assert.IsNotNull(simple);
                Assert.AreEqual(kind, simple.Kind);
                Assert.IsTrue(BoardCommand.PlayerMaySend((uint)kind));
            }
            Assert.AreEqual("bar/19", Move(White, 25, 19, 6).ToString());
            Assert.AreEqual("(no move)", new BackgammonPlay().ToString());
        }

        private static bool ContainsPath(IReadOnlyList<CheckerMove> moves, CheckerMove move)
        {
            foreach (CheckerMove m in moves)
            {
                if (m.SamePath(move))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
