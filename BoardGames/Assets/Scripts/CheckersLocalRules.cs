using Gamebox;
using UnityEngine;

namespace Portfolio.Checkers
{
    /// <summary>
    /// What Checkers offers at one device: two seats, either of which the computer may take (somebody has to be a
    /// person), the rules to play by, who moves first and a clock per move. Seat 1 plays the side that moves first unless
    /// the "first" option says otherwise; each seat shows the side it plays and takes that side's colour from the look
    /// shown (<see cref="Refresh"/>). <see cref="SetupOf"/> turns the setup into the table of a game.
    /// </summary>
    public sealed class CheckersLocalRules : LocalPlayRules
    {
        public const string VariantOption = "variant";
        public const string FirstOption = "first";
        public const string ClockOption = "clock";

        /// <summary>The seconds of a move for each choice of the clock option; 0 plays without a clock.</summary>
        public static readonly int[] ClockSeconds = { 0, 30, 60, 120, 300 };

        private static readonly string[] FirstChoices = { "Rules decide", "Player 1", "Player 2" };
        private static readonly string[] ClockChoices = { "None", "30 seconds", "1 minute", "2 minutes", "5 minutes" };

        public CheckersLocalRules()
        {
            Game = GameType.Checkers;
            Title = "Local Play";
            Hint = "Two people at this device, or one against the computer.";
            MinPlayers = 2;
            MaxPlayers = 2;
            DefaultPlayers = 2;
            AllowComputers = true;
            MinHumans = 1;
            OffersHandOver = false;
            HandOverByDefault = false;
            var variants = new string[VariantRules.All.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                variants[i] = VariantRules.Of(VariantRules.All[i]).NameKey;
            }
            Options.Add(new LocalOption(VariantOption, "Rules", variants));
            Options.Add(new LocalOption(FirstOption, "Who moves first", FirstChoices));
            Options.Add(new LocalOption(ClockOption, "Time per move", ClockChoices));
            Refresh(null);
        }

        /// <summary>Before the setup opens: the seats take the colours of the two sides in the look shown.</summary>
        public void Refresh(CheckersTheme theme)
        {
            SeatColors = new Color[2];
            for (int seat = 0; seat < 2; seat++)
            {
                Side side = SideOfSeat(seat, FindOption(VariantOption).Index, FindOption(FirstOption).Index);
                SeatColors[seat] = theme != null ? theme.ColorOf(side) : (side == Side.Light ? Color.white : new Color(0.25f, 0.25f, 0.25f));
            }
        }

        /// <summary>The seat that plays the side moving first, for the options chosen.</summary>
        public static int FirstSeatOf(int variant, int first)
        {
            switch (first)
            {
                case 1: return 0;
                case 2: return 1;
                default: return VariantRules.Of(variant).FirstMover == Side.Light ? 0 : 1;
            }
        }

        /// <summary>The side a seat plays, for the options chosen.</summary>
        public static Side SideOfSeat(int seat, int variant, int first)
        {
            Side mover = VariantRules.Of(variant).FirstMover;
            return seat == FirstSeatOf(variant, first) ? mover : Sides.Other(mover);
        }

        /// <summary>The seconds per move the setup chose (0 for no clock).</summary>
        public static int ClockSecondsOf(LocalMatch match)
        {
            int choice = match != null ? match.Option(ClockOption) : 0;
            return choice >= 0 && choice < ClockSeconds.Length ? ClockSeconds[choice] : 0;
        }

        /// <summary>An option changed: the seats take the colours of the sides they play now.</summary>
        public override void OptionsChanged(LocalMatch match)
        {
            Color[] colors = SeatColors;
            Side lightSide = SideOfSeat(0, FindOption(VariantOption).Index, FindOption(FirstOption).Index);
            Color lightColor = colors != null && colors.Length == 2 ? colors[lightSide == Side.Light ? 0 : 1] : Color.white;
            Color darkColor = colors != null && colors.Length == 2 ? colors[lightSide == Side.Light ? 1 : 0] : new Color(0.25f, 0.25f, 0.25f);
            for (int seat = 0; seat < match.AllSeats.Count && seat < 2; seat++)
            {
                Side side = SideOfSeat(seat, match.Option(VariantOption), match.Option(FirstOption));
                match[seat].Color = side == Side.Light ? lightColor : darkColor;
            }
        }

        /// <summary>The side the seat plays, in the language shown.</summary>
        public override string SeatDetail(LocalMatch match, int seat)
        {
            if (match == null || seat < 0 || seat > 1)
            {
                return null;
            }
            return L.T(Sides.NameKey(SideOfSeat(seat, match.Option(VariantOption), match.Option(FirstOption))));
        }

        /// <summary>The table of <paramref name="match"/>: its rules, its two seats with their names and levels, who moves first and the clock.</summary>
        public static TableSetup SetupOf(LocalMatch match)
        {
            var setup = new TableSetup();
            int variant = Mathf.Clamp(match.Option(VariantOption), 0, VariantRules.All.Length - 1);
            setup.variant = variant;
            setup.firstSeat = FirstSeatOf(variant, match.Option(FirstOption));
            setup.clockSeconds = ClockSecondsOf(match);
            for (int seat = 0; seat < 2; seat++)
            {
                LocalSeat local = seat < match.Players.Count ? match.Players[seat] : null;
                setup.seats[seat] = new TableSeat
                {
                    name = local != null ? local.Name : "",
                    computer = local != null && !local.IsHuman,
                    level = local != null ? Mathf.Clamp(local.Level, 0, CheckersAI.Levels - 1) : 1
                };
            }
            return setup;
        }
    }
}
