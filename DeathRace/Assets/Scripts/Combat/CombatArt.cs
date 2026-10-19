using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Textures and materials the fighting draws with when the content has none of its own (before the builders made the
    /// effect prefabs, or in a test): soft dots for fire and sparks, puffs for smoke, a ring for shock waves, a blotch for
    /// scorch marks, streaks for tracers and skid marks. Made once in code from seeded noise, on the always included
    /// sprite shader, so a player build never misses them. The glowing ones are brighter than white so bloom catches them.
    /// </summary>
    public static class CombatArt
    {
        private const string ShaderName = "Sprites/Default";

        private static Texture2D softDot;
        private static Texture2D puff;
        private static Texture2D ring;
        private static Texture2D scorch;
        private static Texture2D streak;
        private static Texture2D tread;
        private static Material glow;
        private static Material smoke;
        private static Material ringMaterial;
        private static Material scorchMaterial;
        private static Material tracer;
        private static Material skid;
        private static Material plain;
        private static Material trail;
        private static Texture2D puffTiled;
        private static Material solid;
        private static Shader solidShader;
        private static bool warned;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>Fire, sparks, flashes: a soft dot, HDR so it blooms.</summary>
        public static Material Glow => glow != null ? glow : (glow = Make("Combat Glow", SoftDot, new Color(2.4f, 2.4f, 2.4f, 1f)));

        /// <summary>Smoke and dust: a soft, slightly lumpy puff.</summary>
        public static Material Smoke => smoke != null ? smoke : (smoke = Make("Combat Smoke", Puff, Color.white));

        /// <summary>The shock wave of a blast, seen from above.</summary>
        public static Material Ring => ringMaterial != null ? ringMaterial : (ringMaterial = Make("Combat Ring", RingTexture, new Color(2f, 2f, 2f, 1f)));

        public static Material Scorch => scorchMaterial != null ? scorchMaterial : (scorchMaterial = Make("Combat Scorch", ScorchTexture, Color.white));

        public static Material Tracer => tracer != null ? tracer : (tracer = Make("Combat Tracer", Streak, new Color(3f, 3f, 3f, 1f)));

        public static Material Skid => skid != null ? skid : (skid = Make("Combat Skid", Tread, Color.white));

        /// <summary>Flat glowing colours: lights and flames of the stand-ins (unlit, so bloom catches them).</summary>
        public static Material Plain => plain != null ? plain : (plain = Make("Combat Plain", null, Color.white));

        /// <summary>A smoke trail: puffs repeating along it (a rocket's).</summary>
        public static Material SmokeTrail => trail != null ? trail : (trail = Make("Combat Smoke Trail", PuffTiled, Color.white));

        /// <summary>
        /// The lit material of the stand-in mines, rockets and pickups: the shader of the theme's car paint (a pipeline
        /// shader that is in the build for sure; a primitive's default material is not, and draws pink in a player),
        /// coloured per part through a property block. The unlit plain material when there is no theme.
        /// </summary>
        public static Material Solid(DeathRaceTheme theme)
        {
            Material template = theme != null ? (theme.carPaint != null ? theme.carPaint : theme.carPalette) : null;
            Shader shader = template != null ? template.shader : null;
            if (shader == null)
            {
                return Plain;
            }
            if (solid == null || solidShader != shader)
            {
                solidShader = shader;
                solid = new Material(shader) { name = "Combat Solid", hideFlags = HideFlags.DontSave };
                if (solid.HasProperty(BaseMapId))
                {
                    solid.SetTexture(BaseMapId, null);
                }
            }
            return solid;
        }

        /// <summary>Colours a stand-in part: lit (<see cref="Solid"/>), or a flat glowing colour for lights and flames.</summary>
        public static void Paint(GameObject go, Color color, bool glow, DeathRaceTheme theme)
        {
            var renderer = go.GetComponent<Renderer>();
            Material material = glow ? Plain : Solid(theme);
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            var block = new MaterialPropertyBlock();
            block.SetColor(ColorId, color);
            block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(block);
            renderer.shadowCastingMode = glow ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        }

        public static Texture2D SoftDot => softDot != null ? softDot : (softDot = Radial("Soft Dot", 64, (r, a) => Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f)));

        public static Texture2D Puff => puff != null ? puff : (puff = MakePuff("Puff"));

        /// <summary>The puff again, repeating along its width (trails tile it).</summary>
        public static Texture2D PuffTiled
        {
            get
            {
                if (puffTiled == null)
                {
                    puffTiled = MakePuff("Puff Tiled");
                    puffTiled.wrapModeU = TextureWrapMode.Repeat;
                }
                return puffTiled;
            }
        }

        /// <summary>A soft, slightly lumpy round puff (seeded, so always the same).</summary>
        private static Texture2D MakePuff(string name)
        {
            var random = new System.Random(11);
            float[] lumps = new float[7];
            for (int i = 0; i < lumps.Length; i++)
            {
                lumps[i] = (float)random.NextDouble();
            }
            return Radial(name, 64, (r, a) =>
            {
                float f = a * lumps.Length;
                float lump = 0.82f + 0.18f * Mathf.Lerp(lumps[(int)f % lumps.Length], lumps[((int)f + 1) % lumps.Length], f % 1f);
                float edge = Mathf.Clamp01(1f - r / lump);
                return Mathf.SmoothStep(0f, 1f, edge) * 0.9f;
            });
        }

        public static Texture2D RingTexture => ring != null ? ring : (ring = Radial("Ring", 128, (r, a) =>
        {
            float band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.14f);
            return band * band * (3f - 2f * band);
        }));

        public static Texture2D ScorchTexture
        {
            get
            {
                if (scorch == null)
                {
                    var random = new System.Random(23);
                    float[] edge = new float[24];
                    for (int i = 0; i < edge.Length; i++)
                    {
                        edge[i] = 0.62f + 0.3f * (float)random.NextDouble();
                    }
                    scorch = Radial("Scorch", 128, (r, a) =>
                    {
                        float f = a * edge.Length;
                        int i = (int)f % edge.Length;
                        float reach = Mathf.Lerp(edge[i], edge[(i + 1) % edge.Length], f % 1f);
                        float inside = Mathf.Clamp01((reach - r) / 0.3f);
                        return Mathf.SmoothStep(0f, 0.92f, inside);
                    }, new Color(0.05f, 0.04f, 0.035f, 1f));
                }
                return scorch;
            }
        }

        /// <summary>Across its height a soft line: bright in the middle, gone at the edges (tracers).</summary>
        public static Texture2D Streak
        {
            get
            {
                if (streak == null)
                {
                    streak = new Texture2D(4, 32, TextureFormat.RGBA32, false) { name = "Streak", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                    for (int y = 0; y < 32; y++)
                    {
                        float v = Mathf.Abs(y / 31f * 2f - 1f);
                        float alpha = Mathf.Pow(1f - v, 1.6f);
                        for (int x = 0; x < 4; x++)
                        {
                            streak.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                        }
                    }
                    streak.Apply(false, true);
                }
                return streak;
            }
        }

        /// <summary>A tyre's mark: dark rubber with a faint tread along it, soft at the sides.</summary>
        public static Texture2D Tread
        {
            get
            {
                if (tread == null)
                {
                    tread = new Texture2D(16, 32, TextureFormat.RGBA32, false) { name = "Tread", wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };
                    var random = new System.Random(5);
                    for (int x = 0; x < 16; x++)
                    {
                        float groove = x % 4 == 0 ? 0.75f : 1f;
                        for (int y = 0; y < 32; y++)
                        {
                            float v = Mathf.Abs(y / 31f * 2f - 1f);
                            float alpha = Mathf.Clamp01((1f - v) * 2.2f) * groove * (0.85f + 0.15f * (float)random.NextDouble());
                            tread.SetPixel(x, y, new Color(0.06f, 0.055f, 0.05f, alpha * 0.8f));
                        }
                    }
                    tread.Apply(false, true);
                }
                return tread;
            }
        }

        /// <summary>A material on the sprite shader with <paramref name="texture"/> and a colour (HDR for glows).</summary>
        public static Material Make(string name, Texture texture, Color color)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning($"[Death Race] The shader {ShaderName} is missing: effects fall back to the default material.");
                }
                return null;
            }
            var material = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
            if (texture != null)
            {
                material.mainTexture = texture;
            }
            material.color = color;
            return material;
        }

        /// <summary>A square texture whose alpha is <paramref name="alpha"/>(radius 0..1, angle 0..1) and whose colour is <paramref name="color"/>.</summary>
        private static Texture2D Radial(string name, int size, System.Func<float, float, float> alpha, Color? color = null)
        {
            Color tint = color ?? Color.white;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 1f) % 1f;
                    texture.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, r >= 1f ? 0f : Mathf.Clamp01(alpha(r, a))));
                }
            }
            texture.Apply(true, true);
            return texture;
        }
    }
}
