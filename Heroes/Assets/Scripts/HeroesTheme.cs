using System;
using System.Collections.Generic;
using Gamebox;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The look of the game as a whole: a <see cref="HeroesArt"/> (the models, sprites, materials, fonts and sounds the
    /// builders made in the theme's manner) and what the art does not hold, which is the colours of the interface's
    /// words and of the players, the light, sky and fog of the map, the tints the spells' glows lean toward, and the
    /// pointers. The definition (Resources/Games/Heroes.asset) starts the game with one theme and lists the others; the
    /// manager reads everything through the active one (<see cref="HeroesGameManager.Art"/>, and the static
    /// <see cref="Palette.Active"/> and <see cref="Glows.Active"/> that <see cref="Activate"/> sets), so the whole game
    /// changes its look when another theme is picked (the settings row, the launcher, or <c>-gamebox-theme</c>).
    /// </summary>
    [CreateAssetMenu(fileName = "Theme", menuName = "Heroes/Theme", order = 11)]
    public class HeroesTheme : GameTheme
    {
        /// <summary>The colours of the words and of the players (the old constants of UIKit and HeroesArt.PlayerColor).</summary>
        [Serializable]
        public sealed class Palette
        {
            [Tooltip("Words on leather and stone.")]
            public Color ink = new Color(0.94f, 0.9f, 0.8f);
            public Color dim = new Color(0.72f, 0.67f, 0.56f);
            [Tooltip("Values, highlights and the fill of the bars.")]
            public Color gold = new Color(0.88f, 0.74f, 0.4f);
            public Color bad = new Color(0.92f, 0.45f, 0.38f);
            public Color good = new Color(0.55f, 0.88f, 0.5f);
            [Tooltip("Words on parchment.")]
            public Color inkOnParchment = new Color(0.24f, 0.15f, 0.07f);
            public Color dimOnParchment = new Color(0.42f, 0.3f, 0.17f);
            [Tooltip("The gradient of the headings of the windows and of the menu's titles, top and bottom.")]
            public Color headingTop = new Color(1f, 0.93f, 0.7f);
            public Color headingBottom = new Color(0.86f, 0.66f, 0.3f);
            [Tooltip("The gradient of the name of the game on the title, top and bottom.")]
            public Color logoTop = new Color(1f, 0.95f, 0.75f);
            public Color logoBottom = new Color(0.82f, 0.58f, 0.22f);
            [Tooltip("What the highlights of the interface lean toward (the glow of a picked place, the frame on the little map, " +
                     "the lines of the map's grid) by accentMix; 0 leaves them their own gold.")]
            public Color accent = new Color(1f, 0.86f, 0.45f);
            [Range(0f, 1f)] public float accentMix;
            [Tooltip("How much of their saturation and brightness the colours of the little map keep.")]
            public float mapSaturation = 1f;
            public float mapValue = 1f;
            [Tooltip("The colours of the players: red, blue, green, tan, and grey for nobody.")]
            public Color[] players =
            {
                new Color(0.86f, 0.16f, 0.14f), new Color(0.2f, 0.42f, 0.9f), new Color(0.2f, 0.7f, 0.25f),
                new Color(0.86f, 0.68f, 0.3f), new Color(0.7f, 0.7f, 0.7f)
            };

            /// <summary>The palette everything reads: the active theme's, or the classic colours without a theme.</summary>
            public static Palette Active { get; set; } = new Palette();

            /// <summary>The colour of a player, grey for anything but the four.</summary>
            public Color Player(int color)
            {
                if (players == null || players.Length == 0)
                {
                    return Color.grey;
                }
                int index = color >= 0 && color < 4 && color < players.Length ? color : players.Length - 1;
                return players[index];
            }

            /// <summary>
            /// A highlight of the interface (drawn in gold in the classic look) in the palette's: it takes the hue and
            /// saturation of <see cref="accent"/> by <see cref="accentMix"/> and keeps most of its own brightness and its alpha.
            /// </summary>
            public Color Accent(Color color)
            {
                if (accentMix <= 0f)
                {
                    return color;
                }
                Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                Color.RGBToHSV(accent, out float toHue, out float toSaturation, out float toValue);
                float mixedHue = Mathf.Repeat(Mathf.LerpAngle(hue * 360f, toHue * 360f, accentMix), 360f) / 360f;
                Color toned = Color.HSVToRGB(mixedHue, Mathf.Lerp(saturation, toSaturation, accentMix), Mathf.Lerp(value, toValue, accentMix * 0.5f));
                toned.a = color.a;
                return toned;
            }

            /// <summary>A colour of the little map as the palette paints it: less saturated or darker, or as it is.</summary>
            public Color32 Map(Color32 color)
            {
                if (Mathf.Approximately(mapSaturation, 1f) && Mathf.Approximately(mapValue, 1f))
                {
                    return color;
                }
                Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                Color mapped = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * mapSaturation), Mathf.Clamp01(value * mapValue));
                return new Color32((byte)Mathf.RoundToInt(mapped.r * 255f), (byte)Mathf.RoundToInt(mapped.g * 255f),
                    (byte)Mathf.RoundToInt(mapped.b * 255f), color.a);
            }
        }


        /// <summary>The light, sky and fog of the map (the battlefield has a sky of its own for every land).</summary>
        [Serializable]
        public sealed class Atmosphere
        {
            public Color sun = new Color(1f, 0.96f, 0.88f);
            public float sunIntensity = 1.45f;
            [Range(0f, 1f)] public float shadowStrength = 0.72f;
            [Tooltip("Euler angles of the sun's light (pitch, yaw).")]
            public Vector2 sunAngles = new Vector2(46f, 138f);
            public Color ambientSky = new Color(0.48f, 0.55f, 0.66f);
            public Color ambientEquator = new Color(0.40f, 0.42f, 0.40f);
            public Color ambientGround = new Color(0.24f, 0.21f, 0.17f);
            public Color fog = new Color(0.62f, 0.68f, 0.75f);
            public float fogStart = 90f;
            public float fogEnd = 260f;
            [Tooltip("The sky over the map (a Skybox material).")]
            public Material sky;

            /// <summary>Lights the open scene this way: its sun, its ambient light, its fog and its sky.</summary>
            public void Apply(Light sunLight)
            {
                if (sunLight != null)
                {
                    sunLight.color = sun;
                    sunLight.intensity = sunIntensity;
                    sunLight.shadowStrength = shadowStrength;
                    sunLight.transform.rotation = Quaternion.Euler(sunAngles.x, sunAngles.y, 0f);
                    RenderSettings.sun = sunLight;
                }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = ambientSky;
                RenderSettings.ambientEquatorColor = ambientEquator;
                RenderSettings.ambientGroundColor = ambientGround;
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = fog;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
                if (sky != null)
                {
                    RenderSettings.skybox = sky;
                }
                DynamicGI.UpdateEnvironment();
            }
        }


        /// <summary>
        /// What the glows of the spells and blows lean toward: every colour an effect is given leans by <see cref="mix"/>
        /// toward <see cref="warm"/> when it is a warm one (fire, holy light, gold sparks) and toward <see cref="cold"/>
        /// otherwise (frost, lightning, healing, curses), so a theme can have its magic in blood red and cold blue.
        /// </summary>
        [Serializable]
        public sealed class Glows
        {
            public Color warm = new Color(1f, 0.6f, 0.2f);
            public Color cold = new Color(0.6f, 0.8f, 1f);
            [Range(0f, 1f), Tooltip("0 leaves the effects their own colours.")]
            public float mix;

            /// <summary>The glows in use: the active theme's, or the effects' own colours without a theme.</summary>
            public static Glows Active { get; set; } = new Glows();

            /// <summary>A colour of an effect, leaning toward the theme's warm or cold glow; the alpha is kept.</summary>
            public Color Grade(Color color)
            {
                if (mix <= 0f)
                {
                    return color;
                }
                Color.RGBToHSV(color, out float hue, out float saturation, out float value);
                bool isWarm = hue >= 0.9f || hue <= 0.19f;
                Color toward = isWarm ? warm : cold;
                Color.RGBToHSV(toward, out float towardHue, out float towardSaturation, out _);
                // Grey stays grey: only what has a colour of its own takes the theme's.
                float amount = mix * Mathf.Clamp01(saturation * 2f);
                float lean = Mathf.Lerp(saturation, Mathf.Max(saturation, towardSaturation), amount);
                Color graded = Color.HSVToRGB(Mathf.Lerp(hue, towardHue, amount), lean, value);
                graded.a = color.a;
                return graded;
            }
        }


        /// <summary>The pointers of the theme, by <see cref="CursorKind"/>; left empty, the art's own are used.</summary>
        [Serializable]
        public sealed class Pointers
        {
            public Texture2D[] cursors = new Texture2D[0];
            /// <summary>The pixel of each pointer that clicks, from its top left corner.</summary>
            public Vector2[] hotspots = new Vector2[0];
        }

        [Tooltip("Everything the builders made in the theme's manner: models, sprites, materials, fonts and sounds.")]
        public HeroesArt art;
        public Palette palette = new Palette();
        public Atmosphere atmosphere = new Atmosphere();
        public Glows glows = new Glows();
        public Pointers pointers = new Pointers();

        public override GameType Game => GameType.Heroes;

        /// <summary>The theme the game shows now (the definition's, the player's choice or the command line's), or null.</summary>
        public static HeroesTheme Current => GameThemes.Active<HeroesTheme>(GameType.Heroes);

        /// <summary>
        /// Makes <paramref name="theme"/> the one the static lookups answer for (<see cref="Palette.Active"/>,
        /// <see cref="Glows.Active"/>); null puts the classic colours back. The manager calls it when it wakes and when
        /// the theme changes; a tool that draws the interface without a manager calls it too.
        /// </summary>
        public static void Activate(HeroesTheme theme)
        {
            Palette.Active = theme != null && theme.palette != null ? theme.palette : new Palette();
            Glows.Active = theme != null && theme.glows != null ? theme.glows : new Glows();
        }

        /// <summary>The picture of a pointer: the theme's own, else the art's.</summary>
        public Texture2D Cursor(CursorKind kind)
        {
            int index = (int)kind;
            if (pointers != null && pointers.cursors != null && index >= 0 && index < pointers.cursors.Length && pointers.cursors[index] != null)
            {
                return pointers.cursors[index];
            }
            return art != null ? art.Cursor(kind) : null;
        }

        /// <summary>The pixel of <see cref="Cursor"/> that clicks, from its top left corner.</summary>
        public Vector2 CursorHotspot(CursorKind kind)
        {
            int index = (int)kind;
            if (pointers != null && pointers.cursors != null && index >= 0 && index < pointers.cursors.Length && pointers.cursors[index] != null)
            {
                return pointers.hotspots != null && index < pointers.hotspots.Length ? pointers.hotspots[index] : Vector2.zero;
            }
            return art != null ? art.CursorHotspot(kind) : Vector2.zero;
        }

        /// <summary>Makes a pointer of the theme the system's pointer (the default one if the kind has no picture).</summary>
        public void UseCursor(CursorKind kind)
        {
            if (Cursor(kind) == null)
            {
                kind = CursorKind.Default;
            }
            Texture2D texture = Cursor(kind);
            UnityEngine.Cursor.SetCursor(texture, texture != null ? CursorHotspot(kind) : Vector2.zero, CursorMode.Auto);
        }

        /// <summary>
        /// The base check (every empty reference, the wrapped art included) and the game's own: the art must have a look
        /// for every creature, hero class, town, ground, obstacle, pointer and sound the rules can ask for.
        /// </summary>
        public override bool Validate(List<string> problems)
        {
            int before = problems.Count;
            base.Validate(problems);
            if (art == null)
            {
                problems.Add($"{name}.art is empty");
                return false;
            }
            Expect(problems, art.units.Count == Creatures.Count, $"art.units has {art.units.Count} creatures of {Creatures.Count}");
            foreach (CreatureDef creature in Creatures.All)
            {
                Expect(problems, art.units.Exists(unit => unit.creature == creature.Id), $"art.units has no {creature.Id}");
            }
            foreach (HeroClass heroClass in Enum.GetValues(typeof(HeroClass)))
            {
                Expect(problems, art.heroes.Exists(hero => hero.heroClass == heroClass), $"art.heroes has no {heroClass}");
            }
            foreach (Faction faction in new[] { Faction.Castle, Faction.Necropolis, Faction.Stronghold })
            {
                HeroesArt.TownArt town = art.towns.Find(candidate => candidate.faction == faction);
                Expect(problems, town != null && town.byColor.Length == 5 && town.portraits.Length == 5, $"art.towns has no {faction} in five colours");
            }
            foreach (BattleObstacle kind in Enum.GetValues(typeof(BattleObstacle)))
            {
                if (kind != BattleObstacle.None)
                {
                    Expect(problems, art.ObstacleVariants(kind, TerrainType.Grass).Length > 0, $"art.obstacles has no {kind}");
                }
            }
            foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
            {
                Expect(problems, art.Sky(terrain) != null, $"art.skies has no sky for {terrain}");
                Expect(problems, art.Backdrop(terrain).Length > 0, $"art.backdrops has nothing for {terrain}");
            }
            Expect(problems, art.layers.Length == Enum.GetValues(typeof(GroundLayer)).Length, "art.layers has a layer for every ground");
            Expect(problems, art.cursors.Length == Enum.GetValues(typeof(CursorKind)).Length, "art.cursors has a pointer of every kind");
            Expect(problems, art.sounds.Length == Enum.GetValues(typeof(Sfx)).Length, "art.sounds has a sound of every kind");
            Expect(problems, art.flags.Length == 5 && art.towers.Length == 5, "art.flags and art.towers come in five colours");
            Expect(problems, art.siegeBanners.Length == 5, "art.siegeBanners come in five colours");
            Expect(problems, art.titleLooks.Length == Enum.GetValues(typeof(TextLook)).Length
                && art.bodyLooks.Length == art.titleLooks.Length && art.logoLooks.Length == art.titleLooks.Length, "the fonts have a material for every look");
            Expect(problems, art.forestTrees.Length > 0 && art.pineTrees.Length > 0 && art.deadTrees.Length > 0, "art has trees of every kind");
            Expect(problems, palette != null && palette.players != null && palette.players.Length == 5, "palette.players has five colours");
            return problems.Count == before;
        }

        private void Expect(List<string> problems, bool condition, string message)
        {
            if (!condition)
            {
                problems.Add($"{name}: {message}");
            }
        }
    }
}
