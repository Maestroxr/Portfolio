using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class StrikePathsTest
    {
        /// <summary>The largest air unit (the transport) must be fully outside the field at a path's start.</summary>
        private const float LargestRadius = 3.2f;


        [Test]
        public void EveryPathIsDeterministicAndStartsOffScreen()
        {
            Vector2 half = StrikeRules.HalfSize;
            Assert.That(FlightPaths.All.Length, Is.EqualTo(12));
            foreach (FlightPath path in FlightPaths.All)
            {
                Vector2[] first = FlightPaths.Waypoints(path, false);
                Vector2[] second = FlightPaths.Waypoints(path, false);
                Assert.That(first, Is.EqualTo(second), $"{path} is the same every time.");
                Assert.That(first.Length, Is.GreaterThanOrEqualTo(3), $"{path} has a shape.");
                Vector2 start = first[0];
                bool outside = Mathf.Abs(start.x) >= half.x + LargestRadius || Mathf.Abs(start.y) >= half.y + LargestRadius;
                Assert.IsTrue(outside, $"{path} starts off screen ({start}).");
                if (path == FlightPath.RiseUp)
                {
                    Assert.That(start.y, Is.LessThan(-half.y), "RiseUp comes from the bottom.");
                }
                else if (path == FlightPath.CrossLeft)
                {
                    Assert.That(start.x, Is.GreaterThan(half.x), "CrossLeft enters from the right side.");
                    Assert.That(first[first.Length - 1].x, Is.LessThan(-half.x), "CrossLeft leaves at the left side.");
                }
                else
                {
                    Assert.That(start.y, Is.GreaterThan(half.y), $"{path} enters from the top.");
                    Assert.That(first[1].y, Is.LessThan(start.y), $"{path} comes down onto the screen.");
                }
            }
        }


        [Test]
        public void MirroringFlipsLeftAndRight()
        {
            foreach (FlightPath path in FlightPaths.All)
            {
                Vector2[] plain = FlightPaths.Waypoints(path, false);
                Vector2[] mirrored = FlightPaths.Waypoints(path, true);
                for (int i = 0; i < plain.Length; i++)
                {
                    Assert.That(mirrored[i].x, Is.EqualTo(-plain[i].x), $"{path} waypoint {i}");
                    Assert.That(mirrored[i].y, Is.EqualTo(plain[i].y), $"{path} waypoint {i}");
                }
            }
        }


        [Test]
        public void HoveringPathsRepeatOnAClosedLoop()
        {
            foreach (FlightPath path in FlightPaths.All)
            {
                bool hovers = path == FlightPath.HoverTop || path == FlightPath.HoverMid || path == FlightPath.Spiral;
                Assert.That(FlightPaths.DefaultType(path), Is.EqualTo(hovers ? FlightType.Repeat : FlightType.Linear), path.ToString());
                Vector2[] points = FlightPaths.Normalised(path);
                int from = FlightPaths.RepeatFrom(path);
                Assert.That(from, Is.InRange(0, points.Length - 2), path.ToString());
                if (hovers)
                {
                    Assert.That(from, Is.GreaterThan(0), $"{path} flies in before it loops.");
                    Assert.That((points[points.Length - 1] - points[from]).magnitude, Is.LessThan(1e-4f), $"{path} ends where its loop starts.");
                    for (int i = from; i < points.Length; i++)
                    {
                        Assert.That(Mathf.Abs(points[i].x), Is.LessThan(1f), $"{path} loops on screen.");
                        Assert.That(Mathf.Abs(points[i].y), Is.LessThan(1f), $"{path} loops on screen.");
                    }
                }
            }
        }


        [Test]
        public void CurvesAreFlownAtConstantSpeedThroughTheWaypoints()
        {
            foreach (FlightPath path in FlightPaths.All)
            {
                FlightCurve curve = FlightPaths.Curve(path, false, false);
                Vector2[] points = FlightPaths.Waypoints(path, false);
                Assert.That(curve.Start, Is.EqualTo(points[0]), path.ToString());
                Assert.That((curve.End - points[points.Length - 1]).magnitude, Is.LessThan(1e-4f), path.ToString());
                Assert.That(curve.Length, Is.GreaterThanOrEqualTo(Vector2.Distance(points[0], points[points.Length - 1])), path.ToString());
                const float step = 0.25f;
                for (float d = step; d < curve.Length; d += step)
                {
                    float moved = Vector2.Distance(curve.PointAt(d - step), curve.PointAt(d));
                    Assert.That(moved, Is.EqualTo(step).Within(0.02f), $"{path} at {d:0.0} m moves evenly.");
                    Vector2 direction = curve.DirectionAt(d);
                    Assert.That(direction.magnitude, Is.EqualTo(1f).Within(1e-3f));
                }
                Assert.That(curve.Loops, Is.False);
                Assert.IsTrue(curve.IsPastEnd(curve.Length + 0.01f));
            }
        }


        [Test]
        public void LinearFlightsKeepTheirDirectionAndLeaveTheField()
        {
            Vector2 half = StrikeRules.HalfSize;
            foreach (FlightPath path in FlightPaths.All)
            {
                foreach (bool mirror in new[] { false, true })
                {
                    FlightCurve curve = FlightPaths.Curve(path, mirror, false);
                    Vector2 past = curve.PointAt(curve.Length + 5f);
                    Assert.That((past - (curve.End + curve.EndDirection * 5f)).magnitude, Is.LessThan(1e-3f), $"{path} goes straight on.");
                    Vector2 gone = curve.PointAt(curve.Length + 60f);
                    bool outside = Mathf.Abs(gone.x) > half.x + LargestRadius || Mathf.Abs(gone.y) > half.y + LargestRadius;
                    Assert.IsTrue(outside, $"{path} (mirror {mirror}) leaves the field ({gone}).");
                }
            }
        }


        [Test]
        public void RepeatingFlightsWrapIntoTheirLoop()
        {
            foreach (FlightPath path in new[] { FlightPath.HoverTop, FlightPath.HoverMid, FlightPath.Spiral, FlightPath.Zigzag })
            {
                FlightCurve curve = FlightPaths.Curve(path, false, true);
                Assert.IsTrue(curve.Loops, path.ToString());
                Assert.That(curve.LoopStart, Is.LessThan(curve.Length));
                float loop = curve.Length - curve.LoopStart;
                for (int lap = 1; lap <= 3; lap++)
                {
                    float d = curve.LoopStart + 1.3f + lap * loop;
                    Assert.That((curve.PointAt(d) - curve.PointAt(curve.LoopStart + 1.3f)).magnitude, Is.LessThan(1e-3f), $"{path} lap {lap}");
                    Assert.That(curve.Wrap(d), Is.InRange(curve.LoopStart, curve.Length));
                }
                Assert.IsFalse(curve.IsPastEnd(curve.Length * 10f));
            }
            Assert.That(FlightPaths.Curve(FlightPath.Spiral, true, true), Is.SameAs(FlightPaths.Curve(FlightPath.Spiral, true, true)), "Curves are shared.");
        }


        [Test]
        public void CatmullRomPassesThroughItsInnerPoints()
        {
            var p0 = new Vector2(-1f, 3f);
            var p1 = new Vector2(0f, 0f);
            var p2 = new Vector2(2f, 1f);
            var p3 = new Vector2(5f, -2f);
            Assert.That((FlightPaths.CatmullRom(p0, p1, p2, p3, 0f) - p1).magnitude, Is.LessThan(1e-5f));
            Assert.That((FlightPaths.CatmullRom(p0, p1, p2, p3, 1f) - p2).magnitude, Is.LessThan(1e-5f));
        }
    }
}
