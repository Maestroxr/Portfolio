using System.Linq;
using Gamebox;
using Gamebox.Lockstep;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.BoardGames.Tests
{
    /// <summary>
    /// The campaign: twelve valid levels in the order of the design (six of draughts, three of chess, three of
    /// backgammon), the first level of each game open and every other one unlocked by finishing the one before it in the
    /// same game, and the stars rule of each game (one for the win, one for the second target, one for winning quickly).
    /// </summary>
    public class CampaignTest
    {
        /// <summary>Title, rule set, computer level, the player's side, the second target and the length to win within, per level.</summary>
        private static readonly object[][] Design =
        {
            new object[] { "First Moves", RuleSet.EnglishDraughts, 0, Side.Dark, 8, 60 },
            new object[] { "Crowning Glory", RuleSet.EnglishDraughts, 1, Side.Light, 7, 80 },
            new object[] { "Flying Kings", RuleSet.RussianDraughts, 1, Side.Light, 7, 80 },
            new object[] { "The Wide Board", RuleSet.InternationalDraughts, 1, Side.Light, 12, 120 },
            new object[] { "Grandmaster's Table", RuleSet.EnglishDraughts, 2, Side.Dark, 6, 100 },
            new object[] { "World Championship", RuleSet.InternationalDraughts, 2, Side.Light, 10, 140 },
            new object[] { "King's Pawn", RuleSet.Chess, 0, Side.Light, 8, 120 },
            new object[] { "The Black Pieces", RuleSet.Chess, 1, Side.Dark, 6, 140 },
            new object[] { "Checkmate Master", RuleSet.Chess, 2, Side.Light, 4, 160 },
            new object[] { "Bear Them Off", RuleSet.Backgammon, 0, Side.Light, 2, 110 },
            new object[] { "Hit and Run", RuleSet.Backgammon, 1, Side.Dark, 2, 100 },
            new object[] { "Double or Nothing", RuleSet.Backgammon, 2, Side.Light, 2, 100 },
        };

        private static BoardGamesCampaign Campaign()
        {
            BoardGamesCampaign campaign = AssetDatabase.FindAssets("t:BoardGamesCampaign")
                .Select(guid => AssetDatabase.LoadAssetAtPath<BoardGamesCampaign>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(found => found != null);
            Assert.IsNotNull(campaign, "the campaign asset is missing; run Board Games > Build Everything");
            return campaign;
        }

        private static BoardGamesLevel Level(RuleSet ruleSet, int computerLevel, Side playerSide, int piecesKept, int maxPlies)
        {
            var level = ScriptableObject.CreateInstance<BoardGamesLevel>();
            level.Configure(ruleSet, computerLevel, (int)playerSide, piecesKept, maxPlies, "A test level.");
            return level;
        }

        /// <summary>A game that <paramref name="winner"/> has won after <paramref name="plies"/> plies, the loser resigning.</summary>
        private static CheckersGame Won(Variant variant, Side winner, int plies)
        {
            var game = new CheckersGame(variant);
            var random = new SystemRandom(5);
            for (int i = 0; i < plies && !game.IsOver; i++)
            {
                game.Play(CheckersAI.Choose(game, 0, random));
            }
            Assert.IsFalse(game.IsOver, "the test game ended by itself");
            Assert.IsTrue(game.Resign(Sides.Other(winner)));
            Assert.AreEqual(winner, game.Winner);
            return game;
        }

        /// <summary>
        /// A finished chess game written field by field: <paramref name="winner"/> won by checkmate after
        /// <paramref name="plies"/> plies with its king and <paramref name="pieces"/> other pieces left (pawns), the loser
        /// with a bare king.
        /// </summary>
        private static ChessGame ChessWon(Side winner, int pieces, int plies)
        {
            var game = new ChessGame();
            Side loser = Sides.Other(winner);
            game.cells[ChessGame.Square(4, winner == Side.Light ? 0 : 7)] = ChessPieces.Make(winner, ChessPieceKind.King);
            game.cells[ChessGame.Square(4, loser == Side.Light ? 0 : 7)] = ChessPieces.Make(loser, ChessPieceKind.King);
            for (int i = 0; i < pieces; i++)
            {
                game.cells[ChessGame.Square(i % 8, winner == Side.Light ? 1 + i / 8 : 6 - i / 8)] = ChessPieces.Make(winner, ChessPieceKind.Pawn);
            }
            game.ply = plies;
            game.result = (int)(winner == Side.Light ? GameResult.LightWins : GameResult.DarkWins);
            game.endReason = (int)EndReason.Checkmate;
            return game;
        }

        /// <summary>A finished backgammon game written field by field: <paramref name="winner"/> won <paramref name="kind"/> times the cube after <paramref name="turns"/> decisions.</summary>
        private static BackgammonGame BackgammonWon(Side winner, int kind, int cube, int turns)
        {
            var game = new BackgammonGame
            {
                cube = true,
                cubeValue = cube,
                turn = turns,
                score = kind * cube,
                result = (int)(winner == Side.Light ? GameResult.LightWins : GameResult.DarkWins),
                endReason = (int)EndReason.BorneOff,
                phase = (int)BackgammonPhase.Over
            };
            game.off[(int)winner] = BackgammonGame.CheckersPerSide;
            return game;
        }

        [Test]
        public void TwelveValidLevelsInTheOrderOfTheDesign()
        {
            BoardGamesCampaign campaign = Campaign();
            Assert.AreEqual(Design.Length, campaign.Count);
            Assert.AreEqual(3, campaign.MaxStarsPerLevel);
            Assert.AreEqual(Design.Length * 3, campaign.MaxStars);
            for (int i = 0; i < Design.Length; i++)
            {
                BoardGamesLevel level = campaign.Level(i);
                Assert.IsNotNull(level, $"level {i} is missing");
                Assert.AreSame(level, campaign[i], $"level {i}");
                Assert.IsTrue(level.IsLevelValid(out string message), $"level {i}: {message}");
                Assert.AreEqual(i, level.Index, $"level {i}");
                Assert.AreEqual(Design[i][0], level.Title, $"level {i}");
                Assert.AreEqual((RuleSet)Design[i][1], level.RuleSet, $"level {i}");
                Assert.AreEqual(RuleSets.ModeOf((RuleSet)Design[i][1]), level.Mode, $"level {i}");
                Assert.AreEqual(level.Mode, campaign.ModeOf(i), $"level {i}");
                Assert.AreEqual(Design[i][2], level.ComputerLevel, $"level {i}");
                Assert.AreEqual((int)(Side)Design[i][3], level.PlayerSide, $"level {i}");
                Assert.AreEqual((Side)Design[i][3], level.Side, $"level {i}");
                Assert.AreEqual(Design[i][4], level.PiecesKept, $"level {i}");
                Assert.AreEqual(Design[i][5], level.MaxPlies, $"level {i}");
                Assert.IsFalse(string.IsNullOrEmpty(level.Description), $"level {i} has no description");
                if (level.Mode == GameMode.Checkers)
                {
                    Assert.AreEqual(RuleSets.CheckersVariant((RuleSet)Design[i][1]), level.Rules.Variant, $"level {i}");
                }
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, campaign.Levels(GameMode.Checkers).ToArray());
            CollectionAssert.AreEqual(new[] { 6, 7, 8 }, campaign.Levels(GameMode.Chess).ToArray());
            CollectionAssert.AreEqual(new[] { 9, 10, 11 }, campaign.Levels(GameMode.Backgammon).ToArray());
        }

        [Test]
        public void ALevelOutOfRangeIsNotValid()
        {
            AssertInvalid(Level(RuleSet.EnglishDraughts, 7, Side.Light, 8, 60), "there is no computer level 7");
            AssertInvalid(Level(RuleSet.InternationalDraughts, 1, Side.Light, 40, 120), "nobody keeps more pieces than they start with");
            AssertInvalid(Level(RuleSet.Chess, 1, Side.Light, 16, 120), "a chess side has fifteen pieces besides the king");
            AssertInvalid(Level(RuleSet.Backgammon, 1, Side.Light, 4, 100), "a win is worth at most a backgammon (3)");
            AssertInvalid(Level(RuleSet.Chess, 1, Side.Light, 8, 1), "a game takes at least two plies");
            AssertValid(Level(RuleSet.Chess, 2, Side.Dark, 15, 120));
            AssertValid(Level(RuleSet.Backgammon, 0, Side.Light, 3, 100));
        }

        /// <summary>Gives a test level a title, as the content builder does (a level without one is never valid).</summary>
        private static void Title(BoardGamesLevel level)
        {
            var serialized = new SerializedObject(level);
            serialized.FindProperty("<Title>k__BackingField").stringValue = "A test";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertInvalid(BoardGamesLevel level, string why)
        {
            Title(level);
            try
            {
                Assert.IsFalse(level.IsLevelValid(out string message), why);
                Assert.IsFalse(string.IsNullOrEmpty(message));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        private static void AssertValid(BoardGamesLevel level)
        {
            Title(level);
            try
            {
                Assert.IsTrue(level.IsLevelValid(out string message), message);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void TheFirstLevelOfEachGameIsOpenAndEachLevelUnlocksTheNextOfItsGame()
        {
            BoardGamesCampaign campaign = Campaign();
            var progress = new BoardGamesProgress(null, GameType.BoardGames);
            foreach (GameMode mode in RuleSets.Modes)
            {
                var levels = campaign.Levels(mode);
                Assert.Greater(levels.Count, 0, $"{mode} has levels");
                Assert.IsTrue(campaign.IsUnlocked(levels[0], progress), $"the first level of {mode} is open");
                for (int i = 1; i < levels.Count; i++)
                {
                    Assert.IsFalse(campaign.IsUnlocked(levels[i], progress), $"{mode} level {i} is open before level {i - 1} was won");
                }
            }
            int completed = 0;
            foreach (GameMode mode in RuleSets.Modes)
            {
                var levels = campaign.Levels(mode);
                for (int i = 0; i < levels.Count - 1; i++)
                {
                    progress.RecordLevel(levels[i], 1, 100);
                    completed++;
                    Assert.IsTrue(campaign.IsUnlocked(levels[i + 1], progress), $"finishing {mode} level {i} opens level {i + 1}");
                    if (i + 2 < levels.Count)
                    {
                        Assert.IsFalse(campaign.IsUnlocked(levels[i + 2], progress), $"finishing {mode} level {i} does not open level {i + 2}");
                    }
                }
            }
            Assert.AreEqual(completed, campaign.TotalStars(progress));
            Assert.IsFalse(campaign.IsUnlocked(-1, progress));
            Assert.IsFalse(campaign.IsUnlocked(campaign.Count, progress));
            Assert.IsTrue(campaign.IsUnlocked(campaign.Count - 1, null), "without progress everything is open");
        }

        [Test]
        public void ChessLevelsOpenWithoutAnyDraughtsLevelWon()
        {
            BoardGamesCampaign campaign = Campaign();
            var progress = new BoardGamesProgress(null, GameType.BoardGames);
            progress.RecordLevel(campaign.Levels(GameMode.Chess)[0], 2, 100);
            Assert.IsTrue(campaign.IsUnlocked(campaign.Levels(GameMode.Chess)[1], progress));
            Assert.IsFalse(campaign.IsUnlocked(campaign.Levels(GameMode.Checkers)[1], progress), "a chess win opens no draughts level");
            Assert.IsFalse(campaign.IsUnlocked(campaign.Levels(GameMode.Backgammon)[1], progress), "a chess win opens no backgammon level");
        }

        [Test]
        public void StarsAreForTheWinThePiecesKeptAndTheSpeed()
        {
            BoardGamesLevel level = Level(RuleSet.EnglishDraughts, 0, Side.Dark, 8, 60);
            try
            {
                CheckersGame quick = Won(Variant.English, Side.Dark, 10);
                Assert.AreEqual(3, BoardGamesCampaign.StarsFor(level, quick, Side.Dark), "a quick win with every piece");
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(level, quick, Side.Light), "the loser earns nothing");

                // The same win, but as if it had taken longer than the level allows: the speed star is lost.
                CheckersGame slow = quick.Clone();
                slow.ply = level.MaxPlies + 1;
                Assert.AreEqual(2, BoardGamesCampaign.StarsFor(level, slow, Side.Dark), "a slow win loses the speed star");

                // And with too few pieces left: the win alone is one star.
                CheckersGame thinned = slow.Clone();
                for (int square = 0; square < thinned.board.cells.Length && thinned.board.Count(Side.Dark) >= level.PiecesKept; square++)
                {
                    if (Pieces.Belongs(thinned.board[square], Side.Dark))
                    {
                        thinned.board[square] = Pieces.None;
                    }
                }
                Assert.Less(thinned.board.Count(Side.Dark), level.PiecesKept);
                Assert.AreEqual(1, BoardGamesCampaign.StarsFor(level, thinned, Side.Dark), "a win with too few pieces kept is one star");

                var playing = new CheckersGame(Variant.English);
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(level, playing, Side.Dark), "no stars before the end");
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(null, quick, Side.Dark));
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(level, null, Side.Dark));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void ChessStarsCountThePiecesBesidesTheKingAndThePlies()
        {
            BoardGamesLevel level = Level(RuleSet.Chess, 1, Side.Dark, 6, 140);
            try
            {
                Assert.AreEqual(3, BoardGamesCampaign.StarsFor(level, ChessWon(Side.Dark, 6, 140), Side.Dark), "six pieces besides the king, in time");
                Assert.AreEqual(2, BoardGamesCampaign.StarsFor(level, ChessWon(Side.Dark, 5, 140), Side.Dark), "the king does not count as a piece kept");
                Assert.AreEqual(2, BoardGamesCampaign.StarsFor(level, ChessWon(Side.Dark, 9, 141), Side.Dark), "too slow");
                Assert.AreEqual(1, BoardGamesCampaign.StarsFor(level, ChessWon(Side.Dark, 1, 200), Side.Dark), "a win alone");
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(level, ChessWon(Side.Light, 15, 20), Side.Dark), "a loss earns nothing");
                ChessGame drawn = ChessWon(Side.Dark, 8, 30);
                drawn.result = (int)GameResult.Draw;
                drawn.endReason = (int)EndReason.Stalemate;
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(level, drawn, Side.Dark), "a draw earns nothing");
                ChessGame scored = ChessWon(Side.Light, 7, 50);
                Assert.AreEqual(7 * 10 + 1000 - 50, BoardGamesCampaign.ScoreFor(scored, Side.Light));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void BackgammonStarsAreForAGammonAndTheDecisions()
        {
            BoardGamesLevel level = Level(RuleSet.Backgammon, 1, Side.Dark, 2, 100);
            try
            {
                Assert.AreEqual(3, BoardGamesCampaign.StarsFor(level, BackgammonWon(Side.Dark, 2, 1, 100), Side.Dark), "a gammon in time");
                Assert.AreEqual(3, BoardGamesCampaign.StarsFor(level, BackgammonWon(Side.Dark, 3, 2, 80), Side.Dark), "a backgammon is more than a gammon");
                Assert.AreEqual(2, BoardGamesCampaign.StarsFor(level, BackgammonWon(Side.Dark, 1, 1, 90), Side.Dark), "a single game misses the gammon star");
                Assert.AreEqual(2, BoardGamesCampaign.StarsFor(level, BackgammonWon(Side.Dark, 1, 4, 90), Side.Dark),
                    "the cube makes a single game worth more points, not a gammon");
                Assert.AreEqual(2, BoardGamesCampaign.StarsFor(level, BackgammonWon(Side.Dark, 2, 1, 101), Side.Dark), "too many decisions");
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(level, BackgammonWon(Side.Light, 3, 1, 50), Side.Dark), "a loss earns nothing");
                Assert.AreEqual(0, BoardGamesCampaign.StarsFor(level, new BackgammonGame(), Side.Dark), "no stars before the end");
                BackgammonGame scored = BackgammonWon(Side.Light, 2, 2, 60);
                Assert.AreEqual(4 * 100 + 1000 - 60, BoardGamesCampaign.ScoreFor(scored, Side.Light));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void TheStarRulesLineIsTheGames()
        {
            Assert.AreEqual(BoardGamesCampaign.StarRules, BoardGamesCampaign.StarRulesOf(GameMode.Checkers));
            Assert.AreEqual(BoardGamesCampaign.StarRules, BoardGamesCampaign.StarRulesOf(GameMode.Chess));
            Assert.AreEqual(BoardGamesCampaign.BackgammonStarRules, BoardGamesCampaign.StarRulesOf(GameMode.Backgammon));
        }

        [Test]
        public void TheScoreRewardsPiecesLeftAndPliesSaved()
        {
            CheckersGame quick = Won(Variant.English, Side.Dark, 10);
            Assert.AreEqual(quick.board.Count(Side.Dark) * 10 + 1000 - quick.ply, BoardGamesCampaign.ScoreFor(quick, Side.Dark));
            Assert.GreaterOrEqual(BoardGamesCampaign.ScoreFor(null, Side.Light), 1);
        }

        [Test]
        public void ProgressCountsGamesWinsAndDraws()
        {
            var progress = new BoardGamesProgress(null, GameType.BoardGames);
            Assert.AreEqual(0, progress.Games);
            progress.RecordGame(true, false);
            progress.RecordGame(false, true);
            progress.RecordGame(false, false);
            Assert.AreEqual(3, progress.Games);
            Assert.AreEqual(1, progress.Wins);
            Assert.AreEqual(1, progress.Draws);
        }
    }
}
