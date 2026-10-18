using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Where the features of the terrain kinds of the Iron Tide and Hive Fall campaigns lie in every variant of a tile:
    /// the floes of pack ice, the islands and shoals of the archipelago, the mounds and brood pits of a hive nest, the lanes
    /// through a crystal field. The art builder lays the tiles out with it (<see cref="AsteroidsArtBuilder.Plan"/>) and the
    /// content builder places the ground units by it (on floes, islands and mounds; boats in the leads; vehicles in the
    /// lanes), so the two always agree. Tile metres: x -32 to 32, y -10 to 10; the screen shows about x -18 to 18. Every
    /// feature keeps clear of the tile's ends, so a tile joins open water (ice, islands) or open ground (nest, crystals).
    /// </summary>
    internal static class CampaignTiles
    {
        /// <summary>Half the width of a lane through a crystal field (m).</summary>
        public const float LaneHalfWidth = 2.8f;

        private static readonly (Vector2 at, float radius)[][] FloeSets =
        {
            new[] { (new Vector2(-11f, 2.5f), 5.2f), (new Vector2(1.5f, -4.5f), 4.6f), (new Vector2(12.5f, 3.5f), 4.8f), (new Vector2(-25f, -3f), 6f),
                (new Vector2(26f, -2f), 5.5f), (new Vector2(-3f, 6.6f), 2f), (new Vector2(7f, -7.6f), 1.4f) },
            new[] { (new Vector2(-6f, -3f), 5.6f), (new Vector2(9f, 4f), 4.4f), (new Vector2(-16f, 6f), 3f), (new Vector2(17f, -5f), 4f),
                (new Vector2(-26f, 1f), 5f), (new Vector2(27f, 3f), 6f), (new Vector2(2f, 6.8f), 1.6f) }
        };

        private static readonly (Vector2 at, float radius)[][] IslandSets =
        {
            new[] { (new Vector2(-11f, 2f), 6f), (new Vector2(12f, -3.5f), 4.8f), (new Vector2(28f, 4f), 6f), (new Vector2(-28f, -4f), 4.5f) },
            new[] { (new Vector2(2f, 0f), 7f), (new Vector2(-24f, -4f), 5f), (new Vector2(25f, 3f), 4f), (new Vector2(-14f, 5f), 2.2f) },
            new[] { (new Vector2(-8f, -3f), 2.2f), (new Vector2(9f, 4.5f), 2f), (new Vector2(-24f, 3f), 4f), (new Vector2(24f, -4f), 3.6f) }
        };

        private static readonly (Vector2 at, float radius)[][] ShoalSets =
        {
            new[] { (new Vector2(1f, 5f), 3.5f), (new Vector2(-2f, -6f), 3f) },
            new[] { (new Vector2(-12f, -5f), 3.5f), (new Vector2(14f, 6f), 3f) },
            new[] { (new Vector2(0f, 0f), 6.5f), (new Vector2(-14f, 4f), 4f), (new Vector2(15f, -4f), 4f), (new Vector2(-8f, -3f), 4f), (new Vector2(9f, 4.5f), 4f) }
        };

        private static readonly (Vector2 at, float radius)[][] MoundSets =
        {
            new[] { (new Vector2(-9f, 2f), 4f), (new Vector2(8f, -3f), 3.5f), (new Vector2(-24f, -3f), 5f), (new Vector2(23f, 4f), 4.5f) },
            new[] { (new Vector2(0f, -1f), 4.5f), (new Vector2(-14f, 4f), 3f), (new Vector2(14f, 3f), 3.2f), (new Vector2(-27f, -3f), 4.5f), (new Vector2(27f, -2f), 4f) },
            new[] { (new Vector2(-6f, -3.5f), 3.2f), (new Vector2(10f, 3f), 4f), (new Vector2(-20f, 4f), 3.5f), (new Vector2(24f, -4f), 4.5f) }
        };

        private static readonly (Vector2 at, float radius)[][] PitSets =
        {
            new[] { (new Vector2(0f, 5f), 1.8f), (new Vector2(-2f, -6f), 1.5f), (new Vector2(14f, 6f), 2f), (new Vector2(-16f, -6f), 1.6f) },
            new[] { (new Vector2(-7f, -5.5f), 1.7f), (new Vector2(7f, 6.5f), 1.5f), (new Vector2(-19f, -6f), 2f), (new Vector2(20f, -6f), 1.6f) },
            new[] { (new Vector2(3f, 2f), 2f), (new Vector2(-12f, 6f), 1.5f), (new Vector2(16f, -6.5f), 1.7f) }
        };

        private static readonly float[][] LaneSets = { new[] { -8f, 8f }, new[] { -12f, 0f, 12f } };

        public static (Vector2 at, float radius)[] Floes(int variant) => Pick(FloeSets, variant);
        public static (Vector2 at, float radius)[] Islands(int variant) => Pick(IslandSets, variant);
        public static (Vector2 at, float radius)[] Shoals(int variant) => Pick(ShoalSets, variant);
        public static (Vector2 at, float radius)[] Mounds(int variant) => Pick(MoundSets, variant);
        public static (Vector2 at, float radius)[] Pits(int variant) => Pick(PitSets, variant);
        public static float[] Lanes(int variant) => Pick(LaneSets, variant);

        /// <summary>The floes, islands and mounds a unit can stand on in the screen's width (centres within |x| &lt; 16).</summary>
        public static List<(Vector2 at, float radius)> OnScreen((Vector2 at, float radius)[] features)
        {
            var list = new List<(Vector2, float)>();
            foreach ((Vector2 at, float radius) feature in features)
            {
                if (Mathf.Abs(feature.at.x) < 16f)
                {
                    list.Add(feature);
                }
            }
            return list;
        }

        /// <summary>
        /// The signed distance (m) to the edges of round features with ragged rims: negative inside the nearest one. The
        /// rims are periodic noise around each feature, the same for the art and the placement.
        /// </summary>
        public static float Field((Vector2 at, float radius)[] features, Vector2 p, int seed, float ragged)
        {
            float best = 30f;
            for (int i = 0; i < features.Length; i++)
            {
                Vector2 d = p - features[i].at;
                float angle = Mathf.Atan2(d.y, d.x) / (Mathf.PI * 2f) + 0.5f;
                float rim = (SpaceTextures.PNoise(angle, 0.5f, 9, 1, seed + i * 17) - 0.5f) * ragged * Mathf.Min(1f, features[i].radius / 3f);
                best = Mathf.Min(best, d.magnitude - features[i].radius - rim);
            }
            return best;
        }

        /// <summary>The distance (m) from <paramref name="x"/> to the nearest lane's edge: negative inside a lane.</summary>
        public static float LaneField(float[] lanes, float x)
        {
            float best = 30f;
            foreach (float lane in lanes)
            {
                best = Mathf.Min(best, Mathf.Abs(x - lane) - LaneHalfWidth);
            }
            return best;
        }

        private static T Pick<T>(T[] sets, int variant)
        {
            int count = sets.Length;
            return sets[((variant % count) + count) % count];
        }
    }


    /// <summary>
    /// The art of the Iron Tide and Hive Fall campaigns: five worlds in the ground pipeline (Glacier, Reef, Hive, Crystal and
    /// Halo, appended to <see cref="GroundThemes"/>), the tiles of their new terrain kinds (Ice, Islands, Nest, Crystals) and
    /// how each world dresses its ground; the models and paints of the new units and bosses (BuildStrikeCampaignArt).
    /// </summary>
    internal static partial class AsteroidsArtBuilder
    {
        // ------------------------------------------------------------------ worlds

        /// <summary>The worlds of the Iron Tide (Glacier, Reef) and Hive Fall (Hive, Crystal, Halo) campaigns.</summary>
        private static GroundTheme[] StrikeCampaignWorlds()
        {
            Color H(string hex) => SpaceTextures.Hex(hex);
            var glacier = new GroundTheme
            {
                Name = "Glacier", Seed = 83, Accent = H("9FE3FF"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Rough, 2), (TerrainKind.Road, 3), (TerrainKind.Base, 2), (TerrainKind.Coast, 2), (TerrainKind.Sea, 2), (TerrainKind.Ice, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Grainy(H("A9B8C8"), H("DCE6EF"), 0.04f, 0.8f, 0f, 801),
                    t => SpaceTextures.Cracked(H("9CCDE4"), H("7AB4D4"), H("3A6884"), 5, 0.02f, 803),
                    t => SpaceTextures.Rocky(H("4C535C"), H("A6B0BA"), H("1A1E24"), 0.5f, 0.2f, 805),
                    t => SpaceTextures.Asphalt(H("2E3236"), H("5A6066"), 807),
                    t => SpaceTextures.Slabs(H("A3A9B0"), H("C2C7CC"), H("666C74"), 2, 0.15f, true, 809),
                    t => SpaceTextures.Ripples(811)
                },
                Shallow = H("3E6E80"), Deep = H("0B2233"), Foam = new Color(0.94f, 0.97f, 1f, 0.75f), Waves = 0.6f, Gloss = 0.7f,
                Sun = new Color(0.92f, 0.96f, 1f), SunIntensity = 1.2f, Ambient = new Color(0.4f, 0.44f, 0.52f),
                Shadow = new Color(0.04f, 0.08f, 0.18f, 0.4f), CraterTint = new Color(0.26f, 0.28f, 0.32f, 1f),
                AltShare = 0.1f, RockShare = 0f, Variation = 0.06f, Dunes = 0.6f, CoastProp = PropKind.Pine,
                Stone = GroundSwatch.SnowShade, Stone2 = GroundSwatch.StoneDark, Walls = GroundSwatch.CorpWhite, Roof = GroundSwatch.CorpSteel,
                Plant = GroundSwatch.Pine, Plant2 = GroundSwatch.Snow, Scatter = GlacierScatter, BaseDressing = CorporateBase
            };
            var reef = new GroundTheme
            {
                Name = "Reef", Seed = 97, Accent = H("38E0C8"),
                Kinds = new[] { (TerrainKind.Sea, 3), (TerrainKind.Islands, 3), (TerrainKind.Coast, 2), (TerrainKind.Plain, 2), (TerrainKind.Base, 2), (TerrainKind.Road, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Grainy(H("BFA272"), H("E8D6A8"), 0.03f, 0.6f, 0f, 901),
                    t => SpaceTextures.Foliage(H("1E4822"), H("4E8C3A"), H("6A5634"), 903),
                    t => SpaceTextures.Rocky(H("5A4A3E"), H("A08A72"), H("201812"), 0.45f, 0.25f, 905),
                    t => SpaceTextures.Asphalt(H("33343A"), H("5C5D62"), 907),
                    t => SpaceTextures.Slabs(H("B0ACA2"), H("CFCBC0"), H("74706A"), 2, 0.2f, false, 909),
                    t => SpaceTextures.Ripples(911)
                },
                Shallow = H("2CC8BE"), Deep = H("0A4A88"), Foam = new Color(0.95f, 0.98f, 0.95f, 0.85f), Waves = 1.1f, Gloss = 0.85f,
                Sun = new Color(1f, 0.97f, 0.9f), SunIntensity = 1.3f, Ambient = new Color(0.38f, 0.42f, 0.44f),
                Shadow = new Color(0f, 0.05f, 0.1f, 0.42f), CraterTint = new Color(0.3f, 0.25f, 0.18f, 1f),
                AltShare = 0.3f, RockShare = 0f, Variation = 0.1f, Dunes = 0.4f,
                Stone = GroundSwatch.Coral, Stone2 = GroundSwatch.StoneDark, Walls = GroundSwatch.CorpWhite, Roof = GroundSwatch.RoofRed,
                Plant = GroundSwatch.Leaf, Plant2 = GroundSwatch.LeafDark, Scatter = ReefScatter, BaseDressing = CorporateBase
            };
            var hive = new GroundTheme
            {
                Name = "Hive", Seed = 109, Accent = H("B46CFF"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Rough, 2), (TerrainKind.Nest, 3), (TerrainKind.River, 2), (TerrainKind.Lake, 2), (TerrainKind.Base, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Cracked(H("5E364A"), H("7C4A5C"), H("2A1220"), 9, 0.05f, 1001),
                    t => SpaceTextures.Foliage(H("361A3E"), H("74387A"), H("281820"), 1003),
                    t => SpaceTextures.Rocky(H("1E1420"), H("4C3658"), H("08040A"), 0.55f, 0.3f, 1005),
                    t => SpaceTextures.Asphalt(H("262229"), H("4A4450"), 1007),
                    t => SpaceTextures.Slabs(H("7A767E"), H("96929C"), H("46424C"), 2, 0.6f, false, 1009),
                    t => SpaceTextures.Ripples(1011)
                },
                Shallow = H("4C8A1E"), Deep = H("12280A"), Foam = new Color(0.8f, 1f, 0.45f, 0.5f), LiquidGlow = new Color(0.45f, 1.3f, 0.15f, 1f),
                CrackGlow = new Color(1.5f, 0.35f, 1.9f, 1f), Waves = 0.5f, Gloss = 0.5f,
                Sun = new Color(1f, 0.84f, 0.92f), SunIntensity = 1.2f, Ambient = new Color(0.36f, 0.27f, 0.38f),
                Shadow = new Color(0.08f, 0f, 0.1f, 0.5f), CraterTint = new Color(0.2f, 0.1f, 0.16f, 1f),
                AltShare = 0.3f, RockShare = 0.12f, Variation = 0.14f, Dunes = 0.3f, PavedRoughness = 0.3f,
                Stone = GroundSwatch.Chitin, Stone2 = GroundSwatch.ChitinDark, Walls = GroundSwatch.ConcreteDark, Roof = GroundSwatch.Flesh,
                Plant = GroundSwatch.Flesh, Plant2 = GroundSwatch.Resin, Scatter = HiveScatter, BaseDressing = OverrunBase
            };
            var crystal = new GroundTheme
            {
                Name = "Crystal", Seed = 127, Accent = H("FF7AE0"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Rough, 2), (TerrainKind.Crystals, 2), (TerrainKind.Lake, 2), (TerrainKind.Road, 2), (TerrainKind.Base, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Cracked(H("C4BAD2"), H("DCD4E8"), H("847A98"), 6, 0.02f, 1101),
                    t => SpaceTextures.Grainy(H("85709E"), H("B09CC6"), 0.03f, 0.5f, 0f, 1103),
                    t => SpaceTextures.Rocky(H("2A2440"), H("6A5A94"), H("100C1C"), 0.55f, 0.1f, 1105),
                    t => SpaceTextures.Asphalt(H("2C2A34"), H("54505E"), 1107),
                    t => SpaceTextures.Slabs(H("A4A0AC"), H("C2BEC8"), H("68646E"), 2, 0.2f, true, 1109),
                    t => SpaceTextures.Ripples(1111)
                },
                Shallow = H("52DCEA"), Deep = H("2A1A6A"), Foam = new Color(0.9f, 1f, 1f, 0.5f), LiquidGlow = new Color(0.35f, 1.5f, 2.3f, 1f),
                CrackGlow = new Color(1.5f, 0.45f, 1.7f, 1f), Waves = 0.4f, Gloss = 0.8f,
                Sun = new Color(1f, 0.95f, 1f), SunIntensity = 1.25f, Ambient = new Color(0.4f, 0.35f, 0.46f),
                Shadow = new Color(0.05f, 0f, 0.12f, 0.45f), CraterTint = new Color(0.3f, 0.26f, 0.36f, 1f),
                AltShare = 0.2f, RockShare = 0f, Variation = 0.08f, Dunes = 0.5f,
                Stone = GroundSwatch.Salt, Stone2 = GroundSwatch.ChitinDark, Walls = GroundSwatch.Plate, Roof = GroundSwatch.PlateDark,
                Plant = GroundSwatch.CrystalRose, Plant2 = GroundSwatch.CrystalCyan, Scatter = CrystalScatter, BaseDressing = CrystalBase
            };
            var halo = new GroundTheme
            {
                Name = "Halo", Seed = 139, Accent = H("FFD27A"),
                Kinds = new[] { (TerrainKind.Plain, 3), (TerrainKind.Road, 2), (TerrainKind.Base, 2), (TerrainKind.Lake, 2) },
                Layers = new Func<GroundTheme, SpaceTextures.Surface>[]
                {
                    t => SpaceTextures.Slabs(H("A39E94"), H("C2BCB0"), H("5A5650"), 2, 0.25f, true, 1201),
                    t => SpaceTextures.Slabs(H("6A6662"), H("817C76"), H("383532"), 4, 0.2f, true, 1203),
                    t => Wide(SpaceTextures.Slabs(H("383A42"), H("4E505A"), H("14161A"), 4, 0.3f, true, 1205)),
                    t => SpaceTextures.Asphalt(H("2A2C30"), H("44464C"), 1207),
                    t => SpaceTextures.Slabs(H("B2AEA6"), H("CECAC2"), H("66625C"), 1, 0.15f, true, 1209),
                    t => Stars(1211)
                },
                Shallow = H("0A1022"), Deep = H("010208"), Foam = new Color(0.45f, 0.75f, 1f, 0.45f), LiquidGlow = new Color(1.6f, 1.8f, 2.4f, 1f),
                Waves = 0.04f, Gloss = 0f,
                Sun = new Color(1f, 0.96f, 0.88f), SunIntensity = 1.35f, Ambient = new Color(0.3f, 0.32f, 0.38f),
                Shadow = new Color(0f, 0f, 0.03f, 0.55f), CraterTint = new Color(0.14f, 0.13f, 0.12f, 1f),
                AltShare = 0.22f, RockShare = 0.01f, Variation = 0.05f, Blocky = true,
                Stone = GroundSwatch.PlateDark, Stone2 = GroundSwatch.SteelDark, Walls = GroundSwatch.CorpWhite, Roof = GroundSwatch.Plate,
                Plant = GroundSwatch.Flesh, Plant2 = GroundSwatch.Chitin, Scatter = HaloScatter, BaseDressing = StationBase
            };
            return new[] { glacier, reef, hive, crystal, halo };
        }


        /// <summary>The space seen through a breach in the Halo's deck: a dark field with sparse stars that glow (the liquid pattern; low alpha glows).</summary>
        private static SpaceTextures.Surface Stars(int seed)
        {
            return new SpaceTextures.Surface
            {
                Meters = 5f,
                Relief = 0f,
                Shadows = 0f,
                Ambient = 1f,
                Height = (u, v) =>
                {
                    const int cells = 48;
                    int x = Mathf.FloorToInt(u * cells);
                    int y = Mathf.FloorToInt(v * cells);
                    float star = SpaceTextures.Hash(SpaceTextures.Mod(x, cells), SpaceTextures.Mod(y, cells), seed);
                    if (star < 0.93f)
                    {
                        return 1f;
                    }
                    float fx = u * cells - x - 0.5f;
                    float fy = v * cells - y - 0.5f;
                    float r = Mathf.Sqrt(fx * fx + fy * fy);
                    return Mathf.Clamp01(r / (0.18f + (star - 0.93f) * 3f));
                },
                Albedo = (u, v, h) => new Color(h, h, h, 1f)
            };
        }

        // ------------------------------------------------------------------ units, bosses and shots

        /// <summary>
        /// The paints of the campaigns' aircraft and bosses, the glows of their shots and the shield dome, and the procedural
        /// models of their aircraft and boss hulls and parts (Art/Strike, next to the Shadow Strike's).
        /// </summary>
        static partial void BuildStrikeCampaignArt()
        {
            BuildCampaignMaterials();
            BuildCampaignMeshes();
        }


        private static void BuildCampaignMaterials()
        {
            Texture2D palette = Texture("Palette");
            Texture2D paletteEmission = Texture("PaletteEmission");
            // Palette tints of the procedural models; the words of the names pick their Arcade 80s colours (Steel white,
            // Purple violet, Crystal magenta).
            (string name, Color tint)[] tints =
            {
                ("StrikeIronSteel", new Color(0.92f, 0.95f, 1f)), ("StrikeHivePurple", new Color(1f, 0.82f, 0.96f)), ("StrikeCrystalRose", new Color(1f, 0.88f, 1f))
            };
            foreach ((string name, Color tint) in tints)
            {
                AsteroidsAssets.SaveMaterial($"{StrikeArt}/Materials/{name}.mat", Lit, m => SetupLit(m, tint, palette, null, 0.4f, 0.3f, paletteEmission, new Color(2.2f, 2.2f, 2.2f)));
            }
            // The Iron Tide's hull paint: the pack's grey, cold, with orange canopies.
            Material pack = PackMaterial("Unified/StarSparrow Grey");
            AsteroidsAssets.SaveMaterial($"{StrikeArt}/Materials/AirIronSteel.mat", Lit, m =>
            {
                if (pack != null)
                {
                    m.CopyPropertiesFromMaterial(pack);
                    m.shaderKeywords = pack.shaderKeywords;
                }
                m.SetColor("_BaseColor", new Color(0.86f, 0.9f, 0.98f));
                m.SetColor("_Color", new Color(0.86f, 0.9f, 0.98f));
                m.SetColor("_EmissionColor", new Color(2.6f, 1.1f, 0.3f));
                m.EnableKeyword("_EMISSION");
            });
            AsteroidsAssets.SaveMaterial($"{StrikeArt}/Materials/ShieldDome.mat", ShieldShader, m =>
            {
                m.SetColor("_BaseColor", new Color(0.3f, 0.95f, 1.4f, 0.35f));
                m.SetFloat("_FresnelPower", 3f);
                m.SetFloat("_Hex", 1f);
                m.SetFloat("_HexScale", 7f);
                m.SetFloat("_Core", 0.02f);
            });
            Texture2D plasma = Texture("Plasma");
            Texture2D glow = Texture("Glow");
            StrikeGlow("EnemyShellGlow", plasma, new Color(3.2f, 1f, 0.25f, 1f), true);
            StrikeGlow("EnemyShellMarker", Texture("DangerRing"), new Color(2.8f, 0.4f, 0.3f, 0.85f), true);
            StrikeGlow("EnemySporeGlow", plasma, new Color(1.1f, 2.9f, 0.4f, 1f), true);
            StrikeGlow("EnemySporeCore", glow, new Color(2.4f, 0.5f, 2.6f, 0.9f), true);
            StrikeGlow("EnemyNeedle", Texture("Bolt"), new Color(1.3f, 2.6f, 3.4f, 1f), true);
            StrikeGlow("EnemySeekerFlame", glow, new Color(3f, 0.6f, 1.4f, 0.9f), true);
        }


        private static void BuildCampaignMeshes()
        {
            string Path(string name) => $"{StrikeArt}/Models/{name}.asset";
            AsteroidsAssets.SaveMesh(SpaceModels.DroneBody(), Path("Drone"));
            AsteroidsAssets.SaveMesh(SpaceModels.StingerBody(), Path("Stinger"));
            AsteroidsAssets.SaveMesh(SpaceModels.MiteBody(), Path("Mite"));
            BuildCampaignBossMeshes(Path);
        }


        /// <summary>The hulls and parts of the campaigns' bosses (SpaceModels.StrikeCampaignBosses.cs).</summary>
        private static void BuildCampaignBossMeshes(Func<string, string> path)
        {
            // The Iron Tide's parts and hulls.
            AsteroidsAssets.SaveMesh(SpaceModels.HowitzerHead(), path("HowitzerHead"));
            AsteroidsAssets.SaveMesh(SpaceModels.SamRack(), path("SamRack"));
            AsteroidsAssets.SaveMesh(SpaceModels.SweepGun(), path("SweepGun"));
            AsteroidsAssets.SaveMesh(SpaceModels.HangarBay(), path("HangarBay"));
            AsteroidsAssets.SaveMesh(SpaceModels.Radome(1.4f), path("Radome"));
            AsteroidsAssets.SaveMesh(SpaceModels.Radome(2f), path("RadomeLarge"));
            AsteroidsAssets.SaveMesh(SpaceModels.HaulerHull(), path("HaulerHull"));
            AsteroidsAssets.SaveMesh(SpaceModels.IcebreakerHull(), path("IcebreakerHull"));
            AsteroidsAssets.SaveMesh(SpaceModels.CarrierHull(), path("CarrierHull"));
            AsteroidsAssets.SaveMesh(SpaceModels.AtollFortress(), path("AtollFortress"));
            AsteroidsAssets.SaveMesh(SpaceModels.Hydrofoils(), path("Hydrofoils"));
            AsteroidsAssets.SaveMesh(SpaceModels.Drydock(), path("Drydock"));
            AsteroidsAssets.SaveMesh(SpaceModels.BattleshipHull(), path("BattleshipHull"));
            AsteroidsAssets.SaveMesh(SpaceModels.LeviathanHull(), path("LeviathanHull"));
            AsteroidsAssets.SaveMesh(SpaceModels.ReactorCrown(), path("ReactorCrown"));
            // The Hive's parts and bodies.
            AsteroidsAssets.SaveMesh(SpaceModels.HiveMaw(), path("HiveMaw"));
            AsteroidsAssets.SaveMesh(SpaceModels.SporeSac(1f), path("SporeSac"));
            AsteroidsAssets.SaveMesh(SpaceModels.SporeSac(1.4f), path("SporeSacLarge"));
            AsteroidsAssets.SaveMesh(SpaceModels.HiveHeart(1.6f), path("HiveHeart"));
            AsteroidsAssets.SaveMesh(SpaceModels.HiveHeart(2.4f), path("HiveHeartLarge"));
            AsteroidsAssets.SaveMesh(SpaceModels.WyrmSegment(1.3f), path("WyrmSegment"));
            AsteroidsAssets.SaveMesh(SpaceModels.WyrmSegment(1.6f), path("WyrmSegmentLarge"));
            AsteroidsAssets.SaveMesh(SpaceModels.WyrmHead(), path("WyrmHead"));
            AsteroidsAssets.SaveMesh(SpaceModels.SporeTower(), path("SporeTower"));
            AsteroidsAssets.SaveMesh(SpaceModels.BroodMound(), path("BroodMound"));
            AsteroidsAssets.SaveMesh(SpaceModels.TowerRidge(), path("TowerRidge"));
            AsteroidsAssets.SaveMesh(SpaceModels.Burrow(), path("Burrow"));
            AsteroidsAssets.SaveMesh(SpaceModels.BroodShipBody(), path("BroodShipBody"));
            AsteroidsAssets.SaveMesh(SpaceModels.HiveMotherBody(), path("HiveMotherBody"));
            // The crystal things.
            AsteroidsAssets.SaveMesh(SpaceModels.CrystalPrism(1f), path("CrystalPrism"));
            AsteroidsAssets.SaveMesh(SpaceModels.CrystalPrism(1.45f), path("CrystalPrismLarge"));
            AsteroidsAssets.SaveMesh(SpaceModels.CrystalHeart(1.8f), path("CrystalHeart"));
            AsteroidsAssets.SaveMesh(SpaceModels.ColossusBody(), path("ColossusBody"));
            AsteroidsAssets.SaveMesh(SpaceModels.ResonatorBase(), path("ResonatorBase"));
            AsteroidsAssets.SaveMesh(SpaceModels.CrystalRing(5.2f, 6), path("CrystalRing"));
        }


        // ------------------------------------------------------------------ the tiles of the new terrain kinds

        /// <summary>The kinds <see cref="Plan"/> does not lay out itself: the campaigns' Ice, Islands, Nest and Crystals.</summary>
        private static void CampaignTile(TilePlan plan, System.Random random)
        {
            switch (plan.Kind)
            {
                case TerrainKind.Ice: IceTile(plan, random); break;
                case TerrainKind.Islands: IslandsTile(plan, random); break;
                case TerrainKind.Nest: NestTile(plan, random); break;
                case TerrainKind.Crystals: CrystalsTile(plan, random); break;
            }
        }


        /// <summary>Pack ice: snowy floes with a rim of blue ice in dark water; the leads between them stay open.</summary>
        private static void IceTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            (Vector2 at, float radius)[] floes = CampaignTiles.Floes(plan.Variant);
            int seed = theme.Seed + 7 + plan.Variant * 31;
            float Floe(Vector2 p) => CampaignTiles.Field(floes, p, seed, 1.4f);
            plan.Liquid = p => DeepAtEnds(p, Floe(p) + 0.15f);
            plan.Alt = p => 1.1f - Mathf.Abs(Floe(p) + 1.2f);
            plan.Rock = p => -10f;
            plan.Tint = p =>
            {
                float wet = Floe(p) > -0.6f ? 0.86f : 1f;
                return new Color(wet, wet, wet, 1f);
            };
            plan.Blocked = (p, r) => Floe(p) > -1.4f - r || NearAny(floes, p, 2.4f + r);
            foreach ((Vector2 at, float radius) in floes)
            {
                if (radius < 3f)
                {
                    continue;
                }
                for (int i = 0; i < 4; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float distance = radius * (0.45f + (float)random.NextDouble() * 0.3f);
                    Vector2 spot = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    plan.Place(i % 2 == 0 ? PropKind.IceBlock : PropKind.Rock, spot, (float)random.NextDouble() * 360f,
                        i % 2 == 0 ? new Vector3(1.2f, 0.45f, 0.9f) : new Vector3(0.7f, 0.25f, 0.6f), i % 2 == 0 ? GroundSwatch.IceBlue : GroundSwatch.SnowShade,
                        GroundSwatch.Snow, random.Next());
                }
            }
        }


        /// <summary>Islands with beaches and palms in a sea that is shallow and bright near their shores (shoals too).</summary>
        private static void IslandsTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            (Vector2 at, float radius)[] islands = CampaignTiles.Islands(plan.Variant);
            (Vector2 at, float radius)[] shoals = CampaignTiles.Shoals(plan.Variant);
            int seed = theme.Seed + 11 + plan.Variant * 37;
            float Land(Vector2 p) => CampaignTiles.Field(islands, p, seed, 1.8f);
            float Shoal(Vector2 p) => CampaignTiles.Field(shoals, p, seed + 5, 2.5f);
            plan.Liquid = p => DeepAtEnds(p, Mathf.Min(Land(p), Mathf.Max(Shoal(p) * 0.6f + 1.1f, 0.6f)));
            plan.Alt = p => -Land(p) - 1.8f;
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Min(rock(p), -Land(p) - 1f);
            plan.Tint = p =>
            {
                float land = -Land(p);
                float wet = land > 0f ? Mathf.Lerp(0.75f, 1f, SpaceTextures.Step(0f, 1.2f, land)) : 0.8f;
                return new Color(wet, wet, wet, 1f);
            };
            plan.Blocked = (p, r) => Land(p) > -1.2f - r || NearAny(islands, p, 2.3f + r);
            foreach ((Vector2 at, float radius) in islands)
            {
                int palms = Mathf.RoundToInt(radius * 2.2f);
                for (int i = 0; i < palms; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    float distance = radius * (0.5f + (float)random.NextDouble() * 0.4f);
                    Vector2 spot = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    bool palm = random.NextDouble() < 0.6;
                    plan.Place(palm ? PropKind.Palm : PropKind.Bush, spot, (float)random.NextDouble() * 360f,
                        palm ? new Vector3(2.4f, 0.85f, 2.4f) * (0.8f + (float)random.NextDouble() * 0.3f) : new Vector3(1.1f, 0.35f, 1f), theme.Plant, theme.Plant2, random.Next());
                }
                for (int i = 0; i < 3; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    Vector2 spot = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius + 0.4f);
                    if (Mathf.Abs(spot.y) < 9f)
                    {
                        plan.Place(PropKind.Rock, spot, (float)random.NextDouble() * 360f, new Vector3(0.8f, 0.3f, 0.6f), theme.Stone, theme.Stone2, random.Next(), false);
                    }
                }
            }
        }


        /// <summary>A hive nest: chitin mounds in a ring of creep, egg pods and tendrils around them, brood pits of glowing acid.</summary>
        private static void NestTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            (Vector2 at, float radius)[] mounds = CampaignTiles.Mounds(plan.Variant);
            (Vector2 at, float radius)[] pits = CampaignTiles.Pits(plan.Variant);
            int seed = theme.Seed + 13 + plan.Variant * 41;
            float Mound(Vector2 p) => CampaignTiles.Field(mounds, p, seed, 1.6f);
            float Pit(Vector2 p) => CampaignTiles.Field(pits, p, seed + 3, 0.8f);
            plan.Liquid = p => -Pit(p);
            Func<Vector2, float> alt = plan.Alt;
            plan.Alt = p => Mathf.Max(alt(p), 2.6f - Mound(p));
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Max(Mathf.Min(rock(p), Pit(p) - 1f), -Mound(p) - 0.4f);
            plan.Blocked = (p, r) => NearAny(mounds, p, 2.2f + r) || Pit(p) < 1f + r;
            foreach ((Vector2 at, float radius) in mounds)
            {
                int pods = Mathf.RoundToInt(radius * 2f);
                for (int i = 0; i < pods; i++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    Vector2 spot = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius + 0.6f + (float)random.NextDouble() * 1.6f);
                    bool pod = random.NextDouble() < 0.6;
                    plan.Place(pod ? PropKind.Pod : PropKind.Tendril, spot, (float)random.NextDouble() * 360f,
                        pod ? new Vector3(2f, 0.6f, 2f) : new Vector3(1.1f, 1f, 1.1f), pod ? GroundSwatch.Flesh : GroundSwatch.Chitin,
                        pod ? GroundSwatch.Ichor : GroundSwatch.ChitinDark, random.Next());
                }
            }
            theme.Scatter?.Invoke(plan, random, 0.6f);
        }


        /// <summary>A crystal field: dark glass and tall crystal clusters between lanes of dust that units drive along.</summary>
        private static void CrystalsTile(TilePlan plan, System.Random random)
        {
            GroundTheme theme = plan.Theme;
            float[] lanes = CampaignTiles.Lanes(plan.Variant);
            float Lane(Vector2 p) => CampaignTiles.LaneField(lanes, p.x) + (SpaceTextures.PNoise(p.x / 64f + 0.5f, p.y / 20f + 0.5f, 16, 5, theme.Seed + 21) - 0.5f) * 0.8f;
            Func<Vector2, float> alt = plan.Alt;
            plan.Alt = p => Mathf.Max(alt(p), 0.4f - Lane(p));
            Func<Vector2, float> rock = plan.Rock;
            plan.Rock = p => Mathf.Max(rock(p), Lane(p) - 1.6f);
            plan.Blocked = (p, r) => Lane(p) < 1.3f + r;
            plan.Sprinkle(random, 40, 0.5f, 31f, r => (PropKind.Shard, new Vector3(3.4f, 1.6f, 3.4f) * (0.7f + (float)r.NextDouble() * 0.5f),
                r.NextDouble() < 0.5 ? GroundSwatch.CrystalRose : GroundSwatch.CrystalCyan, GroundSwatch.CrystalWhite));
            plan.Sprinkle(random, 24, 0.5f, 31f, r => (PropKind.Crystal, new Vector3(1.1f, 0.6f, 1.1f), GroundSwatch.CrystalWhite,
                r.NextDouble() < 0.5 ? GroundSwatch.CrystalRose : GroundSwatch.CrystalCyan));
            plan.Sprinkle(random, 14, 0.5f, 31f, r => (PropKind.Rock, new Vector3(0.7f, 0.25f, 0.6f), theme.Stone2, theme.Stone));
        }


        /// <summary>
        /// Open water deepens toward the tile's ends, so a tile of floes or islands joins a tile of open sea without a seam
        /// (land stays land: the features keep clear of the ends).
        /// </summary>
        private static float DeepAtEnds(Vector2 p, float liquid)
        {
            return liquid > 0f ? Mathf.Max(liquid, (Mathf.Abs(p.y) - 7.5f) * 5f) : liquid;
        }


        /// <summary>Whether <paramref name="p"/> is within <paramref name="reach"/> of the centre of any feature on screen (the units' spots).</summary>
        private static bool NearAny((Vector2 at, float radius)[] features, Vector2 p, float reach)
        {
            foreach ((Vector2 at, float radius) in features)
            {
                if (Mathf.Abs(at.x) < 16f && (p - at).sqrMagnitude < reach * reach)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------ dressing

        private static void GlacierScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(22 * density), 14f, 31f, r => Pick(r, PropKind.Pine, new Vector3(2.2f, 0.9f, 2.2f), 0.3f, GroundSwatch.Pine, GroundSwatch.Snow));
            plan.Sprinkle(random, (int)(16 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.6f, 0.22f, 0.5f), 0.4f, r.NextDouble() < 0.6 ? GroundSwatch.SnowShade : GroundSwatch.StoneDark, t.Stone));
            plan.Sprinkle(random, (int)(6 * density), 4f, 31f, r => Pick(r, PropKind.Crystal, new Vector3(1.1f, 0.7f, 1.1f), 0.3f, GroundSwatch.IceBlue, GroundSwatch.IceDeep));
            plan.Sprinkle(random, (int)(5 * density), 12f, 31f, r => Pick(r, PropKind.IceBlock, new Vector3(1.6f, 0.5f, 1.2f), 0.3f, GroundSwatch.IceBlue, GroundSwatch.Snow));
            plan.Sprinkle(random, (int)(4 * density), 15f, 31f, r => Pick(r, PropKind.Boulders, new Vector3(1.8f, 0.55f, 1.8f), 0.3f, GroundSwatch.SnowShade, GroundSwatch.StoneDark));
        }


        private static void ReefScatter(TilePlan plan, System.Random random, float density)
        {
            GroundTheme t = plan.Theme;
            plan.Sprinkle(random, (int)(18 * density), 12f, 31f, r => Pick(r, PropKind.Palm, new Vector3(2.5f, 0.85f, 2.5f), 0.25f, GroundSwatch.Palm, GroundSwatch.Leaf));
            plan.Sprinkle(random, (int)(20 * density), 0.5f, 31f, r => Pick(r, PropKind.Bush, new Vector3(1.1f, 0.35f, 1f), 0.35f, r.NextDouble() < 0.5 ? GroundSwatch.LeafDark : GroundSwatch.Moss, t.Plant));
            plan.Sprinkle(random, (int)(10 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.6f, 0.22f, 0.5f), 0.4f, r.NextDouble() < 0.5 ? GroundSwatch.Coral : GroundSwatch.StoneDark, t.Stone));
            plan.Sprinkle(random, (int)(8 * density), 3f, 31f, r => Pick(r, PropKind.Fern, new Vector3(1f, 0.3f, 1f), 0.3f, GroundSwatch.Leaf, GroundSwatch.LeafLight));
        }


        private static void HiveScatter(TilePlan plan, System.Random random, float density)
        {
            plan.Sprinkle(random, (int)(10 * density), 14f, 31f, r => Pick(r, PropKind.Spire, new Vector3(1.8f, 0.9f, 1.8f), 0.3f, GroundSwatch.Chitin, GroundSwatch.ChitinDark));
            plan.Sprinkle(random, (int)(12 * density), 3f, 31f, r => Pick(r, PropKind.Pod, new Vector3(1.9f, 0.55f, 1.9f), 0.3f, GroundSwatch.Flesh, GroundSwatch.Ichor));
            plan.Sprinkle(random, (int)(10 * density), 6f, 31f, r => Pick(r, PropKind.Tendril, new Vector3(0.8f, 0.8f, 0.8f), 0.3f, GroundSwatch.Chitin, GroundSwatch.ChitinDark));
            plan.Sprinkle(random, (int)(14 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.6f, 0.22f, 0.5f), 0.4f, r.NextDouble() < 0.5 ? GroundSwatch.Chitin : GroundSwatch.Resin, GroundSwatch.ChitinDark));
        }


        private static void CrystalScatter(TilePlan plan, System.Random random, float density)
        {
            plan.Sprinkle(random, (int)(10 * density), 13f, 31f, r => Pick(r, PropKind.Shard, new Vector3(3f, 1.4f, 3f), 0.35f, r.NextDouble() < 0.5 ? GroundSwatch.CrystalRose : GroundSwatch.CrystalCyan, GroundSwatch.CrystalWhite));
            plan.Sprinkle(random, (int)(10 * density), 2f, 31f, r => Pick(r, PropKind.Crystal, new Vector3(1f, 0.55f, 1f), 0.3f, GroundSwatch.CrystalWhite, r.NextDouble() < 0.5 ? GroundSwatch.CrystalRose : GroundSwatch.CrystalCyan));
            plan.Sprinkle(random, (int)(14 * density), 0.5f, 31f, r => Pick(r, PropKind.Rock, new Vector3(0.6f, 0.22f, 0.5f), 0.4f, r.NextDouble() < 0.5 ? GroundSwatch.Salt : GroundSwatch.ChitinDark, GroundSwatch.Salt));
        }


        private static void HaloScatter(TilePlan plan, System.Random random, float density)
        {
            plan.Sprinkle(random, (int)(8 * density), 15f, 31f, r => Pick(r, PropKind.Vent, new Vector3(1.6f, 0.35f, 1.6f), 0.25f, GroundSwatch.PlateDark, GroundSwatch.Pipe));
            plan.Sprinkle(random, (int)(6 * density), 16f, 31f, r => Pick(r, PropKind.Container, new Vector3(1.3f, 0.5f, 2.6f), 0.15f, r.NextDouble() < 0.5 ? GroundSwatch.CorpWhite : GroundSwatch.Hazard, GroundSwatch.PlateDark));
            plan.Sprinkle(random, (int)(4 * density), 17f, 30f, r => Pick(r, PropKind.Pipe, new Vector3(1f, 0.45f, 6f), 0.3f, GroundSwatch.Pipe, GroundSwatch.PipeDark));
            // The Hive has come aboard: pods and tendrils grow over the deck.
            plan.Sprinkle(random, (int)(6 * density), 4f, 31f, r => Pick(r, PropKind.Pod, new Vector3(1.2f, 0.4f, 1.2f), 0.3f, GroundSwatch.Flesh, GroundSwatch.Ichor));
            plan.Sprinkle(random, (int)(4 * density), 8f, 31f, r => Pick(r, PropKind.Tendril, new Vector3(0.8f, 0.8f, 0.8f), 0.3f, GroundSwatch.Chitin, GroundSwatch.ChitinDark));
            for (int i = 0; i < 3; i++)
            {
                var at = new Vector3((random.NextDouble() < 0.5 ? -1f : 1f) * (10f + (float)random.NextDouble() * 18f), ((float)random.NextDouble() - 0.5f) * 16f, -0.05f);
                plan.Lights.Add((at, 3f, new Color(1f, 0.85f, 0.5f, 0.3f)));
            }
        }


        /// <summary>The Iron Tide's base: white hangars with orange stripes, a radome, tanks and stacked containers.</summary>
        private static void CorporateBase(TilePlan plan, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    plan.Place(PropKind.Fence, new Vector2(side * 15.4f, -7f + i * 7f), 90f, new Vector3(5.2f, 0.4f, 0.1f), GroundSwatch.CorpSteel, GroundSwatch.CorpSteel, 0, false);
                }
                bool hangar = (plan.Variant + (side > 0 ? 1 : 0)) % 2 == 0;
                if (hangar)
                {
                    plan.Place(PropKind.Hangar, new Vector2(side * 21.5f, 0.5f), 90f, new Vector3(5.4f, 0.85f, 8f), GroundSwatch.CorpWhite, GroundSwatch.CorpOrange, random.Next(), false);
                }
                else
                {
                    plan.Place(PropKind.Dome, new Vector2(side * 21f, -2f), 0f, new Vector3(5f, 0.9f, 5f), GroundSwatch.CorpWhite, GroundSwatch.CorpSteel, random.Next(), false);
                    plan.Place(PropKind.Building, new Vector2(side * 20.5f, 5.5f), 0f, new Vector3(4.5f, 0.6f, 3f), GroundSwatch.CorpWhite, GroundSwatch.CorpOrange, random.Next(), false);
                }
                plan.Place(PropKind.Tank, new Vector2(side * 27.5f, -5f), 0f, new Vector3(2.4f, 0.8f, 2.4f), GroundSwatch.CorpWhite, GroundSwatch.CorpOrange, 0, false);
                plan.Place(PropKind.Container, new Vector2(side * 27.5f, 2.5f), 0f, new Vector3(1.3f, 0.55f, 2.8f), GroundSwatch.CorpOrange, GroundSwatch.CorpSteel, 0, false);
                plan.Place(PropKind.Container, new Vector2(side * 25.8f, 2.8f), 0f, new Vector3(1.3f, 0.55f, 2.8f), GroundSwatch.CorpSteel, GroundSwatch.CorpOrange, 0, false);
                plan.Place(PropKind.Antenna, new Vector2(side * 17.4f, side * 7.5f), 0f, new Vector3(1.3f, 0.9f, 1.3f), GroundSwatch.Metal, GroundSwatch.GlowOrange, 0, false);
            }
        }


        /// <summary>A colony base the Hive overran: its buildings stand among pods and tendrils, resin over the roofs.</summary>
        private static void OverrunBase(TilePlan plan, System.Random random)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                plan.Place(PropKind.Dome, new Vector2(side * 21f, -2.5f), 0f, new Vector3(5f, 0.85f, 5f), GroundSwatch.ConcreteLight, GroundSwatch.Resin, random.Next(), false);
                plan.Place(PropKind.Building, new Vector2(side * 20f, 5.5f), 0f, new Vector3(4f, 0.55f, 3f), GroundSwatch.ConcreteDark, GroundSwatch.Flesh, random.Next(), false);
                plan.Place(PropKind.Pipe, new Vector2(side * 17f, 0f), 0f, new Vector3(1f, 0.4f, 17f), GroundSwatch.Pipe, GroundSwatch.PipeDark, 0, false);
                for (int i = 0; i < 4; i++)
                {
                    var at = new Vector2(side * (24f + (float)random.NextDouble() * 6f), -8f + i * 5f);
                    plan.Place(i % 2 == 0 ? PropKind.Pod : PropKind.Tendril, at, (float)random.NextDouble() * 360f,
                        i % 2 == 0 ? new Vector3(1.6f, 0.5f, 1.6f) : new Vector3(0.9f, 0.9f, 0.9f), i % 2 == 0 ? GroundSwatch.Flesh : GroundSwatch.Chitin,
                        i % 2 == 0 ? GroundSwatch.Ichor : GroundSwatch.ChitinDark, random.Next(), false);
                }
            }
        }


        /// <summary>A mining colony among the crystals: the colony's domes and panels, crystal clusters crowding in.</summary>
        private static void CrystalBase(TilePlan plan, System.Random random)
        {
            ColonyBase(plan, random);
            for (int side = -1; side <= 1; side += 2)
            {
                plan.Place(PropKind.Shard, new Vector2(side * 29f, 7f), (float)random.NextDouble() * 360f, new Vector3(2.4f, 0.9f, 2.4f),
                    side < 0 ? GroundSwatch.CrystalRose : GroundSwatch.CrystalCyan, GroundSwatch.CrystalWhite, random.Next(), false);
            }
        }
    }
}
