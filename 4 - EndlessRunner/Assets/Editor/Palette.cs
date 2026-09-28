using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>Named colours of the model palette.</summary>
    internal enum Swatch
    {
        White, OffWhite, Black, Charcoal, DarkGray, Gray, LightGray, Silver,
        Red, DarkRed, Orange, Yellow, Gold, Cream,
        Green, DarkGreen, LeafLight, Leaf, LeafDark, Pine, PineDark, Grass,
        Brown, DarkBrown, Wood, WoodLight, WoodDark, Bark,
        Skin, SkinShade, Hair, Cheek, Hoodie, HoodieDark, Shorts, Shoe, ShoeSole, Cap, Backpack, Eye,
        Blue, Navy, Sky, Cyan, Teal, Purple, Violet, Pink, Magenta,
        Sand, SandDark, Terracotta, RockRed, RockOrange, Cactus, CactusDark, CactusLight,
        Snow, SnowShade, Ice, IceDeep, Carrot,
        Basalt, Ash, Obsidian, NightLeaf, NightTrunk, NightRock,
        Metal, MetalDark, Rust,
        WagonRed, WagonBlue, WagonGreen, WagonYellow, WagonTrim, WagonDark,
        HazardYellow, HazardBlack, FlagRed, FlagWhite,
        FlowerRed, FlowerYellow, FlowerPink, FlowerPurple, FlowerWhite, MushroomRed,
        Roof, RoofDark, Wall, Window,
        GlowYellow, GlowWarm, GlowCyan, GlowMagenta, GlowOrange, GlowLava, GlowWhite, GlowGreen, GlowPurple, GlowRed, GlowBlue,
        // The city at night (the Night Shift theme): muted concrete, steel and streetwear, and the neon that lights them.
        Asphalt, AsphaltWet, Concrete, ConcreteDark, ConcreteLight, Steel, SteelDark, SteelLight, Iron, Tar,
        Brick, BrickDark, Glass, WindowDark, WindowLit, NeonPink, NeonCyan, NeonAmber, NeonGreen, NeonRed, LampWhite,
        TaxiYellow, TaxiDark, ConeOrange, ConeWhite, SignGreen, SignWhite, Gravel, TileWhite, TileGreen, Cable, Tarp,
        Container1, Container2, Container3, Container4, ContainerDark, Chip, ChipRim,
        Denim, DenimDark, Jacket, JacketDark, Sneaker, SneakerSole, Bag, BagStrap, Beard, Beanie
    }


    /// <summary>
    /// The palette every generated model is coloured with. A model is one mesh whose UVs point at the texel of the
    /// colour each face should have, so all models share one material (and batch well). A second texture holds the
    /// emission of the glowing swatches.
    /// </summary>
    internal static class Palette
    {
        public const int Size = 16;

        private static readonly Dictionary<Swatch, (string hex, float glow)> Colors = new Dictionary<Swatch, (string, float)>
        {
            { Swatch.White, ("F6F6F2", 0f) }, { Swatch.OffWhite, ("E6DFD2", 0f) }, { Swatch.Black, ("1D1D24", 0f) },
            { Swatch.Charcoal, ("2E3038", 0f) }, { Swatch.DarkGray, ("4A4D57", 0f) }, { Swatch.Gray, ("7D828E", 0f) },
            { Swatch.LightGray, ("B9BEC8", 0f) }, { Swatch.Silver, ("D8DDE4", 0f) },
            { Swatch.Red, ("E63B35", 0f) }, { Swatch.DarkRed, ("A42A2A", 0f) }, { Swatch.Orange, ("F5801F", 0f) },
            { Swatch.Yellow, ("FFCB2E", 0f) }, { Swatch.Gold, ("E9A81C", 0f) }, { Swatch.Cream, ("FBEBC5", 0f) },
            { Swatch.Green, ("4CB050", 0f) }, { Swatch.DarkGreen, ("2E7D32", 0f) }, { Swatch.LeafLight, ("8BD24E", 0f) },
            { Swatch.Leaf, ("5DB845", 0f) }, { Swatch.LeafDark, ("3E8F3A", 0f) }, { Swatch.Pine, ("2F7A4E", 0f) },
            { Swatch.PineDark, ("235C3C", 0f) }, { Swatch.Grass, ("6FBF4A", 0f) },
            { Swatch.Brown, ("8B5A2B", 0f) }, { Swatch.DarkBrown, ("5A381C", 0f) }, { Swatch.Wood, ("B87D46", 0f) },
            { Swatch.WoodLight, ("D6A66B", 0f) }, { Swatch.WoodDark, ("7D5230", 0f) }, { Swatch.Bark, ("6E4A2E", 0f) },
            { Swatch.Skin, ("F4C48F", 0f) }, { Swatch.SkinShade, ("DDA571", 0f) }, { Swatch.Hair, ("5B3A29", 0f) },
            { Swatch.Cheek, ("F69A9A", 0f) }, { Swatch.Hoodie, ("2F86EB", 0f) }, { Swatch.HoodieDark, ("1F63B8", 0f) },
            { Swatch.Shorts, ("27335F", 0f) }, { Swatch.Shoe, ("FAFAFA", 0f) }, { Swatch.ShoeSole, ("E53A35", 0f) },
            { Swatch.Cap, ("EB4034", 0f) }, { Swatch.Backpack, ("F7B92F", 0f) }, { Swatch.Eye, ("20202A", 0f) },
            { Swatch.Blue, ("3079E0", 0f) }, { Swatch.Navy, ("1E2E5C", 0f) }, { Swatch.Sky, ("7CC8FF", 0f) },
            { Swatch.Cyan, ("3FD3D8", 0f) }, { Swatch.Teal, ("1AAE95", 0f) }, { Swatch.Purple, ("8E46B0", 0f) },
            { Swatch.Violet, ("6A4FD6", 0f) }, { Swatch.Pink, ("FF82B8", 0f) }, { Swatch.Magenta, ("D83FE8", 0f) },
            { Swatch.Sand, ("EAC98F", 0f) }, { Swatch.SandDark, ("CBA36B", 0f) }, { Swatch.Terracotta, ("C9663D", 0f) },
            { Swatch.RockRed, ("A9523B", 0f) }, { Swatch.RockOrange, ("D98545", 0f) }, { Swatch.Cactus, ("5E9F4A", 0f) },
            { Swatch.CactusDark, ("3F7B37", 0f) }, { Swatch.CactusLight, ("86C063", 0f) },
            { Swatch.Snow, ("F4F8FF", 0f) }, { Swatch.SnowShade, ("CFE0F2", 0f) }, { Swatch.Ice, ("A9DBF3", 0f) },
            { Swatch.IceDeep, ("5EA9D6", 0f) }, { Swatch.Carrot, ("FF8C1A", 0f) },
            { Swatch.Basalt, ("2A2A30", 0f) }, { Swatch.Ash, ("4B4549", 0f) }, { Swatch.Obsidian, ("1B1621", 0f) },
            { Swatch.NightLeaf, ("1F5A57", 0f) }, { Swatch.NightTrunk, ("3E2F3F", 0f) }, { Swatch.NightRock, ("3A4260", 0f) },
            { Swatch.Metal, ("9EA7B2", 0f) }, { Swatch.MetalDark, ("59606C", 0f) }, { Swatch.Rust, ("9E5B3B", 0f) },
            { Swatch.WagonRed, ("D9463F", 0f) }, { Swatch.WagonBlue, ("3C7FDB", 0f) }, { Swatch.WagonGreen, ("3FA64F", 0f) },
            { Swatch.WagonYellow, ("F2B434", 0f) }, { Swatch.WagonTrim, ("EDEDED", 0f) }, { Swatch.WagonDark, ("2D3B4E", 0f) },
            { Swatch.HazardYellow, ("FFC619", 0f) }, { Swatch.HazardBlack, ("232323", 0f) }, { Swatch.FlagRed, ("E8403A", 0f) },
            { Swatch.FlagWhite, ("FFFFFF", 0f) },
            { Swatch.FlowerRed, ("F0463C", 0f) }, { Swatch.FlowerYellow, ("FFD93D", 0f) }, { Swatch.FlowerPink, ("FF8FC8", 0f) },
            { Swatch.FlowerPurple, ("A777E3", 0f) }, { Swatch.FlowerWhite, ("FFFFFF", 0f) }, { Swatch.MushroomRed, ("DE3A2F", 0f) },
            { Swatch.Roof, ("C4493D", 0f) }, { Swatch.RoofDark, ("8F3129", 0f) }, { Swatch.Wall, ("F3E3C3", 0f) },
            { Swatch.Window, ("8FD3FF", 0f) },
            { Swatch.GlowYellow, ("FFE066", 1f) }, { Swatch.GlowWarm, ("FFC266", 1f) }, { Swatch.GlowCyan, ("5CF2FF", 1f) },
            { Swatch.GlowMagenta, ("FF5CE1", 1f) }, { Swatch.GlowOrange, ("FF9A2E", 1f) }, { Swatch.GlowLava, ("FF5A14", 1.4f) },
            { Swatch.GlowWhite, ("FFFFFF", 1f) }, { Swatch.GlowGreen, ("6CFF7A", 1f) }, { Swatch.GlowPurple, ("B36CFF", 1f) },
            { Swatch.GlowRed, ("FF4545", 1f) }, { Swatch.GlowBlue, ("59A8FF", 1f) },
            { Swatch.Asphalt, ("2B2D31", 0f) }, { Swatch.AsphaltWet, ("1F2226", 0f) }, { Swatch.Concrete, ("8A8A86", 0f) },
            { Swatch.ConcreteDark, ("5E5F5C", 0f) }, { Swatch.ConcreteLight, ("A9A8A2", 0f) }, { Swatch.Steel, ("6E7378", 0f) },
            { Swatch.SteelDark, ("3C4045", 0f) }, { Swatch.SteelLight, ("9AA0A6", 0f) }, { Swatch.Iron, ("2A2C30", 0f) },
            { Swatch.Tar, ("141517", 0f) }, { Swatch.Brick, ("6E3A30", 0f) }, { Swatch.BrickDark, ("4A2620", 0f) },
            { Swatch.Glass, ("1E2A38", 0f) }, { Swatch.WindowDark, ("2A3442", 0f) }, { Swatch.WindowLit, ("FFC978", 0.45f) },
            { Swatch.NeonPink, ("FF3F8E", 1.2f) }, { Swatch.NeonCyan, ("33E6FF", 1.2f) }, { Swatch.NeonAmber, ("FFB020", 1.1f) },
            { Swatch.NeonGreen, ("5AFF7A", 1f) }, { Swatch.NeonRed, ("FF2E2E", 1.2f) }, { Swatch.LampWhite, ("F3F6FF", 1f) },
            { Swatch.TaxiYellow, ("E8B923", 0f) }, { Swatch.TaxiDark, ("8F6F0F", 0f) }, { Swatch.ConeOrange, ("FF6A1F", 0f) },
            { Swatch.ConeWhite, ("F2F2F2", 0.3f) }, { Swatch.SignGreen, ("1E6B3C", 0f) }, { Swatch.SignWhite, ("F5F5F5", 0.35f) },
            { Swatch.Gravel, ("6F6A63", 0f) }, { Swatch.TileWhite, ("D8D4C8", 0f) }, { Swatch.TileGreen, ("3F6F63", 0f) },
            { Swatch.Cable, ("1A1A1A", 0f) }, { Swatch.Tarp, ("3E5A8A", 0f) },
            { Swatch.Container1, ("B03A2E", 0f) }, { Swatch.Container2, ("2F5F8F", 0f) }, { Swatch.Container3, ("3F7A4A", 0f) },
            { Swatch.Container4, ("C7862B", 0f) }, { Swatch.ContainerDark, ("2A2A2A", 0f) }, { Swatch.Chip, ("5B6068", 0f) },
            { Swatch.ChipRim, ("C9CED6", 0f) },
            { Swatch.Denim, ("2F3A57", 0f) }, { Swatch.DenimDark, ("222B42", 0f) }, { Swatch.Jacket, ("23252B", 0f) },
            { Swatch.JacketDark, ("16171B", 0f) }, { Swatch.Sneaker, ("2B2B2E", 0f) }, { Swatch.SneakerSole, ("DCDCD6", 0f) },
            { Swatch.Bag, ("4A4E3A", 0f) }, { Swatch.BagStrap, ("2B2D24", 0f) }, { Swatch.Beard, ("3A2A22", 0f) },
            { Swatch.Beanie, ("3B3B45", 0f) }
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
