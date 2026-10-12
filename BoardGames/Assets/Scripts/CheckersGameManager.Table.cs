using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// Running a table, the same way for every kind of game: <see cref="BeginTable"/> seats the players and starts the
    /// director, which offers each turn to a person at this device or to the computer and waits for the board to show
    /// every move; <see cref="AfterEntry"/> is the one place that reacts to a command the table took (a move, giving up,
    /// the clock running out, a seat handed to the computer); the clicks of the board become moves here
    /// (<see cref="SquareClicked"/>); and <see cref="Finish"/> records the result, the stars and the progress.
    /// </summary>
    public partial class CheckersGameManager
    {
        /// <summary>Seconds the host waits for a move it sent for the computer before it thinks again.</summary>
        private const float ComputerMovePatience = 4f;

        private TableSetup pendingSetup;
        private CheckersGame pendingSaved;
        private int pendingLevel = -1;
        private Coroutine director;
        /// <summary>Counts what changed the table: the director looks at it again when it changes.</summary>
        private int version;
        private bool finishing;
        private int handedOverPly = -1;
        private int hovered = -1;
        private float clockRemaining = -1f;
        private int clockPly = -1;
        private int reportedWorth = -1;

        /// <summary>Seconds left for the seat to move; -1 without a clock.</summary>
        public float ClockRemaining
        {
            get
            {
                if (table == null || table.IsOver)
                {
                    return -1f;
                }
                if (IsOnlineGame)
                {
                    RoomTurnInfo turn = online != null && online.ServerClient != null ? online.ServerClient.Turn : null;
                    return turn != null && turn.HasLimit ? turn.Remaining : -1f;
                }
                return table.Setup.clockSeconds > 0 ? Mathf.Max(0f, clockRemaining) : -1f;
            }
        }

        /// <summary>1 with the whole time (or no clock), 0 when it ran out.</summary>
        public float ClockFraction
        {
            get
            {
                if (table == null || table.IsOver)
                {
                    return 1f;
                }
                if (IsOnlineGame)
                {
                    RoomTurnInfo turn = online != null && online.ServerClient != null ? online.ServerClient.Turn : null;
                    return turn != null ? turn.RemainingFraction : 1f;
                }
                return table.Setup.clockSeconds > 0 ? Mathf.Clamp01(clockRemaining / table.Setup.clockSeconds) : 1f;
            }
        }

        /// <summary>Whose turn it is, in the language shown.</summary>
        public string TurnWords
        {
            get
            {
                if (table == null)
                {
                    return "";
                }
                if (table.IsOver)
                {
                    return L.T("Game over");
                }
                int seat = table.SeatToMove;
                if (seat < 0)
                {
                    return "";
                }
                if (table.IsComputer(seat))
                {
                    return L.F("{0} is thinking...", NameOf(seat));
                }
                if (IsOnlineGame && seat != onlineSeat || IsHotSeat)
                {
                    return L.F("{0}'s move", NameOf(seat));
                }
                return L.T("Your move");
            }
        }

        /// <summary>The name of a seat as the interface shows it.</summary>
        public string NameOf(int seat)
        {
            TableSetup setup = Setup;
            if (setup == null || seat < 0 || seat >= setup.seats.Length || setup.seats[seat] == null)
            {
                return L.F("Player {0}", seat + 1);
            }
            string name = setup.seats[seat].name;
            return string.IsNullOrEmpty(name) ? L.F("Player {0}", seat + 1) : name;
        }

        // ------------------------------------------------------------------ starting

        /// <summary>The table of a campaign level: the player in seat 0 on the level's side, the computer in seat 1.</summary>
        private static TableSetup SetupOf(CheckersLevel level)
        {
            var setup = new TableSetup { variant = level.Variant, clockSeconds = 0 };
            setup.seats[0] = new TableSeat { name = L.F("Player {0}", 1), computer = false, level = 1 };
            setup.seats[1] = new TableSeat { name = L.T("Computer"), computer = true, level = level.ComputerLevel };
            setup.firstSeat = setup.Rules.FirstMover == level.Side ? 0 : 1;
            return setup;
        }

        /// <summary>
        /// Starts a game at <paramref name="setup"/>: a new one, or <paramref name="saved"/> going on. The players take
        /// their seats, the camera and the board show the position, and the director runs from here.
        /// </summary>
        private void BeginTable(TableSetup setup, uint seed, CheckersGame saved = null, int level = -1)
        {
            StopDirecting();
            table = saved != null ? new CheckersTable(setup, saved, seed) : new CheckersTable(setup, seed);
            campaignLevel = level;
            finishing = false;
            handedOverPly = -1;
            clockPly = -1;
            reportedWorth = -1;
            StarsEarned = 0;
            ResultTitle = "";
            ResultDetail = "";
            ClearSelection();
            var active = new List<IPlayer>();
            for (int i = 0; i < players.Length && i < 2; i++)
            {
                if (players[i] == null)
                {
                    continue;
                }
                PlayerControl control = setup.seats[i].computer ? PlayerControl.Computer
                    : IsOnlineGame && i != onlineSeat ? PlayerControl.Remote : PlayerControl.Local;
                players[i].Assign(i, control, NameOf(i));
                active.Add(players[i]);
            }
            ActivePlayers = active;
            if (cameraRig != null)
            {
                cameraRig.SetTitle(false, false);
                cameraRig.Frame(Game.board.size, HomeSide(), true);
            }
            board?.Show(Game, true);
            UpdateHighlights();
            TransitionState(BaseGameState.Running);
            director = StartCoroutine(Direct());
            RaiseChanged();
        }

        /// <summary>The side the camera shows at the bottom: the person's online and against the computer, Light between two people.</summary>
        private Side HomeSide()
        {
            if (table == null)
            {
                return Side.Light;
            }
            if (IsOnlineGame)
            {
                return Setup.SideOf(onlineSeat);
            }
            int human = SingleHumanSeat();
            return human >= 0 ? Setup.SideOf(human) : Side.Light;
        }

        /// <summary>The one seat a person plays against the computer, or -1 (two people, or two computers).</summary>
        private int SingleHumanSeat()
        {
            TableSetup setup = Setup;
            if (setup == null || setup.Humans != 1)
            {
                return -1;
            }
            return setup.seats[0].computer ? 1 : 0;
        }

        private void StopDirecting()
        {
            if (director != null)
            {
                StopCoroutine(director);
                director = null;
            }
            WaitingForHuman = false;
        }

        /// <summary>Ends the table: the players leave their seats and the title board shows.</summary>
        private void EndTable()
        {
            StopDirecting();
            table = null;
            campaignLevel = -1;
            ClearSelection();
            foreach (CheckersPlayer player in players)
            {
                player?.Assign(-1, PlayerControl.Local);
            }
            ShowIdleBoard();
            RaiseChanged();
        }

        /// <summary>The campaign window's Play: the level starts through the controller.</summary>
        public void StartCampaignLevel(int level)
        {
            if (InSession || campaign == null || level < 0 || level >= campaign.Count)
            {
                return;
            }
            if (table != null)
            {
                EndTable();
                EndLocalMatch();
            }
            pendingSetup = null;
            pendingSaved = null;
            pendingLevel = -1;
            sound?.Play(ClipKind.Click);
            controller.PrepareGame(LevelData.Create(level));
        }

        /// <summary>Play again: the same campaign level, the same local setup, or back to the room online.</summary>
        public void PlayAgain()
        {
            if (table == null)
            {
                return;
            }
            if (IsOnlineGame)
            {
                RequestState(BaseGameState.Initialization);
                return;
            }
            if (campaignLevel >= 0)
            {
                StartCampaignLevel(campaignLevel);
                return;
            }
            if (InLocalMatch)
            {
                LocalMatch match = LocalMatch;
                EndTable();
                BeginLocalMatch(match);
                return;
            }
            TableSetup again = Setup.Clone();
            EndTable();
            pendingSetup = again;
            pendingSaved = null;
            pendingLevel = -1;
            controller.PrepareGame(CurrentLevel);
        }

        /// <summary>
        /// Leaves the game for the title screen (a game at this device stays saved and continues from there); online the
        /// room is left too, for the lobby (<see cref="PlayAgain"/> is the way back to the room).
        /// </summary>
        public void ReturnToTitle()
        {
            if (table == null)
            {
                TransitionState(BaseGameState.Initialization);
                return;
            }
            if (IsOnlineGame)
            {
                if (online != null)
                {
                    online.LeaveMatch();
                }
                else
                {
                    RequestState(BaseGameState.Initialization);
                }
                return;
            }
            if (!table.IsOver)
            {
                Autosave();
            }
            EndTable();
            EndLocalMatch();
            TransitionState(BaseGameState.Initialization);
        }

        // ------------------------------------------------------------------ directing

        /// <summary>Whether nothing changed the table since the director looked at it at <paramref name="at"/>.</summary>
        private bool Same(int at)
        {
            return table != null && version == at && IsGameRunning;
        }

        private IEnumerator Direct()
        {
            yield return null;
            while (table != null)
            {
                if (!IsGameRunning || (board != null && board.IsAnimating) || IsHandingOver)
                {
                    yield return null;
                    continue;
                }
                if (table.IsOver)
                {
                    yield return Finish();
                    director = null;
                    yield break;
                }
                int seat = table.SeatToMove;
                if (seat < 0)
                {
                    yield return null;
                    continue;
                }
                int at = version;
                if (table.IsComputer(seat))
                {
                    if (IsOnlineGame)
                    {
                        yield return DirectComputerOnline(seat, at);
                    }
                    else
                    {
                        yield return DirectComputer(seat, at);
                    }
                    continue;
                }
                bool here = !IsOnlineGame || seat == onlineSeat;
                if (!here || IsOnlineGame && online.AwaitingEcho)
                {
                    // Somebody else moves, or the move of the person here is on its way: what it did shows when it comes back.
                    RaiseChanged();
                    while (Same(at) && (!here || online.AwaitingEcho))
                    {
                        yield return null;
                    }
                    continue;
                }
                if (IsHotSeat && LocalMatch != null && LocalMatch.HandOver && handedOverPly != Game.ply)
                {
                    handedOverPly = Game.ply;
                    bool ready = false;
                    HandOver(LocalMatch[seat], () => ready = true, L.F("{0}'s move", NameOf(seat)));
                    while (!ready && Same(at))
                    {
                        yield return null;
                    }
                    if (!Same(at))
                    {
                        continue;
                    }
                }
                if (IsHotSeat && Options.turnBoard && cameraRig != null)
                {
                    cameraRig.Frame(Game.board.size, Setup.SideOf(seat), false);
                }
                StartClock();
                WaitingForHuman = true;
                UpdateHighlights();
                RaiseChanged();
                while (Same(at))
                {
                    yield return null;
                }
                WaitingForHuman = false;
            }
            director = null;
        }

        /// <summary>The computer's turn at this device: a pause to watch, then its move through the same path as a click.</summary>
        private IEnumerator DirectComputer(int seat, int at)
        {
            RaiseChanged();
            StartClock();
            yield return new WaitForSeconds(Mathf.Max(0.05f, Options.thinkingDelay));
            if (!Same(at))
            {
                yield break;
            }
            Move move = CheckersAI.Choose(Game, table.LevelOf(seat), aiRandom);
            if (move == null)
            {
                Debug.LogWarning($"Checkers: the computer in seat {seat} has no move.");
                yield return null;
                yield break;
            }
            ApplyLocalCommand(CheckersCommand.MoveAlong(seat, move));
        }

        /// <summary>The computer's turn online: the host's client works the move out and sends it through the log.</summary>
        private IEnumerator DirectComputerOnline(int seat, int at)
        {
            RaiseChanged();
            bool host = online.IsHost;
            if (!host)
            {
                while (Same(at) && host == online.IsHost)
                {
                    yield return null;
                }
                yield break;
            }
            if (online.AwaitingComputer)
            {
                // The move sent for the computer is still on its way: it is waited for, not worked out a second time.
                while (Same(at) && online.AwaitingComputer)
                {
                    yield return null;
                }
                yield break;
            }
            yield return new WaitForSeconds(Mathf.Max(0.05f, Options.thinkingDelay));
            if (!Same(at))
            {
                yield break;
            }
            Move move = CheckersAI.Choose(Game, table.LevelOf(seat), aiRandom);
            if (move != null)
            {
                online.PlayComputer(CheckersCommand.MoveAlong(seat, move));
            }
            float patience = Time.unscaledTime + ComputerMovePatience;
            while (Same(at) && host == online.IsHost && (Time.unscaledTime < patience || online.AwaitingComputer))
            {
                yield return null;
            }
        }

        // ------------------------------------------------------------------ entries

        /// <summary>A command of a person or the computer at this device: the table takes it and the game reacts.</summary>
        public void ApplyLocalCommand(CheckersCommand command)
        {
            if (table == null || command == null || IsOnlineGame)
            {
                return;
            }
            bool ok = table.Apply(command, NewSeed());
            AfterEntry(command, ok, LockstepEntryKind.Command, null);
        }

        /// <summary>
        /// The one place that reacts to an entry the table took, offline and online alike: the selection clears, the
        /// board hears of a move, the interface of a time-out or a seat handed to the computer, a game at this device
        /// saves itself after a person's move, and the interface redraws.
        /// </summary>
        private void AfterEntry(CheckersCommand command, bool accepted, LockstepEntryKind kind, IReadOnlyList<CheckersCommand> made)
        {
            if (table == null)
            {
                return;
            }
            version++;
            WaitingForHuman = false;
            ClearSelection();
            switch (kind)
            {
                case LockstepEntryKind.Command when accepted && command != null:
                    if (command.Kind == CommandKind.Move)
                    {
                        MoveTaken(Game.lastMove);
                        if (!IsOnlineGame && !table.IsComputer(command.seat) && !table.IsOver)
                        {
                            Autosave();
                        }
                    }
                    else if (command.Kind == CommandKind.Resign)
                    {
                        ui?.Toast(L.F("{0} resigned.", NameOf(command.seat)));
                    }
                    break;
                case LockstepEntryKind.Command:
                    if (command != null && (!IsOnlineGame || command.seat == onlineSeat))
                    {
                        sound?.Play(ClipKind.Click);
                    }
                    break;
                case LockstepEntryKind.Timeout when made != null && made.Count > 0:
                    ui?.Toast(L.F("Time is up for {0}.", NameOf(made[0].seat)));
                    foreach (CheckersCommand move in made)
                    {
                        if (move.Kind == CommandKind.Move)
                        {
                            MoveTaken(Game.lastMove);
                        }
                    }
                    break;
                case LockstepEntryKind.SeatToComputer when accepted:
                    SeatWentToComputer(command != null ? command.seat : -1);
                    break;
            }
            if (!table.IsOver)
            {
                clockPly = -1;
            }
            else
            {
                WriteResult();
            }
            UpdateHighlights();
            RaiseChanged();
        }

        /// <summary>A move was applied: its sound, the board plays it (the director waits for the animation), and the game hears of it.</summary>
        private void MoveTaken(Move move)
        {
            if (move == null)
            {
                return;
            }
            sound?.Play(move.promotes ? ClipKind.King : move.IsCapture ? ClipKind.Capture : ClipKind.Move);
            board?.Animate(move, Game, null);
            MovePlayed?.Invoke(move);
        }

        /// <summary>A player left the table: the computer plays the seat on.</summary>
        private void SeatWentToComputer(int seat)
        {
            if (seat < 0 || seat > 1)
            {
                return;
            }
            if (seat < players.Length && players[seat] != null)
            {
                players[seat].Assign(seat, PlayerControl.Computer, NameOf(seat));
            }
            ui?.Toast(L.F("{0} left the table. The computer plays on.", NameOf(seat)));
        }

        // ------------------------------------------------------------------ the clock

        /// <summary>A turn of a game at this device begins: the clock of the seat to move is full again.</summary>
        private void StartClock()
        {
            if (table == null || IsOnlineGame || table.Setup.clockSeconds <= 0 || clockPly == Game.ply)
            {
                return;
            }
            clockPly = Game.ply;
            clockRemaining = table.Setup.clockSeconds;
        }

        /// <summary>Counts the time of the seat to move in a game at this device; at zero the table makes the default move.</summary>
        private void TickClock(float deltaTime)
        {
            if (table == null || IsOnlineGame || table.IsOver || table.Setup.clockSeconds <= 0 || clockPly != Game.ply || !IsGameRunning)
            {
                return;
            }
            if (board != null && board.IsAnimating || IsHandingOver)
            {
                return;
            }
            clockRemaining -= deltaTime;
            if (clockRemaining > 0f)
            {
                return;
            }
            clockRemaining = 0f;
            List<CheckersCommand> made = table.Timeout(NewSeed());
            AfterEntry(null, made.Count > 0, LockstepEntryKind.Timeout, made);
        }

        // ------------------------------------------------------------------ input

        /// <summary>A click or tap on a square: selects a piece, plays a move, or picks the route of a capture step by step.</summary>
        public void SquareClicked(int square)
        {
            if (!WaitingForHuman || table == null || table.IsOver || square < 0 || square >= Game.board.cells.Length)
            {
                return;
            }
            if (Selected >= 0 && square != Selected)
            {
                int[] prefix = PartialPath ?? new[] { Selected };
                var withPrefix = new List<Move>();
                foreach (Move move in SelectedMoves)
                {
                    if (StartsWith(move.path, prefix))
                    {
                        withPrefix.Add(move);
                    }
                }
                var ending = new List<Move>();
                var stepping = new List<Move>();
                foreach (Move move in withPrefix)
                {
                    if (move.To == square)
                    {
                        ending.Add(move);
                    }
                    if (move.path.Length > prefix.Length && move.path[prefix.Length] == square)
                    {
                        stepping.Add(move);
                    }
                }
                if (ending.Count == 1 && (stepping.Count <= 1 || ending[0].path.Length == prefix.Length + 1))
                {
                    TryPlay(ending[0]);
                    return;
                }
                if (stepping.Count > 0)
                {
                    var extended = new int[prefix.Length + 1];
                    prefix.CopyTo(extended, 0);
                    extended[prefix.Length] = square;
                    PartialPath = extended;
                    if (stepping.Count == 1 && stepping[0].path.Length == extended.Length)
                    {
                        TryPlay(stepping[0]);
                        return;
                    }
                    sound?.Play(ClipKind.Click);
                    UpdateHighlights();
                    RaiseChanged();
                    return;
                }
                if (ending.Count > 1)
                {
                    PartialPath = new[] { Selected };
                    ui?.Toast(L.T("Several routes end there: click the squares of the route."));
                    UpdateHighlights();
                    RaiseChanged();
                    return;
                }
            }
            Side side = Setup.SideOf(table.SeatToMove);
            sbyte piece = Game.board[square];
            if (piece != Pieces.None && Pieces.Belongs(piece, side))
            {
                IReadOnlyList<Move> moves = Game.LegalFrom(square);
                if (moves.Count > 0)
                {
                    Selected = square;
                    SelectedMoves = moves;
                    PartialPath = null;
                    sound?.Play(ClipKind.Click);
                    UpdateHighlights();
                    RaiseChanged();
                    return;
                }
                // The piece cannot move now; most often because another piece must capture.
                if (MustCapture())
                {
                    ui?.Toast(L.T("A capture is mandatory: another piece must jump."));
                }
                sound?.Play(ClipKind.Click);
            }
            ClearSelection();
            UpdateHighlights();
            RaiseChanged();
        }

        /// <summary>Whether the side to move has to capture (every legal move is a capture then).</summary>
        private bool MustCapture()
        {
            foreach (Move move in Game.Legal())
            {
                if (move != null && move.IsCapture)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The squares of the pieces that may move now, each once: shown faintly while nothing is selected.</summary>
        private List<int> MovableSquares()
        {
            var squares = new List<int>();
            foreach (Move move in Game.Legal())
            {
                if (move != null && !squares.Contains(move.From))
                {
                    squares.Add(move.From);
                }
            }
            return squares;
        }

        private static bool StartsWith(int[] path, int[] prefix)
        {
            if (path == null || prefix == null || path.Length < prefix.Length)
            {
                return false;
            }
            for (int i = 0; i < prefix.Length; i++)
            {
                if (path[i] != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>The pointer is over a square (-1 for none): the legal destination under it lights up.</summary>
        public void SquareHovered(int square)
        {
            if (hovered == square)
            {
                return;
            }
            hovered = square;
            UpdateHighlights();
        }

        /// <summary>Plays <paramref name="move"/> for the person to move when it is legal now: the way the tours move.</summary>
        public bool TryPlay(Move move)
        {
            if (!WaitingForHuman || table == null || move == null || Game.FindLegal(move.path) == null)
            {
                return false;
            }
            int seat = table.SeatToMove;
            bool sent = Commands != null && Commands.Move(seat, move);
            if (sent && IsOnlineGame)
            {
                // The move is on its way to the server; what it did shows when it comes back.
                version++;
                WaitingForHuman = false;
                ClearSelection();
                UpdateHighlights();
                RaiseChanged();
            }
            else if (!sent)
            {
                sound?.Play(ClipKind.Click);
            }
            return sent;
        }

        /// <summary>The person at this device gives up: the one to move between two people, the local seat online.</summary>
        public void ResignLocal()
        {
            if (table == null || table.IsOver || !IsGameRunning)
            {
                return;
            }
            int seat = IsOnlineGame ? onlineSeat : IsHotSeat ? table.SeatToMove : SingleHumanSeat();
            if (seat < 0 || table.IsComputer(seat))
            {
                return;
            }
            Commands?.Resign(seat);
        }

        private void ClearSelection()
        {
            Selected = -1;
            SelectedMoves = noMoves;
            PartialPath = null;
        }

        private void UpdateHighlights()
        {
            if (board == null || table == null)
            {
                return;
            }
            bool showMoves = Options != null && Options.showMoves;
            IReadOnlyList<Move> shown = showMoves ? SelectedMoves : noMoves;
            IReadOnlyList<int> movable = showMoves && WaitingForHuman && Selected < 0 ? MovableSquares() : null;
            board.SetHighlights(Selected, shown, Game.lastMove, WaitingForHuman ? hovered : -1, PartialPath, movable);
        }

        // ------------------------------------------------------------------ ending

        /// <summary>The words of the result, in the language shown (read again when the language changes).</summary>
        private void WriteResult()
        {
            CheckersGame game = Game;
            if (game == null || !game.IsOver)
            {
                ResultTitle = "";
                ResultDetail = "";
                return;
            }
            int winnerSeat = table.WinnerSeat;
            string title;
            if (winnerSeat < 0)
            {
                title = L.T("Draw");
            }
            else if (IsOnlineGame ? winnerSeat == onlineSeat : winnerSeat == SingleHumanSeat())
            {
                title = L.T("You win!");
            }
            else
            {
                title = L.F("{0} wins!", NameOf(winnerSeat));
            }
            string detail;
            switch (game.Reason)
            {
                case EndReason.NoMoves:
                    detail = winnerSeat >= 0 ? L.F("{0} has no moves left.", NameOf(1 - winnerSeat)) : L.T("Nobody can move.");
                    break;
                case EndReason.Resigned:
                    detail = winnerSeat >= 0 ? L.F("{0} resigned.", NameOf(1 - winnerSeat)) : L.T("The game was given up.");
                    break;
                case EndReason.DrawByPlies:
                    detail = L.T("Fifty moves without a capture or a man moving.");
                    break;
                case EndReason.CalledOff:
                    detail = L.T("The game was called off.");
                    break;
                default:
                    detail = "";
                    break;
            }
            if (campaignLevel >= 0)
            {
                detail = string.IsNullOrEmpty(detail) ? L.F("Stars: {0} of 3", StarsEarned) : detail + "  " + L.F("Stars: {0} of 3", StarsEarned);
            }
            ResultTitle = title;
            ResultDetail = detail;
        }

        /// <summary>The rules ended the game: the stars and the progress, the report to the room, then the results screen.</summary>
        private IEnumerator Finish()
        {
            if (finishing || table == null)
            {
                yield break;
            }
            finishing = true;
            WaitingForHuman = false;
            ClearSelection();
            UpdateHighlights();
            int winnerSeat = table.WinnerSeat;
            bool localWon;
            if (IsOnlineGame)
            {
                ReportFinished(winnerSeat);
                localWon = winnerSeat == onlineSeat || winnerSeat < 0 && Game.Reason == EndReason.CalledOff && LocalPlace() == 1;
            }
            else
            {
                int human = SingleHumanSeat();
                if (human >= 0)
                {
                    bool won = winnerSeat == human;
                    CheckersLevel level = campaignLevel >= 0 && campaign != null ? campaign.Level(campaignLevel) : null;
                    if (level != null)
                    {
                        StarsEarned = CheckersCampaign.StarsFor(level, Game, Setup.SideOf(human));
                        if (won)
                        {
                            Progress.RecordLevel(campaignLevel, StarsEarned, CheckersCampaign.ScoreFor(Game, Setup.SideOf(human)));
                        }
                    }
                    Progress.RecordGame(won, winnerSeat < 0);
                    PlayerScore = won ? CheckersCampaign.ScoreFor(Game, Setup.SideOf(human)) : 0;
                    localWon = won;
                }
                else
                {
                    localWon = true;
                }
                ForgetAutosave();
            }
            sound?.Play(localWon && winnerSeat >= 0 ? ClipKind.Win : ClipKind.Lose);
            WriteResult();
            RaiseChanged();
            yield return new WaitForSeconds(0.8f);
            if (table == null)
            {
                yield break;
            }
            TransitionState(localWon ? BaseGameState.Victory : BaseGameState.GameOver);
            RaiseChanged();
        }
    }
}
