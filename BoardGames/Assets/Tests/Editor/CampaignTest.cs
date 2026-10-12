using System.Linq;
using Gamebox;
using Gamebox.Lockstep;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Checkers.Tests
{
    /// <summary>
    /// The campaign: six valid levels in the order of the design, each unlocked by finishing the one before, and the
    /// stars rule (one for the win, one for the pieces kept, one for winning quickly).
    /// </summary>
    public class CampaignTest
    {
        /// <summary>Title, variant, computer level, the player's side, pieces to keep and plies to win within, per level.</summary>
        private static readonly object[][] Design =
        {
            new object[] { "First Moves", Variant.English, 0, Side.Dark, 8, 60 },
            new object[] { "Crowning Glory", Variant.English, 1, Side.Light, 7, 80 },
            new object[] { "Flying Kings", Variant.Russian, 1, Side.Light, 7, 80 },
            new object[] { "The Wide Board", Variant.International, 1, Side.Light, 12, 120 },
            new object[] { "Grandmaster's Table", Variant.English, 2, Side.Dark, 6, 100 },
            new object[] { "World Championship", Variant.International, 2, Side.Light, 10, 140 },
        };

        private static CheckersCampaign Campaign()
        {
            CheckersCampaign campaign = AssetDatabase.FindAssets("t:CheckersCampaign")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CheckersCampaign>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(found => found != null);
            Assert.IsNotNull(campaign, "the campaign asset is missing; run Checkers > Build Everything");
            return campaign;
        }

        private static CheckersLevel Level(Variant variant, int computerLevel, Side playerSide, int piecesKept, int maxPlies)
        {
            var level = ScriptableObject.CreateInstance<CheckersLevel>();
            level.Configure((int)variant, computerLevel, (int)playerSide, piecesKept, maxPlies, "A test level.");
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

        [Test]
        public void SixValidLevelsInTheOrderOfTheDesign()
        {
            CheckersCampaign campaign = Campaign();
            Assert.AreEqual(Design.Length, campaign.Count);
            Assert.AreEqual(3, campaign.MaxStarsPerLevel);
            Assert.AreEqual(18, campaign.MaxStars);
            for (int i = 0; i < Design.Length; i++)
            {
                CheckersLevel level = campaign.Level(i);
                Assert.IsNotNull(level, $"level {i} is missing");
                Assert.AreSame(level, campaign[i], $"level {i}");
                Assert.IsTrue(level.IsLevelValid(out string message), $"level {i}: {message}");
                Assert.AreEqual(i, level.Index, $"level {i}");
                Assert.AreEqual(Design[i][0], level.Title, $"level {i}");
                Assert.AreEqual((int)(Variant)Design[i][1], level.Variant, $"level {i}");
                Assert.AreEqual(Design[i][2], level.ComputerLevel, $"level {i}");
                Assert.AreEqual((int)(Side)Design[i][3], level.PlayerSide, $"level {i}");
                Assert.AreEqual((Side)Design[i][3], level.Side, $"level {i}");
                Assert.AreEqual(Design[i][4], level.PiecesKept, $"level {i}");
                Assert.AreEqual(Design[i][5], level.MaxPlies, $"level {i}");
                Assert.IsFalse(string.IsNullOrEmpty(level.Description), $"level {i} has no description");
                Assert.AreEqual((Variant)Design[i][1], level.Rules.Variant, $"level {i}");
            }
        }

        [Test]
        public void ALevelOutOfRangeIsNotValid()
        {
            CheckersLevel level = Level(Variant.English, 7, Side.Light, 8, 60);
            try
            {
                Assert.IsFalse(level.IsLevelValid(out string message));
                Assert.IsFalse(string.IsNullOrEmpty(message));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
            level = Level(Variant.International, 1, Side.Light, 40, 120);
            try
            {
                Assert.IsFalse(level.IsLevelValid(out _), "nobody keeps more pieces than they start with");
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void EachLevelUnlocksTheNext()
        {
            CheckersCampaign campaign = Campaign();
            var progress = new CheckersProgress(null, GameType.Checkers);
            Assert.IsTrue(campaign.IsUnlocked(0, progress));
            for (int i = 1; i < campaign.Count; i++)
            {
                Assert.IsFalse(campaign.IsUnlocked(i, progress), $"level {i} is open before level {i - 1} was won");
            }
            for (int i = 0; i < campaign.Count - 1; i++)
            {
                progress.RecordLevel(i, 1, 100);
                Assert.IsTrue(campaign.IsUnlocked(i + 1, progress), $"finishing level {i} opens level {i + 1}");
                if (i + 2 < campaign.Count)
                {
                    Assert.IsFalse(campaign.IsUnlocked(i + 2, progress), $"finishing level {i} does not open level {i + 2}");
                }
            }
            Assert.AreEqual(campaign.Count - 1, campaign.TotalStars(progress));
        }

        [Test]
        public void StarsAreForTheWinThePiecesKeptAndTheSpeed()
        {
            CheckersLevel level = Level(Variant.English, 0, Side.Dark, 8, 60);
            try
            {
                CheckersGame quick = Won(Variant.English, Side.Dark, 10);
                Assert.AreEqual(3, CheckersCampaign.StarsFor(level, quick, Side.Dark), "a quick win with every piece");
                Assert.AreEqual(0, CheckersCampaign.StarsFor(level, quick, Side.Light), "the loser earns nothing");

                // The same win, but as if it had taken longer than the level allows: the speed star is lost.
                CheckersGame slow = quick.Clone();
                slow.ply = level.MaxPlies + 1;
                Assert.AreEqual(2, CheckersCampaign.StarsFor(level, slow, Side.Dark), "a slow win loses the speed star");

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
                Assert.AreEqual(1, CheckersCampaign.StarsFor(level, thinned, Side.Dark), "a win with too few pieces kept is one star");

                var playing = new CheckersGame(Variant.English);
                Assert.AreEqual(0, CheckersCampaign.StarsFor(level, playing, Side.Dark), "no stars before the end");
                Assert.AreEqual(0, CheckersCampaign.StarsFor(null, quick, Side.Dark));
                Assert.AreEqual(0, CheckersCampaign.StarsFor(level, null, Side.Dark));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void TheScoreRewardsPiecesLeftAndPliesSaved()
        {
            CheckersGame quick = Won(Variant.English, Side.Dark, 10);
            Assert.AreEqual(quick.board.Count(Side.Dark) * 10 + 1000 - quick.ply, CheckersCampaign.ScoreFor(quick, Side.Dark));
            Assert.GreaterOrEqual(CheckersCampaign.ScoreFor(null, Side.Light), 1);
        }

        [Test]
        public void ProgressCountsGamesWinsAndDraws()
        {
            var progress = new CheckersProgress(null, GameType.Checkers);
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
