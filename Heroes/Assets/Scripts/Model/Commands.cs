using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Portfolio.Heroes
{
    /// <summary>
    /// What a player can do. The numbers go into the action log of an online game (the kind of an action), so they are
    /// part of the protocol: only ever append, and keep the server's copy of the ones it tells apart in step
    /// (Server/Lib.cs: the battle commands start at <see cref="FirstBattle"/>).
    /// </summary>
    public enum CommandKind
    {
        // Adventure
        MoveHero = 0,
        EndTurn = 1,
        Build = 2,
        Recruit = 3,
        HireHero = 4,
        MoveArmy = 5,
        Trade = 6,
        Choose = 7,
        DismissStack = 8,
        SleepHero = 9,
        VisitTown = 10,
        // Battle
        BattleMove = 20,
        BattleAttack = 21,
        BattleShoot = 22,
        BattleWait = 23,
        BattleDefend = 24,
        BattleCast = 25,
        BattleRetreat = 26,
        // The server's own
        SeatToComputer = 40
    }

    /// <summary>
    /// A command as data: what the interface asks the rules for, what a computer player decides on and what an online game
    /// sends through the action log. Five numbers carry its arguments; their meaning depends on the kind.
    /// </summary>
    [Serializable]
    public sealed class GameCommand
    {
        public const int FirstBattle = 20;

        public CommandKind kind;
        public int player;
        public int a;
        public int b;
        public int c;
        public int d;
        public int e;

        public bool IsBattle => (int)kind >= FirstBattle && (int)kind < (int)CommandKind.SeatToComputer;

        public static GameCommand Of(CommandKind kind, int player, int a = 0, int b = 0, int c = 0, int d = 0, int e = 0)
        {
            return new GameCommand { kind = kind, player = player, a = a, b = b, c = c, d = d, e = e };
        }

        // ------------------------------------------------------------------ adventure

        public static GameCommand Move(int player, int hero, int cell) => Of(CommandKind.MoveHero, player, hero, cell);

        public static GameCommand EndTurn(int player) => Of(CommandKind.EndTurn, player);

        public static GameCommand Build(int player, int town, BuildingId building) => Of(CommandKind.Build, player, town, (int)building);

        /// <summary>Recruits <paramref name="count"/> of a town's tier (1 to 7) into its garrison, or into the visiting hero's army with <paramref name="toHero"/>.</summary>
        public static GameCommand Recruit(int player, int town, int tier, int count, bool toHero) => Of(CommandKind.Recruit, player, town, tier, count, toHero ? 1 : 0);

        /// <summary>Recruits from a dwelling on the map: <paramref name="dwelling"/> is the object, for the hero visiting it.</summary>
        public static GameCommand RecruitAtDwelling(int player, int dwelling, int hero, int count) => Of(CommandKind.Recruit, player, dwelling, 0, count, 2, hero);

        public static GameCommand Hire(int player, int town, int tavernSlot) => Of(CommandKind.HireHero, player, town, tavernSlot);

        /// <summary>
        /// Moves creatures between armies: a container is a hero id, or -(town id + 1) for a town's garrison.
        /// <paramref name="count"/> 0 moves the whole slot (swapping with what is there when the creatures differ).
        /// </summary>
        public static GameCommand MoveArmy(int player, int from, int fromSlot, int to, int toSlot, int count) => Of(CommandKind.MoveArmy, player, from, fromSlot, to, toSlot, count);

        public static int Garrison(int town) => -(town + 1);

        public static GameCommand Trade(int player, ResourceKind give, ResourceKind get, int amount) => Of(CommandKind.Trade, player, (int)give, (int)get, amount);

        /// <summary>Answers the first pending choice of the player with option <paramref name="option"/>.</summary>
        public static GameCommand Choose(int player, int option) => Of(CommandKind.Choose, player, option);

        public static GameCommand Dismiss(int player, int container, int slot) => Of(CommandKind.DismissStack, player, container, slot);

        public static GameCommand Sleep(int player, int hero, bool sleep) => Of(CommandKind.SleepHero, player, hero, sleep ? 1 : 0);

        // ------------------------------------------------------------------ battle

        public static GameCommand BattleMove(int player, int stack, int cell) => Of(CommandKind.BattleMove, player, stack, cell);

        /// <summary>Walks (or flies) to <paramref name="from"/> and strikes <paramref name="target"/>.</summary>
        public static GameCommand BattleAttack(int player, int stack, int target, int from) => Of(CommandKind.BattleAttack, player, stack, target, from);

        public static GameCommand BattleShoot(int player, int stack, int target) => Of(CommandKind.BattleShoot, player, stack, target);

        public static GameCommand BattleWait(int player, int stack) => Of(CommandKind.BattleWait, player, stack);

        public static GameCommand BattleDefend(int player, int stack) => Of(CommandKind.BattleDefend, player, stack);

        public static GameCommand BattleCast(int player, SpellId spell, int cell) => Of(CommandKind.BattleCast, player, (int)spell, cell);

        public static GameCommand BattleRetreat(int player) => Of(CommandKind.BattleRetreat, player);

        // ------------------------------------------------------------------ as text, for the action log

        public string Payload()
        {
            return string.Join(",", ((int)kind).ToString(CultureInfo.InvariantCulture), player.ToString(CultureInfo.InvariantCulture),
                a.ToString(CultureInfo.InvariantCulture), b.ToString(CultureInfo.InvariantCulture), c.ToString(CultureInfo.InvariantCulture),
                d.ToString(CultureInfo.InvariantCulture), e.ToString(CultureInfo.InvariantCulture));
        }

        public static GameCommand Parse(string payload)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return null;
            }
            string[] parts = payload.Split(',');
            if (parts.Length < 7)
            {
                return null;
            }
            var numbers = new int[7];
            for (int i = 0; i < 7; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]))
                {
                    return null;
                }
            }
            if (!Enum.IsDefined(typeof(CommandKind), numbers[0]))
            {
                return null;
            }
            return Of((CommandKind)numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], numbers[5], numbers[6]);
        }

        public override string ToString()
        {
            return $"{kind} p{player} ({a}, {b}, {c}, {d}, {e})";
        }
    }

    /// <summary>What happened, for the view to show: the rules record them as they go, the view plays them afterwards.</summary>
    public enum EventKind
    {
        Message,
        DayBegan,
        WeekBegan,
        TurnBegan,
        HeroMoved,
        HeroStopped,
        ResourcesGained,
        ObjectRemoved,
        ObjectCaptured,
        ObjectVisited,
        ArtifactFound,
        SpellLearned,
        ExperienceGained,
        HeroLeveled,
        StatRaised,
        TownBuilt,
        CreaturesRecruited,
        HeroHired,
        HeroDefeated,
        TownCaptured,
        ArmyChanged,
        Revealed,
        BattleStarted,
        RoundBegan,
        StackTurn,
        StackMoved,
        StackAttacked,
        StackShot,
        StackDamaged,
        StackHealed,
        StackDied,
        StackWaited,
        StackDefended,
        SpellCast,
        EffectAdded,
        MoraleBoost,
        MoraleFail,
        LuckyStrike,
        BattleEnded,
        CreaturesRaised,
        PlayerEliminated,
        GameOver,
        ChoiceNeeded
    }

    public sealed class GameEvent
    {
        public EventKind kind;
        public int player = -1;
        public int a;
        public int b;
        public int c;
        public int d;
        public int e;
        public List<int> cells;
        public string text;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(kind).Append(" p").Append(player).Append(" (").Append(a).Append(", ").Append(b).Append(", ").Append(c).Append(", ").Append(d).Append(')');
            if (!string.IsNullOrEmpty(text))
            {
                builder.Append(" \"").Append(text).Append('"');
            }
            return builder.ToString();
        }
    }
}
