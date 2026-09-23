using System.Collections;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The online half of the manager: a match played in step with the other players of a room
    /// (<see cref="MonopolyOnlineController"/>, Server/Lib.cs). The match is a <see cref="LockstepMatch"/> that nothing
    /// but the action log of the server changes: the controller takes every action to it and tells the manager
    /// (<see cref="ILockstepHost{TCommand}"/>). What arrives is applied at once, so the rules engine may run ahead of
    /// the board, which plays the events as it does in a game at one device. The director offers a decision only to the
    /// player at this device, and only once the board caught up; for everybody else it shows who the table waits for.
    /// The computer players are moved by the client of the host, through the log like everybody else. There is no
    /// saving and no pausing, and the results lead back to the room.
    /// </summary>
    public partial class MonopolyGameManager : ILockstepHost<MatchCommand>
    {
        /// <summary>What the online controller hands over before a match of the room starts: the table as the server seated it.</summary>
        internal sealed class OnlineTable
        {
            /// <summary>The seats in the order of play; none of them is off.</summary>
            public MatchSetup Setup;
            /// <summary>The seat of the player at this device.</summary>
            public int LocalSeat;
            /// <summary>The seed of the room: the decks are shuffled and the deeds dealt from it.</summary>
            public uint Seed;
            public int FirstPlayer;
        }

        /// <summary>How much faster the board plays while the match is ahead of it.</summary>
        private const float CatchUpPace = 1.6f;
        /// <summary>Seconds the host waits for a move it sent for a computer player before it thinks again.</summary>
        private const float ComputerMovePatience = 4f;

        [SerializeField] private MonopolyOnlineController online;

        private OnlineTable onlineTable;
        private LockstepMatch lockstep;
        private int localSeat = -1;
        /// <summary>Counts what arrived from the server: the director looks at the table again when it changes.</summary>
        private int onlineVersion;
        private int reportedTurn = -1;
        private int reportedWorth = -1;

        public MonopolyOnlineController Online => online;

        /// <summary>Whether the match is played online, in step with the other players of a room.</summary>
        public bool IsOnlineMatch => lockstep != null;

        /// <summary>The seat of the player at this device in an online match, else -1.</summary>
        public int LocalSeat => localSeat;

        internal LockstepMatch Lockstep => lockstep;

        /// <summary>Where the commands of the interface go: to the server in an online match, else straight to the rules engine.</summary>
        public IMonopolyCommands Commands => lockstep != null && online != null ? (IMonopolyCommands)online : controller;

        /// <summary>The board of an online match plays faster while the match is ahead of it.</summary>
        private float OnlinePace => lockstep != null && Match != null && Match.HasEvents ? CatchUpPace : 1f;

        /// <summary>Whether somebody at this device plays <paramref name="player"/>: no computer player, and online nobody elsewhere.</summary>
        private bool PlayedHere(PlayerState player)
        {
            return !player.bot && (lockstep == null || player.index == localSeat);
        }

        protected override IOnlineLobby OnlineLobby => online;

        // ------------------------------------------------------------------ starting and ending

        /// <summary>The online controller hands over the table of the match that is about to start.</summary>
        internal void PrepareOnlineMatch(OnlineTable table)
        {
            onlineTable = table;
        }

        /// <summary>Seats the table the server set up and starts the match from the seed of the room, as every other client does.</summary>
        private void StartOnlineMatch()
        {
            OnlineTable table = onlineTable;
            onlineTable = null;
            if (table == null || table.Setup == null || table.Setup.seats.Count < 2)
            {
                UI?.UpdateError("The table of the online match did not arrive.");
                return;
            }
            var started = new LockstepMatch(matchSettings.Board.CreateLayout(), matchSettings.Rules, table.Seed);
            for (int i = 0; i < table.Setup.seats.Count; i++)
            {
                SeatSetup seat = table.Setup.seats[i];
                PlayerState player = started.Match.AddPlayer(table.Setup.PlayingName(i), seat.token, seat.kind == SeatKind.Computer, seat.level);
                player.color = i;
            }
            started.Match.Start(table.FirstPlayer);
            lockstep = started;
            localSeat = table.LocalSeat;
            onlineVersion = 0;
            reportedTurn = -1;
            reportedWorth = -1;
            BeginDirecting(lockstep.Match, fresh: true);
            ui.Panel(localSeat)?.MarkAsLocal();
            for (int i = 0; i < tokens.Count && i < Match.players.Count; i++)
            {
                PlayerState player = Match.players[i];
                tokens[i].Assign(i, player.bot ? PlayerControl.Computer : i == localSeat ? PlayerControl.Local : PlayerControl.Remote, player.name);
            }
        }

        /// <summary>The session is over (the results were left, the match or the room was, the connection dropped): the table is cleared.</summary>
        protected override void OnSessionChanged()
        {
            if (InSession || (lockstep == null && onlineTable == null))
            {
                return;
            }
            StopDirecting();
            lockstep = null;
            onlineTable = null;
            localSeat = -1;
            Match = null;
            ui.CloseAll();
            board.ClearHighlights();
            foreach (MonopolyPlayer token in tokens)
            {
                token.Assign(-1, PlayerControl.Local);
            }
            ShowIdleBoard();
        }

        /// <summary>The results of an online match lead back to the room, where the host starts the next one.</summary>
        private void ReturnToRoom()
        {
            RequestState(BaseGameState.Initialization);
        }

        // ------------------------------------------------------------------ what the server sends

        LockstepTable<MatchCommand> ILockstepHost<MatchCommand>.Table => lockstep;

        int ILockstepHost<MatchCommand>.TableSeat => localSeat;

        string ILockstepHost<MatchCommand>.TurnCaption => OnlineCaption();

        /// <summary>
        /// An entry of the log was taken: the match has it already, in the order of the log. The board follows through the
        /// events of the match; what they do not tell is told here.
        /// </summary>
        void ILockstepHost<MatchCommand>.EntryTaken(LockstepEntry<MatchCommand> entry)
        {
            if (lockstep == null || Match == null)
            {
                return;
            }
            switch (entry.Kind)
            {
                case LockstepEntryKind.Timeout when entry.Made.Count > 0:
                    // The default moves are made for the seat the table waited for.
                    TimeRanOut(entry.Made[0].seat);
                    break;
                case LockstepEntryKind.SeatToComputer when entry.Accepted:
                    SeatWentToComputer(entry.Seat);
                    break;
                case LockstepEntryKind.Command when entry.Accepted:
                    CommandArrived(entry.Command);
                    break;
            }
            if (lockstep.AnsweredOffer != null)
            {
                OfferAnswered(lockstep.AnsweredOffer, lockstep.AnswerAccepted);
            }
            onlineVersion++;
            ReportToRoom();
        }

        /// <summary>A player left the table: the computer plays the seat on.</summary>
        private void SeatWentToComputer(int seat)
        {
            if (seat < 0 || seat >= Match.players.Count)
            {
                return;
            }
            PlayerState player = Match.players[seat];
            if (seat < tokens.Count)
            {
                tokens[seat].Assign(seat, PlayerControl.Computer, player.name);
            }
            ui.Toast($"{Named(seat)} left the table. The computer plays on.", MonopolyStyle.Ink, Icons.Robot);
            ui.RefreshPlayers(Match);
        }

        /// <summary>What the board cannot tell from the events of the match: an offer is on the table.</summary>
        private void CommandArrived(MatchCommand command)
        {
            if (command.kind == CommandKind.ProposeTrade)
            {
                sound?.Play(Sfx.Trade);
                ui.Toast($"{Named(command.offer.from)} {Verb(command.offer.from, "offers")} {NamedObject(command.offer.to)} a trade.", MonopolyStyle.Blue, Icons.Handshake);
            }
        }

        private void OfferAnswered(TradeOffer offer, bool accepted)
        {
            ui.Offer.Close();
            if (accepted)
            {
                ui.Banner("DEAL!", MonopolyStyle.Green, 0.9f);
                return;
            }
            PlayerState from = Match.players[offer.from];
            PlayerState to = Match.players[offer.to];
            if (from.bot && online.IsHost)
            {
                bots.Refused(Match, offer);
            }
            ui.Toast($"{MonopolyStyle.Named(to)} turned down {MonopolyStyle.NamedPossessive(from, false)} offer.", MonopolyStyle.Muted, Icons.Handshake);
            sound?.Play(Sfx.Error, 0.6f);
        }

        /// <summary>The clock of the room ran out and the default move was made for <paramref name="seat"/>.</summary>
        private void TimeRanOut(int seat)
        {
            if (seat < 0 || seat >= Match.players.Count)
            {
                return;
            }
            ui.Toast($"Time is up for {NamedObject(seat)}.", MonopolyStyle.Red, Icons.Clock);
            if (seat != localSeat)
            {
                return;
            }
            // The choices on screen are no longer the player's to make.
            ui.Banner("TIME'S UP!", MonopolyStyle.Red, 0.8f);
            sound?.Play(Sfx.Error, 0.7f);
            ui.Manage.Close();
            ui.Trade.Close();
            board.ClearHighlights();
        }

        /// <summary>
        /// The room learns what the player at this device is worth as the turns pass (the score the members are ranked
        /// by should the room end early), and that they are done once the match is over.
        /// </summary>
        private void ReportToRoom()
        {
            int worth = localSeat >= 0 && localSeat < Match.players.Count ? Match.NetWorth(localSeat) : 0;
            if (Match.IsOver)
            {
                // The room gives the members the places of their seats, a computer player that won before them all.
                int place = localSeat >= 0 && localSeat < Match.players.Count ? Match.players[localSeat].place : 0;
                online.ReportTableFinished(Match.winner, place, worth);
            }
            else if (Match.turn != reportedTurn && worth != reportedWorth)
            {
                reportedTurn = Match.turn;
                reportedWorth = worth;
                online.ReportScore(worth);
            }
        }

        /// <summary>
        /// The room finished. When the match has not (too few players are left, or the host ended the game), it ends
        /// here as it does on every other client: all of them took the same actions, so they rank the players the same.
        /// </summary>
        void ILockstepHost<MatchCommand>.RoomFinished()
        {
            if (lockstep == null || Match == null || Match.IsOver)
            {
                return;
            }
            ui.Toast("The match was called off: the richest player wins.", MonopolyStyle.Ink, Icons.Flag);
            Match.Resign();
            onlineVersion++;
        }

        /// <summary>A command of the player at this device did not reach the log: the choice is theirs again.</summary>
        void ILockstepHost<MatchCommand>.CommandFailed()
        {
            sound?.Play(Sfx.Error, 0.7f);
            onlineVersion++;
        }

        /// <summary>Who the table waits for, for the clock of the lobby.</summary>
        private string OnlineCaption()
        {
            if (lockstep == null || Match == null)
            {
                return "";
            }
            int seat = lockstep.Waiting;
            if (seat < 0)
            {
                return "Game over";
            }
            bool answer = lockstep.PendingOffer != null;
            string caption = seat == localSeat ? answer ? "Your answer" : "Your move"
                : answer ? $"{Match.players[seat].name} answers" : $"{Match.players[seat].name}'s move";
            // The banner has room for a short line; long names get smaller letters.
            return caption.Length <= 17 ? caption : caption.Length <= 22 ? $"<size=80%>{caption}</size>" : $"<size=62%>{caption}</size>";
        }

        // ------------------------------------------------------------------ what the player may do

        /// <summary>Whether the player at this device sits at <paramref name="seat"/> and the director waits for their decision.</summary>
        internal bool OnlineDeciding(int seat)
        {
            return lockstep != null && IsGameRunning && WaitingForHuman && seat == localSeat && lockstep.PendingOffer == null
                && Match.Decider == seat && !Match.HasEvents;
        }

        /// <summary>Whether the player at this device sits at <paramref name="seat"/> and may build, sell, mortgage or trade now.</summary>
        internal bool OnlineManaging(int seat)
        {
            return lockstep != null && IsGameRunning && seat == localSeat && lockstep.CanManage(seat) && !Match.HasEvents;
        }

        /// <summary>Whether the offer on the table is for the player at this device to answer.</summary>
        internal bool OnlineAnswering(int seat)
        {
            return lockstep != null && IsGameRunning && seat == localSeat && lockstep.PendingOffer != null && lockstep.PendingOffer.to == seat;
        }

        // ------------------------------------------------------------------ directing

        private IEnumerator DirectOnline()
        {
            yield return null;
            while (Match != null && lockstep != null)
            {
                if (!IsGameRunning)
                {
                    yield return null;
                    continue;
                }
                List<MatchEvent> events = Match.TakeEvents();
                if (events.Count > 0)
                {
                    WaitingForHuman = false;
                    ui.Actions.Hide();
                    foreach (MatchEvent e in events)
                    {
                        yield return Play(e);
                    }
                    // Actions that arrived meanwhile moved the match on: the board follows it once their events played.
                    if (Match != null && !Match.HasEvents)
                    {
                        SyncBoard(false);
                    }
                    continue;
                }
                if (Match.IsOver)
                {
                    yield return Finish();
                    director = null;
                    yield break;
                }
                int seat = lockstep.Waiting;
                if (seat < 0)
                {
                    yield return null;
                    continue;
                }
                PlayerState player = Match.players[seat];
                int version = onlineVersion;
                bool host = online.IsHost;
                bool sending = online.AwaitingEcho;
                float patience = float.PositiveInfinity;
                if (seat != localSeat)
                {
                    // Popups of a turn that is over (the clock ended it) do not stay for somebody else's.
                    ui.Manage.Close();
                    ui.Trade.Close();
                }

                if (lockstep.PendingOffer != null)
                {
                    TradeOffer offer = lockstep.PendingOffer;
                    PlayerState from = Match.players[offer.from];
                    if (seat == localSeat && !player.bot && !sending)
                    {
                        ui.Actions.Hide();
                        ui.Offer.Show(Match, offer, ui.TokenSprite(from.token), accept => online.AnswerTrade(seat, accept));
                        WaitingForHuman = true;
                    }
                    else
                    {
                        ui.Actions.Show(player.name, "is considering the offer...", MonopolyStyle.PlayerColor(player.color), ui.TokenSprite(player.token), null, true);
                        if (player.bot && host && online.AwaitingComputer)
                        {
                            // The answer sent for the computer is still on its way: it is waited for, not given twice.
                            patience = Time.unscaledTime;
                        }
                        else if (player.bot && host)
                        {
                            yield return new WaitForSeconds(Beat(0.9f));
                            if (!SameTable(version))
                            {
                                continue;
                            }
                            online.PlayComputer(MatchCommand.Answer(seat, bots.WouldAccept(Match, offer)));
                            patience = Time.unscaledTime + ComputerMovePatience;
                        }
                    }
                }
                else if (player.bot)
                {
                    ShowBotTurn(player);
                    if (host && online.AwaitingComputer)
                    {
                        // The move sent for the computer is still on its way (what arrived meanwhile was somebody
                        // managing property): it is waited for, not worked out a second time.
                        patience = Time.unscaledTime;
                    }
                    else if (host)
                    {
                        yield return new WaitForSeconds(Beat(matchSettings.BotThinkTime));
                        if (!SameTable(version))
                        {
                            continue;
                        }
                        MatchCommand move = null;
                        if (Match.phase == MatchPhase.Roll && tradeCheckedTurn != Match.turn)
                        {
                            tradeCheckedTurn = Match.turn;
                            TradeOffer offer = bots.ProposeTrade(Match, seat);
                            move = offer != null ? MatchCommand.Propose(offer) : null;
                        }
                        move ??= bots.Decide(Match);
                        if (move == null)
                        {
                            Debug.LogWarning($"Monopoly: the computer player {player.name} has no move in {Match.phase}.");
                        }
                        else
                        {
                            online.PlayComputer(move);
                        }
                        patience = Time.unscaledTime + ComputerMovePatience;
                    }
                }
                else if (seat != localSeat)
                {
                    ShowBotTurn(player);
                }
                else if (sending)
                {
                    // The choice is on its way to the server; what it did shows when it comes back.
                    ui.Actions.Hide();
                }
                else
                {
                    ShowDecision(player);
                    WaitingForHuman = true;
                }

                // A move of the host for a computer player that is still on its way is not worked out again.
                while (SameTable(version) && host == online.IsHost && sending == online.AwaitingEcho &&
                       (Time.unscaledTime < patience || online.AwaitingComputer))
                {
                    yield return null;
                }
                WaitingForHuman = false;
            }
            director = null;
        }

        /// <summary>Whether nothing arrived since the director looked at the table at <paramref name="version"/>.</summary>
        private bool SameTable(int version)
        {
            return Match != null && lockstep != null && version == onlineVersion && !Match.HasEvents && IsGameRunning;
        }
    }
}
