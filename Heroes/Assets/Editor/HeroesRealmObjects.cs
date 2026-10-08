using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using V = Portfolio.Heroes.EditorTools.HeroesRealmVillage;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The map objects and towns of a theme made of the textured kits (<see cref="ModelSet.Kits"/>), and the arrow towers
    /// of its sieges. The buildings are houses of the village kit (<see cref="HeroesRealmVillage"/>) put together at the
    /// kit's size and drawn small, as miniatures on the map; around them stand the props of the props kit and the rocks,
    /// trees and flowers of the nature kit, and the treasures keep the item models of the packs. Every object takes the
    /// room its classic counterpart took (a hexagon is 2.08 m across) and leaves the front right free for the banner its
    /// owner plants there. A map shows many of them at once, on the web too, so each keeps to a budget of triangles: about
    /// 3k for what lies on the ground for the taking, 8k for a building, 40k for a town; the kit's heaviest pieces (carved
    /// corner stones, candelabra, iron fences, cobbled paths) give way to lighter ones that read the same from the map.
    /// </summary>
    internal static class HeroesRealmObjects
    {
        private const string Village = V.Kit;
        private const string Props = V.Props;
        private const string Nature = V.Nature;
        private const string Items = "Quaternius/Items";
        private const string Halloween = "KayKit/Halloween";

        private static HeroesThemeSpec Spec => HeroesThemeSpec.Current;
        private static string Generated => Spec.Generated;

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

        /// <summary>
        /// Builds an object at the kit's size under a holder scaled by <paramref name="scale"/> and saves it as a prefab
        /// of <paramref name="folder"/>.
        /// </summary>
        private static GameObject Make(string name, string folder, float scale, Action<Transform> build)
        {
            GameObject root = HeroesArtBuilder.Root(name);
            Transform model = V.Holder(root.transform, "Model", Vector3.zero, 0f, scale);
            build(model);
            V.Settle(root);
            return HeroesArtBuilder.Save(root, $"{Generated}/{folder}/{name}.prefab");
        }

        private static void Add(HeroesArt art, ObjectKind kind, int subtype, GameObject prefab)
        {
            art.objects.Add(new HeroesArt.ObjectArt { kind = kind, subtype = subtype, prefab = prefab });
        }

        /// <summary>The colour a player's banners and roofs are dyed (the theme's palette), or the grey of nobody.</summary>
        private static Color Player(int color)
        {
            Color[] players = Spec.Palette != null ? Spec.Palette.players : null;
            if (color < 4 && players != null && color < players.Length)
            {
                return players[color];
            }
            return color < 4 ? HeroesTheme.Palette.Active.Player(color) : new Color(0.72f, 0.72f, 0.74f);
        }

        /// <summary>A log of the kit (a roof's ridge log) lying along <paramref name="yaw"/>, its underside at the height of <paramref name="at"/>.</summary>
        private static GameObject Log(Transform parent, Vector3 at, float yaw, float length, float thick, Color tint = default)
        {
            return V.Prop(parent, $"{Village}/Roof_Log", at, Quaternion.Euler(0f, yaw, 0f), new Vector3(thick / 1.15f, thick / 1.39f, length / 10.7f), 0f, tint);
        }

        /// <summary>A log stood on its end: a stump, a stake of a palisade.</summary>
        private static GameObject Stump(Transform parent, Vector3 at, float height, float thick, Color tint = default)
        {
            return V.Prop(parent, $"{Village}/Roof_Log", at, Quaternion.Euler(90f, 0f, 0f), new Vector3(thick / 1.15f, thick / 1.39f, height / 10.7f), 0f, tint);
        }

        /// <summary>A model leaning <paramref name="lean"/> degrees toward <paramref name="toward"/> (a yaw), standing on the ground at <paramref name="at"/>.</summary>
        private static GameObject Leaning(Transform parent, string model, Vector3 at, float toward, float lean, float height, Color tint = default)
        {
            return V.Prop(parent, model, at, Quaternion.Euler(0f, toward, 0f) * Quaternion.Euler(lean, 0f, 0f), Vector3.one, height, tint);
        }

        /// <summary>A flat plank of the kit (a square of a plank floor) stretched and turned into a beam, a paddle or a sail.</summary>
        private static GameObject Plank(Transform parent, Vector3 center, Quaternion turn, Vector3 stretch)
        {
            GameObject plank = V.Prop(parent, $"{Village}/Roof_Wooden_2x1_Middle", Vector3.zero, turn, stretch);
            Place(plank, parent, center);
            return plank;
        }

        /// <summary>Moves a placed model so the middle of its bounds is at <paramref name="center"/>.</summary>
        private static void Place(GameObject piece, Transform parent, Vector3 center)
        {
            if (piece == null)
            {
                return;
            }
            Bounds bounds = V.LocalBounds(parent, piece);
            piece.transform.localPosition += center - bounds.center;
        }

        /// <summary>Grass, flowers and bushes of the nature kit about a place, for the green of a lived-in land.</summary>
        private static void Green(Transform parent, params (string piece, float x, float z, float height)[] plants)
        {
            int i = 0;
            foreach ((string piece, float x, float z, float height) in plants)
            {
                V.Prop(parent, $"{Nature}/{piece}", new Vector3(x, -0.05f, z), i++ * 67f % 360f, height);
            }
        }

        /// <summary>
        /// Candles burning on a slim post of stone <paramref name="height"/> high (none for 0: candles set on what stands
        /// at <paramref name="at"/>), their flames alight.
        /// </summary>
        private static void Candles(Transform parent, Vector3 at, float height)
        {
            if (height > 0f)
            {
                V.Prop(parent, $"{Village}/Prop_Chimney2", at, Quaternion.Euler(0f, 30f, 0f), new Vector3(0.36f, height / 3f, 0.36f));
            }
            Vector3 top = at + Vector3.up * height;
            V.Prop(parent, $"{Props}/Candle_2", top + new Vector3(-0.06f, 0f, 0.02f), 0f, 0.42f);
            V.Prop(parent, $"{Props}/Candle_1", top + new Vector3(0.09f, 0f, -0.04f), 0f, 0.26f);
            foreach ((float x, float z, float y) in new[] { (-0.06f, 0.02f, 0.42f), (0.09f, -0.04f, 0.26f) })
            {
                V.Paint(V.Prop(parent, $"{Items}/Crystal2", top + new Vector3(x, y, z), 0f, 0.11f), Embers);
            }
        }

        // ------------------------------------------------------------------ materials

        private static Material Sulfur => V.Gem(new Color(1f, 0.78f, 0.12f), 0.45f, "Sulfur");
        private static Material Crystal => V.Gem(new Color(0.6f, 0.86f, 1f), 0.32f, "Crystal");
        private static Material[] Jewels => new[]
        {
            V.Gem(new Color(0.95f, 0.12f, 0.18f), 0.42f, "Ruby"), V.Gem(new Color(0.12f, 0.82f, 0.32f), 0.4f, "Emerald"),
            V.Gem(new Color(0.2f, 0.38f, 1f), 0.45f, "Sapphire"), V.Gem(new Color(0.7f, 0.26f, 1f), 0.42f, "Amethyst")
        };
        private static Material Flame => V.Gem(new Color(1f, 0.34f, 0.05f), 1.5f, "Flame");
        private static Material Embers => V.Gem(new Color(1f, 0.68f, 0.18f), 1.3f, "Embers");
        private static Material Quicksilver => V.Metal(new Color(0.78f, 0.8f, 0.86f), "Quicksilver");
        /// <summary>The cold glow in the windows of the dead.</summary>
        private static Material Witchlight => V.Gem(new Color(0.55f, 1f, 0.75f), 0.9f, "Witchlight");
        /// <summary>The slate of the dead, violet in its grey.</summary>
        private static Material NecroSlate => V.Slate(new Color(0.5f, 0.42f, 0.7f), 0.66f, "Necropolis");
        /// <summary>Blue-grey slate, the roofs of the houses behind a castle's keep.</summary>
        private static Material Slate => V.Slate(new Color(0.46f, 0.52f, 0.62f), 0.72f, "Slate");
        /// <summary>Bare grey slate, the roofs of a town nobody holds.</summary>
        private static Material Bare => V.Slate(new Color(0.58f, 0.6f, 0.66f), 0.6f, "Bare");
        /// <summary>Raked sand: an arena's floor.</summary>
        private static Material Sand => V.Canvas(new Color(0.6f, 0.5f, 0.34f), "Sand");

        // ------------------------------------------------------------------ resources

        /// <summary>A heap of a resource about <paramref name="size"/> across, centred on <paramref name="at"/>.</summary>
        private static void Heap(Transform parent, ResourceKind kind, Vector3 at, float size, int seed)
        {
            switch (kind)
            {
                case ResourceKind.Gold:
                    V.Prop(parent, $"{Items}/Gold_Ingots", at + new Vector3(0.08f, 0f, 0.16f) * size, 20f + seed * 9f, 0.4f * size);
                    V.Prop(parent, $"{Props}/Coin_Pile_2", at + new Vector3(-0.22f, 0f, -0.16f) * size, 30f + seed * 31f, 0f, size * 3f);
                    V.Prop(parent, $"{Props}/Coin_Pile", at + new Vector3(0.3f, 0f, -0.24f) * size, 120f + seed * 17f, 0f, size * 2.6f);
                    break;
                case ResourceKind.Wood:
                {
                    float thick = 0.24f * size;
                    for (int row = 0; row < 3; row++)
                    {
                        for (int i = 0; i < 3 - row; i++)
                        {
                            float x = (i - (2 - row) * 0.5f) * thick * 1.02f;
                            Log(parent, at + new Vector3(x, row * thick * 0.84f, (row % 2) * 0.04f * size), 90f + seed * 7f % 12f, 1.15f * size, thick);
                        }
                    }
                    break;
                }
                case ResourceKind.Ore:
                {
                    var ore = new Color(0.58f, 0.48f, 0.44f);
                    var dark = new Color(0.42f, 0.4f, 0.44f);
                    (string stone, float x, float z, float y, float scale, Color tint)[] stones =
                    {
                        ("Pebble_Square_1", -0.22f, 0.1f, 0f, 2.2f, ore), ("Pebble_Square_3", 0.22f, 0.12f, 0f, 2.1f, dark),
                        ("Pebble_Square_5", 0f, -0.24f, 0f, 2f, ore), ("Pebble_Round_2", -0.34f, -0.26f, 0f, 1.6f, dark),
                        ("Pebble_Round_4", 0.38f, -0.18f, 0f, 1.5f, ore), ("Pebble_Square_2", 0.02f, 0.06f, 0.22f, 2f, dark),
                        ("Pebble_Square_6", -0.12f, -0.08f, 0.38f, 1.6f, ore)
                    };
                    foreach ((string stone, float x, float z, float y, float scale, Color tint) in stones)
                    {
                        V.Prop(parent, $"{Nature}/{stone}", at + new Vector3(x, y, z) * size, x * 300f + seed * 40f, 0f, scale * size, tint);
                    }
                    break;
                }
                case ResourceKind.Mercury:
                    V.Prop(parent, $"{Props}/Bucket_Metal", at + new Vector3(-0.1f, 0f, 0.12f) * size, 30f, 0.42f * size);
                    V.Slab(parent, PrimitiveType.Cylinder, at + new Vector3(-0.1f, 0.33f, 0.12f) * size, new Vector3(0.46f, 0.01f, 0.46f) * size, Quicksilver);
                    V.Prop(parent, $"{Props}/Potion_2", at + new Vector3(0.36f, 0f, -0.18f) * size, 0f, 0.46f * size);
                    V.Prop(parent, $"{Props}/Potion_4", at + new Vector3(-0.4f, 0f, -0.3f) * size, 40f, 0.36f * size);
                    V.Prop(parent, $"{Props}/Potion_1", at + new Vector3(0.08f, 0f, -0.42f) * size, 80f, 0.28f * size);
                    break;
                case ResourceKind.Sulfur:
                    Crystals(parent, at, size, new[] { Sulfur }, seed);
                    break;
                case ResourceKind.Crystal:
                    Crystals(parent, at, size, new[] { Crystal }, seed);
                    break;
                default:
                    Crystals(parent, at, size, Jewels, seed);
                    break;
            }
        }

        /// <summary>Crystals springing from a stone, about <paramref name="size"/> across, in the materials given in turn.</summary>
        private static void Crystals(Transform parent, Vector3 at, float size, Material[] gems, int seed)
        {
            V.Prop(parent, $"{Nature}/Pebble_Round_3", at + Vector3.down * 0.04f * size, seed * 50f, 0f, 2.3f * size, new Color(0.62f, 0.6f, 0.64f));
            (float toward, float lean, float reach, float height)[] spikes =
            {
                (0f, 4f, 0f, 0.95f), (40f, 30f, 0.2f, 0.62f), (150f, 34f, 0.22f, 0.55f), (250f, 26f, 0.18f, 0.72f), (320f, 38f, 0.26f, 0.42f), (95f, 42f, 0.3f, 0.36f)
            };
            for (int i = 0; i < spikes.Length; i++)
            {
                (float toward, float lean, float reach, float height) = spikes[i];
                float yaw = toward + seed * 23f;
                Vector3 from = at + Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * reach * size + Vector3.up * 0.06f * size;
                GameObject spike = Leaning(parent, $"{Items}/Crystal2", from, yaw, lean, height * size);
                V.Paint(spike, gems[i % gems.Length]);
            }
        }

        // ------------------------------------------------------------------ mines

        /// <summary>A mine: the pit head of the resource it gives, its adit dug into the rocks, with a heap of it by the door.</summary>
        private static void Mines(HeroesArt art)
        {
            ResourceKind[] kinds =
            {
                ResourceKind.Gold, ResourceKind.Wood, ResourceKind.Ore, ResourceKind.Mercury, ResourceKind.Sulfur, ResourceKind.Crystal, ResourceKind.Gems
            };
            foreach (ResourceKind kind in kinds)
            {
                GameObject prefab = kind == ResourceKind.Wood ? Sawmill() : Mine(kind);
                Add(art, ObjectKind.Mine, (int)kind, prefab);
            }
        }

        private static GameObject Mine(ResourceKind kind)
        {
            return Make($"Mine{kind}", "Objects", 0.42f, model =>
            {
                GameObject hill = V.Prop(model, $"{Nature}/Rock_Medium_1", new Vector3(-1.3f, -0.3f, 2.6f), 20f, 3.3f);
                V.Prop(model, $"{Nature}/Rock_Medium_3", new Vector3(2.1f, -0.3f, 3.0f), -40f, 2.9f);
                // The adit: a timber frame set into the face of the rock, dark inside.
                float face = V.LocalBounds(model, hill).min.z + 0.55f;
                V.Put(model, "DoorFrame_Round_WoodDark", new Vector3(-1.5f, 0f, face), 180f);
                V.Slab(model, PrimitiveType.Cube, new Vector3(-1.5f, 1.02f, face + 0.12f), new Vector3(1.1f, 2.04f, 0.08f), V.Darkness());
                Log(model, new Vector3(-1.5f, 0f, face - 1.0f), 0f, 1.6f, 0.16f, new Color(0.7f, 0.62f, 0.55f));
                var plan = new Plan
                {
                    Wide = 1, Deep = 2, Floors = new[] { Facade.Stone }, Roof = Roofing.Planks, Door = 0, Windows = Sides.Right, Shutters = Sides.None, Fancy = false,
                    Seed = (int)kind
                };
                V.House(model, "PitHead", plan, new Vector3(0.8f, 0f, 1.3f), 0f);
                Heap(model, kind, new Vector3(-0.9f, 0f, -1.6f), 2.5f, (int)kind);
                Leaning(model, $"{Props}/Pickaxe_Bronze", new Vector3(2.0f, 0f, 0f), 180f, 14f, 1.25f);
                Green(model, ("Grass_Common_Short", -3.2f, 1.8f, 0.9f), ("Grass_Common_Tall", -3.3f, -1.0f, 1f));
            });
        }

        /// <summary>The sawmill the wood comes from: an open shed of planks, a sawhorse, logs stacked before it, pines behind.</summary>
        private static GameObject Sawmill()
        {
            return Make("MineWood", "Objects", 0.42f, model =>
            {
                V.Prop(model, $"{Nature}/Pine_5", new Vector3(-2.0f, 0f, 3.2f), 30f, 5.8f);
                V.Prop(model, $"{Nature}/Pine_5", new Vector3(2.3f, 0f, 3.4f), 160f, 6.6f);
                var plan = new Plan { Wide = 2, Deep = 1, Floors = new[] { Facade.Open }, Roof = Roofing.Planks, RidgeAcross = true };
                V.House(model, "Shed", plan, new Vector3(0.3f, 0f, 0.9f), 0f);
                Stump(model, new Vector3(-0.5f, 0f, 0.9f), 0.8f, 0.36f);
                Stump(model, new Vector3(1.1f, 0f, 0.9f), 0.8f, 0.36f);
                Log(model, new Vector3(0.3f, 0.8f, 0.9f), 90f, 2.8f, 0.4f);
                Heap(model, ResourceKind.Wood, new Vector3(-1.0f, 0f, -1.7f), 2.8f, 1);
                Stump(model, new Vector3(-2.9f, 0f, -0.3f), 0.55f, 0.6f);
                Leaning(model, $"{Props}/Axe_Bronze", new Vector3(-2.85f, 0.5f, -0.3f), 200f, 18f, 0.8f);
                Green(model, ("Grass_Common_Short", -1.9f, 2.2f, 0.9f), ("Fern_1", 3.4f, 1.0f, 0.8f), ("Grass_Common_Short", 2.4f, -0.4f, 0.8f));
            });
        }

        // ------------------------------------------------------------------ the pickups

        /// <summary>A heap of a resource lying on the ground for the taking.</summary>
        private static void Piles(HeroesArt art)
        {
            ResourceKind[] kinds =
            {
                ResourceKind.Gold, ResourceKind.Wood, ResourceKind.Ore, ResourceKind.Mercury, ResourceKind.Sulfur, ResourceKind.Crystal, ResourceKind.Gems
            };
            foreach (ResourceKind kind in kinds)
            {
                GameObject prefab = Make($"Pile{kind}", "Objects", 0.45f, model =>
                {
                    Heap(model, kind, Vector3.zero, 2f, 3 + (int)kind);
                    if (kind == ResourceKind.Ore)
                    {
                        Leaning(model, $"{Props}/Pickaxe_Bronze", new Vector3(0.75f, 0f, 0.35f), 110f, 60f, 1.1f);
                    }
                    else if (kind == ResourceKind.Wood)
                    {
                        Leaning(model, $"{Props}/Axe_Bronze", new Vector3(0.9f, 0f, -0.35f), 60f, 70f, 0.9f);
                    }
                });
                Add(art, ObjectKind.Resource, (int)kind, prefab);
            }
        }

        private static void Treasures(HeroesArt art)
        {
            Add(art, ObjectKind.Treasure, -1, Make("Treasure", "Objects", 1f, model =>
            {
                // The chest opens toward +z as it comes: it is turned to open toward the map's camera, gold heaped inside.
                Transform turned = V.Holder(model, "Chest", new Vector3(0f, 0f, 0.05f), 180f, 1f);
                GameObject chest = V.Prop(turned, $"{Items}/Chest_Open", Vector3.zero, 0f, 0.62f);
                Bounds box = V.LocalBounds(turned, chest);
                V.Prop(turned, $"{Items}/Gold_Ingots", new Vector3(0f, box.min.y + 0.1f, box.min.z + box.size.z * 0.3f), 0f, 0.26f);
                V.Prop(model, $"{Props}/Chalice", new Vector3(0.52f, 0f, -0.2f), 0f, 0.3f);
            }));
            Add(art, ObjectKind.Campfire, -1, Make("Campfire", "Objects", 1f, model =>
            {
                Fire(model, 7);
                V.Prop(model, $"{Props}/Bag", new Vector3(0.72f, 0f, 0.3f), -30f, 0.5f);
            }));
            Add(art, ObjectKind.Scroll, -1, Make("Scroll", "Objects", 1f, model =>
            {
                V.Prop(model, $"{Village}/Stairs_Exterior_Platform", Vector3.zero, 15f, 0f, 0.26f);
                V.Prop(model, $"{Props}/Scroll_2", new Vector3(0f, 0.26f, 0f), 35f, 0f, 3.6f);
                V.Prop(model, $"{Props}/Candle_2", new Vector3(-0.17f, 0.26f, 0.15f), 0f, 0.18f);
                V.Prop(model, $"{Props}/Candle_1", new Vector3(0.2f, 0.26f, 0.14f), 0f, 0.11f);
                V.Prop(model, $"{Props}/Book_Stack_1", new Vector3(0.45f, 0f, -0.2f), 20f, 0f, 1.3f);
            }));
        }

        /// <summary>Burning logs in a ring of <paramref name="stones"/> stones, about a metre across.</summary>
        private static void Fire(Transform model, int stones)
        {
            for (int i = 0; i < stones; i++)
            {
                float angle = i * 360f / stones;
                Vector3 at = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 0.42f;
                V.Prop(model, $"{Nature}/Pebble_Round_{1 + i % 5}", at, angle * 1.7f, 0f, 0.62f, new Color(0.7f, 0.68f, 0.66f));
            }
            var burnt = new Color(0.45f, 0.36f, 0.3f);
            Log(model, new Vector3(0f, 0.02f, 0f), 20f, 0.7f, 0.1f, burnt);
            Log(model, new Vector3(0f, 0.02f, 0f), 140f, 0.68f, 0.1f, burnt);
            Log(model, new Vector3(0f, 0.02f, 0f), 260f, 0.66f, 0.1f, burnt);
            (float toward, float lean, float reach, float height, bool core)[] tongues =
            {
                (0f, 3f, 0f, 0.55f, false), (70f, 16f, 0.08f, 0.38f, false), (200f, 14f, 0.09f, 0.42f, false), (120f, 6f, 0.03f, 0.3f, true), (330f, 8f, 0.04f, 0.26f, true)
            };
            foreach ((float toward, float lean, float reach, float height, bool core) in tongues)
            {
                Vector3 at = Quaternion.Euler(0f, toward, 0f) * Vector3.forward * reach + Vector3.up * 0.05f;
                V.Paint(Leaning(model, $"{Items}/Crystal2", at, toward, lean, height), core ? Embers : Flame);
            }
        }

        // ------------------------------------------------------------------ the places a hero visits

        private static void Buildings(HeroesArt art)
        {
            Add(art, ObjectKind.Shrine, -1, Make("Shrine", "Objects", 0.36f, Shrine));
            Add(art, ObjectKind.MercenaryCamp, -1, Make("MercenaryCamp", "Objects", 0.4f, MercenaryCamp));
            Add(art, ObjectKind.MarlettoTower, -1, Make("MarlettoTower", "Objects", 0.28f, MarlettoTower));
            Add(art, ObjectKind.StarAxis, -1, Make("StarAxis", "Objects", 0.36f, StarAxis));
            Add(art, ObjectKind.GardenOfRevelation, -1, Make("GardenOfRevelation", "Objects", 0.4f, Garden));
            Add(art, ObjectKind.Arena, -1, Make("Arena", "Objects", 0.4f, Arena));
            Add(art, ObjectKind.LearningStone, -1, Make("LearningStone", "Objects", 0.45f, LearningStone));
            Add(art, ObjectKind.WitchHut, -1, Make("WitchHut", "Objects", 0.42f, WitchHut));
            Add(art, ObjectKind.Windmill, -1, Make("Windmill", "Objects", 0.33f, Windmill));
            Add(art, ObjectKind.WaterWheel, -1, Make("WaterWheel", "Objects", 0.36f, WaterWheel));
            Add(art, ObjectKind.Watchtower, -1, Make("Watchtower", "Objects", 0.3f, Watchtower));
            Add(art, ObjectKind.Stables, -1, Make("Stables", "Objects", 0.36f, Stables));
            Add(art, ObjectKind.Fountain, -1, Make("Fountain", "Objects", 0.42f, Fountain));
            Add(art, ObjectKind.Temple, -1, Make("Temple", "Objects", 0.3f, Temple));
            Add(art, ObjectKind.TreeOfKnowledge, -1, Make("TreeOfKnowledge", "Objects", 0.45f, TreeOfKnowledge));
        }

        /// <summary>An open shrine of four stone pillars under a blue tiled spire, a glowing crystal on its altar.</summary>
        private static void Shrine(Transform model)
        {
            V.Prop(model, $"{Village}/Stairs_Exterior_Platform", Vector3.zero, Quaternion.identity, new Vector3(1.7f, 0.45f, 1.7f));
            Transform shrine = V.Holder(model, "Shrine", new Vector3(0f, 0.45f, 0f), 0f, 1f);
            foreach (Vector3 corner in new[] { new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(1f, 0f, 1f), new Vector3(-1f, 0f, 1f) })
            {
                V.Prop(shrine, $"{Village}/Prop_Chimney2", corner * 1.12f, Quaternion.Euler(0f, Mathf.Atan2(corner.x, corner.z) * Mathf.Rad2Deg, 0f), new Vector3(0.55f, 1.05f, 0.55f));
            }
            GameObject spire = V.Put(shrine, "Roof_Tower_RoundTiles", new Vector3(0f, 3.12f, 0f), 0f);
            spire.transform.localScale *= 0.62f;
            V.Swap(spire, "MI_RoundTiles", V.Tiles(new Color(0.2f, 0.36f, 0.9f), "Shrine"));
            V.Prop(shrine, $"{Village}/Stairs_Exterior_Platform", Vector3.zero, 0f, 0f, 0.5f);
            V.Paint(V.Prop(shrine, $"{Items}/Crystal1", new Vector3(0f, 0.5f, 0f), 20f, 0.9f), V.Gem(new Color(0.35f, 0.6f, 1f), 1.4f, "Shrine"));
            Candles(shrine, new Vector3(-0.55f, 0.5f, -0.5f), 0f);
            Green(model, ("Flower_3_Group", 2.3f, 1.5f, 1.1f), ("Grass_Common_Short", -2.2f, -1.6f, 0.8f), ("Grass_Common_Short", -2.3f, 1.4f, 0.9f));
        }

        /// <summary>A camp of sellswords: canvas tents, a straw man and a rack of arms in a yard fenced off, their colours flying.</summary>
        private static void MercenaryCamp(Transform model)
        {
            Tent(model, new Vector3(-1.3f, 0f, 1.8f), 10f, new Color(0.86f, 0.8f, 0.66f));
            Tent(model, new Vector3(1.5f, 0f, 2.3f), -14f, new Color(0.66f, 0.24f, 0.18f));
            V.Prop(model, $"{Props}/Dummy", new Vector3(-1.6f, 0f, -1.1f), 160f, 1.9f);
            V.Prop(model, $"{Props}/WeaponStand", new Vector3(0.4f, 0f, -0.6f), -10f, 1.15f);
            V.Line(model, $"{Village}/Prop_WoodenFence_Single", new Vector3(-3.2f, 0f, -2.4f), new Vector3(-3.2f, 0f, 0.2f), 2.06f);
            V.Line(model, $"{Village}/Prop_WoodenFence_Extension1", new Vector3(-3.2f, 0f, -2.4f), new Vector3(-0.8f, 0f, -2.4f), 2.06f);
            V.Standard(model, new Vector3(-0.1f, 0f, 3.5f), 0f, V.Cloth(new Color(0.66f, 0.24f, 0.18f)), 3.6f);
            Green(model, ("Grass_Common_Short", 2.9f, 0.4f, 0.9f), ("Grass_Common_Short", -2.6f, -1.6f, 0.7f));
        }

        /// <summary>A tent: two slopes of a plank roof set on the ground, dressed in canvas, its ends closed, its ridge a pole.</summary>
        private static void Tent(Transform parent, Vector3 at, float yaw, Color canvas)
        {
            var plan = new Plan { Wide = 1, Deep = 2, Floors = new Facade[0], Roof = Roofing.Planks };
            Transform tent = V.House(parent, "Tent", plan, at, yaw);
            tent.localScale = new Vector3(1f, 1.25f, 1f);
            Material cloth = V.Canvas(canvas, ColorUtility.ToHtmlStringRGB(canvas));
            V.Swap(tent.gameObject, "MI_WoodTrim", cloth);
            V.Swap(tent.gameObject, "MI_WoodTrim_Wear", cloth);
            V.Swap(tent.gameObject, "MI_Plaster", cloth);
            Log(parent, at + new Vector3(0f, 1.25f * 1.06f - 0.05f, 0f), yaw, 4.4f, 0.12f);
        }

        /// <summary>A slender tower of stone with a blue spire, a little hall of stone at its foot.</summary>
        private static void MarlettoTower(Transform model)
        {
            Material arcane = V.Tiles(new Color(0.24f, 0.3f, 0.86f), "Marletto");
            var tower = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Stone, Facade.Stone, Facade.Stone }, Roof = Roofing.Spire, SpireScale = 1.25f, Tiles = arcane, Door = 0,
                Windows = Sides.Front, Shutters = Sides.None, Seed = 21
            };
            V.House(model, "Tower", tower, new Vector3(0.8f, 0f, 0.6f), 0f);
            var hall = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Stone }, Roof = Roofing.Tiles, RidgeAcross = true, Tiles = arcane, Door = -1, Windows = Sides.Front,
                Shutters = Sides.None, Fancy = false, Quoins = false, Seed = 22
            };
            V.House(model, "Hall", hall, new Vector3(-1.2f, 0f, 1.0f), 0f);
            Green(model, ("Grass_Common_Short", -2.4f, -0.6f, 0.9f));
        }

        /// <summary>A stepped plinth with an obelisk of stone blocks, a golden star alight on its top, candles about it.</summary>
        private static void StarAxis(Transform model)
        {
            V.Prop(model, $"{Village}/Stairs_Exterior_Platform", Vector3.zero, 0f, 0f, 1.7f);
            V.Prop(model, $"{Village}/Stairs_Exterior_Platform", new Vector3(0f, 1.7f, 0f), 45f, 0f, 1.05f);
            V.Prop(model, $"{Village}/Corner_ExteriorWide_Brick", new Vector3(0f, 2.75f, 0f), Quaternion.identity, new Vector3(1.5f, 1.85f, 1.5f));
            V.Paint(V.Prop(model, $"{Items}/Star", new Vector3(0f, 8.4f, 0f), 0f, 1.8f), V.Gem(new Color(1f, 0.8f, 0.3f), 1.6f, "Star"));
            foreach (float yaw in new[] { 45f, 135f, 225f, 315f })
            {
                Candles(model, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * 2.15f, 1.6f);
            }
            Green(model, ("Grass_Common_Short", -2.6f, 1.2f, 0.8f), ("Flower_3_Single", 2.5f, 1.8f, 1f));
        }

        /// <summary>A garden in bloom round a great tree: a pool with lilies at its feet, stepping stones to it, a bench to sit on.</summary>
        private static void Garden(Transform model)
        {
            V.Prop(model, $"{Nature}/CommonTree_5", new Vector3(0.9f, -0.1f, 1.6f), 40f, 6.4f);
            var pool = new Vector3(-0.9f, 0f, -0.6f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                V.Prop(model, $"{Nature}/Pebble_Round_{1 + i % 5}", pool + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.25f, angle, 0f, 1.35f);
            }
            V.Slab(model, PrimitiveType.Cylinder, pool + Vector3.up * 0.08f, new Vector3(2.4f, 0.01f, 2.4f), V.Water());
            V.Prop(model, $"{Nature}/Plant_7", pool + new Vector3(-0.3f, 0.09f, 0.2f), 30f, 0.12f);
            V.Prop(model, $"{Nature}/Plant_7_Big", pool + new Vector3(0.45f, 0.09f, -0.25f), 100f, 0.12f);
            for (int i = 0; i < 3; i++)
            {
                V.Prop(model, $"{Nature}/Pebble_Square_{2 + i}", new Vector3(0.8f + i * 0.4f, -0.04f, -1.9f - i * 0.75f), i * 40f, 0f, 1.8f);
            }
            Green(model, ("Flower_3_Group", 1.9f, -0.4f, 1.2f), ("Bush_Common_Flowers", -2.4f, 2.4f, 1.5f), ("Flower_3_Single", -2.6f, -1.6f, 1f),
                ("Flower_3_Single", 2.4f, -1.8f, 1f), ("Grass_Common_Short", 2.9f, 1.6f, 0.9f));
            V.Prop(model, $"{Props}/Bench", new Vector3(-0.8f, 0f, 1.5f), 180f, 0.55f);
        }

        /// <summary>A ring of fences round a floor of sand where heroes train, a straw man in it, the banners of the games beside it.</summary>
        private static void Arena(Transform model)
        {
            const float radius = 2.8f;
            for (int i = 0; i < 8; i++)
            {
                if (i == 6)
                {
                    // The gap the fighters come in by, toward the viewer.
                    continue;
                }
                Vector3 from = Quaternion.Euler(0f, i * 45f + 22.5f, 0f) * Vector3.forward * radius;
                Vector3 to = Quaternion.Euler(0f, (i + 1) * 45f + 22.5f, 0f) * Vector3.forward * radius;
                V.Line(model, $"{Village}/Prop_WoodenFence_Single", from, to, 2.06f);
            }
            V.Slab(model, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(5.4f, 0.02f, 5.4f), Sand);
            V.Prop(model, $"{Props}/Dummy", new Vector3(-0.6f, 0f, 0.3f), 160f, 1.9f);
            V.Prop(model, $"{Props}/WeaponStand", new Vector3(0.2f, 0f, 3.6f), 0f, 1.15f);
            V.Prop(model, $"{Props}/Bench", new Vector3(1.0f, 0f, -0.6f), 30f, 0.55f);
            V.Standard(model, new Vector3(-2.6f, 0f, 2.6f), 0f, V.Cloth(new Color(0.85f, 0.62f, 0.18f)), 3.6f);
            V.Standard(model, new Vector3(2.7f, 0f, 2.5f), 0f, V.Cloth(new Color(0.7f, 0.15f, 0.12f)), 3.6f);
            Green(model, ("Grass_Common_Short", -3.4f, -0.4f, 0.8f), ("Grass_Common_Short", 3.4f, 0.6f, 0.8f));
        }

        /// <summary>A great standing stone, a lectern with its open book before it and candles burning.</summary>
        private static void LearningStone(Transform model)
        {
            V.Prop(model, $"{Nature}/Rock_Medium_3", new Vector3(0f, -0.2f, 0.6f), Quaternion.Euler(0f, 30f, 0f), new Vector3(0.8f, 1.9f, 0.8f), 4.2f, new Color(0.9f, 0.88f, 0.92f));
            V.Prop(model, $"{Nature}/Rock_Medium_2", new Vector3(-1.5f, -0.2f, 1.4f), 80f, 1.5f);
            V.Prop(model, $"{Props}/BookStand", new Vector3(0.9f, 0f, -1.2f), 200f, 1.45f);
            V.Prop(model, $"{Props}/Book_Stack_2", new Vector3(-0.6f, 0f, -1.3f), 30f, 0f, 2f);
            Candles(model, new Vector3(-1.5f, 0f, -0.5f), 1.3f);
            V.Prop(model, $"{Props}/Scroll_1", new Vector3(0.1f, 0f, -1.7f), 70f, 0f, 2.4f);
            Green(model, ("Grass_Common_Tall", 1.6f, 1.2f, 1.2f), ("Fern_1", -2.2f, 0.4f, 0.8f), ("Flower_3_Group", 1.9f, -0.2f, 0.9f));
        }

        /// <summary>A crooked hut on stilts, vines down its walls, a cauldron bubbling green below it, a skull on a post.</summary>
        private static void WitchHut(Transform model)
        {
            var dark = new Color(0.78f, 0.74f, 0.8f);
            var stilts = new Plan { Wide = 1, Deep = 1, Floors = new[] { Facade.Open }, Roof = Roofing.None, Railings = true, Tint = dark };
            Transform raised = V.House(model, "Stilts", stilts, new Vector3(0.4f, 0f, 1.1f), 8f);
            raised.localScale = new Vector3(1f, 0.5f, 1f);
            var hut = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Timbered }, Roof = Roofing.Planks, Door = 0, Windows = Sides.Left | Sides.Right, Shutters = Sides.None,
                Fancy = false, Vines = true, Seed = 31, Tint = dark
            };
            V.House(model, "Hut", hut, new Vector3(0.4f, 1.5f, 1.1f), 8f);
            V.Prop(model, $"{Village}/Stairs_Exterior_Straight", new Vector3(0.25f, 0f, -0.55f), Quaternion.Euler(0f, 8f, 0f), new Vector3(0.5f, 1.5f, 0.75f));
            V.Prop(model, $"{Props}/Cauldron", new Vector3(-1.5f, 0f, -0.9f), 0f, 0.75f);
            V.Slab(model, PrimitiveType.Cylinder, new Vector3(-1.5f, 0.64f, -0.9f), new Vector3(0.8f, 0.01f, 0.8f), V.Gem(new Color(0.3f, 1f, 0.35f), 0.7f, "Brew")).name = "Brew";
            V.Prop(model, $"{Halloween}/post_skull", new Vector3(2.0f, 0f, -0.2f), 200f, 1.5f);
            Green(model, ("Mushroom_Common", 1.6f, -1.2f, 0.5f), ("Bush_Common", -2.2f, 2.2f, 1.6f), ("Grass_Common_Tall", 2.6f, -0.2f, 1.1f));
        }

        /// <summary>A plastered mill tower under a tiled spire, its four lattice sails turned to the wind.</summary>
        private static void Windmill(Transform model)
        {
            var plan = new Plan { Wide = 1, Deep = 1, Floors = new[] { Facade.Footed, Facade.Plaster }, Roof = Roofing.Spire, SpireScale = 1.2f, Door = 0, Fancy = false, Seed = 41 };
            V.House(model, "Mill", plan, Vector3.zero, 0f);
            var hub = new Vector3(0f, 6.4f, -1.45f);
            Log(model, hub + new Vector3(0f, -0.2f, 0f), 0f, 1.2f, 0.4f);
            for (int i = 0; i < 4; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, 0f, 20f + i * 90f);
                GameObject sail = V.Prop(model, $"{Village}/Prop_WoodenFence_Single", Vector3.zero, turn, new Vector3(1.75f, 1.7f, 1.4f));
                // A fence stands on the ground; the sail hangs off the hub along its arm, its lattice to the wind.
                Place(sail, model, hub + turn * Vector3.right * 2.05f);
            }
            V.Prop(model, $"{Props}/Bag", new Vector3(-1.5f, 0f, -0.9f), 20f, 0.8f);
            Green(model, ("Grass_Common_Short", -1.8f, 1.6f, 0.9f), ("Grass_Common_Short", 1.9f, 1.8f, 0.8f));
        }

        /// <summary>A mill house over its race, the water wheel turning in the stream before it.</summary>
        private static void WaterWheel(Transform model)
        {
            var plan = new Plan { Wide = 2, Deep = 2, Floors = new[] { Facade.Footed }, Roof = Roofing.Tiles, Door = 0, Seed = 51 };
            V.House(model, "Mill", plan, new Vector3(-0.6f, 0f, 1.4f), 0f);
            const float race = -1.55f;
            V.Slab(model, PrimitiveType.Cube, new Vector3(1.7f, 0.05f, race), new Vector3(4.8f, 0.02f, 1.5f), V.Water());
            foreach (int side in new[] { -1, 1 })
            {
                V.Line(model, $"{Village}/Prop_ExteriorBorder_Straight1", new Vector3(-0.7f, 0f, race + side * 0.85f), new Vector3(4.1f, 0f, race + side * 0.85f), 2f,
                    new Color(0.88f, 0.88f, 0.88f));
            }
            var hub = new Vector3(1.9f, 1.6f, race);
            Log(model, hub + new Vector3(0f, -0.2f, -0.6f), 0f, 2.8f, 0.36f);
            const float radius = 1.85f;
            for (int i = 0; i < 16; i++)
            {
                Quaternion spoke = Quaternion.Euler(0f, 0f, i * 22.5f);
                Vector3 outward = spoke * Vector3.up;
                // The paddles stand out from the rim; the rims are planks set end to end round the wheel; the spokes meet at the hub.
                Plank(model, hub + outward * (radius - 0.12f), spoke * Quaternion.Euler(0f, 0f, 90f), new Vector3(0.24f, 1.6f, 0.36f));
                Quaternion half = Quaternion.Euler(0f, 0f, i * 22.5f + 11.25f);
                foreach (float z in new[] { -0.34f, 0.34f })
                {
                    Plank(model, hub + half * Vector3.up * (radius - 0.3f) + Vector3.forward * z, half * Quaternion.Euler(-90f, 0f, 0f), new Vector3(0.38f, 1f, 0.09f));
                }
                if (i % 2 == 0)
                {
                    Plank(model, hub + outward * (radius - 0.3f) * 0.5f, spoke * Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(90f, 0f, 0f), new Vector3((radius - 0.3f) / 2f, 1f, 0.06f));
                }
            }
            V.Prop(model, $"{Props}/Bag", new Vector3(-2.8f, 0f, -1.0f), 20f, 0.8f);
            Green(model, ("Grass_Common_Short", 3.8f, -0.4f, 1f), ("Grass_Common_Short", 3.0f, 3.0f, 1.1f));
        }

        /// <summary>A lookout of stone with a railed timber top under a spire, firewood stacked at its foot.</summary>
        private static void Watchtower(Transform model)
        {
            var plan = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Stone, Facade.Stone, Facade.Open }, Roof = Roofing.Spire, SpireScale = 1.3f, Railings = true,
                Door = 0, Shutters = Sides.None, Fancy = false, Seed = 61
            };
            V.House(model, "Tower", plan, new Vector3(0.2f, 0f, 0.6f), 0f);
            Log(model, new Vector3(-1.8f, 0f, -0.6f), 0f, 1.6f, 0.34f);
            Log(model, new Vector3(-1.45f, 0f, -0.6f), 0f, 1.6f, 0.34f);
            Log(model, new Vector3(-1.62f, 0.3f, -0.6f), 0f, 1.6f, 0.34f);
            Green(model, ("Grass_Common_Short", 1.8f, -0.4f, 0.8f), ("Grass_Common_Short", -1.2f, 2.2f, 0.8f));
        }

        /// <summary>A long stable under planks, a paddock fenced before it with a horse, a barrel of apples.</summary>
        private static void Stables(Transform model)
        {
            var plan = new Plan
            {
                Wide = 3, Deep = 1, Floors = new[] { Facade.Footed }, Roof = Roofing.Planks, RidgeAcross = true, Door = 1, Windows = Sides.Front, Fancy = false, Seed = 71
            };
            V.House(model, "Stable", plan, new Vector3(0f, 0f, 1.8f), 0f);
            V.Line(model, $"{Village}/Prop_WoodenFence_Single", new Vector3(-3.2f, 0f, -2.4f), new Vector3(0.6f, 0f, -2.4f), 2.06f);
            V.Line(model, $"{Village}/Prop_WoodenFence_Single", new Vector3(-3.2f, 0f, -2.4f), new Vector3(-3.2f, 0f, 0.6f), 2.06f);
            V.Prop(model, "Quaternius/Animals/Horse", new Vector3(-1.5f, 0f, -0.9f), -70f, 2.4f);
            V.Prop(model, $"{Props}/Barrel", new Vector3(2.5f, 0f, 0.4f), 0f, 0.9f);
            Green(model, ("Grass_Common_Tall", -2.6f, 0.3f, 1f), ("Grass_Common_Short", 0.1f, -0.2f, 0.7f), ("Grass_Common_Short", 3.1f, -1.3f, 0.8f));
        }

        /// <summary>A square stone basin brimming with water, a column with an urn in its middle, flowers and a bench round it.</summary>
        private static void Fountain(Transform model)
        {
            foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
            {
                Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
                V.Prop(model, $"{Village}/Stairs_Exterior_SingleSideThick", turn * new Vector3(1.4f, 0f, 0f), turn, new Vector3(1f, 0.6f, 1.6f));
            }
            V.Slab(model, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(2.7f, 0.02f, 2.7f), V.Water());
            V.Prop(model, $"{Village}/Prop_Chimney2", Vector3.zero, Quaternion.Euler(0f, 45f, 0f), new Vector3(0.6f, 0.55f, 0.6f));
            V.Prop(model, $"{Props}/Vase_2", new Vector3(0f, 1.65f, 0f), 0f, 0.8f);
            Green(model, ("Bush_Common_Flowers", -2.6f, 1.6f, 1.3f), ("Flower_3_Group", 2.5f, 1.8f, 1.1f), ("Flower_4_Single", -2.4f, -1.6f, 1f),
                ("Grass_Common_Short", 2.4f, -0.4f, 0.8f));
            V.Prop(model, $"{Props}/Bench", new Vector3(0f, 0f, 2.6f), 0f, 0.55f);
        }

        /// <summary>A long chapel of stone under red tiles, its gable to the road, a bell tower at its side.</summary>
        private static void Temple(Transform model)
        {
            var chapel = new Plan
            {
                Wide = 2, Deep = 3, Floors = new[] { Facade.Stone }, Roof = Roofing.Tiles, Door = 0, Windows = Sides.Left | Sides.Right, Shutters = Sides.None,
                Fancy = false, Quoins = false, Seed = 81
            };
            V.House(model, "Chapel", chapel, new Vector3(-0.8f, 0f, 0.8f), 0f);
            var tower = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Stone, Facade.Stone, Facade.Stone }, Roof = Roofing.Tiles, Door = -1, Windows = Sides.Front | Sides.Back,
                Sill = 2, Shutters = Sides.None, Quoins = false, Seed = 82
            };
            V.House(model, "Bells", tower, new Vector3(2.2f, 0f, -0.2f), 0f);
            Green(model, ("Grass_Common_Short", -3.6f, -1.6f, 1f), ("Grass_Common_Short", 3.4f, 2.6f, 0.9f));
        }

        /// <summary>A great tree in golden leaf, books and a lectern among its roots.</summary>
        private static void TreeOfKnowledge(Transform model)
        {
            GameObject tree = V.Prop(model, $"{Nature}/CommonTree_3", new Vector3(0f, -0.1f, 0.6f), 30f, 8f);
            Material gold = HeroesKitArt.Repainted("Nature", "Leaves_NormalTree", -1, "Golden", HeroesKitArt.Hue(0.12f, 0.5f, 0.15f, new Color(1f, 0.74f, 0.16f), 1.15f));
            V.Swap(tree, "Leaves_NormalTree", gold);
            V.Prop(model, $"{Props}/BookStand", new Vector3(1.2f, 0f, -1.2f), 200f, 1.4f);
            V.Prop(model, $"{Props}/Book_Stack_1", new Vector3(-0.9f, 0f, -1.1f), 40f, 0f, 2.2f);
            V.Prop(model, $"{Props}/Book_Stack_2", new Vector3(-1.4f, 0f, -0.4f), 110f, 0f, 1.8f);
            Candles(model, new Vector3(-1.9f, 0f, 0.9f), 1.3f);
            Green(model, ("Flower_3_Single", 2f, 0.6f, 1f), ("Fern_1", 1.8f, 2.2f, 0.8f));
        }

        // ------------------------------------------------------------------ dwellings

        /// <summary>Dwellings of the wilds, one look for each faction's creatures.</summary>
        private static void Dwellings(HeroesArt art)
        {
            GameObject castle = Make("DwellingCastle", "Objects", 0.38f, model =>
            {
                var plan = new Plan { Wide = 2, Deep = 2, Floors = new[] { Facade.Footed, Facade.Timbered }, Door = 0, Windows = Sides.Front, Seed = 91 };
                V.House(model, "Barracks", plan, new Vector3(-0.6f, 0f, 1f), 0f);
                V.Standard(model, new Vector3(1.0f, 0f, -1.6f), 0f, V.Cloth(HeroesRealmArt.CastleBlue), 3.6f);
                Green(model, ("Grass_Common_Short", 2.6f, -0.2f, 0.8f), ("Grass_Common_Short", -3.0f, -1.4f, 0.8f));
            });
            GameObject necropolis = Make("DwellingNecropolis", "Objects", 0.38f, model =>
            {
                var cold = new Color(0.78f, 0.76f, 0.84f);
                var plan = new Plan
                {
                    Wide = 1, Deep = 2, Floors = new[] { Facade.Stone }, Roof = Roofing.Tiles, Tiles = NecroSlate, Door = 0, Windows = Sides.None, Seed = 92, Tint = cold
                };
                V.House(model, "Crypt", plan, new Vector3(0f, 0f, 1.2f), 0f);
                V.Line(model, $"{Halloween}/fence", new Vector3(-2.8f, 0f, -1.5f), new Vector3(-0.6f, 0f, -1.5f), 4f, cold);
                V.Line(model, $"{Halloween}/fence", new Vector3(-2.8f, 0f, -1.5f), new Vector3(-2.8f, 0f, 2.3f), 4f, cold);
                Grave(model, "gravestone", new Vector3(-1.7f, 0f, 0.2f), 10f);
                Grave(model, "grave_A", new Vector3(-1.8f, 0f, 1.8f), -10f);
                Grave(model, "gravemarker_A", new Vector3(1.9f, 0f, -0.3f), 15f);
                V.Prop(model, $"{Halloween}/tree_dead_large", new Vector3(2.3f, 0f, 2.2f), 200f, 4.6f, 1f, new Color(0.62f, 0.58f, 0.62f));
                Candles(model, new Vector3(1.1f, 0f, -1.1f), 1.3f);
                V.Prop(model, $"{Halloween}/bone_A", new Vector3(0.2f, 0f, -1.9f), 40f, 0.2f);
            });
            GameObject stronghold = Make("DwellingStronghold", "Objects", 0.38f, model =>
            {
                var plan = new Plan
                {
                    Wide = 2, Deep = 1, Floors = new[] { Facade.Timbered }, Roof = Roofing.Planks, RidgeAcross = true, Door = 0, Windows = Sides.Front, Fancy = false, Seed = 93
                };
                V.House(model, "Hall", plan, new Vector3(-0.4f, 0f, 1.6f), 0f);
                Log(model, new Vector3(-0.4f, 3.95f, 1.6f), 90f, 4.8f, 0.42f);
                GameObject stall = V.Prop(model, $"{Props}/Stall_Empty", new Vector3(-2.0f, 0f, -1.4f), 20f, 2.4f);
                V.Swap(stall, "MI_Banner", V.Cloth(HeroesRealmArt.StrongholdRed));
                V.Prop(model, $"{Props}/WeaponStand", new Vector3(2.4f, 0f, 0.6f), -80f, 1.15f);
                V.Prop(model, $"{Halloween}/post_skull", new Vector3(0.6f, 0f, -1.6f), 200f, 1.6f);
                V.Line(model, $"{Village}/Prop_WoodenFence_Single", new Vector3(-3.4f, 0f, 3.0f), new Vector3(2.8f, 0f, 3.0f), 2.06f);
                Green(model, ("Grass_Common_Short", -0.6f, -2.2f, 0.7f));
            });
            GameObject wilds = Make("DwellingWilds", "Objects", 0.36f, model =>
            {
                // A den: a cave mouth, dark inside, between two boulders with a third across them.
                V.Prop(model, $"{Nature}/Rock_Medium_1", new Vector3(-2.7f, -0.3f, 1.2f), 70f, 2.4f);
                V.Prop(model, $"{Nature}/Rock_Medium_3", new Vector3(2.7f, -0.3f, 1.3f), 100f, 2.3f);
                GameObject back = V.Prop(model, $"{Nature}/Rock_Medium_2", new Vector3(0f, -0.2f, 3.9f), 0f, 2.6f);
                float mouth = V.LocalBounds(model, back).min.z + 0.25f;
                V.Slab(model, PrimitiveType.Cube, new Vector3(0f, 0.85f, mouth), new Vector3(2.0f, 1.7f, 0.1f), V.Darkness());
                V.Prop(model, $"{Nature}/Rock_Medium_2", new Vector3(0f, 1.5f, mouth - 0.1f), 90f, 1.1f);
                V.Prop(model, $"{Nature}/CommonTree_5", new Vector3(-2.6f, -0.1f, 2.4f), 70f, 5.6f);
                V.Prop(model, $"{Halloween}/bone_A", new Vector3(0.4f, 0f, -0.9f), 30f, 0.22f);
                V.Prop(model, $"{Halloween}/skull", new Vector3(-0.3f, 0f, -1.2f), 160f, 0.35f);
                Green(model, ("Mushroom_Common", -1.9f, -0.6f, 0.5f), ("Fern_1", 2.6f, 1f, 0.9f), ("Grass_Common_Tall", -2.8f, 0.2f, 1.1f));
            });

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

        /// <summary>A gravestone of the packs, in cold grey stone.</summary>
        private static void Grave(Transform parent, string model, Vector3 at, float yaw)
        {
            V.Prop(parent, $"{Halloween}/{model}", at, yaw, model.StartsWith("grave_", StringComparison.Ordinal) ? 0.9f : 1.4f, 1f, new Color(0.72f, 0.72f, 0.8f));
        }

        // ------------------------------------------------------------------ artifacts

        /// <summary>Every artifact lies on the map as the thing it is, on a little plinth of stone.</summary>
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
                    prefab = Make($"Artifact{def.Item}", "Artifacts", 1f, model =>
                    {
                        V.Prop(model, $"{Village}/Stairs_Exterior_Platform", Vector3.zero, 45f, 0f, 0.27f);
                        GameObject item = V.Prop(model, $"{Items}/{def.Item}", new Vector3(0f, 0.27f, 0f), 0f, 0.72f);
                        V.Fit(model, item, 0.66f);
                    });
                    made[def.Item] = prefab;
                }
                Add(art, ObjectKind.Artifact, i, prefab);
            }
            if (made.Count > 0)
            {
                Add(art, ObjectKind.Artifact, -1, made.TryGetValue("Chalice", out GameObject chalice) ? chalice : made.Values.First());
            }
        }

        // ------------------------------------------------------------------ towns

        /// <summary>How small a town's buildings are drawn: its tallest tower stands about 4.2 m.</summary>
        private static float TownScale(Faction faction)
        {
            return faction == Faction.Castle ? 0.25f : faction == Faction.Necropolis ? 0.26f : 0.28f;
        }

        private static void Towns(HeroesArt art)
        {
            foreach (Faction faction in new[] { Faction.Castle, Faction.Necropolis, Faction.Stronghold })
            {
                var byColor = new GameObject[5];
                for (int color = 0; color < 5; color++)
                {
                    int owner = color;
                    byColor[color] = Make($"{faction}Town{color}", "Towns", TownScale(faction), model =>
                    {
                        switch (faction)
                        {
                            case Faction.Necropolis:
                                Necropolis(model, owner);
                                break;
                            case Faction.Stronghold:
                                Stronghold(model, owner);
                                break;
                            default:
                                Castle(model, owner);
                                break;
                        }
                    });
                }
                art.towns.Add(new HeroesArt.TownArt { faction = faction, byColor = byColor });
            }
        }

        /// <summary>The grey a town nobody holds has gone (clear for a held one): its walls dulled, its banners down.</summary>
        private static Color Faded(int color)
        {
            return color == 4 ? new Color(0.8f, 0.8f, 0.84f) : default;
        }

        /// <summary>The cloth of the owner's banners: a player's colour, or the washed-out white a town nobody holds still flies.</summary>
        private static Material OwnerCloth(int color)
        {
            return V.Cloth(color < 4 ? Player(color) : new Color(0.86f, 0.85f, 0.8f));
        }

        /// <summary>A pennant on a pole set into the tip of a spire whose base is at <paramref name="at"/> and that is <paramref name="spire"/> high.</summary>
        private static void Pennant(Transform parent, Vector3 at, float spire, Material cloth, Color tint, float pole = 2.6f)
        {
            V.Standard(parent, at + Vector3.up * spire * 0.86f, 0f, cloth, pole, tint, 0.8f);
        }

        /// <summary>
        /// The Castle: a keep of stone under a slate spire flying the owner's pennant, plaster and timber houses about it
        /// under their red tiles with the owner's banners on their walls, all behind a crenellated curtain wall whose gate
        /// stands between two towers flying the owner's standards; inside the gate a little square with a stall under an
        /// awning in the owner's colour.
        /// </summary>
        private static void Castle(Transform model, int color)
        {
            Color tint = Faded(color);
            Material cloth = OwnerCloth(color);
            Material tiles = color == 4 ? Bare : null;
            Material slate = color == 4 ? Bare : Slate;

            var keep = new Plan
            {
                Wide = 2, Deep = 2, Floors = new[] { Facade.Stone, Facade.Stone, Facade.Stone }, Roof = Roofing.Spire, Tiles = slate, Door = -1, Sill = 1,
                Shutters = Sides.None, Fancy = false, Seed = 101, Tint = tint
            };
            V.House(model, "Keep", keep, new Vector3(0f, 0f, 1.6f), 0f);
            Pennant(model, new Vector3(0f, 9f, 1.6f), 6.79f, cloth, tint);
            V.WallBanner(model, new Vector3(-0.95f, 8.5f, -0.49f), 180f, cloth, 3.2f);
            V.WallBanner(model, new Vector3(0.95f, 8.5f, -0.49f), 180f, cloth, 3.2f);
            V.WallBanner(model, new Vector3(0f, 8.5f, 3.69f), 0f, cloth, 3.2f);

            V.House(model, "HouseWest", new Plan { Wide = 2, Deep = 2, Floors = new[] { Facade.Footed, Facade.Timbered }, Tiles = tiles, Door = 1, Seed = 102, Tint = tint },
                new Vector3(-4.8f, 0f, 1.4f), 0f);
            V.WallBanner(model, new Vector3(-5.8f, 5.6f, -0.69f), 180f, cloth, 2f);
            V.House(model, "HouseEast", new Plan { Wide = 2, Deep = 2, Floors = new[] { Facade.Footed }, RidgeAcross = true, Tiles = tiles, Door = 0, Vines = true, Chimney = true, Seed = 103, Tint = tint },
                new Vector3(4.8f, 0f, 1.8f), 0f);
            V.WallBanner(model, new Vector3(5.8f, 2.6f, -0.29f), 180f, cloth, 1.8f);
            V.House(model, "HouseNorthWest", new Plan { Wide = 2, Deep = 2, Floors = new[] { Facade.Footed }, RidgeAcross = true, Tiles = tiles, Door = -1, Windows = Sides.Back, Shutters = Sides.Back, Seed = 104, Tint = tint },
                new Vector3(-3.6f, 0f, 6.6f), 0f);
            V.House(model, "HouseNorthEast", new Plan { Wide = 2, Deep = 2, Floors = new[] { Facade.Footed }, Tiles = tiles, Door = -1, Windows = Sides.Back, Shutters = Sides.Back, Seed = 105, Tint = tint },
                new Vector3(3.8f, 0f, 6.6f), 0f);

            // The curtain wall: crenellated, across the front and back along the sides, its gate between two towers.
            const float wallZ = -5.0f;
            const float wallX = 7.4f;
            foreach (int side in new[] { -1, 1 })
            {
                var gateTower = new Plan
                {
                    Wide = 1, Deep = 1, Floors = new[] { Facade.Stone, Facade.Stone }, Roof = Roofing.Battlements, Door = -1, Windows = Sides.Front, Sill = 1,
                    Shutters = Sides.None, Fancy = false, Seed = 110 + side, Tint = tint
                };
                V.House(model, side < 0 ? "GateTowerWest" : "GateTowerEast", gateTower, new Vector3(side * 2f, 0f, wallZ + 0.6f), 0f);
                V.Standard(model, new Vector3(side * 2f, 6.2f, wallZ + 0.8f), 0f, cloth, 3.4f, tint, 1.2f);
                for (int i = 0; i < 2; i++)
                {
                    V.Put(model, "Wall_UnevenBrick_Straight", new Vector3(side * (4f + 2f * i), 0f, wallZ), 180f, tint);
                }
                V.Merlons(model, new Vector3(side * 3.1f, 3.12f, wallZ + 0.11f), new Vector3(side * (wallX - 0.2f), 3.12f, wallZ + 0.11f), 7, tint);
                for (int i = 0; i < 3; i++)
                {
                    V.Put(model, "Wall_UnevenBrick_Straight", new Vector3(side * wallX, 0f, wallZ + 1f + 2f * i), side < 0 ? 270f : 90f, tint);
                }
                V.Merlons(model, new Vector3(side * (wallX - 0.11f), 3.12f, wallZ + 0.6f), new Vector3(side * (wallX - 0.11f), 3.12f, wallZ + 6f), 7, tint);
                V.Prop(model, $"{Village}/Prop_Chimney2", new Vector3(side * wallX, 0f, wallZ), Quaternion.identity, new Vector3(0.75f, 1.2f, 0.75f), 0f, tint);
            }
            V.Put(model, "Wall_UnevenBrick_Door_Round", new Vector3(0f, 0f, wallZ), 180f, tint);
            V.Put(model, "Door_1_Round", new Vector3(-0.53f, 0f, wallZ + 0.05f), 180f + 75f, tint);
            V.Merlons(model, new Vector3(-0.9f, 3.12f, wallZ + 0.11f), new Vector3(0.9f, 3.12f, wallZ + 0.11f), 3, tint);
            V.WallBanner(model, new Vector3(0f, 3.0f, wallZ - 0.17f), 180f, cloth, 1.2f);

            // The square inside the gate: flagstones, a stall under an awning in the owner's colour.
            for (int x = -1; x <= 1; x++)
            {
                for (int z = 0; z < 2; z++)
                {
                    V.Put(model, "Floor_UnevenBrick", new Vector3(x * 2f, 0.03f, wallZ + 1.4f + z * 2f), 0f, tint);
                }
            }
            V.Put(model, "Floor_UnevenBrick", new Vector3(0f, 0.03f, wallZ - 1.0f), 0f, tint);
            GameObject stall = V.Prop(model, $"{Props}/Stall_Empty", new Vector3(-2.9f, 0f, -2.4f), 15f, 2.4f, 1f, tint);
            V.Swap(stall, "MI_Banner", cloth);
            Green(model, ("Grass_Common_Short", 3.4f, -6.2f, 0.9f), ("Grass_Common_Short", -3.6f, -6.0f, 1f), ("Bush_Common", 6.4f, 5.0f, 1.4f));
        }

        /// <summary>
        /// The Necropolis: a cathedral gone cold, dark stone under violet slate, its bell tower the tallest thing for miles,
        /// the owner's banners long down its face and a cold light in its windows; crypts and a mausoleum about it, graves
        /// behind iron fences, dead trees, candles.
        /// </summary>
        private static void Necropolis(Transform model, int color)
        {
            var stone = new Color(0.74f, 0.72f, 0.82f);
            Color tint = color == 4 ? new Color(0.66f, 0.66f, 0.7f) : stone;
            Material slate = color == 4 ? Bare : NecroSlate;
            Material cloth = OwnerCloth(color);

            var nave = new Plan
            {
                Wide = 2, Deep = 3, Floors = new[] { Facade.Stone, Facade.Stone }, Roof = Roofing.Tiles, Tiles = slate, Door = -1, Windows = Sides.Left | Sides.Right | Sides.Back,
                Sill = 1, Shutters = Sides.None, Fancy = false, Glass = Witchlight, Seed = 201, Tint = tint
            };
            V.House(model, "Nave", nave, new Vector3(0f, 0f, 2.6f), 0f);
            var belfry = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Stone, Facade.Stone, Facade.Stone, Facade.Stone }, Roof = Roofing.Spire, SpireScale = 1.3f,
                Tiles = slate, Door = 0, Windows = Sides.Back, Sill = 2, Shutters = Sides.None, Glass = Witchlight, Seed = 202, Tint = tint
            };
            V.House(model, "Belfry", belfry, new Vector3(0f, 0f, -1.4f), 0f);
            Pennant(model, new Vector3(0f, 12f, -1.4f), 6.79f * 0.65f, cloth, tint);
            V.WallBanner(model, new Vector3(0f, 10.6f, -2.49f), 180f, cloth, 5.6f);
            V.WallBanner(model, new Vector3(-2.09f, 5.6f, 2.6f), 270f, cloth, 3f);
            V.WallBanner(model, new Vector3(2.09f, 5.6f, 2.6f), 90f, cloth, 3f);
            V.WallBanner(model, new Vector3(0f, 5.6f, 5.69f), 0f, cloth, 3f);
            foreach ((string name, Vector3 at, int seed) in new[] { ("CryptWest", new Vector3(-4.6f, 0f, -0.4f), 203), ("CryptEast", new Vector3(4.6f, 0f, 0.4f), 204) })
            {
                var crypt = new Plan
                {
                    Wide = 1, Deep = 1, Floors = new[] { Facade.Stone }, Roof = Roofing.Tiles, Tiles = slate, Door = 0, Windows = Sides.None, Seed = seed, Tint = tint
                };
                V.House(model, name, crypt, at, 0f);
            }
            V.House(model, "Ossuary", new Plan { Wide = 2, Deep = 1, Floors = new[] { Facade.Stone }, RidgeAcross = true, Tiles = slate, Door = 0, Windows = Sides.Back, Shutters = Sides.None, Fancy = false, Glass = Witchlight, Seed = 205, Tint = tint },
                new Vector3(-4.6f, 0f, 4.6f), 0f);
            V.House(model, "Mausoleum", new Plan { Wide = 1, Deep = 2, Floors = new[] { Facade.Stone }, Tiles = slate, Door = -1, Windows = Sides.Back | Sides.Right, Shutters = Sides.None, Fancy = false, Glass = Witchlight, Seed = 206, Tint = tint },
                new Vector3(4.8f, 0f, 4.6f), 0f);

            // Iron fences round the yard of graves before it, the way in between stone posts with candles on them.
            const float fenceZ = -4.8f;
            foreach (int side in new[] { -1, 1 })
            {
                V.Line(model, $"{Halloween}/fence", new Vector3(side * 1.5f, 0f, fenceZ), new Vector3(side * 6.6f, 0f, fenceZ), 4f, tint);
                V.Line(model, $"{Halloween}/fence", new Vector3(side * 6.6f, 0f, fenceZ), new Vector3(side * 6.6f, 0f, -1.6f), 4f, tint);
                V.Prop(model, $"{Village}/Prop_Chimney2", new Vector3(side * 1.3f, 0f, fenceZ), Quaternion.Euler(0f, 45f, 0f), new Vector3(0.55f, 0.7f, 0.55f), 0f, tint);
                Candles(model, new Vector3(side * 1.3f, 2.1f, fenceZ), 0f);
                V.Standard(model, new Vector3(side * 2.3f, 0f, fenceZ + 0.7f), 0f, cloth, 5f, tint, 1.2f);
                Grave(model, "gravestone", new Vector3(side * 3.4f, 0f, -3.6f), side * 10f);
                Grave(model, side < 0 ? "grave_A" : "grave_B", new Vector3(side * 4.8f, 0f, -3.3f), -side * 8f);
                Grave(model, side < 0 ? "gravemarker_A" : "gravemarker_B", new Vector3(side * 5.8f, 0f, -2.3f), side * 15f);
                Grave(model, "gravestone", new Vector3(side * 3.0f, 0f, -2.1f), -side * 6f);
            }
            V.Prop(model, $"{Nature}/DeadTree_5", new Vector3(-6.9f, 0f, 1.8f), 30f, 8.4f);
            V.Prop(model, $"{Halloween}/tree_dead_large", new Vector3(6.8f, 0f, 2.6f), 200f, 6.4f, 1f, new Color(0.62f, 0.58f, 0.62f));
            Candles(model, new Vector3(-1.3f, 0f, -3.0f), 1.3f);
            Candles(model, new Vector3(1.3f, 0f, -3.0f), 1.3f);
        }

        /// <summary>
        /// The Stronghold: a great hall of timber under planks and a ridge log, its gable to the gate, a lookout flying the
        /// clan's war banner, huts, a market of stalls under awnings in the owner's colour, a rack of weapons and an anvil,
        /// the fire the clan gathers round, all behind a palisade of stakes hung with shields painted in the owner's colour,
        /// tall war standards at its gate.
        /// </summary>
        private static void Stronghold(Transform model, int color)
        {
            Color tint = Faded(color);
            Material cloth = OwnerCloth(color);
            Color paint = color < 4 ? Color.Lerp(Color.white, Player(color), 0.85f) : new Color(0.8f, 0.8f, 0.78f);

            var hall = new Plan { Wide = 2, Deep = 3, Floors = new[] { Facade.Footed, Facade.Timbered }, Roof = Roofing.Planks, Door = 0, Fancy = false, Seed = 301, Tint = tint };
            V.House(model, "Hall", hall, new Vector3(0f, 0f, 2.0f), 0f);
            Log(model, new Vector3(0f, 7.95f, 2.0f), 0f, 7.6f, 0.55f, tint);
            V.WallBanner(model, new Vector3(0.95f, 5.6f, -1.09f), 180f, cloth, 2.8f);
            V.WallBanner(model, new Vector3(0f, 5.0f, 5.09f), 0f, cloth, 2.8f);
            var lookout = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Timbered, Facade.Open, Facade.Open }, Roof = Roofing.Planks, Railings = true, Door = 0, Windows = Sides.None, Seed = 302, Tint = tint
            };
            V.House(model, "Lookout", lookout, new Vector3(-5.4f, 0f, 3.6f), 0f);
            V.Standard(model, new Vector3(-5.4f, 9.6f, 3.6f), 0f, cloth, 4.2f, tint, 1.3f);
            V.House(model, "HutEast", new Plan { Wide = 1, Deep = 1, Floors = new[] { Facade.Timbered }, Roof = Roofing.Planks, Door = 0, Windows = Sides.Back, Fancy = false, Seed = 303, Tint = tint },
                new Vector3(5.4f, 0f, 4.4f), -10f);
            V.House(model, "HutSouthEast", new Plan { Wide = 2, Deep = 1, Floors = new[] { Facade.Footed }, Roof = Roofing.Planks, RidgeAcross = true, Door = 0, Fancy = false, Seed = 304, Tint = tint },
                new Vector3(4.8f, 0f, 0.6f), 0f);

            foreach ((float x, float z, float yaw) in new[] { (-3.4f, -1.8f, 10f), (3.0f, -2.6f, -15f) })
            {
                GameObject stall = V.Prop(model, $"{Props}/Stall_Empty", new Vector3(x, 0f, z), yaw, 2.5f, 1f, tint);
                V.Swap(stall, "MI_Banner", cloth);
            }
            V.Prop(model, $"{Props}/WeaponStand", new Vector3(-5.2f, 0f, 0.2f), 90f, 1.15f, 1f, tint);
            Stump(model, new Vector3(-4.4f, 0f, -1.6f), 0.55f, 0.8f, tint);
            V.Prop(model, $"{Props}/Anvil", new Vector3(-4.4f, 0.55f, -1.6f), 30f, 0.5f, 1f, tint);

            // A palisade of stakes across the front, open in the middle where the war standards stand, shields hung on it.
            const float wallZ = -4.8f;
            Material painted = HeroesKitArt.Material("Props", "MI_Trim_Furniture", -1, paint);
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 8; i++)
                {
                    Stump(model, new Vector3(side * (1.9f + i * 0.7f), 0f, wallZ + (i % 2) * 0.06f), 2.4f + (i * 7 % 3) * 0.18f, 0.66f, tint);
                }
                for (int i = 1; i < 5; i++)
                {
                    Stump(model, new Vector3(side * 7.1f, 0f, wallZ + i * 0.7f), 2.4f + (i * 5 % 3) * 0.18f, 0.66f, tint);
                }
                foreach (float x in new[] { 3.0f, 5.4f })
                {
                    GameObject shield = V.Prop(model, $"{Props}/Shield_Wooden", new Vector3(side * x, 1.2f, wallZ - 0.36f), 180f, 0.95f, 1f, tint);
                    V.Swap(shield, "MI_Trim_Furniture", painted);
                }
                V.Prop(model, $"{Halloween}/post_skull", new Vector3(side * 1.45f, 0f, wallZ - 0.2f), 180f, 3.2f, 1f, tint);
                V.Standard(model, new Vector3(side * 1.5f, 0f, wallZ + 0.9f), 0f, cloth, 6f, tint, 1.4f);
            }
            Transform fire = V.Holder(model, "Fire", new Vector3(0.2f, 0f, -2.0f), 0f, 1.6f);
            Fire(fire, 7);
            Green(model, ("Pine_5", -5.8f, 7.2f, 6.8f), ("Pine_5", 6.0f, 7.4f, 6.2f), ("Grass_Common_Tall", 6.2f, -3.4f, 1.2f), ("Grass_Common_Tall", -6.2f, -3.0f, 1.1f));
        }

        // ------------------------------------------------------------------ sieges

        /// <summary>How small the arrow tower is drawn: it stands about 3.2 m tall, its pennant over that.</summary>
        private const float ArrowTowerScale = 0.29f;

        /// <summary>The arrow tower a besieged town fights with, in the colour of its owner (4 for nobody).</summary>
        public static GameObject Tower(int color)
        {
            GameObject root = HeroesArtBuilder.Root($"Tower{color}");
            Transform model = V.Holder(root.transform, "Model", Vector3.zero, 0f, ArrowTowerScale);
            ArrowTowerModel(model, color);
            V.Settle(root);
            return HeroesArtBuilder.Save(root, $"{Generated}/Scenery/Tower{color}.prefab");
        }

        /// <summary>The arrow tower as the creature that shoots from a town's wall.</summary>
        public static HeroesArt.UnitArt ArrowTower(HeroesArtBuilder.UnitSpec spec)
        {
            CreatureDef def = Creatures.Get(spec.Creature);
            float height = HeroesArtBuilder.HeightOf(def, spec.Height);
            GameObject root = HeroesArtBuilder.Root(def.Name.Replace(" ", string.Empty));
            Transform model = V.Holder(root.transform, "Model", Vector3.zero, 0f, ArrowTowerScale * height / 3.2f);
            ArrowTowerModel(model, 0);
            V.Settle(root);
            GameObject prefab = HeroesArtBuilder.Save(root, $"{Generated}/Units/{spec.Creature}.prefab");
            return new HeroesArt.UnitArt
            {
                creature = spec.Creature,
                prefab = prefab,
                height = height,
                idle = spec.Idle,
                walk = spec.Walk,
                attack = spec.Attack,
                shoot = spec.Shoot,
                hit = spec.Hit,
                death = spec.Death,
                cast = spec.Cast,
                projectile = spec.Projectile,
                flies = def.Abilities.HasFlag(Ability.Flying)
            };
        }

        /// <summary>A tower of stone two floors high under a slate spire flying its owner's pennant, banners on its faces.</summary>
        private static void ArrowTowerModel(Transform model, int color)
        {
            var plan = new Plan
            {
                Wide = 1, Deep = 1, Floors = new[] { Facade.Stone, Facade.Stone }, Roof = Roofing.Spire, SpireScale = 1.25f, Tiles = color == 4 ? Bare : Slate, Door = -1,
                Sill = 1, Shutters = Sides.None, Fancy = false, Seed = 401, Tint = Faded(color)
            };
            V.House(model, "Tower", plan, Vector3.zero, 0f);
            Material cloth = OwnerCloth(color);
            Pennant(model, new Vector3(0f, 6f, 0f), 6.79f * 0.625f, cloth, Faded(color), 1.8f);
            V.WallBanner(model, new Vector3(0f, 5.6f, -1.09f), 180f, cloth, 2.6f);
            V.WallBanner(model, new Vector3(0f, 5.6f, 1.09f), 0f, cloth, 2.6f);
        }
    }
}
