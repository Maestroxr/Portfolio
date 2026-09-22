using System.Collections.Generic;
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
    /// level of the campaign, whose board the host's client writes into the options of the room. The server deals
    /// that board and judges every flip (Server/Lib.cs), so the faces of the cards stay on the server until they turn;
    /// the flips come back as events, which the manager plays like the flips of a local game
    /// (MemoryCardsGameManager.Versus.cs).
    /// </summary>
    public class MemoryCardsOnlineController : OnlineGameController
    {
        private static readonly string[] TurnSeconds = { "10", "15", "20", "30", "45" };

        private readonly List<MemoryCardsGameManager.OnlineFlip> arrived = new List<MemoryCardsGameManager.OnlineFlip>();
        private readonly Dictionary<byte, int> seatIndex = new Dictionary<byte, int>();
        private MemoryCardsGameManager.OnlineFlip preview;
        private RoomOptionSpec[] optionSpecs;

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
            optionSpecs ??= new[] { new RoomOptionSpec("turn", "Seconds a turn", TurnSeconds, null, 2) };

        private MemoryCardsCampaign Campaign => BaseManager != null ? BaseManager.Campaign as MemoryCardsCampaign : null;

        /// <summary>
        /// The options of a room: what the host picked in the lobby plus the board of the level, which the server deals
        /// from these numbers (it does not know the levels of the game).
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
            string turn = RoomOptions.Value(picked, "turn") ?? TurnSeconds[2];
            return RoomOptions.Write("turn", turn, "cards", rules.Cards, "columns", rules.Columns, "match", rules.MatchSize,
                "wilds", rules.Wilds, "bombs", rules.Bombs, "peeks", rules.Peeks, "frozen", rules.Frozen,
                "preview", Mathf.RoundToInt(rules.PreviewTime * 10f), "animals", campaign.AnimalPool(board).Count);
        }

        public override string DescribeRoom(RoomInfo room)
        {
            string set = room.Option("match", 2) >= 3 ? "triplets" : "pairs";
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
                    Outcome = row.Outcome,
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
            var frozen = new bool[dealt.Cards];
            foreach (BoardCard card in connection.Db.BoardCard.RoomId.Filter(room.Id))
            {
                if (card.Index < frozen.Length)
                {
                    frozen[card.Index] = card.Frozen;
                }
            }
            // The board shown for memorizing came with the start; the flips of this game follow.
            MemoryCardsGameManager.OnlineFlip shown = arrived.FindLast(flip => flip.Outcome == MemoryCardsGameManager.OnlineFlip.Preview);
            arrived.Clear();
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

        protected override void OnServerTick()
        {
            if (arrived.Count == 0)
            {
                return;
            }
            MemoryCardsGameManager manager = MemoryCards;
            if (manager == null || !manager.IsOnlineVersus)
            {
                // The preview of a game that is about to start waits for OnRoomStarted; anything else is stale.
                arrived.RemoveAll(flip => flip.Outcome != MemoryCardsGameManager.OnlineFlip.Preview);
                return;
            }
            foreach (MemoryCardsGameManager.OnlineFlip flip in arrived)
            {
                if (flip.Outcome == MemoryCardsGameManager.OnlineFlip.Preview)
                {
                    continue;
                }
                flip.Seat = seatIndex.TryGetValue((byte)flip.Seat, out int seat) ? seat : -1;
                manager.OnlineFlipArrived(flip);
            }
            arrived.Clear();
        }

        protected override void OnTurnChanged(RoomTurnInfo turn)
        {
            if (turn != null && seatIndex.TryGetValue(turn.Seat, out int seat))
            {
                MemoryCards?.OnlineTurn(seat, (int)turn.Number);
            }
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
