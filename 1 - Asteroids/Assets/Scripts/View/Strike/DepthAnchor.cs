using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Keeps its own transform at <see cref="depth"/> behind the playfield plane, projecting onto its parent's position
    /// (see <see cref="DepthLayer"/>). Depth-placed prefabs are root (logic, z = 0) -> "Depth" (this) -> "Visual"; the
    /// visual is never moved here, so the bodies can animate it.
    /// </summary>
    public class DepthAnchor : MonoBehaviour
    {
        [Tooltip("Meters behind the playfield plane (the ground is at 1.2).")]
        [SerializeField] internal float depth = StrikeRules.GroundDepth;

        public float Depth
        {
            get => depth;
            set => depth = value;
        }


        private void LateUpdate()
        {
            Place();
        }


        /// <summary>Places the anchor now (also done every frame after the bodies moved).</summary>
        public void Place()
        {
            Transform parent = transform.parent;
            Vector3 logical = parent != null ? parent.position : Vector3.zero;
            DepthLayer.Place(transform, new Vector2(logical.x, logical.y), depth);
        }
    }
}
