using System;
using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.Checkers
{
    /// <summary>The kinds of command a seat can give; the numbers travel in the action log and the server, so they never change.</summary>
    public enum CommandKind : uint
    {
        Move = 0,
        Resign = 1,
        SeatToComputer = 2
    }

    /// <summary>
    /// One entry of the game's action log as the table reads it: a move along a path of squares, a resignation, or a
    /// seat handed to the computer at a level. The payload is the numbers of the path or the level, parted by commas.
    /// </summary>
    [Serializable]
    public sealed class CheckersCommand
    {
        /// <summary>The most squares a path may name: a jump per piece the other side has, and the start.</summary>
        public const int MaxPathLength = 32;

        public int kind;
        public int seat;
        public int[] path;
        public int level;

        public CommandKind Kind => (CommandKind)kind;

        public static CheckersCommand MoveAlong(int seat, Move move)
        {
            return new CheckersCommand
            {
                kind = (int)CommandKind.Move,
                seat = seat,
                path = (int[])move.path.Clone(),
                level = 0
            };
        }

        public static CheckersCommand Resign(int seat)
        {
            return new CheckersCommand { kind = (int)CommandKind.Resign, seat = seat, path = new int[0] };
        }

        public static CheckersCommand ToComputer(int seat, int level)
        {
            return new CheckersCommand { kind = (int)CommandKind.SeatToComputer, seat = seat, path = new int[0], level = level };
        }

        /// <summary>The path's squares (a move) or the level (a seat to the computer), parted by commas; nothing for a resignation.</summary>
        public string Payload()
        {
            switch (Kind)
            {
                case CommandKind.Move:
                    return PayloadText.Numbers(',', path ?? new int[0]);
                case CommandKind.SeatToComputer:
                    return PayloadText.Number(level);
                default:
                    return "";
            }
        }

        /// <summary>Reads a command back from the log; null when the payload is not what <paramref name="kind"/> writes.</summary>
        public static CheckersCommand Parse(int seat, CommandKind kind, string payload)
        {
            switch (kind)
            {
                case CommandKind.Move:
                {
                    var squares = new List<int>();
                    if (!PayloadText.TryNumbers(payload, ',', squares, MaxPathLength) || squares.Count < 2)
                    {
                        return null;
                    }
                    for (int i = 0; i < squares.Count; i++)
                    {
                        if (squares[i] < 0 || squares[i] >= 100)
                        {
                            return null;
                        }
                    }
                    return new CheckersCommand { kind = (int)kind, seat = seat, path = squares.ToArray() };
                }
                case CommandKind.Resign:
                    return string.IsNullOrEmpty(payload) ? Resign(seat) : null;
                case CommandKind.SeatToComputer:
                {
                    if (!PayloadText.TryNumber(payload, out int level) || level < 0 || level >= CheckersAI.Levels)
                    {
                        return null;
                    }
                    return ToComputer(seat, level);
                }
                default:
                    return null;
            }
        }
    }
}
