using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    internal static partial class CarArt
    {
        /// <summary>
        /// The colours of every car, item and wheel, one texel each. The painted parts of a car point at the white and
        /// grey "Paint" texels, so the paint material (the same texture, tinted by the driver's colour) shades them, and
        /// the wreck material (the same texture, charred) still finds a sensible texel for every face. Lights are
        /// separate renderers whose texels glow (the emission map), so a theme lights them at night and a car brightens
        /// its own tail lights when it brakes. The order is fixed: a name always lands on the same texel.
        /// </summary>
        private static readonly (string name, string hex, float glow)[] Swatches =
        {
            // Neutrals and trim.
            ("White", "F2F0EA", 0f), ("OffWhite", "D9D4C8", 0f), ("Black", "16171A", 0f), ("Charcoal", "26282D", 0f),
            ("DarkGray", "3A3D44", 0f), ("Gray", "6B6F78", 0f), ("LightGray", "A3A8B0", 0f), ("Chrome", "D3D8DE", 0f),
            ("ChromeDark", "8E959E", 0f), ("Gunmetal", "3E434B", 0f), ("GunDark", "202328", 0f), ("Rubber", "18181B", 0f),
            ("Tread", "2C2C30", 0f), ("Rim", "BCC2CA", 0f), ("RimDark", "5C626B", 0f), ("Hub", "7C838C", 0f),
            ("Glass", "1B2532", 0f), ("GlassShine", "52687F", 0f), ("Interior", "2B2622", 0f), ("Seat", "4A2F25", 0f),
            ("Grille", "111214", 0f), ("Rust", "8F4A26", 0f), ("RustDark", "5E2E17", 0f), ("RustLight", "B86A35", 0f),
            ("Primer", "7F8B86", 0f), ("Faded", "6E8FA8", 0f), ("Brass", "D4A843", 0f), ("BrassDark", "8C6A22", 0f),
            ("Olive", "5B6236", 0f), ("OliveDark", "3D4224", 0f), ("Khaki", "9C9468", 0f), ("Hazard", "F2B21C", 0f),
            ("HazardDark", "1D1D1D", 0f), ("Red", "D8352C", 0f), ("DarkRed", "8C1F1A", 0f), ("Orange", "E8771E", 0f),
            ("Yellow", "F5C832", 0f), ("Green", "3FA34D", 0f), ("DarkGreen", "23663A", 0f), ("Blue", "2F6FD6", 0f),
            ("Navy", "1C2F5E", 0f), ("Carbon", "17181C", 0f), ("CarbonLight", "2C2E34", 0f), ("Plate", "5E6660", 0f),
            ("PlateDark", "3D4440", 0f), ("PlateLight", "838C85", 0f), ("Rivet", "A2AAA4", 0f), ("Exhaust", "3A3633", 0f),
            ("ExhaustTip", "8A7760", 0f), ("Soot", "0E0E0E", 0f), ("Canvas", "7A6E52", 0f), ("CanvasDark", "5A5038", 0f),
            ("Wood", "8A6236", 0f), ("License", "E8E2C8", 0f), ("Mud", "6B5338", 0f), ("Strap", "2A2620", 0f),

            // Paint: white and greys, tinted by the driver's colour (the paint material's _BaseColor).
            ("Paint", "FFFFFF", 0f), ("PaintShade", "CDCDCD", 0f), ("PaintDark", "8E8E8E", 0f), ("PaintDeep", "545454", 0f),

            // Lights: the emission map lights these.
            ("Headlight", "FFF3D2", 1f), ("Xenon", "DCEEFF", 1f), ("Taillight", "E4261C", 1f), ("TailDim", "7A1410", 0.3f),
            ("Indicator", "FF9A1F", 0.9f), ("Beacon", "FF3B2F", 1f), ("Led", "F2F7FF", 1f), ("Spot", "FFF8E0", 1f),

            // Items.
            ("CashGreen", "2FA84F", 0f), ("CashDark", "1C6B33", 0f), ("Bill", "93D48C", 0f), ("BillDark", "5AA35A", 0f),
            ("CrossRed", "E5322B", 0f), ("MedWhite", "F4F1EA", 0f), ("NitroBlue", "2E86F0", 0f), ("NitroDeep", "1A4FA8", 0f),
            ("NitroLight", "A6D8FF", 0f), ("MineBody", "3A3E36", 0f), ("MineTop", "585E52", 0f), ("Spike", "AEB4BA", 0f),
            ("RocketBody", "DEDAD0", 0f), ("RocketNose", "C8321F", 0f), ("RocketBand", "2C3036", 0f), ("Copper", "B5703F", 0f),

            // Item glows (the rings under pickups, the mine's lamp, the rocket's flame).
            ("GlowGreen", "4DFF7A", 1f), ("GlowRed", "FF3B3B", 1f), ("GlowAmber", "FFB52E", 1f), ("GlowBlue", "40AAFF", 1f),
            ("GlowWhite", "FFFFFF", 1f), ("MineLight", "FF2A1F", 1f), ("Flame", "FFB040", 1f), ("FlameCore", "FFF2C0", 1f),

            // Added with the cars of the higher leagues (after the rest, so no colour above moves): the Spectre's light
            // strips and the hot throat of the Apocalypse's jets.
            ("GlowCyan", "3FE6FF", 1f), ("JetHot", "FF7A2A", 1f)
        };

        private static PaletteTexture palette;

        /// <summary>The palette of the cars and items (built once per domain, always in the same order).</summary>
        public static PaletteTexture Palette
        {
            get
            {
                if (palette == null)
                {
                    var built = new PaletteTexture(16);
                    foreach ((string name, string hex, float glow) in Swatches)
                    {
                        built.Add(name, hex, glow);
                    }
                    palette = built;
                }
                return palette;
            }
        }

        /// <summary>The UV of a palette colour (throws for a typo, so a model never silently shows magenta).</summary>
        public static Vector2 UV(string name)
        {
            return Palette.UV(name);
        }

        public static Color ColorOf(string name)
        {
            return Palette.ColorOf(name);
        }
    }
}
