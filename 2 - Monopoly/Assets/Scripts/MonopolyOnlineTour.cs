using System;
using System.Collections;
using System.Linq;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
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
    /// match off a little later. <c>hostsaves</c> and <c>joinsaves</c> play a round or two, then the host saves the match by
    /// name and calls it off, opens a new room from the save and the guest joins it: both write down the match they go on
    /// with, which has to be the one saved. <c>hostprivate</c> and <c>joinprivate</c> play a private room, entered with
    /// its code; <c>hostparty</c> and <c>joinparty</c> bring other players to their device first (<c>-gamebox-companions</c>,
    /// <c>-gamebox-guests</c>), who hold seats of their own: the autopilot then plays whichever seat of the device the
    /// table waits for, with the hand-over screen between their turns, which it lifts itself. <c>hostscreen</c> opens the
    /// room as its shared screen, which plays no seat (it waits for <c>-monopoly-players</c> guests, two by default, moves
    /// the computer players and takes pictures of the match as everybody sees it); <c>joinremote</c> joins as a remote
    /// control and plays through the remote panel (every decision is a press of a button the panel offers; a missing
    /// button is written down as a GAP and the command sent directly); <c>joinown</c> joins such a room but plays on its own
    /// device. With <c>-monopoly-switch N</c> a remote plays on its own device after N seconds and is a remote again after
    /// twice as long, writing down that the match went on. <c>-monopoly-patience M</c> is the minutes the host waits before
    /// calling the match off. Every action of the log is written down with the checksum of the match after it, so the
    /// logs of the players show whether they stayed in step. Run the players with <c>-gamebox-identity</c> to tell them
    /// apart, and <c>-gamebox-server</c> / <c>-gamebox-database</c> for a test server. Without the argument it does nothing.
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
        /// <summary>The round after which the host of a tour with saves saves the match, and the seconds the saved match is played on.</summary>
        private const int SaveInRound = 2;
        private const float PlaySavedFor = 40f;
        private const string OnlineSaveName = "Tour online save";
        /// <summary>Seconds after which a remote plays on its own device, and twice as many until it is a remote again; 0 never.</summary>
        private const string SwitchArgument = "-monopoly-switch";
        /// <summary>How many guests a shared screen waits for before it starts (two by default: the screen plays no seat).</summary>
        private const string PlayersArgument = "-monopoly-players";
        /// <summary>Minutes the host waits before calling the match off, in place of <see cref="Patience"/>.</summary>
        private const string PatienceArgument = "-monopoly-patience";

        private MonopolyGameManager manager;
        private bool piloting;
        private int sleepy;
        private bool proposed;
        private bool timeoutShot;
        private bool curtainShot;
        private bool savedOnline;
        private bool playingSave;
        private int panelPresses;
        private int panelGaps;
        private string switching = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            Bootstrap<MonopolyOnlineTour>(Argument);
        }

        private bool Quits => Role.EndsWith("quits", StringComparison.Ordinal);

        /// <summary>A tour that saves the match online and plays it on in a room opened from the save.</summary>
        private bool Resaves => Role.EndsWith("saves", StringComparison.Ordinal);

        private MonopolyMatch Match => manager.Match;

        private bool Over => manager.State.Is(BaseGameState.Victory) || manager.State.Is(BaseGameState.GameOver);

        /// <summary>The remote panel's content while this device plays as a remote and the panel is up; null otherwise.</summary>
        private MonopolyRemotePanel Panel => manager != null && manager.Online != null && manager.Online.ShowsRemotePanel ? manager.Online.Panel : null;

        /// <summary>Whether the remote panel is the interface right now (a remote during the match).</summary>
        private bool OnPanel => Panel != null && Panel.IsShown;

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
            if (InParty)
            {
                // The party hands the device over between the turns of the people at it; the autopilot lifts the curtain.
                manager.Online.HandOverLocalSeats = true;
            }

            if (Hosting)
            {
                // A shared screen plays no seat: it waits for the players of the table on their own devices.
                int guests = Quits ? 2 : AsScreen ? Math.Max(1, NumberArgument(PlayersArgument, 2)) : 1;
                string options = RoomOptions.Write(RoomOptions.TurnOption, 20, RoomOptions.ComputersOption, 1,
                    RoomOptions.ComputerLevelOption, 1);
                // The biggest table of the game: a party at this device and its guests fit in it (four people at most).
                yield return HostRoom("Tour table", Mode, options, 4);
                yield return new WaitForSeconds(0.5f);
                yield return Shot("01_room");
                yield return WaitForGuestDevices(guests, 120f);
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
            Note($"playing {Seats()} of {Match.players.Count}: " + string.Join(", ", Match.players.Select(p => $"{p.name}{(p.bot ? " (computer)" : "")}")));
            bool screen = manager.IsScreen;
            if (screen)
            {
                Note($"the shared screen: nothing to play here; host {Server.IsHost}, remote panel {manager.RemoteMode}, local seat {manager.LocalSeat}");
            }
            else
            {
                Note($"remote control: {manager.RemoteMode}");
                piloting = true;
                StartCoroutine(Pilot());
            }
            float switchAfter = NumberArgument(SwitchArgument, 0f);
            if (switchAfter > 0f && !Hosting)
            {
                StartCoroutine(SwitchDevices(switchAfter));
            }
            StartCoroutine(ShotWhen("04_the_clock_runs_down", () => WaitingForSomebodyElse && Server.Turn != null && Server.Turn.Remaining < 6f));
            StartCoroutine(ShotWhen("05_trade_offer", () => manager.MonopolyUI.Offer.IsOpen
                || manager.Lockstep != null && manager.Lockstep.PendingOffer != null && (screen || OnPanel && manager.WaitingForHuman), 0.6f));
            StartCoroutine(ShotWhen("06_round_4", () => Match != null && Match.round >= 4 && (manager.WaitingForHuman || screen && !Match.HasEvents)));
            StartCoroutine(ShotWhen("07_round_12", () => Match != null && Match.round >= 12 && (!manager.WaitingForHuman || screen)));
            if (screen)
            {
                StartCoroutine(ShotWhen("06b_auction_on_the_screen", () => Match != null && Match.phase == MatchPhase.Auction && !Match.HasEvents, 0.4f));
            }
            else
            {
                StartCoroutine(ShotWhen("03b_panel_decision", () => OnPanel && manager.WaitingForHuman && Panel.Choices.Count > 0, 0.3f));
                StartCoroutine(ShotWhen("06c_panel_properties", () => OnPanel && Match != null && Match.round >= 3 && Panel.Properties.Any(p => p.actions.Count > 0), 0.3f));
            }
            yield return new WaitForSeconds(6f);
            yield return Shot("03_first_turns");

            float started = Time.realtimeSinceStartup;
            float inherited = -1f;
            float patience = NumberArgument(PatienceArgument, Patience) * 60f;
            while (!Over && Server.InRoom && Time.realtimeSinceStartup - started < patience)
            {
                if (Quits && Time.realtimeSinceStartup - started > QuitAfter)
                {
                    yield return LeaveTheMatch();
                    yield break;
                }
                if (Resaves && Hosting && !savedOnline && Match.round > SaveInRound && manager.WaitingForHuman)
                {
                    SaveOnline();
                    break;
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
            yield return EndTheMatch();
            if (Resaves)
            {
                yield return PlayTheSave();
            }
            // The guests leave first, so the host still sees the room lose its members.
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return new WaitForSeconds(1f);
            if (panelPresses > 0 || panelGaps > 0)
            {
                Note($"remote panel: {panelPresses} presses, {panelGaps} gaps");
            }
            if (switching.Length > 0)
            {
                Note("switching: " + switching);
            }
            Note("done");
            Quit();
        }

        /// <summary>
        /// A remote plays on its own device after <paramref name="after"/> seconds of the match (the panel goes, the board
        /// and its prompts come), and is a remote again after twice as long: both times the tour writes down that the
        /// match went on (the log grew) and takes a picture.
        /// </summary>
        private IEnumerator SwitchDevices(float after)
        {
            yield return new WaitForSeconds(after);
            yield return Switch(false, "06d_own_device");
            yield return new WaitForSeconds(after);
            yield return Switch(true, "06e_remote_again");
        }

        private IEnumerator Switch(bool remote, string shotName)
        {
            if (Over || !Server.InRoom || manager.Lockstep == null)
            {
                yield break;
            }
            int applied = manager.Lockstep.Applied;
            string answer = "";
            Note($"switching to {(remote ? "the remote" : "this device")} in round {Match.round} ({manager.Lockstep.Applied} applied, panel {OnPanel}, waiting {manager.WaitingForHuman})");
            manager.Online.SetRemoteMode(remote, error => answer = error ?? "ok");
            yield return WaitFor(() => answer.Length > 0 && manager.RemoteMode == remote, 15f, remote ? "the remote panel" : "the board");
            Note($"switched: {answer}; remote panel {manager.RemoteMode}, panel up {OnPanel}, offer window {manager.MonopolyUI.Offer.IsOpen}, choices {manager.MonopolyUI.Actions.HasChoices}");
            yield return new WaitForSeconds(1.5f);
            yield return Shot(shotName);
            yield return WaitFor(() => manager.Lockstep == null || manager.Lockstep.Applied > applied || Over, 60f, "the match to go on");
            bool wentOn = manager.Lockstep != null && manager.Lockstep.Applied > applied;
            Note($"after the switch the match {(wentOn ? "went on" : "STOOD STILL")}: applied {applied} -> {(manager.Lockstep != null ? manager.Lockstep.Applied : -1)}");
            switching += $"{(remote ? "remote again" : "own device")} in round {Match.round} ({(wentOn ? "went on" : "stood still")}); ";
        }

        /// <summary>Saves the match as it is by name, as the Save the Game of the match menu does.</summary>
        private void SaveOnline()
        {
            savedOnline = true;
            bool saved = manager.SaveAs(OnlineSaveName, out string problem);
            Note($"saved online: {(saved ? "ok" : problem)}; round {Match.round}, applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8} " +
                 $"cash {string.Join("/", Match.players.Select(p => p.cash))}");
        }

        /// <summary>
        /// The room of the save: the host leaves the old room and opens one from the save, the guest joins it; both write down
        /// the match they go on with, play it a while, and the host calls it off.
        /// </summary>
        private IEnumerator PlayTheSave()
        {
            playingSave = true;
            Note("--- the saved match");
            yield return new WaitForSeconds(Hosting ? 3f : 0.5f);
            Server.LeaveRoom();
            yield return WaitFor(() => !Server.InRoom, 10f, "out of the room");
            if (Hosting)
            {
                yield return new WaitForSeconds(2f);
                yield return HostSavedGame(manager.Online, OnlineSaveName, "Tour saved table", RoomOptions.Write(RoomOptions.TurnOption, 20));
                yield return new WaitForSeconds(0.5f);
                yield return Shot("12_room_of_the_save");
                yield return WaitForGuestDevices(1, 120f);
                StartRoom();
            }
            else
            {
                yield return JoinFirstRoom(120f, room => room.PlaysSave);
                yield return new WaitForSeconds(0.7f);
                yield return Shot("12_joined_the_saved_game");
            }
            yield return WaitFor(() => manager.IsOnlineMatch && manager.IsGameRunning, 60f, "the saved match");
            if (!manager.IsOnlineMatch)
            {
                yield return Fail("the saved match did not start");
                yield break;
            }
            Note($"resumed: {Seats()}, round {Match.round}, applied {manager.Lockstep.Applied} sum {manager.Lockstep.Checksum():X8} " +
                 $"cash {string.Join("/", Match.players.Select(p => p.cash))}; " +
                 string.Join(", ", Match.players.Select(p => $"{p.name}{(p.bot ? " (computer)" : "")}")));
            yield return new WaitForSeconds(3f);
            yield return Shot("13_the_saved_match");
            piloting = true;
            float started = Time.realtimeSinceStartup;
            while (!Over && Server.InRoom && Time.realtimeSinceStartup - started < PlaySavedFor)
            {
                yield return null;
            }
            yield return EndTheMatch();
        }

        /// <summary>
        /// The host calls the match off when it is not over; everybody waits for the results and goes back to the room.
        /// </summary>
        private IEnumerator EndTheMatch()
        {
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
            yield return Shot(playingSave ? "14_back_in_the_room" : "09_back_in_the_room");
            Note($"back in the room: session {manager.InSession}, match {manager.Match != null}, state {manager.State}, lobby {manager.Online.LobbyUI.IsOpen}");
        }

        private bool WaitingForSomebodyElse
        {
            get
            {
                int seat = manager.Lockstep != null ? manager.Lockstep.Waiting : -1;
                return seat >= 0 && !manager.IsLocalSeat(seat) && !Match.players[seat].bot && !Match.HasEvents;
            }
        }

        /// <summary>The seats played at this device, for the log: "seat 2", or "seats 0 and 2" for a party.</summary>
        private string Seats()
        {
            return manager.LocalSeats.Count == 0 ? "no seat (the shared screen)"
                : manager.LocalSeats.Count == 1 ? $"seat {manager.LocalSeats[0]}" : $"seats {string.Join(" and ", manager.LocalSeats)}";
        }

        /// <summary>
        /// Waits until <paramref name="guests"/> players from other devices joined the room and are ready (the players at
        /// this device do not count), then writes down who sits where.
        /// </summary>
        private IEnumerator WaitForGuestDevices(int guests, float patience)
        {
            yield return WaitForGuests(guests, patience);
            Note($"members: {string.Join(", ", Server.Members.Select(member => $"{Server.NameOf(member.Identity)} seat {member.Seat}{(Server.IsLocal(member) ? " (here)" : "")}"))}");
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

        /// <summary>
        /// Presses the button of <paramref name="kind"/> (with <paramref name="argument"/>, when given) on the remote panel
        /// while this device plays as a remote, so the panel is what is exercised; a button the panel does not offer is
        /// written down as a gap, and the command is sent <paramref name="directly"/> instead, as it is on a device of its
        /// own.
        /// </summary>
        private void Press(PanelChoiceKind kind, int seat, Action directly, int? argument = null)
        {
            MonopolyRemotePanel panel = Panel;
            if (panel != null && panel.IsShown)
            {
                PanelChoice choice = panel.Find(kind, argument);
                if (choice != null && choice.enabled)
                {
                    panelPresses++;
                    Note($"seat {seat} presses {choice.label} on the panel");
                    panel.Press(choice);
                    return;
                }
                panelGaps++;
                Note($"GAP: the panel offers no {kind}{(argument != null ? " " + argument : "")} for seat {seat} in {Match.phase}: " +
                     $"choices {string.Join(", ", panel.Choices)}; properties {string.Join(", ", panel.Properties.Select(p => $"{p.name} [{string.Join(", ", p.actions)}]"))}");
            }
            directly();
        }

        /// <summary>
        /// Plays the seats of this device (the one the table waits for, when several people sit here): rolls, buys what it
        /// can afford, builds, bids a little, pays or gives up, ends turns, answers offers, and lifts the hand-over curtain.
        /// As a remote it does all of it through the remote panel (<see cref="Press"/>).
        /// </summary>
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
                TurnCurtain curtain = manager.GetComponent<TurnCurtain>();
                if (curtain != null && curtain.IsShown)
                {
                    if (!curtainShot)
                    {
                        curtainShot = true;
                        yield return Shot("05b_hand_over");
                    }
                    Note($"taking the device for seat {seat}");
                    curtain.Lift();
                    continue;
                }
                if (manager.MonopolyUI.Cards.IsOpen)
                {
                    manager.MonopolyUI.Cards.Confirm();
                    continue;
                }
                TradeOffer pending = manager.Lockstep.PendingOffer;
                // On a remote the panel asks, not the offer window.
                if (pending != null && pending.to == seat && (manager.MonopolyUI.Offer.IsOpen || OnPanel && manager.WaitingForHuman))
                {
                    yield return new WaitForSeconds(1.2f);
                    bool accept = BotBrain.TradeGain(Match, pending, seat) >= 0f;
                    Note($"seat {seat} answers the offer of seat {pending.from}: {(accept ? "accept" : "decline")}");
                    Press(accept ? PanelChoiceKind.Accept : PanelChoiceKind.Decline, seat, () => manager.Online.AnswerTrade(seat, accept));
                    continue;
                }
                if (!manager.WaitingForHuman || Match.Decider != seat || pending != null)
                {
                    continue;
                }
                if (OnPanel)
                {
                    // The director offered the decision this frame; the panel draws its buttons on the next one.
                    yield return null;
                    if (!manager.WaitingForHuman || Match == null || Match.Decider != seat || manager.Lockstep == null || manager.Lockstep.PendingOffer != null)
                    {
                        continue;
                    }
                }
                if (sleepy > 0)
                {
                    sleepy--;
                    Note($"seat {seat} leaves {Match.phase} to the clock");
                    int applied = manager.Lockstep.Applied;
                    yield return WaitFor(() => manager.Lockstep == null || manager.Lockstep.Applied != applied, 40f, "the clock");
                    continue;
                }
                IMonopolyCommands commands = manager.Commands;
                switch (Match.phase)
                {
                    case MatchPhase.Roll:
                    case MatchPhase.JailChoice:
                        Press(PanelChoiceKind.Roll, seat, () => commands.Roll(seat));
                        break;
                    case MatchPhase.BusChoice:
                        Press(PanelChoiceKind.Bus, seat, () => commands.ChooseBus(seat, 2), 2);
                        break;
                    case MatchPhase.MoveAnywhere:
                        // The panel's first pick is the best one; on a device of its own the tour goes to the last space.
                        Press(PanelChoiceKind.Destination, seat, () => commands.ChooseDestination(seat, 39));
                        break;
                    case MatchPhase.BuyChoice:
                        if (Match.CanAffordPurchase && Match.players[seat].cash > 250)
                        {
                            Press(PanelChoiceKind.Buy, seat, () => commands.Buy(seat));
                        }
                        else
                        {
                            Press(PanelChoiceKind.DeclineBuy, seat, () => commands.DeclineBuy(seat));
                        }
                        break;
                    case MatchPhase.Auction:
                    {
                        int minimum = Match.auction.MinimumBid;
                        if (minimum <= 60 && Match.players[seat].cash > 400)
                        {
                            Press(PanelChoiceKind.Bid, seat, () => commands.Bid(seat, minimum), minimum);
                        }
                        else
                        {
                            Press(PanelChoiceKind.PassBid, seat, () => commands.PassBid(seat));
                        }
                        break;
                    }
                    case MatchPhase.RaiseFunds:
                    {
                        int space = Match.PropertiesOf(seat).Where(s => Match.CanMortgage(seat, s, out _)).DefaultIfEmpty(-1).First();
                        if (space >= 0)
                        {
                            Press(PanelChoiceKind.Mortgage, seat, () => commands.Mortgage(seat, space), space);
                        }
                        else
                        {
                            Press(PanelChoiceKind.Bankrupt, seat, () => commands.DeclareBankruptcy(seat));
                        }
                        break;
                    }
                    case MatchPhase.EndTurn:
                        if (!EndTurnExtras(seat, commands))
                        {
                            manager.MonopolyUI.Manage.Close();
                            Press(PanelChoiceKind.EndTurn, seat, () => commands.EndTurn(seat));
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
                Note($"seat {seat} builds on {Match.Space(site).name}");
                Press(PanelChoiceKind.Build, seat, () => commands.Build(seat, site), site);
                return true;
            }
            // The gift goes to another person at this device when there is one (the offer is then answered here, after the
            // hand-over), else to the last person at the table: with a party at the guest's device, one of its companions.
            PlayerState other = Match.players.Where(p => !p.bot && !p.bankrupt && p.index != seat)
                .OrderBy(p => manager.IsLocalSeat(p.index) ? 0 : 1).ThenByDescending(p => p.index).FirstOrDefault();
            if (Hosting && !proposed && Match.round >= 2 && other != null && Match.players[seat].cash > 300)
            {
                proposed = true;
                var offer = new TradeOffer { from = seat, to = other.index, giveCash = 25 };
                // The remote panel proposes no trades (the trade window is the device's): the offer is sent as the device would.
                Note($"seat {seat} offers {other.name} (seat {other.index}{(manager.IsLocalSeat(other.index) ? ", at this device" : "")}) a gift of $25{(OnPanel ? " (from the device: the panel has no trade window)" : "")}");
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
