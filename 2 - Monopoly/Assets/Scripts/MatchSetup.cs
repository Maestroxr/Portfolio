using System;
using System.Collections.Generic;
using System.Linq;

namespace Portfolio.Monopoly
{
    public enum SeatKind { Human, Computer, Off }

    [Serializable]
    public class SeatSetup
    {
        public SeatKind kind;
        public string name;
        public int token;
        public BotLevel level = BotLevel.Normal;
    }

    /// <summary>Who sits at the table: up to four seats, each a human, a computer player or empty, with a name and a token.</summary>
    [Serializable]
    public class MatchSetup
    {
        public List<SeatSetup> seats = new List<SeatSetup>();

        public int ActiveCount => seats.Count(seat => seat.kind != SeatKind.Off);

        public bool IsValid(out string message)
        {
            if (ActiveCount < 2)
            {
                message = "Seat at least two players.";
                return false;
            }
            var tokens = seats.Where(seat => seat.kind != SeatKind.Off).Select(seat => seat.token).ToList();
            if (tokens.Distinct().Count() != tokens.Count)
            {
                message = "Every player needs a different token.";
                return false;
            }
            message = "OK";
            return true;
        }

        /// <summary>You against three computer players, the usual start.</summary>
        public static MatchSetup Default()
        {
            var setup = new MatchSetup();
            setup.seats.Add(new SeatSetup { kind = SeatKind.Human, name = "You", token = 0 });
            setup.seats.Add(new SeatSetup { kind = SeatKind.Computer, name = "Ada", token = 1, level = BotLevel.Normal });
            setup.seats.Add(new SeatSetup { kind = SeatKind.Computer, name = "Max", token = 2, level = BotLevel.Normal });
            setup.seats.Add(new SeatSetup { kind = SeatKind.Computer, name = "Lulu", token = 5, level = BotLevel.Normal });
            return setup;
        }

        public MatchSetup Clone()
        {
            return new MatchSetup
            {
                seats = seats.Select(seat => new SeatSetup { kind = seat.kind, name = seat.name, token = seat.token, level = seat.level }).ToList()
            };
        }

        /// <summary>Names the computer players pick from.</summary>
        /// <summary>Whether a name is "You", the default name of the one person at the table.</summary>
        public static bool IsYou(string name)
        {
            return name != null && string.Equals(name.Trim(), "You", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The name a seat plays under: "You" only reads right for the one person at the table, so a computer player or
        /// one of several people called that gets a name of their own.
        /// </summary>
        public string PlayingName(int index)
        {
            SeatSetup seat = seats[index];
            if (!IsYou(seat.name) || (seat.kind == SeatKind.Human && seats.Count(s => s.kind == SeatKind.Human) == 1))
            {
                return seat.name;
            }
            return seat.kind == SeatKind.Computer ? BotNames[index % BotNames.Length] : $"Player {index + 1}";
        }

        public static readonly string[] BotNames ={ "Ada", "Max", "Lulu", "Otto", "Mia", "Leo", "Zoe", "Hugo" };
    }
}
