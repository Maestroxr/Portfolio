using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>Named colours of the generated models' palette.</summary>
    internal enum Swatch
    {
        White, Hull, HullDark, Panel, Trim, Black, Chrome, Gunmetal,
        Red, DarkRed, Orange, Yellow, Gold, Green, DarkGreen, Teal,
        Cyan, Blue, Navy, Violet, Purple, Magenta, Pink, Hazard,
        Rock, RockDark, Basalt, IceLight, Ice, IceDeep, CrystalCore, CrystalDark,
        GlowCyan, GlowBlue, GlowGreen, GlowRed, GlowOrange, GlowYellow, GlowMagenta, GlowViolet, GlowWhite, GlowIce
    }


    /// <summary>
    /// The palette the generated models are coloured with: a model is one mesh whose UVs point at the texel of each
    /// face's colour, so the saucers, pods, drones, crystals and pickups share one material. A second texture holds
    /// the emission of the glowing swatches, which bloom.
    /// </summary>
    internal static class Palette
    {
        public const int Size = 16;

        private static readonly Dictionary<Swatch, (string hex, float glow)> Colors = new Dictionary<Swatch, (string, float)>
        {
            { Swatch.White, ("F2F5FA", 0f) }, { Swatch.Hull, ("C9D2DE", 0f) }, { Swatch.HullDark, ("3B4250", 0f) },
            { Swatch.Panel, ("8D97A6", 0f) }, { Swatch.Trim, ("5E6878", 0f) }, { Swatch.Black, ("15171D", 0f) },
            { Swatch.Chrome, ("E3E9F2", 0f) }, { Swatch.Gunmetal, ("262B35", 0f) },
            { Swatch.Red, ("E23B3B", 0f) }, { Swatch.DarkRed, ("8E1F28", 0f) }, { Swatch.Orange, ("F2851F", 0f) },
            { Swatch.Yellow, ("FFCF3A", 0f) }, { Swatch.Gold, ("D9A441", 0f) }, { Swatch.Green, ("3FBF5F", 0f) },
            { Swatch.DarkGreen, ("24713A", 0f) }, { Swatch.Teal, ("1FA4A0", 0f) },
            { Swatch.Cyan, ("3FD6F2", 0f) }, { Swatch.Blue, ("2F6FE0", 0f) }, { Swatch.Navy, ("1B2550", 0f) },
            { Swatch.Violet, ("6A4FE0", 0f) }, { Swatch.Purple, ("7B3FB0", 0f) }, { Swatch.Magenta, ("D23FD8", 0f) },
            { Swatch.Pink, ("FF7FC2", 0f) }, { Swatch.Hazard, ("FFC21A", 0f) },
            { Swatch.Rock, ("6E6A66", 0f) }, { Swatch.RockDark, ("3C3936", 0f) }, { Swatch.Basalt, ("24222A", 0f) },
            { Swatch.IceLight, ("E6F6FF", 0f) }, { Swatch.Ice, ("A9DDF5", 0f) }, { Swatch.IceDeep, ("5DA6D9", 0f) },
            { Swatch.CrystalCore, ("2A1E3A", 0f) }, { Swatch.CrystalDark, ("47285E", 0f) },
            { Swatch.GlowCyan, ("5CF2FF", 1f) }, { Swatch.GlowBlue, ("5A9BFF", 1f) }, { Swatch.GlowGreen, ("6CFF8A", 1f) },
            { Swatch.GlowRed, ("FF4040", 1f) }, { Swatch.GlowOrange, ("FF9A2E", 1f) }, { Swatch.GlowYellow, ("FFE066", 1f) },
            { Swatch.GlowMagenta, ("FF4FE8", 1f) }, { Swatch.GlowViolet, ("A66CFF", 1f) }, { Swatch.GlowWhite, ("FFFFFF", 1f) },
            { Swatch.GlowIce, ("9FEAFF", 0.35f) }
        };

        public static Color Color(Swatch swatch)
        {
            return ColorUtility.TryParseHtmlString("#" + Colors[swatch].hex, out Color color) ? color : UnityEngine.Color.magenta;
        }

        public static Vector2 UV(Swatch swatch)
        {
            int index = (int)swatch;
            return new Vector2((index % Size + 0.5f) / Size, (index / Size + 0.5f) / Size);
        }

        /// <summary>The palette (or its emission) as pixels, one texel per swatch.</summary>
        public static Color[] Pixels(bool emission)
        {
            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = emission ? UnityEngine.Color.black : UnityEngine.Color.magenta;
            }
            foreach (KeyValuePair<Swatch, (string hex, float glow)> entry in Colors)
            {
                Color color = Color(entry.Key);
                int index = (int)entry.Key;
                pixels[index] = emission ? color * Mathf.Min(1f, entry.Value.glow) : color;
            }
            return pixels;
        }
    }
}
