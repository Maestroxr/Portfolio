using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Anything the <see cref="TrackGenerator"/> places along the track: road tiles, obstacles, pickups and scenery.
    /// Pieces are pooled; the pivot is at the start of the piece (lowest z) and it reaches <see cref="length"/> meters
    /// down the track, which is how the generator knows when a piece is behind the runner and can be recycled.
    /// </summary>
    public class TrackPiece : MonoBehaviour
    {
        [Tooltip("Extent of the piece along the track, measured from its pivot.")]
        [SerializeField] internal float length = 1f;

        internal TrackPiecePool Pool { get; set; }

        /// <summary>Set while the piece is out on the track.</summary>
        internal bool Live { get; set; }

        /// <summary>Set by pieces that are done (e.g. knocked away) and can be recycled right away.</summary>
        internal bool Expired { get; set; }

        /// <summary>Id of the layout entry the piece was spawned for, or -1.</summary>
        internal int PlacementId { get; set; } = -1;

        /// <summary>Pieces placed on top of this one (coins on a wagon); they go away with it.</summary>
        internal readonly List<TrackPiece> Attached = new List<TrackPiece>();

        public float Length => length;

        public float StartZ => transform.position.z;

        public float EndZ => transform.position.z + length;

        /// <summary>Called when the piece is placed on the track.</summary>
        public virtual void OnSpawned()
        {
            Expired = false;
        }

        /// <summary>Called when the piece leaves the track, right before it goes back to its pool.</summary>
        public virtual void OnRecycled()
        {
            Attached.Clear();
        }

        internal virtual void ReturnToPool()
        {
            if (Pool != null)
            {
                Pool.Undeploy(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
