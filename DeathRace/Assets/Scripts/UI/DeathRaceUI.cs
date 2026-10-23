using System.Collections.Generic;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The game's menu: BaseGame's shared menu (installed by the scene builder) with the game's buttons. Start is
    /// relabelled Career and opens the career; Quick Race, Split Screen (not on a touch screen) and Play Online sit under
    /// it. The shared menu does not show over the results of a race (the results window does), nor while one of the
    /// game's windows (the garage, the quick race setup) is open: a window asks for the menu to step aside with
    /// <see cref="Cover"/> and gives it back with <see cref="Uncover"/>.
    /// </summary>
    public sealed class DeathRaceUI : GameUI
    {
        [SerializeField] private Button quickRaceButton;
        [SerializeField] private Button localPlayButton;
        [SerializeField] private Button onlineButton;

        private readonly HashSet<object> covers = new HashSet<object>();

        /// <summary>The application is shutting down: a window closing then leaves the title screen's corners alone (their objects go in any order).</summary>
        private static bool quitting;

        private DeathRaceManager Game => Manager as DeathRaceManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void WatchQuitting()
        {
            quitting = false;
            Application.quitting += () => quitting = true;
        }

        /// <summary>Whether one of the game's windows has the menu stepped aside.</summary>
        public bool IsCovered => covers.Count > 0;

        /// <summary>The badge of the player's avatar in the top left corner of the title screen.</summary>
        protected override (Vector2 anchor, Vector2 offset)? AvatarPlace => (new Vector2(0f, 1f), new Vector2(36f, -30f));

        protected override void Awake()
        {
            base.Awake();
            quickRaceButton = quickRaceButton != null ? quickRaceButton : FindMenuButton("QuickRace");
            localPlayButton = localPlayButton != null ? localPlayButton : FindMenuButton("LocalPlay");
            onlineButton = onlineButton != null ? onlineButton : FindMenuButton("PlayOnline");
            if (quickRaceButton != null)
            {
                quickRaceButton.onClick.AddListener(() => Game?.OpenQuickRace());
            }
            if (localPlayButton != null)
            {
                localPlayButton.onClick.AddListener(() => Game?.OpenLocalPlay());
                // Split screen needs keys or pads for everybody: a touch screen has one pair of thumbs.
                localPlayButton.gameObject.SetActive(!MobilePlatform.UsesTouch);
            }
            if (onlineButton != null)
            {
                onlineButton.onClick.AddListener(() => Game?.OpenOnline());
            }
        }

        /// <summary>The menu's first button opens the career (a new one, or the one being played).</summary>
        protected override void OnStartNewGameClicked()
        {
            if (Game != null && Manager.State.BaseState == BaseGameState.Initialization)
            {
                Game.OpenCareer();
                return;
            }
            base.OnStartNewGameClicked();
        }

        /// <summary>Puts the menu aside for <paramref name="owner"/> (a window of the game) until it calls <see cref="Uncover"/>.</summary>
        public void Cover(object owner)
        {
            if (owner != null && covers.Add(owner))
            {
                HideMenu();
                // The badge of the player's avatar (and the Shop button beside it) belongs to the title screen, not to a
                // window over it.
                GetComponent<AvatarCorner>()?.Hide();
                GetComponent<ShopCorner>()?.Hide();
            }
        }

        public void Uncover(object owner)
        {
            // A window closing as the scene goes away may outlive the menu by a moment, or the corners at the quit.
            if (this == null || quitting)
            {
                return;
            }
            if (owner != null && covers.Remove(owner) && covers.Count == 0 && Manager != null)
            {
                UpdateGameState(Manager.State);
                if (Manager.State.BaseState == BaseGameState.Initialization && AvatarPlace is (Vector2 anchor, Vector2 offset) place)
                {
                    AvatarCorner.For(this, Manager).At(place.anchor, place.offset).Show();
                    ShopCorner.For(this, Manager).Beside(place.anchor, place.offset).Show();
                }
            }
        }

        public override void UpdateGameState(GameState state)
        {
            base.UpdateGameState(state);
            // Resume belongs to the pause menu only (the title has computer cars racing behind it, nothing to resume),
            // and a career is saved from the title between races whenever there is one.
            if (ReturnToGame != null)
            {
                ReturnToGame.gameObject.SetActive(state.BaseState == BaseGameState.Paused);
            }
            if (state.BaseState == BaseGameState.Initialization && Manager != null)
            {
                SetInteractable(SaveGame, Manager.SaveRefusal == null);
            }
            bool results = state.BaseState == BaseGameState.GameOver || state.BaseState == BaseGameState.Victory;
            if (results || IsCovered)
            {
                // The results window (or the game's own window) is the screen now; Back still returns to the menu.
                HideMenu();
            }
        }

        public override void Show()
        {
            if (IsCovered)
            {
                return;
            }
            base.Show();
        }

        private Button FindMenuButton(string buttonName)
        {
            Transform parent = MenuButtonsUI != null ? MenuButtonsUI : MenuRoot != null ? MenuRoot : transform;
            foreach (Button button in parent.GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                {
                    return button;
                }
            }
            return null;
        }
    }
}
