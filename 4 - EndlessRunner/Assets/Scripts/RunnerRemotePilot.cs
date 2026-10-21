using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The pilot of a remote control, for the online tour (<see cref="RunnerOnlineTour"/>): plays the seat the shared
    /// screen runs for this device through the panel's seam (<see cref="OnlineGameController.SetPadState"/>), as a finger
    /// on the pad would. It has no runner of its own: it reads the ghost of its seat, which follows the poses the screen
    /// sends, and the track laid out here (the same on every device), and judges with the autopilot's brain
    /// (<see cref="RunnerPilotBrain"/>) a little ahead of where the newest pose is, to make up for the way of a move to
    /// the screen and of the pose back. Not part of the game.
    /// </summary>
    internal sealed class RunnerRemotePilot : MonoBehaviour
    {
        /// <summary>Seconds a move takes to the screen's runner and its pose back here: the pilot judges this far ahead.</summary>
        public float Lead = 0.3f;

        private readonly RunnerPilotBrain brain = new RunnerPilotBrain { LaneCooldown = 0.6f };
        private readonly uint[] bits = new uint[4];
        private RunnerGameManager manager;
        private RunnerOnlineController online;
        private int seat;

        /// <summary>The moves sent so far.</summary>
        public int Moves { get; private set; }

        /// <summary>Plays <paramref name="seat"/> of the race of <paramref name="manager"/> through <paramref name="online"/>.</summary>
        public void Begin(RunnerGameManager manager, RunnerOnlineController online, int seat)
        {
            this.manager = manager;
            this.online = online;
            this.seat = seat;
            ControlScheme scheme = SeatInput.Scheme();
            bits[(int)RunnerAction.Left] = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.LeftAction));
            bits[(int)RunnerAction.Right] = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.RightAction));
            bits[(int)RunnerAction.Jump] = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.JumpAction));
            bits[(int)RunnerAction.Slide] = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.SlideAction));
        }

        private void Update()
        {
            if (manager == null || online == null || !online.ShowsRemotePanel || !manager.InRace || manager.track == null)
            {
                return;
            }
            RunnerGhost ghost = manager.GhostOf(seat);
            if (ghost == null || !ghost.HasPose)
            {
                return;
            }
            RoomPoseInfo pose = ghost.LatestPose;
            RunnerPoseState state = RunnerPoseState.Unpack(pose.State);
            if (!state.Running || state.Done)
            {
                return;
            }
            float laneWidth = manager.runner != null ? manager.runner.LaneWidth : LayoutBuilder.LaneWidth;
            float lanes = pose.Position.x / Mathf.Max(0.1f, laneWidth);
            // Half way across, the runner is in the lane it heads for.
            int lane = state.LaneChange > 0 ? Mathf.CeilToInt(lanes - 0.02f) : state.LaneChange < 0 ? Mathf.FloorToInt(lanes + 0.02f) : Mathf.RoundToInt(lanes);
            lane = Mathf.Clamp(lane, -1, 1);
            float speed = Mathf.Max(0f, pose.Velocity.z);
            Vector3 position = pose.Position + new Vector3(0f, 0f, speed * Lead);
            RunnerAction? action = brain.Decide(manager.track, position, lane, speed, laneWidth, Time.deltaTime);
            if (action.HasValue)
            {
                online.SetPadState(Vector2.zero, Vector2.zero, 0u, bits[(int)action.Value]);
                Moves++;
            }
        }
    }
}
