using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// Places things behind the playfield plane so they still project exactly onto their logical position: with the
    /// perspective camera at <see cref="Distance"/> in front of the plane, something at depth z (away from the camera)
    /// drawn at xy x k and scaled by k = (Distance + z) / Distance lands on the same pixels as xy on the plane. The ground,
    /// its decals, ground unit visuals and shadows use it; the air layer stays at z = 0. The camera's x and y are 0.
    /// </summary>
    public static class DepthLayer
    {
        /// <summary>Distance from the camera to the playfield plane; the <see cref="CameraRig"/> sets it when it is placed.</summary>
        public static float Distance { get; set; } = 10f / Mathf.Tan(20f * Mathf.Deg2Rad);

        /// <summary>The scale of something at <paramref name="depth"/> that has to look as it would on the plane.</summary>
        public static float Factor(float depth)
        {
            return Distance > 0.001f ? (Distance + depth) / Distance : 1f;
        }

        /// <summary>
        /// Puts <paramref name="target"/> at <paramref name="depth"/> where it projects onto <paramref name="logical"/>, scaled
        /// to look its logical size (its parent is expected to be unscaled).
        /// </summary>
        public static void Place(Transform target, Vector2 logical, float depth)
        {
            if (target == null)
            {
                return;
            }
            float k = Factor(depth);
            target.position = new Vector3(logical.x * k, logical.y * k, depth);
            target.localScale = Vector3.one * k;
        }
    }
}
