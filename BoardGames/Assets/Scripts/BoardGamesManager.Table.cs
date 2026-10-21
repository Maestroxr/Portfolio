using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// Running a table, the same way for every game and every way of playing: <see cref="BeginTable"/> seats the
    /// players and starts the director, which offers each decision to a person at this device or to the computer and
    /// waits for the game's views to show every command; <see cref="AfterEntry"/> is the one place that reacts to a
    /// command the table took (a move, a roll, the cube, giving up, the clock running out, a seat handed to the
    /// computer); and <see cref="Finish"/> records the result, the stars and the progress. What a game shows is its own
    /// (the hooks of BoardGamesManager.Modes.cs).
    /// </summary>
    public partial class BoardGamesManager
    {
        /// <summary>Seconds the host waits for a command it sent for the computer before it thinks again.</summary>
        private const float ComputerMovePatience = 4f;

        private TableSetup pendingSetup;
        private IBoardGame pendingSaved;
        private int pendingLevel = -1;
        private Coroutine director;
        /// <summary>Counts what changed the table: the director looks at it again when it changes.</summary>
        private int version;
        private bool finishing;
        /// <summary>The seat the last hot seat hand-over was for (-1 none yet): a seat acting twice in a row is handed over once.</summary>
        private int handedOverSeat = -1;
        private float clockRemaining = -1f;
        /// <summary>The <see cref="IBoardGame.Turn"/> the local clock was started for; -1 restarts it.</summary>
        private int clockTurn = -1;
        /// <summary>What each seat played at this device was last reported to be worth online (-1 nothing yet).</summary>
        private readonly int[] reportedWorth = new int[DeviceSeats.TableSize];
        /// <summary>The command the computer worked out last (see <see cref="Think"/>).</summary>
        private BoardCommand thought;

        /// <summary>Seconds left for the seat to act; -1 without a clock.</summary>
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

        /// <summary>Whose decision it is, in the language shown (the game may say it its own way, e.g. "Roll the dice").</summary>
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
                int seat = table.SeatToAct;
                if (seat < 0)
                {
                    return "";
                }
                string words = GameTurnWords(seat);
                if (!string.IsNullOrEmpty(words))
                {
                    return words;
                }
                if (table.IsComputer(seat))
                {
                    return L.F("{0} is thinking...", NameOf(seat));
                }
                // A person on another device, or one of several here, is named; the one person here is addressed.
                if (!PlayedHere(seat) || SeveralHere)
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

        /// <summary>
        /// The table of a campaign level: the player in seat 0 on the level's side, the computer in seat 1. Backgammon
        /// levels play with the doubling cube and the opening roll.
        /// </summary>
        private static TableSetup SetupOf(BoardGamesLevel level)
        {
            var setup = new TableSetup { ruleSet = (int)level.RuleSet, clockSeconds = 0, doublingCube = true, openingRoll = true };
            setup.seats[0] = new TableSeat { name = L.F("Player {0}", 1), computer = false, level = 1 };
            setup.seats[1] = new TableSeat { name = L.T("Computer"), computer = true, level = level.ComputerLevel };
            setup.firstSeat = RuleSets.FirstMover(level.RuleSet) == level.Side ? 0 : 1;
            return setup;
        }

        /// <summary>
        /// Starts a game at <paramref name="setup"/>: a new one, or <paramref name="saved"/> going on. The players take
        /// their seats, the game shows its position (the other games hide), and the director runs from here.
        /// </summary>
        private void BeginTable(TableSetup setup, uint seed, IBoardGame saved = null, int level = -1)
        {
            StopDirecting();
            table = BoardTable.Create(setup, seed, saved);
            campaignLevel = level;
            finishing = false;
            handedOverSeat = -1;
            clockTurn = -1;
            for (int i = 0; i < reportedWorth.Length; i++)
            {
                reportedWorth[i] = -1;
            }
            thought = null;
            ForgetPerks();
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
                PlayerControl control = table.Setup.seats[i].computer ? PlayerControl.Computer
                    : IsOnlineGame && !onlineSeats.Has(i) ? PlayerControl.Remote : PlayerControl.Local;
                players[i].Assign(i, control, NameOf(i));
                active.Add(players[i]);
            }
            ActivePlayers = active;
            cameraRig?.SetTitle(false, false);
            EndGamesExcept(table.Mode);
            GameBegin();
            UpdateHighlights();
            TransitionState(BaseGameState.Running);
            director = StartCoroutine(Direct());
            RaiseChanged();
        }

        /// <summary>
        /// The side the camera shows at the bottom: the one person's here, online and against the computer; Light between
        /// two people at this device, online or not (the board turns to the one to act when the setting says so).
        /// </summary>
        private Side HomeSide()
        {
            if (table == null)
            {
                return Side.Light;
            }
            if (IsOnlineGame)
            {
                return onlineSeats.HomeSide(Setup);
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

        /// <summary>Ends the table: its game hides, the players leave their seats and the title board shows.</summary>
        private void EndTable()
        {
            StopDirecting();
            if (table != null)
            {
                GameEnd(table.Mode);
            }
            table = null;
            onlineTable = false;
            campaignLevel = -1;
            thought = null;
            ForgetPerks();
            foreach (BoardGamesPlayer player in players)
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
                if (!IsGameRunning || GameBusy() || IsHandingOver)
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
                int seat = table.SeatToAct;
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
                bool here = PlayedHere(seat);
                if (!here || IsOnlineGame && online.AwaitingEcho)
                {
                    // Somebody else acts, or the command of the person here is on its way: what it did shows when it comes back.
                    RaiseChanged();
                    while (Same(at) && (!here || online.AwaitingEcho))
                    {
                        yield return null;
                    }
                    continue;
                }
                // Several people at this device hand it over between them: in a game here when the setup asks for the
                // screen, online through the party's setting in the lobby (the base shows the curtain, or goes on at once).
                bool handOver = IsOnlineGame ? SeveralHere : IsHotSeat && LocalMatch != null && LocalMatch.HandOver;
                if (handOver && handedOverSeat != seat)
                {
                    // One hand-over per change of the person to act: a backgammon roll and its play are one.
                    handedOverSeat = seat;
                    bool ready = false;
                    string detail = L.F("{0}'s move", NameOf(seat));
                    if (IsOnlineGame)
                    {
                        online.HandOverSeat(seat, () => ready = true, detail);
                    }
                    else
                    {
                        HandOver(LocalMatch[seat], () => ready = true, detail);
                    }
                    while (!ready && Same(at))
                    {
                        yield return null;
                    }
                    if (!Same(at))
                    {
                        continue;
                    }
                }
                if (SeveralHere && Options.turnBoard)
                {
                    GameFrame(Setup.SideOf(seat), false);
                }
                StartClock();
                WaitingForHuman = true;
                GameTurnStarted(seat);
                UpdateHighlights();
                RaiseChanged();
                while (table != null && version == at)
                {
                    if (!IsGameRunning)
                    {
                        // A pause (the menu, the settings, the app in the background) keeps what the person has done of
                        // the turn, a partial backgammon play or a pawn about to be promoted; the board takes no clicks meanwhile.
                        WaitingForHuman = false;
                        while (table != null && version == at && !IsGameRunning)
                        {
                            yield return null;
                        }
                        if (table != null && version == at)
                        {
                            WaitingForHuman = true;
                            UpdateHighlights();
                            RaiseChanged();
                        }
                        continue;
                    }
                    yield return null;
                }
                WaitingForHuman = false;
                ClearSelection();
                UpdateHighlights();
            }
            director = null;
        }

        /// <summary>The computer's turn at this device: it thinks, a pause to watch, then its command through the same path as a click.</summary>
        private IEnumerator DirectComputer(int seat, int at)
        {
            RaiseChanged();
            StartClock();
            float started = Time.time;
            yield return Think(seat, at);
            if (!Same(at))
            {
                yield break;
            }
            BoardCommand command = thought;
            thought = null;
            if (command == null)
            {
                Debug.LogWarning($"Board Games: the computer in seat {seat} has nothing to do.");
                yield return null;
                yield break;
            }
            float pause = ThinkingPause(command) - (Time.time - started);
            if (pause > 0f)
            {
                yield return new WaitForSeconds(pause);
            }
            if (!Same(at))
            {
                yield break;
            }
            ApplyLocalCommand(command);
        }

        /// <summary>The computer's turn online: the host's client works the command out and sends it through the log.</summary>
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
                // The command sent for the computer is still on its way: it is waited for, not worked out a second time.
                while (Same(at) && online.AwaitingComputer)
                {
                    yield return null;
                }
                yield break;
            }
            float started = Time.time;
            yield return Think(seat, at);
            if (!Same(at))
            {
                yield break;
            }
            BoardCommand command = thought;
            thought = null;
            float pause = command != null ? ThinkingPause(command) - (Time.time - started) : 0f;
            if (pause > 0f)
            {
                yield return new WaitForSeconds(pause);
            }
            if (!Same(at) || host != online.IsHost)
            {
                yield break;
            }
            if (command != null)
            {
                online.PlayComputer(command);
            }
            float patience = Time.unscaledTime + ComputerMovePatience;
            while (Same(at) && host == online.IsHost && (Time.unscaledTime < patience || online.AwaitingComputer))
            {
                yield return null;
            }
        }

        /// <summary>
        /// The computer works out the command for <paramref name="seat"/> on a copy of the game, on a worker thread (on
        /// WebGL, which has none, at once), and leaves it in <see cref="thought"/>; it gives up when the table changes.
        /// </summary>
        private IEnumerator Think(int seat, int at)
        {
            thought = null;
            IBoardGame copy = table.Game.CloneGame();
            int level = table.LevelOf(seat);
            var thinking = new SystemRandom((int)(NewSeed() & int.MaxValue));
#if UNITY_WEBGL && !UNITY_EDITOR
            thought = BoardAI.Choose(copy, seat, level, thinking);
            yield break;
#else
            System.Threading.Tasks.Task<BoardCommand> task = System.Threading.Tasks.Task.Run(() => BoardAI.Choose(copy, seat, level, thinking));
            while (!task.IsCompleted)
            {
                if (!Same(at))
                {
                    yield break;
                }
                yield return null;
            }
            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception != null ? task.Exception.GetBaseException() : new System.Exception("the computer failed"));
                yield break;
            }
            thought = task.Result;
#endif
        }

        /// <summary>How long the computer seems to think: the setting, shorter for a roll of the dice.</summary>
        private float ThinkingPause(BoardCommand command)
        {
            float delay = Mathf.Max(0.05f, Options != null ? Options.thinkingDelay : 0.6f);
            return command != null && command.Kind == CommandKind.Roll ? Mathf.Max(0.05f, delay * 0.4f) : delay;
        }

        // ------------------------------------------------------------------ entries

        /// <summary>A command of a person or the computer at this device: the table takes it and the game reacts.</summary>
        public bool ApplyLocalCommand(BoardCommand command)
        {
            if (table == null || command == null || IsOnlineGame)
            {
                return false;
            }
            IBoardGame before = BeforeTakeBack(command);
            bool ok = table.Apply(command, NewSeed());
            if (ok && before != null)
            {
                takeBacks.Add(before);
            }
            AfterEntry(command, ok, LockstepEntryKind.Command, null);
            return ok;
        }

        /// <summary>
        /// The one place that reacts to an entry the table took, offline and online alike: the selection clears, the
        /// game shows a command of its own (<c>XEntry</c>), the interface hears of giving up, a time-out or a seat handed
        /// to the computer, a game at this device saves itself after a person's decision, and the interface redraws.
        /// The table has taken the entry (all the commands of a time-out) before the game hears of it.
        /// </summary>
        private void AfterEntry(BoardCommand command, bool accepted, LockstepEntryKind kind, IReadOnlyList<BoardCommand> made)
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
                    if (command.Kind == CommandKind.Resign)
                    {
                        ui?.Toast(L.F("{0} resigned.", NameOf(command.seat)));
                    }
                    else if (command.Kind != CommandKind.SeatToComputer)
                    {
                        GameEntry(command);
                        if (!IsOnlineGame && !table.IsComputer(command.seat) && !table.IsOver)
                        {
                            Autosave();
                        }
                    }
                    break;
                case LockstepEntryKind.Command:
                    if (command != null && (!IsOnlineGame || onlineSeats.Has(command.seat)))
                    {
                        sound?.Play(ClipKind.Click);
                    }
                    break;
                case LockstepEntryKind.Timeout when made != null && made.Count > 0:
                    foreach (BoardCommand entry in made)
                    {
                        if (entry != null && entry.Kind != CommandKind.Resign && entry.Kind != CommandKind.SeatToComputer)
                        {
                            GameEntry(entry);
                        }
                    }
                    // After the moves' own words (a check, a cube taken), so the player learns that the clock decided.
                    ui?.Toast(L.F("Time is up for {0}.", NameOf(made[0].seat)));
                    // A move the clock made is not one to take back.
                    takeBacks.Clear();
                    if (!IsOnlineGame && !table.IsOver)
                    {
                        Autosave();
                    }
                    break;
                case LockstepEntryKind.SeatToComputer when accepted:
                    SeatWentToComputer(command != null ? command.seat : -1);
                    break;
            }
            if (!table.IsOver)
            {
                clockTurn = -1;
            }
            else
            {
                WriteResult();
            }
            UpdateHighlights();
            RaiseChanged();
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

        /// <summary>A decision of a game at this device begins: the clock of the seat to act is full again (once per <see cref="IBoardGame.Turn"/>).</summary>
        private void StartClock()
        {
            if (table == null || IsOnlineGame || table.Setup.clockSeconds <= 0 || clockTurn == table.Game.Turn)
            {
                return;
            }
            clockTurn = table.Game.Turn;
            clockRemaining = table.Setup.clockSeconds;
        }

        /// <summary>Counts the time of the seat to act in a game at this device; at zero the table makes the default commands.</summary>
        private void TickClock(float deltaTime)
        {
            if (table == null || IsOnlineGame || table.IsOver || table.Setup.clockSeconds <= 0 || clockTurn != table.Game.Turn || !IsGameRunning)
            {
                return;
            }
            if (GameBusy() || IsHandingOver)
            {
                return;
            }
            clockRemaining -= deltaTime;
            if (clockRemaining > 0f)
            {
                return;
            }
            clockRemaining = 0f;
            List<BoardCommand> made = table.Timeout(NewSeed());
            AfterEntry(null, made.Count > 0, LockstepEntryKind.Timeout, made);
        }

        // ------------------------------------------------------------------ giving up

        /// <summary>
        /// The person at this device gives up: the one to act between two people here, online the seat the people here act
        /// for now (<see cref="LocalSeat"/>: the one to act when it is theirs, else the one seat here).
        /// </summary>
        public void ResignLocal()
        {
            if (table == null || table.IsOver || !IsGameRunning)
            {
                return;
            }
            int seat = IsOnlineGame ? LocalSeat : IsHotSeat ? table.SeatToAct : SingleHumanSeat();
            if (seat < 0 || table.IsComputer(seat))
            {
                return;
            }
            Commands?.Resign(seat);
        }

        // ------------------------------------------------------------------ ending

        /// <summary>The words of the result, in the language shown (read again when the language changes).</summary>
        private void WriteResult()
        {
            IBoardGame game = Current;
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
            else if (!SeveralHere && PlayedHere(winnerSeat))
            {
                // The one person at this device won; among several here the winner is named, as between two people.
                title = L.T("You win!");
            }
            else
            {
                title = L.F("{0} wins!", NameOf(winnerSeat));
            }
            string detail;
            switch (game.Reason)
            {
                case EndReason.Resigned:
                    detail = winnerSeat >= 0 ? L.F("{0} resigned.", NameOf(1 - winnerSeat)) : L.T("The game was given up.");
                    break;
                case EndReason.CalledOff:
                    detail = L.T("The game was called off.");
                    break;
                default:
                    detail = GameResultDetail(winnerSeat) ?? "";
                    break;
            }
            int bracket = IsOnlineGame ? BracketSeatOfDraw() : -1;
            if (bracket >= 0)
            {
                string onward = L.F("{0} goes on in the tournament.", NameOf(bracket));
                detail = string.IsNullOrEmpty(detail) ? onward : detail + "  " + onward;
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
            IBoardGame game = table.Game;
            int winnerSeat = table.WinnerSeat;
            bool localWon;
            // The seat that won, or that a drawn tournament match sends on in the bracket.
            int decided = winnerSeat;
            if (IsOnlineGame)
            {
                decided = ReportFinished(winnerSeat);
                // A seat played at this device won, or the room placed somebody here first when it called the game off.
                // The shared screen plays no seat: it celebrates whoever won; a draw or a called-off game is nobody's.
                localWon = onlineSeats.Any
                    ? PlayedHere(decided) || decided < 0 && game.Reason == EndReason.CalledOff && PlacedFirstHere()
                    : winnerSeat >= 0;
            }
            else
            {
                int human = SingleHumanSeat();
                if (human >= 0)
                {
                    bool won = winnerSeat == human;
                    Side side = Setup.SideOf(human);
                    BoardGamesLevel level = campaignLevel >= 0 && campaign != null ? campaign.Level(campaignLevel) : null;
                    if (level != null)
                    {
                        StarsEarned = BoardGamesCampaign.StarsFor(level, game, side);
                        if (won)
                        {
                            Progress.RecordLevel(campaignLevel, StarsEarned, BoardGamesCampaign.ScoreFor(game, side));
                        }
                    }
                    Progress.RecordGame(won, winnerSeat < 0);
                    PlayerScore = won ? BoardGamesCampaign.ScoreFor(game, side) : 0;
                    localWon = won;
                }
                else
                {
                    localWon = true;
                }
                ForgetAutosave();
            }
            sound?.Play(localWon && decided >= 0 ? ClipKind.Win : ClipKind.Lose);
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
