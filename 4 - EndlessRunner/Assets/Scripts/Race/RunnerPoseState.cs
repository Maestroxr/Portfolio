using System;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// What a runner is doing, as it travels with every pose of a race: the state flags of the pose are these
    /// values packed into one number. The ghost of the runner on the other devices animates from them, and turns the
    /// moments they change into the events of the animator (a jump, a landing, a slide).
    /// </summary>
    public struct RunnerPoseState : IEquatable<RunnerPoseState>
    {
        private const uint RunningBit = 1u << 0;
        private const uint GroundedBit = 1u << 1;
        private const uint SlidingBit = 1u << 2;
        private const uint InvulnerableBit = 1u << 3;
        private const uint LaneLeftBit = 1u << 4;
        private const uint LaneRightBit = 1u << 5;
        private const uint LaunchedBit = 1u << 6;
        private const uint SuperJumpBit = 1u << 7;
        private const uint ShieldBit = 1u << 8;
        private const uint MagnetBit = 1u << 9;
        private const uint DeadBit = 1u << 10;
        private const uint FinishedBit = 1u << 11;

        public bool Running;
        public bool Grounded;
        public bool Sliding;
        public bool Invulnerable;

        /// <summary>-1 or 1 while switching lanes, 0 otherwise.</summary>
        public int LaneChange;

        /// <summary>Thrown into the air by a bounce pad, until the runner lands.</summary>
        public bool Launched;

        public bool SuperJump;
        public bool Shield;
        public bool Magnet;

        /// <summary>Out of hearts.</summary>
        public bool Dead;

        /// <summary>Over the finish line and cheering.</summary>
        public bool Finished;

        /// <summary>The run of this runner is over, one way or the other.</summary>
        public bool Done => Dead || Finished;

        public uint Pack()
        {
            uint bits = 0;
            bits |= Running ? RunningBit : 0;
            bits |= Grounded ? GroundedBit : 0;
            bits |= Sliding ? SlidingBit : 0;
            bits |= Invulnerable ? InvulnerableBit : 0;
            bits |= LaneChange < 0 ? LaneLeftBit : 0;
            bits |= LaneChange > 0 ? LaneRightBit : 0;
            bits |= Launched ? LaunchedBit : 0;
            bits |= SuperJump ? SuperJumpBit : 0;
            bits |= Shield ? ShieldBit : 0;
            bits |= Magnet ? MagnetBit : 0;
            bits |= Dead ? DeadBit : 0;
            bits |= Finished ? FinishedBit : 0;
            return bits;
        }

        public static RunnerPoseState Unpack(uint bits)
        {
            return new RunnerPoseState
            {
                Running = (bits & RunningBit) != 0,
                Grounded = (bits & GroundedBit) != 0,
                Sliding = (bits & SlidingBit) != 0,
                Invulnerable = (bits & InvulnerableBit) != 0,
                LaneChange = (bits & LaneLeftBit) != 0 ? -1 : (bits & LaneRightBit) != 0 ? 1 : 0,
                Launched = (bits & LaunchedBit) != 0,
                SuperJump = (bits & SuperJumpBit) != 0,
                Shield = (bits & ShieldBit) != 0,
                Magnet = (bits & MagnetBit) != 0,
                Dead = (bits & DeadBit) != 0,
                Finished = (bits & FinishedBit) != 0
            };
        }

        public bool Equals(RunnerPoseState other)
        {
            return Pack() == other.Pack();
        }

        public override bool Equals(object other)
        {
            return other is RunnerPoseState state && Equals(state);
        }

        public override int GetHashCode()
        {
            return (int)Pack();
        }
    }
}
