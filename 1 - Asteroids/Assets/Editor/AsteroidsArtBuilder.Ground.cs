using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// A world of the strike mode as the ground builders see it: its tile kinds and variants, its six detail layers, its
    /// liquid, lighting and prop colours, and the natural patches every one of its tiles shares (so tiles join without a
    /// seam).
    /// </summary>
    internal sealed class GroundTheme
    {
        public string Name;
        public int Seed;
        public Color Accent;
        public (TerrainKind kind, int variants)[] Kinds;
        /// <summary>The detail layers: base, alt, rock, asphalt, concrete, liquid pattern.</summary>
        public Func<GroundTheme, SpaceTextures.Surface>[] Layers;
        public Color Shallow;
        public Color Deep;
        public Color Foam;
        public Color LiquidGlow = Color.black;
        public Color CrackGlow = Color.black;
        public float Waves = 1f;
        public float Gloss = 0.6f;
        public Color Sun = Color.white;
        public float SunIntensity = 1.5f;
        public Color Ambient;
        public Color Shadow = new Color(0f, 0f, 0f, 0.45f);
        public Color CraterTint = new Color(0.25f, 0.2f, 0.16f, 1f);
        public bool Night;
        /// <summary>How much of the ground the alt layer and the rock layer cover where the tile kinds meet (0..1).</summary>
        public float AltShare = 0.35f;
        public float RockShare = 0.1f;
        /// <summary>Strength of the large-scale tint variation.</summary>
        public float Variation = 0.12f;
        /// <summary>Natural patches follow a 2.5 m grid of plates (the station).</summary>
        public bool Blocky;
        /// <summary>Strength of the dune shading of the open ground (0 for none).</summary>
        public float Dunes;
        /// <summary>Roughness (m) of the paved edges: 0.05 for kerbed asphalt, more for dirt roads.</summary>
        public float PavedRoughness = 0.05f;
        /// <summary>Colours of the props: rocks, second rocks, walls, roofs, vegetation, second vegetation.</summary>
        public GroundSwatch Stone = GroundSwatch.Stone;
        public GroundSwatch Stone2 = GroundSwatch.StoneDark;
        public GroundSwatch Walls = GroundSwatch.Tan;
        public GroundSwatch Roof = GroundSwatch.ConcreteDark;
        public GroundSwatch Plant = GroundSwatch.Olive;
        public GroundSwatch Plant2 = GroundSwatch.OliveDark;
        /// <summary>Dresses the open ground of a tile (the theme's small scattered props).</summary>
        public Action<TilePlan, System.Random, float> Scatter;
        /// <summary>Buildings beside a base pad.</summary>
        public Action<TilePlan, System.Random> BaseDressing;

        public string Folder => $"Art/Ground/{Name}";
    }


    /// <summary>
    /// The layout of one terrain tile (design 4.1): the four fields of the ground shader (signed distances in metres,
    /// positive inside), the concrete share of its paving, its tint and its props. Built the same way by the art builder
    /// (textures, prop mesh) and the prefab builder (lights).
    /// </summary>
    internal sealed class TilePlan
    {
        public GroundTheme Theme;
        public TerrainKind Kind;
        public int Variant;
        public Func<Vector2, float> Paved = p => -20f;
        public Func<Vector2, float> Liquid = p => -20f;
        public Func<Vector2, float> Rock;
        public Func<Vector2, float> Alt;
        public Func<Vector2, float> Concrete = p => 0f;
        /// <summary>Extra tint (multiplied), for farmland patches and scorched spots; null for none.</summary>
        public Func<Vector2, Color> Tint;
        /// <summary>Where props must not stand (centre and clearance): the areas kept for units, roads and water.</summary>
        public Func<Vector2, float, bool> Blocked = (p, r) => false;
        public readonly List<PropSpot> Props = new List<PropSpot>();
        /// <summary>Shallow craters shaded into the colour map (centre, radius).</summary>
        public readonly List<(Vector2 at, float radius)> Craters = new List<(Vector2, float)>();
        /// <summary>Night lights: position in tile metres (z toward the camera is negative), size, colour.</summary>
        public readonly List<(Vector3 position, float size, Color color)> Lights = new List<(Vector3, float, Color)>();

        public string Name => $"{Kind}{Variant}";
        public string Folder => $"{Theme.Folder}/Tiles";


        /// <summary>Adds a prop unless it would stand on a blocked spot or cross the tile's ends; returns whether it was added.</summary>
        public bool Place(PropKind kind, Vector2 at, float yaw, Vector3 size, GroundSwatch main, GroundSwatch accent, int seed, bool check = true)
        {
            float reach = Mathf.Max(size.x, size.z) * 0.5f;
            if (check && (Mathf.Abs(at.y) > StrikeRules.TileLength * 0.5f - reach - 0.2f || Mathf.Abs(at.x) > StrikeRules.TileWidth * 0.5f - reach
                || Blocked(at, reach)))
            {
                return false;
            }
            Props.Add(new PropSpot
            {
                Kind = kind, Position = at, Yaw = yaw, Size = size, Main = main, Accent = accent, Seed = seed,
                Shadow = kind != PropKind.Mark
            });
            return true;
        }


        /// <summary>Marks the last prop as lit at night (windows and roof lights glow).</summary>
        public void LightLast(bool night)
        {
            if (night && Props.Count > 0)
            {
                PropSpot spot = Props[Props.Count - 1];
                spot.Lit = true;
                Props[Props.Count - 1] = spot;
            }
        }


        /// <summary>Adds up to <paramref name="count"/> craters that keep clear of each other and of blocked spots.</summary>
        public void AddCraters(System.Random random, int count, float minRadius, float maxRadius, float minX = 0f)
        {
            for (int i = 0; i < count * 6 && count > 0; i++)
            {
                float r = minRadius + (float)random.NextDouble() * (maxRadius - minRadius);
                float x = minX + (float)random.NextDouble() * (30f - minX);
                var at = new Vector2(random.NextDouble() < 0.5 ? -x : x, ((float)random.NextDouble() - 0.5f) * (StrikeRules.TileLength - r * 3f));
                bool free = !Blocked(at, r * 1.3f);
                foreach ((Vector2 other, float size) in Craters)
                {
                    free &= (other - at).magnitude > (r + size) * 1.4f;
                }
                if (free)
                {
                    Craters.Add((at, r));
                    count--;
                }
            }
        }


        /// <summary>Scatters up to <paramref name="count"/> props at random free spots in the x range.</summary>
        public void Sprinkle(System.Random random, int count, float minX, float maxX, Func<System.Random, (PropKind kind, Vector3 size, GroundSwatch main, GroundSwatch accent)> pick,
            bool bothSides = true)
        {
            for (int i = 0; i < count * 3 && count > 0; i++)
            {
                (PropKind kind, Vector3 size, GroundSwatch main, GroundSwatch accent) = pick(random);
                float x = minX + (float)random.NextDouble() * (maxX - minX);
                if (bothSides && random.NextDouble() < 0.5)
                {
                    x = -x;
                }
                float y = ((float)random.NextDouble() - 0.5f) * StrikeRules.TileLength;
                if (Place(kind, new Vector2(x, y), (float)random.NextDouble() * 360f, size, main, accent, random.Next()))
                {
                    count--;
                }
            }
        }
    }


    /// <summary>The ground art: terrain textures and models, ground units, props and decals (design 4.1, 4.2).</summary>
    internal static partial class AsteroidsArtBuilder
    {
        /// <summary>Range (m) of the four mask fields: paved, liquid, rock, alt (the ground materials use the same).</summary>
        public static readonly Vector4 MaskRanges = new Vector4(1.5f, 6f, 3f, 3f);

        public static readonly string[] LayerNames = { "Base", "Alt", "Rock", "Asphalt", "Concrete", "Liquid" };

        private static Shader GroundShader => AssetDatabase.LoadAssetAtPath<Shader>(AsteroidsAssets.Path("Art/Shaders/Ground.shader"));

        /// <summary>Builds only the ground art (for quick iterations in batch).</summary>
        internal static void BuildGroundArtOnly()
        {
            BuildGroundArt();
            AssetDatabase.SaveAssets();
        }


        static partial void BuildGroundArt()
        {
            AsteroidsAssets.SaveTexture(SpaceTextures.GroundPalette(false), "Art/Ground/Textures/GroundPalette.png", PaletteImporter);
            AsteroidsAssets.SaveTexture(SpaceTextures.GroundPalette(true), "Art/Ground/Textures/GroundPaletteEmission.png", PaletteImporter);
            AsteroidsAssets.SaveTexture(SpaceTextures.ShadowBlob(), "Art/Ground/Textures/ShadowBlob.png", DecalImporter);
            AsteroidsAssets.SaveTexture(SpaceTextures.CraterDecal(false, 71), "Art/Ground/Textures/Crater.png", DecalImporter);
            AsteroidsAssets.SaveTexture(SpaceTextures.CraterDecal(true, 83), "Art/Ground/Textures/Rubble.png", DecalImporter);
            var palette = AsteroidsAssets.Load<Texture2D>("Art/Ground/Textures/GroundPalette.png");
            var emission = AsteroidsAssets.Load<Texture2D>("Art/Ground/Textures/GroundPaletteEmission.png");
            AsteroidsAssets.SaveMaterial("Art/Ground/Materials/GroundPalette.mat", Lit, m => SetupLit(m, Color.white, palette, null, 0.2f, 0.05f, emission, new Color(2f, 2f, 2f)));
            AsteroidsAssets.SaveMaterial("Art/Ground/Materials/UnitShadow.mat", Glow, m => DecalMaterial(m, AsteroidsAssets.Load<Texture2D>("Art/Ground/Textures/ShadowBlob.png"),
                new Color(0f, 0f, 0f, 0.5f)));
            AsteroidsAssets.SaveMaterial("Art/Ground/Materials/Telegraph.mat", Glow, m =>
            {
                m.SetTexture("_BaseMap", Texture("Glow"));
                m.SetColor("_BaseColor", new Color(3f, 0.35f, 0.3f, 1f));
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            });
            AsteroidsAssets.SaveMaterial("Art/Ground/Materials/LampGlow.mat", Glow, m =>
            {
                m.SetTexture("_BaseMap", Texture("Glow"));
                m.SetColor("_BaseColor", new Color(1f, 0.8f, 0.5f, 0.5f));
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            });
            SaveMesh(SpaceModels.GroundQuad(), "Art/Ground/Models/GroundTile.asset");

            foreach (GroundTheme theme in GroundThemes)
            {
                BuildTheme(theme);
            }
            BuildGroundUnitMeshes();
        }


        /// <summary>The generated material of a tile (null before the art is built).</summary>
        public static Material TileMaterial(TilePlan plan)
        {
            return AsteroidsAssets.Load<Material>($"{plan.Folder}/{plan.Name}.mat");
        }


        /// <summary>The generated prop mesh of a tile (null when it has no props).</summary>
        public static Mesh TileProps(TilePlan plan)
        {
            return AsteroidsAssets.Load<Mesh>($"{plan.Folder}/{plan.Name}Props.asset");
        }


        /// <summary>The crater material of a theme.</summary>
        public static Material CraterMaterial(GroundTheme theme)
        {
            return AsteroidsAssets.Load<Material>($"{theme.Folder}/Crater.mat");
        }


        private static void BuildTheme(GroundTheme theme)
        {
            var layers = new Texture2D[LayerNames.Length];
            for (int i = 0; i < LayerNames.Length; i++)
            {
                SpaceTextures.Surface surface = theme.Layers[i](theme);
                layers[i] = AsteroidsAssets.SaveTexture(SpaceTextures.Detail(surface), $"{theme.Folder}/Layers/{LayerNames[i]}.png", LayerImporter);
            }
            Color crater = theme.CraterTint;
            AsteroidsAssets.SaveMaterial($"{theme.Folder}/Crater.mat", Glow, m => DecalMaterial(m, AsteroidsAssets.Load<Texture2D>("Art/Ground/Textures/Crater.png"), crater));
            AsteroidsAssets.SaveMaterial($"{theme.Folder}/Rubble.mat", Glow, m => DecalMaterial(m, AsteroidsAssets.Load<Texture2D>("Art/Ground/Textures/Rubble.png"), crater));
            foreach ((TerrainKind kind, int variants) in theme.Kinds)
            {
                for (int v = 0; v < variants; v++)
                {
                    BuildTile(Plan(theme, kind, v), layers);
                }
            }
        }


        private static void BuildTile(TilePlan plan, Texture2D[] layers)
        {
            GroundTheme theme = plan.Theme;
            Func<Vector2, float> rock = plan.Rock;
            Func<Vector2, float> alt = plan.Alt;
            Texture2D masks = AsteroidsAssets.SaveTexture(SpaceTextures.TileMasks(p => new Vector4(plan.Paved(p), plan.Liquid(p), rock(p), alt(p)), MaskRanges),
                $"{plan.Folder}/{plan.Name}Masks.png", DataImporter);
            Texture2D colors = AsteroidsAssets.SaveTexture(SpaceTextures.TileColors(p => TileTint(plan, p)), $"{plan.Folder}/{plan.Name}Color.png", DataImporter);
            AsteroidsAssets.SaveMaterial($"{plan.Folder}/{plan.Name}.mat", GroundShader, m =>
            {
                m.SetTexture("_Masks", masks);
                m.SetTexture("_ColorMap", colors);
                m.SetTexture("_BaseTex", layers[0]);
                m.SetTexture("_AltTex", layers[1]);
                m.SetTexture("_RockTex", layers[2]);
                m.SetTexture("_RoadTex", layers[3]);
                m.SetTexture("_PavedTex", layers[4]);
                m.SetTexture("_LiquidTex", layers[5]);
                m.SetColor("_ShallowColor", theme.Shallow);
                m.SetColor("_DeepColor", theme.Deep);
                m.SetColor("_FoamColor", theme.Foam);
                m.SetColor("_LiquidEmission", theme.LiquidGlow);
                m.SetColor("_CrackEmission", theme.CrackGlow);
                m.SetVector("_Ranges", MaskRanges);
                m.SetVector("_Edges", new Vector4(theme.PavedRoughness, 0.25f, theme.Blocky ? 0.04f : 0.7f, theme.Blocky ? 0.04f : 0.6f));
                m.SetFloat("_DetailSize", 5f);
                m.SetFloat("_Waves", theme.Waves);
                m.SetFloat("_Gloss", theme.Gloss);
                m.enableInstancing = false;
            });
            var props = new MeshBuilder();
            foreach (PropSpot spot in plan.Props)
            {
                SpaceModels.Prop(props, spot);
            }
            string propsPath = $"{plan.Folder}/{plan.Name}Props.asset";
            if (props.VertexCount > 0)
            {
                AsteroidsAssets.SaveMesh(props, propsPath);
            }
            else if (TileProps(plan) != null)
            {
                // A tile that lost its props loses the old mesh too: the prefab builder adds the props of any mesh it finds.
                AssetDatabase.DeleteAsset(AsteroidsAssets.Path(propsPath));
            }
        }


        /// <summary>The colour map of a tile at <paramref name="p"/>: the theme's variation, the plan's tint and the props' baked shadows.</summary>
        private static Color TileTint(TilePlan plan, Vector2 p)
        {
            GroundTheme theme = plan.Theme;
            float u = p.x / StrikeRules.TileWidth + 0.5f;
            float v = p.y / StrikeRules.TileLength + 0.5f;
            float broad = SpaceTextures.PFbm(u, v, 5, 2, 3, theme.Seed + 401) - 0.5f;
            float hue = SpaceTextures.PFbm(u, v, 8, 3, 2, theme.Seed + 409) - 0.5f;
            float k = 1f + broad * theme.Variation * 2f;
            var tint = new Color(k * (1f + hue * theme.Variation), k, k * (1f - hue * theme.Variation), 1f);
            if (plan.Tint != null)
            {
                Color extra = plan.Tint(p);
                tint = new Color(tint.r * extra.r, tint.g * extra.g, tint.b * extra.b, 1f);
            }
            float open = 1f - Mathf.Clamp01(plan.Paved(p) + 0.5f) - Mathf.Clamp01(plan.Liquid(p) + 0.5f);
            var sun = new Vector2(SpaceTextures.GroundLight.x, SpaceTextures.GroundLight.y);
            if (theme.Dunes > 0f && open > 0f)
            {
                const float e = 0.25f;
                float Dune(Vector2 q) => SpaceTextures.PFbm(q.x / StrikeRules.TileWidth + 0.5f, q.y / StrikeRules.TileLength + 0.5f, 6, 2, 3, theme.Seed + 419) * 2.2f;
                var slope = new Vector2(Dune(p + new Vector2(e, 0f)) - Dune(p - new Vector2(e, 0f)), Dune(p + new Vector2(0f, e)) - Dune(p - new Vector2(0f, e))) / (2f * e);
                float shade = 1f - Vector2.Dot(slope, sun) * theme.Dunes;
                tint *= Mathf.Lerp(1f, Mathf.Clamp(shade, 0.7f, 1.3f), Mathf.Clamp01(open));
            }
            foreach ((Vector2 at, float radius) in plan.Craters)
            {
                Vector2 d = p - at;
                float r = d.magnitude / radius;
                if (r > 1.6f || r < 0.0001f)
                {
                    continue;
                }
                Vector2 outward = d / d.magnitude;
                float wall = SpaceTextures.Step(0.25f, 1f, r) * (1f - SpaceTextures.Step(0.98f, 1.02f, r));
                float rim = r > 1f ? 1f - SpaceTextures.Step(1f, 1.6f, r) : 0f;
                float shade = 1f + Vector2.Dot(-outward, sun) * wall * 0.85f + Vector2.Dot(outward, sun) * rim * 0.5f;
                float floor = r < 1f ? Mathf.Lerp(0.82f, 1f, SpaceTextures.Step(0.2f, 0.9f, r)) : 1f + rim * 0.08f;
                tint *= Mathf.Clamp(shade, 0.35f, 1.6f) * floor;
            }
            float light = 1f;
            Vector2 step = SpaceTextures.ShadowStep;
            foreach (PropSpot spot in plan.Props)
            {
                if (!spot.Shadow)
                {
                    continue;
                }
                float reach = Mathf.Max(spot.Size.x, spot.Size.z) * 0.5f + spot.Size.y * step.magnitude + 0.6f;
                if ((p - spot.Position).sqrMagnitude > reach * reach)
                {
                    continue;
                }
                light *= 1f - 0.5f * ShadowCover(spot, p, step);
            }
            tint *= light;
            tint.a = Mathf.Clamp01(plan.Concrete(p));
            return tint;
        }


        /// <summary>How much of <paramref name="p"/> lies in the shadow a prop casts (its footprint swept along the sun), soft edged.</summary>
        private static float ShadowCover(PropSpot spot, Vector2 p, Vector2 step)
        {
            float yaw = spot.Yaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(yaw);
            float sin = Mathf.Sin(yaw);
            Vector2 d = p - spot.Position;
            var local = new Vector2(d.x * cos + d.y * sin, -d.x * sin + d.y * cos);
            var sweep = new Vector2(step.x * cos + step.y * sin, -step.x * sin + step.y * cos) * spot.Size.y;
            float hx = Mathf.Max(0.15f, spot.Size.x * 0.5f);
            float hy = Mathf.Max(0.15f, spot.Size.z * 0.5f);
            bool box = spot.Kind == PropKind.Building || spot.Kind == PropKind.Container || spot.Kind == PropKind.Hangar
                || spot.Kind == PropKind.Wall || spot.Kind == PropKind.Solar || spot.Kind == PropKind.Vent || spot.Kind == PropKind.Tower
                || spot.Kind == PropKind.Tent || spot.Kind == PropKind.Bridge || spot.Kind == PropKind.Car;
            float best = 0f;
            for (int i = 0; i <= 6; i++)
            {
                Vector2 q = local - sweep * (i / 6f);
                float inside;
                if (box)
                {
                    float ox = Mathf.Abs(q.x) - hx;
                    float oy = Mathf.Abs(q.y) - hy;
                    inside = -Mathf.Max(ox, oy);
                }
                else
                {
                    float e = Mathf.Sqrt((q.x / hx) * (q.x / hx) + (q.y / hy) * (q.y / hy));
                    inside = (1f - e) * Mathf.Min(hx, hy);
                }
                best = Mathf.Max(best, Mathf.Clamp01(inside / 0.35f + 0.5f));
            }
            return best;
        }

        // ------------------------------------------------------------------ themes

        private static GroundTheme[] themes;

        /// <summary>The six worlds of design 4.1, in the order of their assets.</summary>
        public static GroundTheme[] GroundThemes => themes ??= MakeThemes();


        /// <summary>The rotation of the sun of every strike theme: it shines from <see cref="SpaceTextures.GroundLight"/>.</summary>
        public static Vector3 StrikeSunAngles
        {
            get
            {
                Vector3 toward = SpaceTextures.GroundLight;
                Vector3 angles = Quaternion.LookRotation(new Vector3(-toward.x, -toward.y, toward.z), Vector3.up).eulerAngles;
                return new Vector3(Mathf.Round(angles.x * 10f) / 10f, Mathf.Round(angles.y * 10f) / 10f, 0f);
            }
        }


        private static GroundTheme[] MakeThemes()
        {
            Color H(string hex) => SpaceTextures.Hex(hex);
            var desert = new GroundTheme
            {
                Name = "Desert", Seed = 11, Accent = H("E8B25A"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Rough, 2), (TerrainKind.Road, 3), (TerrainKind.Base, 2), (TerrainKind.Coast, 2), (TerrainKind.Sea, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Grainy(H("9A6A3E"), H("C99E68"), 0.03f, 1f, 0f, 101),
                    t => SpaceTextures.Cracked(H("B8966A"), H("A07E56"), H("6E5236"), 6, 0.03f, 103),
                    t => SpaceTextures.Rocky(H("7E4428"), H("C98A5A"), H("3A2014"), 0.45f, 0.35f, 105),
                    t => SpaceTextures.Asphalt(H("2C2D30"), H("58585A"), 107),
                    t => SpaceTextures.Slabs(H("A8A398"), H("C8C3B6"), H("6C675E"), 2, 0.22f, false, 109),
                    t => SpaceTextures.Ripples(111)
                },
                Shallow = H("3FB0A8"), Deep = H("0C3C62"), Foam = new Color(0.92f, 0.95f, 0.9f, 0.8f),
                Sun = new Color(1f, 0.93f, 0.82f), SunIntensity = 1.2f, Ambient = new Color(0.36f, 0.32f, 0.29f),
                Shadow = new Color(0.12f, 0.07f, 0.02f, 0.45f), CraterTint = new Color(0.34f, 0.25f, 0.17f, 1f),
                AltShare = 0.22f, RockShare = 0.05f, Variation = 0.1f, Dunes = 0.9f,
                Stone = GroundSwatch.Clay, Stone2 = GroundSwatch.SandDark, Walls = GroundSwatch.Tan, Roof = GroundSwatch.ConcreteDark,
                Plant = GroundSwatch.Olive, Plant2 = GroundSwatch.OliveDark, Scatter = DesertScatter, BaseDressing = MilitaryBase
            };
            var jungle = new GroundTheme
            {
                Name = "Jungle", Seed = 23, Accent = H("7CCB5A"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Rough, 2), (TerrainKind.Road, 3), (TerrainKind.Field, 2), (TerrainKind.River, 2), (TerrainKind.Lake, 2), (TerrainKind.Base, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Foliage(H("183614"), H("4E8C32"), H("2E2616"), 201),
                    t => SpaceTextures.Rows(H("56402A"), H("4E7A2C"), 203),
                    t => Wide(SpaceTextures.Grainy(H("4A3420"), H("8A6644"), 0.08f, 0f, 0.3f, 205)),
                    t => SpaceTextures.Grainy(H("5E4028"), H("8E6A48"), 0.05f, 0f, 0.15f, 207),
                    t => SpaceTextures.Slabs(H("8C8C84"), H("ACAAA0"), H("58584F"), 2, 0.25f, false, 209),
                    t => SpaceTextures.Ripples(211)
                },
                Shallow = H("4A5E36"), Deep = H("16301F"), Foam = new Color(0.7f, 0.74f, 0.6f, 0.22f), Gloss = 0.3f, PavedRoughness = 0.35f,
                Sun = new Color(1f, 0.97f, 0.9f), SunIntensity = 1.35f, Ambient = new Color(0.36f, 0.42f, 0.38f),
                Shadow = new Color(0f, 0.05f, 0f, 0.5f), CraterTint = new Color(0.22f, 0.17f, 0.1f, 1f),
                AltShare = 0f, RockShare = 0.12f, Variation = 0.14f,
                Stone = GroundSwatch.StoneDark, Stone2 = GroundSwatch.Mud, Walls = GroundSwatch.Wood, Roof = GroundSwatch.RoofGreen,
                Plant = GroundSwatch.Leaf, Plant2 = GroundSwatch.LeafDark, Scatter = JungleScatter, BaseDressing = MilitaryBase
            };
            var city = new GroundTheme
            {
                Name = "City", Seed = 37, Accent = H("6FA8FF"), Night = true,
                Kinds = new[] { (TerrainKind.City, 2), (TerrainKind.Road, 3), (TerrainKind.Base, 2), (TerrainKind.River, 2), (TerrainKind.Plain, 3) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Foliage(H("34482A"), H("7A9050"), H("54483A"), 301),
                    t => SpaceTextures.Slabs(H("7A6E62"), H("988C7E"), H("463E36"), 8, 0.4f, false, 303),
                    t => SpaceTextures.Rocky(H("58534E"), H("8C867E"), H("262220"), 0.3f, 0f, 305),
                    t => SpaceTextures.Asphalt(H("24252A"), H("46474C"), 307),
                    t => SpaceTextures.Slabs(H("8A8882"), H("A8A59E"), H("53514C"), 4, 0.5f, false, 309),
                    t => SpaceTextures.Ripples(311)
                },
                Shallow = H("30505E"), Deep = H("0A1826"), Foam = new Color(0.5f, 0.6f, 0.7f, 0.25f), Gloss = 0.45f,
                Sun = new Color(0.62f, 0.72f, 1f), SunIntensity = 0.95f, Ambient = new Color(0.24f, 0.27f, 0.4f),
                Shadow = new Color(0f, 0f, 0.02f, 0.35f), CraterTint = new Color(0.13f, 0.11f, 0.1f, 1f),
                AltShare = 0f, RockShare = 0.05f, Variation = 0.1f,
                Stone = GroundSwatch.StoneDark, Stone2 = GroundSwatch.ConcreteDark, Walls = GroundSwatch.ConcreteLight, Roof = GroundSwatch.ConcreteDark,
                Plant = GroundSwatch.Leaf, Plant2 = GroundSwatch.LeafDark, Scatter = CityScatter, BaseDressing = MilitaryBase
            };
            var moon = new GroundTheme
            {
                Name = "Moon", Seed = 41, Accent = H("C8D2DC"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Rough, 2), (TerrainKind.Base, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Grainy(H("5E5D5A"), H("9A9993"), 0.06f, 0f, 0.5f, 401),
                    t => SpaceTextures.Grainy(H("434346"), H("6A6A6C"), 0.03f, 0f, 0.35f, 403),
                    t => SpaceTextures.Rocky(H("5A5956"), H("C2C0B8"), H("18181A"), 0.55f, 0f, 405),
                    t => SpaceTextures.Slabs(H("383C42"), H("4E545C"), H("1C2024"), 5, 0.2f, true, 407),
                    t => SpaceTextures.Slabs(H("888C90"), H("A6AAAE"), H("4E5258"), 2, 0.3f, true, 409),
                    t => SpaceTextures.Ripples(411)
                },
                Shallow = H("5A5A5E"), Deep = H("2A2A2E"), Foam = new Color(0.6f, 0.6f, 0.6f, 0f),
                Sun = new Color(1f, 1f, 0.98f), SunIntensity = 1.45f, Ambient = new Color(0.17f, 0.18f, 0.23f),
                Shadow = new Color(0f, 0f, 0f, 0.6f), CraterTint = new Color(0.22f, 0.22f, 0.22f, 1f),
                AltShare = 0.35f, RockShare = 0.06f, Variation = 0.08f,
                Stone = GroundSwatch.Regolith, Stone2 = GroundSwatch.RegolithDark, Walls = GroundSwatch.Metal, Roof = GroundSwatch.Plate,
                Plant = GroundSwatch.Regolith, Plant2 = GroundSwatch.RegolithDark, Scatter = MoonScatter, BaseDressing = ColonyBase
            };
            var volcanic = new GroundTheme
            {
                Name = "Volcanic", Seed = 53, Accent = H("FF7A30"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Rough, 2), (TerrainKind.Lake, 2), (TerrainKind.Base, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Grainy(H("38322F"), H("6A605A"), 0.04f, 0f, 0.2f, 501),
                    t => SpaceTextures.Cracked(H("3C3536"), H("2A2426"), H("0E0808"), 7, 0.06f, 503),
                    t => SpaceTextures.Rocky(H("1C181A"), H("4C4446"), H("080404"), 0.5f, 0f, 505),
                    t => SpaceTextures.Asphalt(H("202024"), H("3C3C40"), 507),
                    t => SpaceTextures.Slabs(H("6C6864"), H("8A8680"), H("3C3834"), 2, 0.7f, false, 509),
                    t => SpaceTextures.LavaCrust(511)
                },
                Shallow = H("4A2412"), Deep = H("24120E"), Foam = new Color(1f, 0.55f, 0.15f, 0.55f),
                LiquidGlow = new Color(7f, 2f, 0.45f, 1f), CrackGlow = new Color(3.2f, 0.8f, 0.2f, 1f), Waves = 0.25f, Gloss = 0.1f,
                Sun = new Color(1f, 0.84f, 0.7f), SunIntensity = 1.5f, Ambient = new Color(0.38f, 0.27f, 0.25f),
                Shadow = new Color(0.05f, 0f, 0f, 0.5f), CraterTint = new Color(0.1f, 0.08f, 0.08f, 1f),
                AltShare = 0.18f, RockShare = 0.14f, Variation = 0.1f,
                Stone = GroundSwatch.Basalt, Stone2 = GroundSwatch.Ash, Walls = GroundSwatch.SteelDark, Roof = GroundSwatch.Rust,
                Plant = GroundSwatch.Ash, Plant2 = GroundSwatch.Basalt, Scatter = VolcanicScatter, BaseDressing = IndustrialBase
            };
            var station = new GroundTheme
            {
                Name = "Station", Seed = 67, Accent = H("5CE0FF"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Base, 2), (TerrainKind.Rough, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Slabs(H("7C8690"), H("9AA4AE"), H("3A4046"), 2, 0.25f, true, 601),
                    t => SpaceTextures.Slabs(H("4A525C"), H("5E6874"), H("22282E"), 2, 0.2f, true, 603),
                    t => Wide(SpaceTextures.Slabs(H("30363E"), H("444C56"), H("14181C"), 4, 0.3f, true, 605)),
                    t => SpaceTextures.Asphalt(H("282A2E"), H("404448"), 607),
                    t => SpaceTextures.Slabs(H("98A0A6"), H("B6BCC2"), H("565C62"), 1, 0.2f, true, 609),
                    t => SpaceTextures.Ripples(611)
                },
                Shallow = H("2A8C9C"), Deep = H("0A3848"), Foam = new Color(0.6f, 1f, 1f, 0.4f), LiquidGlow = new Color(0.1f, 0.8f, 1.1f, 1f),
                Sun = new Color(0.9f, 0.95f, 1f), SunIntensity = 1.4f, Ambient = new Color(0.22f, 0.25f, 0.32f),
                Shadow = new Color(0f, 0f, 0.02f, 0.55f), CraterTint = new Color(0.12f, 0.12f, 0.14f, 1f),
                AltShare = 0.28f, RockShare = 0.1f, Variation = 0.05f, Blocky = true,
                Stone = GroundSwatch.PlateDark, Stone2 = GroundSwatch.SteelDark, Walls = GroundSwatch.Plate, Roof = GroundSwatch.PlateDark,
                Plant = GroundSwatch.PipeDark, Plant2 = GroundSwatch.Pipe, Scatter = StationScatter, BaseDressing = StationBase
            };
            return new[] { desert, jungle, city, moon, volcanic, station };
        }


        /// <summary>A detail layer the ground shader samples over 10 m (the rock slot) that was written for 5 m.</summary>
        private static SpaceTextures.Surface Wide(SpaceTextures.Surface surface)
        {
            surface.Meters = 10f;
            return surface;
        }

        // ------------------------------------------------------------------ tile plans

        /// <summary>
        /// The layout of tile <paramref name="kind"/> / <paramref name="variant"/> of <paramref name="theme"/> (design 4.1):
        /// deterministic, so the art and prefab builders get the same one.
        /// </summary>
        public static TilePlan Plan(GroundTheme theme, TerrainKind kind, int variant)
        {
            var plan = new TilePlan { Theme = theme, Kind = kind, Variant = variant };
            var random = new System.Random(theme.Seed * 1009 + (int)kind * 101 + variant * 13 + 5);
            float alt = theme.AltShare;
            float rock = theme.RockShare;
            switch (kind)
            {
                case TerrainKind.Rough:
                    rock += 0.3f;
                    alt += 0.1f;
                    break;
                case TerrainKind.Plain:
                    rock *= variant == 1 ? 1.5f : 0.8f;
                    break;
            }
            plan.Alt = p => Natural(theme, p, theme.AltShare, alt, 401);
            plan.Rock = p => Natural(theme, p, theme.RockShare, rock, 409);
            switch (kind)
            {
                case TerrainKind.Plain: PlainTile(plan, random); break;
                case TerrainKind.Rough: RoughTile(plan, random); break;
                case TerrainKind.Road: RoadTile(plan, random); break;
                case TerrainKind.River: RiverTile(plan, random); break;
                case TerrainKind.Lake: LakeTile(plan, random); break;
                case TerrainKind.Coast: CoastTile(plan, random); break;
                case TerrainKind.Sea: SeaTile(plan, random); break;
                case TerrainKind.Base: BaseTile(plan, random); break;
                case TerrainKind.City: CityTile(plan, random); break;
                case TerrainKind.Field: FieldTile(plan, random); break;
            }
            return plan;
        }


        /// <summary>
        /// A natural patch field of the theme (metres, positive inside): its share of the ground goes from
        /// <paramref name="edgeShare"/> at the tile's ends (the same in every tile of the theme) to <paramref name="midShare"/>
        /// in its middle. The noise repeats every 20 m along the level.
        /// </summary>
        private static float Natural(GroundTheme theme, Vector2 p, float edgeShare, float midShare, int salt)
        {
            float fade = SpaceTextures.Step(StrikeRules.TileLength * 0.5f, StrikeRules.TileLength * 0.5f - 4f, Mathf.Abs(p.y));
            float share = Mathf.Lerp(edgeShare, midShare, fade);
            if (share <= 0.001f)
            {
                return -10f;
            }
            Vector2 at = p;
            float cellEdge = 0f;
            if (theme.Blocky)
            {
                const float cell = 2.5f;
                at = new Vector2((Mathf.Floor(p.x / cell) + 0.5f) * cell, (Mathf.Floor(p.y / cell) + 0.5f) * cell);
                Vector2 f = (p - at) / cell;
                cellEdge = (0.5f - Mathf.Max(Mathf.Abs(f.x), Mathf.Abs(f.y))) * cell;
            }
            float u = at.x / StrikeRules.TileWidth + 0.5f;
            float v = at.y / StrikeRules.TileLength + 0.5f;
            float threshold = 0.5f + (0.5f - share) * 0.36f;
            if (theme.Blocky)
            {
                return BlockyField(theme, p, threshold, salt);
            }
            float n = SpaceTextures.PFbm(u, v, 16, 5, 4, theme.Seed + salt);
            return (n - threshold) * 20f;
        }


        /// <summary>A union of 2.5 m plates chosen by noise, as a signed distance without seams between chosen plates.</summary>
        private static float BlockyField(GroundTheme theme, Vector2 p, float threshold, int salt)
        {
            const float cell = 2.5f;
            int cx = Mathf.FloorToInt(p.x / cell);
            int cy = Mathf.FloorToInt(p.y / cell);
            bool Chosen(int x, int y)
            {
                float u = (x + 0.5f) * cell / StrikeRules.TileWidth + 0.5f;
                float v = (y + 0.5f) * cell / StrikeRules.TileLength + 0.5f;
                return SpaceTextures.PFbm(u, v, 16, 5, 3, theme.Seed + salt) > threshold;
            }
            bool inside = Chosen(cx, cy);
            float best = cell;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if ((ox == 0 && oy == 0) || Chosen(cx + ox, cy + oy) == inside)
                    {
                        continue;
                    }
                    float x0 = (cx + ox) * cell;
                    float y0 = (cy + oy) * cell;
                    float dx = Mathf.Max(Mathf.Max(x0 - p.x, p.x - (x0 + cell)), 0f);
                    float dy = Mathf.Max(Mathf.Max(y0 - p.y, p.y - (y0 + cell)), 0f);
                    best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy));
                }
            }
            return inside ? best : -best;
        }


        private static void PlainTile(TilePlan plan, System.Random random)
        {
            plan.Theme.Scatter?.Invoke(plan, random, 1f);
        }


        private static void RoughTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            bool clear = plan.Variant % 2 == 0;
            if (clear)
            {
                plan.Blocked = (p, r) => Mathf.Abs(p.x) < 14f + r;
            }
            System.Func<Vector2, float, bool> blocked = plan.Blocked;
            if (clear)
            {
                plan.Blocked = (p, r) => false;
                plan.AddCraters(random, 3, 0.8f, 1.5f, 0f);
                plan.Blocked = blocked;
            }
            plan.AddCraters(random, clear ? 2 : 4, 1.6f, 3.2f, clear ? 16f : 2f);
            plan.Blocked = (p, r) => blocked(p, r) || CraterAt(plan, p, r);
            if (theme.Name == "Station")
            {
                StationRough(plan, random, clear);
                return;
            }
            theme.Scatter?.Invoke(plan, random, 1.4f);
            float from = clear ? 17f : 3f;
            for (int i = 0; i < 7; i++)
            {
                float x = from + (float)random.NextDouble() * (29f - from);
                x = random.NextDouble() < 0.5 ? -x : x;
                var at = new Vector2(x, ((float)random.NextDouble() - 0.5f) * 12f);
                plan.Place(PropKind.Ridge, at, (float)random.NextDouble() * 360f, new Vector3(4f + (float)random.NextDouble() * 3f, 0.7f, 1.6f),
                    theme.Stone, theme.Stone2, random.Next());
            }
            plan.Sprinkle(random, 14, from, 30f, r => (PropKind.Boulders, new Vector3(1.6f, 0.6f, 1.6f) * (0.8f + (float)r.NextDouble() * 0.6f), theme.Stone, theme.Stone2));
            plan.Sprinkle(random, clear ? 14 : 6, 0.5f, clear ? 13f : 30f, r => (PropKind.Rock, new Vector3(0.5f, 0.2f, 0.45f) * (0.7f + (float)r.NextDouble() * 0.6f), theme.Stone2, theme.Stone), true);
        }


        /// <summary>Whether a circle of radius <paramref name="r"/> at <paramref name="p"/> touches a crater of the plan.</summary>
        private static bool CraterAt(TilePlan plan, Vector2 p, float r)
        {
            foreach ((Vector2 at, float radius) in plan.Craters)
            {
                if ((p - at).magnitude < radius + r)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>The rough ground of the station: machinery pits with grating, pipe runs, vents and tanks.</summary>
        private static void StationRough(TilePlan plan, System.Random random, bool clear)
        {
            float from = clear ? 16f : 2f;
            for (int i = 0; i < 6; i++)
            {
                float x = from + (float)random.NextDouble() * (29f - from);
                x = random.NextDouble() < 0.5 ? -x : x;
                plan.Place(PropKind.Pipe, new Vector2(x, ((float)random.NextDouble() - 0.5f) * 6f), random.NextDouble() < 0.5 ? 0f : 90f,
                    new Vector3(1f, 0.45f, 6f + (float)random.NextDouble() * 6f), GroundSwatch.Pipe, GroundSwatch.Coolant, random.Next());
            }
            plan.Sprinkle(random, 8, from, 30f, r => (PropKind.Vent, new Vector3(2f, 0.4f, 2f) * (0.8f + (float)r.NextDouble() * 0.5f), GroundSwatch.PlateDark, GroundSwatch.Pipe));
            plan.Sprinkle(random, 5, from, 30f, r => (PropKind.Tank, new Vector3(2f, 0.7f, 2f), GroundSwatch.Plate, GroundSwatch.Hazard));
            plan.Sprinkle(random, 6, from, 30f, r => (PropKind.Container, new Vector3(1.3f, 0.5f, 2.6f), r.NextDouble() < 0.5 ? GroundSwatch.RoofBlue : GroundSwatch.Hazard, GroundSwatch.PlateDark));
            for (int i = 0; i < 4; i++)
            {
                var at = new Vector3((random.NextDouble() < 0.5 ? -1f : 1f) * (from + (float)random.NextDouble() * (28f - from)), ((float)random.NextDouble() - 0.5f) * 16f, -0.05f);
                plan.Lights.Add((at, 3f, new Color(0.4f, 0.9f, 1f, 0.3f)));
            }
        }


        private static void RoadTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            float cx = plan.Variant == 1 ? -9f : plan.Variant == 2 ? 9f : 0f;
            plan.Paved = p => 2.5f - Mathf.Abs(p.x - cx);
            Func<Vector2, float> alt = plan.Alt;
            Func<Vector2, float> rock = plan.Rock;
            if (theme.Name != "Jungle")
            {
                plan.Alt = p => Mathf.Max(alt(p), 4f - Mathf.Abs(p.x - cx) + (SpaceTextures.PNoise(p.x / 64f + 0.5f, p.y / 20f + 0.5f, 32, 10, theme.Seed + 3) - 0.5f) * 1.2f);
            }
            plan.Rock = p => Mathf.Min(rock(p), Mathf.Abs(p.x - cx) - 6f);
            plan.Blocked = (p, r) => Mathf.Abs(p.x - cx) < 5f + r;
            if (theme.Name == "Jungle")
            {
                plan.Tint = p =>
                {
                    float d = Mathf.Abs(Mathf.Abs(p.x - cx) - 1.1f);
                    float k = Mathf.Lerp(0.72f, 1f, SpaceTextures.Step(0.15f, 0.45f, d));
                    return new Color(k, k, k, 1f);
                };
            }
            if (theme.Name != "Jungle")
            {
                for (int i = 0; i < 5; i++)
                {
                    plan.Place(PropKind.Mark, new Vector2(cx, -8f + i * 4f), 0f, new Vector3(0.14f, 0.01f, 2f), GroundSwatch.White, GroundSwatch.White, 0, false);
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    plan.Place(PropKind.Mark, new Vector2(cx + side * 2.15f, 0f), 0f, new Vector3(0.1f, 0.01f, 20f), GroundSwatch.Hazard, GroundSwatch.Hazard, 0, false);
                }
            }
            theme.Scatter?.Invoke(plan, random, 0.8f);
            if (theme.Night)
            {
                for (int i = 0; i < 2; i++)
                {
                    float y = -5f + i * 10f;
                    float x = cx + (i == 0 ? -3.4f : 3.4f);
                    plan.Place(PropKind.Lamp, new Vector2(x, y), i == 0 ? 90f : -90f, new Vector3(0.3f, 0.85f, 1.4f), GroundSwatch.SteelDark, GroundSwatch.Lamp, 0, false);
                    plan.Lights.Add((new Vector3(x + (i == 0 ? 0.7f : -0.7f), y, -0.05f), 5f, new Color(1f, 0.8f, 0.5f, 0.35f)));
                }
            }
        }


        private static void RiverTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            int seed = theme.Seed + plan.Variant * 7;
            float Bank(float x, int salt) => (SpaceTextures.PNoise(x / 64f + 0.5f, 0.5f, 12, 1, seed + salt) - 0.5f) * 1.2f;
            plan.Liquid = p => p.y >= 0f ? 4f + Bank(p.x, 1) - p.y : 4f + Bank(p.x, 2) + p.y;
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Max(rock(p), 5.6f + Bank(p.x, 3) * 1.5f - Mathf.Abs(p.y));
            Func<Vector2, float> alt = plan.Alt;
            plan.Alt = p => Mathf.Min(alt(p), Mathf.Abs(p.y) - 6.5f);
            bool bridge = plan.Variant == 0;
            if (bridge)
            {
                plan.Paved = p => Mathf.Min(2.5f - Mathf.Abs(p.x), Mathf.Abs(p.y) - 5.2f);
                plan.Place(PropKind.Bridge, Vector2.zero, 0f, new Vector3(5.4f, 0.45f, 11.4f), GroundSwatch.ConcreteDark, GroundSwatch.Concrete, 0, false);
                plan.Blocked = (p, r) => Mathf.Abs(p.y) < 6.5f + r || Mathf.Abs(p.x) < 4.5f + r;
            }
            else
            {
                plan.Blocked = (p, r) => Mathf.Abs(p.y) < 6.5f + r;
            }
            theme.Scatter?.Invoke(plan, random, 0.8f);
            plan.Sprinkle(random, 10, 1f, 30f, r => (PropKind.Rock, new Vector3(0.6f, 0.2f, 0.5f) * (0.7f + (float)r.NextDouble() * 0.7f), theme.Stone2, theme.Stone));
        }


        private static void LakeTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            var center = new Vector2(plan.Variant % 2 == 0 ? -10f : 10f, 0f);
            float Shore(Vector2 p)
            {
                float angle = Mathf.Atan2(p.y - center.y, p.x - center.x) / (Mathf.PI * 2f) + 0.5f;
                return (SpaceTextures.PNoise(angle, 0.5f, 9, 1, theme.Seed + plan.Variant + 5) - 0.5f) * 1.6f;
            }
            plan.Liquid = p => 7f + Shore(p) - (p - center).magnitude;
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Max(rock(p), 8.4f + Shore(p) * 1.4f - (p - center).magnitude);
            Func<Vector2, float> alt = plan.Alt;
            plan.Alt = p => Mathf.Min(alt(p), (p - center).magnitude - 9f);
            plan.Blocked = (p, r) => (p - center).magnitude < 9f + r;
            theme.Scatter?.Invoke(plan, random, 0.9f);
            if (theme.Name == "Volcanic")
            {
                plan.Lights.Add((new Vector3(center.x, center.y, -0.1f), 16f, new Color(1f, 0.4f, 0.1f, 0.25f)));
            }
        }


        private static void CoastTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            bool seaUp = plan.Variant % 2 == 0;
            float Shore(float x) => (SpaceTextures.PFbm(x / 64f + 0.5f, 0.5f, 6, 1, 3, theme.Seed + 31 + plan.Variant) - 0.5f) * 3f;
            plan.Liquid = p => seaUp ? p.y - Shore(p.x) : Shore(p.x) - p.y;
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p =>
            {
                float land = seaUp ? Shore(p.x) - p.y : p.y - Shore(p.x);
                return Mathf.Max(Mathf.Min(rock(p), land - 1f), (SpaceTextures.PNoise(p.x / 64f + 0.5f, 0.5f, 16, 1, theme.Seed + 37) - 0.72f) * 12f - Mathf.Abs(land + 0.3f) * 1.5f);
            };
            Func<Vector2, float> alt = plan.Alt;
            plan.Alt = p => Mathf.Min(alt(p), (seaUp ? Shore(p.x) - p.y : p.y - Shore(p.x)) - 2.5f);
            plan.Tint = p =>
            {
                float land = seaUp ? Shore(p.x) - p.y : p.y - Shore(p.x);
                float wet = land > 0f ? Mathf.Lerp(0.72f, 1f, SpaceTextures.Step(0f, 2.2f, land)) : 0.72f;
                return new Color(wet, wet, wet * 1.02f, 1f);
            };
            plan.Blocked = (p, r) => (seaUp ? p.y - Shore(p.x) : Shore(p.x) - p.y) > -2f - r;
            theme.Scatter?.Invoke(plan, random, 0.7f);
            plan.Sprinkle(random, 8, 2f, 30f, r => (PropKind.Palm, new Vector3(2.4f, 0.85f, 2.4f) * (0.8f + (float)r.NextDouble() * 0.3f), theme.Plant, theme.Plant2));
            for (int i = 0; i < 5; i++)
            {
                float x = ((float)random.NextDouble() - 0.5f) * 56f;
                float y = Shore(x) + (seaUp ? 1f : -1f) * (0.6f + (float)random.NextDouble() * 2.5f);
                if (Mathf.Abs(y) < 8.5f)
                {
                    plan.Place(PropKind.Rock, new Vector2(x, y), (float)random.NextDouble() * 360f, new Vector3(0.9f, 0.3f, 0.7f), theme.Stone, theme.Stone2, random.Next(), false);
                }
            }
        }


        private static void SeaTile(TilePlan plan, System.Random random)
        {
            plan.Liquid = p => 30f;
            plan.Rock = p => -10f;
            plan.Alt = p => -10f;
            plan.Tint = p =>
            {
                float n = 1f + (SpaceTextures.PFbm(p.x / 64f + 0.5f, p.y / 20f + 0.5f, 4, 2, 3, plan.Theme.Seed + 53 + plan.Variant) - 0.5f) * 0.3f;
                return new Color(n, n, n, 1f);
            };
        }


        private static void BaseTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            plan.Paved = p => 14f - Mathf.Abs(p.x);
            plan.Concrete = p => 1f;
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Min(rock(p), Mathf.Abs(p.x) - 16f);
            Func<Vector2, float> alt = plan.Alt;
            plan.Alt = p => Mathf.Max(Mathf.Min(alt(p), Mathf.Abs(p.x) - 15f), 15.5f - Mathf.Abs(p.x) + (Mathf.Abs(p.x) < 14.5f ? -3f : 0f));
            plan.Blocked = (p, r) => Mathf.Abs(p.x) < 15.2f + r;
            for (int side = -1; side <= 1; side += 2)
            {
                plan.Place(PropKind.Mark, new Vector2(side * 13.4f, 0f), 0f, new Vector3(0.22f, 0.01f, 20f), GroundSwatch.Hazard, GroundSwatch.Hazard, 0, false);
                plan.Place(PropKind.Mark, new Vector2(side * 13.05f, 0f), 0f, new Vector3(0.08f, 0.01f, 20f), GroundSwatch.HazardDark, GroundSwatch.HazardDark, 0, false);
            }
            if (plan.Variant % 2 == 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        plan.Place(PropKind.Mark, new Vector2(side * 10.5f, -8f + i * 4f), 0f, new Vector3(3.2f, 0.01f, 0.12f), GroundSwatch.White, GroundSwatch.White, 0, false);
                    }
                }
                for (int i = 0; i < 5; i++)
                {
                    plan.Place(PropKind.Mark, new Vector2(0f, -8f + i * 4f), 0f, new Vector3(0.18f, 0.01f, 2.2f), GroundSwatch.Hazard, GroundSwatch.Hazard, 0, false);
                }
            }
            else
            {
                var pad = new Vector2(-6.5f, 0f);
                for (int i = 0; i < 20; i++)
                {
                    float angle = i * 18f;
                    Vector2 at = pad + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 3.6f;
                    plan.Place(PropKind.Mark, at, angle, new Vector3(0.2f, 0.01f, 0.95f), GroundSwatch.White, GroundSwatch.White, 0, false);
                }
                plan.Place(PropKind.Mark, pad + new Vector2(-0.8f, 0f), 0f, new Vector3(0.3f, 0.01f, 2.4f), GroundSwatch.White, GroundSwatch.White, 0, false);
                plan.Place(PropKind.Mark, pad + new Vector2(0.8f, 0f), 0f, new Vector3(0.3f, 0.01f, 2.4f), GroundSwatch.White, GroundSwatch.White, 0, false);
                plan.Place(PropKind.Mark, pad, 0f, new Vector3(1.6f, 0.01f, 0.3f), GroundSwatch.White, GroundSwatch.White, 0, false);
                for (int i = 0; i < 5; i++)
                {
                    plan.Place(PropKind.Mark, new Vector2(6.5f, -8f + i * 4f), 0f, new Vector3(0.18f, 0.01f, 2.2f), GroundSwatch.Hazard, GroundSwatch.Hazard, 0, false);
                }
            }
            theme.BaseDressing?.Invoke(plan, random);
            theme.Scatter?.Invoke(plan, random, 0.5f);
        }


        private static void CityTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            float[] streets = { -12f, 0f, 12f };
            float Street(Vector2 p)
            {
                float d = Mathf.Abs(p.y);
                foreach (float x in streets)
                {
                    d = Mathf.Min(d, Mathf.Abs(p.x - x));
                }
                return d;
            }
            plan.Paved = p => 3f - Street(p);
            plan.Concrete = p => SpaceTextures.Step(1.9f, 2.15f, Street(p));
            Func<Vector2, float> alt = plan.Alt;
            plan.Alt = p => Mathf.Max(alt(p), 0.4f);
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Min(rock(p), Street(p) - 3.5f);
            plan.Blocked = (p, r) => Street(p) < 3.1f + r;
            foreach (float x in streets)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    plan.Place(PropKind.Mark, new Vector2(x + side * 2.05f, 6.05f), 0f, new Vector3(0.12f, 0.06f, 7.9f), GroundSwatch.ConcreteLight, GroundSwatch.ConcreteLight, 0, false);
                    plan.Place(PropKind.Mark, new Vector2(x + side * 2.05f, -6.05f), 0f, new Vector3(0.12f, 0.06f, 7.9f), GroundSwatch.ConcreteLight, GroundSwatch.ConcreteLight, 0, false);
                }
                for (int i = 0; i < 4; i++)
                {
                    float y = i < 2 ? -8.5f + i * 3f : 5.5f + (i - 2) * 3f;
                    plan.Place(PropKind.Mark, new Vector2(x, y), 0f, new Vector3(0.12f, 0.01f, 1.5f), GroundSwatch.White, GroundSwatch.White, 0, false);
                }
            }
            float[] blockX = { -32f, -15f, -9f, -3f, 3f, 9f, 15f, 32f };
            float[] blockY = { -9.8f, -3f, 3f, 9.8f };
            for (int bx = 0; bx < blockX.Length; bx += 2)
            {
                for (int by = 0; by < blockY.Length; by += 2)
                {
                    FillBlock(plan, random, new Rect(blockX[bx], blockY[by], blockX[bx + 1] - blockX[bx], blockY[by + 1] - blockY[by]));
                }
            }
            if (theme.Night)
            {
                foreach (float x in streets)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var at = new Vector2(x + side * 2.6f, side * 5f);
                        plan.Place(PropKind.Lamp, at, side > 0 ? 90f : -90f, new Vector3(0.3f, 0.85f, 1.2f), GroundSwatch.SteelDark, GroundSwatch.Lamp, 0, false);
                        plan.Lights.Add((new Vector3(at.x - side * 0.6f, at.y, -0.05f), 5.5f, new Color(1f, 0.8f, 0.5f, 0.35f)));
                    }
                }
            }
        }


        /// <summary>Fills a city block with buildings (a courtyard of the alt layer shows between them).</summary>
        private static void FillBlock(TilePlan plan, System.Random random, Rect block)
        {
            GroundTheme theme = plan.Theme;
            GroundSwatch[] walls = { GroundSwatch.ConcreteLight, GroundSwatch.Concrete, GroundSwatch.Brick, GroundSwatch.Tan, GroundSwatch.Stone };
            GroundSwatch[] roofs = { GroundSwatch.ConcreteDark, GroundSwatch.Asphalt, GroundSwatch.RoofBlue, GroundSwatch.RoofRed, GroundSwatch.SteelDark };
            float x = block.xMin + 0.2f;
            while (x < block.xMax - 1.8f)
            {
                float width = Mathf.Min(block.xMax - 0.2f - x, 2.6f + (float)random.NextDouble() * 3f);
                if (block.xMax - 0.2f - x - width < 1.8f)
                {
                    width = block.xMax - 0.2f - x;
                }
                if (width < 2f)
                {
                    break;
                }
                float y = block.yMin + 0.2f;
                while (y < block.yMax - 1.8f)
                {
                    float depth = Mathf.Min(block.yMax - 0.2f - y, 2.4f + (float)random.NextDouble() * 2.6f);
                    if (block.yMax - 0.2f - y - depth < 1.8f)
                    {
                        depth = block.yMax - 0.2f - y;
                    }
                    if (depth < 2f)
                    {
                        break;
                    }
                    if (random.NextDouble() < 0.93)
                    {
                        float h = 0.4f + (float)random.NextDouble() * 0.5f;
                        var size = new Vector3(width - 0.25f, h, depth - 0.25f);
                        plan.Place(PropKind.Building, new Vector2(x + width * 0.5f, y + depth * 0.5f), 0f, size, walls[random.Next(walls.Length)],
                            roofs[random.Next(roofs.Length)], random.Next(), false);
                        plan.LightLast(theme.Night);
                    }
                    else
                    {
                        plan.Place(PropKind.Tree, new Vector2(x + width * 0.5f, y + depth * 0.5f), (float)random.NextDouble() * 360f, new Vector3(2f, 0.8f, 2f),
                            theme.Plant, theme.Plant2, random.Next(), false);
                    }
                    y += depth;
                }
                x += width;
            }
        }


        private static void FieldTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            const float patchX = 8f;
            const float patchY = 5f;
            int seed = theme.Seed + plan.Variant * 17 + 90;
            float Id(Vector2 p) => SpaceTextures.Hash(Mathf.FloorToInt(p.x / patchX + 100f), Mathf.FloorToInt(p.y / patchY + 100f), seed);
            float Inside(Vector2 p)
            {
                float fx = Mathf.Repeat(p.x, patchX);
                float fy = Mathf.Repeat(p.y, patchY);
                return Mathf.Min(Mathf.Min(fx, patchX - fx), Mathf.Min(fy, patchY - fy)) - 0.45f;
            }
            plan.Alt = p => Mathf.Abs(p.x) > 30f || Mathf.Abs(p.y) > 9.4f || Id(p) < 0.15f ? -1f : Inside(p);
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Min(rock(p), -Inside(p) - 0.1f);
            Color[] crops = { new Color(1f, 1f, 0.9f), new Color(1.06f, 1.02f, 0.86f), new Color(0.9f, 1f, 0.9f), new Color(1.04f, 0.94f, 0.86f) };
            plan.Tint = p => Inside(p) > -0.5f ? crops[Mathf.FloorToInt(Id(p) * crops.Length) % crops.Length] : Color.white;
            for (int i = 0; i < 6; i++)
            {
                float y = -10f + patchY * (1 + (i % 3));
                if (Mathf.Abs(y) > 9f)
                {
                    continue;
                }
                float x = (-3 + random.Next(7)) * patchX + patchX * 0.5f;
                plan.Place(PropKind.Hedge, new Vector2(x, y), 0f, new Vector3(patchX - 1f, 0.35f, 0.5f), theme.Plant2, theme.Plant, random.Next(), false);
            }
            plan.Blocked = (p, r) => Mathf.Abs(p.x) < 28f;
            theme.Scatter?.Invoke(plan, random, 0.6f);
        }

        // ------------------------------------------------------------------ dressing

        private static (PropKind, Vector3, GroundSwatch, GroundSwatch) Pick(System.Random random, PropKind kind, Vector3 size, float spread, GroundSwatch main, GroundSwatch accent)
        {
            return (kind, size * (1f - spread + (float)random.NextDouble() * spread * 2f), main, accent);
        }


        private static void DesertScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(26 * density), 0.5f, 31f, r => Pick(r, PropKind.Shrub, new Vector3(0.9f, 0.22f, 0.9f), 0.35f, t.Plant, t.Plant2));
            plan.Sprinkle(random, (int)(14 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.55f, 0.2f, 0.45f), 0.4f, r.NextDouble() < 0.5 ? t.Stone : t.Stone2, t.Stone));
            plan.Sprinkle(random, (int)(7 * density), 4f, 31f, r => Pick(r, PropKind.Cactus, new Vector3(1.1f, 0.78f, 1.1f), 0.2f, GroundSwatch.Cactus, GroundSwatch.Cactus));
            plan.Sprinkle(random, (int)(4 * density), 15f, 31f, r => Pick(r, PropKind.Boulders, new Vector3(1.8f, 0.55f, 1.8f), 0.3f, t.Stone, t.Stone2));
        }


        private static void JungleScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(46 * density), 14f, 31f, r => Pick(r, PropKind.Tree, new Vector3(3.4f, 0.88f, 3.4f), 0.3f, r.NextDouble() < 0.5 ? t.Plant : t.Plant2, GroundSwatch.Moss));
            plan.Sprinkle(random, (int)(14 * density), 2f, 31f, r => Pick(r, PropKind.Palm, new Vector3(2.6f, 0.85f, 2.6f), 0.25f, t.Plant, t.Plant2));
            plan.Sprinkle(random, (int)(26 * density), 0.5f, 31f, r => Pick(r, PropKind.Bush, new Vector3(1.1f, 0.35f, 1f), 0.35f, r.NextDouble() < 0.5 ? GroundSwatch.LeafDark : GroundSwatch.Moss, t.Plant));
            plan.Sprinkle(random, (int)(20 * density), 0.5f, 31f, r => Pick(r, PropKind.Fern, new Vector3(1f, 0.3f, 1f), 0.3f, GroundSwatch.Leaf, GroundSwatch.LeafLight));
        }


        private static void CityScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(14 * density), 14f, 31f, r => Pick(r, PropKind.Tree, new Vector3(2.6f, 0.8f, 2.6f), 0.3f, t.Plant, t.Plant2));
            plan.Sprinkle(random, (int)(10 * density), 16f, 31f, r => Pick(r, PropKind.Building, new Vector3(3.5f, 0.6f, 3f), 0.3f, GroundSwatch.ConcreteLight, GroundSwatch.RoofBlue));
            plan.Sprinkle(random, (int)(8 * density), 3f, 31f, r => Pick(r, PropKind.Car, new Vector3(0.8f, 0.5f, 1.7f), 0.1f, r.NextDouble() < 0.5 ? GroundSwatch.RoofRed : GroundSwatch.RoofBlue, GroundSwatch.Glass));
            plan.Sprinkle(random, (int)(12 * density), 0.5f, 31f, r => Pick(r, PropKind.Bush, new Vector3(0.9f, 0.3f, 0.9f), 0.3f, GroundSwatch.LeafDark, t.Plant));
            plan.Sprinkle(random, (int)(8 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.6f, 0.2f, 0.5f), 0.3f, t.Stone, t.Stone2));
        }


        private static void MoonScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(22 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.6f, 0.25f, 0.5f), 0.45f, r.NextDouble() < 0.5 ? t.Stone : t.Stone2, t.Stone));
            plan.Sprinkle(random, (int)(6 * density), 12f, 31f, r => Pick(r, PropKind.Boulders, new Vector3(2f, 0.6f, 2f), 0.3f, t.Stone, t.Stone2));
            plan.AddCraters(random, (int)(4 * density), 0.8f, 2.4f);
        }


        private static void VolcanicScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(20 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.6f, 0.25f, 0.5f), 0.4f, r.NextDouble() < 0.5 ? t.Stone : t.Stone2, t.Stone));
            plan.Sprinkle(random, (int)(8 * density), 14f, 31f, r => Pick(r, PropKind.Spire, new Vector3(1.8f, 0.85f, 1.8f), 0.3f, t.Stone, t.Stone2));
            plan.Sprinkle(random, (int)(5 * density), 6f, 31f, r => Pick(r, PropKind.Crystal, new Vector3(1f, 0.6f, 1f), 0.3f, GroundSwatch.Basalt, GroundSwatch.ObsidianGlow));
        }


        private static void StationScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(8 * density), 15f, 31f, r => Pick(r, PropKind.Vent, new Vector3(1.6f, 0.35f, 1.6f), 0.25f, GroundSwatch.PlateDark, GroundSwatch.Pipe));
            plan.Sprinkle(random, (int)(6 * density), 16f, 31f, r => Pick(r, PropKind.Container, new Vector3(1.3f, 0.5f, 2.6f), 0.15f, r.NextDouble() < 0.5 ? GroundSwatch.RoofBlue : GroundSwatch.Hazard, GroundSwatch.PlateDark));
            plan.Sprinkle(random, (int)(5 * density), 17f, 30f, r => Pick(r, PropKind.Pipe, new Vector3(1f, 0.45f, 6f), 0.3f, GroundSwatch.Pipe, GroundSwatch.PipeDark));
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector3((random.NextDouble() < 0.5 ? -1f : 1f) * (8f + (float)random.NextDouble() * 20f), ((float)random.NextDouble() - 0.5f) * 16f, -0.05f);
                plan.Lights.Add((at, 3f, new Color(0.4f, 0.9f, 1f, 0.3f)));
            }
        }


        private static void MilitaryBase(TilePlan plan, System.Random random)
        {
            GroundTheme t = plan.Theme;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    plan.Place(PropKind.Fence, new Vector2(side * 15.4f, -7f + i * 7f), 90f, new Vector3(5.2f, 0.4f, 0.1f), GroundSwatch.SteelDark, GroundSwatch.SteelDark, 0, false);
                }
                bool hangar = (plan.Variant + (side > 0 ? 1 : 0)) % 2 == 0;
                if (hangar)
                {
                    plan.Place(PropKind.Hangar, new Vector2(side * 21.5f, 0.5f), 90f, new Vector3(5.4f, 0.85f, 8f), GroundSwatch.Steel, GroundSwatch.SteelDark, random.Next(), false);
                }
                else
                {
                    plan.Place(PropKind.Building, new Vector2(side * 20f, -4.5f), 0f, new Vector3(4.5f, 0.6f, 3.2f), t.Walls, t.Roof, random.Next(), false);
                    plan.Place(PropKind.Building, new Vector2(side * 20f, 4.5f), 0f, new Vector3(4.5f, 0.6f, 3.2f), t.Walls, t.Roof, random.Next(), false);
                    plan.Place(PropKind.Tower, new Vector2(side * 17.2f, 0f), 0f, new Vector3(1.2f, 0.9f, 1.2f), t.Walls, GroundSwatch.SteelDark, random.Next(), false);
                }
                plan.Place(PropKind.Tank, new Vector2(side * 27.5f, -5f), 0f, new Vector3(2.4f, 0.8f, 2.4f), GroundSwatch.ConcreteLight, GroundSwatch.EnemyRed, 0, false);
                plan.Place(PropKind.Container, new Vector2(side * 27.5f, 2.5f), 0f, new Vector3(1.3f, 0.55f, 2.8f), GroundSwatch.Olive, GroundSwatch.OliveDark, 0, false);
                plan.Place(PropKind.Barrels, new Vector2(side * 17.2f, side * 7.5f), 0f, new Vector3(1.2f, 0.45f, 0.8f), GroundSwatch.Olive, GroundSwatch.EnemyRed, random.Next(), false);
                plan.Place(PropKind.Sandbags, new Vector2(side * 25f, 8f), 0f, new Vector3(3f, 0.35f, 0.5f), GroundSwatch.Khaki, GroundSwatch.Khaki, random.Next(), false);
                if (t.Night)
                {
                    plan.Place(PropKind.Lamp, new Vector2(side * 15f, 9f), side > 0 ? 90f : -90f, new Vector3(0.3f, 0.85f, 1.2f), GroundSwatch.SteelDark, GroundSwatch.Lamp, 0, false);
                    plan.Lights.Add((new Vector3(side * 14.4f, 9f, -0.05f), 6f, new Color(1f, 0.85f, 0.6f, 0.35f)));
                }
            }
        }


        private static void ColonyBase(TilePlan plan, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                plan.Place(PropKind.Dome, new Vector2(side * 21f, -2.5f), 0f, new Vector3(5.5f, 0.9f, 5.5f), GroundSwatch.White, GroundSwatch.Plate, random.Next(), false);
                plan.Place(PropKind.Pipe, new Vector2(side * 17f, 0f), 0f, new Vector3(1f, 0.4f, 17f), GroundSwatch.Pipe, GroundSwatch.PipeDark, 0, false);
                for (int i = 0; i < 3; i++)
                {
                    plan.Place(PropKind.Solar, new Vector2(side * (26f + (i % 2) * 3f), 3.5f + i * 2f), 0f, new Vector3(2.6f, 0.5f, 1.4f), GroundSwatch.Solar, GroundSwatch.SteelDark, 0, false);
                }
                plan.Place(PropKind.Antenna, new Vector2(side * 28f, -7f), 0f, new Vector3(1.4f, 0.9f, 1.4f), GroundSwatch.Metal, GroundSwatch.GlowRed, 0, false);
                plan.Place(PropKind.Container, new Vector2(side * 18.5f, 7.5f), 90f, new Vector3(1.2f, 0.5f, 2.4f), GroundSwatch.Hazard, GroundSwatch.HazardDark, 0, false);
            }
        }


        private static void IndustrialBase(TilePlan plan, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                plan.Place(PropKind.Tower2, new Vector2(side * 22f, -3f), 0f, new Vector3(5f, 0.9f, 5f), GroundSwatch.ConcreteDark, GroundSwatch.Rust, 0, false);
                plan.Place(PropKind.Pipe, new Vector2(side * 17f, 0f), 0f, new Vector3(1f, 0.45f, 17f), GroundSwatch.Rust, GroundSwatch.PipeDark, 0, false);
                plan.Place(PropKind.Tank, new Vector2(side * 27.5f, 4.5f), 0f, new Vector3(2.8f, 0.8f, 2.8f), GroundSwatch.SteelDark, GroundSwatch.Hazard, 0, false);
                plan.Place(PropKind.Building, new Vector2(side * 21f, 6f), 0f, new Vector3(4f, 0.6f, 3f), GroundSwatch.SteelDark, GroundSwatch.Rust, random.Next(), false);
                plan.Place(PropKind.Barrels, new Vector2(side * 27f, -7.5f), 0f, new Vector3(1.2f, 0.45f, 0.8f), GroundSwatch.Rust, GroundSwatch.Hazard, random.Next(), false);
            }
        }


        private static void StationBase(TilePlan plan, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                plan.Place(PropKind.Building, new Vector2(side * 21f, -3.5f), 0f, new Vector3(6f, 0.7f, 5f), GroundSwatch.Plate, GroundSwatch.PlateDark, random.Next(), false);
                plan.Place(PropKind.Vent, new Vector2(side * 27.5f, 4f), 0f, new Vector3(2.4f, 0.4f, 2.4f), GroundSwatch.PlateDark, GroundSwatch.Pipe, 0, false);
                plan.Place(PropKind.Pipe, new Vector2(side * 16.8f, 0f), 0f, new Vector3(1f, 0.4f, 17f), GroundSwatch.Pipe, GroundSwatch.Coolant, 0, false);
                plan.Place(PropKind.Antenna, new Vector2(side * 20f, 6.5f), 0f, new Vector3(1.4f, 0.9f, 1.4f), GroundSwatch.Metal, GroundSwatch.GlowCyan, 0, false);
                plan.Lights.Add((new Vector3(side * 20f, 6.5f, -0.9f), 2f, new Color(0.4f, 0.9f, 1f, 0.5f)));
            }
        }

        // ------------------------------------------------------------------ importers and materials

        private static void PaletteImporter(TextureImporter importer)
        {
            AsteroidsAssets.PaletteTexture(importer);
            importer.textureShape = TextureImporterShape.Texture2D;
        }


        private static void DecalImporter(TextureImporter importer)
        {
            AsteroidsAssets.ParticleTexture(importer);
            importer.textureShape = TextureImporterShape.Texture2D;
        }


        private static void LayerImporter(TextureImporter importer)
        {
            AsteroidsAssets.TileableTexture(importer);
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.anisoLevel = 8;
        }


        /// <summary>Per-tile data: linear, bilinear, clamped, no mipmaps, no compression, not rescaled (80 texels is not a power of two).</summary>
        private static void DataImporter(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }


        /// <summary>An alpha-blended decal of the Glow shader.</summary>
        private static void DecalMaterial(Material material, Texture texture, Color color)
        {
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 10;
        }

        // ------------------------------------------------------------------ ground units

        /// <summary>The ground units with a model (every ground StrikeUnit).</summary>
        public static readonly StrikeUnit[] GroundUnits =
        {
            StrikeUnit.Turret, StrikeUnit.Flak, StrikeUnit.Tank, StrikeUnit.Truck, StrikeUnit.Bunker, StrikeUnit.FuelTank, StrikeUnit.Radar,
            StrikeUnit.Gunboat, StrikeUnit.Hut, StrikeUnit.LaserTower, StrikeUnit.Depot, StrikeUnit.Crate
        };


        private static void BuildGroundUnitMeshes()
        {
            foreach (StrikeUnit unit in GroundUnits)
            {
                UnitModel model = SpaceModels.GroundUnit(unit);
                AsteroidsAssets.SaveMesh(model.Body, $"Art/Ground/Models/Units/{unit}.asset");
                if (model.Turret != null)
                {
                    AsteroidsAssets.SaveMesh(model.Turret, $"Art/Ground/Models/Units/{unit}Turret.asset");
                }
                if (model.Spinner != null)
                {
                    AsteroidsAssets.SaveMesh(model.Spinner, $"Art/Ground/Models/Units/{unit}Spinner.asset");
                }
            }
        }


        /// <summary>A generated ground unit mesh: the body, or its "Turret" / "Spinner" part.</summary>
        public static Mesh GroundUnitMesh(StrikeUnit unit, string part = "")
        {
            return AsteroidsAssets.Load<Mesh>($"Art/Ground/Models/Units/{unit}{part}.asset");
        }
    }
}
