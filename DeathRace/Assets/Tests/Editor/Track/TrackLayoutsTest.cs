using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The nine tracks: the contract's names, lands and laps, and every layout keeping the rules of a good track.</summary>
    public class TrackLayoutsTest
    {
        [Test]
        public void TheNineTracksAreTheContractsInCampaignOrder()
        {
            string[] names = { "Dust Bowl", "Scrapyard Loop", "Frostbite Pass", "Neon Mile", "Canyon Run", "Smelter", "Glacier Ring", "Downtown Carnage", "The Crucible" };
            Biome[] biomes = { Biome.Desert, Biome.Industrial, Biome.Snow, Biome.City, Biome.Desert, Biome.Industrial, Biome.Snow, Biome.City, Biome.Desert };
            int[] laps = { 4, 4, 4, 4, 5, 5, 5, 5, 3 };
            Assert.AreEqual(9, TrackLayouts.Count);
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
        public void OfClampsTheIndex()
        {
            Assert.AreSame(TrackLayouts.All[0], TrackLayouts.Of(-3));
            Assert.AreSame(TrackLayouts.All[4], TrackLayouts.Of(4));
            Assert.AreSame(TrackLayouts.All[8], TrackLayouts.Of(99));
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
