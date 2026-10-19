using Gamebox;
using Gamebox.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Portfolio.DeathRace.EditorTools
{
    /// <summary>
    /// Online play in the scene: the object "Online" with the game's server client (the GameServerClient stamped next to
    /// the bindings of Server/Lib.cs), the <see cref="DeathRaceOnlineController"/> and BaseGame's lobby, wired to the
    /// manager and the menu (BaseGame's <see cref="OnlineInstaller"/>). The lobby is styled from the starting look's lobby
    /// skin (fonts, colours, the nine sliced window, row, button and field sprites); a look the player picks later lays its
    /// own skin over it at run time. Races have no turns, so the turn banner stays away.
    /// </summary>
    internal static partial class DeathRaceSceneBuilder
    {
        /// <summary>The database the game's module is published as (spacetime.json).</summary>
        public const string Database = "skinnerboxes-deathrace";

        /// <summary>Installs online play; returns the online controller (the manager's lobby).</summary>
        private static Component BuildOnline(Scene scene, DeathRaceManager manager, DeathRaceUI ui, DeathRaceTheme starting)
        {
            return OnlineInstaller.Install(scene, typeof(Portfolio.DeathRace.Server.GameServerClient), typeof(DeathRaceOnlineController), manager, ui, Database,
                LobbyStyleOf(starting));
        }

        /// <summary>The lobby in the look of <paramref name="theme"/>: what its lobby skin sets, the lobby's own look for the rest.</summary>
        private static OnlineInstaller.LobbyStyle LobbyStyleOf(DeathRaceTheme theme)
        {
            var style = new OnlineInstaller.LobbyStyle { HideTurnBanner = true };
            LobbySkin skin = theme != null ? theme.Lobby : null;
            if (skin == null)
            {
                return style;
            }
            style.Font = skin.font;
            style.BodyFont = skin.bodyFont;
            style.Accent = Set(skin.accent);
            style.Window = Set(skin.window);
            style.Row = Set(skin.row);
            style.Text = Set(skin.text);
            style.Muted = Set(skin.muted);
            style.Good = Set(skin.good);
            style.Warn = Set(skin.warn);
            style.Dim = Set(skin.dim);
            style.ButtonText = Set(skin.buttonText);
            style.WindowSprite = skin.windowSprite;
            style.RowSprite = skin.rowSprite;
            style.ButtonSprite = skin.buttonSprite;
            style.InputSprite = skin.inputSprite;
            style.TileSprites = skin.tileSprites;
            style.SpriteTint = skin.spriteTint >= 0f ? skin.spriteTint : (float?)null;
            if (style.Accent == null && theme != null)
            {
                style.Accent = theme.hudAccent;
            }
            return style;
        }

        private static Color? Set(Color color)
        {
            return MenuSkin.IsSet(color) ? color : (Color?)null;
        }
    }
}
