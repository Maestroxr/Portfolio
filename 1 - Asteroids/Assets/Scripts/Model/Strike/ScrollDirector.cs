using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>What the <see cref="ScrollDirector"/> asks of the spawner (the scene's <see cref="SpawnService"/>).</summary>
    public interface IStrikeSpawner
    {
        /// <summary>
        /// Member <paramref name="member"/> of an air event, starting at <paramref name="start"/> (screen space) after
        /// <paramref name="delay"/> seconds. Null when its pool is empty.
        /// </summary>
        StrikeAircraft SpawnAircraft(StrikeUnit unit, StrikeEvent spawn, int member, Vector2 start, float delay);

        /// <summary>Member <paramref name="member"/> of a ground event at <paramref name="position"/>. Null when its pool is empty.</summary>
        GroundUnit SpawnGround(StrikeUnit unit, StrikeEvent spawn, int member, Vector2 position);

        /// <summary>The boss of the level (the spawner attaches its parts and scales its health).</summary>
        Boss SpawnBoss(Boss prefab);

        /// <summary>Strike aircraft and ground units in play.</summary>
        int HostilesAlive { get; }
    }


    /// <summary>
    /// Runs a strike level: scrolls the ground at the level's speed, spawns the events as their reference points come up
    /// (ground units <see cref="StrikeRules.SpawnLead"/> m early, so they never pop in; events above the pilot's
    /// difficulty are skipped), eases the scroll to rest exactly at the boss distance and brings in the boss. Plain C#
    /// and deterministic (no randomness), so it can be tested on its own and every client of a shared mission agrees.
    /// </summary>
    public class ScrollDirector
    {
        public enum Stage
        {
            Idle,
            Flying,
            /// <summary>The boss has spawned; the scroll eases to rest.</summary>
            BossApproach,
            /// <summary>The scroll is at rest and the boss fights.</summary>
            Boss,
            Done
        }

        /// <summary>How far before the boss (or the ease, whichever comes first) <see cref="SkipToBoss"/> lands (m).</summary>
        public const float SkipMargin = 2f;

        /// <summary>How the members of a Vee trail behind their leader, per meter of spacing to the side.</summary>
        private const float VeeTrail = 0.75f;

        private readonly StrikeLevel level;
        private readonly IStrikeSpawner spawner;
        private readonly StrikeDifficulty difficulty;
        private readonly StrikeEvent[] events;
        private readonly int[] order;
        private readonly bool[] done;
        private int cursor;
        private bool bossSpawned;
        private float bossSpawnedAt;

        /// <summary>Scroll distance so far (m): screen y = Top + s - Distance for a point at level coordinate s.</summary>
        public float Distance { get; private set; }

        /// <summary>Current scroll speed (m/s).</summary>
        public float Speed { get; private set; }

        public Stage State { get; private set; } = Stage.Idle;

        /// <summary>How far the level got, 0 to 1 (1 when the scroll rests for the boss).</summary>
        public float Progress => level != null && level.BossAt > 0f ? Mathf.Clamp01(Distance / level.BossAt) : 0f;

        /// <summary>Strike aircraft and ground units spawned so far.</summary>
        public int HostilesSpawned { get; private set; }

        /// <summary>The y of the playfield's top edge (the playground's <see cref="Playground.Top"/>).</summary>
        public float Top { get; set; } = StrikeRules.HalfSize.y;

        public StrikeLevel Level => level;

        public StrikeDifficulty Difficulty => difficulty;

        /// <summary>The boss once it has spawned.</summary>
        public Boss Boss { get; private set; }

        /// <summary>The distance at which the scroll starts to ease to rest (<see cref="StrikeRules.BossEaseDistance"/> before the boss).</summary>
        public float EaseStart => BossAt - StrikeRules.BossEaseDistance;

        /// <summary>
        /// The distance at which the boss spawns: a ground boss early enough to ride the ground down from just above the
        /// top edge to its hold line exactly when the scroll stops; an air boss when the ease begins.
        /// </summary>
        public float BossSpawnDistance
        {
            get
            {
                StrikeBoss boss = level != null ? level.StrikeBossPrefab : null;
                if (boss == null || !boss.IsGroundBoss)
                {
                    return EaseStart;
                }
                return BossAt - (GroundBossStartY(boss) - HoldY(boss));
            }
        }

        /// <summary>The boss spawned (stage BossApproach).</summary>
        public event Action<Boss> BossArrived;

        /// <summary>The boss was defeated (stage Done).</summary>
        public event Action Finished;

        private float BossAt => level != null ? Mathf.Max(0f, level.BossAt) : 0f;

        private float Middle => Top - StrikeRules.HalfSize.y;


        public ScrollDirector(StrikeLevel level, IStrikeSpawner spawner, StrikeDifficulty difficulty)
        {
            this.level = level;
            this.spawner = spawner;
            this.difficulty = difficulty;
            events = level != null ? level.Events : new StrikeEvent[0];
            order = new int[events.Length];
            done = new bool[events.Length];
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }
            // A stable sort by at, so a level that is not quite sorted still spawns everything in order.
            Array.Sort(order, (a, b) => At(a) != At(b) ? At(a).CompareTo(At(b)) : a.CompareTo(b));
        }


        /// <summary>The y at which a hold line of a ground boss lies (its <see cref="StrikeBoss.HoldLine"/> of the half height above the middle).</summary>
        public float HoldY(StrikeBoss boss)
        {
            return Middle + (boss != null ? boss.HoldLine : 0f) * StrikeRules.HalfSize.y;
        }


        /// <summary>The y at which a ground boss appears: just above the top edge.</summary>
        public float GroundBossStartY(StrikeBoss boss)
        {
            return Top + (boss != null ? boss.Radius : 0f) + 1f;
        }


        /// <summary>Starts scrolling at <paramref name="distance"/>; events before it are skipped.</summary>
        public void Begin(float distance = 0f)
        {
            Distance = Mathf.Clamp(distance, 0f, BossAt);
            SkipBefore(Distance);
            State = Stage.Flying;
            Speed = Distance < BossAt && level != null ? Mathf.Max(0f, level.ScrollSpeed) : 0f;
        }


        /// <summary>Advances the scroll by <paramref name="deltaTime"/> seconds and spawns what came due.</summary>
        public void Tick(float deltaTime)
        {
            if (State == Stage.Idle || State == Stage.Done || level == null)
            {
                return;
            }
            Advance(Mathf.Max(0f, deltaTime));
            SpawnDue();
            if (!bossSpawned && Distance >= BossSpawnDistance)
            {
                SpawnTheBoss();
            }
            if (State == Stage.BossApproach && Distance >= BossAt && Speed <= 0f)
            {
                State = Stage.Boss;
            }
        }


        /// <summary>The boss is down: stage Done and <see cref="Finished"/>.</summary>
        public void BossDefeated()
        {
            if (State == Stage.Done)
            {
                return;
            }
            State = Stage.Done;
            Speed = 0f;
            Finished?.Invoke();
        }


        /// <summary>Stops everything (leaving the mission); nothing spawns any more.</summary>
        public void Stop()
        {
            State = Stage.Done;
            Speed = 0f;
        }


        /// <summary>
        /// Tours and tests: jumps to <see cref="SkipMargin"/> m before the boss spawns or the ease begins, whichever is
        /// first (the events on the way are skipped; nothing happens once the boss is in). Starts the scroll when idle.
        /// </summary>
        public void SkipToBoss()
        {
            if (State == Stage.Done || bossSpawned || level == null)
            {
                return;
            }
            float target = Mathf.Max(0f, Mathf.Min(BossSpawnDistance, EaseStart) - SkipMargin);
            if (State == Stage.Idle)
            {
                Begin(Mathf.Max(Distance, target));
                return;
            }
            if (target > Distance)
            {
                Distance = target;
                SkipBefore(target);
            }
        }


        /// <summary>
        /// Moves the scroll on: at the level's speed until <see cref="EaseStart"/>, then with the constant deceleration
        /// speed^2 / (2 x 3.6) that brings it to rest exactly at the boss distance.
        /// </summary>
        private void Advance(float deltaTime)
        {
            float bossAt = BossAt;
            float cruise = Mathf.Max(0f, level.ScrollSpeed);
            if (Distance >= bossAt || cruise <= 0f)
            {
                Distance = Mathf.Min(Distance, bossAt);
                Speed = 0f;
                return;
            }
            float left = deltaTime;
            float easeStart = EaseStart;
            if (Distance < easeStart)
            {
                float cruiseTime = (easeStart - Distance) / cruise;
                if (left < cruiseTime)
                {
                    Distance += cruise * left;
                    Speed = cruise;
                    return;
                }
                Distance = easeStart;
                left -= cruiseTime;
            }
            float deceleration = cruise * cruise / (2f * StrikeRules.BossEaseDistance);
            // The speed follows from the distance left, so rounding never carries the scroll past the boss.
            float speed = Mathf.Sqrt(2f * deceleration * Mathf.Max(0f, bossAt - Distance));
            float stopTime = speed / deceleration;
            if (left >= stopTime)
            {
                Distance = bossAt;
                Speed = 0f;
                return;
            }
            Distance = Mathf.Min(bossAt, Distance + speed * left - 0.5f * deceleration * left * left);
            Speed = speed - deceleration * left;
        }


        /// <summary>Spawns every event that came due (each once, however far the tick went).</summary>
        private void SpawnDue()
        {
            while (cursor < order.Length && done[order[cursor]])
            {
                cursor++;
            }
            float reach = Distance + StrikeRules.SpawnLead;
            for (int k = cursor; k < order.Length; k++)
            {
                int index = order[k];
                if (done[index])
                {
                    continue;
                }
                StrikeEvent spawn = events[index];
                if (spawn == null)
                {
                    done[index] = true;
                    continue;
                }
                if (spawn.at > reach)
                {
                    break;
                }
                if (spawn.IsAir && spawn.at > Distance)
                {
                    continue;
                }
                done[index] = true;
                if (spawn.minDifficulty <= difficulty)
                {
                    Spawn(spawn);
                }
            }
        }


        /// <summary>Puts the members of <paramref name="spawn"/> into play.</summary>
        private void Spawn(StrikeEvent spawn)
        {
            if (spawner == null)
            {
                return;
            }
            int count = Mathf.Max(0, spawn.count);
            if (!spawn.IsAir)
            {
                var reference = new Vector2(spawn.x, Top + spawn.at - Distance);
                for (int member = 0; member < count; member++)
                {
                    if (spawner.SpawnGround(spawn.unit, spawn, member, reference + member * spawn.offset) != null)
                    {
                        HostilesSpawned++;
                    }
                }
                return;
            }
            Vector2[] waypoints = FlightPaths.Waypoints(spawn.path, spawn.mirror);
            Vector2 start = AirStart(spawn, waypoints, Middle);
            Vector2 ahead = waypoints.Length > 1 ? waypoints[1] - waypoints[0] : Vector2.down;
            ahead = ahead.sqrMagnitude > 1e-6f ? ahead.normalized : Vector2.down;
            var side = new Vector2(-ahead.y, ahead.x);
            float speed = Mathf.Max(0.1f, StrikeUnitRules.Info(spawn.unit).Speed * Mathf.Max(0.05f, spawn.speed));
            for (int member = 0; member < count; member++)
            {
                FormationSlot(spawn.formation, member, count, Mathf.Max(0f, spawn.spacing), ahead, side, out Vector2 offset, out float trail);
                if (spawner.SpawnAircraft(spawn.unit, spawn, member, start + offset, trail / speed) != null)
                {
                    HostilesSpawned++;
                }
            }
        }


        /// <summary>
        /// Where the leader of the air event <paramref name="spawn"/> starts: the first of its path's
        /// <paramref name="waypoints"/> moved across by the event's x and up to <paramref name="middle"/> (the height of the
        /// middle of the screen). A path that comes in from a side is never moved in toward the screen, so it still starts
        /// fully off screen: an x toward the screen is dropped (the whole path keeps its place), an x further out is kept.
        /// </summary>
        public static Vector2 AirStart(StrikeEvent spawn, Vector2[] waypoints, float middle)
        {
            Vector2 first = waypoints != null && waypoints.Length > 0 ? waypoints[0] : Vector2.zero;
            float across = spawn != null ? spawn.x : 0f;
            bool fromSide = Mathf.Abs(first.x) >= StrikeRules.HalfSize.x;
            if (fromSide && across * Mathf.Sign(first.x) < 0f)
            {
                across = 0f;
            }
            return first + new Vector2(across, middle);
        }


        /// <summary>
        /// Where member <paramref name="member"/> of a formation starts relative to the path's start, and how many meters
        /// behind the leader it flies along the path (a delay): Single and Column one after another, Line side by side, Vee
        /// alternating left and right and trailing back, Pair two abreast in a column of pairs.
        /// </summary>
        public static void FormationSlot(Formation formation, int member, int count, float spacing, Vector2 ahead, Vector2 side,
            out Vector2 offset, out float trail)
        {
            offset = Vector2.zero;
            trail = 0f;
            switch (formation)
            {
                case Formation.Line:
                    offset = side * ((member - (count - 1) * 0.5f) * spacing);
                    break;
                case Formation.Vee:
                {
                    int rank = (member + 1) / 2;
                    float hand = member == 0 ? 0f : member % 2 == 1 ? -1f : 1f;
                    offset = side * (hand * rank * spacing) - ahead * (rank * spacing * VeeTrail);
                    break;
                }
                case Formation.Pair:
                {
                    int row = member / 2;
                    bool alone = member % 2 == 0 && member == count - 1;
                    float hand = alone ? 0f : member % 2 == 0 ? -0.5f : 0.5f;
                    offset = side * (hand * spacing);
                    trail = row * spacing;
                    break;
                }
                default:
                    trail = member * spacing;
                    break;
            }
        }


        private void SpawnTheBoss()
        {
            bossSpawned = true;
            bossSpawnedAt = BossSpawnDistance;
            State = Stage.BossApproach;
            Boss prefab = level.BossPrefab;
            Boss boss = prefab != null && spawner != null ? spawner.SpawnBoss(prefab) : null;
            if (boss == null)
            {
                return;
            }
            if (boss is StrikeBoss strike && strike.IsGroundBoss)
            {
                // Ground-relative and parked: it starts just above the top edge (less what the scroll went past its spawn
                // distance in this tick) and rides the ground down to its hold line.
                strike.Position = new Vector2(strike.Position.x, GroundBossStartY(strike) - (Distance - bossSpawnedAt));
                strike.Velocity = Vector2.zero;
            }
            Boss = boss;
            BossArrived?.Invoke(boss);
        }


        /// <summary>Marks every event before <paramref name="distance"/> as done without spawning it.</summary>
        private void SkipBefore(float distance)
        {
            for (int i = 0; i < events.Length; i++)
            {
                if (events[i] == null || events[i].at < distance)
                {
                    done[i] = true;
                }
            }
        }


        private float At(int index)
        {
            return events[index] != null ? events[index].at : float.MinValue;
        }
    }
}
