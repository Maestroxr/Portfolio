using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// A track piece the runner collects or triggers by running into it: coins, gems, power-ups and bounce pads.
    /// Touches are found by the manager with a swept distance test against the runner every frame, so they do not
    /// depend on physics triggers or on the frame rate. In a local race every runner is tested: the first one to touch a
    /// coin or a power-up gets it, and a bounce pad throws each of them.
    /// </summary>
    public abstract class Collidable : TrackPiece
    {
        [Tooltip("Pickup radius around the pivot.")]
        [SerializeField] internal float radius = 0.6f;
        [Tooltip("Pulled toward the runner while the magnet is active.")]
        [SerializeField] internal bool magnetic;

        // The runners a piece that stays for the others (a bounce pad) went off for already.
        private readonly List<RunnerPlayer> touchedBy = new List<RunnerPlayer>();

        /// <summary>Taken: nobody can touch the piece any more.</summary>
        public bool Collected { get; private set; }

        public float Radius => radius;

        public bool Magnetic => magnetic;

        /// <summary>
        /// Whether the piece stays on the track for the other runners of a local race once a runner touched it (a bounce
        /// pad), instead of being taken by the first one (a coin, a power-up).
        /// </summary>
        protected virtual bool StaysForOthers => false;

        public override void OnSpawned()
        {
            base.OnSpawned();
            Collected = false;
            touchedBy.Clear();
        }

        /// <summary>Handles the runner touching this piece once; later touches are ignored.</summary>
        internal void Touch(RunnerGameManager manager, RunnerPlayer player)
        {
            if (TryTouch(player))
            {
                OnTouched(manager, player);
            }
        }

        /// <summary>
        /// Whether <paramref name="player"/>'s touch counts, which it does once: a piece taken by one runner is gone for
        /// everybody, a piece that <see cref="StaysForOthers"/> goes off once for each runner.
        /// </summary>
        internal bool TryTouch(RunnerPlayer player)
        {
            if (Collected || !CanTouch(player))
            {
                return false;
            }
            if (!StaysForOthers)
            {
                Collected = true;
                return true;
            }
            if (touchedBy.Contains(player))
            {
                return false;
            }
            touchedBy.Add(player);
            return true;
        }

        /// <summary>
        /// Another runner of a race took the piece: nobody here can touch it any more. It stays in sight until the ghost of
        /// that runner gets to it.
        /// </summary>
        internal void MarkTaken()
        {
            Collected = true;
        }

        protected virtual bool CanTouch(RunnerPlayer player)
        {
            return true;
        }

        protected abstract void OnTouched(RunnerGameManager manager, RunnerPlayer player);
    }
}
