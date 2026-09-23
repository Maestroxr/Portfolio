using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>
    /// The rules of Heroes: it holds a <see cref="GameState"/>, takes commands (<see cref="Apply"/>) and reports what
    /// happened as a list of <see cref="GameEvent"/>s for the view to play. It is plain C# without scene objects and uses
    /// only whole number arithmetic and the random numbers it is given, so every device that feeds it the same
    /// commands with the same random numbers holds the same game: that is how online games are played (lockstep over
    /// the action log of the server, see <see cref="LockstepGame"/>).
    ///
    /// The partial files hold the parts: turns and days (Turns), the adventure map (Map), towns and armies (Towns),
    /// heroes, experience and choices (Heroes) and battles on the map (Battle, BattleActions).
    /// </summary>
    public sealed partial class HeroesGame
    {
        private readonly List<GameEvent> events = new List<GameEvent>();
        private readonly List<int> scratch = new List<int>(8);

        public HeroesGame(GameState state, IRandom random)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Random = random ?? new SeededRandom(state.seed);
        }

        public GameState State { get; }

        public IRandom Random { get; set; }

        public HexGrid Grid => State.map.grid;

        public MapData Map => State.map;

        public bool IsOver => State.over;

        public bool InBattle => State.battle.active;

        public BattleState Battle => State.battle;

        public PlayerState CurrentPlayer => State.Player(State.currentPlayer);

        public bool HasEvents => events.Count > 0;

        /// <summary>Takes the events recorded since the last call, in order.</summary>
        public List<GameEvent> TakeEvents()
        {
            var taken = new List<GameEvent>(events);
            events.Clear();
            return taken;
        }

        /// <summary>
        /// The player the game waits for: the one who has a choice to make, else the owner of the troop whose turn it is
        /// in a battle (-1 for neutral monsters, which the host plays), else the player whose turn it is. -2 when the
        /// game is over.
        /// </summary>
        public int WaitingPlayer
        {
            get
            {
                if (State.over)
                {
                    return -2;
                }
                if (State.pending.Count > 0)
                {
                    return State.pending[0].player;
                }
                if (InBattle)
                {
                    BattleStack stack = Battle.Current;
                    return stack != null ? Battle.PlayerOf(stack.side) : -1;
                }
                return State.currentPlayer;
            }
        }

        /// <summary>Whether <paramref name="player"/> is played by the computer (neutral monsters, -1, are).</summary>
        public bool IsComputer(int player)
        {
            PlayerState state = State.Player(player);
            return state == null || !state.human;
        }

        // ------------------------------------------------------------------ commands

        /// <summary>Carries out <paramref name="command"/> when the rules allow it now. Returns whether it was accepted.</summary>
        public bool Apply(GameCommand command)
        {
            if (command == null || State.over)
            {
                return false;
            }
            if (command.kind == CommandKind.SeatToComputer)
            {
                PlayerState seat = State.Player(command.player);
                if (seat == null)
                {
                    return false;
                }
                seat.human = false;
                seat.aiLevel = Math.Max(0, Math.Min(2, command.a));
                Log($"{seat.name} is now played by the computer.");
                return true;
            }
            if (command.kind == CommandKind.Choose)
            {
                return Choose(command.player, command.a);
            }
            // A choice blocks everything else of its player (and, being first in line, the game).
            if (State.pending.Count > 0)
            {
                return false;
            }
            if (command.IsBattle)
            {
                return InBattle && ApplyBattle(command);
            }
            if (InBattle || command.player != State.currentPlayer)
            {
                return false;
            }
            switch (command.kind)
            {
                case CommandKind.MoveHero: return MoveHero(command.player, command.a, command.b);
                case CommandKind.EndTurn: return EndTurn(command.player);
                case CommandKind.Build: return Build(command.player, command.a, (BuildingId)command.b);
                case CommandKind.Recruit:
                    return command.d == 2
                        ? RecruitAtDwelling(command.player, command.a, command.e, command.c)
                        : Recruit(command.player, command.a, command.b, command.c, command.d == 1);
                case CommandKind.HireHero: return Hire(command.player, command.a, command.b);
                case CommandKind.MoveArmy: return MoveArmy(command.player, command.a, command.b, command.c, command.d, command.e);
                case CommandKind.Trade: return Trade(command.player, (ResourceKind)command.a, (ResourceKind)command.b, command.c);
                case CommandKind.DismissStack: return Dismiss(command.player, command.a, command.b);
                case CommandKind.SleepHero: return Sleep(command.player, command.a, command.b != 0);
                default: return false;
            }
        }

        // ------------------------------------------------------------------ events

        private GameEvent Emit(EventKind kind, int player = -1, int a = 0, int b = 0, int c = 0, int d = 0, int e = 0, string text = null, List<int> cells = null)
        {
            var e2 = new GameEvent { kind = kind, player = player, a = a, b = b, c = c, d = d, e = e, text = text, cells = cells };
            events.Add(e2);
            return e2;
        }

        private void Log(string text, int player = -1)
        {
            Emit(EventKind.Message, player, text: text);
        }

        // ------------------------------------------------------------------ heroes: what they add up to

        public IEnumerable<ArtifactDef> Worn(HeroState hero)
        {
            foreach (int id in hero.equipped)
            {
                ArtifactDef def = Artifacts.Get((ArtifactId)id);
                if (def != null)
                {
                    yield return def;
                }
            }
        }

        public int Stat(HeroState hero, PrimaryStat stat)
        {
            int value;
            switch (stat)
            {
                case PrimaryStat.Attack: value = hero.attack; break;
                case PrimaryStat.Defense: value = hero.defense; break;
                case PrimaryStat.Power: value = hero.power; break;
                default: value = hero.knowledge; break;
            }
            foreach (ArtifactDef artifact in Worn(hero))
            {
                switch (stat)
                {
                    case PrimaryStat.Attack: value += artifact.Attack; break;
                    case PrimaryStat.Defense: value += artifact.Defense; break;
                    case PrimaryStat.Power: value += artifact.Power; break;
                    default: value += artifact.Knowledge; break;
                }
            }
            return Math.Max(stat == PrimaryStat.Knowledge || stat == PrimaryStat.Power ? 1 : 0, value);
        }

        public int MaxMana(HeroState hero)
        {
            return Stat(hero, PrimaryStat.Knowledge) * 10;
        }

        public int Morale(HeroState hero)
        {
            if (hero == null)
            {
                return 0;
            }
            int morale = hero.SkillLevel(SkillId.Leadership) + hero.moraleBonus;
            foreach (ArtifactDef artifact in Worn(hero))
            {
                morale += artifact.Morale;
            }
            // Troops of several factions distrust each other.
            var factions = new HashSet<Faction>();
            bool living = false;
            foreach (ArmySlot slot in hero.army.slots)
            {
                if (!slot.IsEmpty)
                {
                    factions.Add(slot.Def.Faction);
                    living |= !slot.Def.IsUndead;
                }
            }
            if (factions.Count > 2)
            {
                morale -= factions.Count - 2;
            }
            if (living && factions.Contains(Faction.Necropolis) && factions.Count > 1)
            {
                morale -= 1;
            }
            return Math.Max(-3, Math.Min(3, morale));
        }

        public int Luck(HeroState hero)
        {
            if (hero == null)
            {
                return 0;
            }
            int luck = hero.SkillLevel(SkillId.Luck) + hero.luckBonus;
            foreach (ArtifactDef artifact in Worn(hero))
            {
                luck += artifact.Luck;
            }
            return Math.Max(-3, Math.Min(3, luck));
        }

        /// <summary>Extra health and speed the hero's artifacts give every creature of the army.</summary>
        public int ArmyHealthBonus(HeroState hero)
        {
            int bonus = 0;
            if (hero != null)
            {
                foreach (ArtifactDef artifact in Worn(hero))
                {
                    bonus += artifact.Health;
                }
            }
            return bonus;
        }

        public int ArmySpeedBonus(HeroState hero)
        {
            int bonus = 0;
            if (hero != null)
            {
                foreach (ArtifactDef artifact in Worn(hero))
                {
                    bonus += artifact.Speed;
                }
            }
            return bonus;
        }

        /// <summary>Movement points the hero has at the start of a day.</summary>
        public int DailyMovement(HeroState hero)
        {
            int slowest = hero.army.SlowestSpeed + ArmySpeedBonus(hero);
            int points = 1400 + 70 * Math.Max(0, Math.Min(8, slowest - 3));
            int logistics = hero.SkillLevel(SkillId.Logistics);
            points += points * logistics * 10 / 100;
            foreach (ArtifactDef artifact in Worn(hero))
            {
                points += artifact.Movement;
            }
            if (hero.stablesWeek == State.Week)
            {
                points += 400;
            }
            return points;
        }

        public int Sight(HeroState hero)
        {
            return HeroData.BaseSight + hero.SkillLevel(SkillId.Scouting);
        }

        public int SpellLevelLimit(HeroState hero)
        {
            return 2 + hero.SkillLevel(SkillId.Wisdom);
        }

        /// <summary>The strength of an army for the computer players' plans: the value of its creatures, raised by the hero's skills.</summary>
        public int Strength(Army army, HeroState hero = null)
        {
            long total = 0;
            foreach (ArmySlot slot in army.slots)
            {
                if (!slot.IsEmpty)
                {
                    total += (long)slot.Def.Value * slot.count;
                }
            }
            if (hero != null)
            {
                int bonus = Stat(hero, PrimaryStat.Attack) + Stat(hero, PrimaryStat.Defense);
                total = total * (100 + bonus * 4) / 100;
                total += Stat(hero, PrimaryStat.Power) * Math.Min(hero.mana, MaxMana(hero)) * 6;
            }
            return (int)Math.Min(total, int.MaxValue);
        }

        public int Strength(HeroState hero)
        {
            return Strength(hero.army, hero);
        }

        // ------------------------------------------------------------------ fog

        public void Reveal(int player, int center, int radius)
        {
            PlayerState state = State.Player(player);
            if (state == null)
            {
                return;
            }
            bool changed = false;
            foreach (int cell in Grid.Disk(center, radius))
            {
                if (state.explored[cell] == 0)
                {
                    state.explored[cell] = 1;
                    changed = true;
                }
            }
            if (changed)
            {
                Emit(EventKind.Revealed, player, center, radius);
            }
        }

        /// <summary>Everything a player's heroes and towns see now (at the start and after loading).</summary>
        public void RevealAll(int player)
        {
            PlayerState state = State.Player(player);
            if (state == null)
            {
                return;
            }
            foreach (int id in state.heroes)
            {
                HeroState hero = State.Hero(id);
                if (hero != null && hero.alive)
                {
                    Reveal(player, hero.cell, Sight(hero));
                }
            }
            foreach (int id in state.towns)
            {
                TownState town = State.Town(id);
                if (town != null)
                {
                    Reveal(player, town.center, 7);
                }
            }
        }
    }
}
