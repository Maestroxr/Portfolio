using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>The surfaces of a track that the theme can dress (<see cref="BiomeLook"/>) and the builder's own paints.</summary>
    internal enum TrackSurface
    {
        Ground = 0,
        Road = 1,
        Shoulder = 2,
        Curb = 3,
        Barrier = 4,
        Line = 5,
        Paint = 6
    }

    /// <summary>
    /// The materials a track is drawn in: the theme's for its land (<see cref="BiomeLook"/>), or plain coloured ones of
    /// its own while the art is missing (so a track always builds and reads clearly), plus the paint of the grid boxes
    /// and the simple materials of the set pieces. Materials made here are shared by every track and kept for the session.
    /// </summary>
    internal static class TrackMaterials
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossinessId = Shader.PropertyToID("_Glossiness");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        private static readonly HashSet<string> warned = new HashSet<string>();
        private static Texture2D stripes;
        private static Texture2D checker;
        private static Shader lit;

        /// <summary>The material of <paramref name="surface"/> in the theme's look of the biome, or a fallback.</summary>
        public static Material For(TrackSurface surface, BiomeLook look, Biome biome, bool night)
        {
            Material themed = Themed(surface, look);
            if (themed != null)
            {
                return themed;
            }
            if (surface != TrackSurface.Paint)
            {
                Warn($"no {surface} material for {biome}", $"Death Race track: the theme has no {surface} material for {biome} yet; using a plain one.");
            }
            return Fallback(surface, biome, night);
        }

        /// <summary>Whether the theme dresses the road (when it does not, the builder paints the edge lines itself).</summary>
        public static bool HasRoad(BiomeLook look)
        {
            return look != null && look.road != null;
        }

        /// <summary>A plain lit material of one colour (shared by everyone who asks for the same one).</summary>
        public static Material Plain(string key, Color color, float smoothness = 0.15f, Color emission = default, Texture2D texture = null)
        {
            if (cache.TryGetValue(key, out Material material) && material != null)
            {
                return material;
            }
            material = new Material(Lit) { name = $"Track {key}" };
            material.SetColor(BaseColorId, color);
            material.SetColor(ColorId, color);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetFloat(GlossinessId, smoothness);
            if (texture != null)
            {
                material.SetTexture(BaseMapId, texture);
                material.SetTexture(MainTexId, texture);
            }
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                material.SetColor(EmissionColorId, emission);
            }
            cache[key] = material;
            return material;
        }

        /// <summary>Says once per session that something is missing (a track builds every race; one warning is plenty).</summary>
        public static void Warn(string key, string message)
        {
            if (warned.Add(key))
            {
                Debug.LogWarning(message);
            }
        }

        private static Material Themed(TrackSurface surface, BiomeLook look)
        {
            if (look == null)
            {
                return null;
            }
            switch (surface)
            {
                case TrackSurface.Ground: return look.ground;
                case TrackSurface.Road: return look.road;
                case TrackSurface.Shoulder: return look.shoulder;
                case TrackSurface.Curb: return look.curb;
                case TrackSurface.Barrier: return look.barrier;
                case TrackSurface.Line: return look.line;
                default: return null;
            }
        }

        private static Material Fallback(TrackSurface surface, Biome biome, bool night)
        {
            string time = night ? "night" : "day";
            switch (surface)
            {
                case TrackSurface.Ground:
                    return Plain($"ground {biome} {time}", Darken(GroundColor(biome), night), 0.05f);
                case TrackSurface.Road:
                    return Plain($"road {biome} {time}", Darken(RoadColor(biome), night), night ? 0.55f : 0.2f);
                case TrackSurface.Shoulder:
                    return Plain($"shoulder {biome} {time}", Darken(ShoulderColor(biome), night), 0.08f);
                case TrackSurface.Curb:
                    return Plain($"curb {time}", Color.white * (night ? 0.7f : 1f), 0.3f, default, Stripes);
                case TrackSurface.Barrier:
                    return Plain($"barrier {biome} {time}", Darken(BarrierColor(biome), night), 0.25f, night ? NeonColor(biome) * 0.9f : default);
                case TrackSurface.Line:
                    return Plain($"line {time}", Color.white * (night ? 0.75f : 1f), 0.3f, default, Checker);
                default:
                    return Plain($"paint {time}", new Color(0.92f, 0.92f, 0.88f, 1f) * (night ? 0.75f : 1f), 0.3f);
            }
        }

        /// <summary>The colour of the lamps and the night glow of a biome.</summary>
        public static Color NeonColor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return new Color(1f, 0.72f, 0.18f, 1f);
                case Biome.Snow: return new Color(0.35f, 0.85f, 1f, 1f);
                case Biome.City: return new Color(1f, 0.25f, 0.75f, 1f);
                default: return new Color(1f, 0.45f, 0.15f, 1f);
            }
        }

        private static Color GroundColor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return new Color(0.42f, 0.4f, 0.36f, 1f);
                case Biome.Snow: return new Color(0.86f, 0.9f, 0.95f, 1f);
                case Biome.City: return new Color(0.32f, 0.33f, 0.35f, 1f);
                default: return new Color(0.78f, 0.6f, 0.4f, 1f);
            }
        }

        private static Color RoadColor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return new Color(0.24f, 0.24f, 0.25f, 1f);
                case Biome.Snow: return new Color(0.3f, 0.32f, 0.36f, 1f);
                case Biome.City: return new Color(0.19f, 0.19f, 0.21f, 1f);
                default: return new Color(0.27f, 0.26f, 0.25f, 1f);
            }
        }

        private static Color ShoulderColor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return new Color(0.52f, 0.48f, 0.42f, 1f);
                case Biome.Snow: return new Color(0.74f, 0.78f, 0.85f, 1f);
                case Biome.City: return new Color(0.55f, 0.55f, 0.53f, 1f);
                default: return new Color(0.66f, 0.5f, 0.34f, 1f);
            }
        }

        private static Color BarrierColor(Biome biome)
        {
            switch (biome)
            {
                case Biome.Industrial: return new Color(0.86f, 0.66f, 0.12f, 1f);
                case Biome.Snow: return new Color(0.82f, 0.22f, 0.18f, 1f);
                case Biome.City: return new Color(0.72f, 0.72f, 0.7f, 1f);
                default: return new Color(0.6f, 0.34f, 0.2f, 1f);
            }
        }

        private static Color Darken(Color color, bool night)
        {
            return night ? new Color(color.r * 0.55f, color.g * 0.58f, color.b * 0.7f, 1f) : color;
        }

        private static Shader Lit
        {
            get
            {
                if (lit == null)
                {
                    lit = Shader.Find("Universal Render Pipeline/Lit");
                    if (lit == null)
                    {
                        lit = Shader.Find("Universal Render Pipeline/Simple Lit");
                    }
                    if (lit == null)
                    {
                        lit = Shader.Find("Standard");
                    }
                }
                return lit;
            }
        }

        /// <summary>Red and white curb stripes: one of each per texture repeat along V.</summary>
        private static Texture2D Stripes
        {
            get
            {
                if (stripes == null)
                {
                    stripes = new Texture2D(1, 2, TextureFormat.RGBA32, false) { name = "Track Curb Stripes", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                    stripes.SetPixel(0, 0, new Color(0.85f, 0.12f, 0.1f, 1f));
                    stripes.SetPixel(0, 1, new Color(0.95f, 0.95f, 0.92f, 1f));
                    stripes.Apply(false, true);
                }
                return stripes;
            }
        }

        /// <summary>A 4 x 4 checker: one texture repeat is 4 m of the line, so the squares are 1 m.</summary>
        private static Texture2D Checker
        {
            get
            {
                if (checker == null)
                {
                    checker = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "Track Checker", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            checker.SetPixel(x, y, (x + y) % 2 == 0 ? new Color(0.95f, 0.95f, 0.92f, 1f) : new Color(0.08f, 0.08f, 0.08f, 1f));
                        }
                    }
                    checker.Apply(false, true);
                }
                return checker;
            }
        }
    }
}
