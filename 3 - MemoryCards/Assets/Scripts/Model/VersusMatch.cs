using System;
using System.Collections.Generic;

namespace Portfolio.MemoryCards
{
    /// <summary>What one player of a versus game has so far.</summary>
    public sealed class VersusSeat
    {
        public int Seat;
        public string Name;
        public int Score;
        public int Sets;
        public int Mistakes;
        public int BestCombo;
        /// <summary>False once the player left an online game; their turn is skipped.</summary>
        public bool Playing = true;
    }


    /// <summary>What a flip meant for the players of a versus game.</summary>
    public struct VersusOutcome
    {
        /// <summary>The player who flipped.</summary>
        public int Seat;
        /// <summary>Points the player won (or lost, to a bomb: no more than they had).</summary>
        public int Points;
        /// <summary>The player goes on: a set, a special card that does not end the turn, or a set still open.</summary>
        public bool KeepsTurn;
        /// <summary>The turn passes once the mistake is turned back (see <see cref="VersusMatch.PassTurn"/>).</summary>
        public bool PassesAfterMistake;
        /// <summary>The player earned the whole turn again: a set, or a special card.</summary>
        public bool FreshClock;
        /// <summary>A set of animals was completed for the player.</summary>
        public bool SetCompleted;

        /// <summary>The turn passes at once (a bomb): whatever the player had face up goes back with it.</summary>
        public bool PassesNow => !KeepsTurn && !PassesAfterMistake;
    }


    /// <summary>
    /// The rules of several players at one board, on top of the rules of the board itself (<see cref="MemoryRound"/>):
    /// the players take turns, a completed set scores for the player who found it and lets them go on, a mistake or a
    /// bomb passes the turn (a bomb takes a half finished set back with it), and every turn has a time limit. A game on
    /// one device feeds it the results of its round; the server of an online game judges every flip with
    /// <see cref="Judge"/> and ranks the players with <see cref="Compare"/>, the same code (this file is compiled into
    /// the server module), and the clients only mirror the seats here.
    /// </summary>
    public sealed class VersusMatch
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;
        public const float DefaultTurnSeconds = 20f;

        private readonly List<VersusSeat> seats = new List<VersusSeat>();

        public VersusMatch(IEnumerable<string> names, float turnSeconds, int firstSeat = 0)
        {
            foreach (string name in names ?? throw new ArgumentNullException(nameof(names)))
            {
                seats.Add(new VersusSeat { Seat = seats.Count, Name = string.IsNullOrEmpty(name) ? $"Player {seats.Count + 1}" : name });
            }
            if (seats.Count < MinPlayers || seats.Count > MaxPlayers)
            {
                throw new ArgumentException($"A versus game has {MinPlayers} to {MaxPlayers} players.");
            }
            TurnSeconds = Math.Max(0f, turnSeconds);
            Current = Math.Max(0, Math.Min(firstSeat, seats.Count - 1));
            TurnLeft = TurnSeconds;
        }

        public IReadOnlyList<VersusSeat> Seats => seats;

        /// <summary>Whose turn it is.</summary>
        public int Current { get; private set; }

        public VersusSeat CurrentSeat => seats[Current];

        /// <summary>Counts the turns, from 1.</summary>
        public int TurnNumber { get; private set; } = 1;

        /// <summary>Seconds a turn lasts; 0 plays without a limit.</summary>
        public float TurnSeconds { get; }

        public float TurnLeft { get; private set; }

        public bool HasTurnLimit => TurnSeconds > 0f;

        /// <summary>
        /// What a flip of the current player means to them: the points (a bomb takes no more than the
        /// <paramref name="score"/> they have), whether they go on and whether the clock starts over. When the turn
        /// passes at once, the cards the player still had face up on <paramref name="board"/> go back down and into
        /// <see cref="FlipResult.FlippedBack"/>, so the next player does not inherit half a set.
        /// </summary>
        public static VersusOutcome Judge(FlipResult result, int score, MemoryRound board)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            var outcome = new VersusOutcome { KeepsTurn = true, SetCompleted = result.SetCompleted };
            switch (result.Outcome)
            {
                case FlipOutcome.Matched:
                case FlipOutcome.WildMatched:
                    outcome.Points = result.Points;
                    // A set earns the whole turn again.
                    outcome.FreshClock = true;
                    break;
                case FlipOutcome.Mismatched:
                case FlipOutcome.WrongOrder:
                    outcome.KeepsTurn = false;
                    outcome.PassesAfterMistake = true;
                    break;
                case FlipOutcome.Bomb:
                    outcome.Points = -Math.Min(Math.Max(0, score), MemoryRound.BombPoints);
                    outcome.KeepsTurn = false;
                    break;
                case FlipOutcome.Clock:
                case FlipOutcome.Peek:
                    outcome.Points = result.Points;
                    outcome.FreshClock = true;
                    break;
            }
            if (outcome.PassesNow && board != null)
            {
                result.FlippedBack.AddRange(board.HideRevealed());
            }
            return outcome;
        }

        /// <summary>
        /// Books the result of a flip of the current player (<see cref="Judge"/>): points go to them, sets and mistakes
        /// are counted, the clock starts over when they earned it, and a bomb passes the turn at once (with the half
        /// finished set of <paramref name="board"/> turned back). After a mistake the turn passes with
        /// <see cref="PassTurn"/>, once the cards are turned back.
        /// </summary>
        public VersusOutcome Apply(FlipResult result, MemoryRound board = null)
        {
            VersusSeat seat = CurrentSeat;
            VersusOutcome outcome = Judge(result, seat.Score, board);
            outcome.Seat = seat.Seat;
            seat.Score = Math.Max(0, seat.Score + outcome.Points);
            if (outcome.SetCompleted)
            {
                seat.Sets++;
                seat.BestCombo = Math.Max(seat.BestCombo, result.Combo);
            }
            if (result.IsMistake)
            {
                seat.Mistakes++;
            }
            if (outcome.FreshClock)
            {
                TurnLeft = TurnSeconds;
            }
            if (outcome.PassesNow)
            {
                PassTurn();
            }
            return outcome;
        }

        /// <summary>The next player who still plays takes over, with a full clock.</summary>
        public void PassTurn()
        {
            for (int step = 1; step <= seats.Count; step++)
            {
                int next = (Current + step) % seats.Count;
                if (seats[next].Playing)
                {
                    Current = next;
                    break;
                }
            }
            TurnNumber++;
            TurnLeft = TurnSeconds;
        }

        /// <summary>Runs the clock of the turn. True when it just ran out: the turn is lost (see <see cref="PassTurn"/>).</summary>
        public bool Tick(float deltaTime)
        {
            if (!HasTurnLimit || deltaTime <= 0f || TurnLeft <= 0f)
            {
                return false;
            }
            TurnLeft = Math.Max(0f, TurnLeft - deltaTime);
            return TurnLeft <= 0f;
        }

        /// <summary>An online game: the server said whose turn it is and how long it lasts.</summary>
        public void SetTurn(int seat, int number, float secondsLeft)
        {
            Current = Math.Max(0, Math.Min(seat, seats.Count - 1));
            TurnNumber = number;
            TurnLeft = secondsLeft;
        }

        /// <summary>
        /// The order of the standings: whoever stayed before whoever left, then by score, then by sets, then by seat.
        /// The server ranks the members of a room with it, so the places it gives are the ones the results show.
        /// </summary>
        public static int Compare(VersusSeat a, VersusSeat b)
        {
            return a.Playing != b.Playing ? b.Playing.CompareTo(a.Playing)
                : a.Score != b.Score ? b.Score.CompareTo(a.Score)
                : a.Sets != b.Sets ? b.Sets.CompareTo(a.Sets)
                : a.Seat.CompareTo(b.Seat);
        }

        /// <summary>Whether two players share a place: the same score and the same sets, both still in the game (or both gone).</summary>
        public static bool SharePlace(VersusSeat a, VersusSeat b)
        {
            return a.Playing == b.Playing && a.Score == b.Score && a.Sets == b.Sets;
        }

        /// <summary>The players from the winner down (<see cref="Compare"/>).</summary>
        public List<VersusSeat> Standings()
        {
            var standings = new List<VersusSeat>(seats);
            standings.Sort(Compare);
            return standings;
        }

        /// <summary>The seats that share the best result: one winner, or the players of a draw.</summary>
        public List<VersusSeat> Winners()
        {
            List<VersusSeat> standings = Standings();
            VersusSeat best = standings[0];
            return standings.FindAll(seat => SharePlace(seat, best));
        }

        /// <summary>
        /// The rules of a board when several players share it: what limits a single player (the clock, hearts, moves)
        /// goes, because the turn timer and the other players take that place, and so do the twists that need one
        /// player's run of play (the parade, the shuffle after mistakes). Clock cards, which would have no clock to
        /// change, are dealt as peek cards. The board, the sets and the other special cards stay.
        /// </summary>
        public static RoundRules RulesFor(RoundRules board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }
            RoundRules rules = board.Copy();
            rules.TimeLimit = 0f;
            rules.MatchTimeBonus = 0f;
            rules.Hearts = 0;
            rules.MoveLimit = 0;
            rules.ShuffleEvery = 0;
            rules.Parade = false;
            rules.Peeks += rules.Clocks;
            rules.Clocks = 0;
            return rules;
        }
    }
}
