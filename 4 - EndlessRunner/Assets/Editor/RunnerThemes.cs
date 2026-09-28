using System;
using UnityEngine;

namespace Portfolio.EndlessRunner.EditorTools
{
    /// <summary>The recipes of every model of a theme, so the art builder builds any theme from the same code.</summary>
    internal sealed class ModelSet
    {
        public Func<MeshBuilder> Torso, Head, UpperArm, Forearm, Thigh, Shin;
        public Func<MeshBuilder> Hurdle, Barrier, CrateStack, BarrelStack, CartBody, Wheel;
        /// <summary>A wagon of the given length in the given livery (0 to 3).</summary>
        public Func<float, int, MeshBuilder> Wagon;
        /// <summary>A ramp of the given width, height and length.</summary>
        public Func<float, float, float, MeshBuilder> Ramp;
        public Func<float, MeshBuilder> Bridge;
        public Func<MeshBuilder> Coin, Gem, Magnet, Shield, Multiplier, SuperJump, JumpPadBase, JumpPadMembrane, FinishArch;
    }


    /// <summary>A piece of scenery of a world: its model and how much of the track it takes.</summary>
    internal sealed class SceneryRecipe
    {
        public string Biome;
        public string Name;
        public Func<MeshBuilder> Model;
        public float Length = 1f;
        /// <summary>Big and far away: casts no shadow.</summary>
        public bool NoShadow;
    }


    /// <summary>
    /// Everything that tells the generators apart between two themes: where the theme's files go, its models and
    /// scenery, the style of its icons and panels, its materials and the proportions of its runner. The builders
    /// take a spec and write the theme; nothing in them is written for one theme only.
    /// </summary>
    internal sealed class ThemeArt
    {
        /// <summary>The asset name of the theme and the key of its folders ("Classic", "NightShift").</summary>
        public string Key;
        public string DisplayName;
        public string Description;
        /// <summary>The folder of the theme's textures, icons, materials, models and fonts, e.g. "Art/Themes/NightShift".</summary>
        public string Art;
        /// <summary>The folder of the theme's pieces and scenery prefabs, e.g. "Prefabs/Themes/NightShift".</summary>
        public string Prefabs;
        /// <summary>The folder of the theme's worlds (the RunnerTheme assets) and its piece catalog.</summary>
        public string Config;
        /// <summary>The game theme asset, e.g. "Config/Themes/Game/NightShift.asset".</summary>
        public string Asset;
        public string[] Biomes;

        public ModelSet Models;
        public SceneryRecipe[] Scenery;
        public TextureFactory.IconStyle Icons;
        public TextureFactory.PanelStyle Panels;

        /// <summary>The smoothness of the palette material: wet streets shine more than cartoon grass.</summary>
        public float PaletteSmoothness = 0.18f;
        /// <summary>The coin's own material colour, or null when the coin is coloured through the palette.</summary>
        public Color? CoinColor;
        public Color HaloGold, HaloGem, HaloMagnet, HaloShield, HaloMultiplier, HaloSuperJump, ShieldRing;
        public Color SparkleTint = Color.white;
        public Color DustColor = new Color(0.85f, 0.8f, 0.7f, 0.55f);

        /// <summary>The proportions of the runner: where the joints sit (see <see cref="RunnerLook"/>).</summary>
        public float HipsHeight = 0.78f;
        public Vector3 Neck = new Vector3(0f, 0.54f, 0f);
        public Vector3 Shoulder = new Vector3(0.265f, 0.46f, 0f);
        public float Elbow = -0.25f;
        public float Hip = 0.11f;
        public float Knee = -0.36f;

        /// <summary>A font file under the theme's Fonts folder to bake, or null for the project's default font.</summary>
        public string FontFile;
        public string FontName;

        public bool IsClassic => Key == "Classic";

        public string Textures => $"{Art}/Textures";
        public string IconFolder => $"{Art}/Icons";
        public string Materials => $"{Art}/Materials";
        public string ModelFolder => $"{Art}/Models";
        public string Fonts => $"{Art}/Fonts";
        public string Pieces => $"{Prefabs}/Pieces";
        public string SceneryFolder => $"{Prefabs}/Scenery";
        public string Catalog => IsClassic ? "Config/PieceCatalog.asset" : $"{Config}/PieceCatalog.asset";

        public string Biome(string key)
        {
            return $"{Config}/{key}.asset";
        }
    }


    /// <summary>The themes of the Endless Runner: the classic cartoon look and the Night Shift, a courier's run through the night city.</summary>
    internal static class RunnerThemes
    {
        public static readonly ThemeArt Classic = new ThemeArt
        {
            Key = "Classic",
            DisplayName = "Classic",
            Description = "The bright cartoon runner: five sunny, snowy, moonlit and fiery worlds, a kid in a hoodie and gold coins.",
            Art = "Art",
            Prefabs = "Prefabs",
            Config = "Config/Themes",
            Asset = "Config/Themes/Game/Classic.asset",
            Biomes = new[] { "Meadow", "Desert", "Snow", "Night", "Volcano" },
            Models = new ModelSet
            {
                Torso = RunnerModels.Torso, Head = RunnerModels.Head, UpperArm = RunnerModels.UpperArm, Forearm = RunnerModels.Forearm,
                Thigh = RunnerModels.Thigh, Shin = RunnerModels.Shin,
                Hurdle = RunnerModels.Hurdle, Barrier = RunnerModels.Barrier, CrateStack = RunnerModels.CrateStack, BarrelStack = RunnerModels.BarrelStack,
                CartBody = RunnerModels.CartBody, Wheel = () => RunnerModels.Wheel(),
                Wagon = (length, livery) => RunnerModels.Wagon(length, WagonBodies[livery], WagonRibs[livery]),
                Ramp = RunnerModels.Ramp, Bridge = RunnerModels.Bridge,
                Coin = RunnerModels.Coin, Gem = () => RunnerModels.Gem(), Magnet = () => RunnerModels.Magnet(), Shield = () => RunnerModels.Shield(),
                Multiplier = () => RunnerModels.StarPower(), SuperJump = () => RunnerModels.SpringShoe(),
                JumpPadBase = RunnerModels.JumpPadBase, JumpPadMembrane = () => RunnerModels.JumpPadMembrane(), FinishArch = RunnerModels.FinishArch
            },
            Scenery = new[]
            {
                new SceneryRecipe { Biome = "Meadow", Name = "TreeRound", Model = () => SceneryModels.RoundTree(11, Swatch.Leaf, Swatch.LeafDark, Swatch.Bark) },
                new SceneryRecipe { Biome = "Meadow", Name = "AppleTree", Model = () => SceneryModels.RoundTree(23, Swatch.LeafLight, Swatch.Leaf, Swatch.Bark, Swatch.FlowerRed, true) },
                new SceneryRecipe { Biome = "Meadow", Name = "Poplar", Model = () => SceneryModels.Poplar(31) },
                new SceneryRecipe { Biome = "Meadow", Name = "Pine", Model = () => SceneryModels.Pine(41, Swatch.Pine, Swatch.PineDark, Swatch.Bark, false) },
                new SceneryRecipe { Biome = "Meadow", Name = "Bush", Model = () => SceneryModels.Bush(51, Swatch.Leaf, Swatch.LeafDark, Swatch.FlowerRed, true) },
                new SceneryRecipe { Biome = "Meadow", Name = "Flowers", Model = () => SceneryModels.Flowers(61) },
                new SceneryRecipe { Biome = "Meadow", Name = "Rock", Model = () => SceneryModels.Rock(71, Swatch.Gray, Swatch.DarkGray) },
                new SceneryRecipe { Biome = "Meadow", Name = "Mushroom", Model = () => SceneryModels.Mushroom(81, Swatch.MushroomRed, Swatch.White, Swatch.OffWhite) },
                new SceneryRecipe { Biome = "Meadow", Name = "Cottage", Model = SceneryModels.Cottage, Length = 2f },
                new SceneryRecipe { Biome = "Meadow", Name = "Fence", Model = () => SceneryModels.Fence(RunnerModels.TileLength), Length = RunnerModels.TileLength },
                new SceneryRecipe { Biome = "Desert", Name = "Saguaro", Model = () => SceneryModels.Saguaro(101) },
                new SceneryRecipe { Biome = "Desert", Name = "SaguaroTall", Model = () => SceneryModels.Saguaro(117) },
                new SceneryRecipe { Biome = "Desert", Name = "BarrelCactus", Model = () => SceneryModels.BarrelCactus(3) },
                new SceneryRecipe { Biome = "Desert", Name = "Mesa", Model = () => SceneryModels.Mesa(131), Length = 8f, NoShadow = true },
                new SceneryRecipe { Biome = "Desert", Name = "DesertRock", Model = () => SceneryModels.DesertRock(141) },
                new SceneryRecipe { Biome = "Desert", Name = "PalmTree", Model = () => SceneryModels.PalmTree(151) },
                new SceneryRecipe { Biome = "Desert", Name = "DeadBush", Model = () => SceneryModels.DeadBush(161) },
                new SceneryRecipe { Biome = "Desert", Name = "Signpost", Model = SceneryModels.Signpost },
                new SceneryRecipe { Biome = "Snow", Name = "SnowPine", Model = () => SceneryModels.Pine(201, Swatch.Pine, Swatch.PineDark, Swatch.Bark, true) },
                new SceneryRecipe { Biome = "Snow", Name = "SnowPineTall", Model = () => SceneryModels.Pine(211, Swatch.PineDark, Swatch.Pine, Swatch.Bark, true, 1.5f) },
                new SceneryRecipe { Biome = "Snow", Name = "Snowman", Model = SceneryModels.Snowman },
                new SceneryRecipe { Biome = "Snow", Name = "IceRock", Model = () => SceneryModels.Rock(221, Swatch.Ice, Swatch.IceDeep) },
                new SceneryRecipe { Biome = "Snow", Name = "IceCrystals", Model = () => SceneryModels.Crystals(231, Swatch.Ice, Swatch.GlowCyan) },
                new SceneryRecipe { Biome = "Snow", Name = "SnowBush", Model = () => SceneryModels.Bush(241, Swatch.Snow, Swatch.SnowShade) },
                new SceneryRecipe { Biome = "Snow", Name = "SnowPole", Model = SceneryModels.SnowPole },
                new SceneryRecipe { Biome = "Night", Name = "NightTree", Model = () => SceneryModels.RoundTree(301, Swatch.NightLeaf, Swatch.PineDark, Swatch.NightTrunk, Swatch.GlowYellow, true) },
                new SceneryRecipe { Biome = "Night", Name = "NightPine", Model = () => SceneryModels.Pine(311, Swatch.NightLeaf, Swatch.PineDark, Swatch.NightTrunk, false) },
                new SceneryRecipe { Biome = "Night", Name = "GlowMushrooms", Model = () => SceneryModels.GlowMushrooms(321) },
                new SceneryRecipe { Biome = "Night", Name = "NightCrystals", Model = () => SceneryModels.Crystals(331, Swatch.Violet, Swatch.GlowPurple, 1.3f) },
                new SceneryRecipe { Biome = "Night", Name = "NightRock", Model = () => SceneryModels.Rock(341, Swatch.NightRock, Swatch.Charcoal) },
                new SceneryRecipe { Biome = "Night", Name = "LampPost", Model = SceneryModels.LampPost },
                new SceneryRecipe { Biome = "Volcano", Name = "BasaltColumns", Model = () => SceneryModels.BasaltColumns(401) },
                new SceneryRecipe { Biome = "Volcano", Name = "DeadTree", Model = () => SceneryModels.DeadTree(411) },
                new SceneryRecipe { Biome = "Volcano", Name = "LavaRock", Model = () => SceneryModels.LavaRock(421) },
                new SceneryRecipe { Biome = "Volcano", Name = "AshRock", Model = () => SceneryModels.Rock(431, Swatch.Ash, Swatch.Basalt) },
                new SceneryRecipe { Biome = "Volcano", Name = "Volcano", Model = SceneryModels.Volcano, Length = 16f, NoShadow = true },
                new SceneryRecipe { Biome = "Volcano", Name = "Torch", Model = SceneryModels.Torch }
            },
            Icons = TextureFactory.IconStyle.Classic,
            Panels = TextureFactory.PanelStyle.Classic,
            PaletteSmoothness = 0.18f,
            CoinColor = new Color(1f, 0.8f, 0.2f),
            HaloGold = new Color(1f, 0.8f, 0.3f, 0.6f),
            HaloGem = new Color(1f, 0.35f, 0.9f, 0.6f),
            HaloMagnet = new Color(1f, 0.3f, 0.3f, 0.7f),
            HaloShield = new Color(0.3f, 0.7f, 1f, 0.35f),
            HaloMultiplier = new Color(0.75f, 0.4f, 1f, 0.7f),
            HaloSuperJump = new Color(0.35f, 1f, 0.45f, 0.7f),
            ShieldRing = new Color(0.4f, 0.8f, 1f, 0.55f)
        };

        private static readonly Swatch[] WagonBodies = { Swatch.WagonRed, Swatch.WagonBlue, Swatch.WagonGreen, Swatch.WagonYellow };
        private static readonly Swatch[] WagonRibs = { Swatch.DarkRed, Swatch.Navy, Swatch.DarkGreen, Swatch.Orange };
        private static readonly Swatch[] TruckBodies = { Swatch.Container1, Swatch.Steel, Swatch.Tarp, Swatch.Container4 };
        private static readonly Swatch[] TruckStripes = { Swatch.SignWhite, Swatch.NeonAmber, Swatch.SignWhite, Swatch.Tar };
        private static readonly Swatch[] SubwayLines = { Swatch.NeonRed, Swatch.Container2, Swatch.NeonGreen, Swatch.NeonAmber };

        public static readonly ThemeArt NightShift = new ThemeArt
        {
            Key = "NightShift",
            DisplayName = "Night Shift",
            Description = "A courier's run through the night city: wet streets, the subway, the docks, the highway and the rooftops at dawn.",
            Art = "Art/Themes/NightShift",
            Prefabs = "Prefabs/Themes/NightShift",
            Config = "Config/Themes/NightShift",
            Asset = "Config/Themes/Game/NightShift.asset",
            Biomes = new[] { "Downtown", "Subway", "Docks", "Highway", "Rooftops" },
            Models = new ModelSet
            {
                Torso = UrbanModels.Torso, Head = UrbanModels.Head, UpperArm = UrbanModels.UpperArm, Forearm = UrbanModels.Forearm,
                Thigh = UrbanModels.Thigh, Shin = UrbanModels.Shin,
                Hurdle = UrbanModels.RoadBlock, Barrier = UrbanModels.Scaffold, CrateStack = UrbanModels.Dumpster, BarrelStack = UrbanModels.VendingMachines,
                CartBody = UrbanModels.MailCart, Wheel = () => RunnerModels.Wheel(Swatch.Tar, Swatch.SteelLight, false),
                Wagon = (length, livery) => length > 12f ? UrbanModels.SubwayCar(length, SubwayLines[livery]) : UrbanModels.BoxTruck(length, TruckBodies[livery], TruckStripes[livery]),
                Ramp = UrbanModels.LoadingRamp, Bridge = UrbanModels.SteelBridge,
                Coin = UrbanModels.CreditChip, Gem = () => RunnerModels.Gem(Swatch.NeonCyan, Swatch.Container2),
                Magnet = () => RunnerModels.Magnet(Swatch.SteelDark, Swatch.NeonRed), Shield = () => RunnerModels.Shield(Swatch.SteelLight, Swatch.Glass, Swatch.NeonCyan),
                Multiplier = () => RunnerModels.StarPower(Swatch.NeonAmber, Swatch.SignWhite), SuperJump = () => RunnerModels.SpringShoe(Swatch.Sneaker, Swatch.NeonGreen, Swatch.SneakerSole),
                JumpPadBase = UrbanModels.LiftPadBase, JumpPadMembrane = () => RunnerModels.JumpPadMembrane(Swatch.NeonAmber, Swatch.Tar), FinishArch = UrbanModels.FinishGantry
            },
            Scenery = new[]
            {
                new SceneryRecipe { Biome = "Downtown", Name = "BlockBrick", Model = () => UrbanScenery.Tower(501, 8f, 7f, 4, Swatch.Brick, 0.5f), Length = 7f, NoShadow = true },
                new SceneryRecipe { Biome = "Downtown", Name = "BlockConcrete", Model = () => UrbanScenery.Tower(511, 6f, 6f, 7, Swatch.Concrete, 0.4f), Length = 6f, NoShadow = true },
                new SceneryRecipe { Biome = "Downtown", Name = "BlockDark", Model = () => UrbanScenery.Tower(521, 10f, 8f, 3, Swatch.ConcreteDark, 0.6f), Length = 8f, NoShadow = true },
                new SceneryRecipe { Biome = "Downtown", Name = "BlockTall", Model = () => UrbanScenery.Tower(531, 7f, 7f, 10, Swatch.Glass, 0.45f), Length = 7f, NoShadow = true },
                new SceneryRecipe { Biome = "Downtown", Name = "ShopPink", Model = () => UrbanScenery.Shopfront(541, Swatch.NeonPink), Length = 6f },
                new SceneryRecipe { Biome = "Downtown", Name = "ShopCyan", Model = () => UrbanScenery.Shopfront(551, Swatch.NeonCyan), Length = 6f },
                new SceneryRecipe { Biome = "Downtown", Name = "Taxi", Model = () => UrbanScenery.Taxi(561), Length = 4.4f },
                new SceneryRecipe { Biome = "Downtown", Name = "TaxiDark", Model = () => UrbanScenery.Taxi(563), Length = 4.4f },
                new SceneryRecipe { Biome = "Downtown", Name = "ConcreteBarrier", Model = UrbanScenery.ConcreteBarrier, Length = 2f },
                new SceneryRecipe { Biome = "Downtown", Name = "Hydrant", Model = UrbanScenery.Hydrant },
                new SceneryRecipe { Biome = "Downtown", Name = "TrashCans", Model = () => UrbanScenery.TrashCans(571), Length = 2f },
                new SceneryRecipe { Biome = "Downtown", Name = "Billboard", Model = () => UrbanScenery.Billboard(581), Length = 6f },
                new SceneryRecipe { Biome = "Downtown", Name = "StreetLamp", Model = () => UrbanScenery.StreetLamp(Swatch.LampWhite) },
                new SceneryRecipe { Biome = "Subway", Name = "TunnelWall", Model = () => UrbanScenery.TunnelWall(RunnerModels.TileLength), Length = RunnerModels.TileLength },
                new SceneryRecipe { Biome = "Subway", Name = "TunnelArch", Model = UrbanScenery.TunnelArch, Length = 1.5f, NoShadow = true },
                new SceneryRecipe { Biome = "Subway", Name = "Pillar", Model = () => UrbanScenery.Pillar(601), Length = 1.5f },
                new SceneryRecipe { Biome = "Subway", Name = "PillarTall", Model = () => UrbanScenery.Pillar(602), Length = 1.5f },
                new SceneryRecipe { Biome = "Subway", Name = "Signal", Model = () => UrbanScenery.Signal(611) },
                new SceneryRecipe { Biome = "Subway", Name = "SignalGreen", Model = () => UrbanScenery.Signal(614) },
                new SceneryRecipe { Biome = "Subway", Name = "CableRun", Model = () => UrbanScenery.CableRun(RunnerModels.TileLength), Length = RunnerModels.TileLength },
                new SceneryRecipe { Biome = "Docks", Name = "ContainerStack", Model = () => UrbanScenery.ContainerStack(701), Length = 6f },
                new SceneryRecipe { Biome = "Docks", Name = "ContainerStackB", Model = () => UrbanScenery.ContainerStack(717), Length = 6f },
                new SceneryRecipe { Biome = "Docks", Name = "ContainerRed", Model = () => UrbanScenery.Container(Swatch.Container1), Length = 6f },
                new SceneryRecipe { Biome = "Docks", Name = "ContainerBlue", Model = () => UrbanScenery.Container(Swatch.Container2), Length = 6f },
                new SceneryRecipe { Biome = "Docks", Name = "Crane", Model = UrbanScenery.Crane, Length = 10f, NoShadow = true },
                new SceneryRecipe { Biome = "Docks", Name = "CratePile", Model = () => UrbanScenery.CratePile(731), Length = 2.2f },
                new SceneryRecipe { Biome = "Docks", Name = "Bollard", Model = UrbanScenery.Bollard },
                new SceneryRecipe { Biome = "Docks", Name = "FloodlightMast", Model = UrbanScenery.FloodlightMast },
                new SceneryRecipe { Biome = "Highway", Name = "GuardRail", Model = () => UrbanScenery.GuardRail(RunnerModels.TileLength), Length = RunnerModels.TileLength },
                new SceneryRecipe { Biome = "Highway", Name = "Cone", Model = UrbanScenery.Cone },
                new SceneryRecipe { Biome = "Highway", Name = "ConeRow", Model = UrbanScenery.ConeRow, Length = 2f },
                new SceneryRecipe { Biome = "Highway", Name = "RoadSign", Model = () => UrbanScenery.RoadSign(801), Length = 1f },
                new SceneryRecipe { Biome = "Highway", Name = "RoadSignB", Model = () => UrbanScenery.RoadSign(802), Length = 1f },
                new SceneryRecipe { Biome = "Highway", Name = "Overpass", Model = UrbanScenery.Overpass, Length = 4f, NoShadow = true },
                new SceneryRecipe { Biome = "Highway", Name = "HighwayLight", Model = UrbanScenery.HighwayLight },
                new SceneryRecipe { Biome = "Highway", Name = "Wreck", Model = () => UrbanScenery.Wreck(811), Length = 4.2f },
                new SceneryRecipe { Biome = "Highway", Name = "ConcreteBarrier", Model = UrbanScenery.ConcreteBarrier, Length = 2f },
                new SceneryRecipe { Biome = "Rooftops", Name = "AcUnit", Model = () => UrbanScenery.AcUnit(901), Length = 1.6f },
                new SceneryRecipe { Biome = "Rooftops", Name = "AcUnitTwin", Model = () => UrbanScenery.AcUnit(902), Length = 1.6f },
                new SceneryRecipe { Biome = "Rooftops", Name = "WaterTower", Model = UrbanScenery.WaterTower, Length = 3f },
                new SceneryRecipe { Biome = "Rooftops", Name = "Antenna", Model = () => UrbanScenery.Antenna(911) },
                new SceneryRecipe { Biome = "Rooftops", Name = "AntennaTall", Model = () => UrbanScenery.Antenna(913) },
                new SceneryRecipe { Biome = "Rooftops", Name = "Skylight", Model = UrbanScenery.Skylight, Length = 3.2f },
                new SceneryRecipe { Biome = "Rooftops", Name = "Vent", Model = UrbanScenery.Vent },
                new SceneryRecipe { Biome = "Rooftops", Name = "Parapet", Model = () => UrbanScenery.Parapet(RunnerModels.TileLength), Length = RunnerModels.TileLength },
                new SceneryRecipe { Biome = "Rooftops", Name = "Skyscraper", Model = () => UrbanScenery.Skyscraper(921), Length = 12f, NoShadow = true },
                new SceneryRecipe { Biome = "Rooftops", Name = "SkyscraperB", Model = () => UrbanScenery.Skyscraper(937), Length = 12f, NoShadow = true }
            },
            Icons = new TextureFactory.IconStyle
            {
                Ink = new Color(0.05f, 0.05f, 0.06f, 1f),
                Gloss = 0.25f,
                Gold = new Color(0.62f, 0.66f, 0.72f), GoldDark = new Color(0.36f, 0.39f, 0.45f), GoldOutline = new Color(0.08f, 0.09f, 0.11f), GoldCore = new Color(0.2f, 0.9f, 1f),
                ChipCoin = true,
                Gem = new Color(0.3f, 0.85f, 1f), GemDark = new Color(0.1f, 0.4f, 0.6f), GemOutline = new Color(0.02f, 0.12f, 0.2f), GemFacet = new Color(0.8f, 0.97f, 1f), GemFacetDark = new Color(0.5f, 0.85f, 1f),
                Heart = new Color(1f, 0.32f, 0.3f), HeartDark = new Color(0.6f, 0.08f, 0.1f), HeartOutline = new Color(0.15f, 0.02f, 0.03f),
                Empty = new Color(0.22f, 0.23f, 0.26f, 0.8f), EmptyDark = new Color(0.12f, 0.12f, 0.14f, 0.8f), EmptyOutline = new Color(0.55f, 0.57f, 0.62f),
                StarEmpty = new Color(0.22f, 0.23f, 0.26f, 0.8f), StarEmptyDark = new Color(0.12f, 0.12f, 0.14f, 0.8f), StarEmptyOutline = new Color(0.55f, 0.57f, 0.62f),
                MagnetTip = new Color(0.78f, 0.8f, 0.84f), MagnetTipDark = new Color(0.45f, 0.48f, 0.52f),
                ShieldStar = new Color(0.9f, 0.95f, 1f), ShieldStarDark = new Color(0.6f, 0.85f, 1f), MultiplierStar = new Color(0.96f, 0.95f, 0.9f),
                SpringShoe = new Color(0.3f, 0.3f, 0.33f), SpringShoeDark = new Color(0.15f, 0.15f, 0.17f),
                PauseDark = new Color(0.72f, 0.74f, 0.76f), InfinityDark = new Color(0.72f, 0.74f, 0.76f),
                Star = new Color(1f, 0.72f, 0.2f), StarDark = new Color(0.8f, 0.42f, 0.05f), StarOutline = new Color(0.2f, 0.1f, 0.02f),
                Metal = new Color(0.7f, 0.73f, 0.78f), MetalDark = new Color(0.4f, 0.43f, 0.48f),
                Lock = new Color(1f, 0.72f, 0.2f), LockDark = new Color(0.75f, 0.4f, 0.05f),
                Magnet = new Color(0.9f, 0.25f, 0.25f), MagnetDark = new Color(0.5f, 0.08f, 0.1f),
                Shield = new Color(0.35f, 0.75f, 1f), ShieldDark = new Color(0.1f, 0.3f, 0.6f), ShieldOutline = new Color(0.75f, 0.78f, 0.82f),
                Multiplier = new Color(1f, 0.72f, 0.2f), MultiplierDark = new Color(0.7f, 0.38f, 0.05f), MultiplierOutline = new Color(0.2f, 0.1f, 0.02f),
                Spring = new Color(0.45f, 1f, 0.55f), SpringDark = new Color(0.15f, 0.6f, 0.25f), SpringSole = new Color(0.86f, 0.86f, 0.84f), SpringSoleDark = new Color(0.55f, 0.55f, 0.55f),
                Paper = new Color(0.94f, 0.94f, 0.92f), PaperDark = new Color(0.72f, 0.74f, 0.76f),
                Pole = new Color(0.55f, 0.57f, 0.6f), PoleDark = new Color(0.32f, 0.34f, 0.37f),
                Skin = new Color(0.9f, 0.72f, 0.56f), SkinDark = new Color(0.72f, 0.52f, 0.38f),
                Cap = new Color(0.2f, 0.21f, 0.25f), CapDark = new Color(0.08f, 0.08f, 0.1f), Hooded = true,
                Check = new Color(0.45f, 1f, 0.55f), CheckDark = new Color(0.15f, 0.6f, 0.25f)
            },
            Panels = new TextureFactory.PanelStyle { Radius = 8f, Glass = true },
            PaletteSmoothness = 0.42f,
            CoinColor = null,
            HaloGold = new Color(0.3f, 0.85f, 1f, 0.35f),
            HaloGem = new Color(0.3f, 0.85f, 1f, 0.45f),
            HaloMagnet = new Color(1f, 0.25f, 0.2f, 0.5f),
            HaloShield = new Color(0.35f, 0.7f, 1f, 0.3f),
            HaloMultiplier = new Color(1f, 0.7f, 0.2f, 0.5f),
            HaloSuperJump = new Color(0.4f, 1f, 0.5f, 0.5f),
            ShieldRing = new Color(0.5f, 0.85f, 1f, 0.45f),
            SparkleTint = new Color(0.85f, 0.95f, 1f),
            DustColor = new Color(0.55f, 0.6f, 0.68f, 0.5f),
            HipsHeight = 0.89f,
            Neck = new Vector3(0f, 0.56f, 0f),
            Shoulder = new Vector3(0.23f, 0.48f, 0f),
            Elbow = -0.28f,
            Hip = 0.1f,
            Knee = -0.42f,
            FontFile = "BarlowCondensed-SemiBold.ttf",
            FontName = "BarlowCondensed"
        };

        public static readonly ThemeArt[] All = { Classic, NightShift };
    }
}
