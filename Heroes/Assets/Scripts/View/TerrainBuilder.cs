using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Shapes the Unity terrain of a map from its cells: heights blended between the cells (hills, mountains raised,
    /// lakes sunk below the water), the ground painted with the terrain of every cell and soft borders between them,
    /// roads along the road cells, forest floor under the woods, rock on the slopes, and the woods themselves as
    /// terrain trees. Nothing here affects the rules; the view only.
    /// </summary>
    public sealed class TerrainBuilder
    {
        public const float MaxHeight = 16f;
        public const float WaterLevel = 1.55f;

        private readonly MapData map;
        private readonly HexLayout layout;
        private readonly HeroesArt art;
        private readonly HexGrid grid;
        private readonly float[] cellHeight;
        private readonly Vector2[] centers;
        private readonly int[][] disk1;
        private readonly int[][] disk2;
        private int[] ring = System.Array.Empty<int>();

        public TerrainBuilder(MapData map, HexLayout layout, HeroesArt art)
        {
            this.map = map;
            this.layout = layout;
            this.art = art;
            grid = map.grid;
            cellHeight = new float[grid.Count];
            centers = new Vector2[grid.Count];
            for (int cell = 0; cell < grid.Count; cell++)
            {
                cellHeight[cell] = map.height[cell] / 255f * MaxHeight;
                grid.Center(cell, out double x, out double y);
                centers[cell] = new Vector2((float)x, (float)y);
            }
            disk1 = new int[grid.Count][];
            disk2 = new int[grid.Count][];
            for (int cell = 0; cell < grid.Count; cell++)
            {
                disk1[cell] = grid.Disk(cell, 1).ToArray();
                disk2[cell] = grid.Disk(cell, 2).ToArray();
            }
        }

        public static int HeightResolution(Vector2 size)
        {
            float largest = Mathf.Max(size.x, size.y);
            return largest > 150f ? 1025 : 513;
        }

        public TerrainData Build()
        {
            Vector2 size = layout.TerrainSize;
            var data = new TerrainData
            {
                heightmapResolution = HeightResolution(size),
                alphamapResolution = Mathf.Max(size.x, size.y) > 150f ? 1024 : 512,
                baseMapResolution = 1024
            };
            data.size = new Vector3(size.x, MaxHeight + 6f, size.y);
            data.SetHeights(0, 0, Heights(data));
            data.terrainLayers = art.layers;
            data.SetAlphamaps(0, 0, Splat(data));
            PlantTrees(data);
            return data;
        }

        // ------------------------------------------------------------------ sampling the cells

        /// <summary>The cells around a grid point: the one it lies in and two rings about it.</summary>
        private void Around(Vector2 point, int rings)
        {
            int center = grid.CellAt(point.x, point.y);
            if (center < 0)
            {
                // Off the grid: the nearest cell of the edge row or column.
                int row = Mathf.Clamp(Mathf.RoundToInt((point.y - 1f) / 1.5f), 0, grid.rows - 1);
                int column = Mathf.Clamp(Mathf.RoundToInt(point.x / 1.7320508f - 1f + 0.5f * (row & 1)), 0, grid.columns - 1);
                center = grid.Index(column, row);
            }
            ring = rings >= 2 ? disk2[center] : disk1[center];
        }

        private float EdgeFalloff(Vector2 point)
        {
            // How far outside the grid the point lies, in grid units (0 inside).
            grid.Size(out double w, out double h);
            float dx = Mathf.Max(0f, Mathf.Max(-point.x, point.x - (float)w));
            float dy = Mathf.Max(0f, Mathf.Max(-point.y, point.y - (float)h));
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private float[,] Heights(TerrainData data)
        {
            int resolution = data.heightmapResolution;
            var heights = new float[resolution, resolution];
            Vector3 size = data.size;
            for (int j = 0; j < resolution; j++)
            {
                float z = j / (float)(resolution - 1) * size.z;
                for (int i = 0; i < resolution; i++)
                {
                    float x = i / (float)(resolution - 1) * size.x;
                    Vector2 point = layout.GridPoint(x, z);
                    float h = HeightAt(point, x, z);
                    heights[j, i] = Mathf.Clamp01(h / size.y);
                }
            }
            return heights;
        }

        private float HeightAt(Vector2 point, float worldX, float worldZ)
        {
            Around(point, 2);
            float total = 0f;
            float weights = 0f;
            float peak = 0f;
            foreach (int cell in ring)
            {
                float d = Vector2.Distance(point, centers[cell]);
                if (d >= 2.4f)
                {
                    continue;
                }
                float w = 1f - d * d / (2.4f * 2.4f);
                w *= w;
                total += cellHeight[cell] * w;
                weights += w;
                var obstacle = (Obstacle)map.obstacle[cell];
                if (obstacle == Obstacle.Mountain || obstacle == Obstacle.Edge)
                {
                    float p = Mathf.Max(0f, 1f - d / 1.6f);
                    peak = Mathf.Max(peak, p * p * 2.5f);
                }
            }
            float h = weights > 0f ? total / weights : cellHeight[0];
            h += peak;
            // Beyond the grid the land climbs into mountains that frame the map.
            float outside = EdgeFalloff(point);
            if (outside > 0f)
            {
                h = Mathf.Lerp(h, 9f + Mathf.PerlinNoise(worldX * 0.08f, worldZ * 0.08f) * 6f, Mathf.Clamp01(outside / 4f));
            }
            // A little unevenness everywhere so the ground catches the light.
            h += (Mathf.PerlinNoise(worldX * 0.35f + 11.3f, worldZ * 0.35f + 7.1f) - 0.5f) * 0.35f;
            return Mathf.Max(0f, h);
        }

        // ------------------------------------------------------------------ painting

        private static int LayerOf(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Grass: return (int)GroundLayer.Grass;
                case TerrainType.Dirt: return (int)GroundLayer.Dirt;
                case TerrainType.Sand: return (int)GroundLayer.Sand;
                case TerrainType.Snow: return (int)GroundLayer.Snow;
                case TerrainType.Swamp: return (int)GroundLayer.Swamp;
                case TerrainType.Rough: return (int)GroundLayer.Rough;
                case TerrainType.Wasteland: return (int)GroundLayer.Wasteland;
                case TerrainType.Water: return (int)GroundLayer.Sand;
                default: return (int)GroundLayer.Rock;
            }
        }

        private float[,,] Splat(TerrainData data)
        {
            int resolution = data.alphamapResolution;
            int layers = art.layers.Length;
            var splat = new float[resolution, resolution, layers];
            Vector3 size = data.size;
            var weights = new float[layers];
            var roadNeighbors = new List<int>(6);
            for (int j = 0; j < resolution; j++)
            {
                float z = (j + 0.5f) / resolution * size.z;
                for (int i = 0; i < resolution; i++)
                {
                    float x = (i + 0.5f) / resolution * size.x;
                    Vector2 point = layout.GridPoint(x, z);
                    System.Array.Clear(weights, 0, layers);
                    Around(point, 1);
                    float noise = Mathf.PerlinNoise(x * 0.21f + 3.7f, z * 0.21f + 1.3f);
                    float road = 0f;
                    foreach (int cell in ring)
                    {
                        float d = Vector2.Distance(point, centers[cell]);
                        float reach = 1.45f + (noise - 0.5f) * 0.5f;
                        if (d < reach)
                        {
                            float w = 1f - d * d / (reach * reach);
                            w *= w;
                            var terrain = (TerrainType)map.terrain[cell];
                            var obstacle = (Obstacle)map.obstacle[cell];
                            int layer = LayerOf(terrain);
                            if (obstacle == Obstacle.Mountain || obstacle == Obstacle.Edge || obstacle == Obstacle.Rocks)
                            {
                                weights[(int)GroundLayer.Rock] += w * (obstacle == Obstacle.Rocks ? 0.6f : 1f);
                                weights[layer] += w * (obstacle == Obstacle.Rocks ? 0.4f : 0.1f);
                            }
                            else if (obstacle == Obstacle.Forest && terrain != TerrainType.Snow)
                            {
                                weights[(int)GroundLayer.ForestFloor] += w * 0.75f;
                                weights[layer] += w * 0.25f;
                            }
                            else
                            {
                                weights[layer] += w;
                            }
                        }
                        if (map.road[cell] != 0)
                        {
                            // Distance to the road's middle line: segments to the road cells around.
                            grid.Neighbors(cell, roadNeighbors);
                            bool alone = true;
                            foreach (int next in roadNeighbors)
                            {
                                if (map.road[next] == 0 || next < cell)
                                {
                                    if (map.road[next] != 0)
                                    {
                                        alone = false;
                                    }
                                    continue;
                                }
                                alone = false;
                                float s = SegmentDistance(point, centers[cell], centers[next]);
                                road = Mathf.Max(road, RoadWeight(s, noise));
                            }
                            if (alone)
                            {
                                road = Mathf.Max(road, RoadWeight(d, noise));
                            }
                        }
                    }
                    if (EdgeFalloff(point) > 0.5f)
                    {
                        weights[(int)GroundLayer.Rock] += Mathf.Clamp01(EdgeFalloff(point) / 3f);
                    }
                    float sum = 0f;
                    for (int l = 0; l < layers; l++)
                    {
                        sum += weights[l];
                    }
                    if (sum <= 0f)
                    {
                        weights[(int)GroundLayer.Grass] = 1f;
                        sum = 1f;
                    }
                    // The steep flanks of the mountains show rock whatever the land.
                    float slope = data.GetSteepness((i + 0.5f) / resolution, (j + 0.5f) / resolution);
                    float rock = Mathf.Clamp01((slope - 28f) / 18f);
                    for (int l = 0; l < layers; l++)
                    {
                        float value = weights[l] / sum * (1f - rock) * (1f - road);
                        if (l == (int)GroundLayer.Rock)
                        {
                            value += rock * (1f - road);
                        }
                        if (l == (int)GroundLayer.Road)
                        {
                            value += road;
                        }
                        splat[j, i, l] = value;
                    }
                }
            }
            return splat;
        }

        private static float RoadWeight(float distance, float noise)
        {
            float width = 0.42f + (noise - 0.5f) * 0.12f;
            return Mathf.Clamp01((width - distance) / 0.22f);
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        // ------------------------------------------------------------------ woods

        private void PlantTrees(TerrainData data)
        {
            var prototypes = new List<TreePrototype>();
            int forestStart = prototypes.Count;
            foreach (GameObject tree in art.forestTrees)
            {
                prototypes.Add(new TreePrototype { prefab = tree });
            }
            int pineStart = prototypes.Count;
            foreach (GameObject tree in art.pineTrees)
            {
                prototypes.Add(new TreePrototype { prefab = tree });
            }
            int deadStart = prototypes.Count;
            foreach (GameObject tree in art.deadTrees)
            {
                prototypes.Add(new TreePrototype { prefab = tree });
            }
            if (prototypes.Count == 0)
            {
                return;
            }
            data.treePrototypes = prototypes.ToArray();
            var trees = new List<TreeInstance>();
            Vector3 size = data.size;
            float r = layout.Radius;
            for (int cell = 0; cell < grid.Count; cell++)
            {
                var obstacle = (Obstacle)map.obstacle[cell];
                var terrain = (TerrainType)map.terrain[cell];
                bool dead = obstacle == Obstacle.DeadTrees;
                bool forest = obstacle == Obstacle.Forest;
                bool edge = obstacle == Obstacle.Edge;
                if (!forest && !dead && !(edge && Hash(cell, 3) % 3 == 0))
                {
                    continue;
                }
                int start;
                int count;
                if (dead)
                {
                    start = deadStart;
                    count = art.deadTrees.Length;
                }
                else if (terrain == TerrainType.Snow || art.forestTrees.Length == 0 || Hash(cell, 5) % 4 == 0)
                {
                    start = pineStart;
                    count = art.pineTrees.Length;
                }
                else
                {
                    start = forestStart;
                    count = art.forestTrees.Length;
                }
                if (count == 0)
                {
                    start = 0;
                    count = prototypes.Count;
                }
                Vector3 center = layout.Flat(cell);
                int planted = dead ? 2 : 3;
                for (int k = 0; k < planted; k++)
                {
                    uint h = Hash(cell, 17 + k * 7);
                    float angle = (h % 360) * Mathf.Deg2Rad;
                    float distance = k == 0 ? (h >> 9) % 30 / 100f * r : (0.35f + (h >> 12) % 40 / 100f) * r;
                    Vector3 p = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                    float scale = (k == 0 ? 1.05f : 0.75f) + (h >> 16) % 30 / 100f;
                    trees.Add(new TreeInstance
                    {
                        prototypeIndex = start + (int)((h >> 5) % (uint)count),
                        position = new Vector3(p.x / size.x, 0f, p.z / size.z),
                        widthScale = scale,
                        heightScale = scale * (0.9f + (h >> 20) % 25 / 100f),
                        rotation = (h >> 3) % 628 / 100f,
                        color = Color.Lerp(Color.white, new Color(0.85f, 0.95f, 0.8f), (h >> 7) % 100 / 100f),
                        lightmapColor = Color.white
                    });
                }
            }
            // Woods along the outside of the map, where the mountains leave room.
            int step = 3;
            for (float z = 1f; z < size.z - 1f; z += step)
            {
                for (float x = 1f; x < size.x - 1f; x += step)
                {
                    Vector2 point = layout.GridPoint(x, z);
                    float outside = EdgeFalloff(point);
                    if (outside < 1.2f || outside > 5f)
                    {
                        continue;
                    }
                    uint h = Hash((int)(x * 31 + z * 7919), 29);
                    if (h % 3 != 0)
                    {
                        continue;
                    }
                    int count = art.pineTrees.Length > 0 ? art.pineTrees.Length : prototypes.Count;
                    int start = art.pineTrees.Length > 0 ? pineStart : 0;
                    float jx = (h >> 4) % 100 / 100f * step;
                    float jz = (h >> 11) % 100 / 100f * step;
                    float scale = 0.9f + (h >> 17) % 40 / 100f;
                    trees.Add(new TreeInstance
                    {
                        prototypeIndex = start + (int)((h >> 3) % (uint)count),
                        position = new Vector3((x + jx) / size.x, 0f, (z + jz) / size.z),
                        widthScale = scale,
                        heightScale = scale,
                        rotation = (h >> 6) % 628 / 100f,
                        color = Color.white,
                        lightmapColor = Color.white
                    });
                }
            }
            data.SetTreeInstances(trees.ToArray(), true);
        }

        public static uint Hash(int a, int salt)
        {
            unchecked
            {
                uint h = (uint)a * 2654435761u + (uint)salt * 40503u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return h;
            }
        }
    }
}
