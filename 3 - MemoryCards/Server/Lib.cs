using Portfolio.MemoryCards;
using SpacetimeDB;

/// <summary>
/// The server module of Memory Cards: the Gamebox base server (users and login, rooms with their members, turns
/// and the turn timer; see the BaseServer folder of this project) plus the game itself. Both declare the same
/// partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// A versus game is dealt and judged here with the rules of the game itself: the model of the Unity project
/// (Assets/Scripts/Model, shown under Model: <see cref="Dealer"/>, <see cref="MemoryRound"/>, <see cref="VersusMatch"/>,
/// <see cref="BoardOptions"/>) is compiled into this module, so the client and the server cannot drift apart. For
/// every flip the board is rebuilt from the tables, the round judges the flip, the versus rules say what it means for
/// the player, and what changed goes back into the tables and out to the clients as a <see cref="FlipEvent"/>. The
/// faces of a board are the private <see cref="CardDeck"/>; the public <see cref="BoardCard"/> rows say where each
/// card stands and show a face only while the card is face up, so nobody can look at a card they have not turned. The
/// turn passes with the base server's turns: a set lets the player go on, a mistake shows for a moment (the clock
/// stops) and passes the turn, a bomb passes it at once, and so does running out of time. The score is kept here and
/// nowhere else: the reports and the early end the base server offers are refused.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor,
/// or <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    /// <summary>The kind of a card on the wire while it is face down.</summary>
    public const byte KindUnknown = (byte)CardKind.Unknown;

    public const int DefaultTurnSeconds = 20;
    public const int MinTurnSeconds = 5;
    public const int MaxTurnSeconds = 120;

    /// <summary>Seconds a mistake stays face up before it turns back and the turn passes.</summary>
    private const double MismatchSeconds = 1.4;
    /// <summary>Seconds the clients take to deal a card, and to get the round going around that.</summary>
    private const double DealSecondsPerCard = 0.04;
    private const double DealSeconds = 2.2;
    private const double PreviewExtraSeconds = 1.8;
    /// <summary>Seconds the clients take to show a peek; the turn that goes on after it gets them on top.</summary>
    private const uint PeekSeconds = 3;

    // ---------------------------------------------------------------------------------------------------
    // Tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The board of a room that plays: its shape, how far the players got, and the run of the current player.</summary>
    [SpacetimeDB.Table(Accessor = "MemoryBoard", Public = true)]
    public partial struct MemoryBoard
    {
        [SpacetimeDB.PrimaryKey]
        public ulong RoomId;

        public ushort Cards;
        public byte Columns;
        /// <summary>Cards of one animal that make a set: 2 for pairs, 3 for triplets.</summary>
        public byte MatchSize;
        public ushort Sets;
        public ushort MatchedSets;

        /// <summary>Seconds every card is shown before the first turn; 0 for none.</summary>
        public float PreviewSeconds;

        /// <summary>Sets in a row of the player whose turn it is; multiplies the points of the next one.</summary>
        public uint Combo;

        /// <summary>The face up cards are a mistake that turns back in a moment; nobody can flip until then.</summary>
        public bool MismatchShowing;

        /// <summary>Counts the flips of the game. A timer only turns back the mistake it was set for.</summary>
        public uint Flips;
    }

    /// <summary>One card of a board, as everybody may see it: the face is only there while the card is face up.</summary>
    [SpacetimeDB.Table(Accessor = "BoardCard", Public = true)]
    public partial struct BoardCard
    {
        [SpacetimeDB.PrimaryKey]
        [SpacetimeDB.AutoInc]
        public ulong Id;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        /// <summary>Position of the card on the board, row by row.</summary>
        public ushort Index;

        /// <summary>The client's <see cref="CardState"/>.</summary>
        public byte State;

        /// <summary>A frozen card takes one flip to crack the ice before it turns.</summary>
        public bool Frozen;

        /// <summary>The client's <see cref="CardKind"/>; <see cref="KindUnknown"/> while the card is face down.</summary>
        public byte Kind;

        /// <summary>The animal of the card, as its index in the client's pool; -1 while face down and for special cards.</summary>
        public int Animal;

        /// <summary>The seat that matched the card, or <see cref="NoSeat"/>.</summary>
        public byte MatchedBy;
    }

    /// <summary>The faces of a board, by card index. Private: a client learns a face when the card turns.</summary>
    [SpacetimeDB.Table(Accessor = "CardDeck")]
    public partial struct CardDeck
    {
        [SpacetimeDB.PrimaryKey]
        public ulong RoomId;

        public List<byte> Kinds;

        /// <summary>The animal of every card as its index in the client's pool; -1 for special cards.</summary>
        public List<int> Animals;
    }

    /// <summary>
    /// What a flip did, for the clients to play: an event table, so the rows are sent to the players of the room
    /// and never stored.
    /// </summary>
    [SpacetimeDB.Table(Accessor = "FlipEvent", Public = true, Event = true)]
    public partial struct FlipEvent
    {
        public ulong RoomId;

        /// <summary>The player it happened to.</summary>
        public byte Seat;

        /// <summary>The card that was flipped; meaningless for cards turning back and the preview.</summary>
        public ushort Card;

        /// <summary>The client's <see cref="FlipOutcome"/>, the server's own kinds (cards turning back, time up, the preview) included.</summary>
        public byte Outcome;

        /// <summary>Points won, or lost to a bomb.</summary>
        public int Points;

        public uint Combo;

        /// <summary>The cards the outcome is about (the set, the mistake, the cards a peek shows), with their faces.</summary>
        public List<ushort> Cards;
        public List<byte> Kinds;
        public List<int> Animals;
    }

    /// <summary>Turns a mistake back after a moment. Private: SpacetimeDB calls <see cref="HideMismatch"/> with the row.</summary>
    [SpacetimeDB.Table(Accessor = "MismatchTimer", Scheduled = nameof(HideMismatch), ScheduledAt = nameof(ScheduledAt))]
    public partial struct MismatchTimer
    {
        [SpacetimeDB.PrimaryKey]
        [SpacetimeDB.AutoInc]
        public ulong ScheduledId;

        public ScheduleAt ScheduledAt;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        public uint Flips;
    }

    /// <summary>Begins the first turn once the clients dealt (and showed) the board.</summary>
    [SpacetimeDB.Table(Accessor = "PlayTimer", Scheduled = nameof(BeginPlay), ScheduledAt = nameof(ScheduledAt))]
    public partial struct PlayTimer
    {
        [SpacetimeDB.PrimaryKey]
        [SpacetimeDB.AutoInc]
        public ulong ScheduledId;

        public ScheduleAt ScheduledAt;

        [SpacetimeDB.Index.BTree]
        public ulong RoomId;

        public uint Round;
    }

    /// <summary>What a player did in versus games over time. A game given up counts as one that was not won.</summary>
    [SpacetimeDB.Table(Accessor = "MemoryStats", Public = true)]
    public partial struct MemoryStats
    {
        [SpacetimeDB.PrimaryKey]
        public Identity Player;

        public uint Games;
        public uint Wins;
        public uint Sets;
        public long BestScore;
    }

    /// <summary>A card of an event: its index and the face the clients may see.</summary>
    private readonly record struct Face(ushort Index, byte Kind, int Animal);

    // ---------------------------------------------------------------------------------------------------
    // Reducers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The player whose turn it is flips a card: the rules of the game judge it (<see cref="MemoryRound.Flip"/>, <see cref="VersusMatch.Judge"/>).</summary>
    [SpacetimeDB.Reducer]
    public static void FlipCard(ReducerContext ctx, ushort index)
    {
        var member = RequirePlayingMember(ctx, out var room);
        RequireTurn(ctx, member);
        var board = ctx.Db.MemoryBoard.RoomId.Find(room.Id) ?? throw new Exception("There is no board.");
        if (board.MismatchShowing)
        {
            throw new Exception("Wait for the cards to turn back.");
        }
        var deck = ctx.Db.CardDeck.RoomId.Find(room.Id) ?? throw new Exception("There is no deck.");
        var round = LoadRound(ctx, room, board, deck);
        if (index >= round.Cards.Count)
        {
            throw new Exception("There is no such card.");
        }
        var card = round.Cards[index];
        if (card.State != CardState.Hidden)
        {
            throw new Exception("This card is face up already.");
        }

        var result = round.Flip(card);
        var outcome = VersusMatch.Judge(result, (int)Math.Min(member.Score, int.MaxValue), round);
        board.Flips++;
        board.Combo = (uint)round.Combo;
        board.MatchedSets = (ushort)round.MatchedSets;
        board.MismatchShowing = result.IsMistake;
        ctx.Db.MemoryBoard.RoomId.Update(board);

        // What the flip changed: the card, its set or its mistake, and a half finished set a bomb took back.
        var changed = new List<MemoryCard> { card };
        changed.AddRange(result.Cards);
        changed.AddRange(result.FlippedBack);
        Store(ctx, room.Id, deck, changed, member.Seat);

        member = AddScore(ctx, member, outcome.Points);
        if (outcome.SetCompleted)
        {
            var stats = StatsOf(ctx, member.Identity);
            stats.Sets++;
            ctx.Db.MemoryStats.Player.Update(stats);
        }

        // The event names the cards it is about, with their faces: the flipped card, its set or mistake, or what a peek shows.
        List<MemoryCard> shown = result.Outcome switch
        {
            FlipOutcome.Cracked => new List<MemoryCard>(),
            FlipOutcome.Peek => round.Cards.Where(other => other == card || other.IsHidden).ToList(),
            FlipOutcome.Revealed or FlipOutcome.Bomb => new List<MemoryCard> { card },
            _ => result.Cards,
        };
        Emit(ctx, room.Id, member.Seat, index, result.Outcome, outcome.Points, (uint)round.Combo, Faces(deck, shown));
        if (result.FlippedBack.Count > 0)
        {
            Emit(ctx, room.Id, member.Seat, index, FlipOutcome.FlippedBack, 0, 0, Faces(deck, result.FlippedBack, hidden: true));
        }

        if (round.IsCleared)
        {
            FinishRoom(ctx, room);
        }
        else if (result.IsMistake)
        {
            // The mistake shows for a moment, with the clock stopped: the turn is decided, and passes when it turns back.
            BeginTurn(ctx, room, member.Seat, 0);
            ctx.Db.MismatchTimer.Insert(new MismatchTimer
            {
                ScheduledAt = new ScheduleAt.Time(ctx.Timestamp + TimeDuration.FromSeconds(MismatchSeconds)),
                RoomId = room.Id,
                Flips = board.Flips,
            });
        }
        else if (outcome.PassesNow)
        {
            NextTurn(ctx, room);
        }
        else if (outcome.FreshClock)
        {
            // A set or a peek earns the whole turn again; a peek is watched first, which costs the player nothing.
            BeginTurn(ctx, room, member.Seat, room.TurnSeconds + (result.Outcome == FlipOutcome.Peek ? PeekSeconds : 0));
        }
        // A card waiting for the rest of its set, or cracked ice: the turn runs on.
    }

    /// <summary>Called by SpacetimeDB a moment after a mistake: the cards turn back and the next player is up.</summary>
    [SpacetimeDB.Reducer]
    public static void HideMismatch(ReducerContext ctx, MismatchTimer timer)
    {
        if (ctx.Sender != ctx.DatabaseIdentity)
        {
            throw new Exception("Only the server turns the cards back.");
        }
        if (ctx.Db.MemoryBoard.RoomId.Find(timer.RoomId) is not { MismatchShowing: true } board || board.Flips != timer.Flips)
        {
            return;
        }
        if (ctx.Db.Room.Id.Find(timer.RoomId) is not { State: RoomState.Playing } room)
        {
            return;
        }
        var seat = TurnOf(ctx, room.Id)?.Seat ?? NoSeat;
        TurnBack(ctx, room, board, seat, FlipOutcome.FlippedBack);
        NextTurn(ctx, room);
    }

    /// <summary>Called by SpacetimeDB once the clients dealt the board: the first turn begins.</summary>
    [SpacetimeDB.Reducer]
    public static void BeginPlay(ReducerContext ctx, PlayTimer timer)
    {
        if (ctx.Sender != ctx.DatabaseIdentity)
        {
            throw new Exception("Only the server begins the game.");
        }
        if (ctx.Db.Room.Id.Find(timer.RoomId) is not { State: RoomState.Playing } room || room.Round != timer.Round
            || TurnOf(ctx, room.Id) is not null)
        {
            return;
        }
        // Another player starts every game of a room.
        var members = MembersOf(ctx, room.Id).Where(member => member.Playing).ToList();
        if (members.Count > 0)
        {
            BeginTurn(ctx, room, members[(int)((room.Round - 1) % (uint)members.Count)].Seat);
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Extension points of the base server
    // ---------------------------------------------------------------------------------------------------

    static partial void ConfigureRoom(ReducerContext ctx, ref Room room, ref string? error)
    {
        room.MinPlayers = VersusMatch.MinPlayers;
        room.MaxPlayers = Math.Clamp(room.MaxPlayers, (byte)VersusMatch.MinPlayers, (byte)VersusMatch.MaxPlayers);
        // A clock there always is: without one an idle player would hold the game forever.
        room.TurnSeconds = (uint)Option(room.Options, TurnOption, DefaultTurnSeconds, MinTurnSeconds, MaxTurnSeconds);
        var rules = RulesOf(room, out var animals);
        if (!rules.IsValid(animals, out var message))
        {
            error = message;
        }
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        var rules = RulesOf(room, out var animals);
        var deal = Dealer.Create(rules, animals, ctx.Rng);
        var deck = ctx.Db.CardDeck.Insert(new CardDeck
        {
            RoomId = room.Id,
            Kinds = deal.Cards.Select(card => (byte)card.Kind).ToList(),
            Animals = deal.Cards.Select(card => card.IsAnimal ? deal.Animals[card.Animal] : -1).ToList(),
        });
        ctx.Db.MemoryBoard.Insert(new MemoryBoard
        {
            RoomId = room.Id,
            Cards = (ushort)deal.Cards.Count,
            Columns = (byte)rules.Columns,
            MatchSize = (byte)rules.MatchSize,
            Sets = (ushort)deal.Animals.Count,
            PreviewSeconds = rules.PreviewTime,
        });
        foreach (var card in deal.Cards)
        {
            ctx.Db.BoardCard.Insert(new BoardCard
            {
                RoomId = room.Id,
                Index = (ushort)card.Id,
                State = (byte)CardState.Hidden,
                Frozen = card.Frozen,
                Kind = KindUnknown,
                Animal = -1,
                MatchedBy = NoSeat,
            });
        }

        var wait = DealSeconds + deal.Cards.Count * DealSecondsPerCard;
        if (rules.PreviewTime > 0f)
        {
            // Memorize: everybody sees every card for a moment, so the faces go out once, for that.
            Emit(ctx, room.Id, NoSeat, 0, FlipOutcome.Preview, 0, 0, Faces(deck, deal.Cards));
            wait += rules.PreviewTime + PreviewExtraSeconds;
        }
        ctx.Db.PlayTimer.Insert(new PlayTimer
        {
            ScheduledAt = new ScheduleAt.Time(ctx.Timestamp + TimeDuration.FromSeconds(wait)),
            RoomId = room.Id,
            Round = room.Round,
        });
    }

    static partial void OnTurnTimedOut(ReducerContext ctx, Room room, RoomTurn turn, ref bool handled)
    {
        // Out of time: what the player had face up turns back, and the base server passes the turn.
        if (ctx.Db.MemoryBoard.RoomId.Find(room.Id) is { } board)
        {
            TurnBack(ctx, room, board, turn.Seat, FlipOutcome.TimedOut);
        }
    }

    static partial void OnMemberLeft(ReducerContext ctx, Room room, RoomMember member)
    {
        if (room.State != RoomState.Playing)
        {
            return;
        }
        // Whoever leaves a running game gives it up: a game played, not won. The others play on (or win, when alone).
        var stats = StatsOf(ctx, member.Identity);
        stats.Games++;
        ctx.Db.MemoryStats.Player.Update(stats);
        // A player who leaves on their turn takes no cards along; the base server passes the turn.
        if (TurnOf(ctx, room.Id) is { } turn && turn.Seat == member.Seat && ctx.Db.MemoryBoard.RoomId.Find(room.Id) is { } board)
        {
            TurnBack(ctx, room, board, member.Seat, FlipOutcome.FlippedBack);
        }
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        // The places go by the rules of the game (score, then sets, then seat: VersusMatch.Compare), the same the
        // results of the clients show; the base server ranked by score alone. A draw is a game nobody won.
        var members = RankMembers(ctx, room);
        var winners = members.Count(member => member.Place == 1);
        foreach (var member in members)
        {
            var stats = StatsOf(ctx, member.Identity);
            stats.Games++;
            if (member.Place == 1 && winners == 1)
            {
                stats.Wins++;
            }
            stats.BestScore = Math.Max(stats.BestScore, member.Score);
            ctx.Db.MemoryStats.Player.Update(stats);
        }
    }

    static partial void OnRoomCleared(ReducerContext ctx, Room room)
    {
        ctx.Db.MismatchTimer.RoomId.Delete(room.Id);
        ctx.Db.PlayTimer.RoomId.Delete(room.Id);
        ctx.Db.BoardCard.RoomId.Delete(room.Id);
        ctx.Db.MemoryBoard.RoomId.Delete(room.Id);
        ctx.Db.CardDeck.RoomId.Delete(room.Id);
    }

    static partial void OnUserDeleted(ReducerContext ctx, User user)
    {
        ctx.Db.MemoryStats.Player.Delete(user.Identity);
    }

    static partial void ValidateAction(ReducerContext ctx, Room room, RoomMember member, ref RoomAction action, ref bool decisive, ref string? error)
    {
        // Every move of this game is a flip the server judges: the action log has no place in it.
        error = "Memory Cards is played with flips, not actions.";
    }

    static partial void ValidateScore(ReducerContext ctx, Room room, RoomMember member, bool final, ref long score, ref string? error)
    {
        error = "The server keeps the score of a Memory Cards game.";
    }

    static partial void ValidateEndRoom(ReducerContext ctx, Room room, RoomMember host, ref string? error)
    {
        error = "A game of Memory Cards plays to the last set. Leave it to give it up.";
    }

    // ---------------------------------------------------------------------------------------------------
    // The board in the tables
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The rules of the board a room plays, as its host wrote them into the options, and the animals to deal from.</summary>
    private static RoundRules RulesOf(Room room, out int animals)
    {
        var options = room.Options;
        return BoardOptions.Read((key, fallback) => Option(options, key, fallback, 0, BoardOptions.MaxValue), out animals);
    }

    /// <summary>
    /// The round of a room as the rules see it right now: the cards from the rows and the deck (a face up card is one
    /// the player is turning), the combo of the current run from the board.
    /// </summary>
    private static MemoryRound LoadRound(ReducerContext ctx, Room room, MemoryBoard board, CardDeck deck)
    {
        var rules = RulesOf(room, out _);
        var rows = CardsOf(ctx, room.Id).ToDictionary(row => row.Index);
        var deal = new Deal();
        // The round counts the animals of the board from 0; the deck knows them by their index in the client's pool.
        deal.Animals.AddRange(deck.Animals.Where(animal => animal >= 0).Distinct().OrderBy(animal => animal));
        for (var i = 0; i < deck.Kinds.Count; i++)
        {
            rows.TryGetValue((ushort)i, out var row);
            deal.Cards.Add(new MemoryCard
            {
                Id = i,
                Slot = i,
                Kind = (CardKind)deck.Kinds[i],
                Animal = deck.Animals[i] >= 0 ? deal.Animals.IndexOf(deck.Animals[i]) : -1,
                State = (CardState)row.State,
                Frozen = row.Frozen,
            });
        }
        return new MemoryRound(rules, deal, 0, (int)board.Combo);
    }

    /// <summary>Writes <paramref name="cards"/> of the round back into their rows: state, ice, the face while the card is up, who matched it.</summary>
    private static void Store(ReducerContext ctx, ulong roomId, CardDeck deck, IEnumerable<MemoryCard> cards, byte seat)
    {
        var rows = CardsOf(ctx, roomId).ToDictionary(row => row.Index);
        foreach (var card in cards.Distinct())
        {
            if (!rows.TryGetValue((ushort)card.Id, out var row))
            {
                continue;
            }
            row.State = (byte)card.State;
            row.Frozen = card.Frozen;
            row.Kind = card.IsHidden ? KindUnknown : deck.Kinds[card.Id];
            row.Animal = card.IsHidden ? -1 : deck.Animals[card.Id];
            if (card.State == CardState.Matched && row.MatchedBy == NoSeat)
            {
                row.MatchedBy = seat;
            }
            rows[row.Index] = ctx.Db.BoardCard.Id.Update(row);
        }
    }

    /// <summary>Turns the face up cards that are not matched back down, faces gone, and ends the run of the player.</summary>
    private static void TurnBack(ReducerContext ctx, Room room, MemoryBoard board, byte seat, FlipOutcome outcome)
    {
        if (ctx.Db.CardDeck.RoomId.Find(room.Id) is not { } deck)
        {
            return;
        }
        var turned = LoadRound(ctx, room, board, deck).HideRevealed();
        ctx.Db.MismatchTimer.RoomId.Delete(room.Id);
        board.MismatchShowing = false;
        board.Combo = 0;
        ctx.Db.MemoryBoard.RoomId.Update(board);
        Store(ctx, room.Id, deck, turned, seat);
        if (turned.Count > 0 || outcome == FlipOutcome.TimedOut)
        {
            Emit(ctx, room.Id, seat, 0, outcome, 0, 0, Faces(deck, turned, hidden: true));
        }
    }

    /// <summary>The sets a seat found: the animal cards it matched make sets of the board's size (a wild card takes a whole set).</summary>
    private static int SetsOf(ReducerContext ctx, ulong roomId, byte seat, int matchSize)
    {
        var cards = CardsOf(ctx, roomId).Count(card => card.MatchedBy == seat && card.Kind == (byte)CardKind.Animal);
        return matchSize > 0 ? cards / matchSize : 0;
    }

    /// <summary>
    /// Gives the members of a finished game their places by the rules of the game (<see cref="VersusMatch.Compare"/>,
    /// equal results share a place) and returns them.
    /// </summary>
    private static List<RoomMember> RankMembers(ReducerContext ctx, Room room)
    {
        var members = MembersOf(ctx, room.Id);
        var matchSize = ctx.Db.MemoryBoard.RoomId.Find(room.Id)?.MatchSize ?? 2;
        var seats = members.ToDictionary(member => member.Identity, member => new VersusSeat
        {
            Seat = member.Seat,
            Score = (int)Math.Min(member.Score, int.MaxValue),
            Sets = SetsOf(ctx, room.Id, member.Seat, matchSize),
        });
        members.Sort((a, b) => VersusMatch.Compare(seats[a.Identity], seats[b.Identity]));
        uint place = 0;
        for (var i = 0; i < members.Count; i++)
        {
            if (i == 0 || !VersusMatch.SharePlace(seats[members[i - 1].Identity], seats[members[i].Identity]))
            {
                place = (uint)i + 1;
            }
            var member = members[i];
            member.Place = place;
            members[i] = ctx.Db.RoomMember.Identity.Update(member);
        }
        return members;
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    private static IEnumerable<BoardCard> CardsOf(ReducerContext ctx, ulong roomId) => ctx.Db.BoardCard.RoomId.Filter(roomId);

    /// <summary>The faces of <paramref name="cards"/> for an event, or none of them when they turned back <paramref name="hidden"/>.</summary>
    private static List<Face> Faces(CardDeck deck, IEnumerable<MemoryCard> cards, bool hidden = false)
    {
        return cards.Select(card => new Face((ushort)card.Id, hidden ? KindUnknown : deck.Kinds[card.Id], hidden ? -1 : deck.Animals[card.Id])).ToList();
    }

    private static void Emit(ReducerContext ctx, ulong roomId, byte seat, ushort flipped, FlipOutcome outcome, int points, uint combo, List<Face> faces)
    {
        ctx.Db.FlipEvent.Insert(new FlipEvent
        {
            RoomId = roomId,
            Seat = seat,
            Card = flipped,
            Outcome = (byte)outcome,
            Points = points,
            Combo = combo,
            Cards = faces.Select(face => face.Index).ToList(),
            Kinds = faces.Select(face => face.Kind).ToList(),
            Animals = faces.Select(face => face.Animal).ToList(),
        });
    }

    private static MemoryStats StatsOf(ReducerContext ctx, Identity player) =>
        ctx.Db.MemoryStats.Player.Find(player) ?? ctx.Db.MemoryStats.Insert(new MemoryStats { Player = player });
}
