using System.Collections.Generic;
using Gamebox;
using Gamebox.Launcher;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.Asteroids.Tests
{
    /// <summary>
    /// The built themes: Classic and Arcade 80s exist and are complete, the definition starts with Classic and lists both,
    /// Classic refers to the art the scene was built with, Arcade 80s replaces every keyed material of Classic with art of
    /// its own, and selecting it changes what the views read (a hull's model, a mission's sector, a strike world).
    /// </summary>
    public class ThemeTest
    {
        private static string Root
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssembly(typeof(AsteroidsTheme).Assembly);
                return package != null ? package.assetPath : "Assets";
            }
        }

        private static AsteroidsTheme Theme(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AsteroidsTheme>($"{Root}/Config/Themes/Game/{name}.asset");
        }

        private static T Asset<T>(string relative) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{Root}/{relative}");
        }

        private static AsteroidsTheme Classic => Theme("Classic");

        private static AsteroidsTheme Arcade => Theme("Arcade80s");


        [SetUp]
        public void SetUp()
        {
            Assume.That(Classic, Is.Not.Null, "Build Everything has not built the themes yet.");
            Assume.That(Arcade, Is.Not.Null, "Build Everything has not built the Arcade 80s theme yet.");
            AsteroidsThemes.Forget();
        }


        [TearDown]
        public void TearDown()
        {
            GameThemes.Select(GameType.Asteroids, (GameTheme)null, false);
            AsteroidsThemes.Forget();
        }


        [Test]
        public void BothThemesValidateClean()
        {
            foreach (AsteroidsTheme theme in new[] { Classic, Arcade })
            {
                var problems = new List<string>();
                Assert.IsTrue(theme.Validate(problems), $"{theme.name}:\n{string.Join("\n", problems)}");
                Assert.AreEqual(GameType.Asteroids, theme.Game);
            }
            Assert.AreEqual("Classic", Classic.DisplayName);
            Assert.AreEqual("Arcade 80s", Arcade.DisplayName);
        }


        [Test]
        public void TheDefinitionStartsWithClassicAndListsBoth()
        {
            var definition = Asset<GameDefinition>("Resources/Games/Asteroids.asset");
            Assert.IsNotNull(definition);
            Assert.AreEqual(Classic, definition.Theme);
            CollectionAssert.AreEqual(new GameTheme[] { Classic, Arcade }, definition.Themes);
            Assert.AreEqual(Arcade, definition.FindTheme("Arcade 80s"));
            Assert.AreEqual(Arcade, definition.FindTheme("Arcade80s"));
        }


        [Test]
        public void ClassicRefersToTheArtTheSceneWasBuiltWith()
        {
            AsteroidsTheme classic = Classic;
            Assert.AreEqual(Asset<Material>("Art/StarSparrow/Materials/StarSparrow Blue.mat"), classic.Ship("Sparrow").material);
            Assert.AreEqual(Asset<PlayerSettings>("Config/PlayerSettings.asset").ModelMesh, classic.Ship("Sparrow").mesh);
            Assert.AreEqual(Asset<Material>("Art/Materials/Rock.mat"), classic.Asteroid(AsteroidKind.Rock).material);
            Assert.AreEqual(Asset<Material>("Art/Materials/Rock.mat"), classic.MaterialOf("Materials/Rock"));
            Assert.AreEqual(Asset<Mesh>("Art/Models/Saucer.asset"), classic.MeshOf("Models/Saucer"));
            Assert.AreEqual(Asset<Sprite>("Art/Interface/Panel.png"), classic.Interface.panel);
            Assert.AreEqual(Asset<Sprite>("Art/Icons/Star.png"), classic.Interface.starFull);
            Assert.AreEqual(Asset<Sprite>("Art/Icons/Laser.png"), classic.Weapon(WeaponType.Laser));
            SectorTheme kepler = Asset<SectorTheme>("Config/Themes/Kepler.asset");
            Assert.AreEqual(kepler, classic.Sector(kepler));
            StrikeTheme desert = Asset<StrikeTheme>("Config/Strike/Themes/Desert.asset");
            Assert.AreEqual(desert, classic.Strike(desert));
            Assert.AreEqual(Asset<Material>("Art/Materials/Space.mat"), classic.Backdrop.space);
            Assert.AreEqual(Asset<Material>("Art/Materials/FontHud.mat"), classic.Fonts.bodyMaterial);
        }


        [Test]
        public void ArcadeReplacesEveryKeyedMaterialAndTheHulls()
        {
            AsteroidsTheme classic = Classic;
            AsteroidsTheme arcade = Arcade;
            int replaced = 0;
            foreach (ThemeSlot<Material> slot in classic.Slots.materials)
            {
                Material theirs = arcade.MaterialOf(slot.key);
                Assert.IsNotNull(theirs, $"Arcade 80s has no material for {slot.key}.");
                if (theirs != slot.value)
                {
                    replaced++;
                    Assert.IsTrue(AssetDatabase.GetAssetPath(theirs).Contains("/Themes/Arcade80s/"), $"{slot.key}: {AssetDatabase.GetAssetPath(theirs)} is not the theme's own.");
                }
            }
            Assert.Greater(replaced, classic.Slots.materials.Count / 2, "Arcade 80s keeps most of the Classic materials.");
            foreach (AsteroidsTheme.ShipLook look in classic.Ships)
            {
                AsteroidsTheme.ShipLook theirs = arcade.Ship(look.hull);
                Assert.IsNotNull(theirs, $"Arcade 80s has no {look.hull}.");
                Assert.AreNotEqual(look.mesh, theirs.mesh, $"{look.hull} keeps the Classic model.");
                Assert.AreNotEqual(look.material, theirs.material, $"{look.hull} keeps the Classic paint.");
                Assert.AreNotEqual(look.preview, theirs.preview, $"{look.hull} keeps the Classic picture.");
                Assert.AreEqual("Portfolio/Asteroids/NeonSurface", theirs.material.shader.name);
                Assert.IsTrue(theirs.mesh.colors.Length > 0, $"{look.hull}'s wire model has no barycentric colours.");
            }
            foreach (AsteroidsTheme.AsteroidLook look in classic.Asteroids)
            {
                AsteroidsTheme.AsteroidLook theirs = arcade.Asteroid(look.kind);
                Assert.AreNotEqual(look.material, theirs.material, $"{look.kind} rocks keep the Classic material.");
                Assert.AreEqual(look.meshes.Length, theirs.meshes.Length);
                Assert.AreNotEqual(look.meshes[0], theirs.meshes[0], $"{look.kind} rocks keep the Classic shapes.");
            }
            Assert.AreNotEqual(classic.Interface.panel, arcade.Interface.panel);
            Assert.AreNotEqual(classic.Interface.starFull, arcade.Interface.starFull);
            Assert.AreNotEqual(classic.Fonts.MaterialOf(TextRole.Body), arcade.Fonts.MaterialOf(TextRole.Body));
            Assert.AreNotEqual(classic.Fonts.MaterialOf(TextRole.Title), arcade.Fonts.MaterialOf(TextRole.Title));
            Assert.AreEqual(1, arcade.StrikeThemes.Count);
            Assert.AreEqual(4, arcade.Sectors.Count);
            Assert.AreEqual("Portfolio/Asteroids/SynthwaveBackground", arcade.Backdrop.space.shader.name);
        }


        [Test]
        public void SelectingArcadeChangesWhatTheViewsRead()
        {
            AsteroidsTheme classic = Classic;
            AsteroidsTheme arcade = Arcade;
            SectorTheme kepler = Asset<SectorTheme>("Config/Themes/Kepler.asset");
            StrikeTheme desert = Asset<StrikeTheme>("Config/Strike/Themes/Desert.asset");
            Assert.AreEqual(classic, AsteroidsThemes.Active);
            Assert.AreEqual(kepler, AsteroidsThemes.Sector(kepler));
            Assert.AreEqual(desert, AsteroidsThemes.Strike(desert));

            GameThemes.Select(GameType.Asteroids, arcade, false);
            Assert.AreEqual(arcade, AsteroidsThemes.Active);
            Assert.AreEqual(arcade.Ship("Sparrow").mesh, AsteroidsThemes.Ship("Sparrow").mesh);
            Assert.AreNotEqual(classic.Ship("Sparrow").mesh, AsteroidsThemes.Ship("Sparrow").mesh);
            SectorTheme theirs = AsteroidsThemes.Sector(kepler);
            Assert.AreNotEqual(kepler, theirs);
            Assert.AreEqual("Kepler", theirs.name);
            StrikeTheme grid = AsteroidsThemes.Strike(desert);
            Assert.AreNotEqual(desert, grid);
            Assert.AreEqual("Grid", grid.name);
            foreach (TerrainKind kind in System.Enum.GetValues(typeof(TerrainKind)))
            {
                Assert.IsTrue(grid.Provides(kind), $"The Grid world has no {kind} tiles.");
            }
            Assert.AreEqual(arcade.Asteroid(AsteroidKind.Ice).meshes[0], AsteroidsThemes.Asteroid(AsteroidKind.Ice).meshes[0]);

            GameThemes.Select(GameType.Asteroids, (GameTheme)null, false);
            Assert.AreEqual(classic, AsteroidsThemes.Active);
            Assert.AreEqual(kepler, AsteroidsThemes.Sector(kepler));
        }
    }
}
