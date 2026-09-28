using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>The sectors of a generated theme: the four sectors of the campaign in its colours (AsteroidsContentBuilder.Theme.cs).</summary>
    internal static partial class AsteroidsContentBuilder
    {
        /// <summary>The look of one sector of a generated theme: the colours the synthwave sky is drawn with and its planet.</summary>
        private sealed class SectorLook
        {
            public string Name;
            public string Title;
            public Color Accent;
            /// <summary>The sky at the top; the sun; the glow of the horizon (and the lower half of the sun); the grid.</summary>
            public Color Sky;
            public Color Sun;
            public Color Glow;
            public Color Grid;
            public float Stars = 1f;
            /// <summary>The Classic planet material the sector's globe is derived from (null for no planet).</summary>
            public Material Planet;
            public string PlanetKey;
            public Color Atmosphere;
            public Vector3 Placement;
            public float Size;
            public Vector3 Tilt;
            public bool Rings;
            public Color Ring;
            public Color Motes;
        }


        /// <summary>A sector of a generated theme by name (its asset name is the Classic sector's, so a mission's sector maps to it).</summary>
        public static SectorTheme ThemeSector(ThemeSpec spec, string name)
        {
            return AsteroidsAssets.Load<SectorTheme>($"{spec.ConfigFolder}/{name}.asset");
        }


        /// <summary>
        /// The four sectors in the look of <paramref name="spec"/>: the same names, titles, planet places and sizes as the
        /// Classic ones, so a mission finds its sector by name, with the theme's neon colours, globes for planets and no
        /// clouds (a globe shows its grid).
        /// </summary>
        private static void BuildThemeSectors(ThemeSpec spec)
        {
            Color H(string hex) => SpaceTextures.Hex(hex);
            var looks = new[]
            {
                new SectorLook
                {
                    Name = "Kepler", Title = "Kepler Belt", Accent = spec.Cyan, Sky = H("05041A"), Sun = spec.Yellow * 2.2f, Glow = spec.Magenta * 0.85f, Grid = spec.Cyan * 1.7f,
                    Stars = 1f, Planet = AsteroidsArtBuilder.Material("PlanetKepler"), PlanetKey = "Materials/PlanetKepler", Atmosphere = spec.Cyan * 1.2f,
                    Placement = new Vector3(0.86f, 0.16f, 150f), Size = 95f, Tilt = new Vector3(18f, 0f, -12f), Rings = false, Ring = spec.Cyan, Motes = spec.Cyan
                },
                new SectorLook
                {
                    Name = "Crimson", Title = "Crimson Expanse", Accent = spec.Pink, Sky = H("12030F"), Sun = new Color(1f, 0.32f, 0.08f) * 2.2f, Glow = spec.Pink * 0.9f,
                    Grid = spec.Magenta * 1.6f, Stars = 0.8f, Planet = AsteroidsArtBuilder.PackMaterial("BonusContent/TinniuqPlanet"),
                    PlanetKey = "StarSparrow/Materials/BonusContent/TinniuqPlanet", Atmosphere = spec.Magenta * 1.2f,
                    Placement = new Vector3(0.13f, 0.82f, 170f), Size = 110f, Tilt = new Vector3(25f, 0f, 10f), Rings = false, Ring = spec.Pink, Motes = spec.Pink
                },
                new SectorLook
                {
                    Name = "Frost", Title = "Frost Rings", Accent = Color.Lerp(spec.Cyan, spec.White, 0.5f), Sky = H("030818"), Sun = new Color(0.85f, 0.95f, 1f) * 1.9f,
                    Glow = spec.Cyan * 0.65f, Grid = Color.Lerp(spec.Cyan, spec.White, 0.45f) * 1.5f, Stars = 1.2f, Planet = AsteroidsArtBuilder.Material("PlanetFrost"),
                    PlanetKey = "Materials/PlanetFrost", Atmosphere = spec.White * 1.1f, Placement = new Vector3(0.83f, 0.78f, 200f), Size = 46f,
                    Tilt = new Vector3(62f, 0f, 22f), Rings = true, Ring = new Color(spec.Cyan.r, spec.Cyan.g, spec.Cyan.b, 0.6f), Motes = spec.White
                },
                new SectorLook
                {
                    Name = "Void", Title = "Void Core", Accent = Color.Lerp(spec.Violet, spec.Magenta, 0.4f), Sky = H("08021A"), Sun = spec.Magenta * 2f, Glow = spec.Violet * 1.1f,
                    Grid = spec.Violet * 2f, Stars = 0.9f, Planet = AsteroidsArtBuilder.Material("PlanetVoid"), PlanetKey = "Materials/PlanetVoid",
                    Atmosphere = spec.Violet * 1.3f, Placement = new Vector3(0.12f, 0.2f, 150f), Size = 100f, Tilt = new Vector3(10f, 0f, 28f), Rings = false,
                    Ring = spec.Violet, Motes = spec.Magenta
                }
            };
            foreach (SectorLook look in looks)
            {
                Material globe = look.Planet != null ? AsteroidsArtBuilder.ThemeMaterial(spec, look.Planet, look.PlanetKey) : null;
                AsteroidsAssets.SaveScriptable<SectorTheme>($"{spec.ConfigFolder}/{look.Name}.asset", t =>
                {
                    t.title = look.Title;
                    t.accent = look.Accent;
                    t.spaceColor = look.Sky;
                    t.nebulaColor = look.Grid;
                    t.highlightColor = look.Sun;
                    t.dustColor = look.Glow;
                    t.starDensity = look.Stars;
                    t.planetMaterial = globe;
                    t.cloudMaterial = null;
                    t.atmosphereColor = look.Atmosphere;
                    t.planetPlacement = look.Placement;
                    t.planetSize = look.Size;
                    t.planetTilt = look.Tilt;
                    t.rings = look.Rings;
                    t.ringColor = look.Ring;
                    t.sunColor = Color.white;
                    t.sunIntensity = 1.2f;
                    t.sunAngles = new Vector3(35f, -40f, 0f);
                    t.ambientColor = new Color(0.2f, 0.16f, 0.3f);
                    t.motesColor = look.Motes;
                });
            }
        }
    }
}
