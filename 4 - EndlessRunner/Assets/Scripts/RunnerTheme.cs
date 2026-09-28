using System;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>Particles that drift around the camera in a world.</summary>
    public enum AmbientEffect
    {
        None,
        Pollen,
        Dust,
        Snow,
        Fireflies,
        Embers,
        Rain,
        Ash
    }


    /// <summary>How a piece of scenery is turned where it stands.</summary>
    public enum SceneryFacing
    {
        /// <summary>Any way: trees, rocks, bushes.</summary>
        Any,
        /// <summary>Square to the road (a quarter turn at a time): buildings, containers.</summary>
        Square,
        /// <summary>Along the road, one way or the other: parked cars, walls, things that span the road.</summary>
        Along,
        /// <summary>Its front (the -z side of the model) toward the road: shops, billboards, lights over the road.</summary>
        Road,
        /// <summary>Its front (the -z side of the model) toward the runner coming down the road: signs and signals.</summary>
        Oncoming
    }


    /// <summary>A decoration the generator scatters beside the road.</summary>
    [Serializable]
    public class SceneryItem
    {
        public TrackPiece prefab;
        public float weight = 1f;
        [Tooltip("Distance from the middle of the road, in meters.")]
        public Vector2 distance = new Vector2(7f, 30f);
        public Vector2 scale = new Vector2(0.8f, 1.3f);
        public SceneryFacing facing = SceneryFacing.Any;

        /// <summary>The turn of a piece standing at <paramref name="x"/>, from a random angle (so every facing draws the same random numbers).</summary>
        public float Yaw(float random, float x)
        {
            switch (facing)
            {
                case SceneryFacing.Square: return Mathf.Round(random / 90f) % 4 * 90f;
                case SceneryFacing.Along: return random < 180f ? 0f : 180f;
                case SceneryFacing.Road: return x < 0f ? 270f : 90f;
                case SceneryFacing.Oncoming: return 0f;
                default: return random;
            }
        }
    }


    /// <summary>
    /// The look of a world: sky, light and fog, the materials of the road and the scenery beside it. Levels reference
    /// a theme; the <see cref="ThemeController"/> blends the environment from one theme to the next.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme", menuName = "Endless Runner/Theme", order = 3)]
    public class RunnerTheme : ScriptableObject
    {
        public string displayName;

        [Header("Sky")]
        public Color skyTop = new Color(0.25f, 0.55f, 0.95f);
        public Color skyHorizon = new Color(0.75f, 0.9f, 1f);
        public Color skyBottom = new Color(0.55f, 0.7f, 0.6f);
        public Color sunDisc = new Color(1f, 0.95f, 0.8f);
        [Range(0.005f, 0.2f)] public float sunSize = 0.04f;
        [Range(0f, 1f)] public float starDensity;
        public Color cloudColor = Color.white;
        [Range(0f, 1f)] public float cloudCoverage = 0.45f;
        public Color mountainFar = new Color(0.55f, 0.7f, 0.85f);
        public Color mountainNear = new Color(0.4f, 0.6f, 0.5f);
        [Range(0f, 0.3f)] public float mountainHeight = 0.09f;

        [Header("Light")]
        public Color sunColor = new Color(1f, 0.96f, 0.88f);
        public float sunIntensity = 1.15f;
        public Vector3 sunRotation = new Vector3(42f, -35f, 0f);
        public Color ambientSky = new Color(0.62f, 0.72f, 0.85f);
        public Color ambientEquator = new Color(0.55f, 0.6f, 0.6f);
        public Color ambientGround = new Color(0.35f, 0.38f, 0.3f);
        [Range(0f, 1f)] public float shadowStrength = 0.6f;

        [Header("Atmosphere")]
        public Color fogColor = new Color(0.75f, 0.88f, 1f);
        public float fogDensity = 0.0105f;
        public AmbientEffect ambientEffect;

        [Header("Track")]
        public Material road;
        public Material stripes;
        public Material curbs;
        public Material ground;
        [Tooltip("The rock walls of a chasm.")]
        public Material cliff;
        public Material chasmFill;
        public Color dustColor = new Color(0.8f, 0.72f, 0.55f, 0.6f);

        [Header("Scenery")]
        public SceneryItem[] scenery = new SceneryItem[0];
        public int sceneryPerTile = 6;
        [Tooltip("Placed on both sides of the road every Roadside Every tiles, e.g. fences or lamp posts.")]
        public TrackPiece roadsideProp;
        public int roadsideEvery = 1;
        public float roadsideOffset = 5.2f;

        [Header("Interface")]
        public Color accent = new Color(0.3f, 0.75f, 0.35f);

        [NonSerialized] private Material[] tileMaterials;

        /// <summary>Materials of a road tile's submeshes: road, stripes, curbs, ground, and the cliffs of a chasm tile.</summary>
        public Material[] TileMaterials
        {
            get
            {
                if (tileMaterials == null || tileMaterials.Length != 5)
                {
                    tileMaterials = new[] { road, stripes, curbs, ground, cliff };
                }
                return tileMaterials;
            }
        }

        private void OnValidate()
        {
            tileMaterials = null;
        }
    }
}
