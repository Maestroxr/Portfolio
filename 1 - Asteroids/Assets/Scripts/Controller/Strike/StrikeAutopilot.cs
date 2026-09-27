using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Flies a strike mission by itself: samples about 35 points in the lower 60% of the playfield, scores their danger
    /// (enemy shots, air units and beams projected about a second ahead), their aim (the x distance to the best target
    /// above) and the pickups, and moves toward the best one. Always fires, cycles to a ground weapon when ground targets
    /// dominate, and sets off a megabomb when the danger is high. Disabled on the ship prefab and on the stand-ins of
    /// other pilots; tests, tours and the online tour enable it.
    ///
    /// The danger of a point is found by flying there in a straight line at about top speed and comparing where the ship
    /// would be with where every threat would be at a few moments within <see cref="lookAhead"/>; closer calls and sooner
    /// ones weigh more, and a hit's damage scales it. Everything is deterministic (no randomness), so a tour replays alike.
    /// </summary>
    public class StrikeAutopilot : MonoBehaviour, IShipInput
    {
        [Tooltip("Points sampled per frame.")]
        [SerializeField] internal int samples = 35;
        [Tooltip("Share of the playfield's height, from the bottom, the ship keeps to.")]
        [SerializeField, Range(0.2f, 1f)] internal float lowerShare = 0.6f;
        [Tooltip("Seconds threats are projected ahead.")]
        [SerializeField] internal float lookAhead = 1f;
        [Tooltip("Danger above which a megabomb is used.")]
        [SerializeField] internal float megabombDanger = 6f;
        [Tooltip("Room (m) kept around the ship's hit circle when dodging.")]
        [SerializeField] internal float safetyMargin = 0.9f;
        [Tooltip("Weight of lining up under a target.")]
        [SerializeField] internal float aimWeight = 1.2f;
        [Tooltip("Weight of reaching a pickup.")]
        [SerializeField] internal float pickupWeight = 3f;
        [Tooltip("Cost per meter of travel (keeps the ship from wandering).")]
        [SerializeField] internal float travelCost = 0.03f;
        [Tooltip("Height above the bottom edge the ship likes to fly at (m).")]
        [SerializeField] internal float homeHeight = 4f;
        [Tooltip("Enemy shots within 5 m that count as a crowd worth a megabomb.")]
        [SerializeField] internal int crowdShots = 7;

        /// <summary>Moments (share of the look-ahead) at which threats are compared with the ship.</summary>
        private static readonly float[] Moments = { 0f, 0.15f, 0.3f, 0.5f, 0.75f, 1f };

        private readonly List<Vector2> points = new List<Vector2>(64);
        private AsteroidsPlayer ship;
        private Vector2 goal;
        private bool hasGoal;
        private float cycleWait;

        public float Turn => 0f;
        public float Thrust => 0f;
        public bool Brake => false;
        public bool Fire { get; private set; }
        public bool DashPressed => false;
        public bool BombPressed { get; private set; }
        public Vector2 Move { get; private set; }
        public bool CyclePressed { get; private set; }

        /// <summary>The point the autopilot flies to (for tours and debugging).</summary>
        public Vector2 Goal => goal;

        /// <summary>The danger of the point chosen in the last frame.</summary>
        public float GoalDanger { get; private set; }

        /// <summary>The danger of staying where the ship is, in the last frame.</summary>
        public float HereDanger { get; private set; }


        private void OnEnable()
        {
            ship = GetComponent<AsteroidsPlayer>();
            if (ship != null)
            {
                ship.Input = this;
            }
            hasGoal = false;
        }


        private void OnDisable()
        {
            if (ship != null && ReferenceEquals(ship.Input, this))
            {
                ship.Input = null;
            }
        }


        public void Read(AsteroidsPlayer player, float deltaTime)
        {
            Move = Vector2.zero;
            Fire = true;
            CyclePressed = false;
            BombPressed = false;
            SpaceField field = player != null ? player.Field : null;
            if (field == null || field.Playground == null)
            {
                return;
            }
            Playground playground = field.Playground;
            Vector2 position = player.Position;
            float speed = StrikeRules.ShipMaxSpeed(player.PlayerSettings) * 0.8f;
            float radius = player.Radius;

            SamplePoints(playground, position, radius);
            AddPickupPoints(field, playground, radius);

            float bestScore = float.MinValue;
            Vector2 best = position;
            float bestDanger = 0f;
            HereDanger = Danger(field, position, position, speed, radius);
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];
                float danger = i == 0 ? HereDanger : Danger(field, position, point, speed, radius);
                float travel = Vector2.Distance(position, point);
                float score = -danger * 10f
                    + Aim(field, point, travel / speed) * aimWeight
                    + Pickups(field, point, travel / speed, radius) * pickupWeight
                    - travel * travelCost
                    - Mathf.Abs(point.y - (playground.Bottom + homeHeight)) * 0.04f
                    - EdgeCost(playground, point);
                if (hasGoal && (point - goal).sqrMagnitude < 1f)
                {
                    score += 0.3f;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = point;
                    bestDanger = danger;
                }
            }
            goal = best;
            hasGoal = true;
            GoalDanger = bestDanger;

            Vector2 offset = best - position;
            Move = new Vector2(Mathf.Clamp(offset.x / 1.2f, -1f, 1f), Mathf.Clamp(offset.y / 1.2f, -1f, 1f));
            if (offset.sqrMagnitude < 0.04f)
            {
                Move = Vector2.zero;
            }

            StrikeGunnery gunnery = player.Strike;
            if (gunnery != null)
            {
                BombPressed = WantsMegabomb(field, player, gunnery, bestDanger);
                cycleWait = Mathf.Max(0f, cycleWait - deltaTime);
                if (cycleWait <= 0f && WantsCycle(field, gunnery.Loadout))
                {
                    CyclePressed = true;
                    cycleWait = 0.4f;
                }
            }
        }


        // ------------------------------------------------------------------ candidates

        /// <summary>The ship's own spot, a ring around it and a grid over the lower part of the playfield.</summary>
        private void SamplePoints(Playground playground, Vector2 position, float radius)
        {
            points.Clear();
            points.Add(position);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                points.Add(Clamp(playground, position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.2f, radius));
            }
            int count = Mathf.Max(4, samples);
            int columns = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(count * 1.6f)));
            int rows = Mathf.Max(2, Mathf.CeilToInt(count / (float)columns));
            Vector2 half = playground.HalfSize;
            float left = playground.Middle.x - half.x + radius + 0.5f;
            float right = playground.Middle.x + half.x - radius - 0.5f;
            float bottom = playground.Bottom + radius + 0.5f;
            float top = playground.Bottom + half.y * 2f * lowerShare;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    float x = Mathf.Lerp(left, right, (c + 0.5f) / columns);
                    float y = Mathf.Lerp(bottom, top, (r + 0.5f) / rows);
                    points.Add(new Vector2(x, y));
                }
            }
        }


        /// <summary>Where the pickups will be when the ship could get there (those that come into the lower part).</summary>
        private void AddPickupPoints(SpaceField field, Playground playground, float radius)
        {
            IReadOnlyList<Reward> rewards = field.Rewards;
            float top = playground.Bottom + playground.HalfSize.y * 2f * lowerShare;
            for (int i = 0; i < rewards.Count && points.Count < 60; i++)
            {
                Reward reward = rewards[i];
                if (reward == null || !reward.InPlay)
                {
                    continue;
                }
                Vector2 at = reward.Position + field.ScrollVelocity * 0.6f;
                if (at.y < top && playground.IsInside(at, 0.5f))
                {
                    points.Add(Clamp(playground, at, radius));
                }
            }
        }


        private static Vector2 Clamp(Playground playground, Vector2 point, float radius)
        {
            return playground.Clamp(point, radius + 0.1f);
        }


        private static float EdgeCost(Playground playground, Vector2 point)
        {
            float fromSide = playground.HalfSize.x - Mathf.Abs(point.x - playground.Middle.x);
            return fromSide < 3f ? (3f - fromSide) * 0.15f : 0f;
        }


        // ------------------------------------------------------------------ danger

        /// <summary>
        /// The danger of flying from <paramref name="from"/> straight to <paramref name="to"/> at <paramref name="speed"/>:
        /// every enemy shot, beam and air unit compared with the ship at a few moments of the look-ahead.
        /// </summary>
        private float Danger(SpaceField field, Vector2 from, Vector2 to, float speed, float radius)
        {
            Vector2 path = to - from;
            float length = path.magnitude;
            Vector2 direction = length > 0.001f ? path / length : Vector2.zero;
            float danger = 0f;

            IReadOnlyList<Shot> shots = field.EnemyShots;
            for (int s = 0; s < shots.Count; s++)
            {
                Shot shot = shots[s];
                if (shot == null || !shot.InPlay)
                {
                    continue;
                }
                float weight = Mathf.Clamp(shot.Damage / 3f, 0.5f, 6f);
                if (shot is EnemyBeam beam)
                {
                    danger += ColumnRisk(from, direction, length, speed, beam.Position, beam.halfWidth + radius + safetyMargin) * 8f;
                    continue;
                }
                danger += Risk(from, direction, length, speed, shot.Position, shot.Velocity, shot.Radius + radius + safetyMargin) * weight;
            }

            IReadOnlyList<Shootable> targets = field.Targets;
            for (int t = 0; t < targets.Count; t++)
            {
                Shootable target = targets[t];
                if (target == null || !target.IsAlive)
                {
                    continue;
                }
                if (target is GroundUnit ground)
                {
                    // A laser tower lighting up is about to burn the column under it.
                    if (ground.telegraph != null && ground.telegraph.activeInHierarchy)
                    {
                        danger += ColumnRisk(from, direction, length, speed, ground.Position, 0.35f + radius + safetyMargin * 1.5f) * 6f;
                    }
                    continue;
                }
                if ((target.Altitude & Altitude.Air) == 0)
                {
                    continue;
                }
                // Ramming costs energy every tick: keep clear of air units, bosses and their parts.
                Vector2 velocity = target is BossPart ? Vector2.zero : target.Velocity;
                danger += Risk(from, direction, length, speed, target.Position, velocity, target.Radius + radius + safetyMargin) * 3f;
            }
            return danger;
        }


        /// <summary>
        /// How close a body at <paramref name="at"/> moving at <paramref name="velocity"/> comes to the ship on its way,
        /// 0 (never within <paramref name="reach"/>) to about 1 (on it right now).
        /// </summary>
        private float Risk(Vector2 from, Vector2 direction, float length, float speed, Vector2 at, Vector2 velocity, float reach)
        {
            // Too far to matter within the look-ahead.
            float closing = velocity.magnitude + speed;
            float range = reach + closing * lookAhead;
            if ((at - from).sqrMagnitude > range * range)
            {
                return 0f;
            }
            float worst = 0f;
            for (int m = 0; m < Moments.Length; m++)
            {
                float time = Moments[m] * lookAhead;
                Vector2 shipAt = from + direction * Mathf.Min(length, speed * time);
                Vector2 bodyAt = at + velocity * time;
                float distance = (bodyAt - shipAt).magnitude;
                if (distance >= reach)
                {
                    continue;
                }
                float closeness = 1f - distance / reach;
                float soon = 1f - Moments[m] * 0.5f;
                worst = Mathf.Max(worst, closeness * soon);
            }
            return worst;
        }


        /// <summary>A beam's column (from <paramref name="source"/> down): how much of the way the ship spends inside it.</summary>
        private float ColumnRisk(Vector2 from, Vector2 direction, float length, float speed, Vector2 source, float halfWidth)
        {
            float worst = 0f;
            for (int m = 0; m < Moments.Length; m++)
            {
                float time = Moments[m] * lookAhead;
                Vector2 shipAt = from + direction * Mathf.Min(length, speed * time);
                if (shipAt.y > source.y + 0.5f)
                {
                    continue;
                }
                float side = Mathf.Abs(shipAt.x - source.x);
                if (side >= halfWidth)
                {
                    continue;
                }
                worst = Mathf.Max(worst, (1f - side / halfWidth * 0.5f) * (1f - Moments[m] * 0.5f));
            }
            return worst;
        }


        // ------------------------------------------------------------------ aim and pickups

        /// <summary>How well <paramref name="point"/> lines up under targets (air units and bosses weigh most).</summary>
        private static float Aim(SpaceField field, Vector2 point, float arrival)
        {
            IReadOnlyList<Shootable> targets = field.Targets;
            float score = 0f;
            for (int t = 0; t < targets.Count; t++)
            {
                Shootable target = targets[t];
                if (!field.IsTargetable(target) || target.Position.y < point.y + 1.5f)
                {
                    continue;
                }
                float weight = target is Boss || target is BossPart ? 2f : (target.Altitude & Altitude.Air) != 0 ? 1.5f : 1f;
                // Where the target will be when the ship is there and its bullets have climbed to it (about 30 m/s).
                float time = arrival + (target.Position.y - point.y) / 30f;
                Vector2 velocity = target is GroundUnit ? target.Velocity + field.ScrollVelocity : target.Velocity;
                float x = target.Position.x + velocity.x * time;
                float dx = (x - point.x) / (1.2f + target.Radius);
                score += weight * Mathf.Exp(-dx * dx);
            }
            return Mathf.Min(score, 3f);
        }


        /// <summary>Whether a pickup will be at <paramref name="point"/> about when the ship gets there.</summary>
        private static float Pickups(SpaceField field, Vector2 point, float arrival, float radius)
        {
            IReadOnlyList<Reward> rewards = field.Rewards;
            float score = 0f;
            float reach = radius + 1.2f;
            for (int i = 0; i < rewards.Count; i++)
            {
                Reward reward = rewards[i];
                if (reward == null || !reward.InPlay)
                {
                    continue;
                }
                Vector2 at = reward.Position + (reward.Velocity + field.ScrollVelocity) * arrival;
                float distance = (at - point).magnitude;
                if (distance >= reach * 2f)
                {
                    continue;
                }
                float value = reward is StrikeReward strike && strike.Money > 0 && strike.Money <= StrikeRules.CreditOrb ? 0.4f : 1f;
                score += value * (distance < reach ? 1f : 1f - (distance - reach) / reach);
            }
            return Mathf.Min(score, 2f);
        }


        // ------------------------------------------------------------------ megabomb and weapons

        /// <summary>A megabomb when even the best point is dangerous, when enemy fire crowds the ship, or low on energy at a boss.</summary>
        private bool WantsMegabomb(SpaceField field, AsteroidsPlayer player, StrikeGunnery gunnery, float bestDanger)
        {
            StrikeLoadout loadout = gunnery.Loadout;
            if (loadout == null || loadout.Megabombs <= 0 || gunnery.MegabombCooldown > 0f)
            {
                return false;
            }
            if (bestDanger > megabombDanger * 0.5f && HereDanger > megabombDanger)
            {
                return true;
            }
            int near = 0;
            IReadOnlyList<Shot> shots = field.EnemyShots;
            Vector2 position = player.Position;
            for (int s = 0; s < shots.Count; s++)
            {
                if (shots[s] != null && shots[s].InPlay && (shots[s].Position - position).sqrMagnitude < 25f)
                {
                    near++;
                }
            }
            if (near >= crowdShots)
            {
                return true;
            }
            bool low = loadout.PhaseShields <= 0 && loadout.Energy < 35f;
            return low && HereDanger > megabombDanger * 0.3f;
        }


        /// <summary>
        /// Whether to select the next special: the selected one cannot hit the layer whose targets dominate the screen and
        /// another owned special can.
        /// </summary>
        private static bool WantsCycle(SpaceField field, StrikeLoadout loadout)
        {
            if (loadout == null)
            {
                return false;
            }
            List<StrikeItem> owned = loadout.OwnedSpecials();
            if (owned.Count < 2)
            {
                return false;
            }
            int air = 0;
            int ground = 0;
            IReadOnlyList<Shootable> targets = field.Targets;
            for (int t = 0; t < targets.Count; t++)
            {
                Shootable target = targets[t];
                if (!field.IsTargetable(target))
                {
                    continue;
                }
                if ((target.Altitude & Altitude.Air) != 0)
                {
                    air++;
                }
                else if ((target.Altitude & Altitude.Ground) != 0)
                {
                    ground++;
                }
            }
            Altitude wanted = ground > air * 2 && ground >= 2 ? Altitude.Ground : air > ground * 2 && air >= 2 ? Altitude.Air : Altitude.None;
            if (wanted == Altitude.None || (StrikeWeaponRules.Mask(loadout.Special) & wanted) != 0)
            {
                return false;
            }
            foreach (StrikeItem item in owned)
            {
                if ((StrikeWeaponRules.Mask(item) & wanted) != 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
