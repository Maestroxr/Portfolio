using Gamebox;
using UnityEngine;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// What Board Games offers at one device: two seats, either of which the computer may take (somebody has to be a
    /// person), the rule set to play (the three draughts rules, chess, backgammon), who starts, a clock per decision and
    /// the backgammon doubling cube. Seat 1 plays the side that moves first unless the "first" option says otherwise (in
    /// backgammon "Rules decide" is the opening roll, with seat 1 playing White; a chosen seat plays White and starts
    /// without it); each seat shows the side it plays, in the words of the game, and takes that side's colour from the
    /// look shown (<see cref="Refresh"/>). <see cref="SetupOf"/> turns the setup into the table of a game.
    /// </summary>
    public sealed class BoardGamesLocalRules : LocalPlayRules
    {
        public const string RuleSetOption = "ruleset";
        public const string FirstOption = "first";
        public const string ClockOption = "clock";
        public const string CubeOption = "cube";

        /// <summary>The seconds of a decision for each choice of the clock option; 0 plays without a clock.</summary>
        public static readonly int[] ClockSeconds = { 0, 30, 60, 120, 300 };

        private static readonly string[] FirstChoices = { "Rules decide", "Player 1", "Player 2" };
        private static readonly string[] ClockChoices = { "None", "30 seconds", "1 minute", "2 minutes", "5 minutes" };
        /// <summary>The cube option's choices: index 0 plays with the doubling cube.</summary>
        private static readonly string[] CubeChoices = { "On", "Off" };

        public BoardGamesLocalRules()
        {
            Game = GameType.BoardGames;
            Title = "Local Play";
            Hint = "Two people at this device, or one against the computer.";
            MinPlayers = 2;
            MaxPlayers = 2;
            DefaultPlayers = 2;
            AllowComputers = true;
            MinHumans = 1;
            OffersHandOver = false;
            HandOverByDefault = false;
            var ruleSets = new string[RuleSets.Count];
            for (int i = 0; i < ruleSets.Length; i++)
            {
                ruleSets[i] = RuleSets.NameKey(RuleSets.All[i]);
            }
            Options.Add(new LocalOption(RuleSetOption, "Rules", ruleSets));
            Options.Add(new LocalOption(FirstOption, "Who moves first", FirstChoices));
            Options.Add(new LocalOption(ClockOption, "Time per move", ClockChoices));
            Options.Add(new LocalOption(CubeOption, "Doubling cube", CubeChoices));
            Refresh(null);
        }

        /// <summary>Before the setup opens: the seats take the colours of the two sides in the look shown.</summary>
        public void Refresh(BoardGamesTheme theme)
        {
            SeatColors = new Color[2];
            for (int seat = 0; seat < 2; seat++)
            {
                Side side = SideOfSeat(seat, FindOption(RuleSetOption).Index, FindOption(FirstOption).Index);
                SeatColors[seat] = theme != null ? theme.ColorOf(side) : (side == Side.Light ? Color.white : new Color(0.25f, 0.25f, 0.25f));
            }
        }

        /// <summary>
        /// Makes <paramref name="match"/> play <paramref name="ruleSet"/>'s game (the title's): when its rule set is of
        /// another game it moves to <paramref name="ruleSet"/>, and the cube option to <paramref name="cube"/>. True when
        /// something changed.
        /// </summary>
        public static bool Preselect(LocalMatch match, RuleSet ruleSet, bool cube)
        {
            if (match == null || RuleSets.ModeOf(match.Option(RuleSetOption)) == RuleSets.ModeOf(ruleSet) && RuleSets.IsValid(match.Option(RuleSetOption)))
            {
                return false;
            }
            match.SetOption(RuleSetOption, (int)ruleSet);
            match.SetOption(CubeOption, cube ? 0 : 1);
            return true;
        }

        /// <summary>The seat that plays the side moving first (White in chess and backgammon), for the options chosen.</summary>
        public static int FirstSeatOf(int ruleSet, int first)
        {
            switch (first)
            {
                case 1: return 0;
                case 2: return 1;
                default: return RuleSets.FirstMover(RuleSets.Of(ruleSet)) == Side.Light ? 0 : 1;
            }
        }

        /// <summary>The side a seat plays, for the options chosen.</summary>
        public static Side SideOfSeat(int seat, int ruleSet, int first)
        {
            Side mover = RuleSets.FirstMover(RuleSets.Of(ruleSet));
            return seat == FirstSeatOf(ruleSet, first) ? mover : Sides.Other(mover);
        }

        /// <summary>The seconds per decision the setup chose (0 for no clock).</summary>
        public static int ClockSecondsOf(LocalMatch match)
        {
            int choice = match != null ? match.Option(ClockOption) : 0;
            return choice >= 0 && choice < ClockSeconds.Length ? ClockSeconds[choice] : 0;
        }

        /// <summary>Whether the setup plays backgammon with the doubling cube (on unless the option says off).</summary>
        public static bool CubeOf(LocalMatch match)
        {
            return match == null || match.Option(CubeOption) != 1;
        }

        /// <summary>An option changed: the seats take the colours of the sides they play now.</summary>
        public override void OptionsChanged(LocalMatch match)
        {
            Color[] colors = SeatColors;
            Side lightSide = SideOfSeat(0, FindOption(RuleSetOption).Index, FindOption(FirstOption).Index);
            Color lightColor = colors != null && colors.Length == 2 ? colors[lightSide == Side.Light ? 0 : 1] : Color.white;
            Color darkColor = colors != null && colors.Length == 2 ? colors[lightSide == Side.Light ? 1 : 0] : new Color(0.25f, 0.25f, 0.25f);
            for (int seat = 0; seat < match.AllSeats.Count && seat < 2; seat++)
            {
                Side side = SideOfSeat(seat, match.Option(RuleSetOption), match.Option(FirstOption));
                match[seat].Color = side == Side.Light ? lightColor : darkColor;
            }
        }

        /// <summary>The side the seat plays, in the words of the rule set chosen and the language shown.</summary>
        public override string SeatDetail(LocalMatch match, int seat)
        {
            if (match == null || seat < 0 || seat > 1)
            {
                return null;
            }
            int ruleSet = match.Option(RuleSetOption);
            return L.T(RuleSets.SideNameKey(RuleSets.Of(ruleSet), SideOfSeat(seat, ruleSet, match.Option(FirstOption))));
        }

        /// <summary>
        /// The table of <paramref name="match"/>: its rule set, its two seats with their names and levels, who moves first,
        /// the clock, and for backgammon the cube and whether the opening roll decides who starts.
        /// </summary>
        public static TableSetup SetupOf(LocalMatch match)
        {
            var setup = new TableSetup();
            int ruleSet = Mathf.Clamp(match.Option(RuleSetOption), 0, RuleSets.Count - 1);
            int first = match.Option(FirstOption);
            setup.ruleSet = ruleSet;
            setup.firstSeat = FirstSeatOf(ruleSet, first);
            setup.clockSeconds = ClockSecondsOf(match);
            setup.doublingCube = CubeOf(match);
            setup.openingRoll = first != 1 && first != 2;
            for (int seat = 0; seat < 2; seat++)
            {
                LocalSeat local = seat < match.Players.Count ? match.Players[seat] : null;
                setup.seats[seat] = new TableSeat
                {
                    name = local != null ? local.Name : "",
                    computer = local != null && !local.IsHuman,
                    level = local != null ? Mathf.Clamp(local.Level, 0, RuleSets.ComputerLevels - 1) : 1
                };
            }
            return setup;
        }
    }
}
