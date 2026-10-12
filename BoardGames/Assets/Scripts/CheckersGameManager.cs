using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The Checkers game module. It seats the two players of a table (<see cref="TableSetup"/>), starts a
    /// <see cref="CheckersTable"/> and directs it: the director offers each turn to a person at this device (through the
    /// board) or to the computer (<see cref="CheckersAI"/>), waits for the board to animate every move and ends the game
    /// when the rules do. Every way of playing goes through the same path: a campaign level, a quick game and a game of
    /// two people at this device build a table here (CheckersGameManager.Table.cs, .Local.cs); a game with a player on
    /// another device is the room's table, changed by the action log alone (CheckersGameManager.Online.cs). Menus,
    /// pausing, custom settings and the state machine come from <see cref="BaseGameManager"/>; a game at this device
    /// saves itself after every move of a person (CheckersGameManager.Saves.cs). The interface redraws on
    /// <see cref="Changed"/>; the board animates on <see cref="MovePlayed"/>.
    /// </summary>
    public partial class CheckersGameManager : BaseGameManager
    {
        [SerializeField] private CheckersSettings settings;
        [SerializeField] private CheckersController controller;
        [SerializeField] private CheckersUI ui;
        [SerializeField] private CheckersCampaign campaign;
        [SerializeField] private BoardView board;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private CheckersAudio sound;
        [SerializeField] private CheckersPlayer[] players = new CheckersPlayer[2];

        private CheckersSettings customSettings;
        private CheckersSettings activeSettings;
        private CheckersProgress progress;
        private IRandom random;
        private IRandom aiRandom;
        private CheckersTable table;
        private int campaignLevel = -1;
        private readonly List<Move> noMoves = new List<Move>();

        /// <summary>The table of the running game, or null between games.</summary>
        public CheckersTable Table => table;

        public CheckersGame Game => table != null ? table.Game : null;

        public TableSetup Setup => table != null ? table.Setup : null;

        /// <summary>Whether the table is the room's: a game with a player on another device.</summary>
        public bool IsOnlineGame => table != null && onlineSeat >= 0;

        /// <summary>Online: the table seat of this device; offline -1 (both seats are here).</summary>
        public int LocalSeat => IsOnlineGame ? onlineSeat : -1;

        /// <summary>Whether the game is one of two people at this device.</summary>
        public bool IsHotSeat => table != null && !IsOnlineGame && table.Setup.Humans > 1;

        /// <summary>The campaign level played, or -1 for every other game.</summary>
        public int CampaignLevel => table != null ? campaignLevel : -1;

        /// <summary>Whether the seat to move is a person at this device, the board is idle and the game runs: a click counts.</summary>
        public bool WaitingForHuman { get; private set; }

        /// <summary>The selected square, or -1.</summary>
        public int Selected { get; private set; } = -1;

        /// <summary>The legal moves of the selected piece (empty when none is selected).</summary>
        public IReadOnlyList<Move> SelectedMoves { get; private set; }

        /// <summary>The squares clicked so far of a capture with several routes (the first is <see cref="Selected"/>), or null.</summary>
        public int[] PartialPath { get; private set; }

        /// <summary>Where the commands of the interface go: to the server in an online game, else straight to the table.</summary>
        public ICheckersCommands Commands => IsOnlineGame && online != null ? (ICheckersCommands)online : controller;

        public CheckersSettings Options => Settings as CheckersSettings ?? settings;

        public CheckersProgress Progress => progress ??= new CheckersProgress(Disk, GameType.Checkers);

        public CheckersTheme Look => ThemeAs<CheckersTheme>();

        /// <summary>The side the camera shows at the bottom now.</summary>
        public Side BottomSide => cameraRig != null ? cameraRig.Bottom : Side.Light;

        /// <summary>The stars the campaign game that just ended earned: 0 to 3.</summary>
        public int StarsEarned { get; private set; }

        public string ResultTitle { get; private set; } = "";

        public string ResultDetail { get; private set; } = "";

        /// <summary>Raised after every change of the table, the selection or the result: the interface redraws.</summary>
        public event Action Changed;

        /// <summary>Raised after a move was applied: the board animates it, and the director waits for it.</summary>
        public event Action<Move> MovePlayed;

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => settings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : settings;
            set => activeSettings = value as CheckersSettings;
        }

        protected override bool UsesTimer => false;

        private CheckersSettings CreateCustomSettings()
        {
            CheckersSettings copy = settings != null ? Instantiate(settings) : ScriptableObject.CreateInstance<CheckersSettings>();
            copy.name = $"{(settings != null ? settings.name : "CheckersSettings")} (custom)";
            return copy;
        }

        protected override void Awake()
        {
            foreach (CheckersPlayer player in players)
            {
                if (player != null)
                {
                    RegisterPlayer(player);
                }
            }
            base.Awake();
            random = new SystemRandom();
            aiRandom = new SystemRandom();
            SelectedMoves = noMoves;
        }

        protected override void Start()
        {
            progress = new CheckersProgress(Disk, GameType.Checkers);
            base.Start();
            ApplyLook(Look);
            ShowIdleBoard();
        }

        protected override void Update()
        {
            base.Update();
            TickClock(Time.deltaTime);
        }

        /// <summary>The player picked another look: the board, the lights and the interface take it at once.</summary>
        protected override void OnThemeChanged(GameTheme theme)
        {
            base.OnThemeChanged(theme);
            ApplyLook(theme as CheckersTheme);
            RaiseChanged();
        }

        private void ApplyLook(CheckersTheme theme)
        {
            if (theme == null)
            {
                return;
            }
            board?.ApplyTheme(theme);
            if (sound != null)
            {
                sound.Volume = Options != null ? Options.volume : 0.8f;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.ambientSky;
            RenderSettings.ambientEquatorColor = theme.ambientEquator;
            RenderSettings.ambientGroundColor = theme.ambientGround;
            Camera main = Camera.main;
            if (main != null)
            {
                main.backgroundColor = theme.background;
            }
            Light sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = theme.sunColor;
                sun.intensity = theme.sunIntensity;
            }
        }

        /// <summary>The language changed: the words the manager writes are read again.</summary>
        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            if (table != null && table.IsOver)
            {
                WriteResult();
            }
            RaiseChanged();
        }

        /// <summary>Back on the title: a game at this device that is still on the table (Escape on the results) is let go.</summary>
        public override void TransitionState(GameState state)
        {
            base.TransitionState(state);
            if (state != null && state.BaseState == BaseGameState.Initialization && table != null && !IsOnlineGame)
            {
                if (!table.IsOver)
                {
                    Autosave();
                }
                EndTable();
                EndLocalMatch();
            }
        }

        protected override void UpdateLevel()
        {
        }

        protected override void UpdateScore()
        {
        }

        public override void LoadLevel(int level)
        {
            LevelIndex = campaign != null ? Mathf.Clamp(level, 0, Mathf.Max(0, campaign.Count - 1)) : 0;
            CurrentLevel = LevelData.Create(LevelIndex);
            UpdateLevel();
        }

        /// <summary>
        /// Starts the game prepared for this call: the room's table online, the local setup a quick game or the setup
        /// window prepared, else the campaign level of <see cref="BaseGameManager.CurrentLevel"/>.
        /// </summary>
        public override void StartGame()
        {
            CheckersSettings options = Options;
            string error = "no settings";
            if (options == null || !options.AreSettingsValid(out error))
            {
                UI?.UpdateError(L.F("Cannot start: {0}", error));
                return;
            }
            if (InSession)
            {
                StartOnlineTable();
                return;
            }
            if (pendingSetup != null)
            {
                TableSetup setup = pendingSetup;
                CheckersGame saved = pendingSaved;
                int level = pendingLevel;
                pendingSetup = null;
                pendingSaved = null;
                pendingLevel = -1;
                BeginTable(setup, NewSeed(), saved, level);
                return;
            }
            CheckersLevel current = campaign != null ? campaign.Level(LevelIndex) : null;
            if (current == null)
            {
                UI?.UpdateError(L.T("There is no such level."));
                return;
            }
            BeginTable(SetupOf(current), NewSeed(), null, LevelIndex);
        }

        /// <summary>The base menu's multiplayer button: only from the title.</summary>
        public override void OpenOnline()
        {
            if (table != null || IsLocalSetupOpen)
            {
                return;
            }
            sound?.Play(ClipKind.Click);
            base.OpenOnline();
        }

        /// <summary>The menu's Two Players button: only from the title.</summary>
        public override void OpenLocalPlay()
        {
            if (table != null || InSession)
            {
                return;
            }
            sound?.Play(ClipKind.Click);
            base.OpenLocalPlay();
        }

        /// <summary>The title screen: an idle starting position of the quick game's rules behind the menu.</summary>
        private void ShowIdleBoard()
        {
            if (table != null || board == null)
            {
                return;
            }
            var idle = new CheckersGame(VariantRules.Of(Options != null ? Options.variant : 0).Variant);
            board.Show(idle, true);
            board.SetHighlights(-1, noMoves, null, -1, null);
            cameraRig?.Frame(idle.board.size, Side.Light, true);
            cameraRig?.SetTitle(true, false);
        }

        /// <summary>A screenshot of the game: an English game against the computer, eight plies in, without the interface.</summary>
        public override System.Collections.IEnumerator PoseForScreenshot()
        {
            var setup = new TableSetup { variant = (int)Variant.English, firstSeat = 1 };
            setup.seats[0] = new TableSeat { name = L.F("Player {0}", 1), computer = false, level = 1 };
            setup.seats[1] = new TableSeat { name = "Ada", computer = true, level = 1 };
            BeginTable(setup, NewSeed());
            for (int i = 0; i < 8 && table != null && !table.IsOver; i++)
            {
                int seat = table.SeatToMove;
                Move move = CheckersAI.Choose(Game, 1, aiRandom);
                if (move == null)
                {
                    break;
                }
                table.Apply(CheckersCommand.MoveAlong(seat, move), NewSeed());
            }
            if (table != null)
            {
                board.Show(Game, true);
                board.SetHighlights(-1, noMoves, Game.lastMove, -1, null);
            }
            yield return new WaitForSecondsRealtime(1.5f);
            HideScreenInterface();
        }

        private uint NewSeed()
        {
            return (uint)random.Range(1, int.MaxValue);
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
