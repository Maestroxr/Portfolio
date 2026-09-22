using SpacetimeDB;

/// <summary>
/// The server module of Memory Cards: the Gamebox base server (users and login, rooms with their members, turns
/// and the turn timer; see the BaseServer folder of this project) plus the game itself. Both declare the same
/// partial class, so the tables, reducers and helpers of the base server are in reach here.
///
/// A versus game is dealt and judged here, so nobody can look at a card they have not turned. The faces of a
/// board are the private <see cref="CardDeck"/>; the public <see cref="BoardCard"/> rows say where each card
/// stands and show a face only while the card is face up. The players call <see cref="FlipCard"/> on their
/// turn; what a flip did goes out as a <see cref="FlipEvent"/> for the clients to animate, the score goes to
/// the member of the base server, and the turn passes with the base server's turns: a set lets the player go
/// on, a mistake shows for a moment and passes the turn, and so does running out of time.
///
/// After a change: publish the module, then generate the client bindings (Gamebox > Server in the Unity editor,
/// or <c>spacetime publish</c> and <c>spacetime generate</c> in the project folder, which read spacetime.json).
/// </summary>
public static partial class Module
{
    // The numbers of the client's CardKind, CardState and FlipOutcome enums.
    public const byte KindAnimal = 0;
    public const byte KindWild = 1;
    public const byte KindBomb = 2;
    public const byte KindPeek = 4;
    public const byte KindUnknown = 255;

    public const byte StateHidden = 0;
    public const byte StateRevealed = 1;
    public const byte StateMatched = 2;
    public const byte StateSpent = 3;

    public const byte OutcomeCracked = 1;
    public const byte OutcomeRevealed = 2;
    public const byte OutcomeMatched = 3;
    public const byte OutcomeMismatched = 4;
    public const byte OutcomeWildMatched = 6;
    public const byte OutcomeBomb = 7;
    public const byte OutcomePeek = 9;
    /// <summary>A mistake turned back face down.</summary>
    public const byte OutcomeFlippedBack = 20;
    /// <summary>The player ran out of time; the cards they had face up turned back.</summary>
    public const byte OutcomeTimedOut = 21;
    /// <summary>The board is shown to everybody before the first turn (memorize).</summary>
    public const byte OutcomePreview = 22;

    public const int PointsPerPair = 100;
    public const int PointsPerTriple = 160;
    public const int MaxComboMultiplier = 5;
    public const int WildBonus = 50;
    public const int PeekPoints = 25;
    public const int BombPoints = 50;

    /// <summary>Seconds a mistake stays face up before it turns back and the turn passes.</summary>
    private const double MismatchSeconds = 1.4;
    /// <summary>Seconds the clients take to deal a card, and to get the round going around that.</summary>
    private const double DealSecondsPerCard = 0.04;
    private const double DealSeconds = 2.2;
    private const double PreviewExtraSeconds = 1.8;

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

        public byte State;

        /// <summary>A frozen card takes one flip to crack the ice before it turns.</summary>
        public bool Frozen;

        /// <summary><see cref="KindUnknown"/> while the card is face down.</summary>
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

    /// <summary>What a player did in versus games over time.</summary>
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

    // ---------------------------------------------------------------------------------------------------
    // The rules of a board
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The board a room plays, as the host's client wrote it into the options of the room.</summary>
    private readonly struct BoardRules
    {
        public readonly int Cards, Columns, MatchSize, Wilds, Bombs, Peeks, Frozen, Animals;
        public readonly float Preview;

        public BoardRules(string? options)
        {
            Cards = Option(options, "cards", 16, 0, 1000);
            Columns = Option(options, "columns", 4, 0, 1000);
            MatchSize = Option(options, "match", 2, 0, 1000);
            Wilds = Option(options, "wilds", 0, 0, 1000);
            Bombs = Option(options, "bombs", 0, 0, 1000);
            Peeks = Option(options, "peeks", 0, 0, 1000);
            Frozen = Option(options, "frozen", 0, 0, 1000);
            Animals = Option(options, "animals", 30, 0, 1000);
            // Tenths of a second, to keep the options whole numbers.
            Preview = Option(options, "preview", 0, 0, 300) / 10f;
        }

        public int AnimalCards => Cards - Wilds - Bombs - Peeks;

        public int Sets => MatchSize > 0 ? AnimalCards / MatchSize : 0;

        public string? Error()
        {
            if (MatchSize < 2 || MatchSize > 4)
            {
                return "Sets are made of 2, 3 or 4 cards.";
            }
            if (Columns < 1 || Columns > 10)
            {
                return "A board has 1 to 10 cards in a row.";
            }
            if (Cards < MatchSize || Cards > 60)
            {
                return "A board has up to 60 cards.";
            }
            if (Cards % Columns != 0)
            {
                return "The cards do not fill the rows of the board.";
            }
            if (AnimalCards < MatchSize || AnimalCards % MatchSize != 0)
            {
                return "The animal cards do not make whole sets.";
            }
            if (Sets > Animals)
            {
                return $"The board needs {Sets} different animals but there are only {Animals}.";
            }
            if (Frozen > AnimalCards)
            {
                return "More frozen cards than animal cards.";
            }
            return null;
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Reducers
    // ---------------------------------------------------------------------------------------------------

    /// <summary>The player whose turn it is flips a card.</summary>
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
        var card = CardAt(ctx, room.Id, index) ?? throw new Exception("There is no such card.");
        if (card.State != StateHidden)
        {
            throw new Exception("This card is face up already.");
        }
        var deck = ctx.Db.CardDeck.RoomId.Find(room.Id) ?? throw new Exception("There is no deck.");
        board.Flips++;

        if (card.Frozen)
        {
            card.Frozen = false;
            ctx.Db.BoardCard.Id.Update(card);
            ctx.Db.MemoryBoard.RoomId.Update(board);
            Emit(ctx, room.Id, member.Seat, index, OutcomeCracked, 0, board.Combo, new List<BoardCard>());
            return;
        }

        card.Kind = deck.Kinds[index];
        card.Animal = deck.Animals[index];
        switch (card.Kind)
        {
            case KindBomb:
            {
                card.State = StateSpent;
                card = ctx.Db.BoardCard.Id.Update(card);
                var loss = -(int)Math.Min(member.Score, BombPoints);
                AddScore(ctx, member, loss);
                board.Combo = 0;
                ctx.Db.MemoryBoard.RoomId.Update(board);
                Emit(ctx, room.Id, member.Seat, index, OutcomeBomb, loss, 0, new List<BoardCard> { card });
                NextTurn(ctx, room);
                return;
            }
            case KindPeek:
            {
                card.State = StateSpent;
                ctx.Db.BoardCard.Id.Update(card);
                AddScore(ctx, member, PeekPoints);
                ctx.Db.MemoryBoard.RoomId.Update(board);
                // Everybody gets the same look at the cards that are still face down.
                var hidden = CardsOf(ctx, room.Id).Where(other => other.State == StateHidden).Select(other => WithFace(other, deck)).ToList();
                Emit(ctx, room.Id, member.Seat, index, OutcomePeek, PeekPoints, board.Combo, hidden);
                BeginTurn(ctx, room, member.Seat);
                return;
            }
        }

        card.State = StateRevealed;
        ctx.Db.BoardCard.Id.Update(card);
        Evaluate(ctx, room, board, deck, member, index);
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
        TurnBack(ctx, room.Id, board, seat, OutcomeFlippedBack);
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
        room.MinPlayers = 2;
        room.MaxPlayers = Math.Clamp(room.MaxPlayers, (byte)2, (byte)4);
        room.TurnSeconds = (uint)Option(room.Options, "turn", 20, 5, 120);
        error = new BoardRules(room.Options).Error();
    }

    static partial void OnRoomStarted(ReducerContext ctx, Room room)
    {
        var rules = new BoardRules(room.Options);
        var (kinds, animals, frozen) = Deal(rules, ctx.Rng);
        ctx.Db.CardDeck.Insert(new CardDeck { RoomId = room.Id, Kinds = kinds, Animals = animals });
        ctx.Db.MemoryBoard.Insert(new MemoryBoard
        {
            RoomId = room.Id,
            Cards = (ushort)rules.Cards,
            Columns = (byte)rules.Columns,
            MatchSize = (byte)rules.MatchSize,
            Sets = (ushort)rules.Sets,
            PreviewSeconds = rules.Preview,
        });
        var cards = new List<BoardCard>();
        for (var i = 0; i < rules.Cards; i++)
        {
            cards.Add(ctx.Db.BoardCard.Insert(new BoardCard
            {
                RoomId = room.Id,
                Index = (ushort)i,
                State = StateHidden,
                Frozen = frozen[i],
                Kind = KindUnknown,
                Animal = -1,
                MatchedBy = NoSeat,
            }));
        }

        var wait = DealSeconds + rules.Cards * DealSecondsPerCard;
        if (rules.Preview > 0f)
        {
            // Memorize: everybody sees every card for a moment, so the faces go out once, for that.
            var deck = new CardDeck { RoomId = room.Id, Kinds = kinds, Animals = animals };
            Emit(ctx, room.Id, NoSeat, 0, OutcomePreview, 0, 0, cards.Select(card => WithFace(card, deck)).ToList());
            wait += rules.Preview + PreviewExtraSeconds;
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
            TurnBack(ctx, room.Id, board, turn.Seat, OutcomeTimedOut);
        }
    }

    static partial void OnMemberLeft(ReducerContext ctx, Room room, RoomMember member)
    {
        // A player who leaves on their turn takes no cards along; the base server passes the turn.
        if (room.State == RoomState.Playing && TurnOf(ctx, room.Id) is { } turn && turn.Seat == member.Seat
            && ctx.Db.MemoryBoard.RoomId.Find(room.Id) is { } board)
        {
            TurnBack(ctx, room.Id, board, member.Seat, OutcomeFlippedBack);
        }
    }

    static partial void OnRoomFinished(ReducerContext ctx, Room room)
    {
        foreach (var member in MembersOf(ctx, room.Id))
        {
            var stats = StatsOf(ctx, member.Identity);
            stats.Games++;
            if (member.Place == 1)
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

    // ---------------------------------------------------------------------------------------------------
    // The rules
    // ---------------------------------------------------------------------------------------------------

    /// <summary>A card turned face up: a set, a mistake, or the player goes on turning. The same rules as the client's MemoryRound.</summary>
    private static void Evaluate(ReducerContext ctx, Room room, MemoryBoard board, CardDeck deck, RoomMember member, ushort flipped)
    {
        var revealed = CardsOf(ctx, room.Id).Where(card => card.State == StateRevealed).ToList();
        var wilds = revealed.Count(card => card.Kind == KindWild);
        var animals = revealed.Where(card => card.Kind == KindAnimal).ToList();
        var animal = animals.Count > 0 ? animals[0].Animal : -1;

        if (animals.Any(card => card.Animal != animal))
        {
            board.MismatchShowing = true;
            board.Combo = 0;
            ctx.Db.MemoryBoard.RoomId.Update(board);
            ctx.Db.MismatchTimer.Insert(new MismatchTimer
            {
                ScheduledAt = new ScheduleAt.Time(ctx.Timestamp + TimeDuration.FromSeconds(MismatchSeconds)),
                RoomId = room.Id,
                Flips = board.Flips,
            });
            Emit(ctx, room.Id, member.Seat, flipped, OutcomeMismatched, 0, 0, revealed);
            return;
        }

        List<BoardCard> set;
        var bonus = 0;
        var outcome = OutcomeMatched;
        var completesSet = true;
        if (wilds >= 2 && animals.Count == 0)
        {
            // Two wild cards together cancel out for a bonus; no set of animals is found.
            set = revealed;
            bonus = WildBonus * 2;
            outcome = OutcomeWildMatched;
            completesSet = false;
        }
        else if (wilds > 0 && animals.Count > 0)
        {
            // A wild card takes every card of the animal off the board, the ones still face down included.
            set = revealed;
            for (var i = 0; i < deck.Kinds.Count; i++)
            {
                if (deck.Kinds[i] == KindAnimal && deck.Animals[i] == animal && CardAt(ctx, room.Id, (ushort)i) is { State: StateHidden } other)
                {
                    set.Add(WithFace(other, deck));
                }
            }
            bonus = WildBonus;
            outcome = OutcomeWildMatched;
        }
        else if (wilds == 0 && animals.Count >= board.MatchSize)
        {
            set = revealed;
        }
        else
        {
            ctx.Db.MemoryBoard.RoomId.Update(board);
            Emit(ctx, room.Id, member.Seat, flipped, OutcomeRevealed, 0, board.Combo, revealed.Where(card => card.Index == flipped).ToList());
            return;
        }

        var matched = new List<BoardCard>();
        foreach (var card in set)
        {
            var done = card;
            done.State = StateMatched;
            done.Frozen = false;
            done.MatchedBy = member.Seat;
            matched.Add(ctx.Db.BoardCard.Id.Update(done));
        }
        var points = bonus;
        if (completesSet)
        {
            board.MatchedSets++;
            board.Combo++;
            var basePoints = board.MatchSize >= 3 ? PointsPerTriple : PointsPerPair;
            points += basePoints * (int)Math.Clamp(board.Combo, 1, MaxComboMultiplier);
            var stats = StatsOf(ctx, member.Identity);
            stats.Sets++;
            ctx.Db.MemoryStats.Player.Update(stats);
        }
        ctx.Db.MemoryBoard.RoomId.Update(board);
        AddScore(ctx, member, points);
        Emit(ctx, room.Id, member.Seat, flipped, outcome, points, board.Combo, matched);

        if (board.MatchedSets >= board.Sets)
        {
            FinishRoom(ctx, room);
        }
        else
        {
            // A set earns another turn, with a full clock.
            BeginTurn(ctx, room, member.Seat);
        }
    }

    /// <summary>Turns the face up cards that are not matched back down, faces gone, and ends the run of the player.</summary>
    private static void TurnBack(ReducerContext ctx, ulong roomId, MemoryBoard board, byte seat, byte outcome)
    {
        var turned = new List<BoardCard>();
        foreach (var card in CardsOf(ctx, roomId).Where(card => card.State == StateRevealed))
        {
            var hidden = card;
            hidden.State = StateHidden;
            hidden.Kind = KindUnknown;
            hidden.Animal = -1;
            turned.Add(ctx.Db.BoardCard.Id.Update(hidden));
        }
        ctx.Db.MismatchTimer.RoomId.Delete(roomId);
        board.MismatchShowing = false;
        board.Combo = 0;
        ctx.Db.MemoryBoard.RoomId.Update(board);
        if (turned.Count > 0 || outcome == OutcomeTimedOut)
        {
            Emit(ctx, roomId, seat, 0, outcome, 0, 0, turned);
        }
    }

    /// <summary>Deals a board: the sets, the special cards, the ice, all shuffled. The same steps as the client's Dealer.</summary>
    private static (List<byte> Kinds, List<int> Animals, List<bool> Frozen) Deal(BoardRules rules, Random random)
    {
        var pool = Enumerable.Range(0, rules.Animals).ToList();
        Shuffle(pool, random);
        var cards = new List<(byte Kind, int Animal, bool Frozen)>();
        for (var set = 0; set < rules.Sets; set++)
        {
            for (var copy = 0; copy < rules.MatchSize; copy++)
            {
                cards.Add((KindAnimal, pool[set], false));
            }
        }
        var ice = Enumerable.Range(0, cards.Count).ToList();
        Shuffle(ice, random);
        foreach (var index in ice.Take(rules.Frozen))
        {
            cards[index] = (KindAnimal, cards[index].Animal, true);
        }
        cards.AddRange(Enumerable.Repeat((KindWild, -1, false), rules.Wilds));
        cards.AddRange(Enumerable.Repeat((KindBomb, -1, false), rules.Bombs));
        cards.AddRange(Enumerable.Repeat((KindPeek, -1, false), rules.Peeks));
        Shuffle(cards, random);
        return (cards.Select(card => card.Kind).ToList(), cards.Select(card => card.Animal).ToList(), cards.Select(card => card.Frozen).ToList());
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (var n = list.Count - 1; n > 0; n--)
        {
            var k = random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    // ---------------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------------

    private static IEnumerable<BoardCard> CardsOf(ReducerContext ctx, ulong roomId) => ctx.Db.BoardCard.RoomId.Filter(roomId);

    private static BoardCard? CardAt(ReducerContext ctx, ulong roomId, ushort index)
    {
        foreach (var card in ctx.Db.BoardCard.RoomId.Filter(roomId))
        {
            if (card.Index == index)
            {
                return card;
            }
        }
        return null;
    }

    /// <summary>A card with its face filled in from the deck, for an event; the row of a face down card keeps hiding it.</summary>
    private static BoardCard WithFace(BoardCard card, CardDeck deck)
    {
        card.Kind = deck.Kinds[card.Index];
        card.Animal = deck.Animals[card.Index];
        return card;
    }

    private static void Emit(ReducerContext ctx, ulong roomId, byte seat, ushort flipped, byte outcome, int points, uint combo, List<BoardCard> cards)
    {
        ctx.Db.FlipEvent.Insert(new FlipEvent
        {
            RoomId = roomId,
            Seat = seat,
            Card = flipped,
            Outcome = outcome,
            Points = points,
            Combo = combo,
            Cards = cards.Select(card => card.Index).ToList(),
            Kinds = cards.Select(card => card.Kind).ToList(),
            Animals = cards.Select(card => card.Animal).ToList(),
        });
    }

    private static MemoryStats StatsOf(ReducerContext ctx, Identity player) =>
        ctx.Db.MemoryStats.Player.Find(player) ?? ctx.Db.MemoryStats.Insert(new MemoryStats { Player = player });
}
