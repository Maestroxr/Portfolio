using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// A track piece the runner collects or triggers by running into it: coins, gems, power-ups and bounce pads.
    /// Touches are found by the manager with a swept distance test against the runner every frame, so they do not
    /// depend on physics triggers or on the frame rate.
    /// </summary>
    public abstract class Collidable : TrackPiece
    {
        [Tooltip("Pickup radius around the pivot.")]
        [SerializeField] internal float radius = 0.6f;
        [Tooltip("Pulled toward the runner while the magnet is active.")]
        [SerializeField] internal bool magnetic;

        public bool Collected { get; private set; }

        public float Radius => radius;

        public bool Magnetic => magnetic;

        public override void OnSpawned()
        {
            base.OnSpawned();
            Collected = false;
        }

        /// <summary>Handles the runner touching this piece once; later touches are ignored.</summary>
        internal void Touch(RunnerGameManager manager, RunnerPlayer player)
        {
            if (Collected || !CanTouch(player))
            {
                return;
            }
            Collected = true;
            OnTouched(manager, player);
        }

        protected virtual bool CanTouch(RunnerPlayer player)
        {
            return true;
        }

        protected abstract void OnTouched(RunnerGameManager manager, RunnerPlayer player);
    }
}
