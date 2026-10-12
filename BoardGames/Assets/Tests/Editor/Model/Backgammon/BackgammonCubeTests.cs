using NUnit.Framework;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The doubling cube (who may offer it and when, taking, dropping, the 64 limit, a game without it) and what a
    /// finished or resigned game is worth: a single game, a gammon, a backgammon, times the cube.
    /// </summary>
    public class BackgammonCubeTests
    {
        private const Side White = Side.Light;
        private const Side Black = Side.Dark;

        private static int[] Path(Side side, params int[] ownPoints)
        {
            return BackgammonPositions.Path(side, ownPoints);
        }

        [Test]
        public void DoubleTakeAndTheCubeChangesHands()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(game.CanDouble(White));
            Assert.IsFalse(game.CanDouble(Black), "not Black's turn");
            Assert.IsFalse(game.Double(Black));
            int turn = game.Turn;
            Assert.IsTrue(game.Double(White));
            Assert.AreEqual(turn + 1, game.Turn);
            Assert.AreEqual(BackgammonPhase.Answering, game.Phase);
            Assert.AreEqual(Black, game.SideToAct);
            Assert.IsFalse(game.CanDouble(White));
            Assert.IsFalse(game.Roll(3, 1), "the cube must be answered first");
            Assert.IsFalse(game.Take(White));
            Assert.IsFalse(game.Drop(White));
            Assert.IsTrue(game.Take(Black));
            Assert.AreEqual(2, game.cubeValue);
            Assert.AreEqual((int)Black, game.cubeOwner);
            Assert.AreEqual(White, game.SideToAct, "the doubler rolls");
            Assert.AreEqual(BackgammonPhase.Rolling, game.Phase);
            Assert.IsFalse(game.CanDouble(White), "Black owns the cube");
            Assert.IsFalse(game.Double(White));
            Assert.IsTrue(game.Roll(3, 1));
            Assert.IsFalse(game.Double(White), "not after rolling");
            Assert.IsFalse(game.Take(Black));
            Assert.IsTrue(game.Play(Path(White, 8, 5, 6, 5)));
            Assert.IsTrue(game.CanDouble(Black));
            Assert.IsTrue(game.Double(Black));
            Assert.IsTrue(game.Take(White));
            Assert.AreEqual(4, game.cubeValue);
            Assert.AreEqual((int)White, game.cubeOwner);
            Assert.AreEqual(Black, game.SideToAct);
            Assert.IsFalse(game.CanDouble(Black));
            Assert.IsFalse(game.IsOver);
        }

        [Test]
        public void ADropGivesTheStakeAsItWas()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(game.Double(White));
            Assert.IsTrue(game.Take(Black));
            Assert.IsTrue(game.Roll(4, 2));
            Assert.IsTrue(game.Play(Path(White, 8, 4, 6, 4)));
            Assert.IsTrue(game.Double(Black));
            Assert.IsTrue(game.Drop(White));
            Assert.IsTrue(game.IsOver);
            Assert.AreEqual(GameResult.DarkWins, game.Result);
            Assert.AreEqual(EndReason.DoubleDeclined, game.Reason);
            Assert.AreEqual(Black, game.Winner);
            Assert.AreEqual(2, game.Points, "the stake before the double");
            Assert.AreEqual(1, game.ResultMultiplier);
            Assert.AreEqual(BackgammonPhase.Over, game.Phase);
            Assert.IsFalse(game.Roll(1, 2));
            Assert.IsFalse(game.Double(Black));
            Assert.IsFalse(game.Resign(White));
            Assert.IsFalse(game.Play(Path(White, 13, 12, 13, 11)));
        }

        [Test]
        public void TheCubeStopsAt64AndOnlyWhenInPlay()
        {
            BackgammonGame game = BackgammonGame.NewGame(true, false, White);
            game.cubeValue = 32;
            game.cubeOwner = (int)White;
            Assert.IsTrue(game.CanDouble(White));
            Assert.IsTrue(game.Double(White));
            Assert.IsTrue(game.Take(Black));
            Assert.AreEqual(BackgammonGame.MaxCube, game.cubeValue);
            game.cubeOwner = (int)White;
            Assert.IsFalse(game.CanDouble(White), "64 is the most");
            Assert.IsFalse(game.Double(White));

            BackgammonGame without = BackgammonGame.NewGame(false, false, White);
            Assert.IsFalse(without.CanDouble(White));
            Assert.IsFalse(without.Double(White));
            Assert.IsFalse(without.Take(Black));

            BackgammonGame opening = BackgammonGame.NewGame(true, true, White);
            Assert.IsFalse(opening.CanDouble(White), "not before the opening roll");
            Assert.IsFalse(opening.Double(White));
        }

        [Test]
        public void FinishedGamesScoreTimesTheCube()
        {
            // White's last checker on its 1-point, dice 2-1; the cube on 2.
            BackgammonGame single = Finish("b 6 10");
            Assert.AreEqual(EndReason.BorneOff, single.Reason);
            Assert.AreEqual(GameResult.LightWins, single.Result);
            Assert.AreEqual(2, single.Points, "a single game");
            Assert.AreEqual(1, single.ResultMultiplier);
            Assert.AreEqual(4, Finish("b 6 15").Points, "a gammon: Black bore off none");
            Assert.AreEqual(2, Finish("b 6 15").ResultMultiplier);
            Assert.AreEqual(6, Finish("b 6 14", "b 20 1").Points, "a backgammon: a black checker in White's home board");
            Assert.AreEqual(3, Finish("b 6 14", "b 20 1").ResultMultiplier);
            Assert.AreEqual(6, Finish("b 6 14", "b 25 1").Points, "a backgammon: a black checker on the bar");
            Assert.AreEqual(4, Finish("b 6 14", "b 18 1").Points, "White's 7-point is outside White's home board");
        }

        private static BackgammonGame Finish(params string[] black)
        {
            var checkers = new string[black.Length + 1];
            checkers[0] = "w 1 1";
            black.CopyTo(checkers, 1);
            BackgammonGame game = BackgammonPositions.Game(White, 2, 1, checkers);
            game.cubeValue = 2;
            game.cubeOwner = (int)Black;
            BackgammonPlay play = game.FindPlay(Path(White, 1, 0));
            Assert.IsNotNull(play);
            Assert.AreEqual(2, play.moves[0].die, "the larger die when only one can be played");
            Assert.IsTrue(game.Play(Path(White, 1, 0)));
            Assert.IsTrue(game.IsOver);
            return game;
        }

        [Test]
        public void ResigningConcedesWhatThePositionIsWorth()
        {
            BackgammonGame start = BackgammonGame.NewGame(true, false, White);
            Assert.AreEqual(1, start.ResignValue(Black), "a gammon is only a threat while the winner is not bearing off");
            Assert.AreEqual(1, start.ResignValue(White));

            BackgammonGame trapped = BackgammonPositions.Game(White, 0, 0, "w 6 15", "b 24 1", "b 13 14");
            Assert.AreEqual(3, trapped.ResignValue(Black), "White bears off and a Black checker stands in White's home board");

            BackgammonGame gammon = BackgammonPositions.Game(White, 0, 0, "w 6 15", "b 13 15");
            gammon.cubeValue = 4;
            Assert.AreEqual(8, gammon.ResignValue(Black));
            Assert.IsTrue(gammon.Resign(Black));
            Assert.AreEqual(GameResult.LightWins, gammon.Result);
            Assert.AreEqual(EndReason.Resigned, gammon.Reason);
            Assert.AreEqual(8, gammon.Points);
            Assert.AreEqual(BackgammonPhase.Over, gammon.Phase);
            Assert.IsFalse(gammon.Resign(White), "once over");

            BackgammonGame single = BackgammonPositions.Game(Black, 3, 2, "w 6 15", "b 6 14");
            Assert.AreEqual(1, single.ResignValue(Black));
            Assert.IsTrue(single.Resign(Black));
            Assert.AreEqual(1, single.Points);

            BackgammonGame offered = BackgammonGame.NewGame(true, false, White);
            Assert.IsTrue(offered.Double(White));
            Assert.IsTrue(offered.Resign(White), "the doubler may still give up");
            Assert.AreEqual(GameResult.DarkWins, offered.Result);
            Assert.AreEqual(1, offered.Points, "a single game at the cube as it was");
        }

        [Test]
        public void CallingOffEndsTheGame()
        {
            BackgammonGame won = BackgammonGame.NewGame(true, false, White);
            won.cubeValue = 2;
            won.CallOff(Black);
            Assert.AreEqual(GameResult.DarkWins, won.Result);
            Assert.AreEqual(EndReason.CalledOff, won.Reason);
            Assert.AreEqual(2, won.Points);
            Assert.IsTrue(won.IsOver);

            BackgammonGame drawn = BackgammonGame.NewGame(true, false, White);
            drawn.CallOff(null);
            Assert.AreEqual(GameResult.Draw, drawn.Result);
            Assert.IsNull(drawn.Winner);
            Assert.AreEqual(0, drawn.Points);
            Assert.AreEqual(BackgammonPhase.Over, drawn.Phase);
        }
    }
}
