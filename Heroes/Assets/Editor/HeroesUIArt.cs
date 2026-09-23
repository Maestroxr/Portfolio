using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gamebox.Editor;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Draws the interface of the game in the manner of the old strategy games: dark leather bound in beveled gold,
    /// with gold corner guards like an old book's, parchment for what is read, wood for the bars along the edges of the
    /// screen, and buttons, slots, cards, bars, ribbons and portrait frames to match. Everything is drawn from code
    /// (so a rebuild writes the same bytes), with the grain of the leather, paper and wood taken from downloaded
    /// textures (ambientCG, Art/UI/Textures) and the key pattern of the parchment's corners from Kenney's Fantasy UI
    /// Borders (Art/UI/Borders). Every sprite is 100 pixels per unit, so one pixel is one unit of the 1920x1080 canvas,
    /// and the framed ones repeat their middle and edges (the grain of their middle tiles seamlessly), so a window as
    /// large as the screen keeps the fine grain of a card. The fonts, icons and pointers are in the other parts of
    /// this class.
    /// </summary>
    internal static partial class HeroesUIArt
    {
        // The colors of the interface: old gold on dark leather and stone, with parchment for what is read.
        public static readonly Color Gold = new Color(0.85f, 0.71f, 0.36f);
        public static readonly Color GoldDark = new Color(0.44f, 0.33f, 0.14f);
        public static readonly Color GoldLight = new Color(0.98f, 0.91f, 0.65f);
        public static readonly Color Leather = new Color(0.17f, 0.11f, 0.08f);
        public static readonly Color LeatherLight = new Color(0.27f, 0.18f, 0.12f);
        public static readonly Color Stone = new Color(0.28f, 0.26f, 0.23f);
        public static readonly Color Parchment = new Color(0.90f, 0.82f, 0.64f);
        public static readonly Color ParchmentDark = new Color(0.72f, 0.62f, 0.44f);
        public static readonly Color Ink = new Color(0.16f, 0.11f, 0.06f);

        private const string Out = "UI/Generated";

        /// <summary>Where the light comes from: the top left, as in every old game.</summary>
        private static readonly Vector2 Light = new Vector2(-0.5f, 0.866f);

        /// <summary>The near black of the lines between gold and leather.</summary>
        private static readonly Color Edge = new Color(0.07f, 0.045f, 0.025f, 0.95f);

        private static readonly Ramp GoldRamp = new Ramp(
            (0f, new Color(0.2f, 0.12f, 0.04f)), (0.28f, new Color(0.46f, 0.3f, 0.1f)), (0.52f, new Color(0.76f, 0.56f, 0.24f)),
            (0.74f, new Color(0.93f, 0.78f, 0.44f)), (0.9f, new Color(1f, 0.92f, 0.64f)), (1f, new Color(1f, 0.98f, 0.86f)));

        private static readonly Ramp BronzeRamp = new Ramp(
            (0f, new Color(0.12f, 0.07f, 0.03f)), (0.3f, new Color(0.3f, 0.19f, 0.08f)), (0.55f, new Color(0.56f, 0.39f, 0.17f)),
            (0.8f, new Color(0.78f, 0.6f, 0.3f)), (1f, new Color(0.95f, 0.82f, 0.52f)));

        private static readonly Ramp IronRamp = new Ramp(
            (0f, new Color(0.08f, 0.08f, 0.08f)), (0.35f, new Color(0.24f, 0.23f, 0.22f)), (0.6f, new Color(0.42f, 0.41f, 0.39f)),
            (0.85f, new Color(0.62f, 0.61f, 0.58f)), (1f, new Color(0.8f, 0.79f, 0.76f)));

        private static readonly Color LeatherDark = new Color(0.085f, 0.052f, 0.035f);
        private static readonly Color LeatherMid = new Color(0.25f, 0.155f, 0.1f);
        private static readonly Color Crimson = new Color(0.62f, 0.07f, 0.08f);

        [MenuItem("Heroes/Art/Interface", false, 42)]
        public static void BuildMenu()
        {
            HeroesArt art = HeroesAssets.Require<HeroesArt>("Art/HeroesArt.asset");
            BuildAll(art);
            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Draws the kit as the game would put it together into Logs/shots/uikit.png. Text drawn for the first time adds
        /// letters to the fonts, and the text of the open scene answers that by marking the scene changed although
        /// nothing in it is; a scene that was saved before is opened again from its file afterwards.
        /// </summary>
        [MenuItem("Heroes/Art/Interface Preview", false, 43)]
        public static void PreviewMenu()
        {
            UnityEngine.SceneManagement.Scene open = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool saved = open.IsValid() && !open.isDirty && !string.IsNullOrEmpty(open.path);
            Preview(HeroesAssets.Require<HeroesArt>("Art/HeroesArt.asset"), Path.Combine(Directory.GetCurrentDirectory(), "Logs", "shots", "uikit.png"));
            open = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (saved && open.isDirty && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(open.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
            }
        }

        public static void BuildAll(HeroesArt art)
        {
            art.insetSprites.Clear();
            art.insets.Clear();
            Kit(art);
            Fonts(art);
            Icons(art);
            Cursors(art);
            Credits(art);
        }

        private static void Kit(HeroesArt art)
        {
            art.frame = Frame(art);
            art.panel = Panel(art);
            art.parchment = ParchmentSprite(art);
            art.strip = Strip(art);
            art.card = Card(art, "Card", 0);
            art.cardHover = Card(art, "CardHover", 1);
            art.cardSelected = Card(art, "CardSelected", 2);
            art.inset = Recess(art);
            art.button = Button(art, "Button", 0);
            art.buttonHover = Button(art, "ButtonHover", 1);
            art.buttonPressed = Button(art, "ButtonPressed", 2);
            art.buttonDisabled = Button(art, "ButtonDisabled", 3);
            art.round = Round("Round", 0);
            art.roundHover = Round("RoundHover", 1);
            art.roundPressed = Round("RoundPressed", 2);
            art.slot = Slot(art, "Slot", 0);
            art.slotHover = Slot(art, "SlotHover", 1);
            art.slotSelected = Slot(art, "SlotSelected", 2);
            art.bar = Bar();
            art.barFrame = BarFrame(art);
            art.barFill = BarFill();
            art.tooltip = Tooltip(art);
            art.divider = Divider();
            art.ribbon = Ribbon(art);
            art.portraitFrame = PortraitFrame(art);
            art.portraitFrameRound = PortraitFrameRound();
            art.portraitBack = PortraitBack(false);
            art.portraitBackRound = PortraitBack(true);
            art.pennant = Pennant();
            art.pennantTrim = PennantTrim();
            art.shade = Shade();
            art.vignette = Vignette();
            art.checkBox = CheckBox(art);
            art.checkMark = CheckMark();
            art.knob = Knob();
            art.glow = Glow();
            art.star = Star(true);
            art.starEmpty = Star(false);
            art.banner = Banner();
        }

        // ------------------------------------------------------------------ saving

        private static Sprite Save(Raster raster, string name, Vector4 border)
        {
            string relative = $"{Out}/{name}.png";
            HeroesAssets.SaveTexture(raster.ToTexture(), relative, importer => HeroesAssets.SpriteImport(importer, border));
            return HeroesAssets.Sprite(relative);
        }

        /// <summary>Saves a framed sprite and records how far in its content goes, where that is not its border.</summary>
        private static Sprite Save(HeroesArt art, Raster raster, string name, Vector4 border, Vector4 inset)
        {
            Sprite sprite = Save(raster, name, border);
            if (sprite != null)
            {
                art.insetSprites.Add(sprite);
                art.insets.Add(inset);
            }
            return sprite;
        }

        private static Vector4 All(float value)
        {
            return new Vector4(value, value, value, value);
        }

        // ------------------------------------------------------------------ windows and panels

        /// <summary>
        /// The window: leather bound in a band of beveled gold, a thin gold line inside it that winds into Kenney's key
        /// pattern at every corner with a red stone set in the loop. The border is wide for the corners, the band
        /// itself narrow.
        /// </summary>
        private static Sprite Frame(HeroesArt art)
        {
            const int n = 256;
            const int b = 48;
            const int s = n + 2 * b;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf Box(float inset, float radius) => p => Sd.Box(p, c, new Vector2(s / 2f - inset, s / 2f - inset), radius);
            Sdf outer = Box(1f, 5f);
            Sdf inner = Box(13f, 3f);
            raster.Fill(Box(11f, 3f), LeatherShader(LeatherDark, LeatherMid, n, new Vector2(b, b)), all);
            InnerShadow(raster, inner, 16f, 0.8f, all);
            Sdf band = p => Mathf.Max(outer(p), -inner(p));
            Metal(raster, band, all, 4.5f, GoldRamp, 0.6f, 0.45f);
            // A line engraved along the middle of the band.
            raster.Fill(p => Sd.Outline(Box(7f, 4f)(p), 1f), new Color(0.28f, 0.17f, 0.05f, 0.6f), all);
            raster.Fill(p => Sd.Outline(Box(7.9f, 4f)(p), 0.6f), new Color(1f, 0.93f, 0.7f, 0.35f), all);
            raster.Fill(p => Sd.Outline(outer(p), 1.6f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.6f), Edge, all);
            Fretwork(raster, s, 17f, 0.9f, all);
            return Save(art, raster, "Frame", All(b), All(18f));
        }

        /// <summary>A section inside a window: a narrow rim of dark gold around darker leather, a rivet in each corner.</summary>
        private static Sprite Panel(HeroesArt art)
        {
            const int n = 256;
            const int b = 18;
            const int s = n + 2 * b;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf Box(float inset, float radius) => p => Sd.Box(p, c, new Vector2(s / 2f - inset, s / 2f - inset), radius);
            Sdf outer = Box(1f, 4f);
            Sdf inner = Box(7f, 2f);
            raster.Fill(Box(5f, 2f), LeatherShader(new Color(0.06f, 0.04f, 0.03f), new Color(0.17f, 0.11f, 0.075f), n, new Vector2(b, b), 0.8f), all);
            InnerShadow(raster, inner, 10f, 0.7f, all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 2.5f, BronzeRamp, 0.62f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.4f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.3f), Edge, all);
            foreach (Vector2 corner in Corners(s))
            {
                Vector2 at = corner + new Vector2(Mathf.Sign(c.x - corner.x), Mathf.Sign(c.y - corner.y)) * 4.2f;
                Stud(raster, at, 2.6f, GoldRamp);
            }
            return Save(art, raster, "Panel", All(b), All(9f));
        }

        /// <summary>
        /// Parchment: the downloaded paper's fibres in the colors of old vellum, stained, darker and burnt toward its
        /// edge, with a key pattern printed in brown ink in its corners.
        /// </summary>
        private static Sprite ParchmentSprite(HeroesArt art)
        {
            const int n = 256;
            const int b = 40;
            const int s = n + 2 * b;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf shape = p => Sd.Box(p, c, new Vector2(s / 2f - 1f, s / 2f - 1f), 3f);
            Swatch paper = Texture("Paper006_Color", n);
            Swatch fibre = Texture("Paper006_Displacement", n);
            raster.Fill(shape, p =>
            {
                float x = p.x - b;
                float y = p.y - b;
                float tone = (paper.Height(x, y) - 0.5f) * 1.6f + (fibre.Height(x, y) - 0.5f) * 1.2f;
                float stain = Periodic(x, y, n, 3, 21) * 0.55f + Periodic(x, y, n, 7, 22) * 0.3f;
                float slope = -(fibre.Height(x + 1f, y) - fibre.Height(x - 1f, y)) * Light.x - (fibre.Height(x, y + 1f) - fibre.Height(x, y - 1f)) * Light.y;
                Color color = Color.Lerp(new Color(0.8f, 0.68f, 0.48f), new Color(0.95f, 0.88f, 0.7f), Mathf.Clamp01(0.62f + tone * 0.35f + stain * 0.22f));
                color *= 1f + slope * 1.6f;
                // Burnt toward the edge.
                float burn = Mathf.Clamp01(1f + shape(p) / 22f);
                color = Color.Lerp(color, new Color(0.45f, 0.3f, 0.16f), burn * burn * 0.75f);
                color.a = 1f;
                return color;
            }, all);
            raster.Fill(p => Sd.Outline(shape(p), 1.5f), new Color(0.3f, 0.19f, 0.09f, 0.95f), all);
            // Printed corners: the key of Kenney's border, in brown ink.
            bool[,] key = Pattern("panel-border-019", 32);
            if (key != null)
            {
                foreach (Vector2 corner in Corners(s))
                {
                    Vector2 inward = new Vector2(Mathf.Sign(c.x - corner.x), Mathf.Sign(c.y - corner.y));
                    Sdf ink = Stencil(key, corner + inward * 4f, inward, 1f);
                    raster.Fill(ink, new Color(0.36f, 0.22f, 0.1f, 0.78f), Sd.Around(corner + inward * 20f, 19f));
                }
            }
            return Save(art, raster, "Parchment", All(b), All(20f));
        }

        /// <summary>A bar along an edge of the screen: dark planks between two beveled gold bands, a narrower one at the ends.</summary>
        private static Sprite Strip(HeroesArt art)
        {
            const int n = 256;
            const int m = 48;
            const int side = 16;
            const int band = 16;
            const int w = n + 2 * side;
            const int h = m + 2 * band;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            var c = new Vector2(w / 2f, h / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(w / 2f - 1f, h / 2f - 1f), 3f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(w / 2f - 6f, h / 2f - 9f), 1.5f);
            Swatch wood = Texture("Planks023A_Color", n, m);
            raster.Fill(outer, p =>
            {
                float x = p.x - side;
                float y = p.y - band;
                float v = wood.Height(x, y);
                Color color = Color.Lerp(new Color(0.05f, 0.03f, 0.02f), new Color(0.3f, 0.19f, 0.11f), Mathf.Clamp01((v - 0.2f) * 1.6f));
                color.a = 1f;
                return color;
            }, all);
            InnerShadow(raster, inner, 7f, 0.75f, all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 3f, GoldRamp, 0.58f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.5f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.4f), Edge, all);
            return Save(art, raster, "Strip", new Vector4(side, band, side, band), new Vector4(8f, 10f, 8f, 10f));
        }

        /// <summary>A small card or a row of a list: a narrow gold rim on leather; brighter when hovered, glowing when picked.</summary>
        private static Sprite Card(HeroesArt art, string name, int state)
        {
            const int n = 128;
            const int b = 18;
            const int s = n + 2 * b;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf Box(float inset, float radius) => p => Sd.Box(p, c, new Vector2(s / 2f - inset, s / 2f - inset), radius);
            Sdf outer = Box(1f, 4f);
            Sdf inner = Box(6f, 2.5f);
            Color dark = state == 0 ? new Color(0.09f, 0.06f, 0.045f) : state == 1 ? new Color(0.13f, 0.09f, 0.06f) : new Color(0.15f, 0.1f, 0.06f);
            Color light = state == 0 ? new Color(0.22f, 0.145f, 0.1f) : state == 1 ? new Color(0.3f, 0.2f, 0.13f) : new Color(0.32f, 0.21f, 0.12f);
            raster.Fill(Box(4f, 2f), LeatherShader(dark, light, n, new Vector2(b, b), 0.8f), all);
            InnerShadow(raster, inner, 8f, 0.6f, all);
            if (state == 2)
            {
                // The warm light of a picked card, inside its rim.
                raster.Fill(inner, p => new Color(1f, 0.78f, 0.35f, 0.55f * Mathf.Pow(1f - Mathf.Clamp01(-inner(p) / 11f), 2f)), all);
            }
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 2.2f, state == 0 ? BronzeRamp : GoldRamp,
                state == 0 ? 0.64f : state == 1 ? 0.6f : 0.72f, 0.42f);
            raster.Fill(p => Sd.Outline(outer(p), 1.3f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.2f), Edge, all);
            return Save(art, raster, name, All(b), All(7f));
        }

        /// <summary>A box sunk into a panel for a figure that must read at once: near black, shadowed from above, a thin rim.</summary>
        private static Sprite Recess(HeroesArt art)
        {
            const int w = 96;
            const int h = 48;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            var c = new Vector2(w / 2f, h / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(w / 2f - 1f, h / 2f - 1f), 5f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(w / 2f - 3.5f, h / 2f - 3.5f), 3.5f);
            raster.Fill(outer, p => Color.Lerp(new Color(0.02f, 0.015f, 0.01f), new Color(0.07f, 0.05f, 0.035f), p.y / h * 0.3f + 0.2f), all);
            // Shadow falls from the top left edge into the box.
            raster.Fill(inner, p =>
            {
                float d = -inner(p);
                Vector2 g = Gradient(inner, p);
                float facing = Mathf.Clamp01(Vector2.Dot(g, Light));
                return new Color(0f, 0f, 0f, 0.8f * facing * Mathf.Pow(1f - Mathf.Clamp01(d / 7f), 2f));
            }, all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 1.5f, BronzeRamp, 0.45f, -0.5f);
            raster.Fill(p => Sd.Outline(outer(p), 1.1f), Edge, all);
            return Save(art, raster, "Inset", All(12f), new Vector4(8f, 5f, 8f, 5f));
        }

        // ------------------------------------------------------------------ buttons and slots

        /// <summary>
        /// A button: dark red lacquered leather inside a beveled gold rim, with a gloss on its upper half. Hovered it
        /// warms and brightens, pressed it sinks and darkens, off it is grey under a rim of iron.
        /// </summary>
        private static Sprite Button(HeroesArt art, string name, int state)
        {
            const int w = 160;
            const int h = 64;
            const int border = 22;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            var c = new Vector2(w / 2f, h / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(w / 2f - 1f, h / 2f - 1f), 8f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(w / 2f - 6f, h / 2f - 6f), 4.5f);
            Color top = state == 1 ? new Color(0.58f, 0.21f, 0.12f) : state == 2 ? new Color(0.2f, 0.07f, 0.045f)
                : state == 3 ? new Color(0.24f, 0.22f, 0.2f) : new Color(0.44f, 0.15f, 0.09f);
            Color bottom = state == 1 ? new Color(0.3f, 0.1f, 0.06f) : state == 2 ? new Color(0.32f, 0.11f, 0.07f)
                : state == 3 ? new Color(0.13f, 0.12f, 0.11f) : new Color(0.21f, 0.065f, 0.04f);
            Swatch grain = Texture("Leather037_Displacement", 128);
            raster.Fill(outer, p =>
            {
                float t = Mathf.Clamp01((p.y - 6f) / (h - 12f));
                Color color = Color.Lerp(bottom, top, t);
                color *= 1f + (grain.Height(p.x, p.y) - 0.5f) * 0.18f;
                color.a = 1f;
                return color;
            }, all);
            if (state == 2)
            {
                raster.Fill(inner, p => new Color(0f, 0f, 0f, 0.7f * Mathf.Pow(1f - Mathf.Clamp01((h - 6f - p.y) / 10f), 2f)), all);
            }
            else
            {
                // A gloss over the upper half, and a warm light along the rim when hovered.
                raster.Fill(p => Mathf.Max(inner(p), -(p.y - h * 0.52f)), p => new Color(1f, 0.92f, 0.8f, 0.1f + 0.08f * Mathf.Clamp01((p.y - h * 0.52f) / (h * 0.4f))), all);
                if (state == 1)
                {
                    raster.Fill(inner, p => new Color(1f, 0.75f, 0.35f, 0.45f * Mathf.Pow(1f - Mathf.Clamp01(-inner(p) / 8f), 2f)), all);
                }
            }
            Ramp ramp = state == 3 ? IronRamp : GoldRamp;
            float level = state == 1 ? 0.7f : state == 2 ? 0.5f : state == 3 ? 0.55f : 0.6f;
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 2.8f, ramp, level, state == 2 ? -0.35f : 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.5f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.3f), new Color(0.05f, 0.02f, 0.01f, 0.9f), all);
            return Save(art, raster, name, All(border), new Vector4(12f, 8f, 12f, 8f));
        }

        /// <summary>The round button of an icon command: a gold ring around a dark leather disc.</summary>
        private static Sprite Round(string name, int state)
        {
            const int size = 80;
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            var c = new Vector2(size / 2f, size / 2f);
            Sdf disc = p => Sd.Circle(p, c, size / 2f - 1.5f);
            Sdf hole = p => Sd.Circle(p, c, size / 2f - 7.5f);
            Color dark = state == 1 ? new Color(0.14f, 0.09f, 0.06f) : state == 2 ? new Color(0.04f, 0.03f, 0.02f) : new Color(0.08f, 0.055f, 0.04f);
            Color light = state == 1 ? new Color(0.38f, 0.25f, 0.16f) : state == 2 ? new Color(0.14f, 0.1f, 0.07f) : new Color(0.27f, 0.18f, 0.12f);
            raster.Fill(disc, p =>
            {
                float t = Mathf.Clamp01(0.5f + Vector2.Dot((p - c) / (size * 0.5f), Light) * 0.6f);
                return Color.Lerp(dark, light, state == 2 ? 1f - t : t);
            }, all);
            raster.Fill(hole, p => new Color(0f, 0f, 0f, 0.6f * Mathf.Pow(1f - Mathf.Clamp01(-hole(p) / 8f), 2f)), all);
            Metal(raster, p => Mathf.Max(disc(p), -hole(p)), all, 2.6f, GoldRamp, state == 1 ? 0.72f : state == 2 ? 0.5f : 0.6f, state == 2 ? -0.3f : 0.45f);
            raster.Fill(p => Sd.Outline(disc(p), 1.4f), Edge, all);
            raster.Fill(p => Sd.Outline(hole(p), 1.3f), Edge, all);
            return Save(raster, name, Vector4.zero);
        }

        /// <summary>The square an army slot is shown in: sunk into the panel under a bronze rim, gold when hovered or picked.</summary>
        private static Sprite Slot(HeroesArt art, string name, int state)
        {
            const int size = 80;
            const int border = 14;
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            var c = new Vector2(size / 2f, size / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(size / 2f - 1f, size / 2f - 1f), 5f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(size / 2f - 5.5f, size / 2f - 5.5f), 3f);
            raster.Fill(outer, p =>
            {
                float r = (p - c).magnitude / (size * 0.5f);
                return Color.Lerp(new Color(0.12f, 0.1f, 0.085f), new Color(0.035f, 0.028f, 0.022f), Mathf.Clamp01(r));
            }, all);
            raster.Fill(inner, p =>
            {
                float d = -inner(p);
                float facing = Mathf.Clamp01(Vector2.Dot(Gradient(inner, p), Light) * 0.7f + 0.3f);
                return new Color(0f, 0f, 0f, 0.85f * facing * Mathf.Pow(1f - Mathf.Clamp01(d / 12f), 2f));
            }, all);
            if (state == 2)
            {
                raster.Fill(inner, p => new Color(1f, 0.8f, 0.36f, 0.7f * Mathf.Pow(1f - Mathf.Clamp01(-inner(p) / 12f), 2.2f)), all);
            }
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 2.2f, state == 0 ? BronzeRamp : GoldRamp,
                state == 0 ? 0.6f : state == 1 ? 0.64f : 0.8f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.3f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.2f), Edge, all);
            return Save(art, raster, name, All(border), All(6f));
        }

        // ------------------------------------------------------------------ bars

        /// <summary>A plain rounded white bar, tinted where it is used.</summary>
        private static Sprite Bar()
        {
            const int width = 32;
            const int height = 16;
            var raster = new Raster(width, height, Color.clear);
            var all = new Rect(0, 0, width, height);
            Sdf shape = p => Sd.Box(p, new Vector2(width / 2f, height / 2f), new Vector2(width / 2f - 1f, height / 2f - 1f), 3f);
            raster.Fill(shape, p => Color.Lerp(new Color(1f, 1f, 1f, 1f), new Color(0.75f, 0.75f, 0.75f, 1f), 1f - p.y / height), all);
            raster.Fill(p => Sd.Box(p, new Vector2(width / 2f, height - 4f), new Vector2(width / 2f - 3f, 1.5f), 1f), new Color(1f, 1f, 1f, 0.35f), all);
            return Save(raster, "Bar", All(5f));
        }

        /// <summary>The trough of a bar: sunk and near black inside a thin gold rim.</summary>
        private static Sprite BarFrame(HeroesArt art)
        {
            const int w = 96;
            const int h = 24;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            var c = new Vector2(w / 2f, h / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(w / 2f - 1f, h / 2f - 1f), 5f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(w / 2f - 4f, h / 2f - 4f), 3f);
            raster.Fill(outer, new Color(0.03f, 0.022f, 0.016f), all);
            raster.Fill(inner, p => new Color(0f, 0f, 0f, 0.7f * Mathf.Clamp01((p.y - h * 0.5f) / (h * 0.4f))), all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 1.6f, GoldRamp, 0.6f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.2f), Edge, all);
            return Save(art, raster, "BarFrame", All(10f), All(4f));
        }

        /// <summary>The fill of a bar: white, rounded, lit from above; tinted where it is used.</summary>
        private static Sprite BarFill()
        {
            const int w = 64;
            const int h = 16;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            var c = new Vector2(w / 2f, h / 2f);
            Sdf shape = p => Sd.Box(p, c, new Vector2(w / 2f - 0.5f, h / 2f - 0.5f), 3f);
            raster.Fill(shape, p =>
            {
                float t = p.y / h;
                float v = t > 0.62f ? Mathf.Lerp(0.92f, 1f, (t - 0.62f) / 0.38f) : Mathf.Lerp(0.55f, 0.92f, t / 0.62f);
                return new Color(v, v, v, 1f);
            }, all);
            raster.Fill(p => Sd.Box(p, new Vector2(w / 2f, h - 3.5f), new Vector2(w / 2f - 3f, 0.9f), 0.9f), new Color(1f, 1f, 1f, 0.6f), all);
            return Save(raster, "BarFill", All(6f));
        }

        // ------------------------------------------------------------------ boxes, rules and banners

        /// <summary>The box a tooltip is shown in: dark leather under a thin gold rim.</summary>
        private static Sprite Tooltip(HeroesArt art)
        {
            const int s = 96;
            const int b = 16;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(s / 2f - 1f, s / 2f - 1f), 6f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(s / 2f - 4.5f, s / 2f - 4.5f), 3.5f);
            raster.Fill(outer, p => Color.Lerp(new Color(0.06f, 0.04f, 0.03f, 0.96f), new Color(0.15f, 0.1f, 0.07f, 0.96f), Mathf.Clamp01(p.y / s)), all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 1.6f, GoldRamp, 0.62f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.2f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1f), Edge, all);
            return Save(art, raster, "Tooltip", All(b), All(8f));
        }

        /// <summary>
        /// Half of a rule: a beveled gold line fading in from the left, ending on the right in half a lozenge. Two of
        /// them, one turned over, make the whole rule (UIKit.Divider).
        /// </summary>
        private static Sprite Divider()
        {
            const int w = 192;
            const int h = 24;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            float mid = h / 2f;
            Sdf line = p => Sd.Box(p, new Vector2(w / 2f - 4f, mid), new Vector2(w / 2f - 4f, 1.6f), 1f);
            Sdf lozenge = p => Sd.Polygon(p, new[] { new Vector2(w - 11f, mid), new Vector2(w, mid + 10f), new Vector2(w + 11f, mid), new Vector2(w, mid - 10f) });
            Sdf knob = p => Sd.Circle(p, new Vector2(w - 20f, mid), 3.4f);
            Sdf shape = p => Mathf.Min(line(p), Mathf.Min(lozenge(p), knob(p)));
            var fade = new Raster(w, h, Color.clear);
            fade.Fill(p => Sd.Outline(shape(p), 2.4f), new Color(0.06f, 0.035f, 0.02f, 0.85f), all);
            Metal(fade, shape, all, 1.4f, GoldRamp, 0.62f, 0.5f);
            // The line fades in from nothing at the left end.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color color = fade.Get(x, y);
                    color.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x / (w * 0.55f)));
                    raster.Set(x, y, color);
                }
            }
            return Save(raster, "Divider", new Vector4(0f, 0f, 28f, 0f));
        }

        /// <summary>The crimson banner a title is written on, gold at its edges, its tails folded behind it at both ends.</summary>
        private static Sprite Ribbon(HeroesArt art)
        {
            const int w = 320;
            const int h = 80;
            const int end = 62;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            float bandLow = 16f;
            float bandHigh = h - 12f;
            // The tails, lower and behind, with a notch cut into their ends.
            foreach (int side in new[] { -1, 1 })
            {
                float x0 = side < 0 ? 2f : w - 2f;
                float x1 = side < 0 ? end - 4f : w - end + 4f;
                var tail = new[]
                {
                    new Vector2(x0, bandLow - 12f), new Vector2(x1, bandLow - 12f), new Vector2(x1, bandHigh - 14f),
                    new Vector2(x0, bandHigh - 14f), new Vector2(x0 - side * 16f, (bandLow + bandHigh) * 0.5f - 13f)
                };
                Sdf tailShape = p => Sd.Polygon(p, tail);
                raster.Fill(tailShape, p => Color.Lerp(new Color(0.22f, 0.02f, 0.03f), new Color(0.4f, 0.05f, 0.06f), (p.y - bandLow + 12f) / (bandHigh - bandLow)), all);
                raster.Fill(p => Sd.Outline(tailShape(p), 1.4f), Edge, all);
                // The fold where the tail turns under the band.
                var fold = new[] { new Vector2(x1, bandLow - 12f), new Vector2(x1 + side * 12f, bandLow), new Vector2(x1, bandLow) };
                raster.Fill(p => Sd.Polygon(p, fold), new Color(0.12f, 0.01f, 0.015f), all);
            }
            Sdf bandShape = p => Sd.Box(p, new Vector2(w / 2f, (bandLow + bandHigh) * 0.5f), new Vector2(w / 2f - end + 12f, (bandHigh - bandLow) * 0.5f), 1.5f);
            raster.Fill(bandShape, p =>
            {
                float t = (p.y - bandLow) / (bandHigh - bandLow);
                // Cloth: darker toward its edges, a soft sheen above the middle.
                float v = 0.55f + 0.45f * Mathf.Sin(t * Mathf.PI) + 0.08f * Mathf.Exp(-Mathf.Pow((t - 0.66f) * 6f, 2f));
                Color color = Crimson * v;
                color.a = 1f;
                return color;
            }, all);
            foreach (float y in new[] { bandLow + 3.2f, bandHigh - 3.2f })
            {
                float yy = y;
                Sdf edge = p => Sd.Box(p, new Vector2(w / 2f, yy), new Vector2(w / 2f - end + 12f, 1.9f), 0.8f);
                Metal(raster, edge, all, 1f, GoldRamp, 0.66f, 0.4f);
            }
            raster.Fill(p => Sd.Outline(bandShape(p), 1.4f), Edge, all);
            return Save(art, raster, "Ribbon", new Vector4(end, 0f, end, 0f), new Vector4(end - 6f, 22f, end - 6f, 16f));
        }

        // ------------------------------------------------------------------ portraits and banners

        /// <summary>A gold frame around a portrait, empty in its middle; small bosses at its corners.</summary>
        private static Sprite PortraitFrame(HeroesArt art)
        {
            const int s = 128;
            const int b = 16;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(s / 2f - 1f, s / 2f - 1f), 4f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(s / 2f - 8f, s / 2f - 8f), 2f);
            raster.Fill(inner, p => new Color(0f, 0f, 0f, 0.65f * Mathf.Pow(1f - Mathf.Clamp01(-inner(p) / 9f), 2f)), all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 3f, GoldRamp, 0.6f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.4f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.4f), Edge, all);
            foreach (Vector2 corner in Corners(s))
            {
                Vector2 at = corner + new Vector2(Mathf.Sign(c.x - corner.x), Mathf.Sign(c.y - corner.y)) * 4.5f;
                Stud(raster, at, 3.6f, GoldRamp);
            }
            return Save(art, raster, "PortraitFrame", All(b), All(8f));
        }

        private static Sprite PortraitFrameRound()
        {
            const int s = 128;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf outer = p => Sd.Circle(p, c, s / 2f - 1.5f);
            Sdf inner = p => Sd.Circle(p, c, s / 2f - 8.5f);
            raster.Fill(inner, p => new Color(0f, 0f, 0f, 0.6f * Mathf.Pow(1f - Mathf.Clamp01(-inner(p) / 9f), 2f)), all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 3f, GoldRamp, 0.6f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.4f), Edge, all);
            raster.Fill(p => Sd.Outline(inner(p), 1.4f), Edge, all);
            return Save(raster, "PortraitFrameRound", Vector4.zero);
        }

        /// <summary>The backdrop of a portrait: dark, a little lighter and warmer behind the head.</summary>
        private static Sprite PortraitBack(bool round)
        {
            const int s = 128;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf shape = round ? p => Sd.Circle(p, c, s / 2f - 2f) : (Sdf)(p => Sd.Box(p, c, new Vector2(s / 2f - 2f, s / 2f - 2f), 3f));
            raster.Fill(shape, p =>
            {
                float r = (p - new Vector2(s * 0.5f, s * 0.6f)).magnitude / (s * 0.7f);
                Color color = Color.Lerp(new Color(0.3f, 0.23f, 0.17f), new Color(0.07f, 0.055f, 0.045f), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(r)));
                color.a = 1f;
                return color;
            }, all);
            raster.Grain(all, 0.02f, 515);
            return Save(raster, round ? "PortraitBackRound" : "PortraitBack", Vector4.zero);
        }

        /// <summary>The cloth of a pennant, in white and shades of grey so a player's color tints it; a swallow tail below.</summary>
        private static Sprite Pennant()
        {
            const int w = 64;
            const int h = 128;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            var cloth = new[]
            {
                new Vector2(9f, h - 8f), new Vector2(w - 9f, h - 8f), new Vector2(w - 9f, 8f), new Vector2(w / 2f, 30f), new Vector2(9f, 8f)
            };
            Sdf shape = p => Sd.Polygon(p, cloth);
            raster.Fill(shape, p =>
            {
                float fold = 0.86f + 0.14f * Mathf.Sin((p.x - 9f) / (w - 18f) * Mathf.PI * 2.5f + 0.6f);
                float shade = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(p.y / h + 0.25f));
                float v = fold * shade;
                return new Color(v, v, v, 1f);
            }, all);
            raster.Fill(shape, p => new Color(0f, 0f, 0f, 0.45f * Mathf.Pow(1f - Mathf.Clamp01(-shape(p) / 5f), 2f)), all);
            raster.Fill(p => Sd.Outline(shape(p), 1.3f), new Color(0.1f, 0.07f, 0.05f, 0.9f), all);
            return Save(raster, "Pennant", Vector4.zero);
        }

        /// <summary>What is drawn over the pennant's cloth in gold: the pole it hangs from and the braid along its edges.</summary>
        private static Sprite PennantTrim()
        {
            const int w = 64;
            const int h = 128;
            var raster = new Raster(w, h, Color.clear);
            var all = new Rect(0, 0, w, h);
            Sdf pole = p => Sd.Box(p, new Vector2(w / 2f, h - 6f), new Vector2(w / 2f - 6f, 2.6f), 2.2f);
            Sdf knobs = p => Mathf.Min(Sd.Circle(p, new Vector2(5f, h - 6f), 4.2f), Sd.Circle(p, new Vector2(w - 5f, h - 6f), 4.2f));
            var cloth = new[]
            {
                new Vector2(9f, h - 8f), new Vector2(w - 9f, h - 8f), new Vector2(w - 9f, 8f), new Vector2(w / 2f, 30f), new Vector2(9f, 8f)
            };
            Sdf braid = p => Sd.Outline(Sd.Polygon(p, cloth) + 3f, 2.2f);
            raster.Fill(p => Sd.Outline(braid(p), 1.2f), new Color(0.1f, 0.06f, 0.02f, 0.6f), all);
            Metal(raster, braid, all, 1f, GoldRamp, 0.62f, 0.4f);
            raster.Fill(p => Mathf.Min(pole(p), knobs(p)) - 1.2f, Edge, all);
            Metal(raster, p => Mathf.Min(pole(p), knobs(p)), all, 1.8f, GoldRamp, 0.62f, 0.45f);
            return Save(raster, "PennantTrim", Vector4.zero);
        }

        /// <summary>Black fading out toward the top, for the shade behind a dialog or along the bottom of the screen.</summary>
        private static Sprite Shade()
        {
            const int w = 16;
            const int h = 256;
            var raster = new Raster(w, h, Color.clear);
            for (int y = 0; y < h; y++)
            {
                float a = Mathf.Lerp(1f, 0.55f, Mathf.SmoothStep(0f, 1f, y / (h - 1f)));
                for (int x = 0; x < w; x++)
                {
                    raster.Set(x, y, new Color(0f, 0f, 0f, a));
                }
            }
            return Save(raster, "Shade", Vector4.zero);
        }

        /// <summary>Black gathering in the corners and clear in the middle.</summary>
        private static Sprite Vignette()
        {
            const int s = 256;
            var raster = new Raster(s, s, Color.clear);
            var c = new Vector2(s / 2f, s / 2f);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float r = (new Vector2(x + 0.5f, y + 0.5f) - c).magnitude / (s * 0.5f * 1.4142f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - 0.35f) / 0.65f)) * 0.9f;
                    raster.Set(x, y, new Color(0f, 0f, 0f, a));
                }
            }
            return Save(raster, "Vignette", Vector4.zero);
        }

        // ------------------------------------------------------------------ small controls

        private static Sprite CheckBox(HeroesArt art)
        {
            const int s = 48;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            Sdf outer = p => Sd.Box(p, c, new Vector2(s / 2f - 1f, s / 2f - 1f), 4f);
            Sdf inner = p => Sd.Box(p, c, new Vector2(s / 2f - 4.5f, s / 2f - 4.5f), 2f);
            raster.Fill(outer, new Color(0.04f, 0.03f, 0.022f), all);
            raster.Fill(inner, p => new Color(0f, 0f, 0f, 0.8f * Mathf.Pow(1f - Mathf.Clamp01(-inner(p) / 8f), 2f) * Mathf.Clamp01(Vector2.Dot(Gradient(inner, p), Light) + 0.4f)), all);
            Metal(raster, p => Mathf.Max(outer(p), -inner(p)), all, 1.8f, GoldRamp, 0.6f, 0.45f);
            raster.Fill(p => Sd.Outline(outer(p), 1.2f), Edge, all);
            return Save(art, raster, "CheckBox", All(10f), All(5f));
        }

        private static Sprite CheckMark()
        {
            const int s = 48;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            Sdf mark = p => Mathf.Min(Sd.Segment(p, new Vector2(11f, 25f), new Vector2(20f, 14f), 3.6f),
                Sd.Segment(p, new Vector2(20f, 14f), new Vector2(37f, 36f), 3.6f));
            raster.Fill(p => mark(p) - 1.8f, Edge, all);
            Metal(raster, mark, all, 1.8f, GoldRamp, 0.7f, 0.4f);
            return Save(raster, "CheckMark", Vector4.zero);
        }

        /// <summary>The knob of a slider: a gold boss with a dark center.</summary>
        private static Sprite Knob()
        {
            const int s = 48;
            var raster = new Raster(s, s, Color.clear);
            var all = new Rect(0, 0, s, s);
            var c = new Vector2(s / 2f, s / 2f);
            raster.Shadow(p => Sd.Circle(p, c, s / 2f - 5f), new Color(0f, 0f, 0f, 0.6f), 3f, new Vector2(1.5f, -2f), all);
            Metal(raster, p => Sd.Circle(p, c, s / 2f - 4f), all, 5f, GoldRamp, 0.6f, 0.45f);
            raster.Fill(p => Sd.Outline(Sd.Circle(p, c, s / 2f - 4f), 1.3f), Edge, all);
            Gem(raster, c, 5f, Crimson);
            return Save(raster, "Knob", Vector4.zero);
        }

        private static Sprite Glow()
        {
            const int size = 128;
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    raster.Set(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
                }
            }
            return Save(raster, "Glow", Vector4.zero);
        }

        private static Sprite Star(bool filled)
        {
            const int size = 64;
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            var center = new Vector2(size / 2f, size / 2f - 1f);
            Sdf star = p => Sd.Star(p, center, size * 0.44f, 5, 0.46f);
            if (filled)
            {
                raster.Shadow(star, new Color(0f, 0f, 0f, 0.55f), 2.5f, new Vector2(1f, -1.5f), all);
                raster.Fill(p => star(p) - 1.6f, Edge, all);
                Metal(raster, star, all, 5f, GoldRamp, 0.68f, 0.4f);
            }
            else
            {
                raster.Fill(star, new Color(0.08f, 0.06f, 0.04f, 0.6f), all);
                raster.Fill(p => Sd.Outline(star(p), 2.2f), new Color(0.5f, 0.4f, 0.24f, 0.9f), all);
            }
            return Save(raster, filled ? "Star" : "StarEmpty", Vector4.zero);
        }

        private static Sprite Banner()
        {
            const int width = 96;
            const int height = 64;
            var raster = new Raster(width, height, Color.clear);
            var all = new Rect(0, 0, width, height);
            var points = new List<Vector2>
            {
                new Vector2(4, height - 4), new Vector2(width - 4, height - 4), new Vector2(width - 4, 14),
                new Vector2(width / 2f, 4), new Vector2(4, 14)
            };
            Sdf shape = p => Sd.Polygon(p, points);
            raster.Fill(shape, p => Color.Lerp(new Color(0.35f, 0.08f, 0.08f), new Color(0.55f, 0.14f, 0.12f), p.y / height), all);
            raster.Grain(all, 0.03f, 99);
            raster.Fill(p => Sd.Outline(shape(p), 2f), Gold, all);
            return Save(raster, "Banner", new Vector4(12, 20, 12, 12));
        }

        // ------------------------------------------------------------------ ornaments

        private static Vector2[] Corners(float size)
        {
            return new[] { new Vector2(0f, 0f), new Vector2(size, 0f), new Vector2(0f, size), new Vector2(size, size) };
        }

        /// <summary>
        /// A thin line of raised gold all around a square sprite, <paramref name="inset"/> pixels in from its edge, that
        /// winds at every corner into the key of Kenney's Fantasy UI Borders (panel-border-019) with a red stone in the
        /// loop. The straight runs are the same all along, so the edges of the sprite still tile.
        /// </summary>
        private static void Fretwork(Raster raster, int size, float inset, float scale, Rect bounds)
        {
            bool[,] key = Pattern("panel-border-019", 24);
            if (key == null)
            {
                return;
            }
            float stroke = 4f * scale;
            float cut = 12f * scale;
            var c = new Vector2(size / 2f, size / 2f);
            float half = size / 2f - inset - stroke / 2f;
            // The straight runs, from the end of one corner's key to the next.
            Sdf runs = p =>
            {
                Vector2 q = p - c;
                float along = half - cut;
                float top = Sd.Box(q, new Vector2(0f, half), new Vector2(along, stroke / 2f));
                float bottom = Sd.Box(q, new Vector2(0f, -half), new Vector2(along, stroke / 2f));
                float left = Sd.Box(q, new Vector2(-half, 0f), new Vector2(stroke / 2f, along));
                float right = Sd.Box(q, new Vector2(half, 0f), new Vector2(stroke / 2f, along));
                return Mathf.Min(Mathf.Min(top, bottom), Mathf.Min(left, right));
            };
            var corners = new List<Sdf>();
            foreach (Vector2 corner in Corners(size))
            {
                Vector2 inward = new Vector2(Mathf.Sign(c.x - corner.x), Mathf.Sign(c.y - corner.y));
                corners.Add(Stencil(key, corner + inward * inset, inward, scale));
            }
            Sdf shape = p =>
            {
                float d = runs(p);
                foreach (Sdf corner in corners)
                {
                    d = Mathf.Min(d, corner(p));
                }
                return d;
            };
            raster.Fill(p => shape(p) - 1.1f, Edge, bounds);
            Metal(raster, shape, bounds, 1.2f, GoldRamp, 0.64f, 0.45f);
            foreach (Vector2 corner in Corners(size))
            {
                Vector2 inward = new Vector2(Mathf.Sign(c.x - corner.x), Mathf.Sign(c.y - corner.y));
                Gem(raster, corner + inward * (inset + 16f * scale), 4f, Crimson);
            }
        }

        /// <summary>A cut stone in a gold setting: dark below, bright above, a spark of light at its top left.</summary>
        private static void Gem(Raster raster, Vector2 center, float radius, Color color)
        {
            Rect bounds = Sd.Around(center, radius + 4f);
            raster.Fill(p => Sd.Circle(p, center, radius + 1.7f), Edge, bounds);
            raster.Fill(p => Sd.Circle(p, center, radius), p =>
            {
                float v = Vector2.Dot((p - center) / radius, Light);
                Color c = Color.Lerp(color * 0.35f, Color.Lerp(color, Color.white, 0.25f), 0.5f + 0.5f * v);
                c.a = 1f;
                return c;
            }, bounds);
            raster.Fill(p => Sd.Circle(p, center + new Vector2(-0.34f, 0.4f) * radius, radius * 0.3f), new Color(1f, 0.95f, 0.9f, 0.9f), bounds);
        }

        /// <summary>A small round rivet.</summary>
        private static void Stud(Raster raster, Vector2 center, float radius, Ramp ramp)
        {
            Rect bounds = Sd.Around(center, radius + 3f);
            raster.Fill(p => Sd.Circle(p, center, radius + 1.2f), Edge, bounds);
            Metal(raster, p => Sd.Circle(p, center, radius), bounds, radius, ramp, 0.62f, 0.5f);
        }

        // ------------------------------------------------------------------ materials

        /// <summary>A gradient of colors over 0..1.</summary>
        private sealed class Ramp
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

        /// <summary>The outward direction of a shape's edge at <paramref name="p"/>.</summary>
        private static Vector2 Gradient(Sdf shape, Vector2 p)
        {
            const float e = 0.5f;
            var g = new Vector2(shape(p + new Vector2(e, 0f)) - shape(p - new Vector2(e, 0f)), shape(p + new Vector2(0f, e)) - shape(p - new Vector2(0f, e)));
            float length = g.magnitude;
            return length > 1e-5f ? g / length : Vector2.zero;
        }

        /// <summary>
        /// Fills a shape with raised, beveled metal: a flat top at <paramref name="level"/> of the ramp, and edges that
        /// slope away over <paramref name="bevel"/> pixels, bright where they face the light and dark where they do not
        /// (a negative <paramref name="contrast"/> sinks the shape instead).
        /// </summary>
        private static void Metal(Raster raster, Sdf shape, Rect bounds, float bevel, Ramp ramp, float level, float contrast)
        {
            raster.Fill(shape, p =>
            {
                float d = shape(p);
                float depth = Mathf.Clamp01(-d / bevel);
                float slope = 1f - depth * depth;
                float lit = Vector2.Dot(Gradient(shape, p), Light) * slope;
                // A soft crest of light along the top of the bevel.
                float crest = Mathf.Exp(-Mathf.Pow((depth - 0.55f) * 3.2f, 2f)) * 0.06f;
                return ramp.At(level + contrast * lit + crest);
            }, bounds);
        }

        /// <summary>Darkens the inside of a shape toward its edge, as if the frame around it stood above it.</summary>
        private static void InnerShadow(Raster raster, Sdf inside, float width, float strength, Rect bounds)
        {
            raster.Fill(inside, p =>
            {
                float t = Mathf.Clamp01(-inside(p) / width);
                return new Color(0f, 0f, 0f, strength * (1f - t) * (1f - t));
            }, bounds);
        }

        /// <summary>
        /// Leather between two tones with the grain of the downloaded leather pressed into it, repeating every
        /// <paramref name="period"/> pixels from <paramref name="origin"/>, so the middle of a sprite tiles.
        /// </summary>
        private static Func<Vector2, Color> LeatherShader(Color dark, Color light, int period, Vector2 origin, float grain = 1f)
        {
            Swatch height = Texture("Leather037_Displacement", period);
            return p =>
            {
                float x = p.x - origin.x;
                float y = p.y - origin.y;
                float v = height.Height(x, y);
                float slope = -(height.Height(x + 1f, y) - height.Height(x - 1f, y)) * Light.x - (height.Height(x, y + 1f) - height.Height(x, y - 1f)) * Light.y;
                float mottle = Periodic(x, y, period, 4, 11) * 0.6f + Periodic(x, y, period, 8, 12) * 0.3f;
                Color color = Color.Lerp(dark, light, Mathf.Clamp01(0.4f + (v - 0.5f) * 0.8f * grain + mottle * 0.25f));
                color *= 1f + slope * 2.4f * grain;
                color.a = 1f;
                return color;
            };
        }

        /// <summary>
        /// Smooth noise in -1..1 that repeats every <paramref name="period"/> pixels both ways, from a lattice of
        /// <paramref name="cells"/> values across the period.
        /// </summary>
        private static float Periodic(float x, float y, int period, int cells, int seed)
        {
            float fx = x / period * cells;
            float fy = y / period * cells;
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0;
            float ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Lattice(x0, y0, cells, seed);
            float b = Lattice(x0 + 1, y0, cells, seed);
            float c = Lattice(x0, y0 + 1, cells, seed);
            float d = Lattice(x0 + 1, y0 + 1, cells, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Lattice(int x, int y, int cells, int seed)
        {
            x = ((x % cells) + cells) % cells;
            y = ((y % cells) + cells) % cells;
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144269504);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFF) / 32767.5f - 1f;
        }

        /// <summary>
        /// A downloaded texture brought down to a tile of <c>width</c> by <c>height</c> pixels (averaging what falls in
        /// each), read with wrapping so it repeats seamlessly.
        /// </summary>
        private sealed class Swatch
        {
            private readonly int width;
            private readonly int height;
            private readonly float[] values;

            public Swatch(Color[] source, int sourceWidth, int sourceHeight, int width, int height)
            {
                this.width = width;
                this.height = height;
                values = new float[width * height];
                float sx = sourceWidth / (float)width;
                float sy = sourceHeight / (float)height;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float sum = 0f;
                        int count = 0;
                        for (int j = Mathf.FloorToInt(y * sy); j < Mathf.Max(Mathf.FloorToInt(y * sy) + 1, Mathf.FloorToInt((y + 1) * sy)); j++)
                        {
                            for (int i = Mathf.FloorToInt(x * sx); i < Mathf.Max(Mathf.FloorToInt(x * sx) + 1, Mathf.FloorToInt((x + 1) * sx)); i++)
                            {
                                Color c = source[Mathf.Min(j, sourceHeight - 1) * sourceWidth + Mathf.Min(i, sourceWidth - 1)];
                                sum += c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                                count++;
                            }
                        }
                        values[y * width + x] = sum / count;
                    }
                }
            }

            /// <summary>The brightness at a point, in pixels of the tile, read between pixels and wrapped.</summary>
            public float Height(float x, float y)
            {
                x -= 0.5f;
                y -= 0.5f;
                int x0 = Mathf.FloorToInt(x);
                int y0 = Mathf.FloorToInt(y);
                float tx = x - x0;
                float ty = y - y0;
                float a = Value(x0, y0);
                float b = Value(x0 + 1, y0);
                float c = Value(x0, y0 + 1);
                float d = Value(x0 + 1, y0 + 1);
                return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
            }

            private float Value(int x, int y)
            {
                x = ((x % width) + width) % width;
                y = ((y % height) + height) % height;
                return values[y * width + x];
            }
        }

        private static readonly Dictionary<string, Swatch> Swatches = new Dictionary<string, Swatch>();

        private static Swatch Texture(string name, int width, int height = 0)
        {
            height = height > 0 ? height : width;
            string key = $"{name}/{width}x{height}";
            if (Swatches.TryGetValue(key, out Swatch cached))
            {
                return cached;
            }
            Texture2D source = Read($"Art/UI/Textures/{name}.jpg");
            Swatch swatch = source != null
                ? new Swatch(source.GetPixels(), source.width, source.height, width, height)
                : new Swatch(new[] { new Color(0.5f, 0.5f, 0.5f) }, 1, 1, width, height);
            if (source == null)
            {
                Debug.LogWarning($"Heroes: the texture {name} is missing; the interface is drawn without its grain.");
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
            Swatches[key] = swatch;
            return swatch;
        }

        /// <summary>A picture read straight from its file, whatever its import settings, or null.</summary>
        private static Texture2D Read(string relative)
        {
            string file = HeroesAssets.FullPath(HeroesAssets.Path(relative));
            if (!File.Exists(file))
            {
                return null;
            }
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(file));
            return texture;
        }

        /// <summary>
        /// The top left <paramref name="size"/> pixels of one of Kenney's borders (white lines on nothing), as a grid of
        /// the pixels that are drawn, with x to the right and y down from the corner.
        /// </summary>
        private static bool[,] Pattern(string name, int size)
        {
            Texture2D texture = Read($"Art/UI/Borders/{name}.png");
            if (texture == null)
            {
                return null;
            }
            var mask = new bool[size, size];
            for (int y = 0; y < size && y < texture.height; y++)
            {
                for (int x = 0; x < size && x < texture.width; x++)
                {
                    Color c = texture.GetPixel(x, texture.height - 1 - y);
                    mask[x, y] = c.a > 0.5f && c.r > 0.5f;
                }
            }
            UnityEngine.Object.DestroyImmediate(texture);
            return mask;
        }

        /// <summary>
        /// A pixel pattern as a shape: every drawn pixel a square of <paramref name="scale"/> pixels, laid from
        /// <paramref name="origin"/> along <paramref name="inward"/> (so one pattern serves all four corners).
        /// </summary>
        private static Sdf Stencil(bool[,] mask, Vector2 origin, Vector2 inward, float scale)
        {
            int size = mask.GetLength(0);
            return p =>
            {
                var q = new Vector2((p.x - origin.x) * inward.x, (p.y - origin.y) * inward.y) / scale;
                int cx = Mathf.FloorToInt(q.x);
                int cy = Mathf.FloorToInt(q.y);
                float best = 4f;
                for (int y = cy - 2; y <= cy + 2; y++)
                {
                    for (int x = cx - 2; x <= cx + 2; x++)
                    {
                        if (x < 0 || y < 0 || x >= size || y >= size || !mask[x, y])
                        {
                            continue;
                        }
                        best = Mathf.Min(best, Sd.Box(q, new Vector2(x + 0.5f, y + 0.5f), new Vector2(0.5f, 0.5f)));
                    }
                }
                return best * scale;
            };
        }
    }
}
