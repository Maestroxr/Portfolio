using System.Diagnostics;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The computer's backgammon: legal plays through whole games at every level, Hard stronger than Easy, the same
    /// answer for the same game and random numbers, sane cube decisions, and Hard quick in the most crowded positions.
    /// </summary>
    public class BackgammonAITests
    {
        private const Side White = Side.Light;
        private const Side Black = Side.Dark;

        /// <summary>
        /// Plays <paramref name="game"/> out with the computer on both sides, the dice from <paramref name="dice"/>; every
        /// play is checked legal. Returns the decisions taken.
        /// </summary>
        private static int PlayOut(BackgammonGame game, int whiteLevel, int blackLevel, IRandom dice, IRandom whims, int maxTurns)
        {
            int decisions = 0;
            while (!game.IsOver && game.Turn < maxTurns)
            {
                Side side = game.SideToAct;
                BoardCommand command = BackgammonAI.Choose(game, (int)side, side == White ? whiteLevel : blackLevel, whims);
                Assert.IsNotNull(command, game.Phase.ToString());
                switch (command.Kind)
                {
                    case CommandKind.Roll:
                    {
                        int a = dice.Range(1, 7);
                        int b = dice.Range(1, 7);
                        while (game.Phase == BackgammonPhase.Opening && a == b)
                        {
                            b = dice.Range(1, 7);
                        }
                        Assert.IsTrue(game.Roll(a, b));
                        break;
                    }
                    case CommandKind.Double:
                        Assert.IsTrue(game.Double(side));
                        break;
                    case CommandKind.Take:
                        Assert.IsTrue(game.Take(side));
                        break;
                    case CommandKind.Drop:
                        Assert.IsTrue(game.Drop(side));
                        break;
                    case CommandKind.Move:
                        Assert.IsNotNull(game.FindPlay(command.path), "a legal play");
                        Assert.IsTrue(game.Play(command.path));
                        break;
                    default:
                        Assert.Fail("unexpected " + command);
                        break;
                }
                decisions++;
            }
            return decisions;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryLevelFinishesAGameAgainstItself(int level)
        {
            for (int g = 0; g < 2; g++)
            {
                BackgammonGame game = BackgammonGame.NewGame(true, true, White);
                PlayOut(game, level, level, new SeededRandom(11u + (uint)(level * 7 + g)), new SeededRandom(3u + (uint)g), 2000);
                Assert.IsTrue(game.IsOver, "within 2000 turns");
                Assert.GreaterOrEqual(game.Points, 1);
                Assert.IsNotNull(game.Winner);
            }
        }

        [Test]
        public void HardBeatsEasy()
        {
            int hardWins = 0;
            int hardPoints = 0;
            int easyPoints = 0;
            for (int g = 0; g < 20; g++)
            {
                BackgammonGame game = BackgammonGame.NewGame(false, true, White);
                Side hard = g % 2 == 0 ? White : Black;
                PlayOut(game, hard == White ? 2 : 0, hard == Black ? 2 : 0, new SeededRandom(100u + (uint)g), new SeededRandom(200u + (uint)g), 2000);
                Assert.IsTrue(game.IsOver);
                if (game.Winner == hard)
                {
                    hardWins++;
                    hardPoints += game.Points;
                }
                else
                {
                    easyPoints += game.Points;
                }
            }
            TestContext.WriteLine($"Hard won {hardWins} of 20, points {hardPoints} to {easyPoints}");
            Assert.IsTrue(hardWins > 10 || hardPoints > easyPoints, $"Hard won {hardWins} of 20, points {hardPoints} to {easyPoints}");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TheSameGameAndRandomNumbersGiveTheSamePlay(int level)
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(game.Roll(4, 4));
            BackgammonPlay one = BackgammonAI.ChoosePlay(game, level, new SeededRandom(8u));
            BackgammonPlay two = BackgammonAI.ChoosePlay(game.Clone(), level, new SeededRandom(8u));
            CollectionAssert.AreEqual(one.Path(), two.Path());
            Assert.AreEqual(4, one.Count);
            Assert.IsNotNull(game.FindPlay(one.Path()));
        }

        [Test]
        public void NothingToDecideOutsideTheGame()
        {
            BackgammonGame over = BackgammonGame.NewGame(true, false, White);
            over.Resign(White);
            Assert.IsNull(BackgammonAI.Choose(over, 0, 2, null));
            Assert.IsNull(BackgammonAI.ChoosePlay(over, 1, null));
            BackgammonGame rolling = BackgammonGame.NewGame(true, true, White);
            Assert.IsNull(BackgammonAI.ChoosePlay(rolling, 1, null), "no dice yet");
            Assert.AreEqual(CommandKind.Roll, BackgammonAI.Choose(rolling, 0, 2, null).Kind, "the opening roll");
            BackgammonGame blocked = BackgammonPositions.Game(White, 2, 6, "w 10 1", "w 1 14", "b 23 2", "b 3 13");
            BackgammonPlay forced = BackgammonAI.ChoosePlay(blocked, 0, new SeededRandom(1u));
            Assert.AreEqual(1, forced.Count);
            Assert.AreEqual(6, forced.moves[0].die);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void HitsAndMakesPointsWhenItCan(int level)
        {
            // 3-1 at the start: making White's 5-point is the classic play.
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(game.Roll(3, 1));
            BackgammonPlay play = BackgammonAI.ChoosePlay(game, level, null);
            BackgammonGame after = game.Preview(play.moves);
            Assert.AreEqual(2, after.CheckersAt(BackgammonGame.PointIndex(White, 5), White), play.ToString());
            // A black blot on White's 5-point, within a direct 3 and 1 of White's checkers.
            BackgammonGame hit = BackgammonPositions.Game(White, 3, 1, "w 8 3", "w 6 5", "w 13 5", "w 24 2", "b 20 1", "b 6 5", "b 8 3", "b 13 4", "b 24 2");
            BackgammonPlay hitting = BackgammonAI.ChoosePlay(hit, level, null);
            Assert.AreEqual(1, hit.Preview(hitting.moves).bar[1], hitting.ToString());
        }

        [TestCase(1)]
        [TestCase(2)]
        public void DoublesOutADeadWonRaceAndTheOtherSideDrops(int level)
        {
            // White needs one roll to finish; Black has borne off five (no gammon) and needs several.
            BackgammonGame game = BackgammonPositions.Game(White, 0, 0, "w 1 2", "b 6 10");
            Assert.IsTrue(BackgammonAI.ShouldDouble(game, level));
            Assert.AreEqual(CommandKind.Double, BackgammonAI.Choose(game, 0, level, null).Kind);
            Assert.IsFalse(BackgammonAI.ShouldDouble(game, 0), "Easy never doubles");
            Assert.IsTrue(game.Double(White));
            for (int taker = 0; taker < BackgammonAI.Levels; taker++)
            {
                Assert.IsFalse(BackgammonAI.ShouldTake(game, taker));
                Assert.AreEqual(CommandKind.Drop, BackgammonAI.Choose(game, 1, taker, null).Kind);
            }
            Assert.Greater(BackgammonAI.WinChance(game, White, level), 900);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PlaysOnForTheGammonInsteadOfDoubling(int level)
        {
            // The same race, but Black bore off none: White wins a gammon by rolling, so it does not double.
            BackgammonGame game = BackgammonPositions.Game(White, 0, 0, "w 1 2", "b 6 15");
            Assert.IsFalse(BackgammonAI.ShouldDouble(game, level));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AHopelessSideNeitherDoublesNorTakes(int level)
        {
            // Black has three checkers on the bar against White's closed board.
            BackgammonGame game = BackgammonPositions.Game(Black, 0, 0, "w 1 2", "w 2 2", "w 3 2", "w 4 2", "w 5 2", "w 6 2", "w 8 3",
                "b 25 3", "b 6 4", "b 7 4", "b 8 4");
            Assert.IsFalse(BackgammonAI.ShouldDouble(game, level));
            Assert.AreEqual(CommandKind.Roll, BackgammonAI.Choose(game, 1, level, null).Kind);
            Assert.Less(BackgammonAI.WinChance(game, Black, level), 100);
            game.sideToAct = (int)White;
            Assert.IsTrue(game.Double(White));
            Assert.IsFalse(BackgammonAI.ShouldTake(game, level));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TheStartIsNoDoubleAndAClearTake(int level)
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsFalse(BackgammonAI.ShouldDouble(game, level));
            int chance = BackgammonAI.WinChance(game, White, level);
            Assert.That(chance, Is.InRange(400, 650));
            Assert.IsTrue(game.Double(White));
            Assert.IsTrue(BackgammonAI.ShouldTake(game, level));
            Assert.AreEqual(CommandKind.Take, BackgammonAI.Choose(game, 1, level, null).Kind);
        }

        [Test]
        public void HardDecidesQuicklyInCrowdedPositions()
        {
            // Checkers spread over many points on both sides, blots everywhere, and doubles: the most plays to weigh.
            string[][] positions =
            {
                new[] { "w 1 1", "w 2 1", "w 3 1", "w 4 1", "w 5 1", "w 6 1", "w 13 1", "w 14 1", "w 15 1", "w 16 1", "w 17 1", "w 18 1", "w 19 1", "w 20 1", "w 21 1",
                    "b 13 1", "b 14 1", "b 15 1", "b 16 1", "b 17 1", "b 18 1", "b 1 3", "b 2 3", "b 3 3" },
                new[] { "w 1 1", "w 2 1", "w 3 1", "w 4 1", "w 5 1", "w 6 1", "w 7 1", "w 8 1", "w 9 1", "w 10 1", "w 11 1", "w 12 1", "w 13 1", "w 14 1", "w 24 1",
                    "b 2 4", "b 3 4", "b 4 1", "b 5 1", "b 6 1", "b 7 1", "b 8 1", "b 9 1", "b 10 1" },
                new[] { "w 2 2", "w 4 2", "w 6 2", "w 8 2", "w 10 2", "w 12 2", "w 14 2", "w 24 1",
                    "b 2 2", "b 4 2", "b 6 2", "b 8 2", "b 10 2", "b 12 2", "b 14 2", "b 24 1" },
                new[] { "w 1 2", "w 2 2", "w 3 2", "w 4 2", "w 5 2", "w 6 2", "w 7 1", "w 9 1", "w 11 1",
                    "b 1 2", "b 2 2", "b 3 2", "b 4 2", "b 5 2", "b 6 2", "b 7 1", "b 9 1", "b 25 1" },
                new[] { "w 6 5", "w 8 3", "w 13 5", "w 24 2", "b 6 5", "b 8 3", "b 13 5", "b 24 2" }
            };
            int[][] rolls = { new[] { 1, 1 }, new[] { 2, 2 }, new[] { 3, 3 }, new[] { 4, 4 }, new[] { 2, 1 }, new[] { 6, 5 } };
            long worst = 0;
            string worstCase = "";
            int mostPlays = 0;
            // Once to warm up the runtime.
            BackgammonAI.ChoosePlay(BackgammonPositions.Game(White, 1, 1, positions[0]), 2, null);
            foreach (string[] position in positions)
            {
                foreach (int[] roll in rolls)
                {
                    BackgammonGame game = BackgammonPositions.Game(White, roll[0], roll[1], position);
                    var watch = Stopwatch.StartNew();
                    BackgammonPlay play = BackgammonAI.ChoosePlay(game, 2, null);
                    watch.Stop();
                    Assert.IsNotNull(game.FindPlay(play.Path()));
                    mostPlays = System.Math.Max(mostPlays, game.LegalPlays().Count);
                    if (watch.ElapsedMilliseconds > worst)
                    {
                        worst = watch.ElapsedMilliseconds;
                        worstCase = game.LegalPlays().Count + " plays, roll " + roll[0] + "-" + roll[1];
                    }
                }
            }
            TestContext.WriteLine($"Hard's slowest decision: {worst} ms ({worstCase}); most plays of a roll {mostPlays}");
            Assert.Greater(mostPlays, 300, "the positions are crowded");
            Assert.Less(worst, 1000, worstCase);
        }
    }
}
