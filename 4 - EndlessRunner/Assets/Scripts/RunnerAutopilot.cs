using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Plays the runner by itself: jumps hurdles and gaps, slides under barriers, dodges walls and carts, takes ramps
    /// onto wagons and hops from wagon to wagon. Not part of the game; add it to the runner to test levels end to end.
    /// </summary>
    [RequireComponent(typeof(RunnerPlayer))]
    public class RunnerAutopilot : MonoBehaviour
    {
        [SerializeField] internal float lookAhead = 22f;

        private RunnerPlayer player;
        private TrackGenerator track;
        private float laneCooldown;
        private float actionCooldown;

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
            laneCooldown -= Time.deltaTime;
            actionCooldown -= Time.deltaTime;
            Vector3 position = player.transform.position;
            float speed = Mathf.Max(4f, player.Speed);
            bool onTop = position.y > 1.5f;

            if (onTop)
            {
                FlyOverWagonGap(position, speed);
                return;
            }
            if (track.TryGetGapAhead(position.z, speed * 0.35f + 1f, out Vector2 gap) && gap.x - position.z < speed * 0.22f + 0.6f)
            {
                Act(RunnerAction.Jump);
                return;
            }

            Obstacle threat = NearestThreat(player.Lane, position, out float distance);
            if (threat == null)
            {
                SeekRamp(position);
                return;
            }
            switch (threat.Kind)
            {
                case ObstacleKind.Hurdle:
                    if (distance < speed * 0.3f + 0.8f)
                    {
                        Act(RunnerAction.Jump);
                    }
                    break;
                case ObstacleKind.Barrier:
                    if (distance < speed * 0.25f + 1f)
                    {
                        Act(RunnerAction.Slide);
                    }
                    break;
                default:
                    if (distance < speed * 0.9f + 2f)
                    {
                        Dodge(position);
                    }
                    break;
            }
        }

        private void Act(RunnerAction action)
        {
            if (actionCooldown > 0f)
            {
                return;
            }
            player.Press(action);
            actionCooldown = 0.35f;
        }

        /// <summary>The closest obstacle in <paramref name="lane"/> that is not walked on, ignoring wagons reached by a ramp.</summary>
        private Obstacle NearestThreat(int lane, Vector3 position, out float distance)
        {
            distance = float.MaxValue;
            Obstacle nearest = null;
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (!obstacle.Live || obstacle.IsKnocked || LaneOf(obstacle) != lane)
                {
                    continue;
                }
                float ahead = obstacle.StartZ - position.z - (obstacle.Kind == ObstacleKind.Cart ? 1.2f : 0f);
                if (ahead < -0.5f || ahead > lookAhead || !obstacle.IsHazard || HasRampInFront(obstacle))
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

        private bool HasRampInFront(Obstacle obstacle)
        {
            if (obstacle.Kind != ObstacleKind.Platform)
            {
                return false;
            }
            foreach (Obstacle other in track.Obstacles)
            {
                if (other.Live && other.Kind == ObstacleKind.Ramp && LaneOf(other) == LaneOf(obstacle) && Mathf.Abs(other.EndZ - obstacle.StartZ) < 0.5f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Switches to the neighbouring lane whose nearest threat is farthest away.</summary>
        private void Dodge(Vector3 position)
        {
            if (laneCooldown > 0f)
            {
                return;
            }
            int best = player.Lane;
            float bestDistance = -1f;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                int lane = player.Lane + direction;
                if (lane < -1 || lane > 1 || SideBlocked(lane, position))
                {
                    continue;
                }
                NearestThreat(lane, position, out float distance);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = lane;
                }
            }
            if (best == player.Lane)
            {
                Act(RunnerAction.Jump);
                return;
            }
            player.Press(best < player.Lane ? RunnerAction.Left : RunnerAction.Right);
            laneCooldown = 0.3f;
        }

        /// <summary>Heads for a ramp in a neighbouring lane: the coins are up on the wagons.</summary>
        private void SeekRamp(Vector3 position)
        {
            if (laneCooldown > 0f)
            {
                return;
            }
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (!obstacle.Live || obstacle.Kind != ObstacleKind.Ramp)
                {
                    continue;
                }
                float ahead = obstacle.StartZ - position.z;
                int lane = LaneOf(obstacle);
                if (ahead < 4f || ahead > 26f || Mathf.Abs(lane - player.Lane) != 1 || SideBlocked(lane, position))
                {
                    continue;
                }
                NearestThreat(lane, position, out float threat);
                if (threat < ahead)
                {
                    continue;
                }
                player.Press(lane < player.Lane ? RunnerAction.Left : RunnerAction.Right);
                laneCooldown = 0.3f;
                return;
            }
        }

        /// <summary>Whether something stands right beside the runner in <paramref name="lane"/>.</summary>
        private bool SideBlocked(int lane, Vector3 position)
        {
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (obstacle.Live && !obstacle.IsKnocked && LaneOf(obstacle) == lane && obstacle.StartZ < position.z + 1.5f && obstacle.EndZ > position.z - 0.5f
                    && obstacle.Kind != ObstacleKind.Ramp && obstacle.Kind != ObstacleKind.Bridge)
                {
                    return true;
                }
            }
            return false;
        }

        private void FlyOverWagonGap(Vector3 position, float speed)
        {
            foreach (Obstacle obstacle in track.Obstacles)
            {
                if (!obstacle.Live || obstacle.Kind != ObstacleKind.Platform || LaneOf(obstacle) != player.Lane)
                {
                    continue;
                }
                float toEnd = obstacle.EndZ - position.z;
                if (obstacle.StartZ <= position.z && toEnd > 0f && toEnd < speed * 0.12f + 0.4f)
                {
                    Act(RunnerAction.Jump);
                    return;
                }
            }
        }

        private int LaneOf(Component piece)
        {
            return Mathf.RoundToInt(piece.transform.position.x / Mathf.Max(0.1f, player.LaneWidth));
        }
    }
}
