using System;
using System.Collections;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Development players only. Started with <c>-monopoly-online &lt;role&gt; &lt;folder&gt; [name]</c>, it plays an online
    /// match by itself and saves screenshots and a log (named after the role, or the name) into the folder, then quits
    /// (<see cref="OnlineTour"/>). The <c>host</c> opens a room (Tycoon Rush, which ends after twenty rounds, one computer
    /// player, a short clock) and starts it once somebody joined and is ready; <c>join</c> joins the first room it sees.
    /// Both play their seats with a simple autopilot, and a guest leaves its first decisions to the clock of the room.
    /// <c>joinquits</c> and <c>hostquits</c> leave the match after a while (the latter waits for two guests first): the
    /// computer plays their seats on and the match goes on; when the host left, the room passes to a guest, who calls the
    /// match off a little later. Every action of the log is written down with the checksum of the match after it, so the
    /// logs of the players show whether they stayed in step. Run the players with <c>-gamebox-identity</c> to tell them
    /// apart, and <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server. Without the argument it does
    /// nothing.
    /// </summary>
    public class MonopolyOnlineTour : OnlineTour
    {
        private const string Argument = "-monopoly-online";
        /// <summary>The mode the room plays: Tycoon Rush ends after twenty rounds.</summary>
        private const int Mode = 4;
        /// <summary>Minutes after which the host calls the match off, should it not be over by then.</summary>
        private const float Patience = 16f;
        /// <summary>Seconds a quitter plays before leaving, and a guest who inherited the room before calling the match off.</summary>
        private const float QuitAfter = 50f;
        private const float CallOffAfter = 70f;

        private MonopolyGameManager manager;
        private bool piloting;
        private int sleepy;
        private bool proposed;
        private bool timeoutShot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<MonopolyOnlineTour>(Argument);
        }

        private bool Quits => Role.EndsWith("quits", StringComparison.Ordinal);

        private MonopolyMatch Match => manager.Match;

        private bool Over => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        private IEnumerator Start()
        {
            OpenLog(true);
            while ((manager = FindAnyObjectByType<MonopolyGameManager>()) == null)
            {
                yield return null;
            }
            yield return new WaitForSeconds(2f);
            if (manager.Online == null)
            {
                yield return Fail("the scene has no online controller");
                yield break;
            }
            yield return Shot("00_title");
            yield return LogIn(manager.Online);
            if (!LoggedIn)
            {
                yield return Fail("no login");
                yield break;
            }
            // After the controller, so the match took an action by the time it is written down here.
            LogActions(kind => ((CommandKind)kind).ToString(), () => manager.Lockstep != null
                ? $"applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8} cash {string.Join("/", manager.Lockstep.Match.players.Select(p => p.cash))}"
                : "no match");
            Server.ActionReceived += ShootTimeout;
            yield return new WaitForSeconds(0.5f);

            if (Hosting)
            {
                int guests = Quits ? 2 : 1;
                string options = RoomOptions.Write(RoomOptions.TurnOption, 20, RoomOptions.ComputersOption, 1,
                    RoomOptions.ComputerLevelOption, 1);
                yield return HostRoom("Tour table", Mode, options, 4);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("01_room");
                yield return WaitForGuests(guests, 120f);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("02_everybody_ready");
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(120f);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("02_room");
                // The first decisions are left to the clock of the room.
                sleepy = Quits ? 0 : 2;
            }

            yield return WaitFor(() => manager.IsOnlineMatch && manager.IsGameRunning, 60f, "the match");
            if (!manager.IsOnlineMatch)
            {
                yield return Fail("the match did not start");
                yield break;
            }
            Note($"playing seat {manager.LocalSeat} of {Match.players.Count}: " + string.Join(", ", Match.players.Select(p => $"{p.name}{(p.bot ? " (computer)" : "")}")));
            piloting = true;
            StartCoroutine(Pilot());
            StartCoroutine(ShotWhen("04_the_clock_runs_down", () => WaitingForSomebodyElse && Server.Turn != null && Server.Turn.Remaining < 6f));
            StartCoroutine(ShotWhen("05_trade_offer", () => manager.MonopolyUI.Offer.IsOpen, 0.6f));
            StartCoroutine(ShotWhen("06_round_4", () => Match != null && Match.round >= 4 && manager.WaitingForHuman));
            StartCoroutine(ShotWhen("07_round_12", () => Match != null && Match.round >= 12 && !manager.WaitingForHuman));
            yield return new WaitForSeconds(6f);
            yield return Shot("03_first_turns");

            float started = Time.realtimeSinceStartup;
            float inherited = -1f;
            while (!Over && Server.InRoom && Time.realtimeSinceStartup - started < Patience * 60f)
            {
                if (Quits && Time.realtimeSinceStartup - started > QuitAfter)
                {
                    yield return LeaveTheMatch();
                    yield break;
                }
                if (!Hosting && Server.IsHost)
                {
                    // The host left and the room passed on: this player moves the computer players now.
                    if (inherited < 0f)
                    {
                        inherited = Time.realtimeSinceStartup;
                        Note("the room is mine now: " + string.Join(", ", Match.players.Select(p => $"{p.name}{(p.bot ? " (computer)" : "")}")));
                        StartCoroutine(ShotLater("10_inherited_the_room", 8f));
                    }
                    else if (Time.realtimeSinceStartup - inherited > CallOffAfter)
                    {
                        break;
                    }
                }
                yield return null;
            }
            if (!Over && Server.IsHost && Server.InRoom)
            {
                Note("calling the match off");
                Server.EndRoom(error => Note("end room: " + (error ?? "ok")));
            }
            yield return WaitFor(() => Over && manager.MonopolyUI.Results.IsOpen, 90f, "the results");
            piloting = false;
            yield return new WaitForSeconds(2f);
            yield return Shot("08_results");
            if (Match != null)
            {
                Note($"result: {manager.State}, round {Match.round}; " + string.Join(", ", Match.Standings().Select(p => $"{p.place}. {p.name} {Match.NetWorth(p.index)}")));
                Note($"final: applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8}");
            }
            yield return WaitFor(() => Server.CurrentRoom != null && Server.CurrentRoom.Phase == RoomPhase.Finished, 60f, "the room to finish");
            Note("places: " + string.Join(", ", Server.Members.Select(member => $"{Server.NameOf(member.Identity)} {member.Place}. with {member.Score}")));

            manager.ActiveController.TransitionState(BaseGameState.Initialization);
            yield return new WaitForSeconds(1.5f);
            yield return Shot("09_back_in_the_room");
            Note($"back in the room: session {manager.InSession}, match {manager.Match != null}, state {manager.State}, lobby {manager.Online.LobbyUI.IsOpen}");
            // The guests leave first, so the host still sees the room lose its members.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            Note("done");
            Quit();
        }

        private bool WaitingForSomebodyElse
        {
            get
            {
                int seat = manager.Lockstep != null ? manager.Lockstep.Waiting : -1;
                return seat >= 0 && seat != manager.LocalSeat && !Match.players[seat].bot && !Match.HasEvents;
            }
        }

        /// <summary>Leaves through the match menu, as a player who has had enough does.</summary>
        private IEnumerator LeaveTheMatch()
        {
            piloting = false;
            manager.ActiveController.TransitionState(BaseGameState.Paused);
            yield return new WaitForSeconds(1f);
            yield return Shot("10_match_menu");
            Note($"leaving the match in round {Match.round}");
            manager.Online.LeaveMatch();
            yield return new WaitForSeconds(1.5f);
            yield return Shot("11_left_the_match");
            Note($"left: session {manager.InSession}, match {manager.Match != null}, state {manager.State}, in a room {Server.InRoom}");
            Note("done");
            Quit();
        }

        /// <summary>Plays the seat of this player: rolls, buys what it can afford, builds, bids a little, pays or gives up, ends turns.</summary>
        private IEnumerator Pilot()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.45f);
                if (!piloting || Match == null || !manager.IsOnlineMatch)
                {
                    continue;
                }
                int seat = manager.LocalSeat;
                if (manager.MonopolyUI.Cards.IsOpen)
                {
                    manager.MonopolyUI.Cards.Confirm();
                    continue;
                }
                TradeOffer pending = manager.Lockstep.PendingOffer;
                if (pending != null && pending.to == seat && manager.MonopolyUI.Offer.IsOpen)
                {
                    yield return new WaitForSeconds(1.2f);
                    bool accept = BotBrain.TradeGain(Match, pending, seat) >= 0f;
                    Note($"answering the offer of seat {pending.from}: {(accept ? "accept" : "decline")}");
                    manager.Online.AnswerTrade(seat, accept);
                    continue;
                }
                if (!manager.WaitingForHuman || Match.Decider != seat || pending != null)
                {
                    continue;
                }
                if (sleepy > 0)
                {
                    sleepy--;
                    Note($"leaving {Match.phase} to the clock");
                    int applied = manager.Lockstep.Applied;
                    yield return WaitFor(() => manager.Lockstep == null || manager.Lockstep.Applied != applied, 40f, "the clock");
                    continue;
                }
                IMonopolyCommands commands = manager.Commands;
                switch (Match.phase)
                {
                    case MatchPhase.Roll:
                    case MatchPhase.JailChoice:
                        commands.Roll(seat);
                        break;
                    case MatchPhase.BusChoice:
                        commands.ChooseBus(seat, 2);
                        break;
                    case MatchPhase.MoveAnywhere:
                        commands.ChooseDestination(seat, 39);
                        break;
                    case MatchPhase.BuyChoice:
                        if (Match.CanAffordPurchase && Match.players[seat].cash > 250)
                        {
                            commands.Buy(seat);
                        }
                        else
                        {
                            commands.DeclineBuy(seat);
                        }
                        break;
                    case MatchPhase.Auction:
                        if (Match.auction.MinimumBid <= 60 && Match.players[seat].cash > 400)
                        {
                            commands.Bid(seat, Match.auction.MinimumBid);
                        }
                        else
                        {
                            commands.PassBid(seat);
                        }
                        break;
                    case MatchPhase.RaiseFunds:
                    {
                        int space = Match.PropertiesOf(seat).Where(s => Match.CanMortgage(seat, s, out _)).DefaultIfEmpty(-1).First();
                        if (space >= 0)
                        {
                            commands.Mortgage(seat, space);
                        }
                        else
                        {
                            commands.DeclareBankruptcy(seat);
                        }
                        break;
                    }
                    case MatchPhase.EndTurn:
                        if (!EndTurnExtras(seat, commands))
                        {
                            manager.MonopolyUI.Manage.Close();
                            commands.EndTurn(seat);
                        }
                        break;
                }
            }
        }

        /// <summary>Managing before the turn ends: a house where one can go up, and once a small gift to another person at the table.</summary>
        private bool EndTurnExtras(int seat, IMonopolyCommands commands)
        {
            int site = Match.PropertiesOf(seat).Where(s => Match.CanBuild(seat, s, out _) && Match.players[seat].cash - Match.Space(s).houseCost > 400)
                .DefaultIfEmpty(-1).First();
            if (site >= 0)
            {
                Note($"building on {Match.Space(site).name}");
                commands.Build(seat, site);
                return true;
            }
            PlayerState other = Match.players.FirstOrDefault(p => !p.bot && !p.bankrupt && p.index != seat);
            if (Hosting && !proposed && Match.round >= 2 && other != null && Match.players[seat].cash > 300)
            {
                proposed = true;
                var offer = new TradeOffer { from = seat, to = other.index, giveCash = 25 };
                Note($"offering {other.name} a gift of $25");
                commands.ProposeTrade(offer);
                return true;
            }
            return false;
        }

        /// <summary>The first time the clock of the room runs out, the screen shows it.</summary>
        private void ShootTimeout(RoomActionInfo action)
        {
            if (action.IsTimeout && !timeoutShot && manager.Lockstep != null)
            {
                timeoutShot = true;
                StartCoroutine(ShotLater("04_time_is_up", 0.6f));
            }
        }

        private IEnumerator ShotWhen(string shotName, Func<bool> condition, float delay = 0f)
        {
            while (!condition())
            {
                if (Over)
                {
                    yield break;
                }
                yield return null;
            }
            yield return ShotLater(shotName, delay);
        }

        private IEnumerator ShotLater(string shotName, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            yield return Shot(shotName);
        }
    }
}
