using System.Collections.Generic;

namespace Portfolio.EndlessRunner
{
    /// <summary>What became of a piece when the server said who got it.</summary>
    public enum ClaimOutcome
    {
        /// <summary>The local runner got the piece.</summary>
        Awarded,

        /// <summary>The local runner touched the piece, but another runner was there first.</summary>
        Lost,

        /// <summary>Another runner took a piece the local runner had not touched.</summary>
        Remote,

        /// <summary>The piece was settled before; nothing changes.</summary>
        Duplicate
    }


    /// <summary>The answer of <see cref="CoinClaims.Settle"/>.</summary>
    public readonly struct ClaimResult
    {
        public ClaimResult(ClaimOutcome outcome, int coins)
        {
            Outcome = outcome;
            Coins = coins;
        }

        public ClaimOutcome Outcome { get; }

        /// <summary>The coins the local runner counts for the piece: its value, doubled by double coins; 0 unless awarded.</summary>
        public int Coins { get; }
    }


    /// <summary>
    /// The books of the coins of a race. The runners share the coins of the track, and the server decides who gets one:
    /// the local runner asks for a piece it touched (<see cref="Request"/>) and counts it when the server says so
    /// (<see cref="Settle"/>), which is also how it learns what the others took. Pieces are known by their id in the
    /// layout of the track, runners by their seat in the room.
    /// </summary>
    public sealed class CoinClaims
    {
        private struct Pending
        {
            public int Value;
            public int Multiplier;
        }

        private readonly Dictionary<int, Pending> pending = new Dictionary<int, Pending>();
        private readonly Dictionary<int, int> owners = new Dictionary<int, int>();
        private readonly Dictionary<int, int> coinsBySeat = new Dictionary<int, int>();
        private readonly Dictionary<int, int> piecesBySeat = new Dictionary<int, int>();

        /// <summary>Pieces the local runner asked for that the server has not answered yet.</summary>
        public int PendingCount => pending.Count;

        /// <summary>Pieces that have an owner.</summary>
        public int SettledCount => owners.Count;

        /// <summary>Coin value of all pieces that have an owner.</summary>
        public int SettledCoins { get; private set; }

        /// <summary>
        /// The local runner touched a piece worth <paramref name="value"/> coins, with <paramref name="multiplier"/> in
        /// effect at that moment. False when the piece is spoken for already: it has an owner, or was asked for before.
        /// </summary>
        public bool Request(int piece, int value, int multiplier)
        {
            if (owners.ContainsKey(piece) || pending.ContainsKey(piece))
            {
                return false;
            }
            pending[piece] = new Pending { Value = value, Multiplier = multiplier < 1 ? 1 : multiplier };
            return true;
        }

        /// <summary>The server gave <paramref name="piece"/> to the runner at <paramref name="seat"/>.</summary>
        public ClaimResult Settle(int piece, int seat, int value, int localSeat)
        {
            if (owners.ContainsKey(piece))
            {
                return new ClaimResult(ClaimOutcome.Duplicate, 0);
            }
            owners[piece] = seat;
            SettledCoins += value;
            coinsBySeat[seat] = CoinsOf(seat) + value;
            piecesBySeat[seat] = PiecesOf(seat) + 1;

            bool asked = pending.TryGetValue(piece, out Pending request);
            pending.Remove(piece);
            if (seat != localSeat)
            {
                return new ClaimResult(asked ? ClaimOutcome.Lost : ClaimOutcome.Remote, 0);
            }
            return new ClaimResult(ClaimOutcome.Awarded, value * (asked ? request.Multiplier : 1));
        }

        /// <summary>Whether the piece has an owner.</summary>
        public bool IsTaken(int piece)
        {
            return owners.ContainsKey(piece);
        }

        public bool IsPending(int piece)
        {
            return pending.ContainsKey(piece);
        }

        /// <summary>The seat that owns the piece, or -1.</summary>
        public int OwnerOf(int piece)
        {
            return owners.TryGetValue(piece, out int seat) ? seat : -1;
        }

        /// <summary>Coin value of the pieces of a seat, as the server counts them (double coins is the runner's own business).</summary>
        public int CoinsOf(int seat)
        {
            return coinsBySeat.TryGetValue(seat, out int coins) ? coins : 0;
        }

        public int PiecesOf(int seat)
        {
            return piecesBySeat.TryGetValue(seat, out int pieces) ? pieces : 0;
        }

        /// <summary>The ids of the pieces of a seat, in ascending order.</summary>
        public List<int> PiecesOwnedBy(int seat)
        {
            var pieces = new List<int>();
            foreach (KeyValuePair<int, int> owner in owners)
            {
                if (owner.Value == seat)
                {
                    pieces.Add(owner.Key);
                }
            }
            pieces.Sort();
            return pieces;
        }

        public void Clear()
        {
            pending.Clear();
            owners.Clear();
            coinsBySeat.Clear();
            piecesBySeat.Clear();
            SettledCoins = 0;
        }
    }
}
