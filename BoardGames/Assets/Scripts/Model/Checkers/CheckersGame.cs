using System;
using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// One game of draughts from its first position to its end: the board, whose turn it is, the plies played and
    /// how many of them were quiet, the result once there is one. Everything the rules look at is in the public fields,
    /// so a saved game and a game played in step on two devices are this and nothing more.
    /// </summary>
    [Serializable]
    public sealed class CheckersGame : IBoardGame
    {
        public int variant;
        public Board board;
        public int sideToMove;
        public int ply;
        public int quietPlies;
        public int result;
        public int endReason;
        public int[] capturedCount = new int[2];
        public Move lastMove;

        [NonSerialized]
        private List<Move> legal;

        /// <summary>For the serializers; the fields are set by them.</summary>
        public CheckersGame()
        {
        }

        /// <summary>The initial position of <paramref name="variant"/>, its first mover to move.</summary>
        public CheckersGame(Variant variant)
        {
            VariantRules rules = VariantRules.Of(variant);
            this.variant = (int)rules.Variant;
            board = Board.Initial(rules);
            sideToMove = (int)rules.FirstMover;
        }

        public VariantRules Rules => VariantRules.Of(variant);

        public RuleSet RuleSet => RuleSets.OfVariant(Rules.Variant);

        public Side SideToAct => SideToMove;

        /// <summary>A ply per move: the turn counter of <see cref="IBoardGame"/>.</summary>
        public int Turn => ply;

        public int Points => IsOver && Winner.HasValue ? 1 : 0;

        public Side SideToMove => sideToMove == (int)Side.Dark ? Side.Dark : Side.Light;

        public GameResult Result => (GameResult)result;

        public EndReason Reason => (EndReason)endReason;

        public bool IsOver => result != (int)GameResult.Playing;

        public Side? Winner
        {
            get
            {
                switch (Result)
                {
                    case GameResult.LightWins:
                        return Side.Light;
                    case GameResult.DarkWins:
                        return Side.Dark;
                    default:
                        return null;
                }
            }
        }

        /// <summary>The legal moves of the side to move, worked out once per position; none once the game is over.</summary>
        public IReadOnlyList<Move> Legal()
        {
            if (legal == null)
            {
                legal = IsOver ? new List<Move>() : MoveGenerator.Legal(board, SideToMove, Rules);
            }
            return legal;
        }

        /// <summary>The legal move along exactly <paramref name="path"/>, or null.</summary>
        public Move FindLegal(int[] path)
        {
            if (path == null || path.Length < 2)
            {
                return null;
            }
            IReadOnlyList<Move> moves = Legal();
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].PathIs(path))
                {
                    return moves[i];
                }
            }
            return null;
        }

        /// <summary>The legal moves of the piece on <paramref name="square"/>; empty when it has none or there is none.</summary>
        public IReadOnlyList<Move> LegalFrom(int square)
        {
            var from = new List<Move>();
            IReadOnlyList<Move> moves = Legal();
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].From == square)
                {
                    from.Add(moves[i]);
                }
            }
            return from;
        }

        /// <summary>Plays <paramref name="move"/> when it is one of the legal moves; false (and nothing changes) otherwise.</summary>
        public bool Play(Move move)
        {
            if (move == null || IsOver)
            {
                return false;
            }
            Move chosen = null;
            IReadOnlyList<Move> moves = Legal();
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].SamePathAs(move))
                {
                    chosen = moves[i];
                    break;
                }
            }
            if (chosen == null)
            {
                return false;
            }
            Side mover = SideToMove;
            Side other = Sides.Other(mover);
            VariantRules rules = Rules;
            bool manMoved = Pieces.IsMan(board.cells[chosen.From]);
            MoveGenerator.Apply(board, chosen, rules);
            ply++;
            if (chosen.IsCapture || manMoved)
            {
                quietPlies = 0;
            }
            else
            {
                quietPlies++;
            }
            capturedCount[(int)mover] += chosen.Jumps;
            lastMove = chosen;
            sideToMove = (int)other;
            legal = null;
            if (!MoveGenerator.HasAnyMove(board, other, rules))
            {
                result = (int)(mover == Side.Light ? GameResult.LightWins : GameResult.DarkWins);
                endReason = (int)EndReason.NoMoves;
            }
            else if (quietPlies >= rules.DrawPlies)
            {
                result = (int)GameResult.Draw;
                endReason = (int)EndReason.DrawByPlies;
            }
            return true;
        }

        /// <summary><paramref name="side"/> gives up: the other side wins. False once the game is over.</summary>
        public bool Resign(Side side)
        {
            if (IsOver)
            {
                return false;
            }
            result = (int)(side == Side.Light ? GameResult.DarkWins : GameResult.LightWins);
            endReason = (int)EndReason.Resigned;
            legal = null;
            return true;
        }

        /// <summary>The room ended the game early: <paramref name="winner"/> wins, or nobody does.</summary>
        public void CallOff(Side? winner)
        {
            if (winner.HasValue)
            {
                result = (int)(winner.Value == Side.Light ? GameResult.LightWins : GameResult.DarkWins);
            }
            else
            {
                result = (int)GameResult.Draw;
            }
            endReason = (int)EndReason.CalledOff;
            legal = null;
        }

        public IBoardGame CloneGame()
        {
            return Clone();
        }

        public CheckersGame Clone()
        {
            var copy = new CheckersGame
            {
                variant = variant,
                board = board.Clone(),
                sideToMove = sideToMove,
                ply = ply,
                quietPlies = quietPlies,
                result = result,
                endReason = endReason,
                capturedCount = (int[])capturedCount.Clone(),
                lastMove = lastMove
            };
            return copy;
        }

        /// <summary>Everything the rules look at, folded: the cells, the side to move, the plies, the result.</summary>
        public uint Checksum()
        {
            var sum = new StateChecksum();
            sum.Add(board.cells.Length);
            for (int i = 0; i < board.cells.Length; i++)
            {
                sum.Add(board.cells[i]);
            }
            sum.Add(sideToMove).Add(ply).Add(quietPlies).Add(result).Add(endReason);
            return sum.Value;
        }

        /// <summary>What <paramref name="side"/> has on the board: 100 a man, 300 a king (250 in the English game).</summary>
        public int Material(Side side)
        {
            int kingValue = Rules.Variant == Variant.English ? 250 : 300;
            return board.Count(side, false) * 100 + board.Count(side, true) * kingValue;
        }
    }
}
