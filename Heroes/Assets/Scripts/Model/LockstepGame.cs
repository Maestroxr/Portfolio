using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>
    /// A game several devices play in step: each runs this class with the same map (generated from the room's seed) and
    /// feeds it the same actions in the same order (the server's action log), so all of them hold the same game after
    /// every entry of the log. Nothing else may change the game. The random numbers of an action (damage, luck, level
    /// ups) come from the number the server stamped on it, which nobody knows before the action is in the log.
    /// </summary>
    public sealed class LockstepGame
    {
        private readonly SeededRandom random;

        public LockstepGame(GameState state)
        {
            random = new SeededRandom(state.seed);
            Game = new HeroesGame(state, random) { IsOnlineTable = true };
        }

        public HeroesGame Game { get; }

        /// <summary>Entries of the log taken so far, accepted or not.</summary>
        public int Applied { get; private set; }

        /// <summary>
        /// Takes an action of the log: <paramref name="command"/> (null when it could not be read) with the number the
        /// server stamped on it. Returns whether the rules accepted it; a refused action changes nothing.
        /// </summary>
        public bool Apply(GameCommand command, uint stamp)
        {
            Applied++;
            if (command == null)
            {
                return false;
            }
            random.Reseed(stamp);
            return Game.Apply(command);
        }

        /// <summary>
        /// The server called time: whoever the game waits for makes the default move. A choice takes its first option, a
        /// troop in battle defends, a turn on the map ends. Returns the command that was made, or null.
        /// </summary>
        public GameCommand Timeout(uint stamp)
        {
            Applied++;
            random.Reseed(stamp);
            GameCommand command = DefaultMove(Game);
            if (command != null && !Game.Apply(command))
            {
                return null;
            }
            return command;
        }

        public static GameCommand DefaultMove(HeroesGame game)
        {
            if (game.IsOver)
            {
                return null;
            }
            GameState state = game.State;
            if (state.pending.Count > 0)
            {
                return GameCommand.Choose(state.pending[0].player, 0);
            }
            if (game.InBattle)
            {
                BattleStack stack = game.Battle.Current;
                if (stack == null)
                {
                    return null;
                }
                return GameCommand.BattleDefend(game.Battle.PlayerOf(stack.side), stack.id);
            }
            return GameCommand.EndTurn(state.currentPlayer);
        }

        /// <summary>A number that is the same on two devices exactly when their games are (everything the rules look at, folded).</summary>
        public uint Checksum()
        {
            return Checksum(Game.State);
        }

        public static uint Checksum(GameState s)
        {
            var sum = new Fold();
            sum.Add(s.day).Add(s.currentPlayer).Add(s.over).Add(s.winner).Add(s.pending.Count);
            foreach (PlayerState player in s.players)
            {
                sum.Add(player.alive).Add(player.human).Add(player.daysWithoutTown);
                foreach (int value in player.resources.values)
                {
                    sum.Add(value);
                }
                sum.Add(player.heroes).Add(player.towns);
            }
            foreach (HeroState hero in s.heroes)
            {
                sum.Add(hero.alive).Add(hero.owner).Add(hero.cell).Add(hero.level).Add(hero.experience).Add(hero.movement).Add(hero.mana);
                sum.Add(hero.attack).Add(hero.defense).Add(hero.power).Add(hero.knowledge);
                foreach (ArmySlot slot in hero.army.slots)
                {
                    sum.Add(slot.creature).Add(slot.count);
                }
                sum.Add(hero.spells);
                foreach (SkillEntry skill in hero.skills)
                {
                    sum.Add(skill.skill).Add(skill.level);
                }
            }
            foreach (TownState town in s.towns)
            {
                sum.Add(town.owner).Add(town.builtToday).Add(town.built);
                foreach (int available in town.available)
                {
                    sum.Add(available);
                }
                foreach (ArmySlot slot in town.garrison.slots)
                {
                    sum.Add(slot.creature).Add(slot.count);
                }
            }
            foreach (MapObject obj in s.objects)
            {
                sum.Add(obj.removed).Add(obj.owner).Add(obj.amount).Add(obj.visitedBy.Count);
            }
            BattleState battle = s.battle;
            sum.Add(battle.active);
            if (battle.active)
            {
                sum.Add(battle.round).Add(battle.current).Add(battle.attackerCast).Add(battle.defenderCast);
                foreach (BattleStack stack in battle.stacks)
                {
                    sum.Add(stack.alive).Add(stack.cell).Add(stack.count).Add(stack.health).Add(stack.shots).Add(stack.acted).Add(stack.retaliations);
                }
            }
            return sum.Value;
        }

        /// <summary>FNV-1a over the numbers it is given.</summary>
        private sealed class Fold
        {
            public uint Value { get; private set; } = 2166136261u;

            public Fold Add(int number)
            {
                uint bits = (uint)number;
                for (int i = 0; i < 4; i++)
                {
                    Value = (Value ^ (bits & 0xFFu)) * 16777619u;
                    bits >>= 8;
                }
                return this;
            }

            public Fold Add(bool flag)
            {
                return Add(flag ? 1 : 0);
            }

            public Fold Add(List<int> numbers)
            {
                Add(numbers.Count);
                foreach (int number in numbers)
                {
                    Add(number);
                }
                return this;
            }
        }
    }
}
