using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    public sealed partial class HeroesGame
    {
        private readonly Queue<int> bfs = new Queue<int>();

        private bool ApplyBattle(GameCommand command)
        {
            BattleState battle = Battle;
            if (command.kind == CommandKind.BattleRetreat)
            {
                return Retreat(command.player);
            }
            if (command.kind == CommandKind.BattleCast)
            {
                return Cast(command.player, (SpellId)command.a, command.b);
            }
            BattleStack stack = battle.Stack(command.a);
            if (stack == null || !stack.alive || stack.id != battle.current || battle.PlayerOf(stack.side) != command.player)
            {
                return false;
            }
            switch (command.kind)
            {
                case CommandKind.BattleMove: return MoveStack(stack, command.b);
                case CommandKind.BattleAttack: return Attack(stack, command.b, command.c);
                case CommandKind.BattleShoot: return Shoot(stack, command.b);
                case CommandKind.BattleWait: return Wait(stack);
                case CommandKind.BattleDefend: return Defend(stack);
                default: return false;
            }
        }

        // ------------------------------------------------------------------ reach

        private bool FreeInBattle(int cell, BattleStack except)
        {
            BattleState battle = Battle;
            if (!battle.Contains(cell) || battle.IsBlocked(cell))
            {
                return false;
            }
            BattleStack there = battle.StackAt(cell);
            return there == null || there == except;
        }

        /// <summary>
        /// The cells the stack can move to this turn, with the steps it takes (walkers go around obstacles and troops,
        /// flyers go straight). The stack's own cell has 0.
        /// </summary>
        public Dictionary<int, int> BattleReach(BattleStack stack)
        {
            var reach = new Dictionary<int, int>();
            BattleState battle = Battle;
            reach[stack.cell] = 0;
            if (stack.Def.Has(Ability.Immobile))
            {
                return reach;
            }
            int speed = Speed(stack);
            if (stack.Def.IsFlying)
            {
                foreach (int cell in battle.cells)
                {
                    if (cell != stack.cell && FreeInBattle(cell, stack))
                    {
                        int distance = Grid.Distance(stack.cell, cell);
                        if (distance <= speed)
                        {
                            reach[cell] = distance;
                        }
                    }
                }
                return reach;
            }
            bfs.Clear();
            bfs.Enqueue(stack.cell);
            while (bfs.Count > 0)
            {
                int cell = bfs.Dequeue();
                int steps = reach[cell];
                if (steps >= speed)
                {
                    continue;
                }
                Grid.Neighbors(cell, scratch);
                foreach (int next in scratch)
                {
                    if (!reach.ContainsKey(next) && FreeInBattle(next, stack))
                    {
                        reach[next] = steps + 1;
                        bfs.Enqueue(next);
                    }
                }
            }
            return reach;
        }

        /// <summary>The walking path from the stack to <paramref name="target"/> (a reachable cell), the target last.</summary>
        public List<int> BattlePath(BattleStack stack, int target)
        {
            var path = new List<int>();
            if (stack.Def.IsFlying || target == stack.cell)
            {
                path.Add(target);
                return path;
            }
            var from = new Dictionary<int, int> { [stack.cell] = -1 };
            bfs.Clear();
            bfs.Enqueue(stack.cell);
            while (bfs.Count > 0)
            {
                int cell = bfs.Dequeue();
                if (cell == target)
                {
                    break;
                }
                Grid.Neighbors(cell, scratch);
                foreach (int next in scratch)
                {
                    if (!from.ContainsKey(next) && FreeInBattle(next, stack))
                    {
                        from[next] = cell;
                        bfs.Enqueue(next);
                    }
                }
            }
            if (!from.ContainsKey(target))
            {
                return path;
            }
            for (int cell = target; cell != stack.cell; cell = from[cell])
            {
                path.Add(cell);
            }
            path.Reverse();
            return path;
        }

        public bool EnemyAdjacent(BattleStack stack)
        {
            Grid.Neighbors(stack.cell, scratch);
            foreach (int cell in scratch)
            {
                BattleStack other = Battle.StackAt(cell);
                if (other != null && other.side != stack.side && !other.IsTower)
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanShoot(BattleStack stack)
        {
            return stack.Def.IsRanged && stack.shots > 0 && !EnemyAdjacent(stack);
        }

        /// <summary>Cells the stack can reach this turn from which it can strike <paramref name="target"/>.</summary>
        public List<int> AttackCells(BattleStack stack, BattleStack target, Dictionary<int, int> reach = null)
        {
            var cells = new List<int>();
            if (target == null || !target.alive || target.side == stack.side)
            {
                return cells;
            }
            reach = reach ?? BattleReach(stack);
            var around = Grid.Neighbors(target.cell);
            foreach (int cell in around)
            {
                if (reach.ContainsKey(cell))
                {
                    cells.Add(cell);
                }
            }
            cells.Sort();
            return cells;
        }

        // ------------------------------------------------------------------ actions

        private void Walk(BattleStack stack, int cell)
        {
            if (cell == stack.cell)
            {
                return;
            }
            List<int> path = BattlePath(stack, cell);
            int from = stack.cell;
            stack.facing = (int)(Grid.X2(cell) >= Grid.X2(from) ? HexSide.East : HexSide.West);
            stack.cell = cell;
            Emit(EventKind.StackMoved, Battle.PlayerOf(stack.side), stack.id, from, cell, stack.Def.IsFlying ? 1 : 0, cells: path);
        }

        private bool MoveStack(BattleStack stack, int cell)
        {
            if (cell == stack.cell || !BattleReach(stack).ContainsKey(cell))
            {
                return false;
            }
            Walk(stack, cell);
            FinishAction(stack, true);
            return true;
        }

        private bool Attack(BattleStack stack, int targetId, int from)
        {
            BattleStack target = Battle.Stack(targetId);
            if (target == null || !target.alive || target.side == stack.side)
            {
                return false;
            }
            Dictionary<int, int> reach = BattleReach(stack);
            if (!reach.TryGetValue(from, out int steps) || Grid.Distance(from, target.cell) != 1)
            {
                return false;
            }
            Walk(stack, from);
            stack.facing = (int)(Grid.X2(target.cell) >= Grid.X2(stack.cell) ? HexSide.East : HexSide.West);
            Melee(stack, target, steps);
            FinishAction(stack, true);
            return true;
        }

        private void Melee(BattleStack stack, BattleStack target, int steps)
        {
            Strike(stack, target, false, steps, false);
            if (target.alive && stack.alive && CanRetaliate(target, stack))
            {
                target.retaliations++;
                Strike(target, stack, false, 0, true);
            }
            if (stack.Def.Has(Ability.DoubleAttack) && stack.alive && target.alive)
            {
                Strike(stack, target, false, 0, false);
            }
        }

        private bool CanRetaliate(BattleStack defender, BattleStack attacker)
        {
            if (attacker.Def.Has(Ability.NoRetaliation) || defender.IsTower)
            {
                return false;
            }
            if (defender.Def.Has(Ability.UnlimitedRetaliation))
            {
                return true;
            }
            int allowed = defender.Def.Has(Ability.TwoRetaliations) ? 2 : 1;
            return defender.retaliations < allowed;
        }

        private bool Shoot(BattleStack stack, int targetId)
        {
            BattleStack target = Battle.Stack(targetId);
            if (target == null || !target.alive || target.side == stack.side || !CanShoot(stack))
            {
                return false;
            }
            stack.facing = (int)(Grid.X2(target.cell) >= Grid.X2(stack.cell) ? HexSide.East : HexSide.West);
            Strike(stack, target, true, 0, false);
            stack.shots--;
            if (stack.Def.Has(Ability.DoubleAttack) && stack.alive && target.alive && stack.shots > 0)
            {
                Strike(stack, target, true, 0, false);
                stack.shots--;
            }
            FinishAction(stack, true);
            return true;
        }

        private void TowerShot(BattleStack tower)
        {
            // The towers shoot at the most valuable besiegers by themselves.
            BattleStack best = null;
            long bestValue = -1;
            foreach (BattleStack stack in Battle.stacks)
            {
                if (!stack.alive || stack.side == tower.side)
                {
                    continue;
                }
                long value = (long)stack.Def.Value * stack.count;
                if (value > bestValue)
                {
                    bestValue = value;
                    best = stack;
                }
            }
            if (best != null)
            {
                Strike(tower, best, true, 0, false);
            }
            tower.acted = true;
            Battle.actions++;
            CheckBattleEnd();
        }

        private bool Wait(BattleStack stack)
        {
            if (stack.waited || stack.moraleUsed)
            {
                return false;
            }
            stack.waited = true;
            Battle.waiting.Add(stack.id);
            Emit(EventKind.StackWaited, Battle.PlayerOf(stack.side), stack.id);
            Battle.actions++;
            AdvanceTurn();
            return true;
        }

        private bool Defend(BattleStack stack)
        {
            stack.defending = true;
            Emit(EventKind.StackDefended, Battle.PlayerOf(stack.side), stack.id);
            FinishAction(stack, false);
            return true;
        }

        private bool Retreat(int player)
        {
            BattleState battle = Battle;
            BattleStack current = battle.Current;
            if (current == null || battle.PlayerOf(current.side) != player)
            {
                return false;
            }
            int side = current.side;
            if (battle.HeroOf(side) < 0 || (side == 1 && battle.town >= 0))
            {
                return false;
            }
            EndBattle(side == 0 ? BattleResult.AttackerFled : BattleResult.DefenderFled);
            return true;
        }

        // ------------------------------------------------------------------ damage

        /// <summary>
        /// The damage <paramref name="attacker"/> deals <paramref name="target"/>: the dice of every creature (ten at most,
        /// scaled up for a bigger stack), attack against defense (5% per point up to +300%, 2.5% down to -70%), the
        /// hero's skills, spells on either, and the penalties of shooting far or fighting up close with a bow.
        /// <paramref name="roll"/> false gives the average, for the plans of the computer.
        /// </summary>
        public int Damage(BattleStack attacker, BattleStack target, bool ranged, int steps, bool roll, out bool lucky)
        {
            lucky = false;
            CreatureDef def = attacker.Def;
            HeroState hero = State.Hero(Battle.HeroOf(attacker.side));
            HeroState targetHero = State.Hero(Battle.HeroOf(target.side));
            int dice = Math.Min(attacker.count, 10);
            long sum = 0;
            bool blessed = attacker.HasEffect(SpellId.Bless);
            bool cursed = attacker.HasEffect(SpellId.Curse);
            for (int i = 0; i < dice; i++)
            {
                if (blessed)
                {
                    sum += def.MaxDamage;
                }
                else if (cursed)
                {
                    sum += def.MinDamage;
                }
                else if (roll)
                {
                    sum += Random.Range(def.MinDamage, def.MaxDamage + 1);
                }
                else
                {
                    sum += (def.MinDamage + def.MaxDamage) / 2;
                }
            }
            if (attacker.count > 10)
            {
                sum = sum * attacker.count / 10;
            }
            int attack = def.Attack + (hero != null ? Stat(hero, PrimaryStat.Attack) : 0);
            if (!ranged)
            {
                attack += attacker.EffectAmount(SpellId.Bloodlust);
            }
            attack -= attacker.EffectAmount(SpellId.Weakness);
            attack += attacker.EffectAmount(SpellId.Prayer);
            int defense = target.Def.Defense + (targetHero != null ? Stat(targetHero, PrimaryStat.Defense) : 0);
            defense += target.EffectAmount(SpellId.StoneSkin) + target.EffectAmount(SpellId.Prayer);
            if (target.defending)
            {
                defense += Math.Max(1, defense / 5);
            }
            long permille = attack >= defense ? 1000 + 50 * Math.Min(attack - defense, 60) : 1000 - 25 * Math.Min(defense - attack, 28);
            long damage = sum * permille / 1000;
            if (hero != null)
            {
                if (ranged)
                {
                    int archery = hero.SkillLevel(SkillId.Archery);
                    damage = damage * (100 + (archery == 3 ? 50 : archery == 2 ? 25 : archery * 10)) / 100;
                }
                else
                {
                    damage = damage * (100 + 10 * hero.SkillLevel(SkillId.Offense)) / 100;
                }
            }
            if (targetHero != null)
            {
                damage = damage * (100 - 5 * targetHero.SkillLevel(SkillId.Armorer)) / 100;
            }
            if (ranged && Grid.Distance(attacker.cell, target.cell) > 10 && !attacker.IsTower)
            {
                damage /= 2;
            }
            if (!ranged && def.IsRanged && !def.Has(Ability.NoMeleePenalty))
            {
                damage /= 2;
            }
            if (!ranged && def.Has(Ability.Charge) && steps > 0)
            {
                damage = damage * (100 + 5 * steps) / 100;
            }
            if (roll && hero != null)
            {
                int luck = Luck(hero);
                if (luck > 0 && Random.Range(0, 100) < luck * 4)
                {
                    lucky = true;
                    damage *= 2;
                }
            }
            return (int)Math.Max(1, Math.Min(damage, int.MaxValue));
        }

        /// <summary>The total health left in a stack.</summary>
        public int TotalHealth(BattleStack stack)
        {
            int each = stack.Def.Health + ArmyHealthBonus(State.Hero(Battle.HeroOf(stack.side)));
            return stack.count <= 0 ? 0 : (stack.count - 1) * each + stack.health;
        }

        /// <summary>Takes <paramref name="damage"/> from a stack; returns the creatures killed.</summary>
        private int Hurt(BattleStack stack, int damage)
        {
            int each = stack.Def.Health + ArmyHealthBonus(State.Hero(Battle.HeroOf(stack.side)));
            int total = TotalHealth(stack);
            int left = Math.Max(0, total - damage);
            int count = left == 0 ? 0 : (left + each - 1) / each;
            int killed = stack.count - count;
            int lost = total - left;
            stack.count = count;
            stack.health = count == 0 ? 0 : left - (count - 1) * each;
            if (stack.IsTower)
            {
                lost = 0;
            }
            if (stack.side == 0)
            {
                Battle.attackerLostHealth += lost;
            }
            else
            {
                Battle.defenderLostHealth += lost;
            }
            if (killed > 0 && !stack.IsTower)
            {
                AddLoss(stack.side == 0 ? Battle.attackerLosses : Battle.defenderLosses, stack.creature, killed);
            }
            if (count == 0)
            {
                stack.alive = false;
                Emit(EventKind.StackDied, Battle.PlayerOf(stack.side), stack.id, stack.cell);
            }
            return killed;
        }

        private static void AddLoss(List<ArmySlot> losses, int creature, int count)
        {
            foreach (ArmySlot loss in losses)
            {
                if (loss.creature == creature)
                {
                    loss.count += count;
                    return;
                }
            }
            losses.Add(new ArmySlot { creature = creature, count = count });
        }

        private void Strike(BattleStack attacker, BattleStack target, bool ranged, int steps, bool retaliation)
        {
            int damage = Damage(attacker, target, ranged, steps, true, out bool lucky);
            if (lucky)
            {
                Emit(EventKind.LuckyStrike, Battle.PlayerOf(attacker.side), attacker.id);
            }
            int killed = Hurt(target, damage);
            Emit(ranged ? EventKind.StackShot : EventKind.StackAttacked, Battle.PlayerOf(attacker.side), attacker.id, target.id, damage, killed, retaliation ? 1 : 0);
            if (attacker.Def.Has(Ability.LifeDrain) && attacker.alive)
            {
                Heal(attacker, damage, true);
            }
            if (ranged && attacker.Def.Has(Ability.AreaShot))
            {
                Grid.Neighbors(target.cell, scratch);
                var around = new List<int>(scratch);
                foreach (int cell in around)
                {
                    BattleStack other = Battle.StackAt(cell);
                    if (other != null && other != attacker && !other.Def.IsUndead)
                    {
                        int splash = Damage(attacker, other, true, 0, true, out _);
                        int dead = Hurt(other, splash);
                        Emit(EventKind.StackDamaged, Battle.PlayerOf(other.side), other.id, splash, dead, attacker.id);
                    }
                }
            }
            if (!ranged && attacker.Def.Has(Ability.Breath))
            {
                // The flame goes on to whoever stands behind the target.
                int dx = Grid.X2(target.cell) - Grid.X2(attacker.cell);
                int dy = Grid.Row(target.cell) - Grid.Row(attacker.cell);
                int behind = -1;
                Grid.Neighbors(target.cell, scratch);
                foreach (int cell in scratch)
                {
                    if (Grid.X2(cell) - Grid.X2(target.cell) == dx && Grid.Row(cell) - Grid.Row(target.cell) == dy)
                    {
                        behind = cell;
                    }
                }
                BattleStack second = behind >= 0 ? Battle.StackAt(behind) : null;
                if (second != null && second != attacker)
                {
                    int flame = Damage(attacker, second, false, 0, true, out _);
                    int dead = Hurt(second, flame);
                    Emit(EventKind.StackDamaged, Battle.PlayerOf(second.side), second.id, flame, dead, attacker.id);
                }
            }
        }

        /// <summary>Heals a stack; with <paramref name="raise"/> the dead come back up to the stack's first size.</summary>
        private int Heal(BattleStack stack, int amount, bool raise)
        {
            int each = stack.Def.Health + ArmyHealthBonus(State.Hero(Battle.HeroOf(stack.side)));
            int total = TotalHealth(stack);
            int max = raise ? stack.startCount * each : stack.count * each;
            int healed = Math.Min(amount, Math.Max(0, max - total));
            if (healed <= 0)
            {
                return 0;
            }
            int now = total + healed;
            int count = (now + each - 1) / each;
            int raised = count - stack.count;
            stack.count = count;
            stack.health = now - (count - 1) * each;
            if (!stack.alive && count > 0)
            {
                stack.alive = true;
            }
            Emit(EventKind.StackHealed, Battle.PlayerOf(stack.side), stack.id, healed, raised);
            return raised;
        }

        // ------------------------------------------------------------------ spells

        /// <summary>Why the hero of <paramref name="side"/> cannot cast <paramref name="spell"/> now, or null.</summary>
        public string CannotCast(int side, SpellId spell)
        {
            BattleState battle = Battle;
            HeroState hero = State.Hero(battle.HeroOf(side));
            SpellDef def = Spells.Get(spell);
            if (hero == null || def == null)
            {
                return "No hero to cast.";
            }
            if (!hero.Knows(spell))
            {
                return "The hero does not know it.";
            }
            if (battle.HasCast(side))
            {
                return "One spell a round.";
            }
            if (hero.mana < def.Cost)
            {
                return "Not enough mana.";
            }
            BattleStack current = battle.Current;
            if (current == null || current.side != side)
            {
                return "Wait for your troops' turn.";
            }
            return null;
        }

        /// <summary>Whether <paramref name="spell"/> can be cast on <paramref name="cell"/> by the hero of <paramref name="side"/>.</summary>
        public bool ValidSpellTarget(int side, SpellId spell, int cell)
        {
            SpellDef def = Spells.Get(spell);
            BattleStack target = Battle.StackAt(cell);
            switch (def.Target)
            {
                case SpellTarget.Enemy:
                    return target != null && target.side != side && !(target.Def.Has(Ability.MagicResist) && def.IsEffect);
                case SpellTarget.Friend:
                    return target != null && target.side == side && !target.IsTower;
                case SpellTarget.Area:
                    return Battle.Contains(cell);
                case SpellTarget.AllFriends:
                    return true;
                case SpellTarget.DeadFriend:
                    BattleStack stack = DeadOrHurtStack(side, cell);
                    if (stack == null)
                    {
                        return false;
                    }
                    bool undead = stack.Def.IsUndead;
                    return spell == SpellId.AnimateDead ? undead : !undead;
                default:
                    return false;
            }
        }

        private BattleStack DeadOrHurtStack(int side, int cell)
        {
            foreach (BattleStack stack in Battle.stacks)
            {
                if (stack.side == side && stack.cell == cell && !stack.IsTower && (stack.count < stack.startCount || !stack.alive))
                {
                    if (!stack.alive && Battle.StackAt(cell) != null)
                    {
                        continue;
                    }
                    return stack;
                }
            }
            return null;
        }

        public int SpellPower(HeroState hero)
        {
            return Stat(hero, PrimaryStat.Power);
        }

        public int SpellDamage(HeroState hero, SpellDef def, BattleStack target)
        {
            long damage = def.Base + (long)def.PerPower * SpellPower(hero);
            damage = damage * (100 + 10 * hero.SkillLevel(SkillId.Sorcery)) / 100;
            if (target != null && target.Def.Has(Ability.MagicResist))
            {
                damage /= 2;
            }
            return (int)Math.Max(1, damage);
        }

        private bool Cast(int player, SpellId spell, int cell)
        {
            BattleState battle = Battle;
            BattleStack current = battle.Current;
            if (current == null || battle.PlayerOf(current.side) != player)
            {
                return false;
            }
            int side = current.side;
            if (CannotCast(side, spell) != null || !ValidSpellTarget(side, spell, cell))
            {
                return false;
            }
            HeroState hero = State.Hero(battle.HeroOf(side));
            SpellDef def = Spells.Get(spell);
            hero.mana -= def.Cost;
            if (side == 0)
            {
                battle.attackerCast = true;
            }
            else
            {
                battle.defenderCast = true;
            }
            Emit(EventKind.SpellCast, player, side, (int)spell, cell, hero.id);
            int rounds = Math.Max(1, SpellPower(hero));
            switch (def.Target)
            {
                case SpellTarget.Enemy:
                case SpellTarget.Friend:
                {
                    BattleStack target = battle.StackAt(cell);
                    if (def.IsDamage)
                    {
                        int damage = SpellDamage(hero, def, target);
                        int killed = Hurt(target, damage);
                        Emit(EventKind.StackDamaged, battle.PlayerOf(target.side), target.id, damage, killed, -1);
                    }
                    else if (spell == SpellId.Cure)
                    {
                        target.effects.RemoveAll(e => !Spells.Get((SpellId)e.spell).IsPositive);
                        Heal(target, def.Base + def.PerPower * SpellPower(hero), false);
                    }
                    else
                    {
                        AddEffect(target, spell, rounds, def);
                    }
                    break;
                }
                case SpellTarget.Area:
                {
                    var hit = new List<BattleStack>();
                    foreach (int area in Grid.Disk(cell, def.Radius))
                    {
                        BattleStack target = battle.StackAt(area);
                        if (target != null)
                        {
                            hit.Add(target);
                        }
                    }
                    foreach (BattleStack target in hit)
                    {
                        int damage = SpellDamage(hero, def, target);
                        int killed = Hurt(target, damage);
                        Emit(EventKind.StackDamaged, battle.PlayerOf(target.side), target.id, damage, killed, -1);
                    }
                    break;
                }
                case SpellTarget.AllFriends:
                    foreach (BattleStack target in battle.stacks)
                    {
                        if (target.alive && target.side == side && !target.IsTower)
                        {
                            AddEffect(target, spell, rounds, def);
                        }
                    }
                    break;
                case SpellTarget.DeadFriend:
                {
                    BattleStack target = DeadOrHurtStack(side, cell);
                    int raised = Heal(target, def.Base + def.PerPower * SpellPower(hero), true);
                    Emit(EventKind.CreaturesRaised, player, target.id, target.creature, raised);
                    break;
                }
            }
            if (!CheckBattleEnd() && battle.Current != null && !battle.Current.alive)
            {
                // The caster's own troop died in its fireball: on to the next.
                battle.Current.acted = true;
                AdvanceTurn();
            }
            return true;
        }

        private void AddEffect(BattleStack target, SpellId spell, int rounds, SpellDef def)
        {
            // Opposites cancel: haste and slow, bless and curse.
            SpellId opposite = spell == SpellId.Haste ? SpellId.Slow : spell == SpellId.Slow ? SpellId.Haste :
                spell == SpellId.Bless ? SpellId.Curse : spell == SpellId.Curse ? SpellId.Bless : SpellId.None;
            target.effects.RemoveAll(e => e.spell == (int)spell || e.spell == (int)opposite);
            target.effects.Add(new EffectState { spell = (int)spell, rounds = rounds, amount = def.Amount });
            Emit(EventKind.EffectAdded, Battle.PlayerOf(target.side), target.id, (int)spell, rounds);
        }
    }
}
