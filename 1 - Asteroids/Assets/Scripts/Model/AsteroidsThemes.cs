using Gamebox;
using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The active <see cref="AsteroidsTheme"/> for the parts of the game that have no manager at hand (a pooled rock being
    /// configured, the ship's visuals, the backdrop, the terrain): the game's theme as <see cref="GameThemes"/> knows it,
    /// kept until the theme changes, and the mappings of a mission's sector and world into the active look.
    /// </summary>
    public static class AsteroidsThemes
    {
        private static AsteroidsTheme active;
        private static bool known;
        private static bool watching;

        /// <summary>The theme the game shows now; null for a game without themes.</summary>
        public static AsteroidsTheme Active
        {
            get
            {
                if (!known)
                {
                    active = GameThemes.Active<AsteroidsTheme>(GameType.Asteroids);
                    known = true;
                    Watch();
                }
                return active;
            }
        }


        /// <summary>Forgets the theme found so far; the next ask reads it again.</summary>
        public static void Forget()
        {
            known = false;
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            known = false;
            active = null;
        }


        private static void Watch()
        {
            if (watching)
            {
                return;
            }
            watching = true;
            GameThemes.Changed += OnChanged;
        }


        private static void OnChanged(GameType game, GameTheme theme)
        {
            if (game == GameType.Asteroids)
            {
                active = theme as AsteroidsTheme;
                known = true;
            }
        }


        /// <summary>The active theme's counterpart of a mission's sector (<paramref name="of"/> itself without a theme).</summary>
        public static SectorTheme Sector(SectorTheme of)
        {
            AsteroidsTheme theme = Active;
            return theme != null ? theme.Sector(of) : of;
        }


        /// <summary>The active theme's counterpart of a strike mission's world (<paramref name="of"/> itself without a theme).</summary>
        public static StrikeTheme Strike(StrikeTheme of)
        {
            AsteroidsTheme theme = Active;
            return theme != null ? theme.Strike(of) : of;
        }


        /// <summary>The accent colour of a mission's sector in the active look.</summary>
        public static Color Accent(SectorTheme of, Color fallback)
        {
            SectorTheme sector = Sector(of);
            return sector != null ? sector.Accent : fallback;
        }


        /// <summary>The accent colour of a strike mission's world in the active look.</summary>
        public static Color Accent(StrikeTheme of, Color fallback)
        {
            StrikeTheme world = Strike(of);
            return world != null ? world.Accent : fallback;
        }


        /// <summary>The look of a hull in the active theme, or null.</summary>
        public static AsteroidsTheme.ShipLook Ship(string hull)
        {
            AsteroidsTheme theme = Active;
            return theme != null ? theme.Ship(hull) : null;
        }


        /// <summary>The look of the rocks of <paramref name="kind"/> in the active theme, or null.</summary>
        public static AsteroidsTheme.AsteroidLook Asteroid(AsteroidKind kind)
        {
            AsteroidsTheme theme = Active;
            return theme != null ? theme.Asteroid(kind) : null;
        }


        /// <summary>A colour of the active theme's palette by field name, or <paramref name="fallback"/>.</summary>
        public static Color Color(string key, Color fallback)
        {
            AsteroidsTheme theme = Active;
            return theme != null ? theme.ColorOf("palette." + key, fallback) : fallback;
        }
    }
}
