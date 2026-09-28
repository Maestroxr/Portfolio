using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Portfolio.MemoryCards.Tests
{
    /// <summary>
    /// The two themes of the game as the content builder leaves them: both exist and validate, the launcher entry lists
    /// them and starts with Classic, Classic points at the original art, After Dark brings worlds and a deck of its own,
    /// and selecting a theme changes what the campaign deals and shows.
    /// </summary>
    public class GameThemesTest
    {
        private const string ClassicPath = "Settings/Themes/Classic.asset";
        private const string AfterDarkPath = "Settings/Themes/AfterDark.asset";
        private const string CampaignPath = "Settings/Campaign/MemoryCardsCampaign.asset";

        private MemoryCardsTheme classic;
        private MemoryCardsTheme afterDark;

        /// <summary>The game's files are mounted at Assets/ in its own project and inside a package in BaseGame.</summary>
        private static string Root
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssembly(typeof(MemoryCardsTheme).Assembly);
                return package != null ? package.assetPath : "Assets";
            }
        }

        private static T Load<T>(string relative) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{Root}/{relative}");
        }

        [SetUp]
        public void SetUp()
        {
            classic = Load<MemoryCardsTheme>(ClassicPath);
            afterDark = Load<MemoryCardsTheme>(AfterDarkPath);
            Assert.IsNotNull(classic, $"{ClassicPath} is missing; run Build Everything.");
            Assert.IsNotNull(afterDark, $"{AfterDarkPath} is missing; run Build Everything.");
        }

        [TearDown]
        public void TearDown()
        {
            GameThemes.Select(GameType.MemoryCards, (GameTheme)null, false);
        }

        [Test]
        public void BothThemesValidateClean()
        {
            foreach (MemoryCardsTheme theme in new[] { classic, afterDark })
            {
                var problems = new List<string>();
                bool ok = theme.Validate(problems);
                Assert.IsTrue(ok, $"{theme.name}:\n{string.Join("\n", problems)}");
                Assert.AreEqual(GameType.MemoryCards, theme.Game);
            }
            Assert.AreEqual("Classic", classic.DisplayName);
            Assert.AreEqual("After Dark", afterDark.DisplayName);
        }

        [Test]
        public void DefinitionListsBothThemesAndStartsWithClassic()
        {
            GameDefinition definition = GameCatalog.Find(GameType.MemoryCards);
            Assert.IsNotNull(definition);
            Assert.AreSame(classic, definition.Theme);
            Assert.AreEqual(2, definition.Themes.Count);
            Assert.AreSame(classic, definition.Themes[0]);
            Assert.AreSame(afterDark, definition.Themes[1]);
            Assert.AreSame(afterDark, definition.FindTheme("After Dark"));
            Assert.AreSame(afterDark, definition.FindTheme("AfterDark"));
        }

        [Test]
        public void ClassicReferencesTheOriginalArt()
        {
            Assert.AreEqual("CardFront", classic.Card.front.name);
            Assert.AreEqual("Wild", classic.Special.wild.name);
            Assert.AreEqual("ButtonGreen", classic.Kit.primary.name);
            Assert.AreEqual("StarFull", classic.Hud.starFull.name);
            Assert.AreEqual("KenneyFuture Title", classic.Text.title.name);
            Assert.AreEqual("KenneyFuture Outline", classic.Text.outline.name);
            Assert.AreEqual(30, classic.Faces.Count);
            Assert.AreEqual("bear", classic.Faces[0].name);
            Assert.AreSame(Load<CardWorld>("Settings/Worlds/Farm.asset"), classic.World(0));
            Assert.AreEqual("Sunny Farm", classic.World(0).displayName);
            Assert.AreEqual("Pattern", classic.Backdrop.pattern.name);
            // The parts the game tints are the white panels the scene always used.
            Assert.AreSame(classic.Kit.panelDepth, classic.Kit.tintPanel);
            Assert.AreSame(classic.Kit.pill, classic.Kit.tintPill);
            Assert.AreSame(classic.Kit.disc, classic.Kit.tintDisc);
            Assert.AreEqual(0, classic.Say.levels.Count, "Classic keeps the words of the levels.");
        }

        [Test]
        public void AfterDarkBringsItsOwnWorldsAndDeck()
        {
            Assert.AreEqual(classic.Worlds.Count, afterDark.Worlds.Count);
            for (int i = 0; i < classic.Worlds.Count; i++)
            {
                Assert.AreNotSame(classic.World(i), afterDark.World(i));
                // Online boards are dealt with the campaign's pool counts, so every world deals as many faces as the original.
                Assert.AreEqual(classic.World(i).animals.Count, afterDark.World(i).animals.Count, $"world {i}");
            }
            Assert.AreEqual("Velvet Lounge", afterDark.World(0).displayName);
            Assert.AreEqual(classic.Faces.Count, afterDark.Faces.Count);
            Assert.IsFalse(afterDark.Faces.Intersect(classic.Faces).Any(), "After Dark shows a face of the Classic deck.");
            Assert.AreEqual("Cinzel Title", afterDark.Text.title.name);
            Assert.AreEqual("PlayfairDisplay Outline", afterDark.Text.outline.name);
            Assert.AreNotSame(classic.Kit.primary, afterDark.Kit.primary);
            Assert.AreNotSame(classic.Special.wild, afterDark.Special.wild);
            Assert.AreNotSame(classic.Backdrop.pattern, afterDark.Backdrop.pattern);
        }

        [Test]
        public void ThemeKeysReachTheTypedFields()
        {
            Assert.AreSame(classic.Kit.primary, classic.SpriteOf("kit.primary"));
            Assert.AreSame(classic.Icons.play, classic.SpriteOf("icons.play"));
            Assert.AreSame(classic.Text.title, classic.MaterialOf("text.title"));
            Assert.AreSame(classic.Backdrop.pattern, classic.TextureOf("backdrop.pattern"));
            Assert.IsTrue(classic.TryColor("palette.ink", out Color ink));
            Assert.AreEqual(classic.Colors.ink, ink);
            Assert.AreSame(classic.World(1).cardBack, classic.SpriteOf("worlds.1.cardBack"));
            Assert.IsNull(classic.SpriteOf("kit.nothing"));
        }

        [Test]
        public void SelectingAfterDarkChangesWhatTheCampaignDealsAndShows()
        {
            MemoryCardsCampaign campaign = Load<MemoryCardsCampaign>(CampaignPath);
            Assert.IsNotNull(campaign);
            Assert.AreSame(classic, GameThemes.Active<MemoryCardsTheme>(GameType.MemoryCards));

            GameThemes.Select(GameType.MemoryCards, afterDark, false);
            MemoryCardsTheme look = GameThemes.Active<MemoryCardsTheme>(GameType.MemoryCards);
            Assert.AreSame(afterDark, look);
            Assert.AreEqual("Velvet Lounge", campaign.WorldOf(0, look).displayName);
            Assert.AreSame(afterDark.Faces[0], campaign.Deck(look)[0]);
            IReadOnlyList<Sprite> pool = campaign.AnimalPool(campaign.Level(0), null, look);
            CollectionAssert.AreEqual(afterDark.World(0).animals, pool);
            Assert.AreEqual(0, campaign.Validate(look).Count, "every level can be dealt from its After Dark world");
            Assert.AreEqual(campaign.Count, afterDark.Say.levels.Count, "After Dark tells every level in its own words");
            Assert.AreEqual("Doors Open", look.Say.Title(0, campaign.Level(0).Title));
            Assert.AreEqual(campaign.Level(1).Description, look.Say.Description(1, campaign.Level(1).Description));

            GameThemes.Select(GameType.MemoryCards, (GameTheme)null, false);
            look = GameThemes.Active<MemoryCardsTheme>(GameType.MemoryCards);
            Assert.AreSame(classic, look);
            Assert.AreEqual("Sunny Farm", campaign.WorldOf(0, look).displayName);
            Assert.AreSame(campaign.Theme(0), campaign.WorldOf(0, null));
            Assert.AreSame(campaign.Animals[0], campaign.Deck(null)[0]);
        }
    }
}
