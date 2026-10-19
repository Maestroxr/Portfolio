using System.Collections.Generic;
using Gamebox.Racing;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The racing line and the speeds planned on it, on a stadium: two straights joined by two half circles.</summary>
    public class RacingLineTest
    {
        public const float Straight = 200f;
        public const float Radius = 40f;
        public const float HalfWidth = 7f;

        /// <summary>
        /// A stadium driven anticlockwise (left turns): the bottom straight from x -100 to 100 at z -40, the bend round
        /// (100, 0), the top straight back, the bend round (-100, 0). Distance 0 is the start of the bottom straight.
        /// </summary>
        public static RacePath Stadium()
        {
            var points = new List<Vector2>();
            float half = Straight * 0.5f;
            for (float x = -half; x < half; x += 10f)
            {
                points.Add(new Vector2(x, -Radius));
            }
            for (int a = 0; a < 12; a++)
            {
                float angle = -Mathf.PI * 0.5f + a * Mathf.PI / 12f;
                points.Add(new Vector2(half + Mathf.Cos(angle) * Radius, Mathf.Sin(angle) * Radius));
            }
            for (float x = half; x > -half; x -= 10f)
            {
                points.Add(new Vector2(x, Radius));
            }
            for (int a = 0; a < 12; a++)
            {
                float angle = Mathf.PI * 0.5f + a * Mathf.PI / 12f;
                points.Add(new Vector2(-half + Mathf.Cos(angle) * Radius, Mathf.Sin(angle) * Radius));
            }
            return new RacePath(points, true, 2f);
        }

        /// <summary>The distance along the stadium of the middle of the first bend.</summary>
        public static float BendMiddle => Straight + Mathf.PI * Radius * 0.5f;

        [Test]
        public void TheLineStaysOnTheRoad()
        {
            RacePath path = Stadium();
            RacingLine line = RacingLine.Build(path, d => HalfWidth);
            float room = HalfWidth - RacingLine.DefaultMargin;
            for (int i = 0; i < line.Count; i++)
            {
                Assert.LessOrEqual(Mathf.Abs(line.OffsetOf(i)), room + 1e-3f, $"sample {i}");
            }
        }

        [Test]
        public void TheLineClipsTheInsideOfABendFromTheOutside()
        {
            RacePath path = Stadium();
            RacingLine line = RacingLine.Build(path, d => HalfWidth);
            float room = HalfWidth - RacingLine.DefaultMargin;
            // Anticlockwise the inside is on the left (+).
            Assert.Greater(line.OffsetAt(BendMiddle), room * 0.6f, "the apex is on the inside");
            Assert.Less(line.OffsetAt(Straight - 15f), 0f, "the way in comes from the outside");
            Assert.Less(line.OffsetAt(Straight + Mathf.PI * Radius + 15f), 0f, "the way out runs to the outside");
        }

        [Test]
        public void TheLineIsFasterThanTheMiddleOfTheRoad()
        {
            RacePath path = Stadium();
            var handling = new CarHandling(new CarLoadout { model = CarModel.Viper }.Stats());
            RacingLine line = RacingLine.Build(path, d => HalfWidth);
            RacingLine centre = RacingLine.Centre(path);
            float onLine = new SpeedProfile(line, handling, 1f, 1f).LapTime(handling);
            float inMiddle = new SpeedProfile(centre, handling, 1f, 1f).LapTime(handling);
            Assert.Less(line.Length, centre.Length, "it cuts the bends");
            Assert.Less(onLine, inMiddle * 0.97f);
            Assert.Greater(onLine, path.Length / handling.TopSpeed, "but no lap beats the top speed all the way");
            Assert.AreEqual(0f, line.Blend(0f).OffsetAt(BendMiddle), 1e-5f, "no share of the line is the middle of the road");
        }

        [Test]
        public void TheProfileIsFastOnTheStraightsAndBrakesBeforeTheBends()
        {
            RacePath path = Stadium();
            var handling = new CarHandling(new CarLoadout { model = CarModel.Viper }.Stats());
            var profile = new SpeedProfile(RacingLine.Build(path, d => HalfWidth), handling, 1f, 1f);
            float bend = profile.SpeedAt(BendMiddle);
            float straight = profile.SpeedAt(Straight * 0.5f);
            float before = profile.SpeedAt(Straight - 20f);
            Assert.AreEqual(Mathf.Sqrt(handling.GripAt(bend, 0f) * (Radius + 2f)), bend, bend * 0.25f, "about what the grip holds round the bend");
            Assert.GreaterOrEqual(straight, handling.TopSpeed);
            Assert.Greater(before, bend);
            Assert.Less(before, straight);
            Assert.AreEqual(profile.SpeedAt(BendMiddle - 5f), profile.MinAhead(BendMiddle - 30f, 30f), 5f);
        }

        [Test]
        public void ACarefulDriverPlansSlower()
        {
            RacePath path = Stadium();
            RacingLine line = RacingLine.Build(path, d => HalfWidth);
            var handling = new CarHandling(new CarLoadout { model = CarModel.Rustbucket }.Stats());
            var bold = new SpeedProfile(line, handling, 1f, 1f);
            var careful = new SpeedProfile(line.Blend(0.3f), handling, 0.7f, 0.6f);
            Assert.Less(careful.SpeedAt(BendMiddle), bold.SpeedAt(BendMiddle) * 0.9f);
            Assert.Less(careful.SpeedAt(Straight - 25f), bold.SpeedAt(Straight - 25f));
        }
    }
}
