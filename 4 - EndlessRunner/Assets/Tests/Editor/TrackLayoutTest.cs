using System.Collections.Generic;
using System.Linq;
using Gamebox;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EndlessRunner.Tests
{
    /// <summary>
    /// A race lays the track out on every device by itself and names its coins by the ids of the layout, so the layout
    /// has to come out the same wherever it is built from the same level, settings and seed, and only then.
    /// </summary>
    public class TrackLayoutTest
    {
        private Tracks tracks;

        [SetUp]
        public void SetUp()
        {
            tracks = new Tracks();
        }

        [TearDown]
        public void TearDown()
        {
            tracks.Destroy();
        }

        [Test]
        public void TheSameSeedLaysOutTheSameTrack()
        {
            RunnerLevel level = tracks.Level(700f);
            RunnerSettings settings = tracks.Settings();
            TrackLayout first = tracks.Layout(level, settings, 4711);
            TrackLayout second = tracks.Layout(level, settings, 4711);
            Assert.Greater(first.Pieces.Count, 100);
            Assert.Greater(first.Coins, 0);
            AssertSame(first, second);
        }

        [Test]
        public void AnotherSeedLaysOutAnotherTrack()
        {
            RunnerLevel level = tracks.Level(700f);
            RunnerSettings settings = tracks.Settings();
            Assert.IsFalse(Same(tracks.Layout(level, settings, 4711), tracks.Layout(level, settings, 4712)));
        }

        [Test]
        public void OtherSettingsLayOutAnotherTrack()
        {
            // The spacing of a track follows the speed of the run and its coin arcs the jump: why a race runs with the
            // settings of the level and not with the custom ones of a player.
            RunnerLevel level = tracks.Level(700f);
            TrackLayout standard = tracks.Layout(level, tracks.Settings(), 4711);
            Assert.IsFalse(Same(standard, tracks.Layout(level, tracks.Settings(14f, 22f), 4711)));
            Assert.IsFalse(Same(standard, tracks.Layout(level, tracks.Settings(10f, 16f, 2.6f), 4711)));
        }

        [Test]
        public void EveryPieceHasAnIdOfItsOwn()
        {
            TrackLayout layout = tracks.Layout(tracks.Level(700f), tracks.Settings(), 99);
            var ids = new HashSet<int>();
            foreach (PiecePlacement piece in layout.Pieces)
            {
                Assert.GreaterOrEqual(piece.Id, 0);
                Assert.IsTrue(ids.Add(piece.Id), $"id {piece.Id} is used twice");
            }
        }

        [Test]
        public void TheCoinsOfATrackAreTheValuesOfItsCoinsAndGems()
        {
            TrackLayout layout = tracks.Layout(tracks.Level(700f), tracks.Settings(), 7);
            int coins = layout.Pieces.Where(piece => piece.Prefab is Coin).Sum(piece => ((Coin)piece.Prefab).Value);
            Assert.AreEqual(coins, layout.Coins);
            Assert.IsTrue(layout.Pieces.Any(piece => piece.Prefab == tracks.Catalog.gem), "a track with gems has some");
        }

        [Test]
        public void AnEndlessRunGrowsTheSameOnEveryDevice()
        {
            RunnerLevel level = tracks.Level(0f);
            RunnerSettings settings = tracks.Settings();
            LayoutBuilder first = tracks.Builder(level, settings, 31337);
            LayoutBuilder second = tracks.Builder(level, settings, 31337);
            // The generator asks for the next chunk when its runner gets near the end of the last one: at another moment
            // on every device, but always for the same stretch. A device that is behind has the start of the same track.
            Grow(first, 5);
            Grow(second, 2);
            AssertSame(first.Layout, second.Layout, second.Layout.Pieces.Count);
            Grow(second, 3);
            AssertSame(first.Layout, second.Layout);
            Assert.AreEqual(first.Layout.Pieces.Count, first.Layout.Pieces.Select(piece => piece.Id).Distinct().Count(), "ids stay unique across the chunks");
            Assert.IsTrue(float.IsPositiveInfinity(first.Layout.FinishZ));
        }

        [Test]
        public void TheLevelsOfTheCampaignLayOutTheSameTrackEveryTime()
        {
            PieceCatalog catalog = Find<PieceCatalog>();
            List<RunnerLevel> levels = FindAll<RunnerLevel>();
            if (catalog == null || levels.Count == 0)
            {
                Assert.Ignore("The campaign of the game is not in this project.");
            }
            foreach (RunnerLevel level in levels)
            {
                Assert.IsNotNull(level.Settings, $"{level.name} has settings of its own");
                float stretch = level.IsEndless ? 900f : level.Length + 120f;
                var first = new LayoutBuilder(level, level.Settings, catalog, Tracks.Gravity, level.Seed);
                var second = new LayoutBuilder(level, level.Settings, catalog, Tracks.Gravity, level.Seed);
                first.GenerateUntil(stretch);
                second.GenerateUntil(stretch);
                Assert.Greater(first.Layout.Pieces.Count, 50, level.name);
                AssertSame(first.Layout, second.Layout);
                if (!level.IsEndless)
                {
                    Assert.Greater(first.Layout.Coins, 0, $"{level.name} has coins to share");
                }
            }
        }

        /// <summary>Extends an endless layout the way the track generator does: a chunk beyond what is there.</summary>
        private static void Grow(LayoutBuilder builder, int chunks)
        {
            for (int i = 0; i < chunks; i++)
            {
                float until = builder.Layout.GeneratedUntil;
                builder.GenerateUntil(float.IsNegativeInfinity(until) ? 300f : until + 300f);
            }
        }

        private static T Find<T>() where T : Object
        {
            List<T> all = FindAll<T>();
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>The assets of a type wherever the game is mounted: under Assets in its own project, as a package in BaseGame.</summary>
        private static List<T> FindAll<T>() where T : Object
        {
            var found = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    found.Add(asset);
                }
            }
            return found;
        }

        private static void AssertSame(TrackLayout expected, TrackLayout actual, int pieces = -1)
        {
            int count = pieces < 0 ? expected.Pieces.Count : pieces;
            if (pieces < 0)
            {
                Assert.AreEqual(expected.Pieces.Count, actual.Pieces.Count, "pieces");
                Assert.AreEqual(expected.Tiles.Count, actual.Tiles.Count, "tiles");
                Assert.AreEqual(expected.Coins, actual.Coins, "coins");
                Assert.AreEqual(expected.FinishZ, actual.FinishZ, "finish");
                CollectionAssert.AreEqual(expected.Gaps, actual.Gaps, "gaps");
                for (int i = 0; i < expected.Tiles.Count; i++)
                {
                    Assert.AreEqual(expected.Tiles[i].Prefab, actual.Tiles[i].Prefab, $"tile {i}");
                    Assert.AreEqual(expected.Tiles[i].Z, actual.Tiles[i].Z, $"tile {i}");
                }
            }
            for (int i = 0; i < count; i++)
            {
                PiecePlacement a = expected.Pieces[i];
                PiecePlacement b = actual.Pieces[i];
                Assert.AreEqual(a.Id, b.Id, $"id of piece {i}");
                Assert.AreEqual(a.Prefab, b.Prefab, $"prefab of piece {a.Id}");
                Assert.AreEqual(a.Position, b.Position, $"position of piece {a.Id}");
                Assert.AreEqual(a.Yaw, b.Yaw, $"yaw of piece {a.Id}");
                Assert.AreEqual(a.Scale, b.Scale, $"scale of piece {a.Id}");
                Assert.AreEqual(a.ParentId, b.ParentId, $"parent of piece {a.Id}");
            }
        }

        private static bool Same(TrackLayout a, TrackLayout b)
        {
            if (a.Pieces.Count != b.Pieces.Count || a.Coins != b.Coins)
            {
                return false;
            }
            for (int i = 0; i < a.Pieces.Count; i++)
            {
                if (a.Pieces[i].Id != b.Pieces[i].Id || a.Pieces[i].Prefab != b.Pieces[i].Prefab || a.Pieces[i].Position != b.Pieces[i].Position)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
