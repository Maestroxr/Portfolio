using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The Board Games module: checkers, chess and backgammon on one table. It seats the two players of a table
    /// (<see cref="TableSetup"/>), starts the <see cref="BoardTable"/> of its rule set and directs it: the director offers
    /// each decision to a person at this device (through the game's board) or to the computer (<see cref="BoardAI"/>),
    /// waits for the game's views to animate every command and ends the game when the rules do. Every way of playing
    /// goes through the same path: a campaign level, a quick game and a game of two people at this device build a table
    /// here (BoardGamesManager.Table.cs, .Local.cs); a game with a player on another device is the room's table, changed
    /// by the action log alone (BoardGamesManager.Online.cs). What each game shows and how its clicks become commands
    /// is in its own half (BoardGamesManager.Checkers.cs, .Chess.cs, .Backgammon.cs), called through the hooks of
    /// BoardGamesManager.Modes.cs. Menus, pausing, custom settings and the state machine come from
    /// <see cref="BaseGameManager"/>; a game at this device saves itself after every decision of a person
    /// (BoardGamesManager.Saves.cs). The interface redraws on <see cref="Changed"/>.
    /// </summary>
    public partial class BoardGamesManager : BaseGameManager
    {
        [SerializeField] private BoardGamesSettings settings;
        [SerializeField] private BoardGamesController controller;
        [SerializeField] private BoardGamesUI ui;
        [SerializeField] private BoardGamesCampaign campaign;
        [SerializeField] private BoardView board;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private BoardGamesAudio sound;
        [SerializeField] private BoardGamesPlayer[] players = new BoardGamesPlayer[2];

        private BoardGamesSettings customSettings;
        private BoardGamesSettings activeSettings;
        private bool started;
        private BoardGamesProgress progress;
        private IRandom random;
        private IRandom aiRandom;
        private BoardTable table;
        private int campaignLevel = -1;

        /// <summary>The table of the running game, or null between games.</summary>
        public BoardTable Table => table;

        public TableSetup Setup => table != null ? table.Setup : null;

        /// <summary>
        /// Whether the table is the room's: a game played online, through the action log of the server. The shared
        /// screen of a room plays one with no seat of its own (<see cref="onlineSeats"/> empty).
        /// </summary>
        public bool IsOnlineGame => table != null && onlineTable;

        /// <summary>
        /// Online: the table seat the people at this device act for now (the seat to act when it is theirs, else the first
        /// of theirs); offline -1 (both seats are here).
        /// </summary>
        public int LocalSeat => IsOnlineGame ? onlineSeats.Acting(table.SeatToAct) : -1;

        /// <summary>Online: the table seats played at this device (one, or both when two people share it); offline none.</summary>
        public IReadOnlyList<int> LocalSeats => IsOnlineGame ? onlineSeats.All : Array.Empty<int>();

        /// <summary>Whether the game is one of two people at this device, not online.</summary>
        public bool IsHotSeat => table != null && !IsOnlineGame && table.Setup.Humans > 1;

        /// <summary>
        /// Whether more than one person plays at this device: two people in a game here, or online the player and a
        /// player who joined at this device, both still at the table (a seat the computer took over counts no more).
        /// </summary>
        public bool SeveralHere
        {
            get
            {
                if (!IsOnlineGame)
                {
                    return IsHotSeat;
                }
                int people = 0;
                foreach (int seat in onlineSeats.All)
                {
                    if (!table.IsComputer(seat))
                    {
                        people++;
                    }
                }
                return people > 1;
            }
        }

        /// <summary>
        /// Whether a person at this device plays <paramref name="seat"/>: a seat the computer does not play, and online
        /// one of the seats of this device.
        /// </summary>
        public bool PlayedHere(int seat)
        {
            if (table == null || seat < 0 || seat >= table.Setup.seats.Length || table.IsComputer(seat))
            {
                return false;
            }
            return !IsOnlineGame || onlineSeats.Has(seat);
        }

        /// <summary>Online: whether <paramref name="seat"/> is the seat of the player at this device themselves, not of a companion who joined here; offline false.</summary>
        public bool IsOwnSeat(int seat)
        {
            return IsOnlineGame && online != null && onlineSeats.Has(seat) && online.IsOwnTableSeat(seat);
        }

        /// <summary>The campaign level played, or -1 for every other game.</summary>
        public int CampaignLevel => table != null ? campaignLevel : -1;

        /// <summary>Whether the seat to act is a person at this device, the board is idle and the game runs: a click counts.</summary>
        public bool WaitingForHuman { get; private set; }

        /// <summary>Where the commands of the interface go: to the server in an online game, else straight to the table.</summary>
        public IBoardCommands Commands => IsOnlineGame && online != null ? (IBoardCommands)online : controller;

        public BoardGamesSettings Options => Settings as BoardGamesSettings ?? settings;

        public BoardGamesProgress Progress => progress ??= new BoardGamesProgress(Disk, GameType.BoardGames) { Avatar = LocalAvatar.Instance };

        public BoardGamesTheme Look => ThemeAs<BoardGamesTheme>();

        /// <summary>The side the camera shows at the bottom now.</summary>
        public Side BottomSide => cameraRig != null ? cameraRig.Bottom : Side.Light;

        /// <summary>The stars the campaign game that just ended earned: 0 to 3.</summary>
        public int StarsEarned { get; private set; }

        public string ResultTitle { get; private set; } = "";

        public string ResultDetail { get; private set; } = "";

        /// <summary>Raised after every change of the table, the selection or the result: the interface redraws.</summary>
        public event Action Changed;

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => settings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : settings;
            set
            {
                activeSettings = value as BoardGamesSettings;
                SettingsApplied();
            }
        }

        protected override bool UsesTimer => false;

        private BoardGamesSettings CreateCustomSettings()
        {
            BoardGamesSettings copy = settings != null ? Instantiate(settings) : ScriptableObject.CreateInstance<BoardGamesSettings>();
            copy.name = $"{(settings != null ? settings.name : "BoardGamesSettings")} (custom)";
            return copy;
        }

        protected override void Awake()
        {
            foreach (BoardGamesPlayer player in players)
            {
                if (player != null)
                {
                    RegisterPlayer(player);
                }
            }
            base.Awake();
            random = new SystemRandom();
            aiRandom = new SystemRandom();
        }

        protected override void Start()
        {
            progress = new BoardGamesProgress(Disk, GameType.BoardGames) { Avatar = LocalAvatar.Instance };
            base.Start();
            RecallTitleMode();
            ApplyLook(Look);
            ShowIdleBoard();
            started = true;
        }

        protected override void Update()
        {
            base.Update();
            TickClock(Time.deltaTime);
        }

        /// <summary>The player picked another look: the boards, the lights and the interface take it at once.</summary>
        protected override void OnThemeChanged(GameTheme theme)
        {
            base.OnThemeChanged(theme);
            ApplyLook(theme as BoardGamesTheme);
            RaiseChanged();
        }

        private void ApplyLook(BoardGamesTheme theme)
        {
            if (theme == null)
            {
                return;
            }
            board?.ApplyTheme(theme);
            GamesApplyLook(theme);
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

        /// <summary>
        /// Other settings became the active ones (saved, loaded, default or custom): the volume, the marks of the game
        /// played, or on the title the board of the title's game (its draughts rules may have changed).
        /// </summary>
        private void SettingsApplied()
        {
            if (!started)
            {
                return;
            }
            // The title's game is the one picked last, whichever settings are active.
            RecallTitleMode();
            if (sound != null && Options != null)
            {
                sound.Volume = Options.volume;
            }
            if (table == null)
            {
                ShowIdleBoard();
            }
            else
            {
                UpdateHighlights();
            }
            RaiseChanged();
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
            BoardGamesSettings options = Options;
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
                IBoardGame saved = pendingSaved;
                int level = pendingLevel;
                pendingSetup = null;
                pendingSaved = null;
                pendingLevel = -1;
                BeginTable(setup, NewSeed(), saved, level);
                return;
            }
            BoardGamesLevel current = campaign != null ? campaign.Level(LevelIndex) : null;
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
            PreselectLocalSetup();
            base.OpenLocalPlay();
        }

        /// <summary>The title screen: the starting position of the title's game behind the menu (the other games hide).</summary>
        private void ShowIdleBoard()
        {
            if (table != null)
            {
                return;
            }
            GameMode mode = TitleMode;
            EndGamesExcept(mode);
            GameShowIdle(mode);
        }

        /// <summary>The main line of the Ruy Lopez: both sides developed and castled, a position that reads as chess at a glance.</summary>
        private static readonly string[] PoseOpening =
        {
            "e2e4", "e7e5", "g1f3", "b8c6", "f1b5", "a7a6", "b5a4", "g8f6", "e1g1", "f8e7", "f1e1", "b7b5", "a4b3", "d7d6",
            "c2c3", "e8g8"
        };

        /// <summary>
        /// The launcher's picture of the game: a chess game against the computer in the main line of the Ruy Lopez, without
        /// the marks of the moves that may be made, framed low and close as the title frames its board, without the
        /// interface.
        /// </summary>
        public override System.Collections.IEnumerator PoseForScreenshot()
        {
            if (Options != null)
            {
                Options.showMoves = false;
            }
            var setup = new TableSetup { ruleSet = (int)RuleSet.Chess, firstSeat = 0 };
            setup.seats[0] = new TableSeat { name = L.F("Player {0}", 1), computer = false, level = 1 };
            setup.seats[1] = new TableSeat { name = "Ada", computer = true, level = 1 };
            BeginTable(setup, NewSeed());
            // The moves are played at once, before the director looks at the table for the first time.
            foreach (string uci in PoseOpening)
            {
                ChessGame chess = ChessGame;
                if (chess == null || chess.IsOver)
                {
                    break;
                }
                int from = ChessGame.Square(uci[0] - 'a', uci[1] - '1');
                int to = ChessGame.Square(uci[2] - 'a', uci[3] - '1');
                ChessMove move = chess.FindLegal(from, to);
                if (move == null || !table.Apply(BoardCommand.Play(table.SeatToAct, move.Path()), NewSeed()))
                {
                    break;
                }
            }
            if (table != null)
            {
                GameBegin();
                UpdateHighlights();
                RaiseChanged();
            }
            cameraRig?.SetTitle(true, true);
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
