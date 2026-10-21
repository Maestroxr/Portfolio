using System;
using System.Collections.Generic;
using System.Globalization;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using Portfolio.MemoryCards.Server;
using UnityEngine;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Online controller of the Memory Cards module: versus games with players on other devices, in a room of the
    /// game's SpacetimeDB database. Rooms, ready and start, turns and the turn timer come from the base
    /// <see cref="OnlineGameController"/> and the base server; this class adds what is Memory Cards. A room plays a
    /// level of the campaign, whose board the host's client writes into the options of the room
    /// (<see cref="BoardOptions"/>). The server deals that board and judges every flip with the rules of the game
    /// (Server/Lib.cs compiles the model in), so the faces of the cards stay on the server until they turn; the flips
    /// come back as events, which the manager plays like the flips of a local game (MemoryCardsGameManager.Versus.cs).
    /// A turn the server begins reaches the manager after the flips of the same moment, so the flip that ended a turn
    /// is shown before the next turn is announced. Several people may play at this device (the companions of the base
    /// controller): each holds a seat of the room, the manager plays every one of them (<see cref="OnlineSeats"/>), a
    /// flip goes out for the seat whose turn it is (<see cref="Flip"/>), and the device is handed over between its seats
    /// when the party asked for that (<see cref="HandOverTo"/>).
    ///
    /// A room may play on a shared screen (<see cref="OnlineGameController.IsScreen"/>): the screen is a client with no seat
    /// of its own that mirrors the board like any other, shows every flip as it arrives, names the players in the banners
    /// and the standings in the results, and never lets a card be clicked. A player at a remote control
    /// (<see cref="OnlineGameController.IsRemote"/>) keeps running the game and sending the flips of their seat as always,
    /// behind the remote panel, whose content is a grid of the same mirror sized for a phone (<see cref="RemoteBoard"/>,
    /// <see cref="BuildRemotePanel"/>); the game's own interface rests while the panel shows (<see cref="OnPresentationChanged"/>).
    /// </summary>
    public class MemoryCardsOnlineController : OnlineGameController
    {
        /// <summary>The seconds a turn may last, for the lobby; the server takes 5 to 120.</summary>
        private static readonly int[] TurnSeconds = { 10, 15, 20, 30, 45 };
        private const int DefaultTurnChoice = 2;

        private readonly List<MemoryCardsGameManager.OnlineFlip> arrived = new List<MemoryCardsGameManager.OnlineFlip>();
        /// <summary>The seats of the running game: board seats against room seats, and which of them are played here.</summary>
        private OnlineSeats seats;
        private RoomOptionSpec[] optionSpecs;
        /// <summary>A turn the server began, handed to the manager after the flips that came with it.</summary>
        private RoomTurnInfo pendingTurn;
        /// <summary>The board seat of the player at this device who had the board last, so a turn of theirs needs no hand-over.</summary>
        private int lastHandOverSeat = -1;

        public MemoryCardsGameManager MemoryCards => BaseManager as MemoryCardsGameManager;

        /// <summary>The grid of the remote panel, once this device showed the panel as a remote control; null before, and on the screen.</summary>
        public RemoteBoard RemoteBoard { get; private set; }

        /// <summary>
        /// The remote panel is the whole interface of a remote: its own grid of the board, sized for a phone held in the hand
        /// (<see cref="RemoteBoard"/>), rather than the board laid out for the television behind a strip.
        /// </summary>
        public override bool RemotePanelCoversGame => true;

        private GameServerClient Client => Server as GameServerClient;

        public override int MaxPlayersLimit => VersusMatch.MaxPlayers;

        public override int MinPlayersLimit => VersusMatch.MinPlayers;

        /// <summary>The boards of the campaign and free play; the endless run is a game for one.</summary>
        public override IReadOnlyList<RoomLevelChoice> AllLevelChoices
        {
            get
            {
                var choices = new List<RoomLevelChoice>();
                MemoryCardsCampaign campaign = Campaign;
                for (int i = 0; campaign != null && i < campaign.Count; i++)
                {
                    MemoryCardsLevel level = campaign.Level(i);
                    if (level != null && level.IsCampaign && level.Settings != null)
                    {
                        RoundRules rules = VersusMatch.RulesFor(level.Settings.ToRules());
                        choices.Add(new RoomLevelChoice(i, $"{MemoryCardsText.T(level.Title)} ({rules.Columns}x{rules.Rows})"));
                    }
                }
                return choices;
            }
        }

        public override IReadOnlyList<RoomOptionSpec> OptionSpecs =>
            optionSpecs ??= new[] { RoomOptionSpec.Clock("Seconds a turn", TurnSeconds, DefaultTurnChoice) };

        private MemoryCardsCampaign Campaign => BaseManager != null ? BaseManager.Campaign as MemoryCardsCampaign : null;

        /// <summary>
        /// The options of a room: the clock the host picked in the lobby plus the board of the level, which the server
        /// deals from these numbers (it does not know the levels of the game).
        /// </summary>
        public override string ComposeOptions(int level, string picked)
        {
            MemoryCardsCampaign campaign = Campaign;
            MemoryCardsLevel board = campaign != null ? campaign.Level(level) : null;
            if (board == null || board.Settings == null)
            {
                return picked;
            }
            RoundRules rules = VersusMatch.RulesFor(board.Settings.ToRules());
            string turn = RoomOptions.Value(picked, RoomOptions.TurnOption) ?? TurnSeconds[DefaultTurnChoice].ToString(CultureInfo.InvariantCulture);
            return RoomOptions.Write(BoardOptions.Pairs(rules, campaign.AnimalPool(board).Count, RoomOptions.TurnOption, turn));
        }

        public override string DescribeRoom(RoomInfo room)
        {
            string set = room.Option(BoardOptions.MatchKey, 2) >= 3 ? MemoryCardsText.T("triplets") : MemoryCardsText.T("pairs");
            return $"{base.DescribeRoom(room)}, {set}";
        }

        /// <summary>
        /// The player at this device who plays board seat <paramref name="seat"/> flips a card: the server checks the turn
        /// and answers with a flip event.
        /// </summary>
        public void Flip(int seat, int card)
        {
            int roomSeat = seats != null ? seats.RoomSeatOf(seat) : -1;
            if (IsPlaying && roomSeat >= 0 && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.FlipCard((byte)roomSeat, (ushort)card);
            }
        }

        /// <summary>
        /// Covers the board until the player at this device who plays board seat <paramref name="seat"/> is ready to take
        /// the device, then calls <paramref name="ready"/>, by the rules of <see cref="OnlineGameController.HandOver"/>: at
        /// once when the party does not hand the device over (<see cref="OnlineGameController.HandOverLocalSeats"/>), when
        /// one player sits here, when the seat is played elsewhere, or when the same player had the board last. The curtain
        /// wears <paramref name="color"/>, the colour of the seat on the scoreboard, as the hand-over of local play does
        /// (the base's would wear the colour of the player's place in the party, which is another one here).
        /// </summary>
        public void HandOverTo(int seat, Color color, Action ready, string detail = null)
        {
            int roomSeat = seats != null ? seats.RoomSeatOf(seat) : -1;
            RoomMemberInfo member = roomSeat >= 0 && Server != null ? Server.LocalMemberAt((byte)roomSeat) : null;
            if (!HandOverLocalSeats || member == null || LocalMembers.Count < 2 || BaseManager == null || lastHandOverSeat == seat)
            {
                lastHandOverSeat = member != null ? seat : lastHandOverSeat;
                ready?.Invoke();
                return;
            }
            lastHandOverSeat = seat;
            var local = new LocalSeat(Mathf.Max(0, LocalIndexOf((byte)roomSeat))) { Name = NameOf(member), Color = color };
            TurnCurtain.For(BaseManager).Show(local, detail, ready);
        }

        protected override CampaignProgress CreateOnlineProgress(IStorageStrategy storage)
        {
            return new MemoryCardsProgress(storage, GameType.MemoryCards);
        }

        /// <summary>The content of the remote panel: the player's score and the grid of the board they tap (<see cref="RemoteBoard"/>).</summary>
        protected override void BuildRemotePanel(RectTransform content, RemotePanelKit kit)
        {
            RemoteBoard = RemoteBoard.Build(kit, this);
        }

        /// <summary>
        /// The remote panel showed or went (this device became a remote control or plays on its own again, in the lobby or
        /// mid-game): the game's own interface rests while the panel covers it, and is back the moment it goes, with the
        /// mirror as it is; a turn based game needs nothing else to switch.
        /// </summary>
        protected override void OnPresentationChanged()
        {
            MemoryCards?.ShowGameView(!ShowsRemotePanel);
        }

        protected override void Start()
        {
            base.Start();
            if (Client != null)
            {
                Client.ConnectionOpened += Bind;
            }
        }

        protected override void OnDestroy()
        {
            if (Client != null)
            {
                Client.ConnectionOpened -= Bind;
            }
            base.OnDestroy();
        }

        /// <summary>Flip events only exist while they are sent, so they are collected here and played after the tick.</summary>
        private void Bind(DbConnection connection)
        {
            connection.Db.FlipEvent.OnInsert += (context, row) =>
            {
                var flip = new MemoryCardsGameManager.OnlineFlip
                {
                    Seat = row.Seat,
                    Card = row.Card,
                    Outcome = (FlipOutcome)row.Outcome,
                    Points = row.Points,
                    Combo = (int)row.Combo
                };
                for (int i = 0; i < row.Cards.Count; i++)
                {
                    flip.Cards.Add(new MemoryCardsGameManager.OnlineCard
                    {
                        Index = row.Cards[i],
                        Kind = (CardKind)row.Kinds[i],
                        Animal = row.Animals[i]
                    });
                }
                arrived.Add(flip);
            };
        }

        protected override IEnumerable<string> RoomQueries(RoomInfo room)
        {
            yield return $"SELECT * FROM memory_board WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM board_card WHERE room_id = {room.Id}";
            yield return $"SELECT * FROM flip_event WHERE room_id = {room.Id}";
        }

        /// <summary>
        /// The server dealt: the manager gets the board and the seats, then loads the level and starts. A shared screen holds
        /// no seat (<see cref="OnlineSeats"/> with nothing played here): it mirrors the board for the players at their remotes.
        /// </summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            MemoryCardsGameManager manager = MemoryCards;
            DbConnection connection = Client != null ? Client.Connection : null;
            MemoryBoard dealt = connection != null ? connection.Db.MemoryBoard.RoomId.Find(room.Id) : null;
            if (manager == null || dealt == null)
            {
                Report(MemoryCardsText.T("The board of the game did not arrive."));
                return;
            }

            // The board seats follow the members by room seat; the seats played here are the device's and its companions'.
            var names = new List<string>();
            var roomSeats = new List<int>();
            var localSeats = new List<int>();
            int deviceSeat = -1;
            foreach (RoomMemberInfo member in Server.Members)
            {
                roomSeats.Add(member.Seat);
                names.Add(NameOf(member));
                if (Server.IsLocal(member))
                {
                    localSeats.Add(member.Seat);
                }
                if (Server.IsMe(member))
                {
                    deviceSeat = member.Seat;
                }
            }
            if (names.Count < VersusMatch.MinPlayers || localSeats.Count == 0 && !IsScreen)
            {
                // The other player dropped between the start and its arrival here; the server is ending the game already.
                Report(MemoryCardsText.T("The other player left before the game began."));
                return;
            }
            seats = new OnlineSeats(roomSeats, localSeats, deviceSeat);
            lastHandOverSeat = -1;
            var frozen = new bool[dealt.Cards];
            foreach (BoardCard card in connection.Db.BoardCard.RoomId.Filter(room.Id))
            {
                if (card.Index < frozen.Length)
                {
                    frozen[card.Index] = card.Frozen;
                }
            }
            // The board shown for memorizing came with the start; the flips of this game follow.
            MemoryCardsGameManager.OnlineFlip shown = arrived.FindLast(flip => flip.Outcome == FlipOutcome.Preview);
            arrived.Clear();
            pendingTurn = null;
            manager.PrepareOnlineGame(new MemoryCardsGameManager.OnlineBoard
            {
                Cards = dealt.Cards,
                Columns = dealt.Columns,
                MatchSize = dealt.MatchSize,
                Sets = dealt.Sets,
                PreviewSeconds = dealt.PreviewSeconds,
                Frozen = frozen,
                Names = names.ToArray(),
                Seats = seats,
                Preview = shown != null ? shown.Cards : null
            });
            base.OnRoomStarted(room);
            PassScores();
        }

        /// <summary>The flips of the tick go to the manager in the order of the server, and a turn that came with them after them.</summary>
        protected override void OnServerTick()
        {
            MemoryCardsGameManager manager = MemoryCards;
            if (manager == null || !manager.IsOnlineVersus || seats == null)
            {
                // The preview of a game that is about to start waits for OnRoomStarted; anything else is stale.
                arrived.RemoveAll(flip => flip.Outcome != FlipOutcome.Preview);
                pendingTurn = null;
                return;
            }
            foreach (MemoryCardsGameManager.OnlineFlip flip in arrived)
            {
                if (flip.Outcome == FlipOutcome.Preview)
                {
                    continue;
                }
                flip.Seat = seats.BoardSeatOf(flip.Seat);
                manager.OnlineFlipArrived(flip);
            }
            arrived.Clear();
            if (pendingTurn != null)
            {
                RoomTurnInfo turn = pendingTurn;
                pendingTurn = null;
                int turnSeat = seats.BoardSeatOf(turn.Seat);
                if (turnSeat >= 0)
                {
                    manager.OnlineTurn(turnSeat, (int)turn.Number);
                }
            }
        }

        /// <summary>
        /// The server began a turn. The events of a frame come in a fixed order (the turn before the tick), while the
        /// flip that ended the last turn came with it: the manager hears of the turn after the flips (<see cref="OnServerTick"/>).
        /// </summary>
        protected override void OnTurnChanged(RoomTurnInfo turn)
        {
            pendingTurn = turn;
        }

        protected override void OnMembersChanged()
        {
            PassScores();
        }

        /// <summary>The results of the manager show once the last flip played out; the places are the server's.</summary>
        protected override void OnRoomFinished(RoomInfo room)
        {
            PassScores();
            MemoryCards?.OnlineFinished();
        }

        /// <summary>The scores are counted by the server; a seat whose member left stops playing.</summary>
        private void PassScores()
        {
            MemoryCardsGameManager manager = MemoryCards;
            if (manager == null || !manager.IsOnlineVersus || seats == null)
            {
                return;
            }
            var present = new HashSet<int>();
            foreach (RoomMemberInfo member in Server.Members)
            {
                int seat = seats.BoardSeatOf(member.Seat);
                if (seat >= 0)
                {
                    present.Add(seat);
                    manager.OnlineSeat(seat, (int)member.Score, true);
                }
            }
            for (int seat = 0; seat < manager.Versus.Seats.Count; seat++)
            {
                if (!present.Contains(seat))
                {
                    manager.OnlineSeat(seat, manager.Versus.Seats[seat].Score, false);
                }
            }
        }
    }
}
