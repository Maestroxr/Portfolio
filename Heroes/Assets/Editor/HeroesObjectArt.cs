using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Builds what stands on the map: the mines and piles, the treasures and shrines, the buildings a hero visits, and
    /// the towns of the three factions in the colors of the players. Each one is a small cluster of the downloaded
    /// models, so a mine is a pit head with its ore beside it and a town is a keep with houses around it.
    /// </summary>
    internal static class HeroesObjectArt
    {
        /// <summary>A model placed in a cluster: where it stands, how it is turned, how tall it is, and its own color.</summary>
        private readonly struct Bit
        {
            public readonly string Model;
            public readonly float X;
            public readonly float Z;
            public readonly float Yaw;
            public readonly float Height;
            /// <summary>Clear to take the cluster's color, else a color of its own.</summary>
            public readonly Color Tint;

            public Bit(string model, float x = 0f, float z = 0f, float yaw = 0f, float height = 0f, Color tint = default)
            {
                Model = model;
                X = x;
                Z = z;
                Yaw = yaw;
                Height = height;
                Tint = tint;
            }
        }

        private static readonly string[] ColorFolders = { "Red", "Blue", "Green", "Yellow", "Red" };

        public static void Build(HeroesArt art)
        {
            art.objects.Clear();
            art.towns.Clear();
            Mines(art);
            Piles(art);
            Treasures(art);
            Buildings(art);
            Dwellings(art);
            Artifacts(art);
            Towns(art);
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject Cluster(string name, string folder, Color tint, params Bit[] bits)
        {
            GameObject root = HeroesArtBuilder.Root(name);
            foreach (Bit bit in bits)
            {
                Color color = bit.Tint.a > 0f ? bit.Tint * tint : tint;
                HeroesArtBuilder.Piece(root.transform, bit.Model, new Vector3(bit.X, 0f, bit.Z), bit.Yaw, 1f, bit.Height, color);
            }
            return HeroesArtBuilder.Save(root, $"Art/Generated/{folder}/{name}.prefab");
        }

        private static GameObject Cluster(string name, string folder, params Bit[] bits)
        {
            return Cluster(name, folder, Color.white, bits);
        }

        private static void Add(HeroesArt art, ObjectKind kind, int subtype, GameObject prefab)
        {
            art.objects.Add(new HeroesArt.ObjectArt { kind = kind, subtype = subtype, prefab = prefab });
        }

        // ------------------------------------------------------------------ the map

        /// <summary>A mine: the pit head of the resource it gives, with a sample of it heaped by the door.</summary>
        private static void Mines(HeroesArt art)
        {
            (ResourceKind kind, string building, Bit sample)[] table =
            {
                (ResourceKind.Gold, "building_mine", new Bit("KayKit/Dungeon/coin_stack_large", 1.5f, -1.0f, 20f, 0.5f)),
                (ResourceKind.Wood, "building_lumbermill", new Bit("KayKit/Props/resource_lumber", 1.5f, -1.0f, -15f, 0.7f)),
                (ResourceKind.Ore, "building_mine", new Bit("KayKit/Props/resource_stone", 1.5f, -1.0f, 25f, 0.7f)),
                (ResourceKind.Mercury, "building_mine", new Bit("Quaternius/Items/Potion3_Filled", 1.5f, -1.0f, 0f, 0.7f)),
                (ResourceKind.Sulfur, "building_mine", new Bit("Quaternius/Items/Crystal4", 1.5f, -1.0f, 0f, 0.9f)),
                (ResourceKind.Crystal, "building_mine", new Bit("Quaternius/Items/Crystal1", 1.5f, -1.0f, 0f, 1.0f)),
                (ResourceKind.Gems, "building_mine", new Bit("Quaternius/Items/Crystal3", 1.5f, -1.0f, 0f, 0.9f))
            };
            foreach ((ResourceKind kind, string building, Bit sample) in table)
            {
                GameObject prefab = Cluster($"Mine{kind}", "Objects",
                    new Bit($"KayKit/Buildings/Yellow/{building}_yellow", -0.3f, 0.3f, 0f, 1.3f),
                    sample,
                    new Bit("KayKit/Props/wheelbarrow", -1.3f, -1.1f, 40f, 0.6f));
                Add(art, ObjectKind.Mine, (int)kind, prefab);
            }
        }

        /// <summary>A pile lying on the ground for the taking.</summary>
        private static void Piles(HeroesArt art)
        {
            (ResourceKind kind, Bit[] bits)[] table =
            {
                (ResourceKind.Gold, new[]
                {
                    new Bit("KayKit/Dungeon/coin_stack_large", 0f, 0f, 0f, 0.4f),
                    new Bit("KayKit/Dungeon/coin_stack_small", 0.35f, -0.25f, 30f, 0.22f),
                    new Bit("KayKit/Dungeon/coin", -0.3f, 0.2f, 0f, 0.06f)
                }),
                (ResourceKind.Wood, new[]
                {
                    new Bit("KayKit/Props/resource_lumber", 0f, 0f, 0f, 0.55f),
                    new Bit("KayKit/Dungeon/trunk_small_A", 0.4f, 0.3f, 60f, 0.3f)
                }),
                (ResourceKind.Ore, new[]
                {
                    new Bit("KayKit/Props/resource_stone", 0f, 0f, 0f, 0.55f),
                    new Bit("KayKit/Nature/rock_single_D", 0.45f, 0.25f, 0f, 0.35f)
                }),
                (ResourceKind.Mercury, new[]
                {
                    new Bit("Quaternius/Items/Potion3_Filled", 0f, 0f, 0f, 0.55f),
                    new Bit("Quaternius/Items/Potion5_Filled", 0.3f, -0.2f, 25f, 0.45f)
                }),
                (ResourceKind.Sulfur, new[]
                {
                    new Bit("Quaternius/Items/Crystal4", 0f, 0f, 0f, 0.7f),
                    new Bit("Quaternius/Items/Mineral", 0.35f, 0.25f, 40f, 0.35f)
                }),
                (ResourceKind.Crystal, new[]
                {
                    new Bit("Quaternius/Items/Crystal1", 0f, 0f, 0f, 0.8f),
                    new Bit("Quaternius/Items/Crystal2", 0.35f, -0.2f, 30f, 0.5f)
                }),
                (ResourceKind.Gems, new[]
                {
                    new Bit("Quaternius/Items/Crystal3", 0f, 0f, 0f, 0.65f),
                    new Bit("Quaternius/Items/Crystal5", 0.3f, 0.25f, -20f, 0.45f)
                })
            };
            foreach ((ResourceKind kind, Bit[] bits) in table)
            {
                Add(art, ObjectKind.Resource, (int)kind, Cluster($"Pile{kind}", "Objects", bits));
            }
        }

        private static void Treasures(HeroesArt art)
        {
            Add(art, ObjectKind.Treasure, -1, Cluster("Treasure", "Objects",
                new Bit("KayKit/Dungeon/chest_gold", 0f, 0f, 0f, 0.7f),
                new Bit("KayKit/Dungeon/coin_stack_small", 0.55f, -0.35f, 20f, 0.2f)));
            Add(art, ObjectKind.Campfire, -1, Cluster("Campfire", "Objects",
                new Bit("KayKit/Dungeon/rubble_half", 0f, 0f, 0f, 0.25f),
                new Bit("KayKit/Dungeon/torch_lit", 0.05f, 0.05f, 0f, 1.1f),
                new Bit("KayKit/Props/crate_A_small", 0.6f, -0.45f, 35f, 0.35f)));
            Add(art, ObjectKind.Scroll, -1, Cluster("Scroll", "Objects",
                new Bit("Quaternius/Items/Scroll", 0f, 0f, 0f, 0.3f, new Color(1f, 0.95f, 0.8f)),
                new Bit("Quaternius/Items/Book2_Closed", 0.35f, 0.2f, 20f, 0.2f)));
        }

        /// <summary>The places a hero visits for a lasting gain.</summary>
        private static void Buildings(HeroesArt art)
        {
            Add(art, ObjectKind.Shrine, -1, Cluster("Shrine", "Objects",
                new Bit("KayKit/Halloween/shrine_candles", 0f, 0f, 0f, 1.7f),
                new Bit("KayKit/Halloween/candle_thin", 0.7f, -0.5f, 0f, 0.45f)));
            Add(art, ObjectKind.MercenaryCamp, -1, Cluster("MercenaryCamp", "Objects",
                new Bit("KayKit/Props/tent", 0f, 0.2f, 15f, 1.5f),
                new Bit("KayKit/Props/weaponrack", 1.0f, -0.6f, -25f, 1.0f),
                new Bit("KayKit/Props/barrel", -1.0f, -0.5f, 0f, 0.6f)));
            Add(art, ObjectKind.MarlettoTower, -1, Cluster("MarlettoTower", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_tower_B_yellow", 0f, 0f, 0f, 3.0f),
                new Bit("KayKit/Buildings/Neutral/wall_straight", -1.2f, -0.6f, 90f, 1.2f)));
            Add(art, ObjectKind.StarAxis, -1, Cluster("StarAxis", "Objects",
                new Bit("KayKit/Dungeon/pillar_decorated", 0f, 0f, 0f, 2.4f),
                new Bit("Quaternius/Items/Star", 0f, 0f, 0f, 0.7f)));
            Add(art, ObjectKind.GardenOfRevelation, -1, Cluster("GardenOfRevelation", "Objects",
                new Bit("KayKit/Nature/tree_single_B", 0f, 0.3f, 0f, 3.0f),
                new Bit("KayKit/Nature/waterplant_A", 0.9f, -0.6f, 0f, 0.7f),
                new Bit("KayKit/Nature/waterlily_A", -0.8f, -0.7f, 30f, 0.2f)));
            Add(art, ObjectKind.Arena, -1, Cluster("Arena", "Objects",
                new Bit("KayKit/Buildings/Neutral/building_stage_A", 0f, 0f, 0f, 1.0f),
                new Bit("KayKit/Props/target", 0.9f, 0.7f, 200f, 1.1f),
                new Bit("KayKit/Props/weaponrack", -1.0f, 0.5f, 25f, 1.0f)));
            Add(art, ObjectKind.LearningStone, -1, Cluster("LearningStone", "Objects",
                new Bit("KayKit/Dungeon/column", 0f, 0f, 0f, 1.8f),
                new Bit("Quaternius/Items/Book1_Closed", 0f, 0f, 0f, 0.35f)));
            Add(art, ObjectKind.WitchHut, -1, Cluster("WitchHut", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_home_B_yellow", 0f, 0f, 0f, 1.5f),
                new Bit("KayKit/Halloween/post_skull", 1.0f, -0.7f, 0f, 1.3f),
                new Bit("KayKit/Halloween/candle_melted", -0.9f, -0.6f, 0f, 0.35f)));
            Add(art, ObjectKind.Windmill, -1, Cluster("Windmill", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_windmill_yellow", 0f, 0f, 0f, 2.4f),
                new Bit("KayKit/Props/sack", 0.9f, -0.6f, 0f, 0.4f)));
            Add(art, ObjectKind.WaterWheel, -1, Cluster("WaterWheel", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_watermill_yellow", 0f, 0f, 0f, 2.4f),
                new Bit("KayKit/Props/bucket_water", 1.0f, -0.6f, 0f, 0.35f)));
            Add(art, ObjectKind.Watchtower, -1, Cluster("Watchtower", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_tower_A_yellow", 0f, 0f, 0f, 2.8f),
                new Bit("KayKit/Props/bucket_arrows", 0.9f, -0.6f, 0f, 0.4f)));
            Add(art, ObjectKind.Stables, -1, Cluster("Stables", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_barracks_yellow", 0f, 0.2f, 0f, 2.0f),
                new Bit("KayKit/Buildings/Neutral/fence_wood_straight", 0f, -1.2f, 0f, 0.8f),
                new Bit("Quaternius/Animals/Horse", 0.9f, -0.7f, -60f, 1.8f)));
            Add(art, ObjectKind.Fountain, -1, Cluster("Fountain", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_well_yellow", 0f, 0f, 0f, 1.3f),
                new Bit("KayKit/Props/bucket_water", 0.7f, -0.5f, 0f, 0.35f)));
            Add(art, ObjectKind.Temple, -1, Cluster("Temple", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_church_yellow", 0f, 0f, 0f, 2.6f),
                new Bit("KayKit/Dungeon/candle_triple", 0.9f, -0.7f, 0f, 0.5f)));
            Add(art, ObjectKind.TreeOfKnowledge, -1, Cluster("TreeOfKnowledge", "Objects",
                new Bit("KayKit/Nature/tree_single_A", 0f, 0f, 0f, 3.6f),
                new Bit("Quaternius/Items/Book4_Closed", 0.9f, -0.8f, 25f, 0.4f)));
        }

        /// <summary>Dwellings of the wilds, one look for each faction's creatures.</summary>
        private static void Dwellings(HeroesArt art)
        {
            GameObject castle = Cluster("DwellingCastle", "Objects",
                new Bit("KayKit/Buildings/Yellow/building_home_A_yellow", 0f, 0.2f, 0f, 1.4f),
                new Bit("KayKit/Props/tent", 1.1f, -0.7f, 20f, 1.1f),
                new Bit("KayKit/Props/target", -1.0f, -0.6f, 180f, 0.9f));
            GameObject necropolis = Cluster("DwellingNecropolis", "Objects",
                new Bit("KayKit/Halloween/crypt", 0f, 0.2f, 0f, 1.9f, new Color(0.44f, 0.42f, 0.52f)),
                new Bit("KayKit/Halloween/grave_A", 1.0f, -0.7f, 15f, 0.5f),
                new Bit("KayKit/Halloween/bone_A", -0.9f, -0.6f, 40f, 0.2f));
            GameObject stronghold = Cluster("DwellingStronghold", "Objects",
                new Bit("KayKit/Props/tent", 0f, 0.2f, 0f, 1.5f),
                new Bit("KayKit/Halloween/post_skull", 1.0f, -0.6f, 0f, 1.3f),
                new Bit("KayKit/Props/barrel", -1.0f, -0.6f, 0f, 0.6f));
            GameObject wilds = Cluster("DwellingWilds", "Objects",
                new Bit("KayKit/Nature/rock_single_C", 0f, 0.3f, 0f, 1.6f),
                new Bit("KayKit/Nature/tree_single_B", -1.0f, -0.4f, 0f, 2.6f),
                new Bit("KayKit/Props/crate_A_big", 0.9f, -0.6f, 25f, 0.6f));

            Add(art, ObjectKind.Dwelling, -1, wilds);
            foreach (CreatureDef def in Creatures.All)
            {
                GameObject prefab = def.Faction == Faction.Castle ? castle
                    : def.Faction == Faction.Necropolis ? necropolis
                    : def.Faction == Faction.Stronghold ? stronghold
                    : null;
                if (prefab != null)
                {
                    Add(art, ObjectKind.Dwelling, (int)def.Id, prefab);
                }
            }
        }

        /// <summary>Every artifact lies on the map as the thing it is, on a little pedestal.</summary>
        private static void Artifacts(HeroesArt art)
        {
            var made = new Dictionary<string, GameObject>();
            for (int i = 0; i < Portfolio.Heroes.Artifacts.Count; i++)
            {
                ArtifactDef def = Portfolio.Heroes.Artifacts.Get((ArtifactId)i);
                if (def == null || string.IsNullOrEmpty(def.Item))
                {
                    continue;
                }
                if (!made.TryGetValue(def.Item, out GameObject prefab))
                {
                    prefab = Cluster($"Artifact{def.Item}", "Artifacts",
                        new Bit("KayKit/Dungeon/rubble_half", 0f, 0f, 0f, 0.2f),
                        new Bit($"Quaternius/Items/{def.Item}", 0f, 0f, 0f, 0.85f));
                    made[def.Item] = prefab;
                }
                Add(art, ObjectKind.Artifact, i, prefab);
            }
            if (made.Count > 0)
            {
                Add(art, ObjectKind.Artifact, -1, made["Chalice"] != null ? made["Chalice"] : null);
            }
        }

        // ------------------------------------------------------------------ towns

        private static void Towns(HeroesArt art)
        {
            foreach (Faction faction in new[] { Faction.Castle, Faction.Necropolis, Faction.Stronghold })
            {
                var byColor = new GameObject[5];
                for (int color = 0; color < 5; color++)
                {
                    string folder = ColorFolders[color];
                    string suffix = folder.ToLowerInvariant();
                    // A town nobody holds has gone grey: the banners are down and the stone is bare.
                    Color tint = color == 4 ? new Color(0.46f, 0.47f, 0.52f) : Color.white;
                    string banner = color == 4 ? "white" : new[] { "red", "blue", "green", "yellow" }[color];
                    byColor[color] = Cluster($"{faction}Town{color}", "Towns", tint, TownBits(faction, suffix, folder, banner));
                }
                art.towns.Add(new HeroesArt.TownArt { faction = faction, byColor = byColor });
            }
        }

        private static Bit[] TownBits(Faction faction, string suffix, string folder, string banner)
        {
            switch (faction)
            {
                case Faction.Necropolis:
                    // A cathedral gone cold: the stone is dark, the tombs are open and the banners are the only color.
                    Color slate = new Color(0.44f, 0.42f, 0.52f);
                    return new[]
                    {
                        new Bit($"KayKit/Buildings/{folder}/building_church_{suffix}", 0f, 0.5f, 0f, 3.6f, slate),
                        new Bit("KayKit/Halloween/crypt", -2.0f, -0.3f, 25f, 1.7f, slate),
                        new Bit("KayKit/Halloween/arch_gate", 0f, -1.9f, 0f, 2.3f, slate),
                        new Bit("KayKit/Halloween/grave_A", 1.7f, -1.1f, 10f, 0.55f, slate),
                        new Bit("KayKit/Halloween/gravestone", 2.1f, 0.5f, 20f, 0.9f, slate),
                        new Bit("KayKit/Halloween/tree_dead_medium", -2.1f, 1.5f, 0f, 3.0f),
                        new Bit("KayKit/Halloween/post_lantern", 1.1f, -1.9f, 0f, 1.6f),
                        new Bit($"KayKit/Dungeon/banner_patternA_{banner}", -1.1f, -1.9f, 0f, 2.0f)
                    };
                case Faction.Stronghold:
                    return new[]
                    {
                        new Bit($"KayKit/Buildings/{folder}/building_barracks_{suffix}", 0f, 0.3f, 0f, 3.0f),
                        new Bit($"KayKit/Buildings/{folder}/building_tower_base_{suffix}", 1.6f, 0.9f, 0f, 1.9f),
                        new Bit("KayKit/Props/tent", -1.5f, -0.7f, 25f, 1.4f),
                        new Bit("KayKit/Props/tent", 1.5f, -0.8f, -30f, 1.2f),
                        new Bit("KayKit/Props/weaponrack", -0.9f, -1.4f, 10f, 1.0f),
                        new Bit("KayKit/Halloween/post_skull", 1.0f, -1.5f, 0f, 1.5f),
                        new Bit("KayKit/Buildings/Neutral/fence_wood_straight", -1.8f, 0.6f, 90f, 0.9f),
                        new Bit($"KayKit/Props/flag_{(banner == "white" ? "red" : banner)}", 0f, -1.6f, 0f, 2.0f)
                    };
                default:
                    return new[]
                    {
                        new Bit($"KayKit/Buildings/{folder}/building_castle_{suffix}", 0f, 0.4f, 0f, 4.2f),
                        new Bit($"KayKit/Buildings/{folder}/building_church_{suffix}", -1.7f, 0.7f, 20f, 1.9f),
                        new Bit($"KayKit/Buildings/{folder}/building_home_A_{suffix}", 1.6f, -0.5f, -25f, 1.1f),
                        new Bit($"KayKit/Buildings/{folder}/building_home_B_{suffix}", -1.5f, -0.8f, 15f, 1.4f),
                        new Bit($"KayKit/Buildings/{folder}/building_tower_A_{suffix}", 1.7f, 0.9f, 0f, 2.4f),
                        new Bit("KayKit/Buildings/Neutral/wall_straight", 0.8f, -1.5f, 0f, 1.3f),
                        new Bit("KayKit/Buildings/Neutral/wall_straight_gate", -0.8f, -1.5f, 0f, 1.5f),
                        new Bit($"KayKit/Props/flag_{(banner == "white" ? "red" : banner)}", 0f, -1.7f, 0f, 2.0f)
                    };
            }
        }
    }
}
