using System.Collections.Generic;
using Gamebox.Racing;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>The measurements a race reads off a track: the grid, the pickups, the widths, the racing line and its speeds.</summary>
    public class TrackGeometryTest
    {
        private static IEnumerable<TrackGeometry> All()
        {
            foreach (TrackLayout layout in TrackLayouts.All)
            {
                yield return new TrackGeometry(layout);
            }
        }

        [Test]
        public void GridSlotsAreOnTheRoadBehindTheLineFacingTheWay()
        {
            foreach (TrackGeometry geometry in All())
            {
                RacePath path = geometry.Path;
                for (int i = 0; i < 8; i++)
                {
                    Pose slot = geometry.GridSlot(i);
                    Vector2 point = RacePath.ToPlane(slot.position);
                    float distance = path.Project(point, geometry.LineDistance - 25f, 40f);
                    string where = $"{geometry.Layout.Name} slot {i}";
                    Assert.Less(path.Delta(geometry.LineDistance, distance), -4f, $"{where} is behind the line");
                    Assert.Less(Mathf.Abs(path.SignedOffset(point, distance)) + 1.2f, geometry.HalfWidthAt(distance), $"{where} is on the road");
                    Assert.AreEqual(0f, slot.position.y, 1e-4f, where);
                    Vector3 forward = slot.rotation * Vector3.forward;
                    Vector2 tangent = path.TangentAt(distance);
                    Assert.Greater(Vector2.Dot(new Vector2(forward.x, forward.z), tangent), 0.99f, $"{where} faces the way of travel");
                }
            }
        }

        [Test]
        public void GridSlotsKeepTheirDistanceAndThePoleLeads()
        {
            foreach (TrackGeometry geometry in All())
            {
                var slots = new List<Vector3>();
                for (int i = 0; i < 8; i++)
                {
                    slots.Add(geometry.GridSlot(i).position);
                }
                for (int i = 0; i < slots.Count; i++)
                {
                    for (int j = i + 1; j < slots.Count; j++)
                    {
                        Assert.Greater(Vector3.Distance(slots[i], slots[j]), 4.5f, $"{geometry.Layout.Name} slots {i} and {j}");
                    }
                }
                float pole = geometry.Path.Delta(geometry.LineDistance, geometry.Path.Project(RacePath.ToPlane(slots[0]), geometry.LineDistance, 40f));
                float second = geometry.Path.Delta(geometry.LineDistance, geometry.Path.Project(RacePath.ToPlane(slots[1]), geometry.LineDistance, 40f));
                Assert.Greater(pole, second, $"{geometry.Layout.Name}: the pole is nearest the line");
            }
        }

        [Test]
        public void PickupSpotsLieOnTheRoadInClustersOfThree()
        {
            foreach (TrackGeometry geometry in All())
            {
                TrackLayout layout = geometry.Layout;
                Assert.AreEqual(layout.PickupDistances.Length * 3, geometry.PickupSpots.Count, layout.Name);
                Assert.AreEqual(geometry.PickupSpots.Count, geometry.PickupClusters.Count, layout.Name);
                for (int i = 0; i < geometry.PickupSpots.Count; i++)
                {
                    Vector2 point = RacePath.ToPlane(geometry.PickupSpots[i]);
                    int cluster = geometry.PickupClusters[i];
                    float distance = geometry.Path.Project(point, layout.PickupDistances[cluster], 30f);
                    Assert.Less(Mathf.Abs(geometry.Path.Delta(layout.PickupDistances[cluster], distance)), 8f, $"{layout.Name} spot {i} is by its cluster");
                    Assert.Less(Mathf.Abs(geometry.Path.SignedOffset(point, distance)) + 1f, geometry.HalfWidthAt(distance), $"{layout.Name} spot {i}");
                    Assert.IsTrue(geometry.IsOnRoad(point), $"{layout.Name} spot {i}");
                }
            }
        }

        [Test]
        public void TheRacingLineStaysOnTheRoadAndCutsTheApex()
        {
            foreach (TrackGeometry geometry in All())
            {
                RacePath path = geometry.Path;
                float tightest = 0f;
                float tightestAt = 0f;
                for (int i = 0; i < path.Count; i++)
                {
                    float distance = i * path.Spacing;
                    float limit = geometry.HalfWidthAt(distance) - TrackGeometry.LineMargin;
                    Assert.LessOrEqual(Mathf.Abs(geometry.RacingLineOffset(distance)), limit + 0.01f, $"{geometry.Layout.Name} at {distance:0}");
                    float curvature = path.CurvatureAt(distance, 10f);
                    if (Mathf.Abs(curvature) > Mathf.Abs(tightest))
                    {
                        tightest = curvature;
                        tightestAt = distance;
                    }
                }
                float apex = geometry.RacingLineOffset(tightestAt);
                Assert.AreEqual(Mathf.Sign(tightest), Mathf.Sign(apex), $"{geometry.Layout.Name}: the line hugs the inside of its tightest corner");
                Assert.Greater(Mathf.Abs(apex), (geometry.HalfWidthAt(tightestAt) - TrackGeometry.LineMargin) * 0.5f, geometry.Layout.Name);
            }
        }

        [Test]
        public void TheSpeedHintIsFlatOutOnStraightsAndLowBeforeTheTightestCorner()
        {
            foreach (TrackGeometry geometry in All())
            {
                RacePath path = geometry.Path;
                float lowest = 1f;
                float highest = 0f;
                for (int i = 0; i < path.Count; i++)
                {
                    float hint = geometry.SpeedHint(i * path.Spacing);
                    Assert.That(hint, Is.InRange(0f, 1f));
                    lowest = Mathf.Min(lowest, hint);
                    highest = Mathf.Max(highest, hint);
                }
                Assert.AreEqual(1f, highest, 1e-4f, $"{geometry.Layout.Name} has somewhere to go flat out");
                Assert.Less(lowest, 0.75f, $"{geometry.Layout.Name} has a corner to brake for");
                Assert.Greater(lowest, 0.15f, $"{geometry.Layout.Name}: no corner is a dead stop");
                Assert.AreEqual(geometry.SafeSpeed(100f) / TrackGeometry.ReferenceSpeed, geometry.SpeedHint(100f), 1e-4f);
            }
        }

        [Test]
        public void HairpinsOpenUpAndTheGorgeClosesIn()
        {
            TrackGeometry dustBowl = new TrackGeometry(TrackLayouts.Of(0));
            float widest = 0f;
            for (int i = 0; i < dustBowl.Path.Count; i++)
            {
                widest = Mathf.Max(widest, dustBowl.HalfWidthAt(i * dustBowl.Path.Spacing) * 2f);
            }
            Assert.Greater(widest, dustBowl.Layout.Width * 1.1f, "the hairpin is wider");
            Assert.AreEqual(dustBowl.Layout.Width, dustBowl.HalfWidthAt(dustBowl.LineDistance) * 2f, 0.01f, "the start straight is not");

            TrackLayout canyon = TrackLayouts.Of(4);
            Assert.IsNotEmpty(canyon.Stretches);
            TrackLayout.Stretch gorge = canyon.Stretches[0];
            var canyonGeometry = new TrackGeometry(canyon);
            Assert.AreEqual(gorge.Width, canyonGeometry.HalfWidthAt((gorge.From + gorge.To) * 0.5f) * 2f, 0.5f);
            Assert.AreEqual(canyon.Width, canyonGeometry.HalfWidthAt(canyonGeometry.LineDistance) * 2f, 0.01f);
        }

        [Test]
        public void ThePavementEndsAtTheEdgeAndTheBarriersStandBehindTheVerge()
        {
            foreach (TrackGeometry geometry in All())
            {
                RacePath path = geometry.Path;
                for (float distance = 0f; distance < path.Length; distance += 37f)
                {
                    Vector2 centre = path.PointAt(distance);
                    Vector2 normal = path.NormalAt(distance);
                    float half = geometry.HalfWidthAt(distance);
                    Assert.IsTrue(geometry.IsOnRoad(centre + normal * (half - 0.3f), distance), $"{geometry.Layout.Name} at {distance:0}");
                    Assert.IsFalse(geometry.IsOnRoad(centre - normal * (half + 0.5f), distance), $"{geometry.Layout.Name} at {distance:0}");
                    Assert.AreEqual(half + TrackGeometry.Shoulder, geometry.BarrierOffset(distance), 1e-4f);
                }
            }
        }

        [Test]
        public void FacingTurnsForwardToTheDirection()
        {
            Vector2[] directions = { Vector2.up, Vector2.right, Vector2.down, new Vector2(-1f, 1f).normalized, new Vector2(0.3f, -0.95f).normalized };
            foreach (Vector2 direction in directions)
            {
                Vector3 forward = TrackGeometry.Facing(direction) * Vector3.forward;
                Assert.AreEqual(direction.x, forward.x, 1e-4f);
                Assert.AreEqual(0f, forward.y, 1e-4f);
                Assert.AreEqual(direction.y, forward.z, 1e-4f);
            }
        }

        [Test]
        public void RespawnStandsOnTheMiddleOfTheRoad()
        {
            var geometry = new TrackGeometry(TrackLayouts.Of(2));
            Pose pose = geometry.Respawn(300f);
            Vector2 point = RacePath.ToPlane(pose.position);
            Assert.AreEqual(300f, geometry.Path.Project(point, 300f), 0.5f);
            Assert.AreEqual(0f, geometry.Path.SignedOffset(point, 300f), 0.05f);
        }
    }
}
