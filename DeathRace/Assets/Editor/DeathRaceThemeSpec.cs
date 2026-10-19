using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>How one biome looks in one theme: the tint and wetness of its surfaces, the glow of its barriers, its fog and its minimap.</summary>
    internal sealed class BiomeSpec
    {
        public Biome Biome;
        /// <summary>
        /// What the track builder multiplies the ground material by: white, the ground pictures carry their own colour
        /// (a tint here would darken them; TrackBuilder.Surface skips it when white).
        /// </summary>
        public Color GroundTint = Color.white;
        /// <summary>The ground material's own colour (multiplies its picture): cools the snow under a warm sun.</summary>
        public Color GroundColor = Color.white;
        public Color RoadTint = Color.white;
        public float GroundSmoothness;
        public float RoadSmoothness;
        /// <summary>The reflector strip of the barriers: black for none (day), an HDR neon colour at night.</summary>
        public Color Neon = Color.black;
        public Color Fog;
        public Color MinimapRoad;
        public Color MinimapEdge;
        public float PropDensity = 1f;
    }


    /// <summary>
    /// Plain editor data of one look of the game: the light and the sky, the post processing, the wetness and tint of the
    /// surfaces per biome, the colours of the interface and of the drivers. The same builders run for both specs:
    /// Scorched (harsh desert daylight, rust and dust; the starting look) and Nightfall (night racing under neon).
    /// </summary>
    internal sealed class DeathRaceThemeSpec
    {
        public string Name;
        public string AssetName;
        public string Description;
        public bool Night;

        // Light and sky.
        public Color SunColor;
        public float SunIntensity;
        public Vector3 SunEuler;
        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;
        public Color Background;
        public bool Fog;
        public float FogStart;
        public float FogEnd;
        /// <summary>The sky the shiny things reflect: horizon, zenith and the colour of the glows near the horizon.</summary>
        public Color SkyHorizon;
        public Color SkyZenith;
        public Color SkyGlow;
        public float ReflectionIntensity;

        // Post processing.
        public float BloomThreshold;
        public float BloomIntensity;
        public float BloomScatter;
        public Color BloomTint;
        public TonemappingMode Tonemapping;
        public float Exposure;
        public float Contrast;
        public float Saturation;
        public Color ColorFilter;
        public float Temperature;
        public float Tint;
        public float Vignette;
        public Color VignetteColor;
        public float Grain;
        public float Aberration;
        public Color Shadows;
        public Color Highlights;

        // Surfaces and props.
        public BiomeSpec[] Biomes;
        public float BarrierSmoothness;
        public float PropSmoothness;

        // Interface.
        public Color Ink;
        public Color Muted;
        public Color Accent;
        public Color AccentDark;
        public Color Second;
        public Color Warn;
        public Color Good;
        public Color Plate;
        public Color PlateDark;
        public Color PlateLight;
        public Color Edge;
        public Color ButtonInk;
        public Color HudText;
        public Color HudAccent;
        public Color HudWarn;

        // People.
        public Color[] SeatColors;
        public Color[] RivalColors;

        public string AssetPath => $"Config/Themes/{AssetName}.asset";
        public string VolumePath => $"Config/Themes/{AssetName}Volume.asset";
        public string MaterialsFolder => $"Art/Materials/{AssetName}";
        public string PropsFolder => $"Art/Props/{AssetName}";
        public string InterfaceFolder => $"Art/UI/{AssetName}";
        public string IconsFolder => $"Art/Icons/{AssetName}";

        public BiomeSpec Of(Biome biome)
        {
            return Biomes[(int)biome];
        }

        public static Color Hex(uint rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }

        private static Color Hdr(uint rgb, float intensity)
        {
            Color c = Hex(rgb);
            return new Color(c.r * intensity, c.g * intensity, c.b * intensity, 1f);
        }

        /// <summary>
        /// The colours of the twenty drivers of the career (index = a driver's colour), the same in both looks: spread
        /// round the hue circle, no two alike, none so dark that it vanishes on the night roads or so pale that it is lost
        /// on the snow.
        /// </summary>
        private static readonly Color[] Drivers =
        {
            Hex(0xE53935), Hex(0x2F6BFF), Hex(0xFFD21F), Hex(0x5CCB2D), Hex(0xFF7A1A),
            Hex(0x9B4DFF), Hex(0x22D3EE), Hex(0xFF4FA3), Hex(0xF4F1EA), Hex(0x8A93A3),
            Hex(0x14B8A6), Hex(0xC9A227), Hex(0x86C8FF), Hex(0x2E9E4F), Hex(0xB0304A),
            Hex(0xC4A3FF), Hex(0xC8663A), Hex(0x8DF0C0), Hex(0x4C5BD4), Hex(0xE8C4A0)
        };

        private static readonly Color[] Seats = { Hex(0xE53935), Hex(0x2F6BFF), Hex(0xFFD21F), Hex(0x5CCB2D) };

        public static readonly DeathRaceThemeSpec Scorched = new DeathRaceThemeSpec
        {
            Name = "Scorched",
            AssetName = "Scorched",
            Description = "Harsh desert daylight, rust and dust.",
            Night = false,
            SunColor = Hex(0xFFE4BE),
            SunIntensity = 1.2f,
            SunEuler = new Vector3(47f, 28f, 0f),
            AmbientSky = Hex(0x7884A0),
            AmbientEquator = Hex(0x857260),
            AmbientGround = Hex(0x4A3A2C),
            Background = Hex(0xB89770),
            Fog = true,
            FogStart = 110f,
            FogEnd = 520f,
            SkyHorizon = Hex(0xD9C4A2),
            SkyZenith = Hex(0x7FA2C8),
            SkyGlow = Hex(0xFFE6B0),
            ReflectionIntensity = 0.8f,
            BloomThreshold = 1.05f,
            BloomIntensity = 0.35f,
            BloomScatter = 0.6f,
            BloomTint = Hex(0xFFE0B8),
            Tonemapping = TonemappingMode.Neutral,
            Exposure = 0f,
            Contrast = 14f,
            Saturation = -2f,
            ColorFilter = Hex(0xFFF3E4),
            Temperature = 6f,
            Tint = 2f,
            Vignette = 0.3f,
            VignetteColor = Hex(0x1E0F05),
            Grain = 0.14f,
            Aberration = 0f,
            Shadows = new Color(0.97f, 0.99f, 1.04f, 0f),
            Highlights = new Color(1.03f, 1f, 0.95f, 0f),
            Biomes = new[]
            {
                new BiomeSpec { Biome = Biome.Desert, GroundSmoothness = 0.3f, RoadSmoothness = 0.38f, Fog = Hex(0xC8A27A), MinimapRoad = Hex(0xF2DDB6), MinimapEdge = Hex(0x3A2414), PropDensity = 1f },
                new BiomeSpec { Biome = Biome.Industrial, GroundSmoothness = 0.35f, RoadSmoothness = 0.42f, Fog = Hex(0x9C8E7A), MinimapRoad = Hex(0xE6DCCB), MinimapEdge = Hex(0x2B2622), PropDensity = 1.15f },
                new BiomeSpec { Biome = Biome.Snow, GroundColor = new Color(0.84f, 0.9f, 1f), GroundSmoothness = 0.45f, RoadSmoothness = 0.5f, Fog = Hex(0xC8D4E2), MinimapRoad = Hex(0xE8F0FA), MinimapEdge = Hex(0x1E2836), PropDensity = 0.95f },
                new BiomeSpec { Biome = Biome.City, GroundSmoothness = 0.35f, RoadSmoothness = 0.42f, Fog = Hex(0xA8A49C), MinimapRoad = Hex(0xEDEAE2), MinimapEdge = Hex(0x26252A), PropDensity = 1.2f }
            },
            BarrierSmoothness = 0.45f,
            PropSmoothness = 0.85f,
            Ink = Hex(0xF3E9D8),
            Muted = Hex(0xB9A68C),
            Accent = Hex(0xFF8A1E),
            AccentDark = Hex(0xB4520E),
            Second = Hex(0xFFC93C),
            Warn = Hex(0xFF4A3A),
            Good = Hex(0x8BD450),
            Plate = Hex(0x2A2522),
            PlateDark = Hex(0x171412),
            PlateLight = Hex(0x4A403A),
            Edge = Hex(0xFF8A1E),
            ButtonInk = Hex(0xFFF6E6),
            HudText = Hex(0xF6EEDF),
            HudAccent = Hex(0xFF9A2E),
            HudWarn = Hex(0xFF4A3A),
            SeatColors = Seats,
            RivalColors = Drivers
        };

        public static readonly DeathRaceThemeSpec Nightfall = new DeathRaceThemeSpec
        {
            Name = "Nightfall",
            AssetName = "Nightfall",
            Description = "Night racing under neon: headlights, glowing barriers, wet asphalt.",
            Night = true,
            SunColor = Hex(0x8FA8FF),
            SunIntensity = 0.6f,
            // The moon stands in the north, ahead of the camera, so the wet roads catch its glint.
            SunEuler = new Vector3(52f, 196f, 0f),
            AmbientSky = Hex(0x3C4C8C),
            AmbientEquator = Hex(0x2A2C5A),
            AmbientGround = Hex(0x111120),
            Background = Hex(0x080A16),
            Fog = true,
            FogStart = 80f,
            FogEnd = 420f,
            SkyHorizon = Hex(0x4A3A86),
            SkyZenith = Hex(0x141A3C),
            SkyGlow = Hex(0xFF4FC8),
            ReflectionIntensity = 1f,
            BloomThreshold = 0.8f,
            BloomIntensity = 1.35f,
            BloomScatter = 0.72f,
            BloomTint = Hex(0xD8E0FF),
            Tonemapping = TonemappingMode.ACES,
            Exposure = 0.55f,
            Contrast = 18f,
            Saturation = 14f,
            ColorFilter = Hex(0xEEF2FF),
            Temperature = -8f,
            Tint = 6f,
            Vignette = 0.36f,
            VignetteColor = Hex(0x02030A),
            Grain = 0.1f,
            Aberration = 0.06f,
            Shadows = new Color(0.98f, 0.98f, 1.06f, 0f),
            Highlights = new Color(1f, 0.98f, 1.03f, 0f),
            Biomes = new[]
            {
                new BiomeSpec { Biome = Biome.Desert, GroundSmoothness = 0.45f, RoadSmoothness = 0.82f, RoadTint = Hex(0xF2EEEA), Neon = Hdr(0xFF6A1A, 2.7f), Fog = Hex(0x1E1424), MinimapRoad = Hex(0xFFD3A8), MinimapEdge = Hex(0x1A0E12), PropDensity = 1f },
                new BiomeSpec { Biome = Biome.Industrial, GroundSmoothness = 0.6f, RoadSmoothness = 0.88f, RoadTint = Hex(0xEEEEF2), Neon = Hdr(0x9CFF1A, 2.1f), Fog = Hex(0x121A1C), MinimapRoad = Hex(0xE2FFB0), MinimapEdge = Hex(0x0C1210), PropDensity = 1.15f },
                new BiomeSpec { Biome = Biome.Snow, GroundSmoothness = 0.55f, RoadSmoothness = 0.85f, RoadTint = Hex(0xF0F2F8), Neon = Hdr(0x2FD8FF, 2.3f), Fog = Hex(0x101A2E), MinimapRoad = Hex(0xBFF6FF), MinimapEdge = Hex(0x081020), PropDensity = 0.95f },
                new BiomeSpec { Biome = Biome.City, GroundSmoothness = 0.65f, RoadSmoothness = 0.9f, RoadTint = Hex(0xF2F2F8), Neon = Hdr(0xFF2FB4, 2.7f), Fog = Hex(0x140C22), MinimapRoad = Hex(0xFFC4F0), MinimapEdge = Hex(0x12081C), PropDensity = 1.2f }
            },
            BarrierSmoothness = 0.65f,
            PropSmoothness = 1f,
            Ink = Hex(0xE8F4FF),
            Muted = Hex(0x8FA3C4),
            Accent = Hex(0x2FE4FF),
            AccentDark = Hex(0x0E7A9C),
            Second = Hex(0xFF3FC8),
            Warn = Hex(0xFF3F6A),
            Good = Hex(0x5CFFA0),
            Plate = Hex(0x0E1324),
            PlateDark = Hex(0x070A14),
            PlateLight = Hex(0x1C2440),
            Edge = Hex(0x2FE4FF),
            ButtonInk = Hex(0xEAF8FF),
            HudText = Hex(0xEAF6FF),
            HudAccent = Hex(0x2FE4FF),
            HudWarn = Hex(0xFF3F6A),
            SeatColors = Seats,
            RivalColors = Drivers
        };

        public static readonly DeathRaceThemeSpec[] All = { Scorched, Nightfall };
    }
}
