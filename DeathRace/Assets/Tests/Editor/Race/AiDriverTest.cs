using System.Collections.Generic;
using Gamebox.Racing;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The computer driver's decisions on the stadium of <see cref="RacingLineTest"/>, from made-up senses.</summary>
    public class AiDriverTest
    {
        private const float Step = 0.05f;

        private RacePath path;
        private CarHandling handling;
        private RacingLine line;

        [SetUp]
        public void SetUp()
        {
            path = RacingLineTest.Stadium();
            handling = new CarHandling(new CarLoadout { model = CarModel.Viper }.Stats());
            line = RacingLine.Build(path, d => RacingLineTest.HalfWidth);
        }

        private AiDriver Driver(float skill = 1f)
        {
            var driver = new AiDriver(skill, 7);
            driver.Prepare(line, handling, d => RacingLineTest.HalfWidth);
            return driver;
        }

        /// <summary>A car on the line at <paramref name="distance"/>, pointing along the road, at <paramref name="speed"/>.</summary>
        private AiSense Sense(float distance, float speed, float turn = 0f)
        {
            Vector2 tangent = path.TangentAt(distance);
            Vector2 forward = Rotate(tangent, turn);
            return new AiSense
            {
                Position = line.PointAt(distance),
                Forward = forward,
                Velocity = forward * speed,
                Speed = speed,
                Distance = distance,
                Offset = line.OffsetAt(distance),
                HalfWidth = RacingLineTest.HalfWidth,
                CarHalfWidth = 1f,
                CarHalfLength = 2.3f,
                Racing = true,
                Armed = true,
                Ammo = 100,
                Pace = 1f,
                Others = new List<AiOther>(),
                Hazards = new List<Vector2>()
            };
        }

        /// <summary>Another car at <paramref name="distance"/> along the road, <paramref name="offset"/> across it, going <paramref name="speed"/>.</summary>
        private AiOther Other(float distance, float offset, float speed, int id = 1)
        {
            Vector2 tangent = path.TangentAt(distance);
            return new AiOther
            {
                Id = id,
                Position = path.PointAt(distance) + path.NormalAt(distance) * offset,
                Velocity = tangent * speed,
                Distance = distance,
                Offset = offset,
                HalfWidth = 1f,
                Health01 = 1f
            };
        }

        private static Vector2 Rotate(Vector2 vector, float degreesRight)
        {
            float radians = -degreesRight * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        /// <summary>Thinks for <paramref name="seconds"/> on the same sense; true if any step did what <paramref name="did"/> asks.</summary>
        private static bool Any(AiDriver driver, AiSense sense, float seconds, System.Func<CarInput, bool> did)
        {
            for (float time = 0f; time < seconds; time += Step)
            {
                if (did(driver.Think(sense, Step)))
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void ItSteersBackTowardsTheRoad()
        {
            AiDriver driver = Driver();
            Assert.Greater(driver.Think(Sense(60f, 20f, turn: -20f), Step).Steer, 0.2f, "pointing left of the road: steer right");
            Assert.Less(driver.Think(Sense(60f, 20f, turn: 20f), Step).Steer, -0.2f, "pointing right of the road: steer left");
        }

        [Test]
        public void ItFloorsItOnAStraightAndBrakesForTheBendAhead()
        {
            AiDriver driver = Driver();
            Assert.AreEqual(1f, driver.Think(Sense(40f, 15f), Step).Throttle, "slow on a straight: full throttle");
            float bend = driver.PlannedSpeed(RacingLineTest.BendMiddle);
            Assert.Less(bend, handling.TopSpeed * 0.95f, "the bend needs braking");
            AiDriver late = Driver();
            Assert.Less(late.Think(Sense(RacingLineTest.Straight + 25f, handling.TopSpeed), Step).Throttle, 0f, "flat out just before the bend: brake");
        }

        [Test]
        public void ABeginnerLapsSlowerThanAChampion()
        {
            Assert.Less(Driver(0f).PlannedSpeed(RacingLineTest.BendMiddle), Driver(1f).PlannedSpeed(RacingLineTest.BendMiddle) * 0.92f);
            Assert.Less(Driver(0f).PlannedSpeed(100f), Driver(1f).PlannedSpeed(100f), "and keeps something back on the straight");
            Assert.Greater(Driver(0f).PlannedLapTime(), Driver(1f).PlannedLapTime() * 1.12f);
            Assert.Greater(Driver(0.5f).PlannedLapTime(), Driver(1f).PlannedLapTime() * 1.04f);
        }

        [Test]
        public void ItFiresAtACarRightAheadButNotAtOneOffToTheSide()
        {
            AiSense ahead = Sense(40f, 25f);
            ahead.Others.Add(Other(65f, line.OffsetAt(65f), 25f));
            Assert.IsFalse(Any(Driver(), ahead, 1.5f, input => input.Fire), "nobody shoots right off the grid");
            Assert.IsTrue(Any(Driver(), ahead, 3f, input => input.Fire));

            AiSense aside = Sense(40f, 25f);
            aside.Others.Add(Other(65f, line.OffsetAt(65f) + 9f, 25f));
            Assert.IsFalse(Any(Driver(), aside, 4f, input => input.Fire));

            AiSense empty = Sense(40f, 25f);
            empty.Ammo = 0;
            empty.Others.Add(Other(65f, line.OffsetAt(65f), 25f));
            Assert.IsFalse(Any(Driver(), empty, 4f, input => input.Fire), "no rounds, no shots");
        }

        [Test]
        public void ItDropsAMineOnACarCloseBehind()
        {
            AiSense chased = Sense(80f, 25f);
            chased.Mines = 2;
            chased.Others.Add(Other(74f, line.OffsetAt(74f), 27f));
            Assert.IsFalse(Any(Driver(), chased, 7f, input => input.DropMine), "no mines in the first seconds of the race");
            Assert.IsTrue(Any(Driver(), chased, 18f, input => input.DropMine));

            AiSense none = chased;
            none.Mines = 0;
            Assert.IsFalse(Any(Driver(), none, 18f, input => input.DropMine));
        }

        [Test]
        public void ItFiresARocketAtACarAheadInTheCone()
        {
            AiSense sense = Sense(20f, 25f);
            sense.Rockets = 1;
            sense.Others.Add(Other(60f, line.OffsetAt(60f), 20f));
            Assert.IsTrue(Any(Driver(), sense, 16f, input => input.FireRocket));
        }

        [Test]
        public void ItBurnsNitroOnALongStraight()
        {
            AiSense sense = Sense(60f, handling.TopSpeed * 0.8f);
            sense.NitroCharges = 2;
            Assert.IsTrue(Any(Driver(), sense, 10f, input => input.Nitro));

            AiSense bend = Sense(RacingLineTest.Straight - 20f, handling.TopSpeed * 0.8f);
            bend.NitroCharges = 2;
            Assert.IsFalse(Any(Driver(), bend, 10f, input => input.Nitro), "not into a bend");
        }

        [Test]
        public void ItPassesASlowerCarOnTheSideWithRoom()
        {
            AiDriver driver = Driver();
            float offset = line.OffsetAt(70f);
            AiSense free = Sense(50f, 30f);
            float alone = driver.Think(free, Step).Steer;
            AiSense blocked = Sense(50f, 30f);
            // A slow car sits on the line: the way past is the side with room (left, unless the line hugs the left edge).
            blocked.Others.Add(Other(70f, offset, 8f));
            float passing = Driver().Think(blocked, Step).Steer;
            float clearance = 1f + 1f + 1.3f;
            float limit = RacingLineTest.HalfWidth - 1f - 0.7f;
            float towards = offset + clearance <= limit ? -1f : 1f;
            Assert.Greater((passing - alone) * towards, 0.05f, "it moves aside to pass");
        }

        [Test]
        public void ItBacksOutWhenStuck()
        {
            AiDriver driver = Driver();
            AiSense stuck = Sense(60f, 0f);
            stuck.WallAhead = true;
            Assert.IsTrue(Any(driver, stuck, 2f, input => input.Throttle < -0.9f));
            Assert.AreEqual(1, driver.StuckCount);
        }

        [Test]
        public void ItHoldsItsFireAndItsLineAfterTheFinish()
        {
            AiDriver driver = Driver();
            driver.Cruise = true;
            AiSense ahead = Sense(40f, 25f);
            ahead.Others.Add(Other(65f, line.OffsetAt(65f), 25f));
            Assert.IsFalse(Any(driver, ahead, 4f, input => input.Fire));
        }

        [Test]
        public void TheRubberBandIsGentle()
        {
            Assert.AreEqual(1f, AiDriver.RubberBand(30f));
            Assert.AreEqual(1f, AiDriver.RubberBand(-30f));
            Assert.Less(AiDriver.RubberBand(200f), 1f);
            Assert.GreaterOrEqual(AiDriver.RubberBand(5000f), 0.93f);
            Assert.Greater(AiDriver.RubberBand(-200f), 1f);
            Assert.LessOrEqual(AiDriver.RubberBand(-5000f), 1.04f);
        }

        [Test]
        public void ItRevsOnTheGridAndWaitsForTheGreenLight()
        {
            AiDriver driver = Driver();
            AiSense grid = Sense(0f, 0f);
            grid.Racing = false;
            grid.Countdown = true;
            CarInput input = driver.Think(grid, Step);
            Assert.Greater(input.Throttle, 0f);
            Assert.IsFalse(input.Fire);
        }

        [Test]
        public void AnglesToTheRightArePositive()
        {
            Assert.AreEqual(90f, AiDriver.AngleRight(Vector2.up, Vector2.right) * Mathf.Rad2Deg, 1e-3f);
            Assert.AreEqual(-90f, AiDriver.AngleRight(Vector2.up, Vector2.left) * Mathf.Rad2Deg, 1e-3f);
        }
    }
}
