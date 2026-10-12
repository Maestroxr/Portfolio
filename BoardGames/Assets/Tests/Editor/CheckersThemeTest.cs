using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Checkers.Tests
{
    /// <summary>
    /// The looks of the game: both theme assets exist and are complete, the definition starts with Classic and lists
    /// both, the two sides of each look are told apart, the keys the themed parts read resolve, and Midnight is another
    /// look altogether.
    /// </summary>
    public class CheckersThemeTest
    {
        private static CheckersTheme Find(string name)
        {
            return AssetDatabase.FindAssets("t:CheckersTheme")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CheckersTheme>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(theme => theme != null && theme.IsCalled(name));
        }

        private static CheckersTheme Classic => Find("Classic");
        private static CheckersTheme Midnight => Find("Midnight");

        [TearDown]
        public void BackToTheDefinition()
        {
            GameThemes.Select(GameType.Checkers, (GameTheme)null, false);
        }

        [Test]
        public void BothThemesExistAndAreComplete()
        {
            foreach (string name in new[] { "Classic", "Midnight" })
            {
                CheckersTheme theme = Find(name);
                Assert.IsNotNull(theme, $"the theme {name} is missing; run Checkers > Build Everything");
                var problems = new List<string>();
                Assert.IsTrue(theme.Validate(problems), $"{name} is incomplete:\n{string.Join("\n", problems)}");
                Assert.AreEqual(GameType.Checkers, theme.Game);
                Assert.IsFalse(string.IsNullOrEmpty(theme.Description), $"{name} has no description");
            }
        }

        [Test]
        public void TheDefinitionStartsWithClassicAndListsBoth()
        {
            GameDefinition definition = GameCatalog.Find(GameType.Checkers);
            Assert.IsNotNull(definition, "the Checkers definition is missing; run Checkers > Build Everything");
            Assert.AreEqual(Classic, definition.Theme);
            CollectionAssert.AreEqual(new GameTheme[] { Classic, Midnight }, definition.Themes.ToArray());
            Assert.AreEqual(Classic, definition.FindTheme("Classic"));
            Assert.AreEqual(Midnight, definition.FindTheme("Midnight"));
            Assert.AreEqual("Checkers", definition.DisplayName);
            Assert.IsFalse(string.IsNullOrEmpty(definition.Description));
        }

        [Test]
        public void EachLookTellsItsTwoSidesApart()
        {
            foreach (CheckersTheme theme in new[] { Classic, Midnight })
            {
                Assert.AreEqual(2, theme.sides.Length, theme.name);
                Assert.AreEqual("Light", theme.sides[0].nameKey, theme.name);
                Assert.AreEqual("Dark", theme.sides[1].nameKey, theme.name);
                Assert.AreNotEqual(theme.sides[0].piece, theme.sides[1].piece, $"{theme.name}: the sides share a piece material");
                Assert.AreNotEqual(theme.sides[0].color, theme.sides[1].color, $"{theme.name}: the sides share a colour");
                Assert.AreEqual(theme.sides[0], theme.SideOf(Side.Light), theme.name);
                Assert.AreEqual(theme.sides[1], theme.SideOf(Side.Dark), theme.name);
                Assert.AreEqual(theme.sides[1].color, theme.ColorOf(Side.Dark), theme.name);
                Assert.AreNotEqual(theme.lightSquare, theme.darkSquare, $"{theme.name}: the squares share a material");
            }
        }

        [Test]
        public void TheKeysTheThemedPartsReadResolve()
        {
            CheckersTheme classic = Classic;
            Assert.AreEqual(classic.lightSquare, classic.MaterialOf("lightSquare"));
            Assert.AreEqual(classic.darkSquare, classic.MaterialOf("darkSquare"));
            Assert.AreEqual(classic.slab, classic.MaterialOf("slab"));
            Assert.AreEqual(classic.table, classic.MaterialOf("table"));
            Assert.AreEqual(classic.sides[1].piece, classic.MaterialOf("sides.1.piece"));
            Assert.IsTrue(classic.TryColor("selectedColor", out Color selected));
            Assert.AreEqual(classic.selectedColor, selected);
            Assert.IsTrue(classic.TryColor("sides.0.color", out Color light));
            Assert.AreEqual(classic.sides[0].color, light);
            Assert.IsFalse(classic.TryColor("nothing", out _));
        }

        [Test]
        public void MidnightIsAnotherLookAltogether()
        {
            CheckersTheme classic = Classic;
            CheckersTheme midnight = Midnight;
            Assert.AreNotEqual(classic.lightSquare, midnight.lightSquare);
            Assert.AreNotEqual(classic.darkSquare, midnight.darkSquare);
            Assert.AreNotEqual(classic.slab, midnight.slab);
            Assert.AreNotEqual(classic.table, midnight.table);
            Assert.AreNotEqual(classic.background, midnight.background);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreNotEqual(classic.sides[i].piece, midnight.sides[i].piece, $"side {i}");
                Assert.AreNotEqual(classic.sides[i].color, midnight.sides[i].color, $"side {i}");
            }
            Assert.IsTrue(midnight.sides[0].piece.IsKeywordEnabled("_EMISSION") || midnight.sides[1].piece.IsKeywordEnabled("_EMISSION"),
                "the neon pieces of Midnight glow");
        }

        [Test]
        public void SelectingMidnightChangesTheActiveLook()
        {
            CheckersTheme midnight = Midnight;
            GameThemes.Select(GameType.Checkers, midnight, false);
            Assert.AreEqual(midnight, GameThemes.Active(GameType.Checkers));
            Assert.AreEqual(midnight, GameThemes.Active<CheckersTheme>(GameType.Checkers));
            GameThemes.Select(GameType.Checkers, Classic, false);
            Assert.AreEqual(Classic, GameThemes.Active(GameType.Checkers));
        }

        [Test]
        public void ThemedPartsFollowTheSelectedTheme()
        {
            var go = new GameObject("Themed square", typeof(MeshFilter), typeof(MeshRenderer));
            try
            {
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = Classic.darkSquare;
                var themed = go.AddComponent<ThemedRenderer>();
                themed.ThemedGame = GameType.Checkers;
                themed.MaterialKey = "darkSquare";
                themed.Watch();
                GameThemes.Select(GameType.Checkers, Midnight, false);
                Assert.AreEqual(Midnight.darkSquare, renderer.sharedMaterial);
                GameThemes.Select(GameType.Checkers, Classic, false);
                Assert.AreEqual(Classic.darkSquare, renderer.sharedMaterial);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
