using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gamebox;
using Gamebox.Launcher;
using NUnit.Framework;
using Portfolio.Heroes.UI;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The themes of the game as the builders left them: both assets exist and are complete, the definition starts with
    /// the classic one and lists both, the classic theme carries the art the scene was built with, the Grim Realm has
    /// art of its own with nothing of the classic one in it, and picking a theme changes what the game reads. These run
    /// in the editor only (they read the assets); dotnet test leaves this folder out.
    /// </summary>
    public class HeroesThemeTests
    {
        private static readonly Color ClassicInk = new Color(0.94f, 0.9f, 0.8f);

        [TearDown]
        public void TearDown()
        {
            GameThemes.Select(GameType.Heroes, (GameTheme)null, false);
            HeroesTheme.Activate(null);
        }

        private static HeroesTheme Theme(string name)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:HeroesTheme"))
            {
                var theme = AssetDatabase.LoadAssetAtPath<HeroesTheme>(AssetDatabase.GUIDToAssetPath(guid));
                if (theme != null && theme.IsCalled(name))
                {
                    return theme;
                }
            }
            Assert.Fail($"there is no {name} theme asset");
            return null;
        }

        private static GameDefinition Definition()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:GameDefinition"))
            {
                var definition = AssetDatabase.LoadAssetAtPath<GameDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null && definition.Type == GameType.Heroes)
                {
                    return definition;
                }
            }
            Assert.Fail("there is no definition of the Heroes game");
            return null;
        }

        /// <summary>The folder of a theme's art asset, where everything made for the theme lives.</summary>
        private static string FolderOf(HeroesArt art)
        {
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(art)).Replace('\\', '/') + "/";
        }

        [Test]
        public void BothThemesExistAndAreComplete()
        {
            foreach (string name in new[] { "Classic", "Grim Realm" })
            {
                HeroesTheme theme = Theme(name);
                Assert.AreEqual(GameType.Heroes, theme.Game);
                var problems = new List<string>();
                Assert.IsTrue(theme.Validate(problems), $"{name}:\n" + string.Join("\n", problems));
            }
        }

        [Test]
        public void TheDefinitionStartsWithClassicAndListsBoth()
        {
            GameDefinition definition = Definition();
            HeroesTheme classic = Theme("Classic");
            HeroesTheme grim = Theme("Grim Realm");
            Assert.AreSame(classic, definition.Theme);
            CollectionAssert.AreEquivalent(new GameTheme[] { classic, grim }, definition.Themes.ToArray());
            Assert.AreSame(classic, definition.Themes[0], "the starting theme is listed first");
            Assert.AreSame(grim, definition.FindTheme("grim realm"));
        }

        [Test]
        public void TheClassicThemeCarriesTheArtTheSceneWasBuiltWith()
        {
            HeroesTheme classic = Theme("Classic");
            Assert.IsNotNull(classic.art);
            StringAssert.EndsWith("/Art/HeroesArt.asset", AssetDatabase.GetAssetPath(classic.art));
            Assert.AreSame(classic.art.frame, classic.Menu.window, "the pause menu's window is the kit's frame");
            Assert.AreSame(classic.art.ribbon, classic.Menu.headerBanner);
            Assert.AreSame(classic.art.button, classic.Menu.button);
            Assert.AreSame(classic.art.bodyFont, classic.Fonts.body);
            Assert.AreSame(classic.art.titleFont, classic.Fonts.title);
            Assert.AreEqual(ClassicInk, classic.palette.ink, "the classic colours are the ones the interface had");
            Assert.AreEqual(new Color(0.86f, 0.16f, 0.14f), classic.palette.Player(0));
            Assert.AreEqual(new Color(0.7f, 0.7f, 0.7f), classic.palette.Player(7), "grey for nobody");
            Assert.AreSame(classic.art.Sky(TerrainType.Grass).material, classic.atmosphere.sky);
            Assert.AreEqual(0f, classic.glows.mix, "the classic effects keep their own colours");
            CollectionAssert.AreEqual(classic.art.cursors, classic.pointers.cursors);
        }

        [Test]
        public void TheGrimRealmHasArtOfItsOwnWithNothingOfTheClassicInIt()
        {
            HeroesTheme classic = Theme("Classic");
            HeroesTheme grim = Theme("Grim Realm");
            Assert.AreNotSame(classic.art, grim.art);
            Assert.AreEqual(classic.art.units.Count, grim.art.units.Count);
            Assert.AreEqual(classic.art.heroes.Count, grim.art.heroes.Count);
            Assert.AreEqual(classic.art.objects.Count, grim.art.objects.Count);
            Assert.AreNotSame(classic.art.frame, grim.art.frame);
            Assert.AreNotSame(classic.art.bodyFont, grim.art.bodyFont);
            Assert.AreNotSame(classic.art.titleFont, grim.art.titleFont);
            Assert.AreNotSame(classic.art.logoFont, grim.art.logoFont);
            Assert.AreNotEqual(classic.palette.ink, grim.palette.ink);
            Assert.Greater(grim.glows.mix, 0f, "the Grim Realm's magic leans toward its own colours");
            Assert.AreSame(grim.art.Sky(TerrainType.Grass).material, grim.atmosphere.sky);

            string folder = FolderOf(grim.art);
            var strays = new List<string>();
            void Own(Object asset, string what)
            {
                if (asset == null)
                {
                    return;
                }
                string path = AssetDatabase.GetAssetPath(asset);
                if (!path.StartsWith(folder, System.StringComparison.Ordinal))
                {
                    strays.Add($"{what}: {path}");
                }
            }
            void OwnPrefab(GameObject prefab, string what)
            {
                if (prefab == null)
                {
                    return;
                }
                Own(prefab, what);
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        Own(material, $"{what} > {renderer.name}");
                    }
                }
            }
            HeroesArt art = grim.art;
            foreach (HeroesArt.UnitArt unit in art.units)
            {
                OwnPrefab(unit.prefab, unit.creature.ToString());
                Own(unit.portrait, $"{unit.creature} portrait");
            }
            foreach (HeroesArt.HeroArt hero in art.heroes)
            {
                OwnPrefab(hero.rider, $"{hero.heroClass} rider");
                OwnPrefab(hero.mount, $"{hero.heroClass} mount");
                Own(hero.portrait, $"{hero.heroClass} portrait");
                Own(hero.mounted, $"{hero.heroClass} mounted");
            }
            foreach (HeroesArt.TownArt town in art.towns)
            {
                foreach (GameObject prefab in town.byColor)
                {
                    OwnPrefab(prefab, $"{town.faction} town");
                }
                foreach (Sprite portrait in town.portraits)
                {
                    Own(portrait, $"{town.faction} town portrait");
                }
            }
            foreach (HeroesArt.ObjectArt item in art.objects)
            {
                OwnPrefab(item.prefab, $"{item.kind} {item.subtype}");
            }
            foreach (HeroesArt.ObstacleArt obstacle in art.obstacles)
            {
                foreach (GameObject variant in obstacle.variants)
                {
                    OwnPrefab(variant, $"{obstacle.kind} on {obstacle.terrain}");
                }
            }
            foreach (HeroesArt.BackdropArt backdrop in art.backdrops)
            {
                foreach (GameObject prefab in backdrop.prefabs)
                {
                    OwnPrefab(prefab, $"backdrop of {backdrop.terrain}");
                }
            }
            OwnPrefab(art.siegeGate, "siege gate");
            OwnPrefab(art.siegeRuin, "siege ruin");
            OwnPrefab(art.siegeStones, "siege stones");
            foreach (GameObject[] group in new[] { art.flags, art.towers, art.siegeBanners, art.forestTrees, art.pineTrees, art.deadTrees, art.rocks, art.waterPlants })
            {
                foreach (GameObject prefab in group)
                {
                    OwnPrefab(prefab, "scenery");
                }
            }
            foreach (TerrainLayer layer in art.layers)
            {
                Own(layer, "terrain layer");
            }
            foreach (HeroesArt.SkyArt sky in art.skies)
            {
                Own(sky.material, $"sky {sky.name}");
            }
            foreach (Texture2D cursor in art.cursors)
            {
                Own(cursor, "pointer");
            }
            foreach (Sprite sprite in new[]
            {
                art.frame, art.panel, art.parchment, art.button, art.buttonHover, art.buttonPressed, art.buttonDisabled, art.slot, art.card,
                art.cardSelected, art.ribbon, art.strip, art.barFrame, art.knob, art.checkBox, art.portraitFrame, art.tooltip, art.divider
            })
            {
                Own(sprite, "kit");
            }
            Own(art.ground, "ground");
            Own(art.water, "water");
            Own(art.fog, "fog of war");
            Own(art.titleFont, "title font");
            Own(art.bodyFont, "body font");
            Own(art.logoFont, "logo font");
            Own(art.iconSprites, "icon sprites");
            Assert.IsEmpty(strays, "everything the Grim Realm shows is its own:\n" + string.Join("\n", strays.Take(40)));
        }

        [Test]
        public void PickingAThemeChangesWhatTheGameReads()
        {
            HeroesTheme classic = Theme("Classic");
            HeroesTheme grim = Theme("Grim Realm");
            GameThemes.Select(GameType.Heroes, grim, false);
            Assert.AreSame(grim, HeroesTheme.Current);
            Assert.AreSame(grim.art, GameThemes.Active<HeroesTheme>(GameType.Heroes).art);
            Assert.AreNotSame(classic.art.frame, HeroesTheme.Current.art.frame);

            HeroesTheme.Activate(grim);
            Assert.AreEqual(grim.palette.ink, UIKit.Ink);
            Assert.AreEqual(grim.palette.gold, UIKit.Gold);
            Assert.AreEqual(grim.palette.inkOnParchment, UIKit.InkOnParchment);
            Assert.AreEqual(grim.palette.Player(0), HeroesArt.PlayerColor(0));
            Assert.AreSame(grim.glows, HeroesTheme.Glows.Active);
            Assert.AreSame(grim.pointers.cursors[(int)CursorKind.Default], grim.Cursor(CursorKind.Default));

            HeroesTheme.Activate(null);
            Assert.AreEqual(ClassicInk, UIKit.Ink, "without a theme the classic colours are back");
            GameThemes.Select(GameType.Heroes, (GameTheme)null, false);
            Assert.AreSame(classic, HeroesTheme.Current, "the definition's theme is the one without a choice");
        }

        [Test]
        public void GlowsLeanTowardTheThemesColoursAndLeaveGreyAlone()
        {
            var glows = new HeroesTheme.Glows { warm = new Color(0.85f, 0.1f, 0.06f), cold = new Color(0.42f, 0.58f, 0.95f), mix = 1f };
            Color fire = glows.Grade(new Color(1f, 0.55f, 0.15f));
            Color.RGBToHSV(fire, out float fireHue, out _, out _);
            Assert.IsTrue(fireHue < 0.05f || fireHue > 0.95f, $"fire leans to blood red, hue {fireHue}");
            Color frost = glows.Grade(new Color(0.6f, 0.8f, 1f));
            Color.RGBToHSV(frost, out float frostHue, out _, out _);
            Assert.IsTrue(frostHue > 0.55f && frostHue < 0.7f, $"frost leans to cold blue, hue {frostHue}");
            Assert.AreEqual(Color.grey, glows.Grade(Color.grey), "grey has no colour to lean with");
            Assert.AreEqual(0.5f, glows.Grade(new Color(1f, 0.5f, 0.2f, 0.5f)).a, "the alpha is kept");
            glows.mix = 0f;
            Assert.AreEqual(new Color(1f, 0.55f, 0.15f), glows.Grade(new Color(1f, 0.55f, 0.15f)), "no mix, no change");
        }
    }
}
