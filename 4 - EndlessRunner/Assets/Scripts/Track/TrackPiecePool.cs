using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Pool of one track piece prefab (a coin, a hurdle, a tree...). The generator looks pools up by prefab.</summary>
    public class TrackPiecePool : PrefabPool<TrackPiece>
    {
        [Tooltip("The prefab this pool instantiates; the same object as the pool's Instance.")]
        [SerializeField] internal TrackPiece prefab;

        public TrackPiece Prefab => prefab;

        public override TrackPiece Deploy()
        {
            TrackPiece piece = base.Deploy();
            piece.Pool = this;
            return piece;
        }
    }
}
