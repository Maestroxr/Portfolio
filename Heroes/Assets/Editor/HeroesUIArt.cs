using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gamebox.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Draws the interface of the game: the dark panels with their gold frames, the buttons, the slots of an army, the
    /// bars and the stars of the campaign, in the style of the old adventure maps. Everything is drawn from code (so a
    /// rebuild writes the same bytes), and the icons and fonts that were downloaded are imported next to it.
    /// </summary>
    internal static class HeroesUIArt
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

        public static void BuildAll(HeroesArt art)
        {
            art.frame = Frame("UI/Generated/Frame.png", 128, 34, Leather, LeatherLight, true);
            art.panel = Frame("UI/Generated/Panel.png", 96, 20, new Color(0.13f, 0.10f, 0.08f), new Color(0.2f, 0.16f, 0.12f), false);
            art.parchment = ParchmentSprite("UI/Generated/Parchment.png", 128, 26);
            art.button = Button("UI/Generated/Button.png", 0);
            art.buttonHover = Button("UI/Generated/ButtonHover.png", 1);
            art.buttonPressed = Button("UI/Generated/ButtonPressed.png", 2);
            art.slot = Slot("UI/Generated/Slot.png");
            art.bar = Bar("UI/Generated/Bar.png");
            art.round = Round("UI/Generated/Round.png");
            art.glow = Glow("UI/Generated/Glow.png");
            art.star = Star("UI/Generated/Star.png", true);
            art.starEmpty = Star("UI/Generated/StarEmpty.png", false);
            art.banner = Banner("UI/Generated/Banner.png");
            Fonts(art);
            Icons(art);
        }

        // ------------------------------------------------------------------ pieces

        private static Sprite Save(Raster raster, string relative, Vector4 border)
        {
            HeroesAssets.SaveTexture(raster.ToTexture(), relative, importer => HeroesAssets.SpriteImport(importer, border));
            return HeroesAssets.Sprite(relative);
        }

        /// <summary>A panel with a gold frame: dark leather inside, a beveled rim, and a diamond in every corner.</summary>
        private static Sprite Frame(string relative, int size, int border, Color inner, Color innerLight, bool ornate)
        {
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            float edge = 3f;
            Sdf outer = p => Sd.Box(p, new Vector2(size / 2f, size / 2f), new Vector2(size / 2f - 1f, size / 2f - 1f), 6f);
            // The leather inside, lit from above.
            raster.Fill(outer, p => Color.Lerp(inner, innerLight, Mathf.Clamp01(p.y / size)), all);
            raster.Grain(all, 0.035f, 12345);
            // The frame: a band of gold with a dark line on both sides.
            Sdf band = p => Mathf.Max(outer(p), -Sd.Box(p, new Vector2(size / 2f, size / 2f), new Vector2(size / 2f - border + edge, size / 2f - border + edge), 4f));
            raster.Fill(band, p =>
            {
                float t = Mathf.Clamp01((p.y + p.x * 0.35f) / (size * 1.35f));
                return Color.Lerp(GoldDark, GoldLight, Mathf.SmoothStep(0f, 1f, t));
            }, all);
            Sdf outline = p => Sd.Outline(outer(p), 1.6f);
            raster.Fill(outline, new Color(0.08f, 0.06f, 0.04f, 0.9f), all);
            Sdf innerOutline = p => Sd.Outline(Sd.Box(p, new Vector2(size / 2f, size / 2f), new Vector2(size / 2f - border + edge, size / 2f - border + edge), 4f), 1.4f);
            raster.Fill(innerOutline, new Color(0.08f, 0.06f, 0.04f, 0.85f), all);
            if (ornate)
            {
                float d = border * 0.62f;
                foreach (Vector2 corner in new[] { new Vector2(d, d), new Vector2(size - d, d), new Vector2(d, size - d), new Vector2(size - d, size - d) })
                {
                    Vector2 c = corner;
                    raster.Fill(p => Sd.Star(p, c, border * 0.42f, 4, 0.42f, 0f), GoldLight, Sd.Around(c, border * 0.5f));
                    raster.Fill(p => Sd.Outline(Sd.Star(p, c, border * 0.42f, 4, 0.42f, 0f), 1.2f), GoldDark, Sd.Around(c, border * 0.6f));
                }
            }
            return Save(raster, relative, new Vector4(border, border, border, border));
        }

        private static Sprite ParchmentSprite(string relative, int size, int border)
        {
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            Sdf shape = p => Sd.Box(p, new Vector2(size / 2f, size / 2f), new Vector2(size / 2f - 1f, size / 2f - 1f), 4f);
            raster.Fill(shape, p =>
            {
                float edge = Mathf.Clamp01(-shape(p) / (border * 0.8f));
                return Color.Lerp(ParchmentDark, Parchment, Mathf.SmoothStep(0f, 1f, edge));
            }, all);
            raster.Grain(all, 0.05f, 777);
            raster.Fill(p => Sd.Outline(shape(p), 1.5f), new Color(0.35f, 0.26f, 0.16f, 0.9f), all);
            return Save(raster, relative, new Vector4(border, border, border, border));
        }

        /// <summary>A button: stone under a gold rim, brighter when the pointer is over it and sunken when pressed.</summary>
        private static Sprite Button(string relative, int state)
        {
            const int width = 96;
            const int height = 48;
            const int border = 18;
            var raster = new Raster(width, height, Color.clear);
            var all = new Rect(0, 0, width, height);
            var center = new Vector2(width / 2f, height / 2f);
            Sdf shape = p => Sd.Box(p, center, new Vector2(width / 2f - 1.5f, height / 2f - 1.5f), 7f);
            Color top = state == 1 ? new Color(0.44f, 0.36f, 0.26f) : state == 2 ? new Color(0.2f, 0.16f, 0.12f) : new Color(0.34f, 0.28f, 0.21f);
            Color bottom = state == 1 ? new Color(0.24f, 0.19f, 0.14f) : state == 2 ? new Color(0.14f, 0.11f, 0.08f) : new Color(0.18f, 0.14f, 0.10f);
            raster.Fill(shape, p => Color.Lerp(bottom, top, state == 2 ? 1f - p.y / height : p.y / height), all);
            raster.Grain(all, 0.03f, 4242);
            Sdf rim = p => Mathf.Max(shape(p), -Sd.Box(p, center, new Vector2(width / 2f - 5f, height / 2f - 5f), 5f));
            Color rimColor = state == 1 ? GoldLight : state == 2 ? GoldDark : Gold;
            raster.Fill(rim, p => Color.Lerp(GoldDark, rimColor, Mathf.Clamp01(p.y / height + 0.15f)), all);
            raster.Fill(p => Sd.Outline(shape(p), 1.5f), new Color(0.07f, 0.05f, 0.03f, 0.95f), all);
            if (state != 2)
            {
                // A highlight along the top edge.
                raster.Fill(p => Sd.Box(p, new Vector2(width / 2f, height - 7f), new Vector2(width / 2f - 8f, 1.2f), 1f), new Color(1f, 1f, 1f, 0.12f), all);
            }
            return Save(raster, relative, new Vector4(border, border, border, border));
        }

        private static Sprite Slot(string relative)
        {
            const int size = 72;
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            var center = new Vector2(size / 2f, size / 2f);
            Sdf shape = p => Sd.Box(p, center, new Vector2(size / 2f - 1.5f, size / 2f - 1.5f), 5f);
            raster.Fill(shape, p => Color.Lerp(new Color(0.10f, 0.08f, 0.06f), new Color(0.20f, 0.16f, 0.12f), p.y / size), all);
            Sdf rim = p => Mathf.Max(shape(p), -Sd.Box(p, center, new Vector2(size / 2f - 4f, size / 2f - 4f), 4f));
            raster.Fill(rim, p => Color.Lerp(GoldDark, Gold, Mathf.Clamp01((p.x + p.y) / (size * 1.6f))), all);
            raster.Fill(p => Sd.Outline(shape(p), 1.4f), new Color(0.06f, 0.04f, 0.03f, 0.9f), all);
            return Save(raster, relative, new Vector4(10, 10, 10, 10));
        }

        private static Sprite Bar(string relative)
        {
            const int width = 32;
            const int height = 16;
            var raster = new Raster(width, height, Color.clear);
            var all = new Rect(0, 0, width, height);
            Sdf shape = p => Sd.Box(p, new Vector2(width / 2f, height / 2f), new Vector2(width / 2f - 1f, height / 2f - 1f), 3f);
            raster.Fill(shape, p => Color.Lerp(new Color(1f, 1f, 1f, 1f), new Color(0.75f, 0.75f, 0.75f, 1f), 1f - p.y / height), all);
            raster.Fill(p => Sd.Box(p, new Vector2(width / 2f, height - 4f), new Vector2(width / 2f - 3f, 1.5f), 1f), new Color(1f, 1f, 1f, 0.35f), all);
            return Save(raster, relative, new Vector4(5, 5, 5, 5));
        }

        private static Sprite Round(string relative)
        {
            const int size = 72;
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            var center = new Vector2(size / 2f, size / 2f);
            raster.Fill(p => Sd.Circle(p, center, size / 2f - 2f), p => Color.Lerp(new Color(0.16f, 0.12f, 0.09f), new Color(0.33f, 0.27f, 0.2f), p.y / size), all);
            raster.Fill(p => Sd.Ring(p, center, size / 2f - 4f, 3.5f), p => Color.Lerp(GoldDark, GoldLight, Mathf.Clamp01(p.y / size + 0.1f)), all);
            raster.Fill(p => Sd.Outline(Sd.Circle(p, center, size / 2f - 2f), 1.5f), new Color(0.06f, 0.05f, 0.03f, 0.9f), all);
            return Save(raster, relative, new Vector4(0, 0, 0, 0));
        }

        private static Sprite Glow(string relative)
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
                    raster.Set(x, y, new Color(1f, 1f, 1f, a * a * a));
                }
            }
            return Save(raster, relative, Vector4.zero);
        }

        private static Sprite Star(string relative, bool filled)
        {
            const int size = 64;
            var raster = new Raster(size, size, Color.clear);
            var all = new Rect(0, 0, size, size);
            var center = new Vector2(size / 2f, size / 2f);
            Sdf star = p => Sd.Star(p, center, size * 0.42f, 5, 0.45f);
            if (filled)
            {
                raster.Fill(star, p => Color.Lerp(new Color(0.95f, 0.72f, 0.2f), GoldLight, Mathf.Clamp01(p.y / size)), all);
                raster.Fill(p => Sd.Outline(star(p), 2f), new Color(0.35f, 0.22f, 0.05f, 0.95f), all);
            }
            else
            {
                raster.Fill(star, new Color(0.2f, 0.17f, 0.13f, 0.55f), all);
                raster.Fill(p => Sd.Outline(star(p), 2f), new Color(0.45f, 0.38f, 0.26f, 0.9f), all);
            }
            return Save(raster, relative, Vector4.zero);
        }

        private static Sprite Banner(string relative)
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
            return Save(raster, relative, new Vector4(12, 20, 12, 12));
        }

        // ------------------------------------------------------------------ fonts and icons

        private static void Fonts(HeroesArt art)
        {
            art.titleFont = Font("Art/Fonts/Cinzel-Bold.ttf", "Art/Generated/Cinzel-Bold SDF.asset", 90);
            art.bodyFont = Font("Art/Fonts/Alegreya-Regular.ttf", "Art/Generated/Alegreya SDF.asset", 90);
        }

        private static TMP_FontAsset Font(string source, string relative, int samplingSize)
        {
            var font = HeroesAssets.Load<UnityEngine.Font>(source.Substring(source.IndexOf("Art/", StringComparison.Ordinal)));
            if (font == null)
            {
                Debug.LogWarning($"Heroes: the font {source} is missing.");
                return null;
            }
            TMP_FontAsset existing = HeroesAssets.Load<TMP_FontAsset>(relative);
            if (existing != null)
            {
                return existing;
            }
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, samplingSize, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            asset.name = Path.GetFileNameWithoutExtension(relative);
            HeroesAssets.EnsureFolderOf(relative);
            AssetDatabase.CreateAsset(asset, HeroesAssets.Path(relative));
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void Icons(HeroesArt art)
        {
            art.icons.Clear();
            art.iconNames.Clear();
            string folder = HeroesAssets.Path("Art/UI/Icons");
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).OrderBy(AssetDatabase.GUIDToAssetPath))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string relative = path.Substring(HeroesAssets.Root.Length + 1);
                HeroesAssets.ConfigureTexture(relative, importer => HeroesAssets.SpriteImport(importer, Vector4.zero, 256));
                Sprite sprite = HeroesAssets.Sprite(relative);
                if (sprite == null)
                {
                    continue;
                }
                string name = Path.GetFileNameWithoutExtension(path);
                art.icons.Add(sprite);
                art.iconNames.Add(name);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { HeroesAssets.Path("Art/UI/Items") }))
            {
                string relative = AssetDatabase.GUIDToAssetPath(guid).Substring(HeroesAssets.Root.Length + 1);
                HeroesAssets.ConfigureTexture(relative, importer => HeroesAssets.SpriteImport(importer, Vector4.zero, 128));
            }
            // The icons the game looks up by name.
            art.resourceIcons = new[] { "gold", "wood", "ore", "mercury", "sulfur", "crystal", "gems" }.Select(art.Icon).ToArray();
            art.statIcons = new[] { "stat_attack", "stat_defense", "stat_power", "stat_knowledge" }.Select(art.Icon).ToArray();
            art.skillIcons = new Sprite[HeroData.SkillCount];
            for (int i = 0; i < art.skillIcons.Length; i++)
            {
                string name = "skill_" + ToSnake(((SkillId)i).ToString());
                art.skillIcons[i] = art.Icon(name);
            }
            art.spellIcons = new Sprite[Spells.Count];
            for (int i = 0; i < art.spellIcons.Length; i++)
            {
                art.spellIcons[i] = art.Icon("spell_" + ToSnake(((SpellId)i).ToString()));
            }
            art.artifactIcons = new Sprite[Artifacts.Count];
            for (int i = 0; i < art.artifactIcons.Length; i++)
            {
                ArtifactDef def = Artifacts.Get((ArtifactId)i);
                art.artifactIcons[i] = def != null ? HeroesAssets.Sprite($"Art/UI/Items/{def.Item}.png") : null;
            }
        }

        public static string ToSnake(string name)
        {
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    if (i > 0)
                    {
                        text.Append('_');
                    }
                    text.Append(char.ToLowerInvariant(name[i]));
                }
                else
                {
                    text.Append(name[i]);
                }
            }
            return text.ToString();
        }
    }
}
