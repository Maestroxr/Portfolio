using Gamebox;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Pool of one kind of road tile (plain road or chasm).</summary>
    public class TerrainCache : PrefabPool<TerrainBehaviour>
    {
        [Tooltip("The tile prefab this pool instantiates; the same object as the pool's Instance.")]
        [SerializeField] internal TerrainBehaviour prefab;

        public TerrainBehaviour Prefab => prefab;

        public override TerrainBehaviour Deploy()
        {
            TerrainBehaviour tile = base.Deploy();
            tile.Cache = this;
            return tile;
        }
    }
}
