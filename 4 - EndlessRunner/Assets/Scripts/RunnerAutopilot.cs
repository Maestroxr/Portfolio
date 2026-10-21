using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Plays the runner by itself: jumps hurdles and gaps, slides under barriers, dodges walls and carts, takes ramps
    /// onto wagons and hops from wagon to wagon (<see cref="RunnerPilotBrain"/>). Not part of the game; add it to the
    /// runner to test levels end to end.
    /// </summary>
    [RequireComponent(typeof(RunnerPlayer))]
    public class RunnerAutopilot : MonoBehaviour
    {
        [SerializeField] internal float lookAhead = 22f;

        private readonly RunnerPilotBrain brain = new RunnerPilotBrain();
        private RunnerPlayer player;
        private TrackGenerator track;

        private void Awake()
        {
            player = GetComponent<RunnerPlayer>();
        }

        private void Update()
        {
            if (track == null)
            {
                track = FindAnyObjectByType<TrackGenerator>();
            }
            if (track == null || !player.IsRunning)
            {
                return;
            }
            brain.LookAhead = lookAhead;
            RunnerAction? action = brain.Decide(track, player.transform.position, player.Lane, player.Speed, player.LaneWidth, Time.deltaTime);
            if (action.HasValue)
            {
                player.Press(action.Value);
            }
        }
    }


    /// <summary>
    /// The judgement of the autopilot, apart from the runner it moves: given where a runner is and what the track holds
    /// ahead, the move to make now, if any. The autopilot of a runner played here feeds it the runner
    /// (<see cref="RunnerAutopilot"/>); the tour's remote control feeds it the ghost of its seat, which the shared screen
    /// runs (<see cref="RunnerRemotePilot"/>).
    /// </summary>
    public sealed class RunnerPilotBrain
    {
        /// <summary>How far ahead the obstacles are looked at, in meters.</summary>
        public float LookAhead = 22f;

        /// <summary>Seconds between two lane changes at least.</summary>
        public float LaneCooldown = 0.3f;

        private float laneCooldown;
        private float actionCooldown;

        /// <summary>
        /// The move for a runner at <paramref name="position"/> in <paramref name="lane"/>, running at <paramref name="speed"/>
        /// meters a second, this frame; null for none.
        /// </summary>
        public RunnerAction? Decide(TrackGenerator track, Vector3 position, int lane, float speed, float laneWidth, float deltaTime)
        {
            laneCooldown -= deltaTime;
            actionCooldown -= deltaTime;
            speed = Mathf.Max(4f, speed);
            bool onTop = position.y > 1.5f;

            if (onTop)
            {
                return FlyOverWagonGap(track, position, lane, speed, laneWidth);
            }
            if (track.TryGetGapAhead(position.z, speed * 0.35f + 1f, out Vector2 gap) && gap.x - position.z < speed * 0.22f + 0.6f)
            {
                return Act(RunnerAction.Jump);
            }

            Obstacle threat = NearestThreat(track, lane, position, laneWidth, out float distance);
            if (threat == null)
            {
                return SeekRamp(track, position, lane, laneWidth);
            }
            switch (threat.Kind)
            {
                case ObstacleKind.Hurdle:
                    return distance < speed * 0.3f + 0.8f ? Act(RunnerAction.Jump) : null;
                case ObstacleKind.Barrier:
                    return distance < speed * 0.25f + 1f ? Act(RunnerAction.Slide) : null;
                default:
                    return distance < speed * 0.9f + 2f ? Dodge(track, position, lane, laneWidth) : null;
            }
        }

        private RunnerAction? Act(RunnerAction action)
        {
            if (actionCooldown > 0f)
            {
                return null;
            }
            actionCooldown = 0.35f;
            return action;
        }

        /// <summary>The closest obstacle in <paramref name="lane"/> that is not walked on, ignoring wagons reached by a ramp.</summary>
        private Obstacle NearestThreat(TrackGenerator track, int lane, Vector3 position, float laneWidth, out float distance)
        {
            distance = float.MaxValue;
            Obstacle nearest = null;
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (!obstacle.Live || obstacle.IsKnocked || LaneOf(obstacle, laneWidth) != lane)
                {
                    continue;
                }
                float ahead = obstacle.StartZ - position.z - (obstacle.Kind == ObstacleKind.Cart ? 1.2f : 0f);
                if (ahead < -0.5f || ahead > LookAhead || !obstacle.IsHazard || HasRampInFront(track, obstacle, laneWidth))
                {
                    continue;
                }
                if (ahead < distance)
                {
                    distance = ahead;
                    nearest = obstacle;
                }
            }
            return nearest;
        }

        private static bool HasRampInFront(TrackGenerator track, Obstacle obstacle, float laneWidth)
        {
            if (obstacle.Kind != ObstacleKind.Platform)
            {
                return false;
            }
            foreach (Obstacle other in track.Obstacles)
            {
                if (other.Live && other.Kind == ObstacleKind.Ramp && LaneOf(other, laneWidth) == LaneOf(obstacle, laneWidth) && Mathf.Abs(other.EndZ - obstacle.StartZ) < 0.5f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Switches to the neighbouring lane whose nearest threat is farthest away.</summary>
        private RunnerAction? Dodge(TrackGenerator track, Vector3 position, int lane, float laneWidth)
        {
            if (laneCooldown > 0f)
            {
                return null;
            }
            int best = lane;
            float bestDistance = -1f;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                int candidate = lane + direction;
                if (candidate < -1 || candidate > 1 || SideBlocked(track, candidate, position, laneWidth))
                {
                    continue;
                }
                NearestThreat(track, candidate, position, laneWidth, out float distance);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            if (best == lane)
            {
                return Act(RunnerAction.Jump);
            }
            laneCooldown = LaneCooldown;
            return best < lane ? RunnerAction.Left : RunnerAction.Right;
        }

        /// <summary>Heads for a ramp in a neighbouring lane: the coins are up on the wagons.</summary>
        private RunnerAction? SeekRamp(TrackGenerator track, Vector3 position, int lane, float laneWidth)
        {
            if (laneCooldown > 0f)
            {
                return null;
            }
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (!obstacle.Live || obstacle.Kind != ObstacleKind.Ramp)
                {
                    continue;
                }
                float ahead = obstacle.StartZ - position.z;
                int rampLane = LaneOf(obstacle, laneWidth);
                if (ahead < 4f || ahead > 26f || Mathf.Abs(rampLane - lane) != 1 || SideBlocked(track, rampLane, position, laneWidth))
                {
                    continue;
                }
                NearestThreat(track, rampLane, position, laneWidth, out float threat);
                if (threat < ahead)
                {
                    continue;
                }
                laneCooldown = LaneCooldown;
                return rampLane < lane ? RunnerAction.Left : RunnerAction.Right;
            }
            return null;
        }

        /// <summary>Whether something stands right beside the runner in <paramref name="lane"/>.</summary>
        private static bool SideBlocked(TrackGenerator track, int lane, Vector3 position, float laneWidth)
        {
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (obstacle.Live && !obstacle.IsKnocked && LaneOf(obstacle, laneWidth) == lane && obstacle.StartZ < position.z + 1.5f && obstacle.EndZ > position.z - 0.5f
                    && obstacle.Kind != ObstacleKind.Ramp && obstacle.Kind != ObstacleKind.Bridge)
                {
                    return true;
                }
            }
            return false;
        }

        private RunnerAction? FlyOverWagonGap(TrackGenerator track, Vector3 position, int lane, float speed, float laneWidth)
        {
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (!obstacle.Live || obstacle.Kind != ObstacleKind.Platform || LaneOf(obstacle, laneWidth) != lane)
                {
                    continue;
                }
                float toEnd = obstacle.EndZ - position.z;
                if (obstacle.StartZ <= position.z && toEnd > 0f && toEnd < speed * 0.12f + 0.4f)
                {
                    return Act(RunnerAction.Jump);
                }
            }
            return null;
        }

        private static int LaneOf(Component piece, float laneWidth)
        {
            return Mathf.RoundToInt(piece.transform.position.x / Mathf.Max(0.1f, laneWidth));
        }
    }
}
