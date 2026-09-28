using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Editor;
using Gamebox.Launcher;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Portfolio.Asteroids.EditorTools
{
    /// <summary>
    /// Builds the theme assets of the Asteroids module from the art and prefabs on disk: the Classic theme, which lists
    /// every material, mesh and sprite the prefabs and the scene were built with (by the keys the themed parts carry, see
    /// <see cref="ThemeKeys"/>) and the typed looks the game's own code reads; and one theme per generated look
    /// (<see cref="AsteroidsArtBuilder.GeneratedThemes"/>), whose art is derived from the Classic entries. The game's
    /// definition then starts with Classic and lists them all. Runs after the prefabs and the campaign content.
    /// </summary>
    internal static class AsteroidsThemeBuilder
    {
        public const string ClassicPath = "Config/Themes/Game/Classic.asset";

        /// <summary>The Classic art, keyed: every material, mesh (with every material seen on it) and sprite the game shows.</summary>
        private sealed class Catalog
        {
            public readonly List<(string key, Material material)> Materials = new List<(string, Material)>();
            public readonly List<(string key, Mesh mesh, List<Material> materials)> Meshes = new List<(string, Mesh, List<Material>)>();
            public readonly List<(string key, Sprite sprite)> Sprites = new List<(string, Sprite)>();

            public void Add(string key, Material material)
            {
                if (key != null && material != null && Materials.All(entry => entry.key != key))
                {
                    Materials.Add((key, material));
                }
            }

            /// <summary>Lists <paramref name="mesh"/> under <paramref name="key"/>, with <paramref name="material"/> among the materials it is drawn with.</summary>
            public void Add(string key, Mesh mesh, Material material)
            {
                if (key == null || mesh == null)
                {
                    return;
                }
                int index = Meshes.FindIndex(entry => entry.key == key);
                if (index < 0)
                {
                    Meshes.Add((key, mesh, new List<Material>()));
                    index = Meshes.Count - 1;
                }
                if (material != null && !Meshes[index].materials.Contains(material))
                {
                    Meshes[index].materials.Add(material);
                }
            }

            public void Add(string key, Sprite sprite)
            {
                if (key != null && sprite != null && Sprites.All(entry => entry.key != key))
                {
                    Sprites.Add((key, sprite));
                }
            }
        }

        public static string ThemePath(ThemeSpec spec)
        {
            return $"Config/Themes/Game/{spec.Name}.asset";
        }

        public static AsteroidsTheme Classic => AsteroidsAssets.Load<AsteroidsTheme>(ClassicPath);

        public static AsteroidsTheme Theme(ThemeSpec spec)
        {
            return AsteroidsAssets.Load<AsteroidsTheme>(ThemePath(spec));
        }


        /// <summary>Builds every theme asset and points the game's definition at them; a theme that is not complete is a build problem.</summary>
        public static void BuildAll()
        {
            EditorUtility.DisplayProgressBar("Asteroids", "Building themes...", 0.5f);
            Catalog catalog = Gather();
            AsteroidsTheme classic = BuildClassic(catalog);
            var themes = new List<AsteroidsTheme> { classic };
            foreach (ThemeSpec spec in AsteroidsArtBuilder.GeneratedThemes)
            {
                AsteroidsTheme generated = BuildGenerated(spec, classic, catalog);
                if (generated != null)
                {
                    themes.Add(generated);
                }
            }
            UpdateDefinition(themes);
            foreach (AsteroidsTheme theme in themes)
            {
                theme.Reindex();
                foreach (string problem in ThemeTools.Validate(theme))
                {
                    AsteroidsAssets.Problem($"theme {theme.name}: {problem}");
                }
            }
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        // ------------------------------------------------------------------ the classic art

        /// <summary>
        /// Every keyed asset of the Classic art: the materials under Art/Materials, Art/Strike/Materials and Art/Ground/Materials,
        /// what the prefabs' themed parts refer to (pack materials and meshes included), the space quad's parts, and the
        /// sprites of the interface, the icons and the hangar pictures.
        /// </summary>
        private static Catalog Gather()
        {
            var catalog = new Catalog();
            foreach (Material material in Find<Material>("t:Material", "Art/Materials", "Art/Strike/Materials", "Art/Ground/Materials"))
            {
                catalog.Add(ThemeKeys.MaterialKey(material), material);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { AsteroidsAssets.Path("Prefabs") }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Prefabs/Themes/"))
                {
                    continue;
                }
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }
                foreach (ThemedRenderer themed in prefab.GetComponentsInChildren<ThemedRenderer>(true))
                {
                    Material[] materials = themed.GetComponent<Renderer>().sharedMaterials;
                    if (themed.Slot >= 0 && themed.Slot < materials.Length)
                    {
                        catalog.Add(themed.MaterialKey, materials[themed.Slot]);
                    }
                }
                foreach (ThemedMesh themed in prefab.GetComponentsInChildren<ThemedMesh>(true))
                {
                    var renderer = themed.GetComponent<Renderer>();
                    catalog.Add(themed.MeshKey, themed.GetComponent<MeshFilter>().sharedMesh, renderer != null ? renderer.sharedMaterial : null);
                }
            }
            // The space behind the playfield (the scene's, built after the themes): its parts by the same keys.
            Material clouds = AsteroidsArtBuilder.PackMaterial("BonusContent/CloudsRed");
            catalog.Add(ThemeKeys.MaterialKey(clouds), clouds);
            Mesh planet = AsteroidsArtBuilder.PackMesh("BonusContent/Planet.FBX", "Planet");
            Mesh cloudMesh = AsteroidsArtBuilder.PackMesh("BonusContent/Clouds.FBX", "Clouds");
            catalog.Add(ThemeKeys.MeshKey(planet), planet, AsteroidsArtBuilder.Material("PlanetKepler"));
            catalog.Add(ThemeKeys.MeshKey(cloudMesh), cloudMesh, clouds);
            Mesh ring = AsteroidsArtBuilder.Model("PlanetRing");
            catalog.Add(ThemeKeys.MeshKey(ring), ring, AsteroidsArtBuilder.Material("PlanetRing"));
            // The hulls and the rocks are set by their own code, from the typed looks; their meshes are listed all the same.
            foreach ((string name, string model, string material, Color engine) in AsteroidsArtBuilder.Hulls)
            {
                Mesh mesh = AsteroidsArtBuilder.ShipMesh(model);
                catalog.Add(ThemeKeys.MeshKey(mesh), mesh, AsteroidsArtBuilder.PackMaterial(material));
            }
            foreach (AsteroidKind kind in Enum.GetValues(typeof(AsteroidKind)))
            {
                Asteroid asteroid = AsteroidsPrefabBuilder.Load<Asteroid>($"Asteroids/{kind}");
                if (asteroid == null)
                {
                    continue;
                }
                Material rock = asteroid.meshFilter != null ? asteroid.meshFilter.GetComponent<Renderer>().sharedMaterial : null;
                foreach (Mesh mesh in asteroid.meshes)
                {
                    catalog.Add(ThemeKeys.MeshKey(mesh), mesh, rock);
                }
            }
            foreach (Sprite sprite in Find<Sprite>("t:Sprite", "Art/Interface", "Art/Icons", "Art/Strike/Icons", "Art/Previews"))
            {
                catalog.Add(ThemeKeys.SpriteKey(sprite), sprite);
            }
            return catalog;
        }


        private static List<T> Find<T>(string filter, params string[] folders) where T : Object
        {
            var found = new List<T>();
            var paths = new List<string>();
            foreach (string folder in folders)
            {
                if (AssetDatabase.IsValidFolder(AsteroidsAssets.Path(folder)))
                {
                    paths.Add(AsteroidsAssets.Path(folder));
                }
            }
            if (paths.Count == 0)
            {
                return found;
            }
            foreach (string guid in AssetDatabase.FindAssets(filter, paths.ToArray()).Distinct().OrderBy(AssetDatabase.GUIDToAssetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    found.Add(asset);
                }
            }
            return found;
        }

        // ------------------------------------------------------------------ classic

        private static AsteroidsTheme BuildClassic(Catalog catalog)
        {
            return AsteroidsAssets.SaveScriptable<AsteroidsTheme>(ClassicPath, theme =>
            {
                AsteroidsAssets.Set(theme, "displayName", p => p.stringValue = "Classic");
                AsteroidsAssets.Set(theme, "description", p => p.stringValue = "The game as built: painted hulls, textured rocks, nebulae and planets, the six worlds of the strike mode.");
                AsteroidsAssets.SetObject(theme, "preview", AsteroidsArtBuilder.Preview("Sparrow"));
                theme.ships.Clear();
                foreach (PlayerSettings hull in AsteroidsContentBuilder.Hangar)
                {
                    if (hull == null)
                    {
                        continue;
                    }
                    theme.ships.Add(new AsteroidsTheme.ShipLook
                    {
                        hull = hull.DisplayName, mesh = hull.ModelMesh, material = hull.ModelMaterial, scale = hull.ModelScale, engineColor = hull.EngineColor,
                        preview = AsteroidsArtBuilder.Preview(hull.DisplayName)
                    });
                }
                theme.asteroids.Clear();
                foreach (AsteroidKind kind in Enum.GetValues(typeof(AsteroidKind)))
                {
                    Asteroid asteroid = AsteroidsPrefabBuilder.Load<Asteroid>($"Asteroids/{kind}");
                    if (asteroid == null)
                    {
                        continue;
                    }
                    theme.asteroids.Add(new AsteroidsTheme.AsteroidLook
                    {
                        kind = kind, meshes = asteroid.meshes.ToArray(), flash = asteroid.flashColor,
                        material = asteroid.meshFilter != null ? asteroid.meshFilter.GetComponent<Renderer>().sharedMaterial : null
                    });
                }
                theme.shots = Named("Shots");
                theme.enemies = Named("Enemies", "Hazards", "Bosses");
                theme.effects = new AsteroidsTheme.EffectMaterials
                {
                    particleAdd = M("ParticleAdd"), particleSpark = M("ParticleSpark"), particleFlare = M("ParticleFlare"), particleShard = M("ParticleShard"),
                    particleSmoke = M("ParticleSmoke"), particleRing = M("ParticleRing"), glow = M("Glow"), flare = M("Flare"), ring = M("Ring"), dangerRing = M("DangerRing"),
                    muzzle = M("Muzzle"), engineFlame = M("EngineFlame"), bossFlame = M("BossFlame"), shipShield = M("ShipShield"), bossShield = M("BossShield"), shipHalo = M("ShipHalo")
                };
                theme.pickups.Clear();
                foreach (GameObject prefab in Prefabs("Pickups"))
                {
                    Material core = null;
                    Material icon = null;
                    foreach (Material material in Materials(prefab))
                    {
                        if (material.name.StartsWith("Core", StringComparison.Ordinal))
                        {
                            core = core != null ? core : material;
                        }
                        else if (material.name.StartsWith("Icon", StringComparison.Ordinal))
                        {
                            icon = icon != null ? icon : material;
                        }
                    }
                    // A pickup without a core (the crystal, a gem) is its first material through and through; its icon is the one of its name.
                    Material first = Materials(prefab).FirstOrDefault();
                    Sprite sprite = icon != null ? AsteroidsArtBuilder.Icon(icon.name.Substring("Icon".Length)) : null;
                    theme.pickups.Add(new AsteroidsTheme.PickupLook
                    {
                        name = prefab.name, core = core != null ? core : first, icon = icon != null ? icon : first,
                        sprite = sprite != null ? sprite : AsteroidsArtBuilder.Icon(prefab.name) != null ? AsteroidsArtBuilder.Icon(prefab.name) : AsteroidsArtBuilder.Icon("Star")
                    });
                }
                theme.backdrop = new AsteroidsTheme.BackdropLook
                {
                    space = M("Space"), atmosphere = M("Atmosphere"), halo = M("AtmosphereHalo"), rings = M("PlanetRing"), motes = M("ParticleAdd")
                };
                theme.sectors = AsteroidsArtBuilder.SectorNames.Select(AsteroidsContentBuilder.Theme).Where(sector => sector != null).ToList();
                theme.strikeThemes = AsteroidsArtBuilder.GroundThemes.Select(world => AsteroidsAssets.Load<StrikeTheme>(world.AssetPath)).Where(world => world != null).ToList();
                Sprite clear = AsteroidsArtBuilder.Interface("Clear");
                theme.ui = new AsteroidsTheme.InterfaceLook
                {
                    panel = I("Panel"), button = I("Button"), frame = I("Frame"), hexagon = I("Hexagon"), hexagonFrame = I("HexagonFrame"), bar = I("Bar"),
                    timerRing = I("TimerRing"), vignette = I("Vignette"), fade = I("Fade"), lane = I("Lane"), glow = I("Glow"), scanlines = clear, crt = clear,
                    starFull = AsteroidsArtBuilder.Icon("Star"), starEmpty = AsteroidsArtBuilder.Icon("StarEmpty"), weapons = WeaponIcons(AsteroidsArtBuilder.Icon)
                };
                theme.icons = Find<Sprite>("t:Sprite", "Art/Icons").Select(sprite => new AsteroidsTheme.NamedSprite { name = sprite.name, sprite = sprite }).ToList();
                theme.strikeIcons = Find<Sprite>("t:Sprite", "Art/Strike/Icons").Select(sprite => new AsteroidsTheme.NamedSprite { name = sprite.name, sprite = sprite }).ToList();
                theme.palette = new AsteroidsTheme.Palette();
                TMP_FontAsset font = TMP_Settings.defaultFontAsset;
                theme.Fonts.body = font;
                theme.Fonts.bodyMaterial = AsteroidsSceneBuilder.HudFont;
                theme.Fonts.title = font;
                theme.Fonts.titleMaterial = AsteroidsSceneBuilder.TitleFont;
                FillMenu(theme.Menu, theme.palette, I("Panel"), I("Button"), clear);
                theme.Slots.materials.Clear();
                foreach ((string key, Material material) in catalog.Materials)
                {
                    ThemeSlots.Set(theme.Slots.materials, key, material);
                }
                theme.Slots.meshes.Clear();
                foreach ((string key, Mesh mesh, List<Material> drawnWith) in catalog.Meshes)
                {
                    ThemeSlots.Set(theme.Slots.meshes, key, mesh);
                }
                theme.Slots.sprites.Clear();
                foreach ((string key, Sprite sprite) in catalog.Sprites)
                {
                    ThemeSlots.Set(theme.Slots.sprites, key, sprite);
                }
                ThemeSlots.Set(theme.Slots.sprites, "Interface/Scanlines", clear);
                ThemeSlots.Set(theme.Slots.sprites, "Interface/Crt", clear);
                theme.Slots.fonts.Clear();
                ThemeSlots.Set(theme.Slots.fonts, "plain", font);
            });
        }


        /// <summary>The shared menu in the game's look: the panels, the buttons and the colours the interface builder gives the pause menu.</summary>
        private static void FillMenu(MenuSkin skin, AsteroidsTheme.Palette palette, Sprite panel, Sprite button, Sprite clear)
        {
            skin.backdrop = panel;
            skin.backdropColor = palette.panel;
            skin.window = panel;
            skin.windowColor = palette.settingsWindow;
            skin.tileWindow = false;
            skin.settingsWindow = panel;
            skin.headerBanner = clear;
            skin.rowsBackground = panel;
            skin.rowsColor = palette.settingsRows;
            skin.rowBackground = clear;
            skin.rowColor = Color.white;
            skin.button = button;
            skin.buttonHover = button;
            skin.buttonPressed = button;
            skin.buttonDisabled = button;
            skin.arrowButton = button;
            skin.inputBox = button;
            skin.inputColor = palette.inputField;
            skin.inputTextColor = Color.white;
            skin.track = clear;
            skin.fill = clear;
            skin.fillColor = default;
            skin.knob = clear;
            skin.checkBox = clear;
            skin.checkMark = clear;
            skin.headerColor = palette.cyan;
            skin.textColor = palette.soft;
            skin.valueColor = Color.white;
            skin.buttonTextColor = Color.white;
            skin.errorColor = palette.red;
        }


        private static Material M(string name)
        {
            return AsteroidsArtBuilder.Material(name);
        }

        private static Sprite I(string name)
        {
            return AsteroidsArtBuilder.Interface(name);
        }

        private static Sprite[] WeaponIcons(Func<string, Sprite> iconOf)
        {
            return new[] { iconOf("Blaster"), iconOf("Laser"), iconOf("Scatter"), iconOf("Missile") };
        }

        private static IEnumerable<GameObject> Prefabs(string folder)
        {
            string path = AsteroidsAssets.Path($"Prefabs/{folder}");
            if (!AssetDatabase.IsValidFolder(path))
            {
                yield break;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { path }).Distinct().OrderBy(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null)
                {
                    yield return prefab;
                }
            }
        }

        /// <summary>The materials of a prefab's renderers, in order, each once.</summary>
        private static List<Material> Materials(GameObject prefab)
        {
            var materials = new List<Material>();
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<TMP_Text>() != null)
                {
                    continue;
                }
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && !materials.Contains(material))
                    {
                        materials.Add(material);
                    }
                }
            }
            return materials;
        }

        /// <summary>The prefabs of these folders by name, each with the first material of its renderers.</summary>
        private static List<AsteroidsTheme.NamedMaterial> Named(params string[] folders)
        {
            var list = new List<AsteroidsTheme.NamedMaterial>();
            foreach (string folder in folders)
            {
                foreach (GameObject prefab in Prefabs(folder))
                {
                    Material first = Materials(prefab).FirstOrDefault();
                    if (first != null)
                    {
                        list.Add(new AsteroidsTheme.NamedMaterial { name = prefab.name, material = first });
                    }
                }
            }
            return list;
        }

        // ------------------------------------------------------------------ generated themes

        /// <summary>
        /// The theme of a generated look: the Classic entries mapped through the spec's derivations (a neon material for
        /// every material, a wire copy for every mesh a hull shader draws, a neon sprite for every sprite the art builder
        /// traced), its own sectors and world, fonts, palette and menu.
        /// </summary>
        private static AsteroidsTheme BuildGenerated(ThemeSpec spec, AsteroidsTheme classic, Catalog catalog)
        {
            if (AsteroidsArtBuilder.NeonSurfaceShader(spec) == null)
            {
                AsteroidsAssets.Problem($"The {spec.DisplayName} theme was not built: its shaders are missing.");
                return null;
            }
            string neonSurface = AsteroidsArtBuilder.NeonSurfaceShader(spec).name;
            var materials = new Dictionary<string, Material>();
            foreach ((string key, Material material) in catalog.Materials)
            {
                materials[key] = AsteroidsArtBuilder.ThemeMaterial(spec, material, key);
            }
            Material Of(Material source)
            {
                string key = ThemeKeys.MaterialKey(source);
                if (key == null)
                {
                    return source;
                }
                if (!materials.TryGetValue(key, out Material derived))
                {
                    derived = AsteroidsArtBuilder.ThemeMaterial(spec, source, key);
                    materials[key] = derived;
                }
                return derived;
            }
            // A mesh gets a wire copy when any of the materials it is drawn with becomes a neon surface (the ship's hull, which
            // its drop shadow shares; the shadow shader reads positions only). A glow must not share a mesh with a surface.
            bool Wired(IEnumerable<Material> drawnWith)
            {
                bool wired = false;
                foreach (Material material in drawnWith)
                {
                    Material derived = material != null ? Of(material) : null;
                    wired |= derived != null && derived.shader != null && derived.shader.name == neonSurface;
                }
                return wired;
            }
            // The theme's own shapes stand in for some Classic meshes (the dense scanned rocks) before they are wired.
            Dictionary<string, Mesh> shapes = AsteroidsArtBuilder.ThemeShapes(spec);
            var meshes = new Dictionary<string, Mesh>();
            foreach ((string key, Mesh classicMesh, List<Material> drawnWith) in catalog.Meshes)
            {
                Mesh mesh = shapes.TryGetValue(key, out Mesh shape) ? shape : classicMesh;
                bool wired = Wired(drawnWith);
                if (wired && drawnWith.Any(material => material != null && material.shader != null && material.shader.name == "Portfolio/Asteroids/Glow"))
                {
                    AsteroidsAssets.Problem($"{spec.DisplayName}: the mesh {key} is drawn both as a surface and as a glow; its wire copy would tint the glow.");
                }
                meshes[key] = wired ? AsteroidsArtBuilder.WireMesh(spec, mesh, key) : mesh;
            }
            Mesh MeshOf(Mesh source, Material first)
            {
                string key = ThemeKeys.MeshKey(source);
                if (key == null)
                {
                    return source;
                }
                if (!meshes.TryGetValue(key, out Mesh derived))
                {
                    Mesh shaped = shapes.TryGetValue(key, out Mesh shape) ? shape : source;
                    derived = Wired(new[] { first }) ? AsteroidsArtBuilder.WireMesh(spec, shaped, key) : shaped;
                    meshes[key] = derived;
                }
                return derived;
            }
            Sprite SpriteOf(Sprite source)
            {
                string key = ThemeKeys.SpriteKey(source);
                Sprite themed = key != null ? AsteroidsArtBuilder.ThemeSprite(spec, key) : null;
                return themed != null ? themed : source;
            }
            // The hangar pictures, with the wire hulls in their neon paint.
            AsteroidsArtBuilder.RenderShipPreviews($"{spec.ArtFolder}/Previews", hull =>
            {
                Mesh mesh = AsteroidsArtBuilder.ShipMesh(hull.model);
                Material paint = AsteroidsArtBuilder.PackMaterial(hull.material);
                return (MeshOf(mesh, paint), Of(paint));
            });
            TMP_FontAsset body = AsteroidsArtBuilder.ThemeFont(spec, spec.BodyFont);
            TMP_FontAsset title = AsteroidsArtBuilder.ThemeFont(spec, spec.TitleFont);
            Sprite clear = AsteroidsArtBuilder.Interface("Clear");

            return AsteroidsAssets.SaveScriptable<AsteroidsTheme>(ThemePath(spec), theme =>
            {
                AsteroidsAssets.Set(theme, "displayName", p => p.stringValue = spec.DisplayName);
                AsteroidsAssets.Set(theme, "description", p => p.stringValue = spec.Description);
                AsteroidsAssets.SetObject(theme, "preview", AsteroidsAssets.Load<Sprite>($"{spec.ArtFolder}/Previews/Sparrow.png"));
                theme.ships = classic.ships.Select(look => new AsteroidsTheme.ShipLook
                {
                    hull = look.hull, mesh = MeshOf(look.mesh, look.material), material = Of(look.material), scale = look.scale, engineColor = spec.Neon(look.engineColor),
                    preview = AsteroidsAssets.Load<Sprite>($"{spec.ArtFolder}/Previews/{look.hull}.png")
                }).ToList();
                theme.asteroids = classic.asteroids.Select(look => new AsteroidsTheme.AsteroidLook
                {
                    kind = look.kind, meshes = look.meshes.Select(mesh => MeshOf(mesh, look.material)).ToArray(), material = Of(look.material), flash = spec.Neon(look.flash)
                }).ToList();
                theme.shots = classic.shots.Select(entry => new AsteroidsTheme.NamedMaterial { name = entry.name, material = Of(entry.material) }).ToList();
                theme.enemies = classic.enemies.Select(entry => new AsteroidsTheme.NamedMaterial { name = entry.name, material = Of(entry.material) }).ToList();
                AsteroidsTheme.EffectMaterials effects = classic.effects;
                theme.effects = new AsteroidsTheme.EffectMaterials
                {
                    particleAdd = Of(effects.particleAdd), particleSpark = Of(effects.particleSpark), particleFlare = Of(effects.particleFlare), particleShard = Of(effects.particleShard),
                    particleSmoke = Of(effects.particleSmoke), particleRing = Of(effects.particleRing), glow = Of(effects.glow), flare = Of(effects.flare), ring = Of(effects.ring),
                    dangerRing = Of(effects.dangerRing), muzzle = Of(effects.muzzle), engineFlame = Of(effects.engineFlame), bossFlame = Of(effects.bossFlame),
                    shipShield = Of(effects.shipShield), bossShield = Of(effects.bossShield), shipHalo = Of(effects.shipHalo)
                };
                theme.pickups = classic.pickups.Select(look => new AsteroidsTheme.PickupLook
                {
                    name = look.name, core = Of(look.core), icon = Of(look.icon), sprite = SpriteOf(look.sprite)
                }).ToList();
                theme.backdrop = new AsteroidsTheme.BackdropLook
                {
                    space = Of(classic.backdrop.space), atmosphere = Of(classic.backdrop.atmosphere), halo = Of(classic.backdrop.halo), rings = Of(classic.backdrop.rings),
                    motes = Of(classic.backdrop.motes)
                };
                theme.sectors = AsteroidsArtBuilder.SectorNames.Select(name => AsteroidsContentBuilder.ThemeSector(spec, name)).Where(sector => sector != null).ToList();
                theme.strikeThemes = AsteroidsArtBuilder.GeneratedGroundThemes.Where(world => world.Neon == spec)
                    .Select(world => AsteroidsAssets.Load<StrikeTheme>(world.AssetPath)).Where(world => world != null).ToList();
                AsteroidsTheme.InterfaceLook ui = classic.ui;
                theme.ui = new AsteroidsTheme.InterfaceLook
                {
                    panel = SpriteOf(ui.panel), button = SpriteOf(ui.button), frame = SpriteOf(ui.frame), hexagon = SpriteOf(ui.hexagon), hexagonFrame = SpriteOf(ui.hexagonFrame),
                    bar = SpriteOf(ui.bar), timerRing = SpriteOf(ui.timerRing), vignette = SpriteOf(ui.vignette), fade = SpriteOf(ui.fade), lane = SpriteOf(ui.lane), glow = SpriteOf(ui.glow),
                    scanlines = AsteroidsArtBuilder.ThemeSprite(spec, "Interface/Scanlines") ?? clear, crt = AsteroidsArtBuilder.ThemeSprite(spec, "Interface/Crt") ?? clear,
                    starFull = SpriteOf(ui.starFull), starEmpty = SpriteOf(ui.starEmpty), weapons = ui.weapons.Select(SpriteOf).ToArray()
                };
                theme.icons = classic.icons.Select(entry => new AsteroidsTheme.NamedSprite { name = entry.name, sprite = SpriteOf(entry.sprite) }).ToList();
                theme.strikeIcons = classic.strikeIcons.Select(entry => new AsteroidsTheme.NamedSprite { name = entry.name, sprite = SpriteOf(entry.sprite) }).ToList();
                theme.palette = NeonPalette(spec);
                theme.Fonts.body = body;
                theme.Fonts.bodyMaterial = AsteroidsArtBuilder.ThemeFontMaterial(spec, AsteroidsArtBuilder.TextRoleName.Hud);
                theme.Fonts.title = title;
                theme.Fonts.titleMaterial = AsteroidsArtBuilder.ThemeFontMaterial(spec, AsteroidsArtBuilder.TextRoleName.Title);
                FillMenu(theme.Menu, theme.palette, theme.ui.panel, theme.ui.button, clear);
                theme.Slots.materials.Clear();
                foreach (KeyValuePair<string, Material> entry in materials)
                {
                    ThemeSlots.Set(theme.Slots.materials, entry.Key, entry.Value);
                }
                theme.Slots.meshes.Clear();
                foreach (KeyValuePair<string, Mesh> entry in meshes)
                {
                    ThemeSlots.Set(theme.Slots.meshes, entry.Key, entry.Value);
                }
                theme.Slots.sprites.Clear();
                foreach ((string key, Sprite sprite) in catalog.Sprites)
                {
                    ThemeSlots.Set(theme.Slots.sprites, key, SpriteOf(sprite));
                }
                ThemeSlots.Set(theme.Slots.sprites, "Interface/Scanlines", theme.ui.scanlines);
                ThemeSlots.Set(theme.Slots.sprites, "Interface/Crt", theme.ui.crt);
                theme.Slots.fonts.Clear();
                ThemeSlots.Set(theme.Slots.fonts, "plain", body);
            });
        }


        /// <summary>The interface colours of a generated look: neon on near black.</summary>
        private static AsteroidsTheme.Palette NeonPalette(ThemeSpec spec)
        {
            Color Alpha(Color color, float alpha)
            {
                return new Color(color.r, color.g, color.b, alpha);
            }
            return new AsteroidsTheme.Palette
            {
                panel = Alpha(Color.Lerp(spec.Magenta, Color.white, 0.2f), 0.96f),
                soft = Color.Lerp(spec.Cyan, spec.White, 0.6f),
                dim = Color.Lerp(spec.Violet, spec.White, 0.45f),
                green = spec.Green,
                blue = Color.Lerp(spec.Violet, spec.Cyan, 0.35f),
                orange = spec.Orange,
                red = spec.Pink,
                gold = spec.Yellow,
                cyan = spec.Cyan,
                strikeAccent = spec.Orange,
                tabIdle = Alpha(Color.Lerp(spec.Violet, Color.white, 0.15f), 0.95f),
                inputField = Alpha(Color.Lerp(spec.Violet, Color.white, 0.3f), 1f),
                logoTop = spec.Cyan,
                logoBottom = spec.Magenta,
                settingsWindow = Alpha(spec.Cyan, 1f),
                settingsRows = Alpha(Color.Lerp(spec.Violet, Color.white, 0.25f), 0.95f)
            };
        }

        // ------------------------------------------------------------------ definition

        /// <summary>The game's definition starts with the first of <paramref name="themes"/> and lists them all.</summary>
        private static void UpdateDefinition(List<AsteroidsTheme> themes)
        {
            var definition = AsteroidsAssets.Load<GameDefinition>("Resources/Games/Asteroids.asset");
            if (definition == null || themes.Count == 0)
            {
                return;
            }
            AsteroidsAssets.SetObject(definition, "theme", themes[0]);
            AsteroidsAssets.SetObjects(definition, "themes", themes.Cast<Object>().ToArray());
            EditorUtility.SetDirty(definition);
            GameThemes.ForgetDefinitions();
        }
    }
}
