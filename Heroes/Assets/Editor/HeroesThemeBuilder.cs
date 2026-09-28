using Gamebox;
using Gamebox.Editor;
using Gamebox.Launcher;
using UnityEditor;
using UnityEngine;

namespace Portfolio.Heroes.EditorTools
{
    /// <summary>
    /// Makes the theme assets (Content/Themes/*.asset) from the art each theme's builders made and the colours and light
    /// of its spec, and points the game's definition at them: the classic theme first, the others listed after it. They
    /// are written in place, so the definition keeps its references, and a theme whose art is not built is skipped.
    /// </summary>
    internal static class HeroesThemeBuilder
    {
        [MenuItem("Heroes/Rebuild Themes", false, 24)]
        public static void BuildMenu()
        {
            BuildAll();
            AssetDatabase.SaveAssets();
            Debug.Log("Heroes: themes built.");
        }

        /// <summary>Every theme whose art exists, and the definition's list of them.</summary>
        public static void BuildAll()
        {
            foreach (HeroesThemeSpec spec in HeroesThemeSpec.All)
            {
                Build(spec);
            }
            Definition();
        }

        /// <summary>The theme asset of <paramref name="spec"/>, from its art: created or updated in place.</summary>
        public static HeroesTheme Build(HeroesThemeSpec spec)
        {
            HeroesArt art = HeroesAssets.Load<HeroesArt>(spec.ArtAsset);
            if (art == null)
            {
                Debug.LogWarning($"Heroes: the art of the {spec.DisplayName} theme ({spec.ArtAsset}) is not built, so the theme is not.");
                return null;
            }
            HeroesTheme theme = HeroesAssets.Load<HeroesTheme>(spec.ThemeAsset);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<HeroesTheme>();
                HeroesAssets.EnsureFolderOf(spec.ThemeAsset);
                AssetDatabase.CreateAsset(theme, HeroesAssets.Path(spec.ThemeAsset));
            }
            HeroesAssets.SetString(theme, "displayName", spec.DisplayName);
            HeroesAssets.SetString(theme, "description", spec.Description);
            HeroesAssets.SetObject(theme, "preview", art.TownPortrait(Faction.Castle, 0));
            theme.art = art;
            theme.palette = Copy(spec.Palette);
            theme.atmosphere = Copy(spec.Atmosphere);
            HeroesArt.SkyArt mapSky = art.skies.Find(sky => sky.name == spec.MapSky) ?? art.Sky(TerrainType.Grass);
            theme.atmosphere.sky = mapSky != null ? mapSky.material : null;
            theme.glows = Copy(spec.Glows);
            theme.pointers = new HeroesTheme.Pointers
            {
                cursors = (Texture2D[])art.cursors.Clone(),
                hotspots = (Vector2[])art.cursorHotspots.Clone()
            };

            // The shared menu in the look of the theme, from the style the scene builder lays it out with; what the style
            // leaves to the prefab the theme names too, so it is complete and re-skins a menu whole.
            GameMenuInstaller.SkinFrom(HeroesSceneBuilder.MenuStyle(art, spec.Palette), theme.Menu, theme.Fonts);
            theme.Menu.backdrop = art.shade;
            theme.Menu.settingsWindow = art.frame;
            theme.Menu.rowsBackground = art.panel;
            theme.Menu.rowsColor = Color.white;
            EditorUtility.SetDirty(theme);
            return theme;
        }

        /// <summary>A copy of a serializable part of a spec, so the asset does not share the spec's own.</summary>
        private static T Copy<T>(T source) where T : class, new()
        {
            return source != null ? JsonUtility.FromJson<T>(JsonUtility.ToJson(source)) : new T();
        }

        /// <summary>The definition starts the game with the classic theme and lists every theme whose asset exists.</summary>
        public static void Definition()
        {
            var definition = HeroesAssets.Load<GameDefinition>("Resources/Games/Heroes.asset");
            if (definition == null)
            {
                return;
            }
            foreach (HeroesThemeSpec spec in HeroesThemeSpec.All)
            {
                HeroesTheme theme = HeroesAssets.Load<HeroesTheme>(spec.ThemeAsset);
                if (theme == null)
                {
                    continue;
                }
                definition.AddTheme(theme);
                if (spec == HeroesThemeSpec.Classic)
                {
                    definition.Theme = theme;
                }
            }
            EditorUtility.SetDirty(definition);
            GameThemes.ForgetDefinitions();
        }
    }
}
