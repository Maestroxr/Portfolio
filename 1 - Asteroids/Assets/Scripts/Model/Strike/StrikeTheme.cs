using System;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The look of a world of the strike mode: the terrain tiles of every kind it provides (at least two variants each),
    /// its lighting while the terrain is shown, the colour of the drop shadows and the material of the craters.
    /// </summary>
    [CreateAssetMenu(fileName = "StrikeTheme", menuName = "Asteroids/Strike Theme", order = 5)]
    public class StrikeTheme : ScriptableObject
    {
        /// <summary>The tiles of one kind: variant n is used for variant n modulo the count.</summary>
        [Serializable]
        public class TileSet
        {
            public TerrainKind kind;
            public TerrainTile[] variants = new TerrainTile[0];
        }

        [SerializeField] internal string title;
        [SerializeField] internal Color accent = new Color(1f, 0.7f, 0.3f);
        [Tooltip("Must provide the kinds its levels use; a missing kind falls back to Plain.")]
        [SerializeField] internal TileSet[] tiles = new TileSet[0];
        [SerializeField] internal Color sunColor = Color.white;
        [SerializeField] internal float sunIntensity = 1.4f;
        [SerializeField] internal Vector3 sunAngles = new Vector3(50f, -30f, 0f);
        [SerializeField] internal Color ambientColor = new Color(0.35f, 0.35f, 0.38f);
        [Tooltip("Colour of the aircraft drop shadows.")]
        [SerializeField] internal Color shadowColor = new Color(0f, 0f, 0f, 0.45f);
        [SerializeField] internal Material craterMaterial;
        [Tooltip("Darker light; the ship's light is on.")]
        [SerializeField] internal bool night;

        public string Title => string.IsNullOrEmpty(title) ? name : title;
        public Color Accent => accent;
        public TileSet[] Tiles => tiles ?? new TileSet[0];
        public Color SunColor => sunColor;
        public float SunIntensity => sunIntensity;
        public Vector3 SunAngles => sunAngles;
        public Color AmbientColor => ambientColor;
        public Color ShadowColor => shadowColor;
        public Material CraterMaterial => craterMaterial;
        public bool Night => night;


        /// <summary>Whether the theme has at least one tile of <paramref name="kind"/>.</summary>
        public bool Provides(TerrainKind kind)
        {
            return VariantCount(kind) > 0;
        }


        /// <summary>How many tiles of <paramref name="kind"/> the theme has.</summary>
        public int VariantCount(TerrainKind kind)
        {
            TileSet set = SetOf(kind);
            if (set == null || set.variants == null)
            {
                return 0;
            }
            int count = 0;
            foreach (TerrainTile tile in set.variants)
            {
                if (tile != null)
                {
                    count++;
                }
            }
            return count;
        }


        /// <summary>
        /// The tile prefab of <paramref name="kind"/> and <paramref name="variant"/> (modulo the variant count). A kind the
        /// theme lacks falls back to Plain; null when there is not even that.
        /// </summary>
        public TerrainTile Tile(TerrainKind kind, int variant)
        {
            TerrainTile tile = Pick(SetOf(kind), variant);
            if (tile == null && kind != TerrainKind.Plain)
            {
                tile = Pick(SetOf(TerrainKind.Plain), variant);
            }
            return tile;
        }


        private TileSet SetOf(TerrainKind kind)
        {
            foreach (TileSet set in Tiles)
            {
                if (set != null && set.kind == kind)
                {
                    return set;
                }
            }
            return null;
        }


        private static TerrainTile Pick(TileSet set, int variant)
        {
            if (set == null || set.variants == null || set.variants.Length == 0)
            {
                return null;
            }
            int count = set.variants.Length;
            int start = ((variant % count) + count) % count;
            for (int i = 0; i < count; i++)
            {
                TerrainTile tile = set.variants[(start + i) % count];
                if (tile != null)
                {
                    return tile;
                }
            }
            return null;
        }
    }
}
