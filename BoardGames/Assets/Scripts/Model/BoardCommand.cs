using System;
using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.BoardGames
{
    /// <summary>The kinds of command a seat can give; the numbers travel in the action log and the server, so they never change.</summary>
    public enum CommandKind : uint
    {
        /// <summary>A move: the squares of a draughts path, a chess move, or the checkers of a backgammon play.</summary>
        Move = 0,
        Resign = 1,
        /// <summary>The computer takes a seat over; only ever the base server's own action online.</summary>
        SeatToComputer = 2,
        /// <summary>Backgammon: throw the dice (the opening roll too); the table rolls them from the stamp of the entry.</summary>
        Roll = 3,
        /// <summary>Backgammon: offer the doubling cube before rolling.</summary>
        Double = 4,
        /// <summary>Backgammon: accept the cube; the game goes on at twice the stake.</summary>
        Take = 5,
        /// <summary>Backgammon: refuse the cube and give the game up at its value.</summary>
        Drop = 6
    }

    /// <summary>
    /// One entry of the game's action log as the table reads it: a move, giving up, a seat handed to the computer, or
    /// one of the backgammon decisions (rolling, the doubling cube). A move's numbers depend on the game:
    /// <list type="bullet">
    /// <item>draughts: the squares the piece visits, the first being where it starts (at least two);</item>
    /// <item>chess: <c>{ from, to, promotion }</c> with the squares 0 to 63 and the piece a pawn becomes (0 for none);</item>
    /// <item>backgammon: <c>{ from, to, from, to, ... }</c>, a pair per checker moved, in the order played, points 0 to 23,
    /// <see cref="BackgammonGame.Bar"/> as a from and <see cref="BackgammonGame.Off"/> as a to.</item>
    /// </list>
    /// The payload is those numbers (or the level of a seat handed to the computer), parted by commas.
    /// </summary>
    [Serializable]
    public sealed class BoardCommand
    {
        /// <summary>The most numbers a move may hold: a draughts capture of every enemy piece, with its start.</summary>
        public const int MaxPathLength = 32;

        /// <summary>The largest number a move may name (a square of the 10x10 board, a point, the bar or off).</summary>
        public const int MaxNumber = 99;

        public int kind;
        public int seat;
        public int[] path = new int[0];
        public int level;

        public CommandKind Kind => (CommandKind)kind;

        /// <summary>Whether a player may send <paramref name="kind"/> online (the computer taking a seat is the server's own action).</summary>
        public static bool PlayerMaySend(uint kind)
        {
            return kind <= (uint)CommandKind.Drop && kind != (uint)CommandKind.SeatToComputer;
        }

        /// <summary>A draughts move along its path.</summary>
        public static BoardCommand MoveAlong(int seat, Move move)
        {
            return Play(seat, move.path);
        }

        /// <summary>A move of any game by its numbers (see the class summary).</summary>
        public static BoardCommand Play(int seat, IReadOnlyList<int> numbers)
        {
            var copy = new int[numbers != null ? numbers.Count : 0];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = numbers[i];
            }
            return new BoardCommand { kind = (int)CommandKind.Move, seat = seat, path = copy };
        }

        public static BoardCommand Resign(int seat)
        {
            return Simple(CommandKind.Resign, seat);
        }

        public static BoardCommand ToComputer(int seat, int level)
        {
            return new BoardCommand { kind = (int)CommandKind.SeatToComputer, seat = seat, path = new int[0], level = level };
        }

        public static BoardCommand Roll(int seat)
        {
            return Simple(CommandKind.Roll, seat);
        }

        public static BoardCommand Double(int seat)
        {
            return Simple(CommandKind.Double, seat);
        }

        public static BoardCommand Take(int seat)
        {
            return Simple(CommandKind.Take, seat);
        }

        public static BoardCommand Drop(int seat)
        {
            return Simple(CommandKind.Drop, seat);
        }

        private static BoardCommand Simple(CommandKind kind, int seat)
        {
            return new BoardCommand { kind = (int)kind, seat = seat, path = new int[0] };
        }

        /// <summary>The numbers of a move or the level of a seat to the computer, parted by commas; nothing for the others.</summary>
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

        /// <summary>
        /// Reads a command back from the log; null when the payload is not what <paramref name="kind"/> writes. A move
        /// is only checked for its shape here (two to <see cref="MaxPathLength"/> numbers from 0 to <see cref="MaxNumber"/>);
        /// the table of the game decides whether it is legal.
        /// </summary>
        public static BoardCommand Parse(int seat, CommandKind kind, string payload)
        {
            switch (kind)
            {
                case CommandKind.Move:
                {
                    var numbers = new List<int>();
                    if (!PayloadText.TryNumbers(payload, ',', numbers, MaxPathLength) || numbers.Count < 2)
                    {
                        return null;
                    }
                    for (int i = 0; i < numbers.Count; i++)
                    {
                        if (numbers[i] < 0 || numbers[i] > MaxNumber)
                        {
                            return null;
                        }
                    }
                    return new BoardCommand { kind = (int)kind, seat = seat, path = numbers.ToArray() };
                }
                case CommandKind.SeatToComputer:
                {
                    if (!PayloadText.TryNumber(payload, out int level) || level < 0 || level >= RuleSets.ComputerLevels)
                    {
                        return null;
                    }
                    return ToComputer(seat, level);
                }
                case CommandKind.Resign:
                case CommandKind.Roll:
                case CommandKind.Double:
                case CommandKind.Take:
                case CommandKind.Drop:
                    return string.IsNullOrEmpty(payload) ? Simple(kind, seat) : null;
                default:
                    return null;
            }
        }

        public override string ToString()
        {
            return $"{Kind} seat {seat}" + (Kind == CommandKind.Move ? " " + Payload() : Kind == CommandKind.SeatToComputer ? " level " + level : "");
        }
    }
}
