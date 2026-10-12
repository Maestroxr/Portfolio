using System.Diagnostics;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The computer player: it takes what it must, sees a win one move away, finishes whole games at every level and
    /// on every board within its budget, answers quickly on the big board, and plays better the higher its level.
    /// </summary>
    public class AITests
    {
        [Test]
        public void BudgetsAndDepthsPerLevel()
        {
            Assert.AreEqual(3, CheckersAI.Levels);
            Assert.AreEqual(1500, CheckersAI.NodeBudget(0));
            Assert.AreEqual(25000, CheckersAI.NodeBudget(1));
            Assert.AreEqual(150000, CheckersAI.NodeBudget(2));
            Assert.AreEqual(150000, CheckersAI.NodeBudget(7), "levels are clamped");
            Assert.AreEqual(2, CheckersAI.MaxDepth(0));
            Assert.AreEqual(6, CheckersAI.MaxDepth(2));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TakesAForcedCapture(int level)
        {
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "l 2 2", "l 0 0", "l 1 5", "d 3 3", "d 7 7");
            Move move = CheckersAI.Choose(game, level, new SystemRandom(1));
            Assert.IsNotNull(move);
            Assert.IsTrue(move.IsCapture);
            Assert.AreEqual(Positions.Square(8, 3, 3), move.captured[0]);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void FindsAWinOneMoveAway(int level)
        {
            // Light's king on 7,3 steps to 6,2 and the dark man on 7,1 has nowhere to go.
            CheckersGame game = Positions.Game(Variant.English, Side.Light, "L 6 0", "L 5 3", "L 7 3", "d 7 1");
            Assert.Greater(game.Legal().Count, 1);
            Move move = CheckersAI.Choose(game, level, new SystemRandom(2));
            Assert.IsNotNull(move);
            Assert.AreEqual(Positions.Square(8, 7, 3), move.From);
            Assert.AreEqual(Positions.Square(8, 6, 2), move.To);
            Assert.IsTrue(game.Play(move));
            Assert.AreEqual(GameResult.LightWins, game.Result);
        }

        [Test]
        public void NoMoveWhenThereIsNone()
        {
            CheckersGame blocked = Positions.Game(Variant.English, Side.Light, "l 0 0", "d 1 1", "d 2 2");
            Assert.IsNull(CheckersAI.Choose(blocked, 2, null));
            var over = new CheckersGame(Variant.English);
            over.Resign(Side.Dark);
            Assert.IsNull(CheckersAI.Choose(over, 1, null));
        }

        [Test]
        public void IsDeterministicForAGameLevelAndRandom()
        {
            var game = new CheckersGame(Variant.Russian);
            for (int i = 0; i < 8; i++)
            {
                game.Play(game.Legal()[i % game.Legal().Count]);
            }
            for (int level = 0; level < CheckersAI.Levels; level++)
            {
                Move one = CheckersAI.Choose(game.Clone(), level, new SystemRandom(5));
                Move two = CheckersAI.Choose(game.Clone(), level, new SystemRandom(5));
                Assert.IsTrue(one.SamePathAs(two), "level " + level);
            }
            Assert.IsTrue(CheckersAI.Choose(game, 2, null).SamePathAs(CheckersAI.Choose(game, 2, new SystemRandom(9))),
                "Hard never asks the random");
        }

        [Test]
        public void EvaluationIsAntisymmetricAndSeesTheEnd()
        {
            var game = new CheckersGame(Variant.International);
            Assert.AreEqual(0, CheckersAI.Evaluate(game, Side.Light) + CheckersAI.Evaluate(game, Side.Dark));
            CheckersGame ahead = Positions.Game(Variant.Russian, Side.Light, "L 0 0", "d 7 7");
            Assert.Greater(CheckersAI.Evaluate(ahead, Side.Light), 0);
            Assert.Less(CheckersAI.Evaluate(ahead, Side.Dark), 0);
            CheckersGame won = Positions.Game(Variant.English, Side.Dark, "l 0 0", "d 1 1", "d 3 3");
            won.Play(won.FindLegal(new[] { Positions.Square(8, 3, 3), Positions.Square(8, 2, 2) }));
            Assert.AreEqual(CheckersAI.WinValue - 1, CheckersAI.Evaluate(won, Side.Dark));
            Assert.AreEqual(-(CheckersAI.WinValue - 1), CheckersAI.Evaluate(won, Side.Light));
            var drawn = new CheckersGame(Variant.English);
            drawn.CallOff(null);
            Assert.AreEqual(0, CheckersAI.Evaluate(drawn, Side.Light));
        }

        [TestCase(Variant.English, 0)]
        [TestCase(Variant.English, 1)]
        [TestCase(Variant.English, 2)]
        [TestCase(Variant.Russian, 0)]
        [TestCase(Variant.Russian, 1)]
        [TestCase(Variant.Russian, 2)]
        [TestCase(Variant.International, 0)]
        [TestCase(Variant.International, 1)]
        [TestCase(Variant.International, 2)]
        public void EveryLevelFinishesAGameAgainstItself(Variant variant, int level)
        {
            var watch = Stopwatch.StartNew();
            CheckersGame game = Positions.PlayOut(variant, level, level, 100 + level, 1000);
            watch.Stop();
            TestContext.WriteLine("{0} level {1}: {2} plies, {3} in {4} ms", variant, level, game.ply, game.Result, watch.ElapsedMilliseconds);
            Assert.IsTrue(game.IsOver, "the game did not end in 1000 plies");
            Assert.AreNotEqual(EndReason.None, game.Reason);
        }

        [Test]
        public void HardAnswersAMiddleGameOnTheBigBoardQuickly()
        {
            var game = new CheckersGame(Variant.International);
            var random = new SystemRandom(17);
            while (game.ply < 24 && !game.IsOver)
            {
                game.Play(CheckersAI.Choose(game, 1, random));
            }
            Assert.IsFalse(game.IsOver);
            long slowest = 0;
            for (int i = 0; i < 3 && !game.IsOver; i++)
            {
                var watch = Stopwatch.StartNew();
                Move move = CheckersAI.Choose(game, 2, null);
                watch.Stop();
                TestContext.WriteLine("Hard, 10x10, ply {0}: {1} in {2} ms", game.ply, move, watch.ElapsedMilliseconds);
                slowest = System.Math.Max(slowest, watch.ElapsedMilliseconds);
                game.Play(move);
            }
            Assert.Less(slowest, 4000);
        }

        [Test]
        public void HardBeatsEasyMoreOftenThanNot()
        {
            int hard = 0;
            int easy = 0;
            for (int i = 0; i < 4; i++)
            {
                bool hardIsLight = i % 2 == 0;
                CheckersGame game = Positions.PlayOut(Variant.English, hardIsLight ? 2 : 0, hardIsLight ? 0 : 2, 200 + i, 1000);
                Side? winner = game.Winner;
                if (winner.HasValue)
                {
                    if ((winner.Value == Side.Light) == hardIsLight)
                    {
                        hard++;
                    }
                    else
                    {
                        easy++;
                    }
                }
                TestContext.WriteLine("game {0}: hard plays {1}, {2} after {3} plies", i, hardIsLight ? "Light" : "Dark", game.Result, game.ply);
            }
            Assert.Greater(hard, easy);
        }
    }
}
