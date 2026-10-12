using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The chess computer: it takes a hanging queen, sees mates one and two moves away, answers the same way for the
    /// same game and random numbers, keeps to its budget of positions, answers a middle game quickly at Hard,
    /// finishes games against itself at every level and plays better the higher its level. The long games run side by
    /// side on tasks (each game on its own, so the outcome is the same as one after the other).
    /// </summary>
    public class ChessAITests
    {
        private const string MiddleGame = "r1bq1rk1/pp3ppp/2n1pn2/2bp4/2P5/2N1PN2/PPQ2PPP/R1B1KB1R w KQ - 0 8";

        /// <summary>What one game between two levels came to, worked out on a task and checked on the test's thread.</summary>
        private sealed class Record
        {
            public ChessGame game;
            public string fault;
            public long milliseconds;
            public long slowest;
        }

        /// <summary>
        /// Plays <paramref name="game"/> on, White at <paramref name="whiteLevel"/> and Black at
        /// <paramref name="blackLevel"/>, until it ends or reaches <paramref name="maxPly"/> plies; notes the first move
        /// that was missing, over budget or refused.
        /// </summary>
        private static Record PlayOut(ChessGame game, int whiteLevel, int blackLevel, uint seed, int maxPly)
        {
            var record = new Record { game = game };
            var random = new SeededRandom(seed);
            var watch = Stopwatch.StartNew();
            while (!game.IsOver && game.ply < maxPly)
            {
                int level = game.SideToMove == Side.Light ? whiteLevel : blackLevel;
                long start = watch.ElapsedMilliseconds;
                ChessThought thought = ChessAI.Think(game, level, random);
                long spent = watch.ElapsedMilliseconds - start;
                if (spent > record.slowest)
                {
                    record.slowest = spent;
                }
                if (thought == null || thought.move == null)
                {
                    record.fault = "no move at ply " + game.ply + " in " + game.ToFen();
                    break;
                }
                if (thought.nodes > ChessAI.NodeBudget(level))
                {
                    record.fault = thought.nodes + " positions at level " + level + " in " + game.ToFen();
                    break;
                }
                if (!game.Play(thought.move))
                {
                    record.fault = "refused " + thought.move.Uci + " in " + game.ToFen();
                    break;
                }
            }
            record.milliseconds = watch.ElapsedMilliseconds;
            return record;
        }

        /// <summary>Whether the side to move has a move that mates at once.</summary>
        private static bool HasMateInOne(ChessGame game)
        {
            foreach (ChessMove move in game.Legal())
            {
                ChessGame copy = game.Clone();
                if (copy.Play(move.Clone()) && copy.Reason == EndReason.Checkmate)
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void BudgetsPerLevel()
        {
            Assert.AreEqual(3, ChessAI.Levels);
            Assert.AreEqual(2500, ChessAI.NodeBudget(0));
            Assert.AreEqual(40000, ChessAI.NodeBudget(1));
            Assert.AreEqual(250000, ChessAI.NodeBudget(2));
            Assert.AreEqual(2500, ChessAI.NodeBudget(-1), "levels are clamped");
            Assert.AreEqual(250000, ChessAI.NodeBudget(7));
            Assert.AreEqual(2, ChessAI.MaxDepth(0));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void TakesAHangingQueen(int level)
        {
            ChessGame game = ChessGame.FromFen("4k3/8/8/3q4/8/2N5/8/4K3 w - - 0 1");
            ChessMove move = ChessAI.ChooseMove(game, level, new SeededRandom(1));
            Assert.IsNotNull(move);
            Assert.AreEqual(ChessGameTests.Sq("c3"), move.from);
            Assert.AreEqual(ChessGameTests.Sq("d5"), move.to);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void FindsMateInOne(int level)
        {
            ChessGame game = ChessGame.FromFen("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1");
            ChessThought thought = ChessAI.Think(game, level, new SeededRandom(2));
            Assert.AreEqual("a1a8", thought.move.Uci);
            Assert.IsTrue(game.Play(thought.move));
            Assert.AreEqual(EndReason.Checkmate, game.Reason);
        }

        [TestCase(1, "7k/8/8/8/8/8/R7/1R4K1 w - - 0 1")]
        [TestCase(2, "7k/8/8/8/8/8/R7/1R4K1 w - - 0 1")]
        [TestCase(1, "kbK5/pp6/1P6/8/8/8/8/R7 w - - 0 1")]
        [TestCase(2, "kbK5/pp6/1P6/8/8/8/8/R7 w - - 0 1")]
        public void FindsMateInTwo(int level, string fen)
        {
            ChessGame game = ChessGame.FromFen(fen);
            Assert.IsFalse(HasMateInOne(game), "no shorter mate");
            ChessThought thought = ChessAI.Think(game, level, new SeededRandom(3));
            Assert.AreEqual(ChessSearch.Mate - 3, thought.score, "mate in three plies");
            Assert.IsTrue(game.Play(thought.move));
            Assert.IsFalse(game.IsOver);
            foreach (ChessMove reply in game.Legal())
            {
                ChessGame after = game.Clone();
                Assert.IsTrue(after.Play(reply.Clone()));
                Assert.IsTrue(HasMateInOne(after), thought.move.san + " " + reply.Uci + " leaves no mate");
            }
        }

        [Test]
        public void NoMoveWhenThereIsNone()
        {
            Assert.IsNull(ChessAI.ChooseMove(null, 1, null));
            ChessGame over = ChessGame.NewGame();
            over.Resign(Side.Dark);
            Assert.IsNull(ChessAI.ChooseMove(over, 2, null));
            Assert.IsNull(ChessAI.Choose(over, 0, 1, null));
            Assert.IsNull(ChessAI.Think(over, 0, null));
            ChessGame mated = ChessGameTests.Play(ChessGame.NewGame(), "f3", "e5", "g4", "Qh4#");
            Assert.IsNull(ChessAI.ChooseMove(mated, 0, new SeededRandom(1)));
        }

        [Test]
        public void TheOnlyMoveIsPlayedWithoutASearch()
        {
            ChessGame game = ChessGame.FromFen("k7/8/8/8/8/8/1q6/K7 w - - 0 1");
            Assert.AreEqual(1, game.Legal().Count);
            ChessThought thought = ChessAI.Think(game, 2, new SeededRandom(1));
            Assert.AreEqual("a1b2", thought.move.Uci);
            Assert.AreEqual(0, thought.nodes);
        }

        [Test]
        public void TheSameGameGetsTheSameMove()
        {
            ChessGame game = ChessGame.FromFen(MiddleGame);
            uint before = game.Checksum();
            ChessThought one = ChessAI.Think(game, 1, new SeededRandom(5));
            ChessThought two = ChessAI.Think(game.Clone(), 1, new SeededRandom(99));
            Assert.AreEqual(one.move.Uci, two.move.Uci, "Normal draws on no random numbers");
            Assert.AreEqual(one.score, two.score);
            Assert.AreEqual(one.nodes, two.nodes);
            Assert.AreEqual(ChessAI.NodeBudget(1), one.nodes, "a middle game uses the whole budget");
            Assert.Greater(one.depth, 3);
            Assert.AreEqual(before, game.Checksum(), "thinking changes nothing");
            for (uint seed = 1; seed <= 5; seed++)
            {
                ChessMove a = ChessAI.ChooseMove(game, 0, new SeededRandom(seed));
                ChessMove b = ChessAI.ChooseMove(game.Clone(), 0, new SeededRandom(seed));
                Assert.AreEqual(a.Uci, b.Uci);
            }
        }

        [Test]
        public void EasyPicksAmongTheGoodMovesAtRandom()
        {
            ChessGame game = ChessGame.NewGame();
            var picked = new System.Collections.Generic.HashSet<string>();
            for (uint seed = 1; seed <= 12; seed++)
            {
                ChessThought thought = ChessAI.Think(game, 0, new SeededRandom(seed));
                Assert.LessOrEqual(thought.nodes, ChessAI.NodeBudget(0));
                picked.Add(thought.move.Uci);
            }
            Assert.Greater(picked.Count, 1);
            Assert.AreEqual(ChessAI.ChooseMove(game, 0, null).Uci, ChessAI.ChooseMove(game, 0, null).Uci, "no random numbers: the best");
        }

        [Test]
        public void HardAnswersAMiddleGameInTime()
        {
            ChessGame game = ChessGame.FromFen(MiddleGame);
            // The first search pays for compiling the code.
            ChessAI.Think(game, 1, null);
            var watch = Stopwatch.StartNew();
            ChessThought thought = ChessAI.Think(game, 2, null);
            watch.Stop();
            TestContext.WriteLine($"Hard: {game.SanOf(thought.move)}, score {thought.score}, depth {thought.depth}, {thought.nodes} positions in {watch.ElapsedMilliseconds} ms");
            Assert.IsNotNull(thought.move);
            Assert.LessOrEqual(thought.nodes, ChessAI.NodeBudget(2));
            Assert.GreaterOrEqual(thought.depth, 5);
            Assert.Less(watch.ElapsedMilliseconds, 2000);
        }

        [Test]
        public void EveryLevelFinishesAGameAgainstItself()
        {
            // Easy and Normal play whole games from the start; Hard (half a second a move) plays one out from a won
            // position so the run stays short: the whole game from the start is an explicit test below.
            Task<Record> easy = Task.Run(() => PlayOut(ChessGame.NewGame(), 0, 0, 11u, 300));
            Task<Record> normal = Task.Run(() => PlayOut(ChessGame.NewGame(), 1, 1, 12u, 300));
            Task<Record> hard = Task.Run(() => PlayOut(ChessGame.FromFen("4k3/2pp4/8/8/8/8/PPPPPPPP/RNBQKBNR w KQ - 0 1"), 2, 2, 13u, 300));
            Task.WaitAll(easy, normal, hard);
            foreach (Task<Record> task in new[] { easy, normal, hard })
            {
                Record record = task.Result;
                TestContext.WriteLine($"{record.game.ply} plies, {record.game.Result} by {record.game.Reason} in {record.milliseconds} ms (slowest move {record.slowest} ms): {record.game.ToFen()}");
                Assert.IsNull(record.fault, record.fault);
                Assert.IsTrue(record.game.IsOver || record.game.ply >= 300);
            }
            Assert.AreEqual(GameResult.LightWins, hard.Result.game.Result, "Hard mates with the whole army");
        }

        [Test]
        public void HardBeatsEasy()
        {
            var games = new Task<Record>[4];
            for (int i = 0; i < games.Length; i++)
            {
                bool hardIsWhite = i % 2 == 0;
                uint seed = 21u + (uint)i;
                games[i] = Task.Run(() => PlayOut(ChessGame.NewGame(), hardIsWhite ? 2 : 0, hardIsWhite ? 0 : 2, seed, 300));
            }
            Task.WaitAll(games);
            int wins = 0;
            for (int i = 0; i < games.Length; i++)
            {
                Record record = games[i].Result;
                Side hard = i % 2 == 0 ? Side.Light : Side.Dark;
                TestContext.WriteLine($"Hard as {hard}: {record.game.ply} plies, {record.game.Result} by {record.game.Reason} in {record.milliseconds} ms");
                Assert.IsNull(record.fault, record.fault);
                if (record.game.Winner == hard)
                {
                    wins++;
                }
            }
            Assert.GreaterOrEqual(wins, 3);
        }

        /// <summary>Hard against itself from the first move, about a minute: run by name after a change to the search.</summary>
        [Explicit]
        [Test]
        public void HardFinishesAWholeGameAgainstItself()
        {
            Record record = PlayOut(ChessGame.NewGame(), 2, 2, 14u, 300);
            TestContext.WriteLine($"{record.game.ply} plies, {record.game.Result} by {record.game.Reason} in {record.milliseconds} ms (slowest move {record.slowest} ms)");
            Assert.IsNull(record.fault, record.fault);
            Assert.IsTrue(record.game.IsOver || record.game.ply >= 300);
        }
    }
}
