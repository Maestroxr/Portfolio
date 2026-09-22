namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// What the <see cref="RunnerAnimator"/> and the <see cref="RunnerCamera"/> read of a runner. The runner played
    /// here (<see cref="RunnerPlayer"/>) moves by itself; the ghost of a runner played on another device
    /// (<see cref="RunnerGhost"/>) has the same to say from the poses it receives, so both look and are filmed alike.
    /// </summary>
    public interface IRunnerMotion
    {
        bool IsRunning { get; }

        bool IsGrounded { get; }

        bool IsSliding { get; }

        bool IsInvulnerable { get; }

        /// <summary>Forward speed in meters per second.</summary>
        float Speed { get; }

        float VerticalSpeed { get; }

        /// <summary>-1 or 1 while switching lanes, 0 otherwise.</summary>
        int LaneChangeDirection { get; }
    }
}
