using System;
using System.Collections.Generic;

namespace Portfolio.Heroes
{
    /// <summary>
    /// Plays one side of a battle: the computer players, the neutral monsters, and a person's troops on auto combat. It
    /// looks one action ahead: the spell of the hero that does most, the shot that kills most, or the blow that kills
    /// most for the least retaliation; failing that it closes in on the most valuable enemy.
    /// </summary>
    public static class BattleAI
    {
        public static GameCommand Next(HeroesGame game)
        {
            BattleState battle = game.Battle;
            BattleStack stack = battle.Current;
            if (!battle.active || stack == null)
            {
                return null;
            }
            int side = stack.side;
            int player = battle.PlayerOf(side);
            GameCommand spell = BestSpell(game, side, player);
            if (spell != null)
            {
                return spell;
            }
            if (ShouldRetreat(game, side))
            {
                return GameCommand.BattleRetreat(player);
            }
            if (game.CanShoot(stack))
            {
                BattleStack target = BestShot(game, stack);
                if (target != null)
                {
                    return GameCommand.BattleShoot(player, stack.id, target.id);
                }
            }
            Dictionary<int, int> reach = game.BattleReach(stack);
            var bestAttack = FindAttack(game, stack, reach, out int from, out long score);
            if (bestAttack != null && score > long.MinValue / 2)
            {
                return GameCommand.BattleAttack(player, stack.id, bestAttack.id, from);
            }
            if (stack.Def.Has(Ability.Immobile))
            {
                return GameCommand.BattleDefend(player, stack.id);
            }
            // Out of reach: shooters without an open shot stay put; the others close in.
            if (stack.Def.IsRanged && stack.shots > 0)
            {
                return GameCommand.BattleDefend(player, stack.id);
            }
            if (!stack.waited && !stack.moraleUsed && battle.round <= 1 && EnemyFaster(game, stack))
            {
                return GameCommand.BattleWait(player, stack.id);
            }
            int step = Approach(game, stack, reach);
            if (step >= 0 && step != stack.cell)
            {
                return GameCommand.BattleMove(player, stack.id, step);
            }
            return GameCommand.BattleDefend(player, stack.id);
        }

        private static bool EnemyFaster(HeroesGame game, BattleStack stack)
        {
            foreach (BattleStack enemy in game.Battle.stacks)
            {
                if (enemy.alive && enemy.side != stack.side && !enemy.IsTower && game.Speed(enemy) > game.Speed(stack) + 1)
                {
                    return true;
                }
            }
            return false;
        }

        private static long StackValue(BattleStack stack)
        {
            return (long)stack.Def.Value * stack.count;
        }

        public static long SideStrength(HeroesGame game, int side)
        {
            long total = 0;
            foreach (BattleStack stack in game.Battle.stacks)
            {
                if (stack.alive && stack.side == side && !stack.IsTower)
                {
                    total += StackValue(stack);
                }
            }
            return total;
        }

        private static bool ShouldRetreat(HeroesGame game, int side)
        {
            BattleState battle = game.Battle;
            if (battle.HeroOf(side) < 0 || battle.round < 2 || (side == 1 && battle.town >= 0))
            {
                return false;
            }
            long mine = SideStrength(game, side);
            long theirs = SideStrength(game, 1 - side);
            return theirs > 0 && mine * 12 < theirs;
        }

        /// <summary>The value of the creatures a hit of <paramref name="damage"/> kills.</summary>
        private static long KillValue(HeroesGame game, BattleStack target, int damage)
        {
            int each = target.Def.Health;
            int total = game.TotalHealth(target);
            int dealt = Math.Min(damage, total);
            // Whole creatures, plus a share for the wounds.
            long kills = dealt >= total ? target.count : (dealt - target.health >= 0 ? 1 + (dealt - target.health) / Math.Max(1, each) : 0);
            return kills * target.Def.Value + (long)dealt * target.Def.Value / Math.Max(1, each) / 4;
        }

        private static BattleStack BestShot(HeroesGame game, BattleStack shooter)
        {
            BattleStack best = null;
            long bestValue = long.MinValue;
            foreach (BattleStack enemy in game.Battle.stacks)
            {
                if (!enemy.alive || enemy.side == shooter.side)
                {
                    continue;
                }
                int damage = game.Damage(shooter, enemy, true, 0, false, out _);
                long value = KillValue(game, enemy, damage);
                // Enemy shooters first: they hurt most from afar.
                if (enemy.Def.IsRanged && enemy.shots > 0)
                {
                    value = value * 3 / 2;
                }
                if (value > bestValue || (value == bestValue && best != null && enemy.id < best.id))
                {
                    bestValue = value;
                    best = enemy;
                }
            }
            return best;
        }

        private static BattleStack FindAttack(HeroesGame game, BattleStack stack, Dictionary<int, int> reach, out int from, out long bestScore)
        {
            from = -1;
            bestScore = long.MinValue;
            BattleStack best = null;
            foreach (BattleStack enemy in game.Battle.stacks)
            {
                if (!enemy.alive || enemy.side == stack.side)
                {
                    continue;
                }
                List<int> cells = game.AttackCells(stack, enemy, reach);
                foreach (int cell in cells)
                {
                    int steps = reach[cell];
                    int damage = game.Damage(stack, enemy, false, steps, false, out _);
                    long score = KillValue(game, enemy, damage);
                    if (stack.Def.Has(Ability.DoubleAttack))
                    {
                        score = score * 17 / 10;
                    }
                    bool survives = damage < game.TotalHealth(enemy);
                    if (survives && !stack.Def.Has(Ability.NoRetaliation) && !enemy.IsTower && (enemy.retaliations == 0 || enemy.Def.Has(Ability.UnlimitedRetaliation)))
                    {
                        // What the wounded enemy strikes back with, roughly.
                        int left = Math.Max(1, enemy.count - (damage / Math.Max(1, enemy.Def.Health)));
                        int back = (enemy.Def.MinDamage + enemy.Def.MaxDamage) / 2 * left;
                        score -= KillValue(game, stack, back) * 2 / 3;
                    }
                    // A shooter hitting in melee is a waste of its bow, but better than nothing.
                    if (enemy.Def.IsRanged)
                    {
                        score = score * 5 / 4;
                    }
                    // Prefer fewer steps (keeps the formation), then the lower cell.
                    score = score * 100 - steps;
                    if (score > bestScore || (score == bestScore && best != null && (enemy.id < best.id || (enemy.id == best.id && cell < from))))
                    {
                        bestScore = score;
                        best = enemy;
                        from = cell;
                    }
                }
            }
            return best;
        }

        private static int Approach(HeroesGame game, BattleStack stack, Dictionary<int, int> reach)
        {
            BattleState battle = game.Battle;
            // Walking distance to every enemy over the field (troops aside, which move), so a troop goes around
            // woods, water and walls (through the breaches, and the gate only for the defenders) instead of getting
            // stuck against them.
            var field = new Dictionary<int, int>();
            var queue = new Queue<int>();
            foreach (BattleStack enemy in battle.stacks)
            {
                if (enemy.alive && enemy.side != stack.side)
                {
                    field[enemy.cell] = 0;
                    queue.Enqueue(enemy.cell);
                }
            }
            var around = new List<int>(6);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                int d = field[cell];
                game.BattleGrid.Neighbors(cell, around);
                foreach (int next in around)
                {
                    if (!field.ContainsKey(next) && battle.Passable(next, stack.side))
                    {
                        field[next] = d + 1;
                        queue.Enqueue(next);
                    }
                }
            }
            int best = stack.cell;
            int bestDistance = field.TryGetValue(stack.cell, out int here) ? here : int.MaxValue;
            var keys = new List<int>(reach.Keys);
            keys.Sort();
            foreach (int cell in keys)
            {
                if (field.TryGetValue(cell, out int d) && d < bestDistance)
                {
                    bestDistance = d;
                    best = cell;
                }
            }
            if (best == stack.cell && bestDistance == int.MaxValue)
            {
                // No way on foot at all: at least get closer as the crow flies.
                int straight = int.MaxValue;
                foreach (int cell in keys)
                {
                    foreach (BattleStack enemy in battle.stacks)
                    {
                        if (enemy.alive && enemy.side != stack.side)
                        {
                            int d = game.BattleGrid.Distance(cell, enemy.cell);
                            if (d < straight)
                            {
                                straight = d;
                                best = cell;
                            }
                        }
                    }
                }
            }
            return best;
        }

        private static GameCommand BestSpell(HeroesGame game, int side, int player)
        {
            BattleState battle = game.Battle;
            HeroState hero = game.State.Hero(battle.HeroOf(side));
            if (hero == null || battle.HasCast(side) || hero.spells.Count == 0)
            {
                return null;
            }
            long bestValue = 0;
            GameCommand best = null;
            var spells = new List<int>(hero.spells);
            spells.Sort();
            foreach (int id in spells)
            {
                var spell = (SpellId)id;
                SpellDef def = Spells.Get(spell);
                if (def == null || game.CannotCast(side, spell) != null)
                {
                    continue;
                }
                foreach (int cell in CandidateCells(game, side, def))
                {
                    if (!game.ValidSpellTarget(side, spell, cell))
                    {
                        continue;
                    }
                    long value = SpellValue(game, hero, side, def, cell);
                    // Mana is precious: the spell has to be worth its price.
                    value -= def.Cost * 25;
                    if (value > bestValue)
                    {
                        bestValue = value;
                        best = GameCommand.BattleCast(player, spell, cell);
                    }
                }
            }
            return best;
        }

        private static IEnumerable<int> CandidateCells(HeroesGame game, int side, SpellDef def)
        {
            var cells = new List<int>();
            foreach (BattleStack stack in game.Battle.stacks)
            {
                if (def.Target == SpellTarget.DeadFriend)
                {
                    if (stack.side == side && stack.count < stack.startCount && !cells.Contains(stack.cell))
                    {
                        cells.Add(stack.cell);
                    }
                    continue;
                }
                if (stack.alive && !cells.Contains(stack.cell))
                {
                    cells.Add(stack.cell);
                }
            }
            if (def.Target == SpellTarget.AllFriends && cells.Count > 0)
            {
                return new[] { cells[0] };
            }
            cells.Sort();
            return cells;
        }

        private static long SpellValue(HeroesGame game, HeroState hero, int side, SpellDef def, int cell)
        {
            BattleState battle = game.Battle;
            BattleStack target = battle.StackAt(cell);
            if (def.IsDamage)
            {
                long value = 0;
                if (def.Target == SpellTarget.Area)
                {
                    foreach (int area in game.BattleGrid.Disk(cell, def.Radius))
                    {
                        BattleStack hit = battle.StackAt(area);
                        if (hit == null)
                        {
                            continue;
                        }
                        long kill = KillValue(game, hit, game.SpellDamage(hero, def, hit));
                        value += hit.side == side ? -kill * 3 / 2 : kill;
                    }
                    return value;
                }
                return target != null ? KillValue(game, target, game.SpellDamage(hero, def, target)) : 0;
            }
            switch (def.Id)
            {
                case SpellId.Cure:
                    return target != null ? Math.Min(game.SpellPower(hero) * 5 + 10, (target.Def.Health - target.health) + 10) * target.Def.Value / Math.Max(1, target.Def.Health) : 0;
                case SpellId.Resurrection:
                case SpellId.AnimateDead:
                {
                    BattleStack stack = null;
                    foreach (BattleStack s in battle.stacks)
                    {
                        if (s.side == side && s.cell == cell && s.count < s.startCount)
                        {
                            stack = s;
                        }
                    }
                    if (stack == null)
                    {
                        return 0;
                    }
                    int restore = def.Base + def.PerPower * game.SpellPower(hero);
                    int missing = (stack.startCount - stack.count) * stack.Def.Health;
                    return (long)Math.Min(restore, missing) * stack.Def.Value / Math.Max(1, stack.Def.Health);
                }
                case SpellId.Prayer:
                    return SideStrength(game, side) / 6;
                default:
                    if (target == null || target.HasEffect(def.Id))
                    {
                        return 0;
                    }
                    long worth = StackValue(target) / (def.IsPositive ? 6 : 5);
                    // A slow or weakened enemy shooter matters less; a hasted shooter matters little.
                    if (def.Id == SpellId.Haste && target.Def.IsRanged)
                    {
                        worth /= 3;
                    }
                    if (def.Id == SpellId.Bloodlust && target.Def.IsRanged)
                    {
                        worth /= 4;
                    }
                    return worth;
            }
        }
    }
}
