using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>A gradient of colors over 0..1: the shading of a beveled metal, from its shadow to its highlight.</summary>
    internal sealed class Ramp
    {
        private readonly (float t, Color color)[] stops;

        public Ramp(params (float t, Color color)[] stops)
        {
            this.stops = stops;
        }

        public Color At(float t)
        {
            t = Mathf.Clamp01(t);
            for (int i = 1; i < stops.Length; i++)
            {
                if (t <= stops[i].t)
                {
                    float k = (t - stops[i - 1].t) / Mathf.Max(0.0001f, stops[i].t - stops[i - 1].t);
                    return Color.Lerp(stops[i - 1].color, stops[i].color, k);
                }
            }
            return stops[stops.Length - 1].color;
        }
    }


    /// <summary>
    /// A change of every colour of the downloaded packs (their texture sheets and the flat colours of their materials) and
    /// of the tints the builders paint them with: less saturated, darker, with a cast toward a colour, so a whole theme
    /// is weathered the same way without a second set of models.
    /// </summary>
    internal sealed class ColorGrade
    {
        /// <summary>What the saturation is multiplied by.</summary>
        public float Saturation = 1f;
        /// <summary>What the brightness is multiplied by, after <see cref="Gamma"/> darkened the middle tones.</summary>
        public float Value = 1f;
        public float Gamma = 1f;
        /// <summary>The cast the result leans toward (scaled by its own brightness), by <see cref="CastMix"/>.</summary>
        public Color Cast = Color.white;
        public float CastMix;

        public Color Apply(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            Color graded = Color.HSVToRGB(hue, Mathf.Clamp01(saturation * Saturation), Mathf.Clamp01(Mathf.Pow(value, Gamma) * Value));
            float luma = graded.r * 0.3f + graded.g * 0.59f + graded.b * 0.11f;
            graded = Color.Lerp(graded, Cast * luma, CastMix);
            graded.a = color.a;
            return graded;
        }
    }


    /// <summary>A sky of the battlefield as the spec describes it, before the sun is measured from its picture.</summary>
    internal sealed class SkyLook
    {
        public string Name;
        /// <summary>The picture in Art/Sky, without its extension.</summary>
        public string File;
        public Color Sun;
        public float Intensity;
        /// <summary>The lowest pitch the sun's light may have, in degrees, so a low sun still lights the field.</summary>
        public float Lowest;
        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;
        /// <summary>Exposure and tint of the skybox material: less than 1 and darker than mid grey for an overcast day.</summary>
        public float Exposure = 1f;
        public Color Tint = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        /// <summary>How much the fog measured from the horizon is darkened, 1 for not at all.</summary>
        public float FogValue = 1f;

        public SkyLook(string name, string file, Color sun, float intensity, float lowest, Color sky, Color equator, Color ground)
        {
            Name = name;
            File = file;
            Sun = sun;
            Intensity = intensity;
            Lowest = lowest;
            AmbientSky = sky;
            AmbientEquator = equator;
            AmbientGround = ground;
        }
    }


    /// <summary>The fonts of a theme: the files, where their font assets go, and the colours of the looks made of them.</summary>
    internal sealed class FontLook
    {
        public string TitleFile;
        public string TitleAsset;
        public string BodyFile;
        public string BodyAsset;
        /// <summary>The real bold and italic faces of the body font, or null for none (bold is then thickened).</summary>
        public string BoldFile;
        public string BoldAsset;
        public string ItalicFile;
        public string ItalicAsset;
        public string LogoFile;
        public string LogoAsset;
        /// <summary>The outline of the title look and its shadow.</summary>
        public Color TitleOutline = new Color(0.14f, 0.07f, 0.02f, 1f);
        public Color TitleShadow = new Color(0f, 0f, 0f, 0.8f);
        /// <summary>The outline of the outlined body look.</summary>
        public Color Outline = new Color(0.06f, 0.035f, 0.02f, 1f);
        /// <summary>The heavy outline of the name of the game and the glow around it.</summary>
        public Color LogoOutline = new Color(0.18f, 0.08f, 0.02f, 1f);
        public Color LogoGlow = new Color(1f, 0.64f, 0.2f, 0.6f);

        /// <summary>The name the look materials of a font asset are filed under: the file name of its font.</summary>
        public static string NameOf(string asset)
        {
            string file = System.IO.Path.GetFileNameWithoutExtension(asset);
            return file.EndsWith(" SDF", StringComparison.Ordinal) ? file.Substring(0, file.Length - 4) : file;
        }
    }


    /// <summary>The light a portrait is rendered in, and how much its corners are darkened afterwards.</summary>
    internal sealed class PortraitLook
    {
        public Color KeyColor = new Color(1f, 0.95f, 0.86f);
        public float Key = 1.35f;
        public Vector2 KeyAngles = new Vector2(38f, -40f);
        public Color FillColor = new Color(0.62f, 0.72f, 0.95f);
        public float Fill = 0.45f;
        public Color RimColor = new Color(1f, 0.9f, 0.72f);
        public float Rim = 1.1f;
        public Color AmbientSky = new Color(0.52f, 0.54f, 0.6f);
        public Color AmbientEquator = new Color(0.4f, 0.38f, 0.36f);
        public Color AmbientGround = new Color(0.2f, 0.18f, 0.16f);
        /// <summary>The key light of a town's picture, seen from higher up and lit harder than a creature.</summary>
        public float TownKey = 1.45f;
        /// <summary>How dark the corners of a portrait go, 0 for not at all.</summary>
        public float Vignette;

        public void Apply(Gamebox.Editor.StudioShot shot)
        {
            shot.KeyColor = KeyColor;
            shot.Key = Key;
            shot.KeyAngles = KeyAngles;
            shot.FillColor = FillColor;
            shot.Fill = Fill;
            shot.RimColor = RimColor;
            shot.Rim = Rim;
            shot.AmbientSky = AmbientSky;
            shot.AmbientEquator = AmbientEquator;
            shot.AmbientGround = AmbientGround;
        }
    }


    /// <summary>The colours of the materials the map is drawn with (the water, the fog of war, the markers).</summary>
    internal sealed class SceneMaterials
    {
        public Color Water = new Color(0.09f, 0.32f, 0.46f, 0.78f);
        public Color FogInside = new Color(0.015f, 0.02f, 0.035f, 1f);
        public Color FogEdge = new Color(0.16f, 0.19f, 0.28f, 1f);
        public float FogOutside = 0.55f;
        public Color Marker = new Color(1f, 0.92f, 0.6f, 0.75f);
        public Color Ring = new Color(1f, 1f, 1f, 0.9f);
        public Color Pool = new Color(0.1f, 0.3f, 0.42f, 0.82f);
        public Color Bog = new Color(0.16f, 0.2f, 0.1f, 0.9f);
    }


    /// <summary>
    /// The colours and manner of the interface kit HeroesUIArt draws: the metal of the rims, the leather of the windows,
    /// the paper, the lacquer of the buttons, the cloth of the ribbon, and whether the frames are ornamented (a key
    /// pattern with a stone in every corner) or riveted.
    /// </summary>
    internal sealed class KitStyle
    {
        // The colours the rest of the builders and the scene refer to.
        public Color Gold = new Color(0.85f, 0.71f, 0.36f);
        public Color GoldDark = new Color(0.44f, 0.33f, 0.14f);
        public Color GoldLight = new Color(0.98f, 0.91f, 0.65f);
        public Color Leather = new Color(0.17f, 0.11f, 0.08f);
        public Color LeatherLight = new Color(0.27f, 0.18f, 0.12f);
        public Color Stone = new Color(0.28f, 0.26f, 0.23f);
        public Color Parchment = new Color(0.90f, 0.82f, 0.64f);
        public Color ParchmentDark = new Color(0.72f, 0.62f, 0.44f);
        public Color Ink = new Color(0.16f, 0.11f, 0.06f);

        /// <summary>The near black of the lines between metal and leather.</summary>
        public Color Edge = new Color(0.07f, 0.045f, 0.025f, 0.95f);
        /// <summary>The bright metal of the rims and bosses, the darker metal of the lesser rims, and the iron of what is off.</summary>
        public Ramp Metal;
        public Ramp MetalDark;
        public Ramp Iron;
        /// <summary>The leather of a window, and the darker leather of a section inside it.</summary>
        public Color LeatherDark = new Color(0.085f, 0.052f, 0.035f);
        public Color LeatherMid = new Color(0.25f, 0.155f, 0.1f);
        public Color PanelDark = new Color(0.06f, 0.04f, 0.03f);
        public Color PanelLight = new Color(0.17f, 0.11f, 0.075f);
        /// <summary>The line engraved along a window's band, and the light on its far side.</summary>
        public Color EngravedDark = new Color(0.28f, 0.17f, 0.05f, 0.6f);
        public Color EngravedLight = new Color(1f, 0.93f, 0.7f, 0.35f);
        /// <summary>Whether the corners wind into the key pattern with a stone set in it (else they are riveted plates).</summary>
        public bool Ornaments = true;
        public Color Gem = new Color(0.62f, 0.07f, 0.08f);
        /// <summary>The leather of a card: at rest, hovered and picked; and the light inside a picked one.</summary>
        public Color[] CardDark = { new Color(0.09f, 0.06f, 0.045f), new Color(0.13f, 0.09f, 0.06f), new Color(0.15f, 0.1f, 0.06f) };
        public Color[] CardLight = { new Color(0.22f, 0.145f, 0.1f), new Color(0.3f, 0.2f, 0.13f), new Color(0.32f, 0.21f, 0.12f) };
        public Color CardGlow = new Color(1f, 0.78f, 0.35f, 0.55f);
        public Color SlotGlow = new Color(1f, 0.8f, 0.36f, 0.7f);
        /// <summary>The lacquer of a button, top and bottom: at rest, hovered, pressed and off; the gloss; the light when hovered.</summary>
        public Color[] ButtonTop = { new Color(0.44f, 0.15f, 0.09f), new Color(0.58f, 0.21f, 0.12f), new Color(0.2f, 0.07f, 0.045f), new Color(0.24f, 0.22f, 0.2f) };
        public Color[] ButtonBottom = { new Color(0.21f, 0.065f, 0.04f), new Color(0.3f, 0.1f, 0.06f), new Color(0.32f, 0.11f, 0.07f), new Color(0.13f, 0.12f, 0.11f) };
        public Color ButtonGloss = new Color(1f, 0.92f, 0.8f);
        public Color ButtonHoverLight = new Color(1f, 0.75f, 0.35f, 0.45f);
        /// <summary>The leather disc of a round button: at rest, hovered and pressed.</summary>
        public Color[] RoundDark = { new Color(0.08f, 0.055f, 0.04f), new Color(0.14f, 0.09f, 0.06f), new Color(0.04f, 0.03f, 0.02f) };
        public Color[] RoundLight = { new Color(0.27f, 0.18f, 0.12f), new Color(0.38f, 0.25f, 0.16f), new Color(0.14f, 0.1f, 0.07f) };
        /// <summary>The planks of a strip.</summary>
        public Color WoodDark = new Color(0.05f, 0.03f, 0.02f);
        public Color WoodLight = new Color(0.3f, 0.19f, 0.11f);
        /// <summary>The paper: its two tones, the burn at its edge, its outline and the ink of its printed corners.</summary>
        public Color PaperDark = new Color(0.8f, 0.68f, 0.48f);
        public Color PaperLight = new Color(0.95f, 0.88f, 0.7f);
        public Color PaperBurn = new Color(0.45f, 0.3f, 0.16f);
        public Color PaperLine = new Color(0.3f, 0.19f, 0.09f, 0.95f);
        public Color PaperInk = new Color(0.36f, 0.22f, 0.1f, 0.78f);
        /// <summary>Blotches on the paper, 0 for clean vellum.</summary>
        public float Stains;
        public Color Stain = new Color(0.4f, 0.34f, 0.28f);
        /// <summary>The cloth of the ribbon, its tails, and the fold where a tail turns under.</summary>
        public Color Ribbon = new Color(0.62f, 0.07f, 0.08f);
        public Color RibbonTailDark = new Color(0.22f, 0.02f, 0.03f);
        public Color RibbonTailLight = new Color(0.4f, 0.05f, 0.06f);
        public Color RibbonFold = new Color(0.12f, 0.01f, 0.015f);
        public Color PortraitBackCenter = new Color(0.3f, 0.23f, 0.17f);
        public Color PortraitBackEdge = new Color(0.07f, 0.055f, 0.045f);
        public Color TooltipDark = new Color(0.06f, 0.04f, 0.03f, 0.96f);
        public Color TooltipLight = new Color(0.15f, 0.1f, 0.07f, 0.96f);
        public Color BannerDark = new Color(0.35f, 0.08f, 0.08f);
        public Color BannerLight = new Color(0.55f, 0.14f, 0.12f);
        public Color StarEmptyFill = new Color(0.08f, 0.06f, 0.04f, 0.6f);
        public Color StarEmptyLine = new Color(0.5f, 0.4f, 0.24f, 0.9f);
        /// <summary>The pointers: the picture of each kind is painted from its light tone at the top to its dark below.</summary>
        public Dictionary<CursorKind, (Color light, Color dark)> Pointers = new Dictionary<CursorKind, (Color light, Color dark)>();
        public Color StrikeLight = new Color(0.95f, 0.96f, 1f);
        public Color StrikeDark = new Color(0.55f, 0.58f, 0.66f);

        /// <summary>The kit as the game had it: old gold on dark leather and stone, parchment for what is read.</summary>
        public static KitStyle Classic()
        {
            var style = new KitStyle
            {
                Metal = new Ramp(
                    (0f, new Color(0.2f, 0.12f, 0.04f)), (0.28f, new Color(0.46f, 0.3f, 0.1f)), (0.52f, new Color(0.76f, 0.56f, 0.24f)),
                    (0.74f, new Color(0.93f, 0.78f, 0.44f)), (0.9f, new Color(1f, 0.92f, 0.64f)), (1f, new Color(1f, 0.98f, 0.86f))),
                MetalDark = new Ramp(
                    (0f, new Color(0.12f, 0.07f, 0.03f)), (0.3f, new Color(0.3f, 0.19f, 0.08f)), (0.55f, new Color(0.56f, 0.39f, 0.17f)),
                    (0.8f, new Color(0.78f, 0.6f, 0.3f)), (1f, new Color(0.95f, 0.82f, 0.52f))),
                Iron = IronRamp()
            };
            var gold = (new Color(1f, 0.93f, 0.66f), new Color(0.74f, 0.52f, 0.2f));
            var hand = (new Color(1f, 0.95f, 0.8f), new Color(0.84f, 0.66f, 0.42f));
            style.Pointers[CursorKind.Default] = gold;
            style.Pointers[CursorKind.Hand] = hand;
            style.Pointers[CursorKind.Move] = gold;
            style.Pointers[CursorKind.Fly] = (new Color(1f, 1f, 1f), new Color(0.7f, 0.8f, 0.95f));
            style.Pointers[CursorKind.Attack] = (new Color(0.95f, 0.96f, 1f), new Color(0.55f, 0.58f, 0.66f));
            style.Pointers[CursorKind.Shoot] = (new Color(1f, 0.9f, 0.62f), new Color(0.66f, 0.42f, 0.18f));
            style.Pointers[CursorKind.Cast] = (new Color(0.82f, 0.9f, 1f), new Color(0.38f, 0.5f, 0.95f));
            style.Pointers[CursorKind.Blocked] = (new Color(1f, 0.5f, 0.42f), new Color(0.72f, 0.1f, 0.08f));
            style.Pointers[CursorKind.Wait] = gold;
            style.Pointers[CursorKind.Info] = (new Color(1f, 0.97f, 0.88f), new Color(0.82f, 0.72f, 0.5f));
            style.Pointers[CursorKind.Travel] = gold;
            style.Pointers[CursorKind.Visit] = gold;
            style.Pointers[CursorKind.Fight] = (new Color(1f, 0.62f, 0.5f), new Color(0.7f, 0.16f, 0.1f));
            style.Pointers[CursorKind.Take] = hand;
            return style;
        }

        /// <summary>
        /// The kit of the Grim Realm: black leather and cold iron with rivets where the gold and its ornaments were, bone
        /// white paper with stains, blood red ribbons, and steel on the bars and bosses.
        /// </summary>
        public static KitStyle Grim()
        {
            var style = new KitStyle
            {
                Gold = new Color(0.66f, 0.68f, 0.72f),
                GoldDark = new Color(0.26f, 0.27f, 0.3f),
                GoldLight = new Color(0.86f, 0.88f, 0.92f),
                Leather = new Color(0.1f, 0.095f, 0.095f),
                LeatherLight = new Color(0.18f, 0.17f, 0.17f),
                Stone = new Color(0.24f, 0.24f, 0.25f),
                Parchment = new Color(0.82f, 0.79f, 0.7f),
                ParchmentDark = new Color(0.62f, 0.59f, 0.52f),
                Ink = new Color(0.1f, 0.09f, 0.09f),
                Edge = new Color(0.03f, 0.03f, 0.035f, 0.95f),
                Metal = new Ramp(
                    (0f, new Color(0.06f, 0.06f, 0.07f)), (0.3f, new Color(0.2f, 0.21f, 0.23f)), (0.52f, new Color(0.4f, 0.42f, 0.46f)),
                    (0.74f, new Color(0.6f, 0.63f, 0.68f)), (0.9f, new Color(0.78f, 0.81f, 0.86f)), (1f, new Color(0.92f, 0.94f, 0.97f))),
                MetalDark = new Ramp(
                    (0f, new Color(0.04f, 0.04f, 0.045f)), (0.3f, new Color(0.13f, 0.13f, 0.14f)), (0.55f, new Color(0.26f, 0.27f, 0.29f)),
                    (0.8f, new Color(0.42f, 0.44f, 0.47f)), (1f, new Color(0.6f, 0.62f, 0.66f))),
                Iron = IronRamp(),
                LeatherDark = new Color(0.04f, 0.038f, 0.04f),
                LeatherMid = new Color(0.14f, 0.13f, 0.135f),
                PanelDark = new Color(0.03f, 0.03f, 0.032f),
                PanelLight = new Color(0.1f, 0.095f, 0.1f),
                EngravedDark = new Color(0.02f, 0.02f, 0.025f, 0.7f),
                EngravedLight = new Color(0.8f, 0.84f, 0.9f, 0.25f),
                Ornaments = false,
                Gem = new Color(0.5f, 0.04f, 0.05f),
                CardDark = new[] { new Color(0.045f, 0.043f, 0.045f), new Color(0.075f, 0.07f, 0.072f), new Color(0.09f, 0.06f, 0.06f) },
                CardLight = new[] { new Color(0.14f, 0.135f, 0.14f), new Color(0.2f, 0.19f, 0.2f), new Color(0.22f, 0.17f, 0.17f) },
                CardGlow = new Color(0.62f, 0.72f, 0.95f, 0.45f),
                SlotGlow = new Color(0.65f, 0.75f, 1f, 0.6f),
                ButtonTop = new[] { new Color(0.2f, 0.19f, 0.2f), new Color(0.42f, 0.1f, 0.08f), new Color(0.06f, 0.055f, 0.06f), new Color(0.16f, 0.16f, 0.16f) },
                ButtonBottom = new[] { new Color(0.07f, 0.065f, 0.07f), new Color(0.2f, 0.04f, 0.035f), new Color(0.12f, 0.11f, 0.12f), new Color(0.08f, 0.08f, 0.08f) },
                ButtonGloss = new Color(0.85f, 0.88f, 0.95f),
                ButtonHoverLight = new Color(1f, 0.35f, 0.25f, 0.35f),
                RoundDark = new[] { new Color(0.05f, 0.048f, 0.05f), new Color(0.09f, 0.085f, 0.09f), new Color(0.025f, 0.025f, 0.028f) },
                RoundLight = new[] { new Color(0.18f, 0.175f, 0.18f), new Color(0.26f, 0.25f, 0.26f), new Color(0.09f, 0.09f, 0.095f) },
                WoodDark = new Color(0.035f, 0.03f, 0.03f),
                WoodLight = new Color(0.19f, 0.16f, 0.14f),
                PaperDark = new Color(0.68f, 0.64f, 0.56f),
                PaperLight = new Color(0.88f, 0.86f, 0.78f),
                PaperBurn = new Color(0.3f, 0.26f, 0.22f),
                PaperLine = new Color(0.16f, 0.14f, 0.12f, 0.95f),
                PaperInk = new Color(0.2f, 0.18f, 0.16f, 0.8f),
                Stains = 0.55f,
                Stain = new Color(0.42f, 0.36f, 0.3f),
                Ribbon = new Color(0.48f, 0.04f, 0.05f),
                RibbonTailDark = new Color(0.14f, 0.01f, 0.015f),
                RibbonTailLight = new Color(0.3f, 0.03f, 0.035f),
                RibbonFold = new Color(0.07f, 0.005f, 0.01f),
                PortraitBackCenter = new Color(0.3f, 0.3f, 0.34f),
                PortraitBackEdge = new Color(0.03f, 0.03f, 0.035f),
                TooltipDark = new Color(0.03f, 0.03f, 0.035f, 0.97f),
                TooltipLight = new Color(0.1f, 0.095f, 0.1f, 0.97f),
                BannerDark = new Color(0.24f, 0.03f, 0.04f),
                BannerLight = new Color(0.42f, 0.06f, 0.07f),
                StarEmptyFill = new Color(0.05f, 0.05f, 0.055f, 0.6f),
                StarEmptyLine = new Color(0.4f, 0.42f, 0.46f, 0.9f),
                StrikeLight = new Color(0.86f, 0.88f, 0.94f),
                StrikeDark = new Color(0.36f, 0.38f, 0.44f)
            };
            var iron = (new Color(0.82f, 0.84f, 0.9f), new Color(0.32f, 0.34f, 0.4f));
            var bone = (new Color(0.9f, 0.88f, 0.82f), new Color(0.56f, 0.53f, 0.48f));
            var blood = (new Color(0.95f, 0.36f, 0.3f), new Color(0.48f, 0.06f, 0.05f));
            var frost = (new Color(0.86f, 0.92f, 1f), new Color(0.36f, 0.5f, 0.85f));
            style.Pointers[CursorKind.Default] = iron;
            style.Pointers[CursorKind.Hand] = bone;
            style.Pointers[CursorKind.Move] = iron;
            style.Pointers[CursorKind.Fly] = frost;
            style.Pointers[CursorKind.Attack] = (style.StrikeLight, style.StrikeDark);
            style.Pointers[CursorKind.Shoot] = (new Color(0.7f, 0.62f, 0.55f), new Color(0.3f, 0.24f, 0.2f));
            style.Pointers[CursorKind.Cast] = frost;
            style.Pointers[CursorKind.Blocked] = blood;
            style.Pointers[CursorKind.Wait] = iron;
            style.Pointers[CursorKind.Info] = bone;
            style.Pointers[CursorKind.Travel] = iron;
            style.Pointers[CursorKind.Visit] = iron;
            style.Pointers[CursorKind.Fight] = blood;
            style.Pointers[CursorKind.Take] = bone;
            return style;
        }

        private static Ramp IronRamp()
        {
            return new Ramp(
                (0f, new Color(0.08f, 0.08f, 0.08f)), (0.35f, new Color(0.24f, 0.23f, 0.22f)), (0.6f, new Color(0.42f, 0.41f, 0.39f)),
                (0.85f, new Color(0.62f, 0.61f, 0.58f)), (1f, new Color(0.8f, 0.79f, 0.76f)));
        }
    }


    /// <summary>
    /// How the builders make the art of one theme: where its files go, how the packs' colours are graded, the terrain,
    /// the skies, the kit, the fonts, the pointers, the light of the portraits, and the colours and light the theme asset
    /// carries beside its art. The builders read <see cref="Current"/> while they run; <see cref="Classic"/> reproduces
    /// the game as it was, <see cref="GrimRealm"/> is its darker second look.
    /// </summary>
    internal sealed class HeroesThemeSpec
    {
        /// <summary>The asset name of the theme, and what the player is shown.</summary>
        public string Name;
        public string DisplayName;
        public string Description;
        /// <summary>The generated models, materials, textures, terrain, portraits and pointers.</summary>
        public string Generated;
        /// <summary>The sprites of the interface kit.</summary>
        public string KitFolder;
        /// <summary>The font assets and their looks.</summary>
        public string FontFolder;
        public string ArtAsset;
        public string ThemeAsset;
        /// <summary>How the packs' colours change, or null for their own.</summary>
        public ColorGrade Grade;
        /// <summary>How the colours of the painted icons change (they go into the kit's folder then), or null for the shared ones.</summary>
        public ColorGrade IconGrade;
        /// <summary>By terrain texture name, how its picture is graded; null keeps the downloaded pictures.</summary>
        public Dictionary<string, ColorGrade> Terrain;
        public SkyLook[] Skies;
        /// <summary>By <see cref="TerrainType"/>: the name of the sky a battle on it is fought under.</summary>
        public string[] SkyOfTerrain;
        public string DuskSky = "Dusk";
        /// <summary>The sky over the map.</summary>
        public string MapSky;
        /// <summary>The fog of the battlefield: where it begins and where it hides everything.</summary>
        public float FogStart = 75f;
        public float FogEnd = 290f;
        public KitStyle Kit;
        public FontLook Fonts;
        public PortraitLook Portraits = new PortraitLook();
        public SceneMaterials Materials = new SceneMaterials();
        /// <summary>Dead trees among the forests of the map.</summary>
        public bool DeadForests;
        /// <summary>More pieces for the ring around a battlefield, by ground: dead trees, ruins, a crypt.</summary>
        public (TerrainType terrain, string model, float width)[] BackdropExtras = new (TerrainType, string, float)[0];
        public HeroesTheme.Palette Palette = new HeroesTheme.Palette();
        public HeroesTheme.Atmosphere Atmosphere = new HeroesTheme.Atmosphere();
        public HeroesTheme.Glows Glows = new HeroesTheme.Glows();

        private static HeroesThemeSpec current;

        /// <summary>The spec the builders read while they run: the classic one unless a build says otherwise.</summary>
        public static HeroesThemeSpec Current
        {
            get => current ?? Classic;
            set => current = value;
        }

        public static HeroesThemeSpec[] All => new[] { Classic, GrimRealm };

        /// <summary>The spec called <paramref name="name"/> (asset or display name), or null.</summary>
        public static HeroesThemeSpec Find(string name)
        {
            foreach (HeroesThemeSpec spec in All)
            {
                if (string.Equals(spec.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(spec.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return spec;
                }
            }
            return null;
        }

        /// <summary>Runs <paramref name="build"/> with this spec current, and puts the one before back afterwards.</summary>
        public void Run(Action build)
        {
            HeroesThemeSpec before = Current;
            Current = this;
            try
            {
                build();
            }
            finally
            {
                Current = before;
            }
        }

        // ------------------------------------------------------------------ the themes

        /// <summary>The game as it was: the packs' own colours, clear skies, gold on leather, Cinzel and Alegreya.</summary>
        public static readonly HeroesThemeSpec Classic = new HeroesThemeSpec
        {
            Name = "Classic",
            DisplayName = "Classic",
            Description = "Old gold on dark leather, parchment and clear skies: the game as it was drawn.",
            Generated = "Art/Generated",
            KitFolder = "UI/Generated",
            FontFolder = "Art/Generated/Fonts",
            ArtAsset = "Art/HeroesArt.asset",
            ThemeAsset = "Content/Themes/Classic.asset",
            Skies = new[]
            {
                new SkyLook("Clear", "kloofendal_48d_partly_cloudy_puresky", new Color(1f, 0.96f, 0.88f), 1.45f, 38f,
                    new Color(0.52f, 0.6f, 0.72f), new Color(0.44f, 0.46f, 0.46f), new Color(0.26f, 0.23f, 0.18f)),
                new SkyLook("Overcast", "kloofendal_overcast_puresky", new Color(0.9f, 0.92f, 0.95f), 1.0f, 45f,
                    new Color(0.6f, 0.62f, 0.66f), new Color(0.5f, 0.5f, 0.5f), new Color(0.3f, 0.28f, 0.25f)),
                new SkyLook("Snow", "snow_field_puresky", new Color(0.96f, 0.97f, 1f), 1.15f, 35f,
                    new Color(0.64f, 0.68f, 0.74f), new Color(0.56f, 0.58f, 0.62f), new Color(0.42f, 0.42f, 0.45f)),
                new SkyLook("Wasteland", "wasteland_clouds_puresky", new Color(1f, 0.87f, 0.68f), 1.35f, 26f,
                    new Color(0.55f, 0.58f, 0.66f), new Color(0.52f, 0.46f, 0.4f), new Color(0.3f, 0.24f, 0.18f)),
                new SkyLook("Dusk", "qwantani_dusk_2_puresky", new Color(1f, 0.64f, 0.44f), 1.0f, 18f,
                    new Color(0.38f, 0.36f, 0.52f), new Color(0.38f, 0.3f, 0.32f), new Color(0.16f, 0.12f, 0.12f))
            },
            SkyOfTerrain = new[] { "Clear", "Clear", "Wasteland", "Snow", "Overcast", "Clear", "Wasteland", "Clear", "Dusk" },
            MapSky = "Clear",
            Kit = KitStyle.Classic(),
            Fonts = new FontLook
            {
                TitleFile = "Art/Fonts/Cinzel-Bold.ttf",
                TitleAsset = "Art/Generated/Cinzel-Bold SDF.asset",
                BodyFile = "Art/Fonts/Alegreya-Regular.ttf",
                BodyAsset = "Art/Generated/Alegreya SDF.asset",
                BoldFile = "Art/Fonts/Alegreya-Bold.ttf",
                BoldAsset = "Art/Generated/Fonts/Alegreya-Bold SDF.asset",
                ItalicFile = "Art/Fonts/Alegreya-Italic.ttf",
                ItalicAsset = "Art/Generated/Fonts/Alegreya-Italic SDF.asset",
                LogoFile = "Art/Fonts/CinzelDecorative-Black.ttf",
                LogoAsset = "Art/Generated/Fonts/CinzelDecorative-Black SDF.asset"
            }
        };

        /// <summary>
        /// The Grim Realm: the same models under an overcast sky, their colours drained and cold, the ground mud, ash and
        /// dead grass, dead trees and ruins around the fields, black leather and iron for the interface, bone white paper,
        /// blood red ribbons, an old printed serif and a blackletter name, and magic in blood red and cold blue.
        /// </summary>
        public static readonly HeroesThemeSpec GrimRealm = new HeroesThemeSpec
        {
            Name = "GrimRealm",
            DisplayName = "Grim Realm",
            Description = "A darker realm: overcast skies, weathered armies, mud and ash, black leather and cold iron.",
            Generated = "Art/Themes/GrimRealm/Generated",
            KitFolder = "Art/Themes/GrimRealm/UI",
            FontFolder = "Art/Themes/GrimRealm/Generated/Fonts",
            ArtAsset = "Art/Themes/GrimRealm/HeroesArt.asset",
            ThemeAsset = "Content/Themes/GrimRealm.asset",
            Grade = new ColorGrade { Saturation = 0.55f, Value = 0.9f, Gamma = 1.1f, Cast = new Color(0.62f, 0.66f, 0.76f), CastMix = 0.18f },
            IconGrade = new ColorGrade { Saturation = 0.6f, Value = 0.95f },
            Terrain = new Dictionary<string, ColorGrade>
            {
                ["Grass"] = new ColorGrade { Saturation = 0.45f, Value = 0.82f, Gamma = 1.05f, Cast = new Color(0.6f, 0.55f, 0.4f), CastMix = 0.35f },
                ["Dirt"] = new ColorGrade { Saturation = 0.6f, Value = 0.72f, Gamma = 1.05f, Cast = new Color(0.42f, 0.34f, 0.26f), CastMix = 0.4f },
                ["Sand"] = new ColorGrade { Saturation = 0.3f, Value = 0.72f, Gamma = 1.05f, Cast = new Color(0.56f, 0.54f, 0.52f), CastMix = 0.45f },
                ["Snow"] = new ColorGrade { Saturation = 0.3f, Value = 0.85f, Gamma = 1.05f, Cast = new Color(0.72f, 0.75f, 0.82f), CastMix = 0.35f },
                ["Swamp"] = new ColorGrade { Saturation = 0.5f, Value = 0.68f, Gamma = 1.1f, Cast = new Color(0.3f, 0.34f, 0.24f), CastMix = 0.4f },
                ["Rough"] = new ColorGrade { Saturation = 0.45f, Value = 0.72f, Gamma = 1.05f, Cast = new Color(0.46f, 0.42f, 0.38f), CastMix = 0.4f },
                ["Wasteland"] = new ColorGrade { Saturation = 0.35f, Value = 0.68f, Gamma = 1.05f, Cast = new Color(0.46f, 0.43f, 0.42f), CastMix = 0.45f },
                ["Rock"] = new ColorGrade { Saturation = 0.35f, Value = 0.68f, Gamma = 1.1f, Cast = new Color(0.34f, 0.37f, 0.42f), CastMix = 0.4f },
                ["Road"] = new ColorGrade { Saturation = 0.45f, Value = 0.74f, Gamma = 1.05f, Cast = new Color(0.4f, 0.38f, 0.36f), CastMix = 0.4f },
                ["ForestFloor"] = new ColorGrade { Saturation = 0.5f, Value = 0.68f, Gamma = 1.1f, Cast = new Color(0.34f, 0.3f, 0.24f), CastMix = 0.4f }
            },
            Skies = new[]
            {
                new SkyLook("Gloom", "kloofendal_overcast_puresky", new Color(0.78f, 0.8f, 0.86f), 1.15f, 45f,
                    new Color(0.4f, 0.42f, 0.47f), new Color(0.34f, 0.34f, 0.36f), new Color(0.16f, 0.15f, 0.15f))
                    { Exposure = 0.62f, Tint = new Color(0.42f, 0.44f, 0.5f, 0.5f), FogValue = 0.62f },
                new SkyLook("Bleak", "kloofendal_48d_partly_cloudy_puresky", new Color(0.82f, 0.82f, 0.86f), 1.25f, 38f,
                    new Color(0.38f, 0.41f, 0.48f), new Color(0.34f, 0.34f, 0.36f), new Color(0.15f, 0.14f, 0.13f))
                    { Exposure = 0.55f, Tint = new Color(0.4f, 0.42f, 0.48f, 0.5f), FogValue = 0.6f },
                new SkyLook("Storm", "wasteland_clouds_puresky", new Color(0.84f, 0.8f, 0.76f), 1.2f, 26f,
                    new Color(0.38f, 0.38f, 0.42f), new Color(0.36f, 0.33f, 0.31f), new Color(0.16f, 0.14f, 0.13f))
                    { Exposure = 0.58f, Tint = new Color(0.42f, 0.4f, 0.42f, 0.5f), FogValue = 0.6f },
                new SkyLook("Snow", "snow_field_puresky", new Color(0.84f, 0.86f, 0.92f), 1.1f, 35f,
                    new Color(0.44f, 0.46f, 0.5f), new Color(0.36f, 0.37f, 0.4f), new Color(0.22f, 0.22f, 0.24f))
                    { Exposure = 0.62f, Tint = new Color(0.42f, 0.44f, 0.5f, 0.5f), FogValue = 0.68f },
                new SkyLook("Dusk", "qwantani_dusk_2_puresky", new Color(0.92f, 0.52f, 0.38f), 1.0f, 18f,
                    new Color(0.3f, 0.28f, 0.38f), new Color(0.3f, 0.23f, 0.25f), new Color(0.12f, 0.1f, 0.1f))
                    { Exposure = 0.7f, Tint = new Color(0.45f, 0.4f, 0.45f, 0.5f), FogValue = 0.7f }
            },
            SkyOfTerrain = new[] { "Gloom", "Bleak", "Storm", "Snow", "Gloom", "Storm", "Dusk", "Bleak", "Dusk" },
            MapSky = "Gloom",
            FogStart = 35f,
            FogEnd = 200f,
            Kit = KitStyle.Grim(),
            Fonts = new FontLook
            {
                TitleFile = "Art/Themes/GrimRealm/Fonts/PirataOne-Regular.ttf",
                TitleAsset = "Art/Themes/GrimRealm/Generated/Fonts/PirataOne SDF.asset",
                BodyFile = "Art/Themes/GrimRealm/Fonts/IMFeENrm28P.ttf",
                BodyAsset = "Art/Themes/GrimRealm/Generated/Fonts/IMFellEnglish SDF.asset",
                ItalicFile = "Art/Themes/GrimRealm/Fonts/IMFeENit28P.ttf",
                ItalicAsset = "Art/Themes/GrimRealm/Generated/Fonts/IMFellEnglish-Italic SDF.asset",
                LogoFile = "Art/Themes/GrimRealm/Fonts/UnifrakturCook-Bold.ttf",
                LogoAsset = "Art/Themes/GrimRealm/Generated/Fonts/UnifrakturCook-Bold SDF.asset",
                TitleOutline = new Color(0.04f, 0.04f, 0.05f, 1f),
                TitleShadow = new Color(0f, 0f, 0f, 0.85f),
                Outline = new Color(0.03f, 0.03f, 0.04f, 1f),
                LogoOutline = new Color(0.08f, 0.02f, 0.02f, 1f),
                LogoGlow = new Color(0.7f, 0.06f, 0.05f, 0.75f)
            },
            Portraits = new PortraitLook
            {
                KeyColor = new Color(0.86f, 0.88f, 0.94f),
                Key = 1.35f,
                KeyAngles = new Vector2(42f, -35f),
                FillColor = new Color(0.42f, 0.46f, 0.58f),
                Fill = 0.5f,
                RimColor = new Color(0.62f, 0.72f, 0.95f),
                Rim = 1.7f,
                AmbientSky = new Color(0.44f, 0.46f, 0.52f),
                AmbientEquator = new Color(0.34f, 0.34f, 0.36f),
                AmbientGround = new Color(0.16f, 0.16f, 0.17f),
                TownKey = 1.4f,
                Vignette = 0.3f
            },
            Materials = new SceneMaterials
            {
                Water = new Color(0.06f, 0.14f, 0.18f, 0.85f),
                FogInside = new Color(0.008f, 0.008f, 0.012f, 1f),
                FogEdge = new Color(0.1f, 0.1f, 0.14f, 1f),
                FogOutside = 0.62f,
                Marker = new Color(0.8f, 0.84f, 0.92f, 0.7f),
                Ring = new Color(0.9f, 0.92f, 1f, 0.9f),
                Pool = new Color(0.06f, 0.14f, 0.18f, 0.85f),
                Bog = new Color(0.1f, 0.12f, 0.07f, 0.92f)
            },
            DeadForests = true,
            BackdropExtras = new[]
            {
                (TerrainType.Grass, "KayKit/Halloween/tree_dead_large", 7f), (TerrainType.Grass, "KayKit/Buildings/Neutral/building_destroyed", 9f),
                (TerrainType.Dirt, "KayKit/Halloween/tree_dead_large", 7f), (TerrainType.Dirt, "KayKit/Halloween/crypt", 8f),
                (TerrainType.Rough, "KayKit/Buildings/Neutral/building_destroyed", 9f), (TerrainType.Rough, "KayKit/Halloween/tree_dead_medium", 6f),
                (TerrainType.Water, "KayKit/Halloween/tree_dead_large", 7f),
                (TerrainType.Swamp, "KayKit/Halloween/tree_dead_large_decorated", 7f), (TerrainType.Swamp, "KayKit/Halloween/crypt", 8f),
                (TerrainType.Snow, "KayKit/Halloween/tree_dead_large", 7f),
                (TerrainType.Sand, "KayKit/Buildings/Neutral/building_destroyed", 9f),
                (TerrainType.Wasteland, "KayKit/Halloween/tree_dead_large", 7f), (TerrainType.Wasteland, "KayKit/Halloween/arch", 6f),
                (TerrainType.Rock, "KayKit/Halloween/pillar", 5f)
            },
            Palette = new HeroesTheme.Palette
            {
                ink = new Color(0.86f, 0.84f, 0.78f),
                dim = new Color(0.6f, 0.58f, 0.54f),
                gold = new Color(0.76f, 0.8f, 0.88f),
                bad = new Color(0.85f, 0.3f, 0.25f),
                good = new Color(0.6f, 0.75f, 0.55f),
                inkOnParchment = new Color(0.12f, 0.1f, 0.1f),
                dimOnParchment = new Color(0.36f, 0.33f, 0.31f),
                headingTop = new Color(0.92f, 0.9f, 0.86f),
                headingBottom = new Color(0.6f, 0.62f, 0.68f),
                logoTop = new Color(0.96f, 0.93f, 0.86f),
                logoBottom = new Color(0.64f, 0.6f, 0.56f),
                accent = new Color(0.72f, 0.8f, 0.95f),
                accentMix = 1f,
                mapSaturation = 0.5f,
                mapValue = 0.8f,
                players = new[]
                {
                    new Color(0.64f, 0.1f, 0.1f), new Color(0.2f, 0.3f, 0.58f), new Color(0.22f, 0.42f, 0.24f),
                    new Color(0.6f, 0.5f, 0.28f), new Color(0.45f, 0.45f, 0.47f)
                }
            },
            Atmosphere = new HeroesTheme.Atmosphere
            {
                sun = new Color(0.82f, 0.84f, 0.9f),
                sunIntensity = 1.3f,
                shadowStrength = 0.65f,
                sunAngles = new Vector2(40f, 150f),
                ambientSky = new Color(0.42f, 0.45f, 0.52f),
                ambientEquator = new Color(0.36f, 0.37f, 0.4f),
                ambientGround = new Color(0.17f, 0.16f, 0.16f),
                fog = new Color(0.4f, 0.42f, 0.47f),
                fogStart = 70f,
                fogEnd = 250f
            },
            Glows = new HeroesTheme.Glows
            {
                warm = new Color(0.85f, 0.1f, 0.06f),
                cold = new Color(0.42f, 0.58f, 0.95f),
                mix = 0.6f
            }
        };
    }
}
