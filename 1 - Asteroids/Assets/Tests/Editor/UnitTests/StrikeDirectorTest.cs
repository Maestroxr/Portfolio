using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Asteroids.Tests
{
    public class StrikeDirectorTest
    {
        /// <summary>A spawn the director asked for.</summary>
        private struct Spawned
        {
            public StrikeUnit Unit;
            public StrikeEvent Event;
            public int Member;
            public Vector2 Position;
            public float Delay;
            public float Distance;
        }


        /// <summary>Records what the director asks for and hands back stand-ins; <see cref="Director"/> reads the distance.</summary>
        private class FakeSpawner : IStrikeSpawner
        {
            public ScrollDirector Director;
            public StrikeAircraft Aircraft;
            public GroundUnit Ground;
            public Boss BossToReturn;
            public readonly List<Spawned> Air = new List<Spawned>();
            public readonly List<Spawned> OnGround = new List<Spawned>();
            public int Bosses;
            public float BossDistance;

            public int HostilesAlive => Air.Count + OnGround.Count;

            public StrikeAircraft SpawnAircraft(StrikeUnit unit, StrikeEvent spawn, int member, Vector2 start, float delay)
            {
                Air.Add(new Spawned { Unit = unit, Event = spawn, Member = member, Position = start, Delay = delay, Distance = Director.Distance });
                return Aircraft;
            }

            public GroundUnit SpawnGround(StrikeUnit unit, StrikeEvent spawn, int member, Vector2 position)
            {
                OnGround.Add(new Spawned { Unit = unit, Event = spawn, Member = member, Position = position, Distance = Director.Distance });
                return Ground;
            }

            public Boss SpawnBoss(Boss prefab)
            {
                Bosses++;
                BossDistance = Director.Distance;
                return BossToReturn;
            }
        }


        private readonly TemporaryObjects objects = new TemporaryObjects();
        private FakeSpawner spawner;

        [SetUp]
        public void SetUp()
        {
            spawner = new FakeSpawner
            {
                Aircraft = objects.Component<StrikeAircraft>("Aircraft"),
                Ground = objects.Component<GroundUnit>("Ground")
            };
        }

        [TearDown]
        public void TearDown()
        {
            objects.Dispose();
        }

        private StrikeLevel Level(float bossAt, params StrikeEvent[] events)
        {
            StrikeLevel level = objects.Asset<StrikeLevel>();
            level.bossAt = bossAt;
            level.scrollSpeed = StrikeRules.ScrollSpeed;
            level.events = events;
            return level;
        }

        private static StrikeEvent Air(float at, int count = 1, Formation formation = Formation.Single, StrikeDifficulty from = StrikeDifficulty.Rookie)
        {
            return new StrikeEvent { at = at, unit = StrikeUnit.Dart, count = count, formation = formation, path = FlightPath.StraightDown, minDifficulty = from };
        }

        private static StrikeEvent Ground(float at, int count = 1, StrikeDifficulty from = StrikeDifficulty.Rookie)
        {
            return new StrikeEvent { at = at, unit = StrikeUnit.Turret, count = count, minDifficulty = from };
        }

        private ScrollDirector Director(StrikeLevel level, StrikeDifficulty difficulty = StrikeDifficulty.Veteran)
        {
            var director = new ScrollDirector(level, spawner, difficulty);
            spawner.Director = director;
            return director;
        }

        private static void Run(ScrollDirector director, float seconds, float step = 1f / 60f)
        {
            for (float t = 0f; t < seconds; t += step)
            {
                director.Tick(step);
            }
        }


        [Test]
        public void NothingHappensBeforeBegin()
        {
            ScrollDirector director = Director(Level(100f, Air(0f), Ground(0f)));
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Idle));
            director.Tick(10f);
            Assert.That(director.Distance, Is.Zero);
            Assert.That(spawner.HostilesAlive, Is.Zero);
        }


        [Test]
        public void EventsSpawnOnceEvenAcrossABigTick()
        {
            ScrollDirector director = Director(Level(300f, Air(10f), Ground(20f, 3), Air(30f, 2, Formation.Line), Ground(200f)));
            director.Begin();
            Assert.That(director.Speed, Is.EqualTo(StrikeRules.ScrollSpeed));
            director.Tick(50f);
            Assert.That(director.Distance, Is.EqualTo(120f).Within(1e-3f));
            Assert.That(spawner.Air.Count, Is.EqualTo(3));
            Assert.That(spawner.OnGround.Count, Is.EqualTo(3));
            Assert.That(director.HostilesSpawned, Is.EqualTo(6));
            Run(director, 10f);
            Assert.That(spawner.Air.Count, Is.EqualTo(3), "No event spawns twice.");
            Assert.That(spawner.OnGround.Count, Is.EqualTo(3));
            Assert.That(spawner.OnGround[2].Member, Is.EqualTo(2));
            director.Tick(40f);
            Assert.That(spawner.OnGround.Count, Is.EqualTo(4));
        }


        [Test]
        public void GroundUnitsSpawnAheadOnTheirLevelCoordinate()
        {
            StrikeEvent row = Ground(50f, 2);
            row.x = 4f;
            row.offset = new Vector2(3f, 1f);
            ScrollDirector director = Director(Level(300f, row));
            director.Begin();
            Run(director, 30f);
            Assert.That(spawner.OnGround.Count, Is.EqualTo(2));
            Spawned first = spawner.OnGround[0];
            float step = StrikeRules.ScrollSpeed / 60f;
            Assert.That(first.Distance + StrikeRules.SpawnLead, Is.GreaterThanOrEqualTo(50f), "Not before it is due.");
            Assert.That(first.Distance + StrikeRules.SpawnLead, Is.LessThan(50f + step + 1e-3f), "On the tick it came due.");
            float top = StrikeRules.HalfSize.y;
            Assert.That(first.Position.x, Is.EqualTo(4f));
            Assert.That(first.Position.y, Is.EqualTo(top + 50f - first.Distance).Within(1e-4f), "y = Top + at - Distance.");
            Assert.That(first.Position.y, Is.GreaterThan(top + StrikeRules.SpawnLead - step - 1e-3f), "Above the screen: no pop-in.");
            Spawned second = spawner.OnGround[1];
            Assert.That((second.Position - first.Position - new Vector2(3f, 1f)).magnitude, Is.LessThan(1e-4f));
        }


        [Test]
        public void AirUnitsStartAtTheirPathInFormation()
        {
            StrikeEvent line = Air(10f, 3, Formation.Line);
            line.x = 5f;
            line.spacing = 3f;
            StrikeEvent column = Air(12f, 3, Formation.Column);
            column.speed = 2f;
            StrikeEvent mirrored = Air(14f);
            mirrored.path = FlightPath.CrossLeft;
            mirrored.mirror = true;
            ScrollDirector director = Director(Level(300f, line, column, mirrored));
            director.Begin();
            Run(director, 7f);
            Assert.That(spawner.Air.Count, Is.EqualTo(7));
            Assert.That(spawner.Air[0].Distance, Is.GreaterThanOrEqualTo(10f), "Air units are due at their own at.");
            Vector2 start = FlightPaths.Waypoints(FlightPath.StraightDown, false)[0] + new Vector2(5f, 0f);
            Assert.That((spawner.Air[1].Position - start).magnitude, Is.LessThan(1e-4f), "The middle of a line is the reference point.");
            Assert.That(Mathf.Abs(spawner.Air[0].Position.x - spawner.Air[2].Position.x), Is.EqualTo(6f).Within(1e-4f));
            Assert.That(spawner.Air[0].Position.y, Is.EqualTo(start.y).Within(1e-4f));
            Assert.That(spawner.Air[0].Delay, Is.Zero);
            float speed = StrikeUnitRules.Info(StrikeUnit.Dart).Speed * 2f;
            for (int member = 0; member < 3; member++)
            {
                Assert.That(spawner.Air[3 + member].Delay, Is.EqualTo(member * 3f / speed).Within(1e-5f), "A column follows along the path.");
                Assert.That((spawner.Air[3 + member].Position - FlightPaths.Waypoints(FlightPath.StraightDown, false)[0]).magnitude, Is.LessThan(1e-4f));
            }
            Assert.That(spawner.Air[6].Position.x, Is.LessThan(-StrikeRules.HalfSize.x), "A mirrored CrossLeft enters from the left.");
        }


        [Test]
        public void EventsAboveTheDifficultyAreSkipped()
        {
            StrikeEvent[] events =
            {
                Air(5f), Ground(6f), Air(7f, 1, Formation.Single, StrikeDifficulty.Veteran), Ground(8f, 1, StrikeDifficulty.Veteran),
                Air(9f, 1, Formation.Single, StrikeDifficulty.Elite), Ground(10f, 2, StrikeDifficulty.Elite)
            };
            var counts = new List<int>();
            foreach (StrikeDifficulty difficulty in new[] { StrikeDifficulty.Rookie, StrikeDifficulty.Veteran, StrikeDifficulty.Elite })
            {
                SetUp();
                ScrollDirector director = Director(Level(100f, events), difficulty);
                director.Begin();
                Run(director, 10f);
                counts.Add(director.HostilesSpawned);
            }
            Assert.That(counts, Is.EqualTo(new[] { 2, 4, 7 }));
        }


        [Test]
        public void TheScrollEasesToRestExactlyAtTheBoss([Values(1f / 60f, 0.37f, 1f / 23.3f)] float step)
        {
            const float bossAt = 150f;
            ScrollDirector director = Director(Level(bossAt));
            director.Begin();
            float previous = director.Speed;
            double time = 0.0;
            double easeTime = 0.0;
            while (director.State != ScrollDirector.Stage.Boss && time < 200f)
            {
                float before = director.Distance;
                director.Tick(step);
                time += step;
                Assert.That(director.Distance, Is.LessThanOrEqualTo(bossAt), "Never past the boss.");
                Assert.That(director.Distance, Is.GreaterThanOrEqualTo(before));
                Assert.That(director.Speed, Is.LessThanOrEqualTo(previous + 1e-4f), "The scroll only slows down.");
                if (before < bossAt - StrikeRules.BossEaseDistance - StrikeRules.ScrollSpeed * step)
                {
                    Assert.That(director.Speed, Is.EqualTo(StrikeRules.ScrollSpeed), "Full speed before the ease.");
                }
                else
                {
                    easeTime += step;
                }
                previous = director.Speed;
            }
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Boss));
            Assert.That(director.Distance, Is.EqualTo(bossAt), "Exactly at the boss.");
            Assert.That(director.Speed, Is.Zero);
            Assert.That(director.Progress, Is.EqualTo(1f));
            // Constant deceleration over 3.6 m from 2.4 m/s takes 2 x 3.6 / 2.4 = 3 s.
            Assert.That(easeTime, Is.EqualTo(3f).Within(2f * step + 1e-3f));
            Assert.That(time, Is.EqualTo((bossAt - StrikeRules.BossEaseDistance) / StrikeRules.ScrollSpeed + 3f).Within(step + 1e-3f));
        }


        [Test]
        public void TheEaseHasConstantDeceleration()
        {
            ScrollDirector director = Director(Level(100f));
            director.Begin(100f - StrikeRules.BossEaseDistance);
            const float step = 0.5f;
            float deceleration = StrikeRules.ScrollSpeed * StrikeRules.ScrollSpeed / 7.2f;
            float previous = director.Speed;
            for (int i = 0; i < 5; i++)
            {
                director.Tick(step);
                Assert.That(previous - director.Speed, Is.EqualTo(deceleration * step).Within(1e-4f));
                previous = director.Speed;
            }
            director.Tick(step + 0.1f);
            Assert.That(director.Speed, Is.Zero);
            Assert.That(director.Distance, Is.EqualTo(100f));
        }


        [Test]
        public void AGroundBossRestsOnItsHoldLine([Values(1f / 60f, 0.25f)] float step)
        {
            const float bossAt = 200f;
            StrikeBoss boss = objects.Component<StrikeBoss>("Crawler");
            boss.groundBoss = true;
            boss.holdLine = 0.55f;
            boss.Radius = 4f;
            StrikeLevel level = Level(bossAt);
            level.boss = boss;
            spawner.BossToReturn = boss;
            ScrollDirector director = Director(level);
            Boss arrived = null;
            float arrivedAt = 0f;
            float arrivedY = 0f;
            director.BossArrived += b =>
            {
                arrived = b;
                arrivedAt = director.Distance;
                arrivedY = b.Position.y;
            };
            bool finished = false;
            director.Finished += () => finished = true;
            director.Begin();
            float holdY = 0.55f * StrikeRules.HalfSize.y;
            float spawnAt = bossAt - (StrikeRules.HalfSize.y + 4f + 1f - holdY);
            Assert.That(director.BossSpawnDistance, Is.EqualTo(spawnAt).Within(1e-4f));
            Run(director, 120f, step);
            Assert.That(spawner.Bosses, Is.EqualTo(1));
            Assert.That(arrived, Is.SameAs(boss));
            Assert.That(arrivedAt, Is.GreaterThanOrEqualTo(spawnAt));
            Assert.That(arrivedAt - spawnAt, Is.LessThan(StrikeRules.ScrollSpeed * step + 1e-3f), "It spawns on the tick it came due.");
            // Riding the ground from where it was placed, it comes to rest on the hold line when the scroll stops.
            float restY = arrivedY - (bossAt - arrivedAt);
            Assert.That(restY, Is.EqualTo(holdY).Within(1e-3f));
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Boss));
            Assert.That(director.Boss, Is.SameAs(boss));
            director.BossDefeated();
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Done));
            Assert.IsTrue(finished);
            director.Tick(1f);
            Assert.That(spawner.Bosses, Is.EqualTo(1));
        }


        [Test]
        public void AnAirBossArrivesWhenTheEaseBegins()
        {
            const float bossAt = 120f;
            StrikeBoss boss = objects.Component<StrikeBoss>("Rotor");
            StrikeLevel level = Level(bossAt);
            level.boss = boss;
            spawner.BossToReturn = boss;
            ScrollDirector director = Director(level);
            director.Begin();
            Run(director, (bossAt - 4f) / StrikeRules.ScrollSpeed);
            Assert.That(spawner.Bosses, Is.Zero);
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Flying));
            Run(director, 0.5f);
            Assert.That(spawner.Bosses, Is.EqualTo(1));
            Assert.That(spawner.BossDistance, Is.GreaterThanOrEqualTo(bossAt - StrikeRules.BossEaseDistance));
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.BossApproach));
            Run(director, 5f);
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Boss));
        }


        [Test]
        public void BeginAtADistanceSkipsTheEventsBeforeIt()
        {
            ScrollDirector director = Director(Level(300f, Air(10f), Ground(50f), Ground(56f), Air(60f), Ground(100f), Air(100f)));
            director.Begin(55f);
            Assert.That(director.Distance, Is.EqualTo(55f));
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Flying));
            Run(director, 25f);
            Assert.That(spawner.OnGround.Count, Is.EqualTo(2));
            Assert.That(spawner.OnGround[0].Event.at, Is.EqualTo(56f), "A ground unit just above the screen still comes.");
            Assert.That(spawner.Air.Count, Is.EqualTo(2));
            Assert.That(spawner.Air[0].Event.at, Is.EqualTo(60f));
        }


        [Test]
        public void SkipToBossJumpsOverTheLevel()
        {
            const float bossAt = 250f;
            StrikeBoss boss = objects.Component<StrikeBoss>("Crawler");
            boss.groundBoss = true;
            boss.Radius = 3f;
            StrikeLevel level = Level(bossAt, Air(10f), Ground(100f), Ground(240f));
            level.boss = boss;
            spawner.BossToReturn = boss;
            ScrollDirector director = Director(level);
            director.Begin();
            Run(director, 5f);
            Assert.That(spawner.Air.Count, Is.EqualTo(1));
            director.SkipToBoss();
            float expected = Mathf.Min(director.BossSpawnDistance, director.EaseStart) - ScrollDirector.SkipMargin;
            Assert.That(director.Distance, Is.EqualTo(expected).Within(1e-4f));
            Assert.That(spawner.Bosses, Is.Zero, "The boss comes on its own after the jump.");
            Run(director, 20f);
            Assert.That(spawner.Air.Count, Is.EqualTo(1), "Nothing new in the air.");
            Assert.That(spawner.OnGround.Count, Is.EqualTo(1), "The event at 100 was jumped over, the one at 240 came.");
            Assert.That(spawner.OnGround[0].Event.at, Is.EqualTo(240f));
            Assert.That(spawner.Bosses, Is.EqualTo(1));
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Boss));
            Assert.That(director.Distance, Is.EqualTo(bossAt));

            SetUp();
            ScrollDirector idle = Director(Level(100f));
            idle.SkipToBoss();
            Assert.That(idle.State, Is.EqualTo(ScrollDirector.Stage.Flying), "Skipping starts an idle scroll.");
            Assert.That(idle.Distance, Is.EqualTo(100f - StrikeRules.BossEaseDistance - ScrollDirector.SkipMargin).Within(1e-4f));
        }


        [Test]
        public void StopEndsEverything()
        {
            ScrollDirector director = Director(Level(100f, Air(50f)));
            director.Begin();
            director.Stop();
            Run(director, 60f);
            Assert.That(director.State, Is.EqualTo(ScrollDirector.Stage.Done));
            Assert.That(director.Speed, Is.Zero);
            Assert.That(spawner.Air.Count, Is.Zero);
        }


        [Test]
        public void FormationsSpreadAroundTheReferencePoint()
        {
            Vector2 ahead = Vector2.down;
            var side = new Vector2(1f, 0f);
            ScrollDirector.FormationSlot(Formation.Vee, 0, 5, 2f, ahead, side, out Vector2 leader, out float leaderTrail);
            ScrollDirector.FormationSlot(Formation.Vee, 1, 5, 2f, ahead, side, out Vector2 left, out _);
            ScrollDirector.FormationSlot(Formation.Vee, 2, 5, 2f, ahead, side, out Vector2 right, out _);
            Assert.That(leader.magnitude, Is.Zero);
            Assert.That(leaderTrail, Is.Zero);
            Assert.That(left.x, Is.EqualTo(-right.x));
            Assert.That(left.y, Is.GreaterThan(0f), "The wings trail behind (above, for a unit flying down).");
            ScrollDirector.FormationSlot(Formation.Pair, 3, 4, 2f, ahead, side, out Vector2 pair, out float pairTrail);
            Assert.That(pair.x, Is.EqualTo(1f));
            Assert.That(pairTrail, Is.EqualTo(2f));
            ScrollDirector.FormationSlot(Formation.Pair, 2, 3, 2f, ahead, side, out Vector2 alone, out _);
            Assert.That(alone.magnitude, Is.Zero, "An odd last member flies in the middle.");
        }
    }
}
