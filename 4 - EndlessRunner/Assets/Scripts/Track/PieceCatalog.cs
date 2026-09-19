using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>The prefabs the track generator builds levels from.</summary>
    [CreateAssetMenu(fileName = "PieceCatalog", menuName = "Endless Runner/Piece Catalog", order = 4)]
    public class PieceCatalog : ScriptableObject
    {
        [Header("Tiles")]
        public TerrainBehaviour roadTile;
        public TerrainBehaviour chasmTile;

        [Header("Pickups")]
        public Coin coin;
        public Coin gem;
        public PowerUpPickup magnet;
        public PowerUpPickup shield;
        public PowerUpPickup multiplier;
        public PowerUpPickup superJump;
        public JumpPad jumpPad;

        [Header("Obstacles")]
        public Obstacle hurdle;
        public Obstacle barrier;
        public Obstacle ramp;
        public Obstacle bridge;
        public Obstacle cart;
        public Obstacle[] blocks = new Obstacle[0];
        public Obstacle[] shortWagons = new Obstacle[0];
        public Obstacle[] longWagons = new Obstacle[0];

        [Header("Landmarks")]
        public TrackPiece finishLine;
        public TrackPiece startLine;

        public PowerUpPickup PowerUp(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Magnet: return magnet;
                case PowerUpType.Shield: return shield;
                case PowerUpType.Multiplier: return multiplier;
                case PowerUpType.SuperJump: return superJump;
                default: return null;
            }
        }
    }
}
