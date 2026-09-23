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
    /// A battle on the adventure map. The battlefield is a block of the map's own cells around the place the armies met
    /// (fifteen columns by eleven rows at most), and whatever stands there (forests, rocks, water, buildings) is in the
    /// way of the troops as it is on the map. Side 0 attacks, side 1 defends.
    /// </summary>
    [Serializable]
    public sealed class BattleState
    {
        public const int HalfWidth = 7;
        public const int HalfHeight = 5;

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
        public int center;
        /// <summary>True when the attacker deploys on the left (west) of the battlefield.</summary>
        public bool attackerLeft = true;
        public List<int> cells = new List<int>();
        public List<int> blocked = new List<int>();
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
            return cells.Contains(cell);
        }

        public bool IsBlocked(int cell)
        {
            return blocked.Contains(cell);
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
        /// <summary>0: the side's hero or monster army; 1: the town garrison fighting next to a visiting hero.</summary>
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
    }

    [Serializable]
    public sealed class EffectState
    {
        public int spell;
        public int rounds;
        public int amount;
    }
}
