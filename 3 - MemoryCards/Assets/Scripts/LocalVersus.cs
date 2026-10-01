using System.Collections.Generic;
using Gamebox;
using UnityEngine;
using static Portfolio.MemoryCards.MemoryCardsText;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// A versus game at one device as the shared local play setup made it (<see cref="LocalMatch"/>): the seats of the
    /// <see cref="VersusMatch"/> with the names of the setup and its turn clock, a <see cref="MemoryCardsComputer"/> for
    /// every computer seat, and when a turn really begins. With the hand-over screen on, a person's turn waits behind the
    /// curtain until they are ready: until then the clock holds and the cards ignore clicks. The computer's turns need no
    /// curtain, and neither does a game with only one person in it. On a computer's turn only the computer flips; on a
    /// person's turn the computer does not. The rules of the setup come from <see cref="CreateRules"/>.
    /// </summary>
    public sealed class LocalVersus
    {
        /// <summary>The key of the turn clock option of the setup.</summary>
        public const string ClockOption = "clock";

        /// <summary>The seconds of a turn for each choice of the clock option; 0 plays without a clock.</summary>
        public static readonly float[] ClockSeconds = { 0f, 10f, 15f, 20f, 30f };

        private readonly Dictionary<int, MemoryCardsComputer> computers = new Dictionary<int, MemoryCardsComputer>();

        public LocalVersus(LocalMatch setup, System.Random random)
        {
            Setup = setup;
            var names = new List<string>();
            foreach (LocalSeat seat in setup.Players)
            {
                names.Add(seat.Name);
                if (!seat.IsHuman)
                {
                    computers[seat.Index] = new MemoryCardsComputer(MemoryCardsComputer.SkillOf(seat.Level), random);
                }
            }
            Match = new VersusMatch(names, TurnSecondsOf(setup));
        }

        /// <summary>
        /// What the setup offers: two to four players, computers that play Easy, Normal or Hard (somebody has to be a
        /// person), the hand-over screen (off at first: the board is the same for everybody) and the turn clock.
        /// </summary>
        public static LocalPlayRules CreateRules()
        {
            var rules = new LocalPlayRules
            {
                Title = K("Versus"),
                Hint = K("Take turns: a set scores and lets you go again, a mistake passes the turn."),
                MinPlayers = VersusMatch.MinPlayers,
                MaxPlayers = VersusMatch.MaxPlayers,
                DefaultPlayers = VersusMatch.MinPlayers,
                AllowComputers = true,
                MinHumans = 1,
                OffersHandOver = true,
                HandOverByDefault = false,
                Game = GameType.MemoryCards
            };
            // "Off" is a word of the shared table.
            rules.Options.Add(new LocalOption(ClockOption, K("Turn clock"), new[] { "Off", K("10 s"), K("15 s"), K("20 s"), K("30 s") },
                ClockIndex(VersusMatch.DefaultTurnSeconds)));
            return rules;
        }

        /// <summary>The choice of the clock option that lasts <paramref name="seconds"/> (the default when none does).</summary>
        public static int ClockIndex(float seconds)
        {
            for (int i = 0; i < ClockSeconds.Length; i++)
            {
                if (Mathf.Approximately(ClockSeconds[i], seconds))
                {
                    return i;
                }
            }
            return ClockIndex(VersusMatch.DefaultTurnSeconds);
        }

        /// <summary>The seconds of a turn the setup chose; the default clock when it has no choice.</summary>
        public static float TurnSecondsOf(LocalMatch setup)
        {
            int choice = setup != null ? setup.Option(ClockOption) : -1;
            return choice >= 0 && choice < ClockSeconds.Length ? ClockSeconds[choice] : VersusMatch.DefaultTurnSeconds;
        }

        public LocalMatch Setup { get; }

        /// <summary>The seats, scores and turns of the game.</summary>
        public VersusMatch Match { get; }

        /// <summary>The setup's seat of a seat of the game.</summary>
        public LocalSeat SeatOf(int seat)
        {
            return Setup.Players[seat];
        }

        public bool IsComputer(int seat)
        {
            return computers.ContainsKey(seat);
        }

        /// <summary>Whether the computer plays the turn now.</summary>
        public bool ComputerTurn => IsComputer(Match.Current);

        /// <summary>The computer whose turn it is, or null on a person's turn.</summary>
        public MemoryCardsComputer CurrentComputer => computers.TryGetValue(Match.Current, out MemoryCardsComputer computer) ? computer : null;

        public IEnumerable<MemoryCardsComputer> Computers => computers.Values;

        /// <summary>Whether the turn began: the player has the board, the clock runs and their clicks count.</summary>
        public bool TurnReady { get; private set; }

        /// <summary>Whether <paramref name="seat"/>'s turn starts behind the hand-over screen.</summary>
        public bool HandsOver(int seat)
        {
            return Setup.HandOver && Setup.Humans > 1 && !IsComputer(seat);
        }

        /// <summary>
        /// The turn of the current seat comes (the first one, or after a pass). Returns whether it waits for the player
        /// behind the hand-over screen (<see cref="Ready"/>); otherwise it is under way at once.
        /// </summary>
        public bool BeginTurn()
        {
            TurnReady = !HandsOver(Match.Current);
            return !TurnReady;
        }

        /// <summary>The player took the device: the turn is under way.</summary>
        public void Ready()
        {
            TurnReady = true;
        }

        /// <summary>Runs the clock of a turn that is under way. True when it just ran out.</summary>
        public bool Tick(float deltaTime)
        {
            return TurnReady && Match.Tick(deltaTime);
        }

        /// <summary>Whether a flip counts now: a person's on their own turn, the computer's on its own.</summary>
        public bool Accepts(bool byComputer)
        {
            return TurnReady && byComputer == ComputerTurn;
        }

        /// <summary>Every computer sees a card that turned face up.</summary>
        public void See(MemoryCard card)
        {
            foreach (MemoryCardsComputer computer in computers.Values)
            {
                computer.See(card);
            }
        }

        /// <summary>Every computer sees cards that showed at once (the memorize phase, a peek).</summary>
        public void SeeAll(IEnumerable<MemoryCard> cards)
        {
            var shown = new List<MemoryCard>(cards);
            foreach (MemoryCardsComputer computer in computers.Values)
            {
                computer.SeeAll(shown);
            }
        }
    }
}
