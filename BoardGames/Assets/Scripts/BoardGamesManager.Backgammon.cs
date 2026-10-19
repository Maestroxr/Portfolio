using System.Collections.Generic;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The backgammon half of the manager: the board with its checkers, dice and cube, the clicks that move the checkers
    /// one die at a time (each move shows on the board at once; the whole play is sent when it is complete), the HUD's
    /// Roll, Double, Take, Drop and Undo, the sounds, and the words of a backgammon result, seat line and turn. A checker
    /// is picked with a click (on its point or the bar) and moved with a click on a lit target; when it can go one place
    /// only it moves at once, and a second click on it moves it by the larger die. The shared code calls these hooks while
    /// the table (or the title) is backgammon.
    /// </summary>
    public partial class BoardGamesManager
    {
        [Header("Backgammon")]
        [SerializeField] private BackgammonBoard backgammonBoard;
        [SerializeField] private BackgammonHud backgammonHud;

        private readonly List<CheckerMove> bgDone = new List<CheckerMove>();
        private readonly BackgammonMarks bgMarks = new BackgammonMarks();
        private int bgSelected = -1;
        private int bgHovered = -1;
        /// <summary>The play a person here sent (the board shows it already), until the table takes it.</summary>
        private int[] bgSent;

        /// <summary>The backgammon board of the scene (the tours look at what it shows).</summary>
        public BackgammonBoard BackgammonView => backgammonBoard;

        /// <summary>The checkers the person to act here moved so far this turn, in order (not sent yet).</summary>
        public IReadOnlyList<CheckerMove> BackgammonMovesDone => bgDone;

        /// <summary>The place (0 to 23, 24 the bar) of the checker picked, or -1.</summary>
        public int BackgammonSelected => bgSelected;

        /// <summary>Whether the person to act here may roll now (the opening roll too).</summary>
        public bool BackgammonCanRoll => BackgammonWaiting(out BackgammonGame game)
            && (game.Phase == BackgammonPhase.Opening || game.Phase == BackgammonPhase.Rolling);

        /// <summary>Whether the person to act here may offer the cube now (before rolling, the cube theirs or in the middle).</summary>
        public bool BackgammonCanDouble => BackgammonWaiting(out BackgammonGame game)
            && game.Phase == BackgammonPhase.Rolling && game.CanDouble(game.SideToAct);

        /// <summary>Whether the person to act here answers an offered cube (Take or Drop).</summary>
        public bool BackgammonAnswering => BackgammonWaiting(out BackgammonGame game) && game.Phase == BackgammonPhase.Answering;

        /// <summary>Whether the person to act here has moved checkers this turn that may be taken back.</summary>
        public bool BackgammonCanUndo => BackgammonWaiting(out BackgammonGame game)
            && game.Phase == BackgammonPhase.Moving && bgDone.Count > 0 && bgSent == null;

        /// <summary>The HUD's line over the buttons: the cube offered ("{0} doubles to {1}."), the opening roll, or null.</summary>
        public string BackgammonOfferLine
        {
            get
            {
                if (!BackgammonWaiting(out BackgammonGame game))
                {
                    return null;
                }
                if (game.Phase == BackgammonPhase.Answering)
                {
                    return L.F("{0} doubles to {1}.", NameOf(1 - table.SeatToAct), Mathf.Max(1, game.cubeValue) * 2);
                }
                return game.Phase == BackgammonPhase.Opening ? L.T("Roll to decide who starts") : null;
            }
        }

        /// <summary>The person to act here rolls the dice (the HUD's Roll, the tours).</summary>
        public bool BackgammonRoll()
        {
            return BackgammonCanRoll && TrySend(BoardCommand.Roll(table.SeatToAct));
        }

        /// <summary>The person to act here offers the cube.</summary>
        public bool BackgammonDouble()
        {
            return BackgammonCanDouble && TrySend(BoardCommand.Double(table.SeatToAct));
        }

        /// <summary>The person to act here takes the cube offered.</summary>
        public bool BackgammonTake()
        {
            return BackgammonAnswering && TrySend(BoardCommand.Take(table.SeatToAct));
        }

        /// <summary>The person to act here refuses the cube offered (and loses the stake).</summary>
        public bool BackgammonDrop()
        {
            return BackgammonAnswering && TrySend(BoardCommand.Drop(table.SeatToAct));
        }

        /// <summary>Takes back the last checker moved this turn; the board shows it flying back.</summary>
        public bool BackgammonUndo()
        {
            BackgammonGame game = BackgammonGame;
            if (!BackgammonCanUndo)
            {
                return false;
            }
            bgDone.RemoveAt(bgDone.Count - 1);
            bgSelected = -1;
            backgammonBoard?.ShowPreview(game, bgDone);
            sound?.Play(ClipKind.Click);
            UpdateHighlights();
            RaiseChanged();
            return true;
        }

        private bool BackgammonWaiting(out BackgammonGame game)
        {
            game = BackgammonGame;
            return game != null && WaitingForHuman && !table.IsOver;
        }

        /// <summary>Whether <paramref name="seat"/> is played by a person at this device (online, one of the seats played here).</summary>
        private bool BackgammonPersonHere(int seat)
        {
            return PlayedHere(seat);
        }

        // ------------------------------------------------------------------ the hooks

        private void BackgammonBegin()
        {
            BackgammonGame game = BackgammonGame;
            BackgammonForget();
            if (game == null || backgammonBoard == null)
            {
                return;
            }
            backgammonBoard.Sound = PlayBackgammonSound;
            backgammonBoard.SetVisible(true);
            BackgammonFrame(HomeSide(), true);
            backgammonBoard.Show(game);
        }

        private void BackgammonEnd()
        {
            BackgammonForget();
            if (backgammonBoard != null)
            {
                backgammonBoard.SetMarks(null);
                backgammonBoard.StopAnimations();
                backgammonBoard.SetVisible(false);
            }
            backgammonHud?.Refresh();
        }

        /// <summary>The title: the starting position behind the menu, the cube in the middle when the quick game plays with it.</summary>
        private void BackgammonShowIdle()
        {
            if (backgammonBoard == null)
            {
                return;
            }
            backgammonBoard.Sound = PlayBackgammonSound;
            backgammonBoard.SetVisible(true);
            backgammonBoard.SetMarks(null);
            BackgammonFrame(Side.Light, true);
            backgammonBoard.Show(BackgammonGame.NewGame(Options == null || Options.doublingCube, true, Side.Light));
            cameraRig?.SetTitle(true, false);
        }

        private void BackgammonFrame(Side bottom, bool immediate)
        {
            cameraRig?.FrameArea(BackgammonLayout.HalfWidth, BackgammonLayout.HalfDepth, bottom, immediate);
            backgammonBoard?.SetBottom(bottom);
        }

        private int BackgammonPick(Ray ray)
        {
            return backgammonBoard != null && backgammonBoard.isActiveAndEnabled ? backgammonBoard.TargetAt(ray) : -1;
        }

        /// <summary>
        /// A click on <paramref name="target"/> while the person here plays the dice: moves the picked checker there when it
        /// can go there (with one die or several), else picks the checker clicked (moving it at once when it has one place to
        /// go, by the larger die on a second click), else lets the pick go.
        /// </summary>
        private void BackgammonClicked(int target)
        {
            BackgammonGame game = BackgammonGame;
            if (game == null || game.Phase != BackgammonPhase.Moving || bgSent != null)
            {
                return;
            }
            IReadOnlyList<CheckerMove> next = game.NextMoves(bgDone);
            if (next.Count == 0)
            {
                BackgammonSend(game);
                return;
            }
            if (bgSelected >= 0 && target != bgSelected)
            {
                List<CheckerMove> route = BackgammonRoute(game, bgSelected, target);
                if (route != null)
                {
                    BackgammonMove(game, route);
                    return;
                }
            }
            if (target >= 0 && BgStartsFrom(next, target))
            {
                if (target == bgSelected)
                {
                    BackgammonMove(game, new List<CheckerMove> { BgLarger(next, target) });
                    return;
                }
                List<KeyValuePair<int, List<CheckerMove>>> routes = BackgammonRoutes(game, target);
                if (routes.Count == 1)
                {
                    BackgammonMove(game, routes[0].Value);
                    return;
                }
                bgSelected = target;
                sound?.Play(ClipKind.Click);
                UpdateHighlights();
                RaiseChanged();
                return;
            }
            Side side = game.SideToAct;
            if (target >= 0 && target < 24 && game.board != null && game.bar != null && game.bar[(int)side] > 0
                && (side == Side.Light ? game.board[target] > 0 : game.board[target] < 0))
            {
                ui?.Toast(L.T("Enter from the bar first."));
            }
            if (bgSelected >= 0)
            {
                bgSelected = -1;
                UpdateHighlights();
                RaiseChanged();
            }
        }

        private void BackgammonHovered(int target)
        {
            if (bgHovered == target)
            {
                return;
            }
            bgHovered = target;
            if (bgSelected >= 0)
            {
                UpdateHighlights();
            }
        }

        private void BackgammonTurnStarted(int seat)
        {
            if (bgSent != null)
            {
                // A play that was sent never landed (refused, failed, or its echo never came): the checkers fly back.
                backgammonBoard?.CancelPreview();
            }
            bgDone.Clear();
            bgSelected = -1;
            bgHovered = -1;
            bgSent = null;
        }

        /// <summary>The table took a backgammon command from any seat: the dice are thrown, the play flies, the cube turns.</summary>
        private void BackgammonEntry(BoardCommand command)
        {
            BackgammonGame game = BackgammonGame;
            if (game == null || command == null || backgammonBoard == null)
            {
                return;
            }
            switch (command.Kind)
            {
                case CommandKind.Roll:
                    backgammonBoard.Roll(game);
                    if (table.LastCommand == command && game.Phase != BackgammonPhase.Moving && !game.IsOver)
                    {
                        // The roll allowed no move: the turn passed at once.
                        ui?.Toast(L.F("{0} cannot move.", NameOf(command.seat)));
                    }
                    break;
                case CommandKind.Move:
                    if (bgSent != null && BgSamePath(command.path, bgSent))
                    {
                        backgammonBoard.CommitPreview(game);
                    }
                    else
                    {
                        backgammonBoard.AnimatePlay(game);
                    }
                    bgSent = null;
                    break;
                case CommandKind.Double:
                    backgammonBoard.ShowCube(game, true);
                    if (!BackgammonPersonHere(table.SeatToAct))
                    {
                        // A person answering here reads the offer over the Take and Drop buttons.
                        ui?.Toast(L.F("{0} doubles to {1}.", NameOf(command.seat), Mathf.Max(1, game.cubeValue) * 2));
                    }
                    break;
                case CommandKind.Take:
                    backgammonBoard.ShowCube(game, true);
                    ui?.Toast(L.F("{0} takes.", NameOf(command.seat)));
                    break;
                case CommandKind.Drop:
                    backgammonBoard.ShowCube(game, true);
                    break;
            }
        }

        /// <summary>The pick goes; a play in progress that was not sent flies back (the director stopped waiting, or an entry came).</summary>
        private void BackgammonClearSelection()
        {
            bgSelected = -1;
            if (bgDone.Count == 0)
            {
                return;
            }
            bgDone.Clear();
            if (bgSent == null)
            {
                backgammonBoard?.CancelPreview();
            }
        }

        private void BackgammonHighlights()
        {
            if (backgammonBoard == null)
            {
                return;
            }
            BackgammonGame game = BackgammonGame;
            bgMarks.Clear();
            if (game == null)
            {
                backgammonBoard.SetMarks(null);
                return;
            }
            if (game.lastPlay != null && game.lastSide >= 0 && bgDone.Count == 0 && bgSent == null)
            {
                bgMarks.lastSide = game.lastSide == (int)Side.Dark ? Side.Dark : Side.Light;
                for (int i = 0; i + 1 < game.lastPlay.Length; i += 2)
                {
                    bgMarks.lastFrom.Add(game.lastPlay[i]);
                    bgMarks.lastTo.Add(game.lastPlay[i + 1]);
                }
            }
            // The dice of the turn stay bright until they are played; after the play (or a roll that could not be played) they dim.
            if (game.Phase == BackgammonPhase.Moving)
            {
                bgMarks.diceLeft = bgSent != null ? new int[0] : game.DiceLeft(bgDone);
            }
            else if (game.Phase != BackgammonPhase.Opening && game.die1 > 0)
            {
                bgMarks.diceLeft = new int[0];
            }
            bool showMoves = Options == null || Options.showMoves;
            if (WaitingForHuman && game.Phase == BackgammonPhase.Moving && bgSent == null)
            {
                bgMarks.side = game.SideToAct;
                if (bgSelected >= 0)
                {
                    bgMarks.selected = bgSelected;
                    bgMarks.hovered = bgHovered;
                    if (showMoves)
                    {
                        foreach (KeyValuePair<int, List<CheckerMove>> route in BackgammonRoutes(game, bgSelected))
                        {
                            bgMarks.targets.Add(route.Key);
                        }
                    }
                    // The hint's target is marked whatever the setting, and lit unless the pointer is on a target of its own.
                    int hinted = HintTargetFor(bgSelected);
                    if (hinted >= 0)
                    {
                        if (!bgMarks.targets.Contains(hinted))
                        {
                            bgMarks.targets.Add(hinted);
                        }
                        if (!bgMarks.targets.Contains(bgHovered))
                        {
                            bgMarks.hovered = hinted;
                        }
                    }
                }
                else if (showMoves)
                {
                    foreach (CheckerMove move in game.NextMoves(bgDone))
                    {
                        if (!bgMarks.movable.Contains(move.from))
                        {
                            bgMarks.movable.Add(move.from);
                        }
                    }
                }
            }
            backgammonBoard.SetMarks(bgMarks);
        }

        private bool BackgammonBusy()
        {
            return backgammonBoard != null && backgammonBoard.IsAnimating;
        }

        private void BackgammonApplyLook(BoardGamesTheme theme)
        {
            backgammonBoard?.ApplyTheme(theme);
        }

        /// <summary>"Single game." / "Gammon!" / "Backgammon!", or who refused the double, with the points won.</summary>
        private string BackgammonResultDetail(int winnerSeat)
        {
            BackgammonGame game = BackgammonGame;
            if (game == null || winnerSeat < 0)
            {
                return "";
            }
            string points = game.Points == 1 ? L.T("1 point") : L.F("{0} points", game.Points);
            switch (game.Reason)
            {
                case EndReason.DoubleDeclined:
                    return L.F("{0} refused the double.", NameOf(1 - winnerSeat)) + " " + points;
                case EndReason.BorneOff:
                {
                    int times = game.ResultMultiplier;
                    string words = times >= 3 ? L.T("Backgammon!") : times == 2 ? L.T("Gammon!") : L.T("Single game.");
                    return words + " " + points;
                }
                default:
                    return points;
            }
        }

        /// <summary>The pips the seat still has to travel and how many of its checkers are off.</summary>
        private string BackgammonSeatLine(int seat)
        {
            BackgammonGame game = BackgammonGame;
            TableSetup setup = Setup;
            if (game == null || setup == null)
            {
                return "";
            }
            Side side = setup.SideOf(seat);
            int off = game.off != null && game.off.Length > (int)side ? game.off[(int)side] : 0;
            return L.F("Pips {0}", game.PipCount(side)) + " \u00B7 " + L.F("Off {0}", off);
        }

        /// <summary>The opening roll, a roll, an answer to the cube in backgammon's words; null for a play (the shared words).</summary>
        private string BackgammonTurnWords(int seat)
        {
            BackgammonGame game = BackgammonGame;
            if (game == null || table == null || table.IsComputer(seat))
            {
                return null;
            }
            // The one person at this device is asked to roll; one of several here, or somebody elsewhere, is named.
            bool here = PlayedHere(seat);
            bool alone = here && !SeveralHere;
            switch (game.Phase)
            {
                case BackgammonPhase.Opening:
                    // The HUD says what the opening roll is for; the line over the board only asks for the roll.
                case BackgammonPhase.Rolling:
                    return alone ? L.T("Roll the dice") : L.F("{0}'s roll", NameOf(seat));
                case BackgammonPhase.Answering:
                    return here ? L.F("{0} doubles: take or drop?", NameOf(1 - seat)) : null;
                default:
                    return null;
            }
        }

        // ------------------------------------------------------------------ moving checkers

        /// <summary>Forgets the input state of a turn (a new table, the end of one).</summary>
        private void BackgammonForget()
        {
            bgDone.Clear();
            bgSelected = -1;
            bgHovered = -1;
            bgSent = null;
        }

        private void PlayBackgammonSound(ClipKind kind, float volume)
        {
            sound?.Play(kind, volume);
        }

        /// <summary>
        /// Every place the checker at <paramref name="from"/> may reach this turn after the moves already made, with the
        /// moves that take it there (one die, or several in a row for the same checker), nearest first; at most one route
        /// per place (the one with the fewest dice, the smallest die for bearing off).
        /// </summary>
        private List<KeyValuePair<int, List<CheckerMove>>> BackgammonRoutes(BackgammonGame game, int from)
        {
            var found = new List<KeyValuePair<int, List<CheckerMove>>>();
            var open = new Queue<List<CheckerMove>>();
            open.Enqueue(new List<CheckerMove>());
            var done = new List<CheckerMove>(bgDone.Count + 4);
            while (open.Count > 0)
            {
                List<CheckerMove> route = open.Dequeue();
                int at = route.Count > 0 ? route[route.Count - 1].to : from;
                done.Clear();
                done.AddRange(bgDone);
                done.AddRange(route);
                var moves = new List<CheckerMove>();
                foreach (CheckerMove move in game.NextMoves(done))
                {
                    if (move.from == at)
                    {
                        moves.Add(move);
                    }
                }
                moves.Sort((a, b) => a.die.CompareTo(b.die));
                foreach (CheckerMove move in moves)
                {
                    var longer = new List<CheckerMove>(route) { move };
                    if (!BgReached(found, move.to))
                    {
                        found.Add(new KeyValuePair<int, List<CheckerMove>>(move.to, longer));
                    }
                    if (move.to != BackgammonGame.Off && longer.Count < 4)
                    {
                        open.Enqueue(longer);
                    }
                }
            }
            return found;
        }

        /// <summary>The moves that take the checker at <paramref name="from"/> to <paramref name="to"/>, or null when it cannot get there.</summary>
        private List<CheckerMove> BackgammonRoute(BackgammonGame game, int from, int to)
        {
            if (to < 0)
            {
                return null;
            }
            foreach (KeyValuePair<int, List<CheckerMove>> route in BackgammonRoutes(game, from))
            {
                if (route.Key == to)
                {
                    return route.Value;
                }
            }
            return null;
        }

        /// <summary>Makes <paramref name="route"/> on the board one move at a time; a whole play goes to the table at once.</summary>
        private void BackgammonMove(BackgammonGame game, List<CheckerMove> route)
        {
            foreach (CheckerMove move in route)
            {
                bgDone.Add(move);
                backgammonBoard?.ShowPreview(game, bgDone);
            }
            bgSelected = -1;
            if (game.IsCompletePlay(bgDone) || game.NextMoves(bgDone).Count == 0)
            {
                BackgammonSend(game);
                return;
            }
            UpdateHighlights();
            RaiseChanged();
        }

        /// <summary>Sends the play made so far; when the table refuses it the checkers fly back.</summary>
        private void BackgammonSend(BackgammonGame game)
        {
            int seat = table.SeatToAct;
            bgSent = new BackgammonPlay(bgDone).Path();
            if (TrySend(BoardCommand.Play(seat, bgSent)))
            {
                return;
            }
            bgSent = null;
            bgDone.Clear();
            bgSelected = -1;
            backgammonBoard?.CancelPreview();
            UpdateHighlights();
            RaiseChanged();
        }

        private static bool BgReached(List<KeyValuePair<int, List<CheckerMove>>> found, int place)
        {
            foreach (KeyValuePair<int, List<CheckerMove>> entry in found)
            {
                if (entry.Key == place)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool BgStartsFrom(IReadOnlyList<CheckerMove> moves, int place)
        {
            foreach (CheckerMove move in moves)
            {
                if (move.from == place)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>The move from <paramref name="place"/> by the largest die.</summary>
        private static CheckerMove BgLarger(IReadOnlyList<CheckerMove> moves, int place)
        {
            CheckerMove best = default;
            bool any = false;
            foreach (CheckerMove move in moves)
            {
                if (move.from == place && (!any || move.die > best.die))
                {
                    best = move;
                    any = true;
                }
            }
            return best;
        }

        private static bool BgSamePath(int[] a, int[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
