using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The ground of a battlefield of its own: a small Unity terrain, dead flat where the troops stand, rising into
    /// hills behind the field and at its sides and never in front of the camera, painted with the land the armies met
    /// on (broad shades of a darker ground and patches of a second one for variety, rock on the steep flanks). Everything comes from the terrain and the
    /// seed of the battle, so the same battle always gets the same ground. The terrain data it makes belongs to the
    /// caller, who destroys it when the battle is over.
    /// </summary>
    public static class BattlefieldGround
    {
        /// <summary>How far below the field the terrain starts, so the land in front can dip a little.</summary>
        public const float Base = 3f;

        /// <summary>The flat margin around the field, in meters, before the land starts to change.</summary>
        private const float Margin = 1.6f;

        /// <summary>
        /// Builds the terrain data for a field of <paramref name="field"/> (its corner at the world origin, its width
        /// along x and depth along z) inside a terrain whose corner is at <paramref name="corner"/> and whose size is
        /// <paramref name="size"/> (x and z). The field's ground is at height 0.
        /// </summary>
        public static TerrainData Build(HeroesArt art, TerrainType land, uint seed, Vector2 field, Vector3 corner, Vector2 size)
        {
            var data = new TerrainData
            {
                heightmapResolution = 257,
                alphamapResolution = 256,
                baseMapResolution = 512
            };
            data.size = new Vector3(size.x, 40f, size.y);
            float offsetX = seed % 997 * 0.37f;
            float offsetZ = seed / 997 % 991 * 0.41f;
            data.SetHeights(0, 0, Heights(data, field, corner, offsetX, offsetZ));
            if (art != null && art.layers != null && art.layers.Length > 0)
            {
                data.terrainLayers = art.layers;
                data.SetAlphamaps(0, 0, Splat(data, art.layers.Length, land, field, corner, offsetX, offsetZ));
            }
            return data;
        }

        /// <summary>The height of the land at a point of the world, from the field's ground (0).</summary>
        public static float HeightAt(float x, float z, Vector2 field, float offsetX, float offsetZ)
        {
            float left = -Margin - x;
            float right = x - (field.x + Margin);
            float back = z - (field.y + Margin);
            float front = -Margin - z;
            float side = Mathf.Max(0f, Mathf.Max(left, right));
            float behind = Mathf.Max(0f, back);
            float before = Mathf.Max(0f, front);
            if (side <= 0f && behind <= 0f && before <= 0f)
            {
                return 0f;
            }
            float noise = Mathf.PerlinNoise(x * 0.045f + offsetX, z * 0.045f + offsetZ);
            float fine = Mathf.PerlinNoise(x * 0.16f + offsetZ, z * 0.16f + offsetX) - 0.5f;
            // Behind the field the land climbs into hills; the farther, the higher.
            float hills = Smooth(behind / 20f) * (6f + noise * 10f) + Mathf.Max(0f, behind - 20f) * 0.18f;
            // At the sides it rises too, more toward the back than toward the camera.
            float toward = Smooth((z + 6f) / (field.y + 12f));
            float flanks = Smooth(side / 16f) * (3f + noise * 8f) * (0.45f + 0.55f * toward);
            float h = Mathf.Max(hills, flanks);
            // In front the land only rolls a little, lower than the field, so nothing stands between the camera and it.
            h -= Smooth(before / 10f) * (0.4f + noise * 0.5f);
            float away = Mathf.Max(side, Mathf.Max(behind, before));
            h += fine * 0.6f * Smooth(away / 4f);
            return h;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float[,] Heights(TerrainData data, Vector2 field, Vector3 corner, float offsetX, float offsetZ)
        {
            int resolution = data.heightmapResolution;
            var heights = new float[resolution, resolution];
            Vector3 size = data.size;
            for (int j = 0; j < resolution; j++)
            {
                float z = corner.z + j / (float)(resolution - 1) * size.z;
                for (int i = 0; i < resolution; i++)
                {
                    float x = corner.x + i / (float)(resolution - 1) * size.x;
                    heights[j, i] = Mathf.Clamp01((HeightAt(x, z, field, offsetX, offsetZ) + Base) / size.y);
                }
            }
            return heights;
        }

        /// <summary>
        /// The layer a land is painted with, the one that breaks it up in patches, and the darker one it is shaded with
        /// in broad sweeps.
        /// </summary>
        private static void Layers(TerrainType land, out GroundLayer main, out GroundLayer patches, out GroundLayer shade)
        {
            switch (land)
            {
                case TerrainType.Dirt:
                    main = GroundLayer.Dirt;
                    patches = GroundLayer.Grass;
                    shade = GroundLayer.Rough;
                    break;
                case TerrainType.Sand:
                case TerrainType.Water:
                    main = GroundLayer.Sand;
                    patches = GroundLayer.Rough;
                    shade = GroundLayer.Dirt;
                    break;
                case TerrainType.Snow:
                    main = GroundLayer.Snow;
                    patches = GroundLayer.Rock;
                    shade = GroundLayer.Rough;
                    break;
                case TerrainType.Swamp:
                    main = GroundLayer.Swamp;
                    patches = GroundLayer.ForestFloor;
                    shade = GroundLayer.Dirt;
                    break;
                case TerrainType.Rough:
                    main = GroundLayer.Rough;
                    patches = GroundLayer.Dirt;
                    shade = GroundLayer.Rock;
                    break;
                case TerrainType.Wasteland:
                    main = GroundLayer.Wasteland;
                    patches = GroundLayer.Rock;
                    shade = GroundLayer.Dirt;
                    break;
                case TerrainType.Rock:
                    main = GroundLayer.Rough;
                    patches = GroundLayer.Rock;
                    shade = GroundLayer.Dirt;
                    break;
                default:
                    main = GroundLayer.Grass;
                    patches = GroundLayer.Dirt;
                    shade = GroundLayer.ForestFloor;
                    break;
            }
        }

        private static float[,,] Splat(TerrainData data, int layers, TerrainType land, Vector2 field, Vector3 corner, float offsetX, float offsetZ)
        {
            Layers(land, out GroundLayer main, out GroundLayer patches, out GroundLayer shade);
            int resolution = data.alphamapResolution;
            var splat = new float[resolution, resolution, layers];
            Vector3 size = data.size;
            var weights = new float[layers];
            for (int j = 0; j < resolution; j++)
            {
                float z = corner.z + (j + 0.5f) / resolution * size.z;
                for (int i = 0; i < resolution; i++)
                {
                    float x = corner.x + (i + 0.5f) / resolution * size.x;
                    System.Array.Clear(weights, 0, layers);
                    bool inside = x > -Margin && x < field.x + Margin && z > -Margin && z < field.y + Margin;
                    float patch = Mathf.PerlinNoise(x * 0.11f + offsetZ, z * 0.11f + offsetX);
                    float amount = Mathf.Clamp01((patch - (inside ? 0.62f : 0.5f)) / 0.18f) * (inside ? 0.55f : 0.85f);
                    float sweep = Mathf.PerlinNoise(x * 0.045f + offsetX * 1.7f, z * 0.045f + offsetZ * 1.3f);
                    float shaded = Mathf.Clamp01((sweep - 0.42f) / 0.3f) * (inside ? 0.42f : 0.6f) * (1f - amount);
                    weights[(int)main] += 1f - amount - shaded;
                    if ((int)patches < layers)
                    {
                        weights[(int)patches] += amount;
                    }
                    if ((int)shade < layers)
                    {
                        weights[(int)shade] += shaded;
                    }
                    // The steep flanks of the hills show their rock whatever the land.
                    float slope = data.GetSteepness((i + 0.5f) / resolution, (j + 0.5f) / resolution);
                    float rock = Mathf.Clamp01((slope - 24f) / 16f);
                    for (int l = 0; l < layers; l++)
                    {
                        float value = weights[l] * (1f - rock);
                        if (l == (int)GroundLayer.Rock)
                        {
                            value += rock;
                        }
                        splat[j, i, l] = value;
                    }
                }
            }
            return splat;
        }
    }
}
