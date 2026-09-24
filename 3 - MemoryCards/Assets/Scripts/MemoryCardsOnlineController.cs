using System.Collections.Generic;
using System.Globalization;
using Gamebox;
using Gamebox.Online;
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
    /// is shown before the next turn is announced.
    /// </summary>
    public class MemoryCardsOnlineController : OnlineGameController
    {
        /// <summary>The seconds a turn may last, for the lobby; the server takes 5 to 120.</summary>
        private static readonly int[] TurnSeconds = { 10, 15, 20, 30, 45 };
        private const int DefaultTurnChoice = 2;

        private readonly List<MemoryCardsGameManager.OnlineFlip> arrived = new List<MemoryCardsGameManager.OnlineFlip>();
        private readonly Dictionary<byte, int> seatIndex = new Dictionary<byte, int>();
        private RoomOptionSpec[] optionSpecs;
        /// <summary>A turn the server began, handed to the manager after the flips that came with it.</summary>
        private RoomTurnInfo pendingTurn;

        public MemoryCardsGameManager MemoryCards => BaseManager as MemoryCardsGameManager;

        private GameServerClient Client => Server as GameServerClient;

        public override int MaxPlayersLimit => VersusMatch.MaxPlayers;

        public override int MinPlayersLimit => VersusMatch.MinPlayers;

        /// <summary>The boards of the campaign and free play; the endless run is a game for one.</summary>
        public override IReadOnlyList<RoomLevelChoice> LevelChoices
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
                        choices.Add(new RoomLevelChoice(i, $"{level.Title} ({rules.Columns}x{rules.Rows})"));
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
            string set = room.Option(BoardOptions.MatchKey, 2) >= 3 ? "triplets" : "pairs";
            return $"{base.DescribeRoom(room)}, {set}";
        }

        /// <summary>The local player flips a card: the server checks the turn and answers with a flip event.</summary>
        public void Flip(int card)
        {
            if (IsPlaying && Client != null && Client.Connection != null)
            {
                Client.Connection.Reducers.FlipCard((ushort)card);
            }
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

        /// <summary>The server dealt: the manager gets the board and the seats, then loads the level and starts.</summary>
        protected override void OnRoomStarted(RoomInfo room)
        {
            MemoryCardsGameManager manager = MemoryCards;
            DbConnection connection = Client != null ? Client.Connection : null;
            MemoryBoard dealt = connection != null ? connection.Db.MemoryBoard.RoomId.Find(room.Id) : null;
            if (manager == null || dealt == null)
            {
                Report("The board of the game did not arrive.");
                return;
            }

            seatIndex.Clear();
            var names = new List<string>();
            int localSeat = -1;
            foreach (RoomMemberInfo member in Server.Members)
            {
                if (Server.IsLocal(member))
                {
                    localSeat = names.Count;
                }
                seatIndex[member.Seat] = names.Count;
                names.Add(NameOf(member));
            }
            if (names.Count < VersusMatch.MinPlayers || localSeat < 0)
            {
                // The other player dropped between the start and its arrival here; the server is ending the game already.
                Report("The other player left before the game began.");
                return;
            }
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
                LocalSeat = localSeat,
                Preview = shown != null ? shown.Cards : null
            });
            base.OnRoomStarted(room);
            PassScores();
        }

        /// <summary>The flips of the tick go to the manager in the order of the server, and a turn that came with them after them.</summary>
        protected override void OnServerTick()
        {
            MemoryCardsGameManager manager = MemoryCards;
            if (manager == null || !manager.IsOnlineVersus)
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
                flip.Seat = seatIndex.TryGetValue((byte)flip.Seat, out int seat) ? seat : -1;
                manager.OnlineFlipArrived(flip);
            }
            arrived.Clear();
            if (pendingTurn != null)
            {
                RoomTurnInfo turn = pendingTurn;
                pendingTurn = null;
                if (seatIndex.TryGetValue(turn.Seat, out int turnSeat))
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
            if (manager == null || !manager.IsOnlineVersus)
            {
                return;
            }
            var present = new HashSet<int>();
            foreach (RoomMemberInfo member in Server.Members)
            {
                if (seatIndex.TryGetValue(member.Seat, out int seat))
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
