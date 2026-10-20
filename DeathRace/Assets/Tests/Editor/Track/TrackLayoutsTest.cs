using System.Collections.Generic;
using Gamebox.Racing;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The twelve tracks: the contract's names, lands and laps, and every layout keeping the rules of a good track.</summary>
    public class TrackLayoutsTest
    {
        [Test]
        public void TheTwelveTracksAreTheContractsInCampaignOrder()
        {
            string[] names =
            {
                "Dust Bowl", "Scrapyard Loop", "Frostbite Pass", "Neon Mile", "Canyon Run", "Smelter", "Glacier Ring", "Downtown Carnage", "The Crucible",
                "Refinery Row", "Avalanche Ridge", "Gridlock Boulevard"
            };
            Biome[] biomes =
            {
                Biome.Desert, Biome.Industrial, Biome.Snow, Biome.City, Biome.Desert, Biome.Industrial, Biome.Snow, Biome.City, Biome.Desert,
                Biome.Industrial, Biome.Snow, Biome.City
            };
            int[] laps = { 4, 4, 4, 4, 5, 5, 5, 5, 3, 5, 5, 4 };
            Assert.AreEqual(12, TrackLayouts.Count);
            for (int i = 0; i < names.Length; i++)
            {
                TrackLayout layout = TrackLayouts.All[i];
                Assert.AreEqual(names[i], layout.Name);
                Assert.AreEqual(biomes[i], layout.Biome, layout.Name);
                Assert.AreEqual(laps[i], layout.Laps, layout.Name);
                Assert.AreEqual(i == 8, layout.Showdown, layout.Name);
            }
        }

        [Test]
        public void EveryLayoutKeepsTheRules()
        {
            var problems = new List<string>();
            foreach (TrackLayout layout in TrackLayouts.All)
            {
                problems.AddRange(new TrackGeometry(layout).Problems());
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryLayoutIsAsLongAsItsKindAsks()
        {
            foreach (TrackLayout layout in TrackLayouts.All)
            {
                float length = TrackGeometry.CreatePath(layout).Length;
                if (layout.Showdown)
                {
                    Assert.That(length, Is.InRange(TrackGeometry.ArenaMinLength, TrackGeometry.ArenaMaxLength), layout.Name);
                }
                else
                {
                    Assert.That(length, Is.InRange(TrackGeometry.MinLength, TrackGeometry.MaxLength), layout.Name);
                }
            }
        }

        [Test]
        public void NamesSeedsAndDescriptionsAreTheirOwn()
        {
            var names = new HashSet<string>();
            var seeds = new HashSet<int>();
            var descriptions = new HashSet<string>();
            foreach (TrackLayout layout in TrackLayouts.All)
            {
                Assert.IsTrue(names.Add(layout.Name), layout.Name);
                Assert.IsTrue(seeds.Add(layout.Seed), $"{layout.Name} shares its seed");
                Assert.IsFalse(string.IsNullOrEmpty(layout.Description), layout.Name);
                Assert.IsTrue(descriptions.Add(layout.Description), $"{layout.Name} shares its description");
            }
        }

        [Test]
        public void TheHarderLeaguesTracksAreFastWithAPinchPoint()
        {
            float longest = 0f;
            for (int index = 9; index < TrackLayouts.Count; index++)
            {
                TrackLayout layout = TrackLayouts.Of(index);
                RacePath path = TrackGeometry.CreatePath(layout);
                Assert.AreEqual(TrackGeometry.MaxWidth, layout.Width, $"{layout.Name} is as wide as a track gets");
                Assert.Greater(path.Length, 1000f, layout.Name);
                Assert.IsFalse(layout.Showdown, layout.Name);
                Assert.IsNotEmpty(layout.Stretches, $"{layout.Name} has a narrow stretch");
                foreach (TrackLayout.Stretch stretch in layout.Stretches)
                {
                    Assert.LessOrEqual(stretch.Width, layout.Width - 3f, $"{layout.Name}: the stretch closes in");
                    Assert.Greater(stretch.To - stretch.From, 60f, $"{layout.Name}: long enough to queue in");
                }
                Assert.GreaterOrEqual(layout.SetPieces.Length, 4, $"{layout.Name} has set pieces");
                // A straight for the nitro: the longest run where the line bends no more than at a start line.
                float run = 0f;
                float best = 0f;
                for (int lap = 0; lap < 2; lap++)
                {
                    for (int i = 0; i < path.Count; i++)
                    {
                        run = Mathf.Abs(path.CurvatureAt(i * path.Spacing, 10f)) < TrackGeometry.StraightCurvature ? run + path.Spacing : 0f;
                        best = Mathf.Max(best, run);
                    }
                }
                Assert.Greater(best, 200f, $"{layout.Name} has a straight of {best:0} m");
                longest = Mathf.Max(longest, path.Length);
            }
            Assert.AreEqual(longest, TrackGeometry.CreatePath(TrackLayouts.Of(11)).Length, 0.01f, "Gridlock Boulevard is the longest of them");
            for (int index = 0; index < 9; index++)
            {
                Assert.Less(TrackGeometry.CreatePath(TrackLayouts.Of(index)).Length, longest, "and of all");
            }
        }

        [Test]
        public void OfClampsTheIndex()
        {
            Assert.AreSame(TrackLayouts.All[0], TrackLayouts.Of(-3));
            Assert.AreSame(TrackLayouts.All[4], TrackLayouts.Of(4));
            Assert.AreSame(TrackLayouts.All[11], TrackLayouts.Of(99));
        }

        [Test]
        public void TheProblemsCatchABadLayout()
        {
            var cramped = new TrackLayout
            {
                Name = "Cramped",
                Description = "",
                Biome = Biome.City,
                Width = 20f,
                Laps = 4,
                StartDistance = 10f,
                PickupDistances = new[] { 10f },
                Points = new[]
                {
                    new UnityEngine.Vector2(0f, 0f), new UnityEngine.Vector2(60f, 0f), new UnityEngine.Vector2(60f, 30f),
                    new UnityEngine.Vector2(0f, 30f)
                }
            };
            string problems = string.Join("\n", new TrackGeometry(cramped).Problems());
            StringAssert.Contains("description", problems);
            StringAssert.Contains("width", problems);
            StringAssert.Contains("long", problems);
            StringAssert.Contains("radius", problems);
            StringAssert.Contains("pickup", problems);
        }
    }
}
