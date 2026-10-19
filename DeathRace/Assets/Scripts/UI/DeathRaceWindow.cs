using Gamebox.UI;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What every window of Death Race but the garage shares on top of BaseGame's <see cref="OverlayWindow"/>: it puts
    /// the shared menu aside while it shows (<see cref="DeathRaceUI.Cover"/>), it knows the game and its look, and it
    /// builds itself (<see cref="Compose"/>) with the pieces of <see cref="RaceWindowParts"/>.
    /// </summary>
    public abstract class DeathRaceWindow : OverlayWindow
    {
        /// <summary>The pieces of this build of the window (made anew with every build).</summary>
        protected RaceWindowParts ui;

        protected DeathRaceManager Game => manager as DeathRaceManager;

        protected DeathRaceTheme Theme => Game != null ? Game.Look : null;

        protected override int SortingOrder => 410;

        protected Color Accent => ui.Accent;

        protected Color Warn => ui.Warn;

        /// <summary>Builds the window, shows it and puts the shared menu aside under it.</summary>
        protected void Present()
        {
            Open();
            Game?.Interface?.Cover(this);
        }

        public override void Hide()
        {
            base.Hide();
            Game?.Interface?.Uncover(this);
        }

        protected override void OnDestroy()
        {
            Game?.Interface?.Uncover(this);
            base.OnDestroy();
        }

        protected sealed override void BuildContent()
        {
            ui = new RaceWindowParts(kit, Theme, L);
            Compose();
        }

        /// <summary>What goes inside the frame, under <see cref="OverlayWindow.panel"/>, built with <see cref="ui"/>.</summary>
        protected abstract void Compose();
    }
}
