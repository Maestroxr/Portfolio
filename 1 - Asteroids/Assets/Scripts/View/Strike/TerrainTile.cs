using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// One 20 x 64 m tile of strike terrain (built by the ground content builder): its kind and variant, and the root the
    /// craters on it are parented to, so they scroll and recycle with it. Tile local coordinates: x in [-32, 32], y in
    /// [-10, 10] (design 4.1).
    /// </summary>
    public class TerrainTile : MonoBehaviour
    {
        [SerializeField] internal TerrainKind kind;
        [SerializeField] internal int variant;
        [Tooltip("Length along the level (m).")]
        [SerializeField] internal float length = StrikeRules.TileLength;
        [Tooltip("Width across the level (m).")]
        [SerializeField] internal float width = StrikeRules.TileWidth;
        [Tooltip("Parent of the crater decals on this tile.")]
        [SerializeField] internal Transform decalRoot;

        public TerrainKind Kind => kind;
        public int Variant => variant;
        public float Length => length;
        public float Width => width;
        public Transform DecalRoot => decalRoot != null ? decalRoot : transform;
    }
}
