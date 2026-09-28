using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Portfolio.Monopoly.EditorTools
{
    /// <summary>
    /// Reads the classic look back out of what the builders made and puts the themed parts on it: every image, text,
    /// outline, renderer and mesh that shows an asset or a colour of the classic <see cref="MonopolyTheme"/> gets a
    /// <see cref="ThemedLook"/> (or the base <see cref="ThemedRenderer"/> / <see cref="ThemedMesh"/>) with the key of
    /// that asset, and a colour becomes an expression of the palette ("palette.ink", "palette.muted~0.6", "seats.1.color")
    /// when it is one of its colours or a shade or tint of one. What matches nothing stays as built. The builders call
    /// <see cref="Paint"/> for the few parts inference cannot know (the words of the edition line, a white that means
    /// the paper), and the tagger leaves those alone.
    /// </summary>
    internal static class MonopolyThemeTagger
    {
        /// <summary>Images tinted white that must stay white in every look (silhouettes and highlights, not surfaces).</summary>
        private static readonly HashSet<string> KeepWhite = new HashSet<string> { "Token", "Ring", "Knob", "Bill", "Banner", "Sheen", "Keyline", "Checkmark", "Crown" };

        private const float Tolerance = 1.6f / 255f;

        /// <summary>A named colour of the theme, for inference.</summary>
        private struct Named
        {
            public string key;
            public Color color;
        }

        private sealed class Lookup
        {
            public readonly Dictionary<Object, string> References = new Dictionary<Object, string>();
            public readonly List<Named> Colors = new List<Named>();
            public MonopolyTheme Theme;
        }

        // ------------------------------------------------------------------ explicit paints

        /// <summary>Puts a themed look with explicit keys on <paramref name="target"/>; the tagger leaves it as it is.</summary>
        public static ThemedLook Paint(Component target, string color = null, string sprite = null, string font = null, string material = null, string words = null)
        {
            ThemedLook look = target.GetComponent<ThemedLook>() ?? target.gameObject.AddComponent<ThemedLook>();
            look.ThemedGame = GameType.Monopoly;
            if (color != null)
            {
                look.ColorExpression = color;
            }
            if (sprite != null)
            {
                look.SpriteKey = sprite;
            }
            if (font != null)
            {
                look.FontKey = font;
            }
            if (material != null)
            {
                look.MaterialKey = material;
            }
            if (words != null)
            {
                look.WordsKey = words;
            }
            return look;
        }

        // ------------------------------------------------------------------ tagging

        /// <summary>Tags the images, texts and outline effects of an interface under <paramref name="root"/>.</summary>
        public static int TagInterface(GameObject root, MonopolyTheme classic)
        {
            Lookup lookup = Read(classic);
            StripMissingScripts(root);
            int tagged = 0;
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponent<ThemedLook>() != null)
                {
                    continue;
                }
                string sprite = image.sprite != null && lookup.References.TryGetValue(image.sprite, out string key) ? key : null;
                string color = Describe(lookup, image.color, allowPaper: !KeepWhite.Contains(image.name), surface: true);
                string effect = Effect(lookup, image);
                if (sprite != null || color != null || effect != null)
                {
                    ThemedLook look = Paint(image, color, sprite);
                    look.EffectExpression = effect;
                    tagged++;
                }
            }
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                tagged += TagText(lookup, text) ? 1 : 0;
            }
            return tagged;
        }

        /// <summary>
        /// Tags the renderers, meshes and printed texts of the 3D parts under <paramref name="root"/>, leaving out the
        /// materials of the objects in <paramref name="drawnByCode"/> and the meshes of those in
        /// <paramref name="shapedByCode"/> (the game's code gives them their look from the theme itself: the tokens,
        /// the owner tags).
        /// </summary>
        public static int TagScene(GameObject root, MonopolyTheme classic, ICollection<GameObject> drawnByCode = null, ICollection<GameObject> shapedByCode = null)
        {
            Lookup lookup = Read(classic);
            StripMissingScripts(root);
            int tagged = 0;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<TMP_Text>() != null || renderer is ParticleSystemRenderer)
                {
                    continue;
                }
                bool drawn = drawnByCode != null && drawnByCode.Contains(renderer.gameObject);
                Material[] materials = drawn ? new Material[0] : renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (materials[slot] == null || !lookup.References.TryGetValue(materials[slot], out string key))
                    {
                        continue;
                    }
                    if (HasRendererTag(renderer, slot))
                    {
                        continue;
                    }
                    var themed = renderer.gameObject.AddComponent<ThemedRenderer>();
                    themed.ThemedGame = GameType.Monopoly;
                    themed.MaterialKey = key;
                    themed.Slot = slot;
                    tagged++;
                }
                var filter = renderer.GetComponent<MeshFilter>();
                bool shaped = shapedByCode != null && shapedByCode.Contains(renderer.gameObject);
                if (!shaped && filter != null && filter.sharedMesh != null && lookup.References.TryGetValue(filter.sharedMesh, out string meshKey) && filter.GetComponent<ThemedMesh>() == null)
                {
                    var themed = filter.gameObject.AddComponent<ThemedMesh>();
                    themed.ThemedGame = GameType.Monopoly;
                    themed.MeshKey = meshKey;
                    tagged++;
                }
            }
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                tagged += TagText(lookup, text) ? 1 : 0;
            }
            return tagged;
        }

        /// <summary>Drops components whose script went away (a themed part of an older BaseGame) before new ones are added.</summary>
        private static void StripMissingScripts(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
        }

        private static bool HasRendererTag(Renderer renderer, int slot)
        {
            foreach (ThemedRenderer existing in renderer.GetComponents<ThemedRenderer>())
            {
                if (existing.Slot == slot)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TagText(Lookup lookup, TMP_Text text)
        {
            ThemedLook existing = text.GetComponent<ThemedLook>();
            if (existing != null)
            {
                // An explicit paint: fill in what it leaves open.
                if (string.IsNullOrEmpty(existing.FontKey))
                {
                    Fonts(lookup, text, out string fontKey, out string materialKey);
                    existing.FontKey = fontKey;
                    existing.MaterialKey = materialKey;
                }
                if (string.IsNullOrEmpty(existing.ColorExpression))
                {
                    existing.ColorExpression = Describe(lookup, text.color, allowPaper: false);
                }
                return false;
            }
            Fonts(lookup, text, out string font, out string material);
            string color = Describe(lookup, text.color, allowPaper: false) ?? WordsOnFace(text);
            if (font == null && color == null)
            {
                return false;
            }
            Paint(text, color, null, font, material);
            return true;
        }

        /// <summary>
        /// White words on a coloured face (a button, a tag, a band): the colour of words on that face in the theme
        /// ("on:palette.green"), which is white again in the classic look. Null for other words.
        /// </summary>
        private static string WordsOnFace(TMP_Text text)
        {
            if (!IsWhite(text.color))
            {
                return null;
            }
            for (Transform parent = text.transform.parent; parent != null; parent = parent.parent)
            {
                if (!parent.TryGetComponent(out Image face))
                {
                    continue;
                }
                var look = face.GetComponent<ThemedLook>();
                return look != null && !string.IsNullOrEmpty(look.ColorExpression) && !look.ColorExpression.StartsWith(MonopolyTheme.OnPrefix)
                    ? MonopolyTheme.OnPrefix + look.ColorExpression
                    : null;
            }
            return null;
        }

        /// <summary>The font key of a text and, when it uses one of the theme's special materials, that material's key.</summary>
        private static void Fonts(Lookup lookup, TMP_Text text, out string font, out string material)
        {
            font = null;
            material = null;
            MonopolyTheme theme = lookup.Theme;
            if (text.fontSharedMaterial != null && text.fontSharedMaterial == theme.titleMaterial)
            {
                font = "titleFont";
                material = "titleMaterial";
                return;
            }
            if (text.fontSharedMaterial != null && text.fontSharedMaterial == theme.textShadowMaterial)
            {
                font = "boldFont";
                material = "textShadowMaterial";
                return;
            }
            if (text.font != null && lookup.References.TryGetValue(text.font, out string key))
            {
                font = key;
            }
        }

        private static string Effect(Lookup lookup, Component graphic)
        {
            var shadow = graphic.GetComponent<Shadow>();
            return shadow != null ? Describe(lookup, shadow.effectColor, allowPaper: false) : null;
        }

        // ------------------------------------------------------------------ colours

        /// <summary>
        /// The expression of the theme that gives <paramref name="color"/>: a colour of the theme, or a shade (<c>*f</c>)
        /// or a tint towards white (<c>~t</c>) of one, with the alpha appended (<c>@a</c>) when it differs. Null when
        /// the colour is none of these. The paper (white) only matches when <paramref name="allowPaper"/>. On a
        /// <paramref name="surface"/> (an image) the colour of the ink is the plate: a dark plate behind white words.
        /// </summary>
        private static string Describe(Lookup lookup, Color color, bool allowPaper, bool surface = false)
        {
            if (surface)
            {
                Color plate = lookup.Theme.palette.plate;
                if (Near(plate.r, color.r) && Near(plate.g, color.g) && Near(plate.b, color.b))
                {
                    return WithAlpha("palette.plate", plate, color);
                }
            }
            foreach (Named named in lookup.Colors)
            {
                // White is the paper of a surface; on anything else (a white word, a white icon) it stays white.
                if (IsWhite(named.color) && !allowPaper)
                {
                    continue;
                }
                if (Near(named.color.r, color.r) && Near(named.color.g, color.g) && Near(named.color.b, color.b))
                {
                    return WithAlpha(named.key, named.color, color);
                }
            }
            foreach (Named named in lookup.Colors)
            {
                if (named.color.maxColorComponent < 0.05f)
                {
                    continue;
                }
                if (named.key == "palette.paper")
                {
                    // Only a plain grey is a shade of the paper (the lip under a white button); tints of white do not exist.
                    bool grey = Near(color.r, color.g) && Near(color.g, color.b);
                    if (!allowPaper || !grey)
                    {
                        continue;
                    }
                    float greyFactor = Factor(named.color, color);
                    if (greyFactor > 0f)
                    {
                        return WithAlpha($"{named.key}*{Number(greyFactor)}", named.color, color);
                    }
                    continue;
                }
                float factor = Factor(named.color, color);
                if (factor > 0f)
                {
                    return WithAlpha($"{named.key}*{Number(factor)}", named.color, color);
                }
                float tint = TintAmount(named.color, color);
                if (tint > 0f)
                {
                    return WithAlpha($"{named.key}~{Number(tint)}", named.color, color);
                }
            }
            return null;
        }

        private static string WithAlpha(string expression, Color source, Color color)
        {
            return Near(source.a, color.a) ? expression : $"{expression}@{Number(color.a)}";
        }

        private static string Number(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool Near(float a, float b)
        {
            return Mathf.Abs(a - b) <= Tolerance;
        }

        private static bool IsWhite(Color color)
        {
            return color.r >= 0.99f && color.g >= 0.99f && color.b >= 0.99f;
        }

        /// <summary>The factor that shades <paramref name="source"/> into <paramref name="color"/>, or 0.</summary>
        private static float Factor(Color source, Color color)
        {
            float factor = -1f;
            foreach ((float s, float c) in new[] { (source.r, color.r), (source.g, color.g), (source.b, color.b) })
            {
                if (s < 0.02f)
                {
                    if (c > Tolerance)
                    {
                        return 0f;
                    }
                    continue;
                }
                float f = c / s;
                if (factor < 0f)
                {
                    factor = f;
                }
                else if (Mathf.Abs(f - factor) > 0.02f)
                {
                    return 0f;
                }
            }
            if (factor < 0.05f || factor > 0.995f)
            {
                return 0f;
            }
            factor = Mathf.Round(factor * 1000f) / 1000f;
            Color check = MonopolyStyle.Shade(source, factor);
            return Near(check.r, color.r) && Near(check.g, color.g) && Near(check.b, color.b) ? factor : 0f;
        }

        /// <summary>How far <paramref name="source"/> is blended towards white to give <paramref name="color"/>, or 0.</summary>
        private static float TintAmount(Color source, Color color)
        {
            float amount = -1f;
            foreach ((float s, float c) in new[] { (source.r, color.r), (source.g, color.g), (source.b, color.b) })
            {
                if (s > 0.98f)
                {
                    if (Mathf.Abs(c - s) > Tolerance)
                    {
                        return 0f;
                    }
                    continue;
                }
                float t = (c - s) / (1f - s);
                if (amount < 0f)
                {
                    amount = t;
                }
                else if (Mathf.Abs(t - amount) > 0.02f)
                {
                    return 0f;
                }
            }
            if (amount < 0.05f || amount > 0.995f)
            {
                return 0f;
            }
            amount = Mathf.Round(amount * 1000f) / 1000f;
            var check = new Color(Mathf.Lerp(source.r, 1f, amount), Mathf.Lerp(source.g, 1f, amount), Mathf.Lerp(source.b, 1f, amount));
            return Near(check.r, color.r) && Near(check.g, color.g) && Near(check.b, color.b) ? amount : 0f;
        }

        // ------------------------------------------------------------------ reading the theme

        /// <summary>Every asset the theme refers to by its key, and its colours in the order inference prefers them.</summary>
        private static Lookup Read(MonopolyTheme theme)
        {
            var lookup = new Lookup { Theme = theme };
            var palette = new List<Named>();
            var print = new List<Named>();
            var seats = new List<Named>();
            var groups = new List<Named>();
            Collect(theme, "", lookup, palette, print, seats, groups, 0);
            lookup.Colors.AddRange(palette);
            lookup.Colors.AddRange(print);
            lookup.Colors.AddRange(seats);
            lookup.Colors.AddRange(groups);
            return lookup;
        }

        private static void Collect(object value, string path, Lookup lookup, List<Named> palette, List<Named> print, List<Named> seats, List<Named> groups, int depth)
        {
            if (value == null || depth > 4)
            {
                return;
            }
            if (value is Object asset)
            {
                if (depth > 0 && !lookup.References.ContainsKey(asset))
                {
                    lookup.References[asset] = path;
                }
                if (!(value is MonopolyTheme) || depth > 0)
                {
                    return;
                }
            }
            if (value is Color color)
            {
                if (path.StartsWith("palette."))
                {
                    palette.Add(new Named { key = path, color = color });
                }
                else if (path.StartsWith("print."))
                {
                    print.Add(new Named { key = path, color = color });
                }
                else if (path.StartsWith("seats.") && path.EndsWith(".color"))
                {
                    seats.Add(new Named { key = path, color = color });
                }
                else if (path.StartsWith("groups.") && path.EndsWith(".color"))
                {
                    groups.Add(new Named { key = path, color = color });
                }
                return;
            }
            if (value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
            {
                return;
            }
            if (value is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Collect(list[i], $"{path}.{i}", lookup, palette, print, seats, groups, depth + 1);
                }
                return;
            }
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                string childPath = string.IsNullOrEmpty(path) ? field.Name : $"{path}.{field.Name}";
                Collect(field.GetValue(value), childPath, lookup, palette, print, seats, groups, depth + 1);
            }
        }
    }
}
