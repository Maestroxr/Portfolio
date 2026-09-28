using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Monopoly.Tests
{
    /// <summary>
    /// The themes of the game: both assets exist and are complete, the definition starts with the classic one and lists
    /// both, the classic one holds the art the scene was built from, and switching to Galactic Trade changes what the
    /// style and the themed parts show.
    /// </summary>
    public class MonopolyThemeTest
    {
        private static MonopolyTheme Find(string name)
        {
            return AssetDatabase.FindAssets("t:MonopolyTheme")
                .Select(guid => AssetDatabase.LoadAssetAtPath<MonopolyTheme>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(theme => theme != null && theme.IsCalled(name));
        }

        private static MonopolyTheme Classic => Find("Classic");
        private static MonopolyTheme Galactic => Find("Galactic Trade");

        [TearDown]
        public void BackToTheDefinition()
        {
            GameThemes.Select(GameType.Monopoly, (GameTheme)null, false);
        }

        [Test]
        public void BothThemesExistAndAreComplete()
        {
            foreach (string name in new[] { "Classic", "Galactic Trade" })
            {
                MonopolyTheme theme = Find(name);
                Assert.IsNotNull(theme, $"the theme {name} is missing; run Monopoly > Build Everything");
                var problems = new List<string>();
                Assert.IsTrue(theme.Validate(problems), $"{name} is incomplete:\n{string.Join("\n", problems)}");
                Assert.AreEqual(GameType.Monopoly, theme.Game);
            }
        }

        [Test]
        public void TheDefinitionStartsWithClassicAndListsBoth()
        {
            GameDefinition definition = GameCatalog.Find(GameType.Monopoly);
            Assert.IsNotNull(definition);
            Assert.AreEqual(Classic, definition.Theme);
            CollectionAssert.AreEqual(new GameTheme[] { Classic, Galactic }, definition.Themes.ToArray());
        }

        [Test]
        public void ClassicHoldsTheArtTheSceneWasBuiltFrom()
        {
            MonopolyTheme classic = Classic;
            Assert.AreEqual("Board", classic.boardMaterial.name);
            Assert.AreEqual("Table", classic.tableMaterial.name);
            Assert.AreEqual("Pewter", classic.tokenMaterial.name);
            Assert.AreEqual("Rounded", classic.ui.rounded.name);
            Assert.AreEqual("Race Car", classic.tokens[0].name);
            Assert.AreEqual("Token_0", classic.tokens[0].mesh.name);
            Assert.AreEqual("Badge_0", classic.tokens[0].badge.name);
            Assert.AreEqual("Poppins Black Title", classic.titleMaterial.name);
            Assert.AreEqual("Poppins Bold Shadow", classic.textShadowMaterial.name);
            Assert.AreEqual(MonopolyStyle.Defaults.Red, classic.palette.red);
            Assert.AreEqual(MonopolyStyle.Defaults.Ink, classic.palette.ink);
            Assert.AreEqual(MonopolyStyle.Defaults.GroupColor(ColorGroup.Brown), classic.groups[0].color);
            Assert.AreEqual(MonopolyStyle.Defaults.PlayerColors[1], classic.seats[1].color);
            Assert.AreEqual("WORLD TOUR EDITION", classic.editionLabel);
            Assert.AreEqual(MonopolyStyle.Groups.Length, classic.groups.Count);
            Assert.AreEqual(MonopolyStyle.Defaults.TokenNames.Length, classic.tokens.Count);
        }

        [Test]
        public void GalacticTradeIsAnotherLookAltogether()
        {
            MonopolyTheme classic = Classic;
            MonopolyTheme galactic = Galactic;
            Assert.AreNotEqual(classic.boardMaterial, galactic.boardMaterial);
            Assert.AreNotEqual(classic.boardTexture, galactic.boardTexture);
            Assert.AreNotEqual(classic.tableTexture, galactic.tableTexture);
            Assert.AreNotEqual(classic.houseMesh, galactic.houseMesh);
            Assert.AreNotEqual(classic.titleMaterial, galactic.titleMaterial);
            Assert.AreEqual("Orbitron Black Title", galactic.titleMaterial.name);
            Assert.AreNotEqual(classic.palette.paper, galactic.palette.paper);
            Assert.AreNotEqual(classic.editionLabel, galactic.editionLabel);
            for (int i = 0; i < classic.tokens.Count; i++)
            {
                Assert.AreNotEqual(classic.tokens[i].mesh, galactic.tokens[i].mesh, $"token {i}");
                Assert.AreNotEqual(classic.tokens[i].badge, galactic.tokens[i].badge, $"badge {i}");
                Assert.AreNotEqual(classic.tokens[i].name, galactic.tokens[i].name, $"name {i}");
            }
            for (int i = 0; i < classic.groups.Count; i++)
            {
                Assert.AreNotEqual(classic.groups[i].color, galactic.groups[i].color, $"group {i}");
            }
            Assert.IsTrue(galactic.boardMaterial.IsKeywordEnabled("_EMISSION"), "the station board glows");
        }

        [Test]
        public void SelectingGalacticTradeChangesTheStyle()
        {
            MonopolyTheme galactic = Galactic;
            GameThemes.Select(GameType.Monopoly, galactic, false);
            Assert.AreEqual(galactic, MonopolyStyle.Theme);
            Assert.AreEqual(galactic.palette.red, MonopolyStyle.Red);
            Assert.AreEqual(galactic.palette.paper, MonopolyStyle.Paper);
            Assert.AreEqual("Rocket", MonopolyStyle.TokenNames[0]);
            Assert.AreEqual(galactic.groups[0].color, MonopolyStyle.GroupColor(ColorGroup.Brown));
            Assert.AreEqual(galactic.groups[1].textColor, MonopolyStyle.GroupTextColor(ColorGroup.LightBlue));
            Assert.AreEqual(galactic.seats[2].color, MonopolyStyle.PlayerColor(2));
            Color washed = MonopolyStyle.Tint(galactic.palette.muted, 0.5f);
            Assert.AreEqual(Mathf.Lerp(galactic.palette.muted.r, galactic.palette.paper.r, 0.5f), washed.r, 0.0001f);
            Assert.AreEqual(galactic.palette.muted.a, washed.a, 0.0001f);

            GameThemes.Select(GameType.Monopoly, Classic, false);
            Assert.AreEqual(MonopolyStyle.Defaults.Red, MonopolyStyle.Red);
            Assert.AreEqual("Race Car", MonopolyStyle.TokenNames[0]);
        }

        [Test]
        public void ColorExpressionsResolveAgainstTheTheme()
        {
            MonopolyTheme classic = Classic;
            Assert.IsTrue(classic.TryResolveColor("palette.ink", out Color ink));
            Assert.AreEqual(classic.palette.ink, ink);
            Assert.IsTrue(classic.TryResolveColor("palette.red*0.72", out Color shade));
            Assert.AreEqual(MonopolyStyle.Shade(classic.palette.red, 0.72f), shade);
            Assert.IsTrue(classic.TryResolveColor("palette.muted~0.5@0.8", out Color tint));
            Assert.AreEqual(0.8f, tint.a, 0.0001f);
            Assert.AreEqual(Mathf.Lerp(classic.palette.muted.r, classic.palette.paper.r, 0.5f), tint.r, 0.0001f);
            Assert.IsTrue(classic.TryResolveColor("seats.1.color", out Color seat));
            Assert.AreEqual(classic.seats[1].color, seat);
            Assert.IsTrue(classic.TryResolveColor("groups[2].color", out Color group));
            Assert.AreEqual(classic.groups[2].color, group);
            Assert.IsFalse(classic.TryResolveColor("palette.nothing", out _));
            Assert.IsFalse(classic.TryResolveColor("", out _));
            Assert.AreEqual(classic.ui.rounded, classic.SpriteOf("ui.rounded"));
            Assert.AreEqual(classic.tokens[3].badge, classic.SpriteOf("tokens.3.badge"));
            Assert.AreEqual(classic.boardMaterial, classic.MaterialOf("boardMaterial"));
            Assert.AreEqual(classic.seats[2].baseMaterial, classic.MaterialOf("seats.2.baseMaterial"));
            Assert.AreEqual("WORLD TOUR EDITION", classic.WordOf("edition"));
        }

        [Test]
        public void WordsOnFacesStayReadableInBothLooks()
        {
            MonopolyTheme classic = Classic;
            MonopolyTheme galactic = Galactic;
            // The classic look keeps its white words on coloured buttons and its ink on the paper.
            Assert.AreEqual(Color.white, classic.ResolveColor("on:palette.green", Color.magenta));
            Assert.AreEqual(Color.white, classic.ResolveColor("on:palette.red", Color.magenta));
            Assert.AreEqual(classic.palette.ink, classic.ResolveColor("on:palette.paper", Color.magenta));
            Assert.AreEqual(Color.white, classic.ResolveColor("on:palette.plate", Color.magenta));
            Assert.AreEqual(classic.palette.ink, classic.palette.plate, "the classic plate is the classic ink");
            // The dark look writes dark words on its bright neon faces and light words on its paper and plates.
            Color onGreen = galactic.ResolveColor("on:palette.green", Color.magenta);
            Assert.Less(onGreen.grayscale, 0.2f, "dark words on the bright green");
            Assert.AreEqual(1f, onGreen.a);
            Assert.AreEqual(galactic.palette.ink, galactic.ResolveColor("on:palette.paper", Color.magenta));
            Assert.AreEqual(Color.white, galactic.ResolveColor("on:palette.plate", Color.magenta));
            Assert.Less(galactic.palette.plate.grayscale, 0.3f, "the plate behind white words is dark");
            Assert.IsFalse(galactic.TryResolveColor("on:palette.nothing", out _));
        }

        [Test]
        public void ThemedPartsFollowTheSelectedTheme()
        {
            var go = new GameObject("Themed piece", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                var renderer = go.GetComponent<MeshRenderer>();
                var filter = go.GetComponent<MeshFilter>();
                renderer.sharedMaterial = Classic.boardMaterial;
                filter.sharedMesh = Classic.houseMesh;
                var themedMaterial = go.AddComponent<ThemedRenderer>();
                themedMaterial.ThemedGame = GameType.Monopoly;
                themedMaterial.MaterialKey = "boardMaterial";
                themedMaterial.Watch();
                var themedMesh = go.AddComponent<ThemedMesh>();
                themedMesh.ThemedGame = GameType.Monopoly;
                themedMesh.MeshKey = "houseMesh";
                themedMesh.Watch();
                GameThemes.Select(GameType.Monopoly, Galactic, false);
                Assert.AreEqual(Galactic.boardMaterial, renderer.sharedMaterial);
                Assert.AreEqual(Galactic.houseMesh, filter.sharedMesh);
                GameThemes.Select(GameType.Monopoly, Classic, false);
                Assert.AreEqual(Classic.boardMaterial, renderer.sharedMaterial);
                Assert.AreEqual(Classic.houseMesh, filter.sharedMesh);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TheSceneIsTaggedForTheThemes()
        {
            string path = AssetDatabase.FindAssets("t:Scene Monopoly").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => p.EndsWith("/Monopoly.unity"));
            Assert.IsNotNull(path, "the Monopoly scene is missing");
            string text = System.IO.File.ReadAllText(path);
            Assert.IsTrue(text.Contains("print.logoWord"), "the logo word of the board is painted from the theme");
            Assert.IsTrue(text.Contains("boardMaterial"), "the board surface takes its material from the theme");
            Assert.IsTrue(text.Contains("palette.ink"), "the interface inks are keyed to the palette");
            Assert.IsTrue(text.Contains("ui.rounded"), "the interface shapes are keyed to the kit");
        }
    }
}
