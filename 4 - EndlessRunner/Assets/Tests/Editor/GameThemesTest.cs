using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.EndlessRunner.Tests
{
    /// <summary>
    /// The two looks of the game: both theme assets are complete, the launcher entry starts with the classic one and
    /// lists both, the classic theme holds the art the scene was built with, the Night Shift brings pieces of the same
    /// names and sizes (so a race lays out the same track on either), and switching changes what the game resolves.
    /// </summary>
    public class GameThemesTest
    {
        private RunnerGameTheme classic;
        private RunnerGameTheme nightShift;

        [SetUp]
        public void SetUp()
        {
            List<RunnerGameTheme> themes = AssetDatabase.FindAssets("t:RunnerGameTheme")
                .Select(guid => AssetDatabase.LoadAssetAtPath<RunnerGameTheme>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(theme => theme != null)
                .ToList();
            classic = themes.FirstOrDefault(theme => theme.IsCalled("Classic"));
            nightShift = themes.FirstOrDefault(theme => theme.IsCalled("Night Shift"));
            Assert.IsNotNull(classic, "the classic theme asset is missing; build the worlds and levels");
            Assert.IsNotNull(nightShift, "the Night Shift theme asset is missing; build the worlds and levels");
        }

        [TearDown]
        public void TearDown()
        {
            GameThemes.Select(GameType.EndlessRunner, (GameTheme)null, false);
        }

        [Test]
        public void BothThemesAreComplete()
        {
            foreach (RunnerGameTheme theme in new[] { classic, nightShift })
            {
                var problems = new List<string>();
                Assert.IsTrue(theme.Validate(problems), $"{theme.DisplayName} leaves references empty:\n{string.Join("\n", problems)}");
                Assert.AreEqual(GameType.EndlessRunner, theme.Game);
                Assert.AreEqual(5, theme.Biomes.Count);
            }
        }

        [Test]
        public void TheDefinitionStartsWithTheClassicThemeAndListsBoth()
        {
            GameDefinition definition = GameCatalog.Find(GameType.EndlessRunner);
            Assert.IsNotNull(definition);
            Assert.AreSame(classic, definition.Theme);
            CollectionAssert.AreEquivalent(new GameTheme[] { classic, nightShift }, definition.Themes.ToArray());
            Assert.AreSame(classic, definition.FindTheme("Classic"));
            Assert.AreSame(nightShift, definition.FindTheme("Night Shift"));
        }

        [Test]
        public void TheClassicThemeHoldsTheArtOfTheScene()
        {
            Assert.AreEqual("Sunny Meadows", classic.Biomes[0].displayName);
            Assert.AreEqual("Lava Land", classic.Biomes[4].displayName);
            Assert.AreEqual("Star", classic.Sprites.star.name);
            Assert.AreEqual("Panel", classic.Sprites.panel.name);
            Assert.AreEqual("Coin", classic.Pieces.coin.name);
            Assert.AreEqual("Palette", classic.Materials.palette.name);
            Assert.AreEqual("Torso", classic.Runner.torso.name);
            Assert.AreEqual("FontTitle", classic.Fonts.titleMaterial.name);
            Assert.AreEqual(new Color(1f, 0.83f, 0.26f), classic.Colors.accent);
            var player = AssetDatabase.FindAssets("Player t:Prefab")
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(prefab => prefab != null && prefab.GetComponent<RunnerPlayer>() != null);
            Assert.IsNotNull(player, "the runner prefab is missing");
            var animator = player.GetComponentInChildren<RunnerAnimator>(true);
            Assert.AreSame(classic.Runner.torso, animator.spine.GetComponent<MeshFilter>().sharedMesh);
            Assert.IsNotNull(player.GetComponent<RunnerSkin>(), "the runner prefab has no skin to follow the theme");
        }

        [Test]
        public void TheNightShiftBringsItsOwnArt()
        {
            Assert.AreEqual("Downtown", nightShift.Biomes[0].displayName);
            Assert.AreNotSame(classic.Pieces, nightShift.Pieces);
            Assert.AreNotSame(classic.Pieces.coin, nightShift.Pieces.coin);
            Assert.AreNotSame(classic.Runner.torso, nightShift.Runner.torso);
            Assert.AreNotSame(classic.Sprites.panel, nightShift.Sprites.panel);
            Assert.AreNotSame(Font(classic), Font(nightShift), "the Night Shift has a font of its own");
            Assert.IsNotNull(Font(nightShift));
            Assert.AreNotSame(classic.Sky, nightShift.Sky);
            foreach (RunnerTheme biome in nightShift.Biomes)
            {
                CollectionAssert.DoesNotContain(classic.Biomes, biome);
                Assert.Greater(biome.scenery.Length, 0, $"{biome.displayName} has no scenery");
            }
        }

        [Test]
        public void ThePiecesOfBothThemesMatchInNameSizeAndKind()
        {
            // A race lays out the track from the catalog: the ids, positions and lengths must not depend on the theme.
            List<TrackPiece> a = Pieces(classic.Pieces);
            List<TrackPiece> b = Pieces(nightShift.Pieces);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].name, b[i].name);
                Assert.AreEqual(a[i].Length, b[i].Length, 0.0001f, a[i].name);
                Assert.AreEqual(a[i].GetType(), b[i].GetType(), a[i].name);
                if (a[i] is Obstacle obstacle)
                {
                    Assert.AreEqual(obstacle.Kind, ((Obstacle)b[i]).Kind, a[i].name);
                }
                if (a[i] is Coin coin)
                {
                    Assert.AreEqual(coin.Value, ((Coin)b[i]).Value, a[i].name);
                }
            }
        }

        private static List<TrackPiece> Pieces(PieceCatalog catalog)
        {
            var pieces = new List<TrackPiece>
            {
                catalog.coin, catalog.gem, catalog.magnet, catalog.shield, catalog.multiplier, catalog.superJump, catalog.jumpPad,
                catalog.hurdle, catalog.barrier, catalog.ramp, catalog.bridge, catalog.cart, catalog.finishLine
            };
            pieces.AddRange(catalog.blocks);
            pieces.AddRange(catalog.shortWagons);
            pieces.AddRange(catalog.longWagons);
            return pieces;
        }

        [Test]
        public void SelectingTheNightShiftResolvesTheWorldsOfTheCampaign()
        {
            RunnerTheme meadow = classic.Biomes[0];
            RunnerTheme snow = classic.Biomes[2];
            GameThemes.Select(GameType.EndlessRunner, classic, false);
            Assert.AreSame(meadow, RunnerGameTheme.Resolve(meadow));
            GameThemes.Select(GameType.EndlessRunner, nightShift, false);
            Assert.AreSame(nightShift, RunnerGameTheme.Active);
            Assert.AreSame(nightShift.Biomes[0], RunnerGameTheme.Resolve(meadow));
            Assert.AreSame(nightShift.Biomes[2], RunnerGameTheme.Resolve(snow));
            Assert.AreSame(nightShift.Biomes[1], nightShift.Biome(nightShift.Biomes[1]), "a world of the theme itself stays");
            var level = AssetDatabase.FindAssets("RunnerLevel1 t:RunnerLevel")
                .Select(guid => AssetDatabase.LoadAssetAtPath<RunnerLevel>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(found => found != null);
            Assert.IsNotNull(level);
            Assert.AreSame(nightShift.Biomes[0], level.Theme, "the level's world follows the theme");
            Assert.AreSame(nightShift.Biomes[0], level.ThemeAt(0f));
            GameThemes.Select(GameType.EndlessRunner, classic, false);
            Assert.AreSame(meadow, level.Theme);
        }

        [Test]
        public void SwitchingThemesChangesWhatTheLookupsReturn()
        {
            GameThemes.Select(GameType.EndlessRunner, classic, false);
            Assert.AreSame(classic.Sprites.coin, GameThemes.Current != null ? RunnerGameTheme.Active.SpriteOf("sprites.coin") : classic.SpriteOf("sprites.coin"));
            Assert.AreSame(classic.Materials.palette, classic.MaterialOf("materials.palette"));
            Assert.AreEqual(classic.Colors.accent, classic.ColorOf("colors.accent", Color.black));
            GameThemes.Select(GameType.EndlessRunner, nightShift, false);
            RunnerGameTheme active = RunnerGameTheme.Active;
            Assert.AreSame(nightShift, active);
            Assert.AreSame(nightShift.Sprites.coin, active.SpriteOf("sprites.coin"));
            Assert.AreSame(nightShift.Biomes[3].road, active.MaterialOf("biomes.3.road"));
            Assert.AreEqual(nightShift.Colors.accent, active.ColorOf("colors.accent", Color.black));
            Assert.AreSame(nightShift.Fonts.titleMaterial, active.MaterialOf("fonts.titleMaterial"));
        }

        /// <summary>The body font of a theme, as an object: the tests do not reference TextMesh Pro.</summary>
        private static object Font(RunnerGameTheme theme)
        {
            return typeof(ThemeFonts).GetField("body").GetValue(theme.Fonts);
        }

        [Test]
        public void TheRunnerAndTheRoadTakeTheLookOfTheTheme()
        {
            GameObject runner = Object.Instantiate(Prefab<RunnerPlayer>());
            GameObject tile = Object.Instantiate(Prefab<TerrainBehaviour>());
            try
            {
                var skin = runner.GetComponent<RunnerSkin>();
                var animator = runner.GetComponentInChildren<RunnerAnimator>(true);
                skin.Apply(nightShift);
                Assert.AreSame(nightShift.Runner.torso, animator.spine.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreSame(nightShift.Runner.shin, animator.shinLeft.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreSame(nightShift.Runner.material, animator.head.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.AreEqual(nightShift.Runner.hipsHeight, animator.hips.localPosition.y, 0.0001f);
                skin.Apply(classic);
                Assert.AreSame(classic.Runner.torso, animator.spine.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreEqual(classic.Runner.hipsHeight, animator.hips.localPosition.y, 0.0001f);

                var road = tile.GetComponent<TerrainBehaviour>();
                road.ApplyTheme(nightShift.Biomes[0]);
                Assert.AreSame(nightShift.Biomes[0].road, road.surface.sharedMaterials[0]);
                Assert.AreSame(nightShift.Biomes[0].ground, road.surface.sharedMaterials[3]);
                road.ApplyTheme(classic.Biomes[0]);
                Assert.AreSame(classic.Biomes[0].road, road.surface.sharedMaterials[0]);
            }
            finally
            {
                Object.DestroyImmediate(runner);
                Object.DestroyImmediate(tile);
            }
        }

        /// <summary>The prefab of the project that carries a <typeparamref name="T"/> at its root (the runner, the plain road tile).</summary>
        private static GameObject Prefab<T>() where T : Component
        {
            GameObject found = AssetDatabase.FindAssets("t:Prefab")
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(prefab => prefab != null && prefab.TryGetComponent(out T component)
                    && !(component is TerrainBehaviour terrain && terrain.HasGap));
            Assert.IsNotNull(found, $"no prefab with a {typeof(T).Name}");
            return found;
        }

        [Test]
        public void TheTrackIsLaidOutInTheWorldsOfItsLook()
        {
            var level = AssetDatabase.FindAssets("RunnerLevel1 t:RunnerLevel")
                .Select(guid => AssetDatabase.LoadAssetAtPath<RunnerLevel>(AssetDatabase.GUIDToAssetPath(guid)))
                .First(found => found != null);
            var tracks = new Tracks();
            try
            {
                foreach (RunnerGameTheme look in new[] { classic, nightShift })
                {
                    var builder = new LayoutBuilder(level, tracks.Settings(), look.Pieces, Tracks.Gravity, 7, look);
                    builder.GenerateUntil(120f);
                    RunnerTheme world = look.Biomes[0];
                    Assert.IsTrue(builder.Layout.Tiles.All(tile => tile.Theme == world), $"{look.DisplayName}: tiles of another world");
                    var scenery = new HashSet<TrackPiece>(world.scenery.Select(item => item.prefab)) { world.roadsideProp };
                    List<PiecePlacement> placed = builder.Layout.Pieces.Where(piece => LayoutBuilder.IsScenery(piece.Id)).ToList();
                    Assert.Greater(placed.Count, 0);
                    Assert.IsTrue(placed.All(piece => scenery.Contains(piece.Prefab)), $"{look.DisplayName}: scenery of another world");
                }
            }
            finally
            {
                tracks.Destroy();
            }
        }

        [Test]
        public void SceneryHasIdsOfItsOwnAndLeavesTheTrackAlone()
        {
            var tracks = new Tracks();
            RunnerTheme bare = ScriptableObject.CreateInstance<RunnerTheme>();
            try
            {
                RunnerLevel level = tracks.Level(600f);
                RunnerSettings settings = tracks.Settings();
                TrackLayout full = tracks.Layout(level, settings, 2024);
                level.theme = bare;
                TrackLayout empty = tracks.Layout(level, settings, 2024);
                Assert.Greater(full.Pieces.Count(piece => LayoutBuilder.IsScenery(piece.Id)), 0, "the theme with scenery placed none");
                Assert.AreEqual(0, empty.Pieces.Count(piece => LayoutBuilder.IsScenery(piece.Id)));
                List<PiecePlacement> a = full.Pieces.Where(piece => !LayoutBuilder.IsScenery(piece.Id)).ToList();
                List<PiecePlacement> b = empty.Pieces.Where(piece => !LayoutBuilder.IsScenery(piece.Id)).ToList();
                Assert.AreEqual(a.Count, b.Count);
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.AreEqual(a[i].Id, b[i].Id);
                    Assert.AreSame(a[i].Prefab, b[i].Prefab);
                    Assert.AreEqual(a[i].Position, b[i].Position);
                    Assert.Less(a[i].Id, LayoutBuilder.SceneryIdBase);
                }
                Assert.AreEqual(full.Coins, empty.Coins);
            }
            finally
            {
                Object.DestroyImmediate(bare);
                tracks.Destroy();
            }
        }
    }
}
