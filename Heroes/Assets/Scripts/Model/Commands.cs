using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gamebox.Lockstep;

namespace Portfolio.Heroes
{
    /// <summary>
    /// What a player can do. The numbers go into the action log of an online game (the kind of an action), so they are
    /// part of the protocol: only ever append, and keep the server's copy of the ones it tells apart in step
    /// (Server/Lib.cs: the battle commands start at <see cref="FirstBattle"/>, and the server refuses a kind past the
    /// last command on the map or the last of a battle).
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
        // The rules' own: the computer leads the realm of a player who left (online the base server says so with an
        // action of its own, and the table hands the seat over; the log never carries this kind)
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

        /// <summary>
        /// The seat an action of this command is sent for: the player's, or the world's (<see cref="LockstepSeats.World"/>)
        /// for the wandering armies of the wilds (-1), which the host's client moves.
        /// </summary>
        public byte Seat => player >= 0 ? (byte)player : LockstepSeats.World;

        /// <summary>
        /// The arguments as the text an action carries: the five numbers. The kind and the player travel as the kind and
        /// the seat of the action, which the server checks (the sender may act for that seat).
        /// </summary>
        public string Payload()
        {
            return string.Join(",", a.ToString(CultureInfo.InvariantCulture), b.ToString(CultureInfo.InvariantCulture),
                c.ToString(CultureInfo.InvariantCulture), d.ToString(CultureInfo.InvariantCulture), e.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// The command of an action of the log, read with the seat and the kind of the action and never with anything the
        /// payload claims. Null when it is no command a player can send: an unknown kind, the rules' own
        /// <see cref="CommandKind.SeatToComputer"/> (the server says that with an action of its own), a seat that is
        /// neither a player's nor the world's (whose armies only fight), or arguments that are not five numbers.
        /// </summary>
        public static GameCommand Parse(int seat, uint kind, string payload)
        {
            if (kind >= (uint)CommandKind.SeatToComputer || !Enum.IsDefined(typeof(CommandKind), (int)kind))
            {
                return null;
            }
            int player = seat == LockstepSeats.World ? -1 : seat;
            if (player < -1 || player >= LockstepSeats.World || player < 0 && kind < FirstBattle)
            {
                return null;
            }
            string[] parts = (payload ?? "").Split(',');
            if (parts.Length != 5)
            {
                return null;
            }
            var numbers = new int[5];
            for (int i = 0; i < numbers.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]))
                {
                    return null;
                }
            }
            return Of((CommandKind)kind, player, numbers[0], numbers[1], numbers[2], numbers[3], numbers[4]);
        }

        public override string ToString()
        {
            return $"{kind} p{player} ({a}, {b}, {c}, {d}, {e})";
        }
    }

    /// <summary>
    /// What happened, for the view to show: the rules record them as they go, the view plays them afterwards. The numbers
    /// of the battle's events are told at each one (player is always the owner of the stack or side that acts, -1 for
    /// neutral monsters, unless said otherwise). Their cells are cells of the battle: of the battlefield's own grid in
    /// <see cref="BattleStyle.Battlefield"/> (<see cref="BattleState.field"/>, 0 to 164), map cells on the map.
    /// </summary>
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
        /// <summary>
        /// player: the attacker's; a: the map cell of the fight (<see cref="BattleState.mapCell"/>, in both styles); b: the
        /// defending player (-1 for neutrals); c: the monster object, or -1; d: the town, or -1; e: the
        /// <see cref="BattleStyle"/>. <see cref="GameEvent.battle"/> is a copy of the battle as it was deployed: field,
        /// obstacles, walls, gate and every stack where it stands. A battle against nobody ends at once (BattleEnded next).
        /// </summary>
        BattleStarted,
        /// <summary>player: -1; a: the round, from 1.</summary>
        RoundBegan,
        /// <summary>a: the stack whose turn it is; b: its cell.</summary>
        StackTurn,
        /// <summary>a: the stack; b: the cell it left; c: the cell it reached; d: 1 when it flew; cells: the path, the last cell last.</summary>
        StackMoved,
        /// <summary>
        /// A blow in melee. player: the striker's owner; a: the striker; b: the stack struck; c: the damage; d: the
        /// creatures killed; e: 1 for a retaliation. When the blow killed the last of them, StackDied follows.
        /// </summary>
        StackAttacked,
        /// <summary>A shot (a tower's too): the numbers of <see cref="StackAttacked"/>, e always 0.</summary>
        StackShot,
        /// <summary>
        /// Damage that is not a blow of its own: a spell, the splash of a shot, a breath. player: the owner of the stack
        /// hurt; a: the stack hurt; b: the damage; c: the creatures killed; d: the stack whose shot or breath it was, or -1
        /// for a spell. StackDied follows when that was the last of them.
        /// </summary>
        StackDamaged,
        /// <summary>
        /// a: the stack; b: the health restored; c: the creatures raised (0 for regeneration and Cure); d: its cell; e: 1
        /// when the stack had fallen and stands again (Resurrection, Animate Dead), to be put back on the field. The one
        /// event of a spell that raises the dead.
        /// </summary>
        StackHealed,
        /// <summary>a: the stack that fell; b: its cell. Always after the blow, shot or damage that killed it.</summary>
        StackDied,
        /// <summary>a: the stack that waits.</summary>
        StackWaited,
        /// <summary>a: the stack that defends.</summary>
        StackDefended,
        /// <summary>player: the caster's; a: the side (0 attacker, 1 defender); b: the <see cref="SpellId"/>; c: the cell; d: the hero. Its effects follow.</summary>
        SpellCast,
        /// <summary>a: the stack; b: the <see cref="SpellId"/>; c: the rounds it lasts.</summary>
        EffectAdded,
        /// <summary>
        /// a: the stack; b: the <see cref="SpellId"/> of a spell lifted from it before its time: Cure lifts the harmful
        /// ones, each told after its SpellCast and before the StackHealed of the cure (none when the stack was unhurt).
        /// Spells that run out at the start of a round are not told: they are counted down with RoundBegan, and one
        /// cancelled by its opposite goes with the EffectAdded of that one.
        /// </summary>
        EffectRemoved,
        /// <summary>a: the stack that gets another turn (a StackTurn follows).</summary>
        MoraleBoost,
        /// <summary>a: the stack that loses its turn.</summary>
        MoraleFail,
        /// <summary>a: the stack whose next blow or shot (the event after this one) is doubled.</summary>
        LuckyStrike,
        /// <summary>
        /// player: the attacker's; a: the <see cref="BattleResult"/>; b: the attacking hero; c: the defending hero, or -1;
        /// d: the monster object, or -1; e: the town, or -1; text: why a battle nobody could win was broken off, or null.
        /// <see cref="GameEvent.battle"/> is a copy of the battle as it ended (losses included). It comes before what the
        /// battle does to the map (HeroDefeated, TownCaptured, HeroMoved, ObjectRemoved, experience).
        /// </summary>
        BattleEnded,
        /// <summary>Necromancy after a battle: player: the hero's owner; a: the hero; b: the creature; c: how many; text.</summary>
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

        /// <summary>
        /// A copy of the battle as it began, on <see cref="EventKind.BattleStarted"/>, and as it ended, on
        /// <see cref="EventKind.BattleEnded"/>: the view builds the field from it, because the rules may already be further
        /// along by the time the event is played (online they run ahead). Never saved or sent.
        /// </summary>
        public BattleState battle;

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
