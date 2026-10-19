using Gamebox.Editor;
using UnityEngine;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// The effects of a race, all generated: the particle textures (flare, fire puff, smoke puff, spark, shock ring,
    /// scorch mark, tracer and tyre tread), their URP particle materials, the debris meshes, and the effect prefabs that
    /// COMBAT plays: explosion, big explosion (a wreck), sparks, damage smoke, black smoke, muzzle flash, fire, nitro
    /// flame, tyre dust and the scorch decal.
    ///
    /// What the race expects of them (COMBAT's RaceEffects): a root without physics holding the particle systems, all
    /// simulated in world space, playing on awake; one-shots do not loop, loops do (they trail behind moving cars).
    /// They are authored at size 1 (an explosion about 4 m across, sparks about 1 m) and scale with their transform.
    /// Systems whose names contain "Tint" take the theme's colour (explosion, sparks, muzzle) as start colour, so their
    /// colour-over-life gradients start white. Sparks and the nitro flame emit along +z. An explosion's "Light" child is a
    /// disabled point light set to its peak (COMBAT flashes it). The scorch is a 1 m quad facing up on the root.
    /// </summary>
    internal static partial class DeathRaceEffectsBuilder
    {
        public const string Folder = "Art/Effects";
        public const string TextureFolder = Folder + "/Textures";
        public const string MaterialFolder = Folder + "/Materials";
        public const string MeshFolder = Folder + "/Meshes";

        public static Material EffectMaterial(string name)
        {
            return DeathRaceAssets.Load<Material>($"{MaterialFolder}/{name}.mat");
        }

        public static Mesh EffectMesh(string name)
        {
            return DeathRaceAssets.Load<Mesh>($"{MeshFolder}/{name}.asset");
        }

        private static Shader ParticlesLit => Shader.Find("Universal Render Pipeline/Particles/Simple Lit");

        // ------------------------------------------------------------------ art

        /// <summary>The textures, materials and meshes of the effects.</summary>
        public static void BuildArt()
        {
            Texture2D flare = Save(FlareTexture(), "Flare");
            Texture2D fire = Save(PuffTexture(11, 0.55f, 2.2f), "Fire");
            Texture2D smoke = Save(PuffTexture(23, 0.75f, 1.4f), "Smoke");
            Texture2D spark = Save(SparkTexture(), "Spark");
            Texture2D ring = Save(RingTexture(), "Ring");
            Texture2D scorch = Save(ScorchTexture(), "Scorch");
            Texture2D tracer = Save(TracerTexture(), "Tracer");
            Texture2D skid = DeathRaceAssets.SaveTexture(SkidTexture(), $"{TextureFolder}/Skid.png", CarArt.TrailImport);

            SaveTransparent("FireAdd", fire, Color.white, Blending.Additive);
            SaveTransparent("FlareAdd", flare, Color.white, Blending.Additive);
            SaveTransparent("SparkAdd", spark, Color.white, Blending.Additive);
            SaveTransparent("RingAdd", ring, Color.white, Blending.Additive);
            SaveTransparent("Smoke", smoke, Color.white, Blending.Alpha);
            SaveTransparent("Scorch", scorch, new Color(0.05f, 0.04f, 0.035f, 0.8f), Blending.Alpha, -20);
            SaveTransparent("Tracer", tracer, Color.white, Blending.Additive);
            SaveTransparent("Skid", skid, Color.white, Blending.Alpha, -30);
            DeathRaceAssets.SaveMaterial($"{MaterialFolder}/Debris.mat", ParticlesLit, m =>
            {
                m.SetTexture("_BaseMap", null);
                m.SetColor("_BaseColor", Color.white);
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_Cull", 2f);
                m.SetFloat("_Smoothness", 0.25f);
                m.SetFloat("_ZWrite", 1f);
                m.SetFloat("_SrcBlend", 1f);
                m.SetFloat("_DstBlend", 0f);
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = 2000;
                m.enableInstancing = true;
            });

            var chunk = new LowPolyBuilder();
            chunk.Color(CarArt.UV("White")).Blob(Vector3.zero, new Vector3(0.5f, 0.32f, 0.42f), 0, 0.3f, 5);
            DeathRaceAssets.SaveMesh(chunk.ToMesh("Chunk"), $"{MeshFolder}/Chunk.asset");
            var shard = new LowPolyBuilder();
            shard.Color(CarArt.UV("White")).Prism(new[] { new Vector2(-0.5f, -0.3f), new Vector2(0.5f, -0.2f), new Vector2(0.2f, 0.35f), new Vector2(-0.35f, 0.25f) }, 0.08f);
            DeathRaceAssets.SaveMesh(shard.ToMesh("Shard"), $"{MeshFolder}/Shard.asset");
            DeathRaceAssets.SaveMesh(CarMeshes.Wheel(0.5f, 0.36f, "Rim", false, 5).ToMesh("Tyre"), $"{MeshFolder}/Tyre.asset");
            DeathRaceAssets.SaveMesh(DecalQuad(), $"{MeshFolder}/Decal.asset");
        }

        private static Texture2D Save(Texture2D texture, string name)
        {
            return DeathRaceAssets.SaveTexture(texture, $"{TextureFolder}/{name}.png", CarArt.SoftImport);
        }

        private static void SaveTransparent(string name, Texture texture, Color color, Blending blend, int queueOffset = 0)
        {
            DeathRaceAssets.SaveMaterial($"{MaterialFolder}/{name}.mat", CarArt.ParticlesUnlit, m => CarArt.Transparent(m, texture, color, blend, true, queueOffset));
        }

        /// <summary>A 1 m quad facing up, lifted 1.5 cm so it lies on the road without flickering.</summary>
        private static Mesh DecalQuad()
        {
            Mesh mesh = CarMeshes.GroundQuad(1f, 1f);
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].y = 0.015f;
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            mesh.name = "Decal";
            return mesh;
        }

        // ------------------------------------------------------------------ textures

        /// <summary>A seeded value noise in 0..1 (smooth between integer lattice points).</summary>
        private static float ValueNoise(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float fx = x - xi;
            float fy = y - yi;
            float u = fx * fx * (3f - 2f * fx);
            float v = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, seed);
            float b = Hash(xi + 1, yi, seed);
            float c = Hash(xi, yi + 1, seed);
            float d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144269504);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Fractal noise: a few octaves of <see cref="ValueNoise"/>.</summary>
        private static float Fractal(float x, float y, int seed, int octaves = 4)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(x, y, seed + i * 31) * amplitude;
                total += amplitude;
                x *= 2.03f;
                y *= 2.03f;
                amplitude *= 0.5f;
            }
            return sum / total;
        }

        /// <summary>A star burst: a hot core, a soft halo and six rays (muzzle flashes and blast flashes).</summary>
        private static Texture2D FlareTexture()
        {
            return CarArt.ToTexture(128, 128, (u, v) =>
            {
                var p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
                float r = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);
                float core = Mathf.Exp(-r * r * 30f);
                float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 3f) * 0.45f;
                float rays = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 3f)), 24f) * Mathf.Pow(Mathf.Clamp01(1f - r), 1.5f) * 0.9f;
                float a = Mathf.Clamp01(core + halo + rays);
                return new Color(1f, 1f, 1f, a);
            });
        }

        /// <summary>A soft, lumpy puff (fire or smoke): a round falloff eaten into by fractal noise.</summary>
        private static Texture2D PuffTexture(int seed, float softness, float contrast)
        {
            return CarArt.ToTexture(128, 128, (u, v) =>
            {
                var p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
                float r = p.magnitude;
                float n = Fractal(u * 5f, v * 5f, seed);
                float shape = Mathf.Clamp01(1f - r / (0.65f + 0.35f * n));
                float a = Mathf.Pow(shape, softness) * Mathf.Clamp01(0.55f + (n - 0.5f) * contrast);
                float shade = Mathf.Lerp(0.75f, 1f, n);
                return new Color(shade, shade, shade, Mathf.Clamp01(a));
            });
        }

        /// <summary>A small bright dot (stretched along its motion by the renderer).</summary>
        private static Texture2D SparkTexture()
        {
            return CarArt.ToTexture(32, 32, (u, v) =>
            {
                float r = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float a = Mathf.Clamp01(Mathf.Exp(-r * r * 6f) * 1.2f);
                return new Color(1f, 1f, 1f, a);
            });
        }

        /// <summary>A thin soft ring: the shock wave on the ground.</summary>
        private static Texture2D RingTexture()
        {
            return CarArt.ToTexture(128, 128, (u, v) =>
            {
                float r = new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f;
                float a = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.07f, 2f)) + Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.25f, 2f)) * 0.25f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(a * (r < 1f ? 1f : 0f)));
            });
        }

        /// <summary>A burnt mark: a dark ragged centre with streaks thrown outwards, fading at the edge.</summary>
        private static Texture2D ScorchTexture()
        {
            return CarArt.ToTexture(256, 256, (u, v) =>
            {
                var p = new Vector2(u - 0.5f, v - 0.5f) * 2f;
                float r = p.magnitude;
                float angle = Mathf.Atan2(p.y, p.x);
                float ragged = Fractal(u * 6f, v * 6f, 41);
                float streaks = Fractal(Mathf.Cos(angle) * 3f + 10f, Mathf.Sin(angle) * 3f + 10f, 57);
                float reach = 0.45f + 0.35f * streaks + 0.15f * ragged;
                float a = Mathf.Clamp01(1f - CarArt.Smooth(reach * 0.55f, reach, r));
                a *= Mathf.Lerp(0.7f, 1f, ragged);
                float shade = Mathf.Lerp(0.6f, 1.2f, Fractal(u * 14f, v * 14f, 73));
                return new Color(Mathf.Clamp01(shade * 0.9f), Mathf.Clamp01(shade * 0.85f), Mathf.Clamp01(shade * 0.8f), a);
            });
        }

        /// <summary>A tracer streak: a hot core across the line's width, soft at both edges (u along, v across).</summary>
        private static Texture2D TracerTexture()
        {
            return CarArt.ToTexture(64, 32, (u, v) =>
            {
                float across = Mathf.Abs(v - 0.5f) * 2f;
                float core = Mathf.Exp(-across * across * 18f);
                float glow = Mathf.Exp(-across * across * 3f) * 0.35f;
                float ends = CarArt.Smooth(0f, 0.15f, u) * CarArt.Smooth(0f, 0.08f, 1f - u);
                return new Color(1f, 1f, 1f, Mathf.Clamp01((core + glow) * ends));
            });
        }

        /// <summary>
        /// Rubber laid on the road: a tread of blocks along u (it repeats along a trail), darkest in the middle of the tyre
        /// and frayed at the edges across v.
        /// </summary>
        private static Texture2D SkidTexture()
        {
            return CarArt.ToTexture(64, 64, (u, v) =>
            {
                float across = Mathf.Abs(v - 0.5f) * 2f;
                float body = 1f - CarArt.Smooth(0.6f, 1f, across);
                float blocks = 0.75f + 0.25f * Mathf.Sign(Mathf.Sin((u * 8f + (across > 0.35f ? 0.5f : 0f)) * Mathf.PI * 2f));
                float turn = u * Mathf.PI * 2f;
                float grit = Mathf.Lerp(0.7f, 1f, Fractal(Mathf.Cos(turn) * 2f + 5f, v * 8f + Mathf.Sin(turn) * 2f, 91, 3));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(body * blocks * grit));
            });
        }
    }
}
