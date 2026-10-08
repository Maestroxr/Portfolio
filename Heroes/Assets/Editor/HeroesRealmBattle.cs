using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Paint = Portfolio.Heroes.EditorTools.HeroesBattleArt.Paint;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// The battlefield of a theme made of the textured kits (<see cref="ModelSet.Kits"/>): its obstacles on every ground,
    /// the ring of hills and woods around a field, and the gatehouse, ruin, stones and banners of a siege. The obstacles
    /// are put together from the nature kit's rocks, trees, grass and mushrooms, the village kit's stone and timber and the
    /// props kit's odds and ends, each standing on one hexagon of 2.08 m, filed as the classic ones are
    /// (<see cref="HeroesBattleArt"/>); on snow, sand, bog, ash and under the earth their stone, bark, leaves and grass are
    /// repainted for the ground. The stumps and the felled logs are cut from the kit's trees, and the pines in snow carry
    /// it in the colours of their vertices. The hills are crags of the kit's rocks scaled up into cliffs and woods of its
    /// trees; the town's wall and gatehouse are of the village's uneven brick crowned with its rock trim.
    /// </summary>
    internal static class HeroesRealmBattle
    {
        private const string Nature = HeroesKitArt.Models + "/Nature";
        private const string Village = HeroesKitArt.Models + "/Village";
        private const string Props = HeroesKitArt.Models + "/Props";

        /// <summary>The theme being built, and where its battlefield art goes.</summary>
        private static HeroesThemeSpec Spec => HeroesThemeSpec.Current;
        private static string Folder => $"{Spec.Generated}/Battlefield";

        /// <summary>A hexagon of the battlefield is 2.08 m across; an obstacle keeps inside it.</summary>
        private const float Cell = HexLayout.DefaultRadius * 1.7320508f;

        /// <summary>The yaw that turns the brick face of a wall of the village toward +z.</summary>
        private const float BrickOut = 0f;

        // ------------------------------------------------------------------ putting pieces together

        /// <summary>
        /// Adds a model to a prefab being built (its root at the origin), as it is meant to stand: turned
        /// <paramref name="yaw"/> and tipped by <paramref name="tilt"/> (about x, then z), <paramref name="height"/> tall (or at
        /// its own size), times <paramref name="scale"/>, the middle of its footprint over <paramref name="at"/> and its lowest
        /// point at the height of <paramref name="at"/>. The village and props kits carry the turn that stands a model up on
        /// the root of its file, which <see cref="HeroesArtBuilder.Piece"/> replaces with the yaw; it is put back under the yaw.
        /// </summary>
        private static GameObject Put(Transform parent, string model, Vector3 at, float yaw = 0f, float height = 0f, float scale = 1f,
            Vector2 tilt = default, Color tint = default)
        {
            GameObject piece = HeroesArtBuilder.Piece(parent, model, Vector3.zero, 0f, scale, height, tint);
            if (piece == null)
            {
                return null;
            }
            GameObject asset = HeroesArtBuilder.Model(model);
            piece.transform.localRotation = Quaternion.Euler(tilt.x, yaw, tilt.y) * asset.transform.localRotation;
            Settle(piece, at);
            return piece;
        }

        /// <summary>Moves a piece so the middle of its footprint is over <paramref name="at"/> and its lowest point at its height.</summary>
        private static void Settle(GameObject piece, Vector3 at)
        {
            Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
            piece.transform.position += new Vector3(at.x - bounds.center.x, at.y - bounds.min.y, at.z - bounds.center.z);
        }

        /// <summary>
        /// Stretches a piece to <paramref name="size"/> (meters along the world's axes; 0 leaves an axis as it is), each of
        /// its own axes by the world axis it lies along (the pieces are turned in quarters when they are stretched).
        /// </summary>
        private static void Resize(GameObject piece, Vector3 size)
        {
            Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
            var factor = new Vector3(
                size.x > 0f ? size.x / Mathf.Max(bounds.size.x, 0.0001f) : 1f,
                size.y > 0f ? size.y / Mathf.Max(bounds.size.y, 0.0001f) : 1f,
                size.z > 0f ? size.z / Mathf.Max(bounds.size.z, 0.0001f) : 1f);
            Vector3 scale = piece.transform.localScale;
            Quaternion turn = piece.transform.rotation;
            for (int i = 0; i < 3; i++)
            {
                Vector3 axis = turn * new Vector3(i == 0 ? 1f : 0f, i == 1 ? 1f : 0f, i == 2 ? 1f : 0f);
                var along = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
                int world = along.x >= along.y && along.x >= along.z ? 0 : along.y >= along.z ? 1 : 2;
                scale[i] *= factor[world];
            }
            piece.transform.localScale = scale;
        }

        /// <summary>
        /// A model of the kits stood as a block: turned <paramref name="yaw"/>, stretched to <paramref name="size"/> (as it
        /// stands after the turn) and put with the middle of its footprint over <paramref name="at"/>, its foot at its height.
        /// </summary>
        private static GameObject Block(Transform parent, string model, Vector3 at, float yaw, Vector3 size)
        {
            GameObject piece = Put(parent, model, Vector3.zero, yaw);
            if (piece == null)
            {
                return null;
            }
            Resize(piece, size);
            Settle(piece, at);
            return piece;
        }

        /// <summary>
        /// A length of the village's wall of <paramref name="model"/>, its brick face outward toward <paramref name="facing"/>
        /// (degrees about y from +z), the middle of its foot at <paramref name="at"/>, <paramref name="width"/> long,
        /// <paramref name="height"/> tall and <paramref name="thickness"/> deep.
        /// </summary>
        private static GameObject Panel(Transform parent, string model, Vector3 at, float facing, float width, float height, float thickness)
        {
            bool across = Mathf.Abs(Mathf.DeltaAngle(facing, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(facing, 270f)) < 1f;
            return Block(parent, model, at, facing + BrickOut, across ? new Vector3(thickness, height, width) : new Vector3(width, height, thickness));
        }

        /// <summary>Scales an obstacle down about its middle on the ground if it spills out of its cell.</summary>
        private static void Fit(GameObject root)
        {
            Bounds bounds = HeroesArtBuilder.BoundsOf(root);
            // The things on a cell are round more than square: their widest half against the hexagon's inner circle, a little over.
            float reach = Mathf.Max(Mathf.Max(-bounds.min.x, bounds.max.x), Mathf.Max(-bounds.min.z, bounds.max.z)) * 1.08f;
            float limit = Cell * 0.5f * 1.12f;
            if (reach > limit)
            {
                float scale = limit / reach;
                foreach (Transform child in root.transform)
                {
                    child.localPosition *= scale;
                    child.localScale *= scale;
                }
            }
        }

        private static Vector3 At(float x, float z, float y = 0f)
        {
            return new Vector3(x, y, z);
        }

        // ------------------------------------------------------------------ the grounds

        private static readonly Color SandRock = new Color(0.96f, 0.84f, 0.64f);
        private static readonly Color AshRock = new Color(0.62f, 0.52f, 0.46f);
        private static readonly Color DeepRock = new Color(0.55f, 0.55f, 0.64f);
        private static readonly Color Amethyst = new Color(0.68f, 0.44f, 1f);
        private static readonly Color Ruby = new Color(0.92f, 0.2f, 0.22f);
        private static readonly Color Weathered = new Color(0.78f, 0.77f, 0.76f);

        /// <summary>
        /// How the kits look on a group of grounds (the first ground names the prefabs): by the name of a kit material, what it
        /// is repainted to there, and the classic paint of the packs' models (mountains, bones).
        /// </summary>
        private sealed class Ground
        {
            public readonly string Name;
            public readonly TerrainType[] Grounds;
            public readonly Paint Paint;
            private readonly Dictionary<string, Material> swaps = new Dictionary<string, Material>();

            public Ground(string name, TerrainType[] grounds, Paint paint)
            {
                Name = name;
                Grounds = grounds;
                Paint = paint;
            }

            public bool Green => Name == "Green";
            public bool Snow => Name == "Snow";
            public bool Sand => Name == "Sand";
            public bool Swamp => Name == "Swamp";
            public bool Waste => Name == "Waste";
            public bool Deep => Name == "Deep";

            /// <summary>The kit's material <paramref name="material"/> repainted by <paramref name="paint"/> (and tinted) on this ground.</summary>
            public void Repaint(string kit, string material, Func<Color, Color> paint, Color tint = default)
            {
                swaps[material] = HeroesKitArt.Repainted(kit, material, -1, Name, paint, tint);
            }

            /// <summary>The kit's material <paramref name="material"/> with another of its base colours on this ground.</summary>
            public void Alternate(string kit, string material, int alternate)
            {
                swaps[material] = HeroesKitArt.Material(kit, material, alternate);
            }

            /// <summary>Gives a kit piece the ground's materials.</summary>
            public GameObject Dress(GameObject piece)
            {
                if (piece != null)
                {
                    foreach (KeyValuePair<string, Material> swap in swaps)
                    {
                        HeroesKitArt.Swap(piece, swap.Key, swap.Value);
                    }
                }
                return piece;
            }

            /// <summary>A kit model put as <see cref="HeroesRealmBattle.Put"/> does, in the ground's materials (and a pine in snow under snow).</summary>
            public GameObject Kit(Transform parent, string model, Vector3 at, float yaw = 0f, float height = 0f, float scale = 1f, Vector2 tilt = default)
            {
                GameObject piece = Dress(Put(parent, model, at, yaw, height, scale, tilt));
                if (Snow && piece != null && model.StartsWith($"{Nature}/Pine_", StringComparison.Ordinal))
                {
                    Snowcap(piece, model);
                }
                return piece;
            }

            /// <summary>A model of the packs put the same way, in the classic paint of the ground.</summary>
            public GameObject Pack(Transform parent, string model, Vector3 at, float yaw = 0f, float height = 0f, Vector2 tilt = default)
            {
                GameObject piece = Put(parent, model, at, yaw, height, 1f, tilt);
                HeroesBattleArt.Repaint(piece, Paint);
                return piece;
            }
        }

        private static Func<Color, Color> Then(Func<Color, Color> first, Func<Color, Color> second)
        {
            return pixel => second(first(pixel));
        }

        /// <summary>
        /// The pixels of a hue range (0..1, through red when from &gt; to) saturated at least <paramref name="saturated"/>
        /// covered by <paramref name="color"/>: snow on the moss of a stone, ash on its lichen. Their light and shade stay.
        /// </summary>
        private static Func<Color, Color> Cover(float from, float to, float saturated, Color color, float amount)
        {
            return pixel =>
            {
                Color.RGBToHSV(pixel, out float h, out float s, out float v);
                bool inside = from <= to ? h >= from && h <= to : h >= from || h <= to;
                if (!inside || s < saturated)
                {
                    return pixel;
                }
                float lean = Mathf.Clamp01((s - saturated) / 0.15f) * amount;
                return Color.Lerp(pixel, color * (0.7f + 0.6f * v), lean);
            };
        }

        /// <summary>Every pixel washed from <paramref name="dark"/> to <paramref name="light"/> by its brightness (one colour over all).</summary>
        private static Func<Color, Color> Wash(Color dark, Color light, float amount, float contrast = 1f)
        {
            return pixel =>
            {
                float luma = Mathf.Clamp01((pixel.r * 0.3f + pixel.g * 0.59f + pixel.b * 0.11f - 0.5f) * contrast + 0.5f);
                return Color.Lerp(pixel, Color.Lerp(dark, light, luma), amount);
            };
        }

        /// <summary>
        /// The groups of grounds and their looks: green lands as the kits are, snow on the stones and needles, the desert's own
        /// stone and bleached wood, bog green and dark, ash and charred wood, and the cold stone under the earth.
        /// </summary>
        private static Ground[] Grounds()
        {
            var green = new Ground("Green", new[] { TerrainType.Grass, TerrainType.Dirt, TerrainType.Rough, TerrainType.Water }, Paint.None);

            var snow = new Ground("Snow", new[] { TerrainType.Snow }, Paint.Snow);
            Func<Color, Color> frost = Then(Cover(0.12f, 0.5f, 0.34f, new Color(0.9f, 0.93f, 1f), 1f),
                Wash(new Color(0.36f, 0.4f, 0.48f), new Color(0.86f, 0.9f, 0.96f), 0.3f, 1.2f));
            snow.Repaint("Nature", "Rocks", frost);
            snow.Repaint("Nature", "PathRocks", frost);
            snow.Repaint("Nature", "Bark_DeadTree", Wash(new Color(0.22f, 0.24f, 0.3f), new Color(0.85f, 0.88f, 0.95f), 0.5f));
            snow.Repaint("Nature", "Bark_NormalTree", Wash(new Color(0.24f, 0.2f, 0.2f), new Color(0.8f, 0.78f, 0.8f), 0.35f));
            snow.Repaint("Nature", "Grass", Wash(new Color(0.55f, 0.5f, 0.42f), new Color(0.92f, 0.9f, 0.84f), 0.75f));
            snow.Repaint("Village", "MI_UnevenBrick", Wash(new Color(0.32f, 0.35f, 0.42f), new Color(0.9f, 0.93f, 0.98f), 0.4f));

            var sand = new Ground("Sand", new[] { TerrainType.Sand }, Paint.Sand);
            sand.Alternate("Nature", "Rocks", 0);
            sand.Repaint("Nature", "PathRocks", Wash(new Color(0.5f, 0.38f, 0.24f), new Color(0.95f, 0.84f, 0.62f), 0.7f));
            sand.Repaint("Nature", "Bark_DeadTree", Wash(new Color(0.46f, 0.38f, 0.3f), new Color(0.95f, 0.88f, 0.74f), 0.85f, 2.2f));
            sand.Repaint("Nature", "Grass", HeroesKitArt.Hue(0.17f, 0.47f, 0.2f, new Color(0.86f, 0.72f, 0.38f)));
            sand.Repaint("Nature", "Leaves_Pine", Wash(new Color(0.12f, 0.16f, 0.06f), new Color(0.46f, 0.5f, 0.2f), 0.5f));
            sand.Repaint("Village", "MI_UnevenBrick", Wash(new Color(0.4f, 0.32f, 0.22f), new Color(0.9f, 0.8f, 0.6f), 0.4f));

            var swamp = new Ground("Swamp", new[] { TerrainType.Swamp }, Paint.Bog);
            Func<Color, Color> mire = Then(HeroesKitArt.Grade(1.1f, 0.85f, new Color(0.45f, 0.5f, 0.3f), 0.2f), Cover(0.12f, 0.5f, 0.2f, new Color(0.3f, 0.4f, 0.14f), 0.5f));
            swamp.Repaint("Nature", "Rocks", mire);
            swamp.Repaint("Nature", "PathRocks", mire);
            swamp.Repaint("Nature", "Bark_DeadTree", Wash(new Color(0.24f, 0.26f, 0.16f), new Color(0.7f, 0.7f, 0.52f), 0.7f, 2f));
            swamp.Repaint("Nature", "Bark_NormalTree", HeroesKitArt.Grade(0.6f, 0.75f, new Color(0.45f, 0.48f, 0.32f), 0.3f));
            swamp.Repaint("Nature", "Bark_TwistedTree", HeroesKitArt.Grade(0.6f, 0.8f, new Color(0.45f, 0.48f, 0.32f), 0.3f));
            // The twisted trees' leaves are a white mask the kit tints green: in the bog a darker, duller olive.
            swamp.Repaint("Nature", "Leaves_TwistedTree", Wash(new Color(0.5f, 0.5f, 0.42f), new Color(0.86f, 0.84f, 0.7f), 1f), new Color(0.86f, 0.62f, 0.8f));
            swamp.Repaint("Nature", "Grass", HeroesKitArt.Hue(0.1f, 0.47f, 0.2f, new Color(0.36f, 0.46f, 0.16f)));

            var waste = new Ground("Waste", new[] { TerrainType.Wasteland }, Paint.Ash);
            Func<Color, Color> ash = Then(Cover(0.12f, 0.5f, 0.2f, new Color(0.42f, 0.36f, 0.32f), 1f), Wash(new Color(0.16f, 0.12f, 0.1f), new Color(0.72f, 0.6f, 0.52f), 0.5f));
            waste.Repaint("Nature", "Rocks", ash);
            waste.Repaint("Nature", "PathRocks", ash);
            // Burnt to the grey of ash, to stand out on the dark ground.
            waste.Repaint("Nature", "Bark_DeadTree", Wash(new Color(0.26f, 0.23f, 0.22f), new Color(0.78f, 0.72f, 0.66f), 0.8f, 2.2f));
            waste.Repaint("Nature", "Grass", Wash(new Color(0.24f, 0.18f, 0.12f), new Color(0.62f, 0.5f, 0.36f), 0.85f));
            waste.Repaint("Nature", "Leaves_Pine", Wash(new Color(0.08f, 0.08f, 0.06f), new Color(0.32f, 0.3f, 0.2f), 0.6f));
            waste.Repaint("Village", "MI_UnevenBrick", Wash(new Color(0.16f, 0.13f, 0.12f), new Color(0.66f, 0.56f, 0.5f), 0.35f));

            var deep = new Ground("Deep", new[] { TerrainType.Rock }, Paint.Deep);
            Func<Color, Color> cold = Then(Cover(0.12f, 0.5f, 0.2f, new Color(0.24f, 0.3f, 0.36f), 0.9f), Wash(new Color(0.1f, 0.1f, 0.14f), new Color(0.6f, 0.62f, 0.72f), 0.5f));
            deep.Repaint("Nature", "Rocks", cold);
            deep.Repaint("Nature", "PathRocks", cold);
            deep.Repaint("Nature", "Bark_DeadTree", Wash(new Color(0.2f, 0.2f, 0.24f), new Color(0.66f, 0.66f, 0.74f), 0.7f, 2f));
            deep.Repaint("Nature", "Leaves_Pine", Wash(new Color(0.04f, 0.08f, 0.08f), new Color(0.18f, 0.3f, 0.3f), 0.6f));
            deep.Repaint("Village", "MI_UnevenBrick", Wash(new Color(0.1f, 0.1f, 0.14f), new Color(0.6f, 0.62f, 0.7f), 0.35f));

            return new[] { green, snow, sand, swamp, waste, deep };
        }

        // ------------------------------------------------------------------ obstacles

        public static void Obstacles(HeroesArt art)
        {
            art.obstacles.Clear();
            Meshes.Clear();
            snowNeedles = null;
            Material pool = Water("Pool", Spec.Materials.Pool, 0.93f, 0.05f);
            Material bog = Water("Bog", Spec.Materials.Bog, 0.85f, 0f);
            Material ice = Water("Ice", new Color(0.72f, 0.86f, 0.96f, 0.86f), 0.95f, 0.1f);
            Material scorch = Scorch();
            Material grain = EndGrain();
            foreach (Ground ground in Grounds())
            {
                Ground g = ground;
                Obstacle(art, BattleObstacle.Rock, g, 3, (root, i) => Rock(root, g, i));
                Obstacle(art, BattleObstacle.Boulder, g, 2, (root, i) => Boulder(root, g, i));
                Obstacle(art, BattleObstacle.Tree, g, g.Green ? 3 : 2, (root, i) => Tree(root, g, i));
                Obstacle(art, BattleObstacle.Pine, g, 3, (root, i) => Pine(root, g, i));
                Obstacle(art, BattleObstacle.DeadTree, g, 3, (root, i) => DeadTree(root, g, i));
                Obstacle(art, BattleObstacle.Stump, g, 2, (root, i) => Stump(root, g, i, grain));
                Obstacle(art, BattleObstacle.Logs, g, 2, (root, i) => Logs(root, g, i, grain));
                Obstacle(art, BattleObstacle.Bones, g, 2, (root, i) => Bones(root, g, i));
                Obstacle(art, BattleObstacle.Crystal, g, 2, (root, i) => Crystal(root, g, i));
                Material earth = Earth(art, g);
                Obstacle(art, BattleObstacle.Mound, g, 2, (root, i) => Mound(root, g, i, earth));
                Material water = g.Swamp ? bog : g.Snow ? ice : pool;
                Obstacle(art, BattleObstacle.Pool, g, 2, (root, i) => Pool(root, g, i, water));
                Obstacle(art, BattleObstacle.Rubble, g, 2, (root, i) => Rubble(root, g, i, grain));
                Obstacle(art, BattleObstacle.Crater, g, 2, (root, i) => Crater(root, g, i, scorch));
            }
            Walls(art);
            // The snow-capped pines also ring a battlefield on snow, instead of the green ones of the map.
            art.snowTrees = art.ObstacleVariants(BattleObstacle.Pine, TerrainType.Snow);
        }

        /// <summary>
        /// Builds <paramref name="looks"/> looks of an obstacle for a group of grounds, each fitted to its cell, and files them
        /// under every ground of the group; the Green group's also serve every ground with nothing of its own.
        /// </summary>
        private static void Obstacle(HeroesArt art, BattleObstacle kind, Ground ground, int looks, Action<Transform, int> build)
        {
            var variants = new List<GameObject>();
            for (int i = 0; i < looks; i++)
            {
                GameObject root = HeroesArtBuilder.Root($"{kind}{ground.Name}{i}");
                build(root.transform, i);
                Fit(root);
                variants.Add(HeroesArtBuilder.Save(root, $"{Folder}/Obstacles/{kind}{ground.Name}{i}.prefab"));
            }
            GameObject[] built = variants.Where(prefab => prefab != null).ToArray();
            foreach (TerrainType terrain in ground.Grounds)
            {
                art.obstacles.Add(new HeroesArt.ObstacleArt { kind = kind, terrain = (int)terrain, variants = built });
            }
            if (ground.Green)
            {
                art.obstacles.Add(new HeroesArt.ObstacleArt { kind = kind, terrain = -1, variants = built });
            }
        }

        /// <summary>A tuft of what grows on the ground at the foot of an obstacle: grass, dry grass, reeds, or a mushroom underground.</summary>
        private static void Tuft(Transform root, Ground g, float x, float z, float size = 1f)
        {
            if (g.Deep)
            {
                g.Kit(root, $"{Nature}/Mushroom_Common", At(x, z), x * 90f, 0.22f * size);
                return;
            }
            string model = g.Swamp ? "Grass_Common_Tall" : g.Green ? "Grass_Common_Short" : "Grass_Wispy_Short";
            g.Kit(root, $"{Nature}/{model}", At(x, z), x * 120f + z * 40f, (g.Swamp ? 0.6f : 0.42f) * size);
        }

        /// <summary>A few pebbles of the path stones around a point.</summary>
        private static void Pebbles(Transform root, Ground g, float x, float z, int count, int seed, float size = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (seed * 1.7f + i * 2.4f) % (Mathf.PI * 2f);
                float reach = 0.12f + 0.08f * ((seed + i) % 3);
                string model = (seed + i) % 2 == 0 ? $"Pebble_Round_{1 + (seed + i) % 5}" : $"Pebble_Square_{1 + (seed + i) % 6}";
                g.Kit(root, $"{Nature}/{model}", At(x + Mathf.Cos(angle) * reach, z + Mathf.Sin(angle) * reach), seed * 37f + i * 71f,
                    (0.09f + 0.03f * ((seed + i) % 3)) * size);
            }
        }

        private static void Rock(Transform root, Ground g, int variant)
        {
            switch (variant)
            {
                case 0:
                    g.Kit(root, $"{Nature}/Rock_Medium_1", At(0f, 0.05f), 20f, 1.1f);
                    Pebbles(root, g, 0.68f, -0.42f, 2, 1);
                    Tuft(root, g, -0.55f, -0.42f);
                    break;
                case 1:
                    g.Kit(root, $"{Nature}/Rock_Medium_2", At(-0.15f, 0.08f), 70f, 0.9f);
                    g.Kit(root, $"{Nature}/Rock_Medium_3", At(0.6f, -0.4f), 200f, 0.5f);
                    Pebbles(root, g, -0.62f, -0.5f, 2, 2);
                    Tuft(root, g, 0.45f, 0.55f);
                    break;
                default:
                    g.Kit(root, $"{Nature}/Rock_Medium_3", At(0f, 0f), 140f, 1.05f);
                    Pebbles(root, g, 0.6f, 0.45f, 2, 3);
                    Tuft(root, g, -0.55f, -0.35f, 1.1f);
                    break;
            }
        }

        private static void Boulder(Transform root, Ground g, int variant)
        {
            if (variant == 0)
            {
                g.Kit(root, $"{Nature}/Rock_Medium_1", At(0f, 0.05f), 0f, 1.75f);
                g.Kit(root, $"{Nature}/Rock_Medium_2", At(0.7f, -0.5f), 40f, 0.5f);
                Pebbles(root, g, -0.65f, -0.5f, 3, 4);
                Tuft(root, g, -0.25f, -0.75f);
            }
            else
            {
                g.Kit(root, $"{Nature}/Rock_Medium_3", At(0f, 0f), 200f, 1.7f);
                g.Kit(root, $"{Nature}/Rock_Medium_1", At(-0.65f, -0.45f), 120f, 0.45f);
                Pebbles(root, g, 0.6f, 0.55f, 2, 5);
                Tuft(root, g, 0.65f, -0.35f);
            }
        }

        private static void Tree(Transform root, Ground g, int variant)
        {
            if (g.Green)
            {
                // The broad crowns read from above, where the battle is watched.
                string[] trees = { "CommonTree_1", "CommonTree_2", "CommonTree_5" };
                g.Kit(root, $"{Nature}/{trees[variant]}", At(0f, 0f), variant * 120f, variant == 2 ? 3.9f : 4.2f);
                if (variant == 1)
                {
                    g.Kit(root, $"{Nature}/Bush_Common_Flowers", At(0.55f, -0.45f), 30f, 0.55f);
                }
                if (variant == 2)
                {
                    g.Kit(root, $"{Nature}/Rock_Medium_2", At(-0.55f, -0.45f), 0f, 0.35f);
                    g.Kit(root, $"{Nature}/Mushroom_Common", At(0.4f, -0.5f), 60f, 0.22f);
                }
                Tuft(root, g, -0.4f, 0.5f);
            }
            else if (g.Snow)
            {
                g.Kit(root, $"{Nature}/{(variant == 0 ? "Pine_3" : "Pine_4")}", At(0f, 0f), variant * 90f, 4.3f);
            }
            else if (g.Swamp)
            {
                g.Kit(root, $"{Nature}/{(variant == 0 ? "TwistedTree_2" : "TwistedTree_4")}", At(0f, 0f), variant * 140f, 3.8f);
                Tuft(root, g, 0.5f, -0.45f);
            }
            else
            {
                g.Kit(root, $"{Nature}/{(variant == 0 ? "DeadTree_3" : "DeadTree_1")}", At(0f, 0f), variant * 120f, variant == 0 ? 3.8f : 3.2f);
                Pebbles(root, g, 0.5f, -0.5f, 2, 6 + variant);
            }
        }

        private static void Pine(Transform root, Ground g, int variant)
        {
            switch (variant)
            {
                case 0:
                    g.Kit(root, $"{Nature}/Pine_3", At(0f, 0f), 0f, 4.4f);
                    break;
                case 1:
                    g.Kit(root, $"{Nature}/Pine_4", At(-0.08f, 0.06f), 45f, 4.2f);
                    g.Kit(root, $"{Nature}/Pine_3", At(0.48f, -0.4f), 200f, 1.5f);
                    break;
                default:
                    // A broad pine drawn a little slimmer, to stand tall in its cell.
                    GameObject pine = g.Kit(root, $"{Nature}/Pine_1", At(0f, 0f), 200f, 4.2f);
                    if (pine != null)
                    {
                        Resize(pine, new Vector3(2.15f, 0f, 2.15f));
                        Settle(pine, At(0f, 0f));
                    }
                    break;
            }
            if (!g.Snow)
            {
                Tuft(root, g, -0.45f, -0.5f);
            }
        }

        private static void DeadTree(Transform root, Ground g, int variant)
        {
            switch (variant)
            {
                case 0:
                    g.Kit(root, $"{Nature}/DeadTree_4", At(0f, 0f), 30f, 3.6f);
                    Tuft(root, g, 0.5f, -0.45f);
                    break;
                case 1:
                    g.Kit(root, $"{Nature}/DeadTree_2", At(0f, 0f), 150f, 3.8f);
                    Pebbles(root, g, -0.5f, -0.45f, 2, 8);
                    break;
                default:
                    g.Kit(root, $"{Nature}/DeadTree_1", At(0f, 0f), 260f, 3f);
                    if (g.Swamp || g.Green)
                    {
                        g.Kit(root, $"{Nature}/Mushroom_Laetiporus", At(0.32f, -0.3f), 200f, 0.3f);
                    }
                    else
                    {
                        g.Pack(root, "KayKit/Halloween/bone_A", At(0.55f, -0.4f), 30f, 0.1f);
                    }
                    Tuft(root, g, -0.45f, 0.4f);
                    break;
            }
        }

        // ------------------------------------------------------------------ stumps and logs cut from the trees

        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

        /// <summary>A cut piece of a tree as a model: its mesh in the kit's bark and the end grain, in the ground's materials.</summary>
        private static GameObject Wood(Transform root, Ground g, Mesh mesh, string bark, Material grain)
        {
            var piece = new GameObject(mesh.name);
            piece.transform.SetParent(root, false);
            piece.AddComponent<MeshFilter>().sharedMesh = mesh;
            piece.AddComponent<MeshRenderer>().sharedMaterials = new[] { HeroesKitArt.Material("Nature", bark), grain };
            return g.Dress(piece);
        }

        /// <summary>
        /// Puts snow on a pine. Its needles are one flat green with nothing in the picture to paint snow on, so a copy of its
        /// mesh carries the snow in the colours of its vertices (white where the needles face the sky, more so toward the top,
        /// dark green beneath), and the needles are drawn with Heroes/Kit Lit over a white copy of their picture.
        /// </summary>
        private static void Snowcap(GameObject piece, string model)
        {
            MeshFilter filter = piece.GetComponentInChildren<MeshFilter>();
            MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (renderer == null)
            {
                return;
            }
            filter.sharedMesh = SnowMesh(model, filter.sharedMesh);
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && HeroesKitArt.BaseName(materials[i].name).StartsWith("Leaves_Pine", StringComparison.Ordinal))
                {
                    materials[i] = SnowNeedles();
                }
            }
            renderer.sharedMaterials = materials;
        }

        /// <summary>
        /// The pine's mesh with snow in the colours of its vertices: white where nothing of the tree is above a vertex (the
        /// tops of its tiers of needles, found by dropping a ray on the tree from above each one), dark green beneath.
        /// </summary>
        private static Mesh SnowMesh(string model, Mesh source)
        {
            string relative = $"{Folder}/Meshes/{System.IO.Path.GetFileName(model)}_Snow.asset";
            if (Meshes.TryGetValue(relative, out Mesh cached) && cached != null)
            {
                return cached;
            }
            Vector3[] vertices = source.vertices;
            Bounds bounds = source.bounds;
            // How flat the needles lie at each vertex, from the faces around it (the file's own normals are made for soft light).
            var flat = new float[vertices.Length];
            var faces = new int[vertices.Length];
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                int[] triangles = source.GetTriangles(sub);
                for (int t = 0; t + 2 < triangles.Length; t += 3)
                {
                    Vector3 a = vertices[triangles[t]];
                    Vector3 face = Vector3.Cross(vertices[triangles[t + 1]] - a, vertices[triangles[t + 2]] - a).normalized;
                    for (int k = 0; k < 3; k++)
                    {
                        flat[triangles[t + k]] += Mathf.Abs(face.y);
                        faces[triangles[t + k]]++;
                    }
                }
            }
            var probe = new GameObject("Snow Probe") { hideFlags = HideFlags.HideAndDontSave };
            var collider = probe.AddComponent<MeshCollider>();
            collider.sharedMesh = source;
            Physics.SyncTransforms();
            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            float depth = bounds.size.y * 0.02f;
            Color needles = new Color(0.1f, 0.3f, 0.17f).linear;
            Color snow = new Color(0.96f, 0.97f, 1f).linear;
            var colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                var ray = new Ray(new Vector3(v.x, bounds.max.y + 1f, v.z), Vector3.down);
                float above = collider.Raycast(ray, out RaycastHit hit, bounds.size.y + 2f) ? Mathf.Max(0f, hit.point.y - v.y) : 0f;
                float lying = faces[i] > 0 ? flat[i] / faces[i] : 0.5f;
                // A little uneven, as snow lies: a fixed jitter by the vertex's place.
                float jitter = Mathf.Repeat(Mathf.Sin(v.x * 12.9898f + v.z * 78.233f + v.y * 37.719f) * 43758.547f, 1f) - 0.5f;
                float cover = Mathf.Clamp01(1f - above / depth + jitter * 0.3f) * Mathf.SmoothStep(0.25f, 0.65f, lying);
                colors[i] = Color.Lerp(needles, snow, Mathf.SmoothStep(0.3f, 0.8f, cover));
            }
            Physics.queriesHitBackfaces = backfaces;
            Object.DestroyImmediate(probe);
            var mesh = new Mesh { name = System.IO.Path.GetFileName(relative), indexFormat = source.indexFormat };
            mesh.vertices = vertices;
            mesh.normals = source.normals;
            mesh.tangents = source.tangents;
            mesh.uv = source.uv;
            mesh.colors = colors;
            mesh.subMeshCount = source.subMeshCount;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                mesh.SetTriangles(source.GetTriangles(sub), sub);
            }
            mesh.RecalculateBounds();
            mesh = HeroesAssets.CreateOrReplace(mesh, relative);
            Meshes[relative] = mesh;
            return mesh;
        }

        private static Material snowNeedles;

        /// <summary>The needles of a pine under snow: the kit's needles in white, drawn with the colours of the vertices.</summary>
        private static Material SnowNeedles()
        {
            if (snowNeedles != null)
            {
                return snowNeedles;
            }
            Material white = HeroesKitArt.Repainted("Nature", "Leaves_Pine", -1, "Snowcap", pixel => new Color(0.96f, 0.97f, 0.98f, pixel.a));
            snowNeedles = HeroesAssets.Material($"{Folder}/Materials/SnowNeedles.mat", Shader.Find("Heroes/Kit Lit"), m =>
            {
                m.CopyPropertiesFromMaterial(white);
                m.shaderKeywords = white.shaderKeywords;
                m.renderQueue = white.renderQueue;
                m.SetOverrideTag("RenderType", white.GetTag("RenderType", false));
                m.doubleSidedGI = white.doubleSidedGI;
                m.enableInstancing = true;
            });
            return snowNeedles;
        }

        private static void Stump(Transform root, Ground g, int variant, Material grain)
        {
            bool dead = !g.Green && !g.Swamp;
            string bark = dead ? "Bark_DeadTree" : "Bark_NormalTree";
            if (variant == 0)
            {
                Mesh mesh = dead ? Trunk($"{Nature}/DeadTree_1", -1f, 0.95f, "StumpDead") : Trunk($"{Nature}/CommonTree_1", -1f, 1.1f, "StumpCommon");
                GameObject stump = Wood(root, g, mesh, bark, grain);
                stump.transform.localScale = new Vector3(1.45f, 0.8f, 1.45f);
                Settle(stump, At(0f, 0f, -0.05f));
                if (g.Green || g.Swamp)
                {
                    g.Kit(root, $"{Nature}/Mushroom_Laetiporus", At(0.3f, -0.18f, 0.12f), 210f, 0.22f);
                }
                Pebbles(root, g, -0.5f, -0.45f, 2, 9);
                Tuft(root, g, 0.45f, 0.4f);
            }
            else
            {
                GameObject stump = Wood(root, g, Trunk($"{Nature}/CommonTree_2", -1f, 0.8f, "StumpLow"), bark, grain);
                stump.transform.localScale = new Vector3(1.6f, 0.85f, 1.6f);
                stump.transform.localRotation = Quaternion.Euler(0f, 70f, 0f);
                Settle(stump, At(-0.1f, 0.05f, -0.05f));
                GameObject log = Wood(root, g, Trunk($"{Nature}/CommonTree_2", 1f, 2.2f, "LogShort"), bark, grain);
                log.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
                Settle(log, At(0.45f, -0.4f));
                Tuft(root, g, -0.5f, 0.45f, 0.9f);
                Pebbles(root, g, 0.55f, 0.45f, 1, 10);
            }
        }

        private static void Logs(Transform root, Ground g, int variant, Material grain)
        {
            bool dead = !g.Green && !g.Swamp;
            string bark = dead ? "Bark_DeadTree" : "Bark_NormalTree";
            Mesh mesh = Trunk($"{Nature}/CommonTree_1", 0.6f, 2.6f, "Log");
            if (variant == 0)
            {
                // A stack of three felled lengths, two on the ground and one in the saddle between them.
                float[,] lay = { { 0f, -0.22f, 0f }, { 0f, 0.22f, 0f }, { 0.05f, 0f, 0.3f } };
                for (int i = 0; i < 3; i++)
                {
                    GameObject log = Wood(root, g, mesh, bark, grain);
                    log.transform.localRotation = Quaternion.Euler(0f, 18f + i * 4f, 90f);
                    log.transform.localScale = Vector3.one * 0.85f;
                    Settle(log, At(lay[i, 0], lay[i, 1], lay[i, 2]));
                }
                Tuft(root, g, -0.6f, 0.5f);
            }
            else
            {
                GameObject log = Wood(root, g, mesh, bark, grain);
                log.transform.localRotation = Quaternion.Euler(0f, 120f, 90f);
                log.transform.localScale = Vector3.one * 0.95f;
                Settle(log, At(0f, 0.05f));
                GameObject stump = Wood(root, g, Trunk($"{Nature}/CommonTree_2", -1f, 0.8f, "StumpLow"), bark, grain);
                stump.transform.localScale = new Vector3(1.1f, 0.6f, 1.1f);
                Settle(stump, At(0.62f, -0.42f, -0.04f));
                Tuft(root, g, -0.55f, -0.4f);
            }
        }

        /// <summary>
        /// A length of a kit tree's trunk as a mesh of its own: its bark (the first submesh) between the heights
        /// <paramref name="from"/> and <paramref name="to"/> of the model as it is made, cut straight across at both and closed
        /// with the end grain (the second submesh), saved among the battlefield's meshes as <paramref name="name"/>. The kits
        /// have neither stumps nor felled logs; they are cut from the trees this way. A cut below the foot leaves the roots.
        /// </summary>
        private static Mesh Trunk(string model, float from, float to, string name)
        {
            string relative = $"{Folder}/Meshes/{name}.asset";
            if (Meshes.TryGetValue(relative, out Mesh cached) && cached != null)
            {
                return cached;
            }
            GameObject asset = HeroesArtBuilder.Model(model);
            MeshFilter filter = asset.GetComponentInChildren<MeshFilter>(true);
            Mesh source = filter.sharedMesh;
            Matrix4x4 place = asset.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Vector3[] positions = source.vertices;
            Vector3[] normals = source.normals;
            Vector2[] uvs = source.uv;
            int[] triangles = source.GetTriangles(0);

            var vertices = new List<Vector3>();
            var vertexNormals = new List<Vector3>();
            var vertexUvs = new List<Vector2>();
            var bark = new List<int>();
            var bottom = new List<(Vector3, Vector3)>();
            var top = new List<(Vector3, Vector3)>();
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                var polygon = new List<(Vector3 p, Vector3 n, Vector2 uv)>(4);
                for (int k = 0; k < 3; k++)
                {
                    int v = triangles[t + k];
                    polygon.Add((place.MultiplyPoint3x4(positions[v]), place.MultiplyVector(normals[v]).normalized, uvs[v]));
                }
                polygon = Clip(polygon, from, 1f, bottom);
                polygon = Clip(polygon, to, -1f, top);
                if (polygon.Count < 3)
                {
                    continue;
                }
                int first = vertices.Count;
                foreach ((Vector3 p, Vector3 n, Vector2 uv) corner in polygon)
                {
                    vertices.Add(corner.p);
                    vertexNormals.Add(corner.n);
                    vertexUvs.Add(corner.uv);
                }
                for (int k = 1; k + 1 < polygon.Count; k++)
                {
                    bark.Add(first);
                    bark.Add(first + k);
                    bark.Add(first + k + 1);
                }
            }
            var caps = new List<int>();
            Cap(bottom, Vector3.down, vertices, vertexNormals, vertexUvs, caps);
            Cap(top, Vector3.up, vertices, vertexNormals, vertexUvs, caps);
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(vertexNormals);
            mesh.SetUVs(0, vertexUvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(bark, 0);
            mesh.SetTriangles(caps, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh = HeroesAssets.CreateOrReplace(mesh, relative);
            Meshes[relative] = mesh;
            return mesh;
        }

        /// <summary>
        /// Clips a convex polygon to the side of the plane y = <paramref name="height"/> that <paramref name="keep"/> points to
        /// (+1 above, -1 below), carrying normals and texture coordinates along, and adds the edge the plane cuts across it
        /// to <paramref name="cut"/>.
        /// </summary>
        private static List<(Vector3 p, Vector3 n, Vector2 uv)> Clip(List<(Vector3 p, Vector3 n, Vector2 uv)> polygon, float height, float keep,
            List<(Vector3, Vector3)> cut)
        {
            var result = new List<(Vector3 p, Vector3 n, Vector2 uv)>(polygon.Count + 1);
            var crossings = new List<Vector3>(2);
            for (int i = 0; i < polygon.Count; i++)
            {
                (Vector3 p, Vector3 n, Vector2 uv) a = polygon[i];
                (Vector3 p, Vector3 n, Vector2 uv) b = polygon[(i + 1) % polygon.Count];
                float da = (a.p.y - height) * keep;
                float db = (b.p.y - height) * keep;
                if (da >= 0f)
                {
                    result.Add(a);
                }
                if (da >= 0f != db >= 0f)
                {
                    float t = da / (da - db);
                    Vector3 p = Vector3.Lerp(a.p, b.p, t);
                    p.y = height;
                    result.Add((p, Vector3.Lerp(a.n, b.n, t).normalized, Vector2.Lerp(a.uv, b.uv, t)));
                    crossings.Add(p);
                }
            }
            if (crossings.Count == 2 && (crossings[0] - crossings[1]).sqrMagnitude > 1e-10f)
            {
                cut.Add((crossings[0], crossings[1]));
            }
            return result;
        }

        /// <summary>
        /// Closes a cut with the end grain: a fan from the middle of the cut edges to each of them, facing
        /// <paramref name="outward"/>, its texture laid flat across it from the middle (the heart) to the bark (the rim).
        /// </summary>
        private static void Cap(List<(Vector3 a, Vector3 b)> edges, Vector3 outward, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
            List<int> triangles)
        {
            if (edges.Count < 3)
            {
                return;
            }
            Vector3 middle = Vector3.zero;
            foreach ((Vector3 a, Vector3 b) in edges)
            {
                middle += a + b;
            }
            middle /= edges.Count * 2f;
            float radius = 0.001f;
            foreach ((Vector3 a, Vector3 b) in edges)
            {
                radius = Mathf.Max(radius, Mathf.Max(Vector2.Distance(new Vector2(a.x, a.z), new Vector2(middle.x, middle.z)),
                    Vector2.Distance(new Vector2(b.x, b.z), new Vector2(middle.x, middle.z))));
            }
            Vector2 Flat(Vector3 p) => new Vector2(0.5f + (p.x - middle.x) / (2f * radius), 0.5f + (p.z - middle.z) / (2f * radius));
            foreach ((Vector3 a, Vector3 b) in edges)
            {
                Vector3 p = a;
                Vector3 q = b;
                if (Vector3.Dot(Vector3.Cross(p - middle, q - middle), outward) < 0f)
                {
                    (p, q) = (q, p);
                }
                int first = vertices.Count;
                vertices.Add(middle);
                vertices.Add(p);
                vertices.Add(q);
                for (int k = 0; k < 3; k++)
                {
                    normals.Add(outward);
                }
                uvs.Add(Flat(middle));
                uvs.Add(Flat(p));
                uvs.Add(Flat(q));
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
            }
        }

        /// <summary>The end grain of a cut trunk: pale wood in rings, darker toward the bark at its rim, a crack from the heart.</summary>
        private static Material EndGrain()
        {
            const int size = 256;
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size * 0.52f, size * 0.47f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float r = p.magnitude / (size * 0.5f) * (1f + 0.04f * Mathf.Sin(angle * 5f) + 0.025f * Mathf.Sin(angle * 11f + 0.7f));
                    float rings = 0.5f + 0.5f * Mathf.Sin(r * 58f + Mathf.Sin(angle * 3f) * 0.6f);
                    Color wood = Color.Lerp(new Color(0.86f, 0.7f, 0.48f), new Color(0.66f, 0.48f, 0.3f), rings * 0.55f + r * 0.25f);
                    Color color = Color.Lerp(wood, new Color(0.32f, 0.22f, 0.15f), Mathf.SmoothStep(0.86f, 0.97f, r));
                    float crack = Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 35f)) < 2.2f * (1f - r) + 0.4f && r < 0.8f ? 0.55f : 1f;
                    raster.Set(x, y, new Color(color.r * crack, color.g * crack, color.b * crack, 1f));
                }
            }
            Texture2D texture = HeroesAssets.SaveTexture(raster.ToTexture(), $"{Folder}/Textures/EndGrain.png", importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Compressed;
            });
            return HeroesAssets.Material($"{Folder}/Materials/EndGrain.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Smoothness", 0.15f);
                m.SetFloat("_Metallic", 0f);
            });
        }

        // ------------------------------------------------------------------ bones, crystals, mounds, pools, rubble, craters

        private static void Bones(Transform root, Ground g, int variant)
        {
            if (variant == 0)
            {
                // Where a warrior fell: his ribs on their side, his skull, his sword still in the ground.
                g.Pack(root, "KayKit/Halloween/ribcage", At(-0.05f, 0.1f, -0.06f), 30f, 0.72f, new Vector2(0f, 90f));
                g.Pack(root, "KayKit/Halloween/skull", At(0.58f, -0.3f), 200f, 0.4f);
                g.Pack(root, "KayKit/Halloween/bone_B", At(-0.5f, -0.42f), 70f, 0.1f);
                g.Pack(root, "KayKit/Halloween/bone_A", At(0.2f, -0.62f), 150f, 0.09f);
                Put(root, $"{Props}/Sword_Bronze", At(-0.45f, 0.5f, -0.2f), 60f, 1.15f, 1f, new Vector2(14f, 10f));
                Tuft(root, g, 0.5f, 0.45f);
                Pebbles(root, g, 0.62f, 0.05f, 2, 11);
            }
            else
            {
                g.Pack(root, "KayKit/Halloween/skull", At(0.05f, -0.05f), 160f, 0.46f);
                g.Pack(root, "KayKit/Halloween/bone_A", At(0.42f, 0.35f), 20f, 0.11f);
                g.Pack(root, "KayKit/Halloween/bone_C", At(-0.48f, -0.25f), 110f, 0.11f);
                Put(root, $"{Props}/Shield_Wooden", At(-0.35f, 0.42f), 200f, 0f, 1.4f, new Vector2(-78f, 0f));
                Put(root, $"{Props}/Axe_Bronze", At(0.5f, -0.4f, -0.12f), 230f, 0.8f, 1f, new Vector2(-20f, 12f));
                g.Kit(root, $"{Nature}/Rock_Medium_2", At(0.62f, 0.1f), 50f, 0.3f);
                Tuft(root, g, -0.6f, 0.05f);
            }
        }

        /// <summary>
        /// A cluster of glowing crystals growing out of the ground: a tall one in the middle and smaller ones leaning out
        /// around it, each sunk by a third (they are cut at both ends), at the foot of a stone; amethyst, or the red of the
        /// crystal the mines dig.
        /// </summary>
        private static void Crystal(Transform root, Ground g, int variant)
        {
            Color color = variant == 0 ? Amethyst : Ruby;
            string tag = variant == 0 ? "Amethyst" : "Ruby";
            string[] shapes = { "Crystal1", "Crystal2", "Crystal3", "Crystal1", "Crystal2", "Crystal3" };
            float[] heights = { 1.45f, 0.95f, 0.8f, 0.7f, 0.55f, 0.45f };
            float[] leans = { 5f, 22f, 28f, 26f, 34f, 40f };
            for (int i = 0; i < shapes.Length; i++)
            {
                float around = i == 0 ? 0f : i * 1.1f + variant * 0.7f;
                float away = i == 0 ? 0f : 0.36f + 0.1f * (i % 2);
                // Slender: drawn upright, narrowed, then tipped forward and turned to lean away from the middle.
                GameObject shard = Put(root, $"Quaternius/Items/{shapes[(i + variant) % shapes.Length]}", Vector3.zero, 0f, heights[i], 1f, default, color);
                if (shard == null)
                {
                    continue;
                }
                Resize(shard, new Vector3(heights[i] * 0.3f, heights[i], heights[i] * 0.3f));
                float facing = 90f - around * Mathf.Rad2Deg;
                shard.transform.rotation = Quaternion.Euler(0f, facing, 0f) * Quaternion.Euler(leans[i], 0f, 0f) * shard.transform.rotation;
                Settle(shard, At(Mathf.Cos(around) * away, Mathf.Sin(around) * away, -heights[i] * 0.28f));
                Glow(shard, color * 0.45f, tag);
            }
            g.Kit(root, $"{Nature}/Rock_Medium_2", At(variant == 0 ? 0.42f : -0.45f, variant == 0 ? 0.5f : 0.45f), 70f, 0.4f);
            Pebbles(root, g, variant == 0 ? -0.55f : 0.5f, variant == 0 ? -0.35f : -0.45f, 3, 12 + variant);
        }

        /// <summary>Gives every material of a piece its glowing copy (the crystals light themselves up), polished.</summary>
        private static void Glow(GameObject piece, Color emission, string tag)
        {
            if (piece == null)
            {
                return;
            }
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = HeroesKitArt.Glowing(materials[i], emission, tag);
                    if (materials[i] != null && materials[i].GetFloat("_Smoothness") < 0.8f)
                    {
                        materials[i].SetFloat("_Smoothness", 0.85f);
                        materials[i].SetFloat("_Metallic", 0.15f);
                        EditorUtility.SetDirty(materials[i]);
                    }
                }
                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>By the name of a group of grounds: the terrain texture its mounds of earth are covered with.</summary>
        private static readonly Dictionary<string, string> EarthOf = new Dictionary<string, string>
        {
            { "Green", "Grass" }, { "Snow", "Snow" }, { "Sand", "Sand" }, { "Swamp", "Swamp" }, { "Waste", "Wasteland" }, { "Deep", "Rock" }
        };

        /// <summary>The ground's own earth (the theme's picture of its terrain), for the mounds.</summary>
        private static Material Earth(HeroesArt art, Ground g)
        {
            string texture = EarthOf.TryGetValue(g.Name, out string name) ? name : "Grass";
            TerrainLayer layer = art.layers != null ? art.layers.FirstOrDefault(candidate => candidate != null && candidate.name == texture) : null;
            Texture2D diffuse = layer != null && layer.diffuseTexture != null ? layer.diffuseTexture : HeroesAssets.Load<Texture2D>($"Art/Terrain/{texture}.jpg");
            var normal = HeroesAssets.Load<Texture2D>($"Art/Terrain/{texture}_Normal.jpg");
            // The picture as big on the dome as on the ground around it (once round a dome is about 5.4 m, over it 2.7 m),
            // and a little darker, the dome facing the sun more than the flat ground does.
            float tile = layer != null && layer.tileSize.x > 0.1f ? layer.tileSize.x : 4f;
            var tiling = new Vector2(5.4f / tile, 2.7f / tile);
            return HeroesAssets.Material($"{Folder}/Materials/Earth{g.Name}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetTexture("_BaseMap", diffuse);
                m.SetTextureScale("_BaseMap", tiling);
                m.SetColor("_BaseColor", g.Deep ? new Color(0.62f, 0.62f, 0.66f) : new Color(0.84f, 0.84f, 0.84f));
                if (normal != null)
                {
                    m.SetTexture("_BumpMap", normal);
                    m.SetTextureScale("_BumpMap", tiling);
                    m.SetFloat("_BumpScale", 0.8f);
                    m.EnableKeyword("_NORMALMAP");
                }
                m.SetFloat("_Smoothness", 0f);
                m.SetFloat("_Metallic", 0f);
            });
        }

        /// <summary>
        /// A mound: a low dome of the ground's own earth, half sunk, a stone or two bedded in it and what grows on the ground
        /// tufted all round its foot and over it.
        /// </summary>
        private static void Mound(Transform root, Ground g, int variant, Material earth)
        {
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(dome.GetComponent<Collider>());
            dome.name = "Earth";
            dome.transform.SetParent(root, false);
            dome.transform.localRotation = Quaternion.Euler(0f, variant * 50f, 0f);
            dome.transform.localScale = new Vector3(Cell * (variant == 0 ? 0.88f : 0.8f), variant == 0 ? 0.62f : 0.8f, Cell * (variant == 0 ? 0.76f : 0.8f));
            dome.GetComponent<MeshRenderer>().sharedMaterial = earth;
            float Over(float x, float z)
            {
                Vector3 local = dome.transform.InverseTransformPoint(new Vector3(x, 0f, z));
                float inside = 1f - 4f * (local.x * local.x + local.z * local.z);
                return inside > 0f ? dome.transform.TransformPoint(new Vector3(0f, 0.5f * Mathf.Sqrt(inside), 0f)).y : 0f;
            }
            if (variant == 0)
            {
                g.Kit(root, $"{Nature}/Rock_Medium_2", At(0.25f, -0.12f, Over(0.25f, -0.12f) - 0.12f), 30f, 0.42f);
                Pebbles(root, g, -0.3f, 0.12f, 2, 14);
            }
            else
            {
                g.Kit(root, $"{Nature}/Rock_Medium_1", At(-0.2f, 0.12f, Over(-0.2f, 0.12f) - 0.14f), 120f, 0.38f);
                g.Kit(root, $"{Nature}/Rock_Medium_3", At(0.3f, -0.25f, Over(0.3f, -0.25f) - 0.1f), 260f, 0.26f);
            }
            // Grass all round the foot, and a little on top.
            const int tufts = 7;
            for (int i = 0; i < tufts; i++)
            {
                float around = (i + 0.4f * variant) / tufts * Mathf.PI * 2f;
                Tuft(root, g, Mathf.Cos(around) * Cell * 0.4f, Mathf.Sin(around) * Cell * 0.36f, 0.8f + 0.1f * (i % 3));
            }
            if (g.Green)
            {
                g.Kit(root, $"{Nature}/Flower_3_Group", At(-0.25f, -0.3f, Over(-0.25f, -0.3f) - 0.05f), 40f, 0.4f);
                g.Kit(root, $"{Nature}/Clover_1", At(0.15f, 0.35f, Over(0.15f, 0.35f) - 0.03f), 0f, 0.3f);
            }
        }

        /// <summary>A pond: a flat disc of water just above the ground, ringed with stones, reeds at one end, leaves floating.</summary>
        private static void Pool(Transform root, Ground g, int variant, Material water)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.name = "Water";
            disc.transform.SetParent(root, false);
            disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            disc.transform.localRotation = Quaternion.Euler(0f, variant * 35f, 0f);
            float wide = Cell * (variant == 0 ? 0.8f : 0.72f);
            float deep = Cell * (variant == 0 ? 0.62f : 0.74f);
            disc.transform.localScale = new Vector3(wide, 0.02f, deep);
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = water;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Stones around the rim.
            int stones = 7 + variant;
            for (int s = 0; s < stones; s++)
            {
                float angle = (s + 0.3f * variant) / stones * Mathf.PI * 2f + variant * 0.6f;
                var rim = new Vector3(Mathf.Cos(angle) * wide * 0.5f, 0f, Mathf.Sin(angle) * deep * 0.5f);
                if (s % 3 == 0)
                {
                    g.Kit(root, $"{Nature}/Rock_Medium_{1 + s % 3}", At(rim.x * 1.02f, rim.z * 1.02f), s * 53f, 0.24f + 0.04f * (s % 2));
                }
                else
                {
                    g.Kit(root, $"{Nature}/Pebble_Round_{1 + s % 5}", At(rim.x, rim.z), s * 41f, 0.13f);
                }
            }
            if (!g.Snow && !g.Deep)
            {
                // Reeds at one end of the pond.
                float side = variant == 0 ? -1f : 1f;
                g.Kit(root, $"{Nature}/{(g.Sand || g.Waste ? "Grass_Wispy_Tall" : "Grass_Common_Tall")}", At(side * wide * 0.42f, deep * 0.3f), 30f, 0.75f);
                g.Kit(root, $"{Nature}/Grass_Wispy_Tall", At(side * wide * 0.3f, deep * 0.45f), 100f, 0.6f);
            }
            if (g.Green || g.Swamp)
            {
                // Pads floating on the water: the purple leaves of the kit's little plant turned green.
                Vector3 at = At(variant == 0 ? 0.2f : -0.15f, variant == 0 ? -0.05f : 0.15f, 0.025f);
                GameObject pads = Put(root, $"{Nature}/Plant_7", at, 40f);
                if (pads != null)
                {
                    HeroesKitArt.Swap(pads, "Leaves", HeroesKitArt.Repainted("Nature", "Leaves", -1, "Pads", HeroesKitArt.Hue(0.55f, 0.95f, 0.25f, new Color(0.26f, 0.5f, 0.2f))));
                    Bounds flat = HeroesArtBuilder.BoundsOf(pads);
                    pads.transform.localScale *= 0.75f / Mathf.Max(0.01f, Mathf.Max(flat.size.x, flat.size.z));
                    Settle(pads, at);
                }
            }
        }

        /// <summary>The ruin of a wall or a house: a broken stub of its uneven brick, its stones tumbled about, shards and grit.</summary>
        private static void Rubble(Transform root, Ground g, int variant, Material grain)
        {
            if (variant == 0)
            {
                Stub(root, g, At(-0.15f, 0.25f), 20f, 1.25f, new[] { 0.95f, 0.6f });
                Rubbish(root, g, At(0.1f, -0.2f), 1.3f);
                Stones(root, g, new[] { (0.55f, -0.35f, 20f, 1.7f, 0f), (-0.6f, -0.4f, 75f, 1.5f, 18f), (0.7f, 0.3f, 130f, 1.4f, -14f), (0.05f, -0.65f, 10f, 1.3f, 0f) });
                Pebbles(root, g, -0.35f, -0.65f, 2, 15);
            }
            else
            {
                Rubbish(root, g, At(0f, 0f), 1.7f);
                Stub(root, g, At(0.25f, 0.35f), 290f, 0.9f, new[] { 0.55f, 0.35f });
                // A charred beam of the roof that fell in.
                GameObject beam = Wood(root, g, Trunk($"{Nature}/CommonTree_2", 1f, 2.2f, "LogShort"), "Bark_DeadTree", grain);
                beam.transform.localScale = new Vector3(0.55f, 1.25f, 0.55f);
                beam.transform.localRotation = Quaternion.Euler(0f, 125f, 78f);
                Settle(beam, At(-0.2f, -0.1f, -0.04f));
                Stones(root, g, new[] { (-0.6f, 0.35f, 60f, 1.6f, 12f), (0.55f, -0.45f, 0f, 1.5f, 0f), (-0.55f, -0.45f, 100f, 1.4f, -10f) });
            }
        }

        /// <summary>
        /// A broken stub of the village's uneven brick wall standing on the ground at <paramref name="at"/>, turned
        /// <paramref name="yaw"/>, <paramref name="length"/> long and stepping down through <paramref name="heights"/> along it.
        /// </summary>
        private static void Stub(Transform root, Ground g, Vector3 at, float yaw, float length, float[] heights)
        {
            var stub = new GameObject("Stub");
            stub.transform.SetParent(root, false);
            float each = length / heights.Length;
            for (int i = 0; i < heights.Length; i++)
            {
                float x = -length * 0.5f + each * (i + 0.5f);
                foreach (float side in new[] { 1f, -1f })
                {
                    GameObject panel = Panel(stub.transform, Brick, At(x, side * 0.09f, -0.06f), side > 0f ? 0f : 180f, each + 0.01f, heights[i], 0.2f);
                    // Broken off, it has lost the timber along its top: stone all through.
                    HeroesKitArt.Swap(panel, "MI_WoodTrim", HeroesKitArt.Material("Village", "MI_UnevenBrick"));
                    g.Dress(panel);
                }
            }
            // Turned and set down as a whole, then its parts are moved up to the prefab's root (the fitting scales those).
            stub.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            stub.transform.localPosition = at;
            var parts = new List<Transform>();
            foreach (Transform part in stub.transform)
            {
                parts.Add(part);
            }
            foreach (Transform part in parts)
            {
                part.SetParent(root, true);
            }
            Object.DestroyImmediate(stub);
        }

        /// <summary>Blocks of the village's stone lying about: (x, z, yaw, size, tilt) each.</summary>
        private static void Stones(Transform root, Ground g, (float x, float z, float yaw, float size, float tilt)[] blocks)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                (float x, float z, float yaw, float size, float tilt) = blocks[i];
                // The rock trim's cream greyed, as stone that has lain in the weather.
                g.Dress(Put(root, $"{Village}/Prop_Brick{1 + i % 4}", At(x, z), yaw, 0f, size, new Vector2(0f, tilt), Weathered));
            }
        }

        /// <summary>A litter of broken stone: the props kit's shards of a vase, in the path stones of the ground.</summary>
        private static void Rubbish(Transform root, Ground g, Vector3 at, float wide)
        {
            GameObject shards = Put(root, $"{Props}/Vase_Rubble_Medium", at, 25f);
            if (shards == null)
            {
                return;
            }
            Bounds bounds = HeroesArtBuilder.BoundsOf(shards);
            shards.transform.localScale *= wide / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z));
            HeroesKitArt.Swap(shards, "MI_Trim_Props_Vertex", HeroesKitArt.Material("Nature", "PathRocks"));
            g.Dress(shards);
            Settle(shards, at);
        }

        /// <summary>A crater: a scorched hollow in the ground ringed by the stones it threw up.</summary>
        private static void Crater(Transform root, Ground g, int variant, Material scorch)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.name = "Scorch";
            disc.transform.SetParent(root, false);
            disc.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            disc.transform.localRotation = Quaternion.Euler(90f, variant * 70f, 0f);
            disc.transform.localScale = new Vector3(Cell * 0.95f, Cell * 0.95f, 1f);
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = scorch;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            int stones = 8 + variant;
            for (int s = 0; s < stones; s++)
            {
                float angle = (s + 0.37f * variant) / stones * Mathf.PI * 2f;
                float radius = Cell * 0.36f + (s % 3) * 0.05f;
                Vector3 at = At(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                switch (s % 3)
                {
                    case 0:
                        g.Kit(root, $"{Nature}/Rock_Medium_{1 + s % 3}", at, s * 53f, 0.26f, 1f, new Vector2(0f, 20f));
                        break;
                    case 1:
                        g.Kit(root, $"{Nature}/Pebble_Square_{1 + s % 6}", at, s * 37f, 0.16f, 1f, new Vector2(25f, 0f));
                        break;
                    default:
                        g.Kit(root, $"{Nature}/Pebble_Round_{1 + s % 5}", at, s * 29f, 0.14f);
                        break;
                }
            }
        }

        /// <summary>A transparent surface of water (or ice) in a colour.</summary>
        private static Material Water(string name, Color color, float smoothness, float metallic)
        {
            return HeroesAssets.Material($"{Folder}/Materials/{name}.mat", Shader.Find("Universal Render Pipeline/Lit"), m =>
            {
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Metallic", metallic);
                Transparent(m);
            });
        }

        /// <summary>The scorch of a crater: dark ash in the middle, fading out to nothing at its rim, a little ragged.</summary>
        private static Material Scorch()
        {
            const int size = 256;
            var raster = new Raster(size, size, Color.clear);
            var center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float angle = Mathf.Atan2(p.y, p.x);
                    float ragged = 1f + 0.08f * Mathf.Sin(angle * 7f) + 0.05f * Mathf.Sin(angle * 13f + 1.3f);
                    float r = p.magnitude / (size * 0.5f * ragged);
                    float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.45f, 1f, r));
                    float ember = Mathf.Clamp01(1f - r / 0.35f);
                    Color color = Color.Lerp(new Color(0.09f, 0.07f, 0.06f), new Color(0.05f, 0.035f, 0.03f), ember);
                    raster.Set(x, y, new Color(color.r, color.g, color.b, a * 0.9f));
                }
            }
            Texture2D texture = HeroesAssets.SaveTexture(raster.ToTexture(), $"{Folder}/Textures/Scorch.png", importer =>
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            });
            return HeroesAssets.Material($"{Folder}/Materials/Scorch.mat", Shader.Find("Universal Render Pipeline/Unlit"), m =>
            {
                m.SetTexture("_BaseMap", texture);
                m.SetColor("_BaseColor", Color.white);
                Transparent(m);
            });
        }

        private static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
        }

        // ------------------------------------------------------------------ the town's wall

        /// <summary>A length of the wall runs across a cell, and a little over (the field stands its lengths end to end).</summary>
        private const float WallLength = Cell + 0.1f;

        /// <summary>
        /// How tall and how thick a length of wall is made: the field stands it twice as tall and 1.3 times as thick
        /// (<see cref="BattlefieldScene.WallHeight"/>), so its stones are drawn half as tall here as they should look.
        /// </summary>
        private const float WallBody = 1.1f;
        private const float WallHalf = 0.5f;

        private const string Brick = Village + "/Wall_UnevenBrick_Straight";

        /// <summary>
        /// The wall of a besieged town, the same on every ground: two lengths of the village's uneven brick back to back,
        /// brick out on both faces, a walkway of the same stone on top edged and crenellated with rock trim, running across a
        /// cell from side to side along x.
        /// </summary>
        private static void Walls(HeroesArt art)
        {
            var wall = new List<GameObject>();
            for (int variant = 0; variant < 2; variant++)
            {
                GameObject root = HeroesArtBuilder.Root($"Wall{variant}");
                Transform t = root.transform;
                for (int side = -1; side <= 1; side += 2)
                {
                    float facing = side > 0 ? 0f : 180f;
                    for (int half = 0; half < 2; half++)
                    {
                        float x = (half - 0.5f) * WallLength * 0.5f;
                        Panel(t, Brick, At(x, side * WallHalf * 0.5f), facing, WallLength * 0.5f + 0.005f, WallBody, WallHalf);
                    }
                    // The merlons: three along each edge, or two and a half at either end on the second look.
                    int count = variant == 0 ? 3 : 4;
                    for (int m = 0; m < count; m++)
                    {
                        float x = variant == 0 ? (m - 1) * WallLength / 3f : (m - 1.5f) * WallLength / 3f;
                        float width = variant == 1 && (m == 0 || m == 3) ? 0.22f : 0.42f;
                        x = Mathf.Clamp(x, -WallLength * 0.5f + width * 0.5f, WallLength * 0.5f - width * 0.5f);
                        Block(t, $"{Village}/Prop_Brick{1 + (m + variant) % 4}", At(x, side * (WallHalf - 0.13f), WallBody - 0.02f), 0f,
                            new Vector3(width, 0.24f, 0.26f));
                    }
                    Block(t, $"{Village}/Prop_ExteriorBorder_Straight1", At(0f, side * (WallHalf - 0.1f), WallBody - 0.04f), facing,
                        new Vector3(WallLength, 0.08f, 0.22f));
                }
                Block(t, $"{Village}/Floor_UnevenBrick", At(0f, 0f, WallBody - 0.03f), 0f, new Vector3(WallLength, 0.03f, WallHalf * 2f - 0.1f));
                wall.Add(HeroesArtBuilder.Save(root, $"{Folder}/Obstacles/Wall{variant}.prefab"));
            }
            art.obstacles.Add(new HeroesArt.ObstacleArt { kind = BattleObstacle.Wall, terrain = -1, variants = wall.Where(p => p != null).ToArray() });
        }

        // ------------------------------------------------------------------ the siege

        /// <summary>The props of a siege, saved in <paramref name="folder"/>.</summary>
        public static void SiegeArt(HeroesArt art, string folder)
        {
            art.siegeGate = Gate(folder);
            art.siegeRuin = Ruin(folder);
            art.siegeStones = Stones(folder);
            art.siegeBanners = Banners(folder);
        }

        /// <summary>The front of the gatehouse (toward the besiegers, +z) and its back, from the middle of the wall, and its heights.</summary>
        private const float GateFront = 1.25f;
        private const float GateBack = -0.85f;
        private const float GateTall = 3.1f;
        private const float TowerTall = 3.75f;

        /// <summary>
        /// The gatehouse in the wall of a besieged town, at the size it stands on the field: an arched gateway of the
        /// village's uneven brick, its doors shut in a frame of rock trim, between two squat towers with arrow slits, merlons
        /// of rock trim along their tops; and before it the drawbridge down on the ground. It runs along x, centred on its
        /// origin (the middle of the wall), and juts out well before the wall toward the besiegers (+z).
        /// </summary>
        private static GameObject Gate(string folder)
        {
            GameObject root = HeroesArtBuilder.Root("Gate");
            Transform t = root.transform;
            const float bay = 0.95f;
            const float tower = 0.75f;
            // The gateway: front and back with the arch, its doors in the front one.
            Panel(t, $"{Village}/Wall_UnevenBrick_Door_Round", At(0f, GateFront - 0.15f), 0f, bay * 2f, GateTall, 0.3f);
            Panel(t, $"{Village}/Wall_UnevenBrick_Door_Round", At(0f, GateBack + 0.15f), 180f, bay * 2f, GateTall, 0.3f);
            GameObject frame = Put(t, $"{Village}/DoorFrame_Round_Brick", At(0f, GateFront), 0f);
            if (frame != null)
            {
                Resize(frame, new Vector3(1.6f * bay, 2.58f * GateTall / 3.12f, 0.36f));
                Settle(frame, At(0f, GateFront - 0.05f));
            }
            GameObject door = Put(t, $"{Village}/Door_8_Round", At(0f, GateFront - 0.2f), 0f);
            if (door != null)
            {
                Resize(door, new Vector3(1.12f * bay, 2.33f * GateTall / 3.12f, 0.16f));
                Settle(door, At(0f, GateFront - 0.2f, 0.03f));
            }
            Block(t, $"{Village}/Floor_UnevenBrick", At(0f, (GateFront + GateBack) * 0.5f, GateTall - 0.03f), 0f, new Vector3(bay * 2f, 0.03f, GateFront - GateBack - 0.1f));
            for (int m = 0; m < 3; m++)
            {
                float x = (m - 1) * 0.62f;
                Block(t, $"{Village}/Prop_Brick{1 + m}", At(x, GateFront - 0.14f, GateTall - 0.02f), 0f, new Vector3(0.4f, 0.32f, 0.28f));
                Block(t, $"{Village}/Prop_Brick{2 + m % 3}", At(x, GateBack + 0.14f, GateTall - 0.02f), 0f, new Vector3(0.4f, 0.32f, 0.28f));
            }
            // The towers either side, standing out a little further.
            for (int side = -1; side <= 1; side += 2)
            {
                float middle = side * (bay + tower * 0.5f);
                float front = GateFront + 0.2f;
                float back = GateBack - 0.1f;
                float deep = front - back;
                Panel(t, $"{Village}/Wall_UnevenBrick_Window_Thin_Round", At(middle, front - 0.15f), 0f, tower, TowerTall, 0.3f);
                Panel(t, Brick, At(middle, back + 0.15f), 180f, tower, TowerTall, 0.3f);
                Panel(t, Brick, At(middle + side * (tower * 0.5f - 0.15f), (front + back) * 0.5f), side > 0 ? 90f : 270f, deep, TowerTall, 0.3f);
                Panel(t, Brick, At(middle - side * (tower * 0.5f - 0.15f), (front + back) * 0.5f), side > 0 ? 270f : 90f, deep, TowerTall, 0.3f);
                Block(t, $"{Village}/Floor_UnevenBrick", At(middle, (front + back) * 0.5f, TowerTall - 0.03f), 0f, new Vector3(tower, 0.03f, deep));
                // Quoins of rock trim up the outer front corner.
                GameObject quoin = Put(t, $"{Village}/Corner_Exterior_Brick", At(0f, 0f), side > 0 ? 0f : 270f);
                if (quoin != null)
                {
                    Resize(quoin, new Vector3(0f, TowerTall, 0f));
                    Bounds b = HeroesArtBuilder.BoundsOf(quoin);
                    Settle(quoin, At(middle + side * (tower * 0.5f - b.extents.x + 0.06f), front - b.extents.z + 0.06f));
                }
                foreach ((float x, float z) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                {
                    Block(t, $"{Village}/Prop_Brick{(x > 0 ? 1 : 3) + (z > 0 ? 0 : 1)}",
                        At(middle + x * (tower * 0.5f - 0.13f), (front + back) * 0.5f + z * (deep * 0.5f - 0.13f), TowerTall - 0.02f), 0f,
                        new Vector3(0.28f, 0.34f, 0.28f));
                }
                Block(t, $"{Village}/Prop_Brick4", At(middle + side * (tower * 0.5f - 0.13f), (front + back) * 0.5f, TowerTall - 0.02f), 0f,
                    new Vector3(0.28f, 0.34f, 0.32f));
            }
            // The drawbridge, down across the ditch.
            GameObject bridge = Block(t, $"{Village}/Floor_WoodDark", At(0f, GateFront + 1f, 0.02f), 0f, new Vector3(1.3f, 0.09f, 1.95f));
            if (bridge != null)
            {
                bridge.name = "Drawbridge";
            }
            return HeroesArtBuilder.Save(root, folder + "/Gate.prefab");
        }

        /// <summary>
        /// A heap of the wall's stones, about a cell across and knee high, standing on the ground at its origin: where an
        /// arrow tower fell, and at the broken ends of the wall beside a breach (smaller there). A broken length of the wall
        /// leans out of it, the blocks of its merlons tumbled about, over a litter of shards.
        /// </summary>
        private static GameObject Ruin(string folder)
        {
            GameObject root = HeroesArtBuilder.Root("Ruin");
            Transform t = root.transform;
            var stone = new Ground("Ruin", new TerrainType[0], Paint.None);
            Rubbish(t, stone, At(0.05f, 0f), 2.1f);
            GameObject slab = Put(t, Brick, At(0f, 0f), 0f);
            if (slab != null)
            {
                Resize(slab, new Vector3(1.15f, 1f, 0.42f));
                slab.transform.localRotation = Quaternion.Euler(0f, 65f, 0f) * Quaternion.Euler(52f, 0f, 0f) * slab.transform.localRotation;
                Settle(slab, At(0.15f, 0.2f, -0.1f));
            }
            Stones(t, stone, new[] { (0.7f, -0.35f, 30f, 2.6f, 0f), (-0.75f, 0.3f, 75f, 2.4f, 18f), (0.05f, -0.8f, 10f, 2.2f, 0f), (-0.2f, 0.75f, 115f, 2.5f, 18f),
                (0.85f, 0.45f, 160f, 2f, 0f), (-0.55f, -0.55f, 220f, 1.8f, -12f), (0.35f, 0.15f, 300f, 2.2f, 24f) });
            Pebbles(t, stone, -0.2f, -0.2f, 3, 16, 1.4f);
            return HeroesArtBuilder.Save(root, folder + "/Ruin.prefab");
        }

        /// <summary>A few loose stones of the wall lying on the ground, about a pace across, for the ground of a breach.</summary>
        private static GameObject Stones(string folder)
        {
            GameObject root = HeroesArtBuilder.Root("Stones");
            Transform t = root.transform;
            var stone = new Ground("Stones", new TerrainType[0], Paint.None);
            Stones(t, stone, new[] { (0.2f, -0.15f, 25f, 2f, 0f), (-0.3f, 0.2f, 80f, 1.7f, 14f), (0.05f, 0.4f, 140f, 1.4f, 0f) });
            Pebbles(t, stone, -0.3f, -0.3f, 2, 17);
            return HeroesArtBuilder.Save(root, folder + "/Stones.prefab");
        }

        /// <summary>
        /// The banners a town's gatehouse hangs out, by the color of its owner (the theme's player colours, white for
        /// nobody): the props kit's cloth on its rod, dyed, standing on its lower edge, the cloth across x.
        /// </summary>
        private static GameObject[] Banners(string folder)
        {
            var banners = new GameObject[5];
            for (int i = 0; i < banners.Length; i++)
            {
                GameObject root = HeroesArtBuilder.Root($"Banner{i}");
                GameObject cloth = Put(root.transform, $"{Props}/Banner_1_Cloth", At(0f, 0f), 0f, 1.5f);
                Color dye = i < 4 && Spec.Palette?.players != null && i < Spec.Palette.players.Length ? Spec.Palette.players[i] : new Color(0.92f, 0.91f, 0.88f);
                HeroesKitArt.Swap(cloth, "MI_Banner", HeroesKitArt.Plain("Props", "MI_Banner", dye));
                banners[i] = HeroesArtBuilder.Save(root, $"{folder}/Banner{i}.prefab");
            }
            return banners;
        }

        // ------------------------------------------------------------------ the ring around a field

        /// <summary>
        /// What a backdrop is made of: crags of the kit's rocks scaled up into cliffs (the tallest <see cref="Height"/>),
        /// boulders about their foot, the trees grown over and around them, and on the ground under the earth crystals.
        /// </summary>
        private sealed class Backdrop
        {
            public float Height;
            public int Rocks;
            public float Width;
            public string[] Trees = new string[0];
            public int Count;
            public Vector2 TreeHeight = new Vector2(5f, 7.5f);
            public int Crystals;
            /// <summary>Whether the twisted trees among them wear their autumn red.</summary>
            public bool Autumn;
        }

        /// <summary>A mountain: a great crag with lesser ones leaning on it, grown over with trees.</summary>
        private static Backdrop Massif(float height, string[] trees, int count, int crystals = 0)
        {
            return new Backdrop { Height = height * 1.2f, Rocks = 4, Width = height, Trees = trees, Count = count, Crystals = crystals };
        }

        /// <summary>A crag: a few rocks, smaller than a mountain's, and trees among them.</summary>
        private static Backdrop Crag(float height, string[] trees, int count)
        {
            return new Backdrop { Height = height, Rocks = 3, Width = height, Trees = trees, Count = count, TreeHeight = new Vector2(5f, 7f) };
        }

        /// <summary>A wood: trees close together on the ground, a few boulders among them.</summary>
        private static Backdrop Grove(float width, string[] trees, int count)
        {
            return new Backdrop { Height = 6f, Rocks = 0, Width = width, Trees = trees, Count = count, TreeHeight = new Vector2(6f, 8.5f) };
        }

        private static Backdrop Fall(Backdrop backdrop)
        {
            backdrop.Autumn = true;
            return backdrop;
        }

        private static readonly string[] None = new string[0];
        private static readonly string[] Leafy = { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5", "TwistedTree_3" };
        private static readonly string[] Mixed = { "CommonTree_1", "Pine_1", "CommonTree_2", "Pine_3", "CommonTree_5", "Pine_4" };
        private static readonly string[] Pines = { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5" };
        private static readonly string[] Dead = { "DeadTree_1", "DeadTree_2", "DeadTree_4", "DeadTree_3" };
        private static readonly string[] Autumn = { "CommonTree_1", "TwistedTree_3", "CommonTree_2", "Pine_3", "TwistedTree_5", "CommonTree_4" };
        private static readonly string[] Willows = { "TwistedTree_2", "DeadTree_1", "TwistedTree_4", "DeadTree_4", "TwistedTree_5" };

        /// <summary>
        /// The hills, mountains and woods that ring a field, by terrain, many meters across: mountains and crags of the kit's
        /// rocks in the stone of the ground (snow on them, the desert's sandstone, ash, the cold rock under the earth), woods of
        /// its trees, and the trees grown over the rocks. The packs' mountains of the classic look stood out among the kits as
        /// flat shaded blocks; the kit's own stone matches the cliffs of the field's hills.
        /// </summary>
        public static void Backdrops(HeroesArt art)
        {
            art.backdrops.Clear();
            Ground[] grounds = Grounds();
            Ground green = grounds[0];
            (TerrainType terrain, Ground ground, Paint paint, Color tint, Backdrop[] pieces)[] table =
            {
                (TerrainType.Grass, green, Paint.Meadow, Color.white, new[]
                {
                    Massif(14f, Mixed, 7), Massif(12f, Mixed, 6), Grove(11f, Leafy, 7), Fall(Grove(11f, Autumn, 7)), Crag(7f, Mixed, 4)
                }),
                (TerrainType.Dirt, green, Paint.Meadow, Color.white, new[]
                {
                    Massif(14f, Mixed, 6), Massif(13f, Pines, 5), Crag(7f, Leafy, 4), Fall(Grove(11f, Autumn, 7))
                }),
                (TerrainType.Rough, green, Paint.Meadow, Color.white, new[]
                {
                    Massif(15f, Pines, 4), Massif(14f, Pines, 3), Massif(13f, Mixed, 4), Crag(8f, Pines, 3)
                }),
                (TerrainType.Water, green, Paint.Meadow, Color.white, new[]
                {
                    Grove(11f, Leafy, 7), Crag(6f, Mixed, 4), Massif(12f, Mixed, 6)
                }),
                (TerrainType.Snow, grounds[1], Paint.Snow, Color.white, new[]
                {
                    Massif(15f, Pines, 6), Massif(14f, Pines, 5), Massif(13f, Pines, 5), Grove(11f, Pines, 7)
                }),
                (TerrainType.Sand, grounds[2], Paint.Sand, SandRock, new[]
                {
                    Massif(14f, Dead, 2), Massif(13f, Dead, 1), Crag(8f, Dead, 3), Crag(6f, None, 0)
                }),
                (TerrainType.Swamp, grounds[3], Paint.Bog, Color.white, new[]
                {
                    Grove(11f, Willows, 7), Grove(11f, Willows, 6), Crag(6f, Willows, 4), Massif(12f, Willows, 5)
                }),
                (TerrainType.Wasteland, grounds[4], Paint.Ash, AshRock, new[]
                {
                    Massif(14f, Dead, 3), Massif(13f, Dead, 3), Crag(8f, Dead, 3)
                }),
                (TerrainType.Rock, grounds[5], Paint.Deep, DeepRock, new[]
                {
                    Massif(15f, None, 0, 3), Massif(14f, None, 0, 2), Massif(13f, None, 0, 2), Crag(8f, None, 0)
                })
            };
            foreach ((TerrainType terrain, Ground ground, Paint paint, Color tint, Backdrop[] own) in table)
            {
                var prefabs = new List<GameObject>();
                for (int i = 0; i < own.Length; i++)
                {
                    GameObject root = HeroesArtBuilder.Root($"Backdrop{terrain}{i}");
                    Compose(root.transform, own[i], ground, new System.Random((int)terrain * 101 + i * 7 + 3), i);
                    prefabs.Add(HeroesArtBuilder.Save(root, $"{Folder}/Backdrops/{terrain}{i}.prefab"));
                }
                // A theme may ring a field with more: dead trees, a ruin, a crypt (painted for the ground like the classic ones).
                foreach ((TerrainType _, string model, float width) in Spec.BackdropExtras.Where(extra => extra.terrain == terrain))
                {
                    GameObject root = HeroesArtBuilder.Root($"Backdrop{terrain}{prefabs.Count}");
                    Landform(root.transform, model, width, prefabs.Count * 60f, paint, tint);
                    prefabs.Add(HeroesArtBuilder.Save(root, $"{Folder}/Backdrops/{terrain}{prefabs.Count}.prefab"));
                }
                art.backdrops.Add(new HeroesArt.BackdropArt { terrain = terrain, prefabs = prefabs.Where(p => p != null).ToArray() });
            }
        }

        /// <summary>A model of the packs as a backdrop: <paramref name="width"/> across, on the ground at the origin, repainted.</summary>
        private static void Landform(Transform root, string model, float width, float yaw, Paint paint, Color tint)
        {
            GameObject piece = HeroesArtBuilder.Piece(root, model, Vector3.zero, yaw, 1f, 0f, tint);
            if (piece == null)
            {
                return;
            }
            Bounds bounds = HeroesArtBuilder.BoundsOf(piece);
            float widest = Mathf.Max(bounds.size.x, bounds.size.z);
            if (widest > 0.001f)
            {
                piece.transform.localScale *= width / widest;
            }
            Settle(piece, Vector3.zero);
            HeroesBattleArt.Repaint(piece, paint);
        }

        /// <summary>
        /// Puts a backdrop together: its crags (the great one in the middle, the lesser ones leaning on it), boulders about
        /// their foot reaching down into the ground (the field stands a backdrop on its hills and sinks it a part of its height:
        /// on a slope its front must not hang over the land falling away before it), then its trees, dropped from above at
        /// spots spread over it and standing where they land, on a rock or on the ground, none on top of another; and under
        /// the earth crystals glowing in the rock.
        /// </summary>
        private static void Compose(Transform root, Backdrop backdrop, Ground g, System.Random dice, int index)
        {
            float Next() => (float)dice.NextDouble();
            // Tall more than wide, the mass in the middle: the field stands a backdrop where its hills level off above a
            // steep rise, and a wide one would hang out over the rise.
            float reach = backdrop.Rocks > 0 ? backdrop.Height * 0.5f : backdrop.Width * 0.5f;
            for (int r = 0; r < backdrop.Rocks; r++)
            {
                float angle = r * 2.2f + Next();
                float away = r == 0 ? 0f : backdrop.Height * (0.28f + 0.14f * Next());
                float height = backdrop.Height * (r == 0 ? 1f : 0.45f + 0.25f * Next());
                GameObject rock = g.Kit(root, $"{Nature}/Rock_Medium_{1 + (r + index) % 3}", Vector3.zero, dice.Next(360), height);
                if (rock != null)
                {
                    float wide = height * (r == 0 ? 0.75f : 0.6f);
                    Resize(rock, new Vector3(wide, 0f, wide));
                    Settle(rock, At(Mathf.Cos(angle) * away, Mathf.Sin(angle) * away, -height * 0.3f));
                }
            }
            // Boulders about the foot, most of them in the ground.
            int boulders = backdrop.Rocks > 0 ? 6 : 3;
            for (int b = 0; b < boulders; b++)
            {
                float angle = (b + Next() * 0.5f) / boulders * Mathf.PI * 2f;
                float height = Mathf.Clamp(backdrop.Height * (0.3f + 0.1f * Next()), 2.4f, 5f);
                float away = reach * (0.7f + 0.2f * Next());
                g.Kit(root, $"{Nature}/Rock_Medium_{1 + (b + index) % 3}", At(Mathf.Cos(angle) * away, Mathf.Sin(angle) * away, -height * 0.6f),
                    dice.Next(360), height);
            }
            reach = Mathf.Max(reach, 4f);
            List<MeshCollider> colliders = Colliders(root);
            if (backdrop.Count > 0 && backdrop.Trees.Length > 0)
            {
                Plant(root, backdrop, g, dice, reach, colliders);
            }
            for (int c = 0; c < backdrop.Crystals; c++)
            {
                // A cluster of the obstacles' crystals, three times their size, on the rock.
                var cluster = new GameObject("Crystals");
                cluster.transform.SetParent(root, false);
                Crystal(cluster.transform, g, c % 2);
                cluster.transform.localScale = Vector3.one * 3f;
                float angle = c * 2.3f + Next();
                cluster.transform.localPosition = Surface(colliders, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach * 0.55f) + Vector3.down * 0.3f;
                cluster.transform.localRotation = Quaternion.Euler(0f, dice.Next(360), 0f);
                // Its parts are moved up to the backdrop's root, where saving tells the parts of one model apart by name.
                var parts = new List<Transform>();
                foreach (Transform part in cluster.transform)
                {
                    parts.Add(part);
                }
                foreach (Transform part in parts)
                {
                    part.SetParent(root, true);
                }
                Object.DestroyImmediate(cluster);
            }
            foreach (MeshCollider collider in colliders)
            {
                Object.DestroyImmediate(collider);
            }
        }

        /// <summary>Colliders on what a backdrop is made of so far, for the rays that find where to put things on it (taken off before it is saved).</summary>
        private static List<MeshCollider> Colliders(Transform root)
        {
            var colliders = new List<MeshCollider>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null)
                {
                    MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    colliders.Add(collider);
                }
            }
            Physics.SyncTransforms();
            return colliders;
        }

        /// <summary>The highest point of the colliders straight under (or over) a spot, or the ground.</summary>
        private static Vector3 Surface(List<MeshCollider> colliders, Vector3 spot)
        {
            float ground = 0f;
            var ray = new Ray(spot + Vector3.up * 500f, Vector3.down);
            foreach (MeshCollider collider in colliders)
            {
                if (collider.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    ground = Mathf.Max(ground, hit.point.y);
                }
            }
            return new Vector3(spot.x, ground, spot.z);
        }

        /// <summary>Plants the trees of a backdrop over its rocks and around them, each where a ray dropped from above lands.</summary>
        private static void Plant(Transform root, Backdrop backdrop, Ground g, System.Random dice, float reach, List<MeshCollider> colliders)
        {
            var spots = new List<Vector3>();
            for (int tries = 0; tries < backdrop.Count * 40 && spots.Count < backdrop.Count; tries++)
            {
                float angle = (float)dice.NextDouble() * Mathf.PI * 2f;
                float away = reach * Mathf.Sqrt((float)dice.NextDouble());
                Vector3 spot = Surface(colliders, new Vector3(Mathf.Cos(angle) * away, 0f, Mathf.Sin(angle) * away));
                if (spots.Any(other => new Vector2(other.x - spot.x, other.z - spot.z).sqrMagnitude < 2.6f * 2.6f))
                {
                    continue;
                }
                spots.Add(spot);
            }
            for (int i = 0; i < spots.Count; i++)
            {
                string tree = backdrop.Trees[i % backdrop.Trees.Length];
                float height = Mathf.Lerp(backdrop.TreeHeight.x, backdrop.TreeHeight.y, (float)dice.NextDouble());
                // Sunk where it stands, so a tree on a slope does not stand on the tip of its roots, and its foot reaches down.
                GameObject planted = g.Kit(root, $"{Nature}/{tree}", spots[i] + Vector3.down * 0.8f, dice.Next(360), height);
                if (backdrop.Autumn)
                {
                    HeroesKitArt.Swap(planted, "Leaves_TwistedTree", HeroesKitArt.Material("Nature", "Leaves_TwistedTree", 0));
                }
            }
        }
    }
}
