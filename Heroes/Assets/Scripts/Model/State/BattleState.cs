using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public enum BattleResult
    {
        None = 0,
        AttackerWon = 1,
        DefenderWon = 2,
        AttackerFled = 3,
        DefenderFled = 4
    }

    /// <summary>
    /// What stands on a cell of a battlefield of its own (<see cref="BattleStyle.Battlefield"/>): every kind is in the way
    /// of the troops that walk, and the view draws each kind with props of the battle's terrain.
    /// </summary>
    public enum BattleObstacle : byte
    {
        None = 0,
        Rock = 1,
        Boulder = 2,
        Tree = 3,
        Pine = 4,
        DeadTree = 5,
        Stump = 6,
        Logs = 7,
        Bones = 8,
        Crystal = 9,
        Mound = 10,
        Pool = 11,
        Rubble = 12,
        Crater = 13,
        /// <summary>The wall of a town under siege.</summary>
        Wall = 14
    }

    /// <summary>
    /// A battle, fought in one of two styles (<see cref="BattleStyle"/>). On the map, the battlefield is a block of the
    /// map's own cells around the place the armies met (fifteen columns by eleven rows at most), and whatever stands
    /// there (forests, rocks, water, buildings) is in the way of the troops as it is on the map; every cell of the battle
    /// is a map cell. On a battlefield of its own, the field is a grid of its own (<see cref="field"/>, fifteen by eleven)
    /// strewn with obstacles of the land the armies met on, and every cell of the battle (of the stacks, the commands and
    /// the events) is a cell of that grid; only <see cref="mapCell"/>, <see cref="attackerCell"/> and
    /// <see cref="targetCell"/> are map cells. Side 0 attacks, side 1 defends.
    /// </summary>
    [Serializable]
    public sealed class BattleState
    {
        public const int HalfWidth = 7;
        public const int HalfHeight = 5;
        /// <summary>The size of a battlefield of its own, as in the original game.</summary>
        public const int FieldColumns = 2 * HalfWidth + 1;
        public const int FieldRows = 2 * HalfHeight + 1;

        public bool active;
        public int attackerHero = -1;
        public int defenderHero = -1;
        public int attackerPlayer = -1;
        /// <summary>-1 for neutral monsters (and neutral towns).</summary>
        public int defenderPlayer = -1;
        public int monsterObject = -1;
        public int town = -1;
        /// <summary>The cell the attacker attacked from, and the one attacked.</summary>
        public int attackerCell = -1;
        public int targetCell = -1;
        /// <summary>What the monster guarded (a treasure to take, a mine to flag), for the attacker once it wins, or -1.</summary>
        public int prize = -1;
        /// <summary>The cell the armies line up around: a map cell on the map, (7, 5) of the field on a battlefield.</summary>
        public int center;
        /// <summary>True when the attacker deploys on the left (west) of the battlefield (always on a battlefield of its own).</summary>
        public bool attackerLeft = true;
        /// <summary>Where the battle is fought.</summary>
        public BattleStyle style;
        /// <summary>The grid of a battlefield of its own (fifteen by eleven); empty (0 by 0) on the map.</summary>
        public HexGrid field = new HexGrid();
        /// <summary>The map cell the armies met at (the town, the monster, the hero attacked): for the camera and the land around.</summary>
        public int mapCell = -1;
        /// <summary>The <see cref="TerrainType"/> of the place, as a number: the ground of a battlefield and its obstacles.</summary>
        public int terrain;
        /// <summary>The seed the obstacles of a battlefield were laid out from; the view may scatter its decoration from it too.</summary>
        public uint fieldSeed;
        /// <summary>Every cell of the battle (all of a battlefield's own; the map cells of the block on the map).</summary>
        public List<int> cells = new List<int>();
        /// <summary>Cells nobody may stand on or walk through (flyers fly over them).</summary>
        public List<int> blocked = new List<int>();
        /// <summary>The cells of the obstacles of a battlefield, siege walls included, each also in <see cref="blocked"/>.</summary>
        public List<int> obstacleCells = new List<int>();
        /// <summary>What stands on each of <see cref="obstacleCells"/> (a <see cref="BattleObstacle"/>, as a number), in the same order.</summary>
        public List<int> obstacleKinds = new List<int>();
        /// <summary>The wall of a town under siege on a battlefield (also in <see cref="blocked"/> and <see cref="obstacleCells"/>).</summary>
        public List<int> walls = new List<int>();
        /// <summary>The gate in the wall, or -1: the defenders go in and out through it, the besiegers cannot pass it.</summary>
        public int gate = -1;
        public List<BattleStack> stacks = new List<BattleStack>();
        public int round;
        /// <summary>Stacks still to act this round, in order.</summary>
        public List<int> order = new List<int>();
        /// <summary>Stacks that waited, acting after the others (slowest first).</summary>
        public List<int> waiting = new List<int>();
        public int current = -1;
        public bool attackerCast;
        public bool defenderCast;
        public BattleResult result;
        public int nextStack;
        /// <summary>Health of the creatures each side lost, for experience and necromancy.</summary>
        public int attackerLostHealth;
        public int defenderLostHealth;
        /// <summary>Creatures each side lost, by creature, for the results.</summary>
        public List<ArmySlot> attackerLosses = new List<ArmySlot>();
        public List<ArmySlot> defenderLosses = new List<ArmySlot>();
        /// <summary>Actions taken, so the game can tell a battle that stalls.</summary>
        public int actions;
        /// <summary>The battle ended because nobody could get at anybody any more: the attacker broke off and stayed where he stood.</summary>
        public bool stalled;

        public bool IsField => style == BattleStyle.Battlefield;

        public BattleStack Stack(int id)
        {
            foreach (BattleStack stack in stacks)
            {
                if (stack.id == id)
                {
                    return stack;
                }
            }
            return null;
        }

        public BattleStack Current => current >= 0 ? Stack(current) : null;

        public BattleStack StackAt(int cell)
        {
            foreach (BattleStack stack in stacks)
            {
                if (stack.alive && stack.cell == cell)
                {
                    return stack;
                }
            }
            return null;
        }

        public bool Contains(int cell)
        {
            // A battlefield of its own has every cell of its grid.
            return IsField ? field.Valid(cell) : cells.Contains(cell);
        }

        public bool IsBlocked(int cell)
        {
            return blocked.Contains(cell);
        }

        /// <summary>
        /// Whether a troop of <paramref name="side"/> may walk through <paramref name="cell"/>, troops aside: a cell of the
        /// field that is not blocked, and not the gate for the besiegers.
        /// </summary>
        public bool Passable(int cell, int side)
        {
            return Contains(cell) && !IsBlocked(cell) && !(side == 0 && cell == gate);
        }

        /// <summary>What stands on <paramref name="cell"/> of a battlefield, or <see cref="BattleObstacle.None"/>.</summary>
        public BattleObstacle ObstacleAt(int cell)
        {
            int index = obstacleCells.IndexOf(cell);
            return index >= 0 ? (BattleObstacle)obstacleKinds[index] : BattleObstacle.None;
        }

        public int HeroOf(int side)
        {
            return side == 0 ? attackerHero : defenderHero;
        }

        public int PlayerOf(int side)
        {
            return side == 0 ? attackerPlayer : defenderPlayer;
        }

        public bool HasCast(int side)
        {
            return side == 0 ? attackerCast : defenderCast;
        }

        public int AliveCount(int side, bool countTowers = false)
        {
            int count = 0;
            foreach (BattleStack stack in stacks)
            {
                if (stack.alive && stack.side == side && (countTowers || !stack.IsTower))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// A copy that shares nothing with this battle: every list, every stack and every effect is copied. The events
        /// carry one (<see cref="GameEvent.battle"/>), so the view can show a battle as it was while the rules go on.
        /// </summary>
        public BattleState Clone()
        {
            var copy = (BattleState)MemberwiseClone();
            copy.field = field != null ? new HexGrid(field.columns, field.rows) : new HexGrid();
            copy.cells = new List<int>(cells);
            copy.blocked = new List<int>(blocked);
            copy.obstacleCells = new List<int>(obstacleCells);
            copy.obstacleKinds = new List<int>(obstacleKinds);
            copy.walls = new List<int>(walls);
            copy.order = new List<int>(order);
            copy.waiting = new List<int>(waiting);
            copy.stacks = new List<BattleStack>(stacks.Count);
            foreach (BattleStack stack in stacks)
            {
                copy.stacks.Add(stack.Clone());
            }
            copy.attackerLosses = new List<ArmySlot>(attackerLosses.Count);
            foreach (ArmySlot loss in attackerLosses)
            {
                copy.attackerLosses.Add(loss.Clone());
            }
            copy.defenderLosses = new List<ArmySlot>(defenderLosses.Count);
            foreach (ArmySlot loss in defenderLosses)
            {
                copy.defenderLosses.Add(loss.Clone());
            }
            return copy;
        }
    }

    [Serializable]
    public sealed class BattleStack
    {
        public int id;
        public int side;
        public int creature;
        public int count;
        /// <summary>Health left of the top creature of the stack.</summary>
        public int health;
        public int startCount;
        public int cell;
        public bool alive = true;
        public int retaliations;
        public bool acted;
        public bool waited;
        public bool defending;
        public bool moraleUsed;
        public int shots;
        /// <summary>The army slot the stack came from, or -1 (summoned, towers, garrison beyond seven).</summary>
        public int slot = -1;
        /// <summary>0: the side's hero or monster army; 1: the town garrison fighting next to a visiting hero; 2: an arrow tower.</summary>
        public int source;
        public int facing;
        public List<EffectState> effects = new List<EffectState>();

        public CreatureDef Def => Creatures.Get(creature);

        public bool IsTower => creature == (int)CreatureId.ArrowTower;

        public bool HasEffect(SpellId spell)
        {
            foreach (EffectState effect in effects)
            {
                if (effect.spell == (int)spell)
                {
                    return true;
                }
            }
            return false;
        }

        public int EffectAmount(SpellId spell)
        {
            foreach (EffectState effect in effects)
            {
                if (effect.spell == (int)spell)
                {
                    return effect.amount;
                }
            }
            return 0;
        }

        public BattleStack Clone()
        {
            var copy = (BattleStack)MemberwiseClone();
            copy.effects = new List<EffectState>(effects.Count);
            foreach (EffectState effect in effects)
            {
                copy.effects.Add(effect.Clone());
            }
            return copy;
        }
    }

    [Serializable]
    public sealed class EffectState
    {
        public int spell;
        public int rounds;
        public int amount;

        public EffectState Clone()
        {
            return new EffectState { spell = spell, rounds = rounds, amount = amount };
        }
    }
}
