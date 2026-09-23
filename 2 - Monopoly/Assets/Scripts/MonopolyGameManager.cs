using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using Gamebox.Lockstep;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The Monopoly game module. It seats the players (<see cref="MatchSetup"/>), starts a <see cref="MonopolyMatch"/>
    /// with the rules of the chosen mode, and directs it: every event the rules engine records is played out on the
    /// board (dice, hopping tokens, money flying between panels, cards, houses popping up) before the next decision is
    /// asked for, from a human player through the interface or from a computer player through <see cref="BotBrain"/>.
    /// Menus, pausing, custom settings and the state machine come from <see cref="BaseGameManager"/>; a match in
    /// progress is saved automatically at the start of every human turn and continues from the title screen. Matches
    /// with players on other devices are in MonopolyGameManager.Online.cs.
    /// </summary>
    public partial class MonopolyGameManager : BaseGameManager
    {
        [SerializeField] private MonopolySettings settings;
        [SerializeField] private MonopolyController controller;
        [SerializeField] private MonopolyUI ui;
        [SerializeField] private MonopolyCampaign campaign;
        [SerializeField] private BoardView board;
        [SerializeField] private Dice dice;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private MonopolyAudio sound;
        [SerializeField] private List<MonopolyPlayer> tokens = new List<MonopolyPlayer>();
        [SerializeField] private ParticleSystem confetti;

        private const string SavePrefix = "Monopoly.Save.";

        private MonopolySettings customSettings;
        private MonopolySettings activeSettings;
        private MonopolySettings matchSettings;
        private MonopolyProgress progress;
        private BotBrain bots;
        private IRandom random;
        private Coroutine director;
        private MatchSetup pendingSetup;
        private int pendingMode;
        private int tradeCheckedTurn = -1;
        private bool humanActed;
        private bool finishing;

        // What the board shows, which runs behind the rules engine while events play.
        private readonly List<int> shownPosition = new List<int>();
        private readonly List<bool> shownJailed = new List<bool>();
        private readonly List<int> shownCash = new List<int>();

        public MonopolyMatch Match { get; private set; }
        public MatchSetup Setup { get; private set; }
        public MonopolySettings MonopolySettings => Settings as MonopolySettings ?? settings;
        public MonopolyUI MonopolyUI => ui;
        public MonopolyController MonopolyController => controller;
        public BoardView Board => board;
        public MonopolyCampaign ModeList => campaign;
        public MonopolyProgress Progress => progress ??= new MonopolyProgress(Disk, GameType.Monopoly);
        public MonopolyLevel CurrentMode => campaign != null ? campaign.Mode(LevelIndex) : null;
        public BotBrain Bots => bots;

        /// <summary>Whether the director waits for a human decision right now.</summary>
        public bool WaitingForHuman { get; private set; }

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => settings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : settings;
            set => activeSettings = value as MonopolySettings;
        }

        protected override bool UsesTimer => false;

        private MonopolySettings CreateCustomSettings()
        {
            MonopolySettings copy = settings != null ? Instantiate(settings) : ScriptableObject.CreateInstance<MonopolySettings>();
            copy.name = $"{(settings != null ? settings.name : "MonopolySettings")} (custom)";
            return copy;
        }

        protected override void Awake()
        {
            foreach (MonopolyPlayer token in tokens)
            {
                RegisterPlayer(token);
            }
            base.Awake();
            random = new SystemRandom();
            bots = new BotBrain(new SystemRandom());
            if (dice != null)
            {
                dice.Bounced += () => sound?.Play(Sfx.DiceBounce, 0.7f);
            }
        }

        protected override void Start()
        {
            base.Start();
            ShowIdleBoard();
        }

        // ------------------------------------------------------------------ starting

        /// <summary>The new game screen's Start: remember the table and start the mode through the controller.</summary>
        public void BeginMatch(MatchSetup chosen, int mode)
        {
            pendingSetup = chosen;
            pendingMode = mode;
            controller.PrepareGame(LevelData.Create(mode));
        }

        public override void LoadLevel(int level)
        {
            LevelIndex = campaign != null ? Mathf.Clamp(level, 0, Mathf.Max(0, campaign.Count - 1)) : 0;
            CurrentLevel = LevelData.Create(LevelIndex);
            UpdateLevel();
        }

        protected override void UpdateLevel()
        {
        }

        protected override void UpdateScore()
        {
        }

        public override void StartGame()
        {
            MonopolyLevel mode = CurrentMode;
            matchSettings = mode != null && mode.Settings != null ? mode.Settings : MonopolySettings;
            string error = "no settings";
            if (matchSettings == null || !matchSettings.AreSettingsValid(out error))
            {
                UI?.UpdateError($"Cannot start: {error}");
                return;
            }
            if (InSession)
            {
                StartOnlineMatch();
                return;
            }
            Setup = (pendingSetup ?? Setup ?? MatchSetup.Default()).Clone();
            pendingSetup = null;
            var match = new MonopolyMatch(matchSettings.Board.CreateLayout(), matchSettings.Rules, random);
            for (int i = 0; i < Setup.seats.Count; i++)
            {
                SeatSetup seat = Setup.seats[i];
                if (seat.kind == SeatKind.Off)
                {
                    continue;
                }
                PlayerState player = match.AddPlayer(Setup.PlayingName(i), seat.token, seat.kind == SeatKind.Computer, seat.level);
                player.color = i;
            }
            // The match deals and starts first; its opening events (dealt deeds, the first turn) play once directing starts.
            match.Start();
            BeginDirecting(match, fresh: true);
        }

        /// <summary>Resets the board and the interface for <paramref name="match"/> and starts directing it.</summary>
        private void BeginDirecting(MonopolyMatch match, bool fresh)
        {
            StopDirecting();
            Match = match;
            finishing = false;
            tradeCheckedTurn = -1;
            ui.CloseAll();
            board.ResetPieces();
            shownPosition.Clear();
            shownJailed.Clear();
            shownCash.Clear();
            for (int i = 0; i < tokens.Count; i++)
            {
                bool used = i < match.players.Count;
                if (used)
                {
                    PlayerState player = match.players[i];
                    tokens[i].Configure(player.token, player.color, !player.bankrupt);
                    shownPosition.Add(fresh ? 0 : player.position);
                    shownJailed.Add(!fresh && player.inJail);
                    shownCash.Add(fresh ? match.rules.startingCash : player.cash);
                }
                else
                {
                    tokens[i].Configure(0, i, false);
                }
            }
            ui.BindPlayers(match);
            for (int i = 0; i < match.players.Count; i++)
            {
                ui.Panel(i)?.SetCash(shownCash[i], false);
            }
            if (!fresh)
            {
                SyncBoard(false);
            }
            ArrangeAll();
            dice?.Show(match.lastRoll.a > 0 ? match.lastRoll : new DiceRoll(3, 4));
            ui.SetMatchInfo(ModeName, match.round, match.rules.roundLimit);
            ui.SetPot(match.rules.freeParkingJackpot, match.pot);
            ui.RefreshPlayers(match);
            cameraRig?.ClearFocus();
            TransitionState(BaseGameState.Running);
            director = StartCoroutine(lockstep != null ? DirectOnline() : Direct());
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

        public string ModeName => CurrentMode != null ? CurrentMode.Title : "Monopoly";

        /// <summary>Leaves the match for the title screen (it stays saved and continues from there).</summary>
        public void ReturnToTitle()
        {
            if (Match != null && !Match.IsOver)
            {
                SaveMatch(true);
            }
            StopDirecting();
            Match = null;
            ui.CloseAll();
            TransitionState(BaseGameState.Initialization);
            ShowIdleBoard();
        }

        /// <summary>The board of the title screen: the tokens waiting on GO.</summary>
        private void ShowIdleBoard()
        {
            if (Match != null)
            {
                return;
            }
            board.ResetPieces();
            int[] lineup = { 0, 1, 2, 5 };
            for (int i = 0; i < tokens.Count; i++)
            {
                tokens[i].Configure(lineup[i % lineup.Length], i, true);
                tokens[i].PlaceAt(board.TokenSpot(0, i, Mathf.Min(4, tokens.Count), false));
            }
            dice?.Show(new DiceRoll(5, 6));
        }

        // ------------------------------------------------------------------ directing

        private float Speed => (matchSettings != null ? Mathf.Max(0.25f, matchSettings.AnimationSpeed) : 1f) * OnlinePace;

        /// <summary>A pause scaled by the animation speed (and shortened while only computer players act).</summary>
        private float Beat(float seconds)
        {
            bool botsOnly = Match != null && Match.Decider >= 0 && Match.players[Match.Decider].bot;
            return seconds / Speed * (botsOnly ? 0.8f : 1f);
        }

        private IEnumerator Direct()
        {
            yield return null;
            while (Match != null)
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
                    SyncBoard(false);
                    continue;
                }
                if (Match.IsOver)
                {
                    yield return Finish();
                    director = null;
                    yield break;
                }
                int decider = Match.Decider;
                if (decider < 0)
                {
                    yield return null;
                    continue;
                }
                PlayerState player = Match.players[decider];
                if (player.bot)
                {
                    ShowBotTurn(player);
                    yield return new WaitForSeconds(Beat(matchSettings.BotThinkTime));
                    if (!IsGameRunning || Match == null || Match.Decider != decider)
                    {
                        continue;
                    }
                    if (Match.phase == MatchPhase.Roll && tradeCheckedTurn != Match.turn)
                    {
                        tradeCheckedTurn = Match.turn;
                        TradeOffer offer = bots.ProposeTrade(Match, decider);
                        if (offer != null)
                        {
                            yield return ResolveOffer(offer);
                            continue;
                        }
                    }
                    if (!bots.Act(Match))
                    {
                        Debug.LogWarning($"Monopoly: the computer player {player.name} could not act in {Match.phase}.");
                        yield return null;
                    }
                    continue;
                }
                MatchPhase phase = Match.phase;
                ShowDecision(player);
                WaitingForHuman = true;
                humanActed = false;
                while (Match != null && !humanActed && !Match.HasEvents && Match.Decider == decider && Match.phase == phase && !Match.IsOver)
                {
                    yield return null;
                }
                WaitingForHuman = false;
            }
            director = null;
        }

        /// <summary>The controller reports every accepted human command, which ends the wait for a decision.</summary>
        public void HumanActed()
        {
            humanActed = true;
        }

        private IEnumerator Finish()
        {
            if (finishing)
            {
                yield break;
            }
            finishing = true;
            ui.Actions.Hide();
            ui.CloseAll();
            cameraRig?.ClearFocus();
            PlayerState winner = Match.winner >= 0 ? Match.players[Match.winner] : null;
            // Online the player at this device wins or loses; the stars and the saved game belong to the games played here.
            bool onlineMatch = IsOnlineMatch;
            bool humanWon = winner != null && PlayedHere(winner);
            int stars = onlineMatch ? 0 : MonopolyCampaign.StarsFor(Match);
            bool best = false;
            if (!onlineMatch && Match.players.Any(p => !p.bot))
            {
                best = Progress.RecordMatch(LevelIndex, humanWon, stars, winner != null ? Match.NetWorth(winner.index) : 0);
            }
            if (!onlineMatch)
            {
                ClearSave();
            }
            if (winner != null)
            {
                ui.Banner($"{winner.name} {MonopolyStyle.Verb(winner, "wins")}!".ToUpperInvariant(), MonopolyStyle.PlayerColor(winner.color), 1.6f);
                tokens[winner.index].SetTurn(true);
                cameraRig?.Focus(tokens[winner.index].transform.position, 0.6f);
            }
            bool cheer = humanWon || (!onlineMatch && Match.players.All(p => !p.bot));
            sound?.Play(cheer ? Sfx.Win : Sfx.Lose);
            sound?.Duck(4f);
            if (confetti != null && cheer)
            {
                confetti.Play();
            }
            yield return new WaitForSeconds(2.2f);
            PlayerScore = winner != null ? Match.NetWorth(winner.index) : 0;
            TransitionState(humanWon ? BaseGameState.Victory : BaseGameState.GameOver);
            if (onlineMatch)
            {
                ui.Results.Show(Match, ModeName, stars, best, ui.TokenSprite, ReturnToRoom, online.LeaveMatch, true);
            }
            else
            {
                ui.Results.Show(Match, ModeName, stars, best, ui.TokenSprite,
                    () => BeginMatch(Setup, LevelIndex),
                    ReturnToTitle);
            }
        }

        // ------------------------------------------------------------------ decisions

        private void ShowBotTurn(PlayerState player)
        {
            // Decisions are made over the whole board.
            cameraRig?.ClearFocus();
            if (Match.phase == MatchPhase.BuyChoice || Match.phase == MatchPhase.Auction)
            {
                // The title deed or the auction says it all; the action panel would only cover the board.
                ui.Actions.Hide();
                if (Match.phase == MatchPhase.BuyChoice && (!ui.Deed.IsOpen || ui.Deed.Space != Match.pendingPurchase))
                {
                    ui.Deed.ShowInfo(Match, Match.pendingPurchase);
                }
                if (Match.phase == MatchPhase.Auction)
                {
                    ui.Auction.Refresh(Match, ui.TokenSprite, -1, null, null);
                }
                return;
            }
            string doing;
            switch (Match.phase)
            {
                case MatchPhase.RaiseFunds: doing = "is raising money..."; break;
                case MatchPhase.JailChoice: doing = "is plotting a jailbreak..."; break;
                case MatchPhase.BusChoice: doing = "is picking a bus ride..."; break;
                case MatchPhase.MoveAnywhere: doing = "is choosing where to go..."; break;
                default: doing = "is thinking..."; break;
            }
            ui.Actions.Show(player.name, doing, MonopolyStyle.PlayerColor(player.color), ui.TokenSprite(player.token), null, true);
        }

        /// <summary>Shows the choices of the human player the match waits for.</summary>
        private void ShowDecision(PlayerState player)
        {
            cameraRig?.ClearFocus();
            int seat = player.index;
            Color color = MonopolyStyle.PlayerColor(player.color);
            Sprite token = ui.TokenSprite(player.token);
            IMonopolyCommands commands = Commands;
            var options = new List<ActionOption>();
            ActionOption manage = ActionOption.Of("Manage", Icons.Building, Color.white, () => commands.OpenManager(seat), Match.PropertiesOf(seat).Any(), KeyCode.M);
            ActionOption trade = ActionOption.Of("Trade", Icons.Handshake, Color.white, () => commands.OpenTrade(seat), Match.ActiveCount > 1, KeyCode.T);
            string title = $"{MonopolyStyle.Possessive(player)} turn";
            string detail = "";
            switch (Match.phase)
            {
                case MatchPhase.Roll:
                    detail = player.doubles > 0 ? "Doubles! Roll again." : "Roll the dice!";
                    options.Add(ActionOption.Of("Roll", Icons.Dice, MonopolyStyle.Red, () => commands.Roll(seat), true, KeyCode.Space));
                    options.Add(manage);
                    options.Add(trade);
                    break;
                case MatchPhase.JailChoice:
                    title = $"{player.name} {MonopolyStyle.Verb(player, "is")} in jail";
                    detail = $"Attempt {player.jailTurns + 1} of {Match.rules.maxJailTurns}: roll doubles to get out.";
                    options.Add(ActionOption.Of("Roll", Icons.Dice, MonopolyStyle.Red, () => commands.Roll(seat), true, KeyCode.Space));
                    options.Add(ActionOption.Of($"Pay {MonopolyStyle.Money(Match.rules.jailFine)}", Icons.Coins, MonopolyStyle.Green, () => commands.PayJailFine(seat), player.cash >= Match.rules.jailFine, KeyCode.P));
                    if (player.jailCards.Count > 0)
                    {
                        options.Add(ActionOption.Of("Use card", Icons.Ticket, MonopolyStyle.Gold, () => commands.UseJailCard(seat), true, KeyCode.U));
                    }
                    options.Add(manage);
                    break;
                case MatchPhase.BusChoice:
                    title = "All aboard the bus!";
                    detail = "Move by either die, or both.";
                    int[] bus = Match.BusOptions;
                    for (int i = 0; i < bus.Length; i++)
                    {
                        int option = i;
                        int target = (player.position + bus[i]) % Match.SpaceCount;
                        options.Add(ActionOption.Of($"{bus[i]}: {Match.Space(target).name}", Icons.Bus, i == 2 ? MonopolyStyle.Red : MonopolyStyle.Blue,
                            () => commands.ChooseBus(seat, option), true, KeyCode.Alpha1 + i));
                    }
                    break;
                case MatchPhase.MoveAnywhere:
                    title = "Triples!";
                    detail = MobilePlatform.Pick("Click any space to move there.", "Tap any space to move there.");
                    options.Add(ActionOption.Of("Best pick", Icons.Star, MonopolyStyle.Gold, () => commands.ChooseDestination(seat, SuggestDestination(seat)), true, KeyCode.Space));
                    for (int space = 0; space < Match.SpaceCount; space++)
                    {
                        board.Highlight(space, new Color(1f, 0.85f, 0.2f, 0.6f));
                    }
                    break;
                case MatchPhase.BuyChoice:
                    // The title deed holds the whole decision (buy, auction, or manage to raise the money first); the
                    // action panel would only cover the board under it.
                    ui.Actions.Hide();
                    ui.Deed.ShowOffer(Match, Match.pendingPurchase, () => commands.Buy(seat), () => commands.DeclineBuy(seat),
                        Match.PropertiesOf(seat).Any() ? () => commands.OpenManager(seat) : (Action)null);
                    ui.Manage.Refresh();
                    return;
                case MatchPhase.Auction:
                    ui.Actions.Hide();
                    ui.Auction.Refresh(Match, ui.TokenSprite, seat, amount => commands.Bid(seat, amount), () => commands.PassBid(seat));
                    return;
                case MatchPhase.RaiseFunds:
                {
                    Debt debt = Match.CurrentDebt;
                    string creditor = debt.creditor >= 0 ? Match.players[debt.creditor].name : debt.creditor == Party.Pot ? "the Free Parking pot" : "the bank";
                    title = $"{player.name} {MonopolyStyle.Verb(player, "owes")} {MonopolyStyle.Money(debt.amount)}";
                    detail = $"To {creditor}. Raise {MonopolyStyle.Money(debt.amount - player.cash)} by selling buildings or mortgaging, or give up.";
                    options.Add(ActionOption.Of("Manage", Icons.Building, MonopolyStyle.Blue, () => commands.OpenManager(seat), true, KeyCode.M));
                    options.Add(trade);
                    options.Add(ActionOption.Of("Go bankrupt", Icons.Flag, Color.white, () => commands.DeclareBankruptcy(seat), true));
                    if (!ui.Manage.IsOpen && Match.LiquidValue(seat) >= debt.amount)
                    {
                        commands.OpenManager(seat);
                    }
                    break;
                }
                case MatchPhase.EndTurn:
                    if (player.inJail)
                    {
                        // Sent there this turn, or a failed roll for doubles.
                        title = $"{player.name} {MonopolyStyle.Verb(player, "is")} in jail";
                        detail = player.jailTurns == 0 ? "Locked up! Build or trade, then end your turn." : "No doubles. You stay in jail for now.";
                    }
                    else
                    {
                        detail = "Build, trade, or end your turn.";
                    }
                    options.Add(ActionOption.Of("End turn", Icons.Check, MonopolyStyle.Red, () => commands.EndTurn(seat), true, KeyCode.Space));
                    options.Add(manage);
                    options.Add(trade);
                    break;
            }
            ui.Actions.Show(title, detail, color, token, options, false);
            ui.Manage.Refresh();
        }

        private int SuggestDestination(int seat)
        {
            // The same choice a computer player would make.
            int best = 0;
            float bestScore = float.MinValue;
            for (int space = 0; space < Match.SpaceCount; space++)
            {
                SpaceData data = Match.Space(space);
                float score = data.IsProperty && !Match.Deed(space).Owned ? BotBrain.Valuation(Match, seat, space) : data.kind == SpaceKind.Go ? Match.rules.salary : 0f;
                if (data.IsProperty && Match.Deed(space).Owned && Match.Owner(space) != seat)
                {
                    score = -Match.Rent(space, 7);
                }
                if (data.kind == SpaceKind.GoToJail)
                {
                    score = -200f;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = space;
                }
            }
            return best;
        }

        /// <summary>
        /// A trade on the table: a computer player answers at once; a human recipient answers on screen. Accepted
        /// trades go through the rules engine, turned down ones are remembered by the computer players.
        /// </summary>
        public IEnumerator ResolveOffer(TradeOffer offer)
        {
            PlayerState from = Match.players[offer.from];
            PlayerState to = Match.players[offer.to];
            bool accepted;
            if (to.bot)
            {
                ui.Actions.Show(to.name, "is considering the offer...", MonopolyStyle.PlayerColor(to.color), ui.TokenSprite(to.token), null, true);
                yield return new WaitForSeconds(Beat(0.9f));
                accepted = bots.WouldAccept(Match, offer);
            }
            else
            {
                bool answered = false;
                accepted = false;
                ui.Actions.Hide();
                ui.Offer.Show(Match, offer, ui.TokenSprite(from.token), yes =>
                {
                    answered = true;
                    accepted = yes;
                });
                sound?.Play(Sfx.Trade);
                while (!answered && Match != null)
                {
                    yield return null;
                }
            }
            if (Match == null)
            {
                yield break;
            }
            if (accepted && Match.ExecuteTrade(offer))
            {
                ui.Banner("DEAL!", MonopolyStyle.Green, 0.9f);
            }
            else
            {
                if (from.bot)
                {
                    bots.Refused(Match, offer);
                }
                ui.Toast($"{MonopolyStyle.Named(to)} turned down {MonopolyStyle.NamedPossessive(from, false)} offer.", MonopolyStyle.Muted, Icons.Handshake);
                sound?.Play(Sfx.Error, 0.6f);
            }
        }

        // ------------------------------------------------------------------ board clicks

        /// <summary>A click or tap on a space of the board.</summary>
        public void BoardClicked(int space)
        {
            if (Match == null || space < 0 || !IsGameRunning)
            {
                return;
            }
            int decider = Match.Decider;
            if (Match.phase == MatchPhase.MoveAnywhere && decider >= 0 && !Match.players[decider].bot && WaitingForHuman)
            {
                Commands.ChooseDestination(decider, space);
                return;
            }
            if (ui.Deed.IsOffer || ui.Auction.IsOpen || ui.Offer.IsOpen || ui.Trade.IsOpen)
            {
                return;
            }
            ui.Deed.ShowInfo(Match, space);
        }

        public void BoardHovered(int space)
        {
            if (Match != null && Match.phase == MatchPhase.MoveAnywhere && WaitingForHuman)
            {
                board.Hover(space, new Color(1f, 1f, 1f, 0.9f));
            }
        }
    }
}
