using System.Collections.Generic;
using Gamebox.Lockstep;

namespace Portfolio.Heroes
{
    /// <summary>
    /// A game several devices play in step: each runs this class with the same map (generated from the room's seed) and
    /// feeds it the same actions in the same order (the server's action log), so all of them hold the same game after
    /// every entry of the log. Nothing else may change the game. The random numbers of an action (damage, luck, level
    /// ups) come from the number the server stamped on it, which nobody knows before the action is in the log. How the
    /// entries are taken is the shared <see cref="LockstepTable{TCommand}"/>; this class says what they mean in Heroes.
    /// </summary>
    public sealed class LockstepGame : LockstepTable<GameCommand>
    {
        /// <summary>
        /// Whether the game waits in a battle that a move on the map opened while the clock of the room ran on: the server
        /// cannot see battles and restarts its clock only on the moves <see cref="RestartsClock"/> names, so the time left
        /// is what was left of the turn on the map, often a few seconds, and it would run out on the first decision of
        /// the battle while the battle is still coming onto the screens. Worked out from the log alone, like the game.
        /// </summary>
        private bool battleOnTheMapsClock;

        public LockstepGame(GameState state) : base(state.seed)
        {
            Game = new HeroesGame(state, Random);
        }

        public HeroesGame Game { get; }

        public override bool IsOver => Game.IsOver;

        /// <summary>
        /// The server called time: whoever the game waits for makes the default move. A choice takes its first option, a
        /// troop in battle defends, a turn on the map ends. Returns the command that was made, or null. The clock of a
        /// turn on the map that runs out on a battle opened in that turn makes no move: its timeout restarted the clock,
        /// so the first decision of every battle has a whole turn of the clock.
        /// </summary>
        public new GameCommand Timeout(uint stamp)
        {
            List<GameCommand> made = base.Timeout(stamp);
            return made.Count > 0 ? made[0] : null;
        }

        protected override bool Accept(GameCommand command)
        {
            bool restarts = RestartsClock(command);
            bool inBattle = Game.InBattle;
            bool accepted = Game.Apply(command);
            if (restarts)
            {
                battleOnTheMapsClock = false;
            }
            else if (accepted && !inBattle && Game.InBattle)
            {
                battleOnTheMapsClock = true;
            }
            return accepted;
        }

        protected override void DefaultMoves(List<GameCommand> made)
        {
            bool battleJustOpened = battleOnTheMapsClock && Game.InBattle;
            battleOnTheMapsClock = false;
            if (battleJustOpened)
            {
                return;
            }
            GameCommand command = DefaultMove(Game);
            if (command != null && Game.Apply(command))
            {
                made.Add(command);
            }
        }

        /// <summary>The realm of a player who left is led by the computer from now on; the rules say so in the log of the game.</summary>
        protected override bool SeatToComputer(int seat, int level)
        {
            // The server restarts the clock with this entry, as with every entry of its own.
            battleOnTheMapsClock = false;
            return Game.Apply(GameCommand.Of(CommandKind.SeatToComputer, seat, level));
        }

        /// <summary>
        /// Whether the server restarts the clock of the room on <paramref name="command"/>, at its place in the log: the
        /// end of a turn, the answer to a choice, every move in a battle, and whatever the host sends for the computer
        /// players and the wilds. The same list as ValidateAction of Server/Lib.cs; were they ever to differ, the clients
        /// would still agree with each other, only the first decision of a battle would get a turn of the clock too many
        /// or too few.
        /// </summary>
        private bool RestartsClock(GameCommand command)
        {
            return command.player < 0 || Game.IsComputer(command.player) || command.kind == CommandKind.EndTurn ||
                   command.kind == CommandKind.Choose || command.IsBattle;
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
        public override uint Checksum()
        {
            return Checksum(Game.State);
        }

        public static uint Checksum(GameState s)
        {
            var sum = new StateChecksum();
            sum.Add(s.day).Add(s.currentPlayer).Add(s.over).Add(s.winner).Add(s.pending.Count).Add((int)s.rules.battleStyle);
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
                sum.Add((int)battle.style).Add(battle.center).Add(battle.mapCell).Add(battle.gate);
                sum.Add(battle.blocked).Add(battle.obstacleCells).Add(battle.obstacleKinds).Add(battle.walls).Add(battle.waiting);
                foreach (BattleStack stack in battle.stacks)
                {
                    sum.Add(stack.alive).Add(stack.cell).Add(stack.count).Add(stack.health).Add(stack.shots).Add(stack.acted).Add(stack.retaliations);
                    sum.Add(stack.waited).Add(stack.defending).Add(stack.moraleUsed).Add(stack.effects.Count);
                    foreach (EffectState effect in stack.effects)
                    {
                        sum.Add(effect.spell).Add(effect.rounds);
                    }
                }
            }
            return sum.Value;
        }
    }
}
