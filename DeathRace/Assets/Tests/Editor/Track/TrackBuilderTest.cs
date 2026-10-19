using System.Collections.Generic;
using Gamebox.Racing;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.DeathRace.Tests
{
    /// <summary>
    /// Building tracks in the scene (with no art at all, as at the start of the project, and in a night look): the
    /// barrier colliders close the road in on the wall layer without touching it, and a restyle keeps them.
    /// </summary>
    public class TrackBuilderTest
    {
        private GameObject root;
        private DeathRaceTheme night;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Track Test");
            night = ScriptableObject.CreateInstance<DeathRaceTheme>();
            night.night = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(night);
        }

        [Test]
        public void EveryTrackBuildsWithoutArt()
        {
            for (int i = 0; i < TrackLayouts.Count; i++)
            {
                Track track = TrackBuilder.Build(TrackLayouts.Of(i), null, null, root.transform);
                Assert.AreSame(TrackLayouts.Of(i), track.Layout);
                Assert.IsNotNull(track.transform.Find("Scenery"), track.Layout.Name);
                Assert.Greater(track.GetComponentsInChildren<MeshRenderer>().Length, 6, $"{track.Layout.Name} has a road, verges, barriers, ground and scenery");
                Assert.AreEqual(track.Geometry.PickupSpots.Count, track.PickupSpots.Count);
                Assert.Greater(track.Bounds.size.x, 100f);
                Assert.AreEqual(track.Path.Samples.Count, track.MinimapOutline.Count);
                Object.DestroyImmediate(track.gameObject);
            }
        }

        [Test]
        public void TheBarrierCollidersCloseTheRoadInOnTheWallLayer()
        {
            for (int i = 0; i < TrackLayouts.Count; i++)
            {
                Track track = TrackBuilder.Build(TrackLayouts.Of(i), null, null, root.transform);
                BoxCollider[] walls = track.GetComponentsInChildren<BoxCollider>();
                Assert.Greater(walls.Length, 20, track.Layout.Name);
                foreach (BoxCollider wall in walls)
                {
                    Assert.AreEqual(DeathRaceLayers.Wall, wall.gameObject.layer, track.Layout.Name);
                    Assert.IsTrue(wall.gameObject.isStatic, track.Layout.Name);
                    Assert.IsNull(wall.attachedRigidbody, track.Layout.Name);
                    Assert.Less(wall.sharedMaterial.dynamicFriction, 0.2f, "cars slide along the barriers");
                    float y = wall.transform.position.y + wall.center.y;
                    Assert.LessOrEqual(y - wall.size.y * 0.5f, -0.5f, "the walls reach below the road");
                    Assert.GreaterOrEqual(y + wall.size.y * 0.5f, 2.5f, "and well above any car");
                }
                RacePath path = track.Path;
                for (int s = 0; s < path.Count; s++)
                {
                    float distance = s * path.Spacing;
                    Vector2 centre = path.Samples[s];
                    Vector2 normal = path.NormalAt(distance);
                    float face = track.BarrierOffset(distance);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 inWall = RacePath.ToWorld(centre + normal * (side * (face + 0.25f)), 0.5f);
                        Assert.IsTrue(Inside(walls, inWall), $"{track.Layout.Name}: a gap in the barrier at {distance:0} m (side {side})");
                        Vector3 onVerge = RacePath.ToWorld(centre + normal * (side * (face - 0.3f)), 0.5f);
                        Assert.IsFalse(Inside(walls, onVerge), $"{track.Layout.Name}: a wall on the verge at {distance:0} m (side {side})");
                    }
                }
                Object.DestroyImmediate(track.gameObject);
            }
        }

        [Test]
        public void ARestyleKeepsTheCollidersAndReplacesTheScenery()
        {
            Track track = TrackBuilder.Build(TrackLayouts.Of(3), null, null, root.transform);
            int walls = track.GetComponentsInChildren<BoxCollider>().Length;
            Transform before = track.transform.Find("Scenery");
            track.Restyle(night);
            Transform after = track.transform.Find("Scenery");
            Assert.IsNotNull(after);
            Assert.AreNotSame(before, after);
            Assert.IsTrue(before == null, "the old scenery is gone");
            Assert.AreEqual(walls, track.GetComponentsInChildren<BoxCollider>().Length);
            Assert.AreSame(night, track.Theme);
        }

        [Test]
        public void ANightTrackHasLampsButNoMoreThanEightLights()
        {
            foreach (int index in new[] { 3, 8 })
            {
                Track track = TrackBuilder.Build(TrackLayouts.Of(index), night, null, root.transform);
                int lights = track.GetComponentsInChildren<Light>().Length;
                Assert.Greater(lights, 0, track.Layout.Name);
                Assert.LessOrEqual(lights, TrackBuilder.MaxLights, track.Layout.Name);
            }
        }

        [Test]
        public void TheSameLayoutAlwaysGrowsTheSameScenery()
        {
            Track first = TrackBuilder.Build(TrackLayouts.Of(0), null, null, root.transform);
            Track second = TrackBuilder.Build(TrackLayouts.Of(0), null, null, root.transform);
            Assert.AreEqual(Vertices(first), Vertices(second));
        }

        private static int Vertices(Track track)
        {
            int count = 0;
            foreach (MeshFilter filter in track.GetComponentsInChildren<MeshFilter>())
            {
                count += filter.sharedMesh.vertexCount;
            }
            return count;
        }

        private static bool Inside(IEnumerable<BoxCollider> walls, Vector3 point)
        {
            foreach (BoxCollider wall in walls)
            {
                Vector3 local = wall.transform.InverseTransformPoint(point) - wall.center;
                Vector3 half = wall.size * 0.5f;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
