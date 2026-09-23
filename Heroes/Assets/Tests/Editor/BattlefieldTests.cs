using System;
using System.Collections.Generic;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// Battles on a field of their own, fifteen hexes by eleven as in the original game, next to the battles fought on
    /// the map: the field, its obstacles and walls, that it always hangs together and comes out the same, that a battle
    /// there ends and hands the armies back to the map, and the fixes of the battle rules that came with it.
    /// </summary>
    public class BattlefieldTests
    {
        private static MapSpec Spec(uint seed, BattleStyle style, int players = 2)
        {
            var spec = new MapSpec { seed = seed, columns = 30, rows = 34, treasure = 2, monsters = 2 };
            spec.rules.battleStyle = style;
            for (int i = 0; i < players; i++)
            {
                spec.players.Add(new PlayerSpec
                {
                    faction = (Faction)(i % 3),
                    human = i == 0,
                    team = i,
                    aiLevel = 1,
                    name = i == 0 ? "You" : $"Rival {i}"
                });
            }
            return spec;
        }

        private static HeroesGame Start(uint seed, BattleStyle style, int players = 2)
        {
            GameState state = MapGenerator.Generate(Spec(seed, style, players));
            var game = new HeroesGame(state, new SeededRandom(seed ^ 0x5bd1e995u));
            game.Begin();
            game.TakeEvents();
            return game;
        }

        private static MapObject FirstMonster(HeroesGame game, int skip = 0)
        {
            foreach (MapObject what in game.State.objects)
            {
                if (what.kind == ObjectKind.Monster && !what.removed && skip-- <= 0)
                {
                    return what;
                }
            }
            return null;
        }

        /// <summary>A cell a hero may stand on and walk from: open, empty, no town gate, no monster next to it.</summary>
        private static bool FreeCell(HeroesGame game, int cell)
        {
            return game.Map.Open(cell) && game.Map.occupant[cell] < 0 && game.State.HeroAt(cell) == null &&
                   game.TownAtGate(cell) == null && game.GuardOf(cell) == null;
        }

        /// <summary>Puts the first hero next to a wandering army with a fair army and walks him in.</summary>
        private static MapObject FightAMonster(HeroesGame game, int skip = 0)
        {
            HeroState hero = game.State.heroes[0];
            MapObject monster = FirstMonster(game, skip);
            Assert.That(monster, Is.Not.Null, "the map holds no wandering army");
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Swordsman, 40);
            hero.army.Add((int)CreatureId.Archer, 30);
            hero.movement = 5000;
            hero.cell = game.Grid.Neighbors(monster.cell)[0];
            Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, monster.cell)), Is.True);
            Assert.That(game.InBattle, Is.True, "walking into the army started no battle");
            return monster;
        }

        /// <summary>Lets the computer fight both sides to the end; returns the commands it gave.</summary>
        private static List<GameCommand> FightItOut(HeroesGame game)
        {
            var commands = new List<GameCommand>();
            for (int step = 0; step < 4000 && game.InBattle; step++)
            {
                GameCommand move = BattleAI.Next(game);
                Assert.That(move, Is.Not.Null, "the computer had no move in a battle");
                Assert.That(game.Apply(move), Is.True, $"the rules refused {move}");
                commands.Add(move);
            }
            Assert.That(game.InBattle, Is.False, "the battle never ended");
            return commands;
        }

        /// <summary>Lets the computer play until a stack of the attacker has its turn and can strike an enemy.</summary>
        private static BattleStack UntilTheAttackerCanStrike(HeroesGame game, out BattleStack target, out int from)
        {
            for (int step = 0; step < 400 && game.InBattle; step++)
            {
                BattleStack stack = game.Battle.Current;
                if (stack != null && stack.side == 0)
                {
                    Dictionary<int, int> reach = game.BattleReach(stack);
                    foreach (BattleStack enemy in game.Battle.stacks)
                    {
                        List<int> cells = game.AttackCells(stack, enemy, reach);
                        if (enemy.alive && !enemy.IsTower && cells.Count > 0)
                        {
                            target = enemy;
                            from = cells[0];
                            return stack;
                        }
                    }
                }
                Assert.That(game.Apply(BattleAI.Next(game)), Is.True);
            }
            Assert.Fail("the attacker never got to strike");
            target = null;
            from = -1;
            return null;
        }

        /// <summary>Lets the computer play until it is a turn of the attacker's.</summary>
        private static BattleStack UntilTheAttackersTurn(HeroesGame game)
        {
            for (int step = 0; step < 400 && game.InBattle; step++)
            {
                BattleStack stack = game.Battle.Current;
                if (stack != null && stack.side == 0)
                {
                    return stack;
                }
                Assert.That(game.Apply(BattleAI.Next(game)), Is.True);
            }
            Assert.Fail("the attacker never had a turn");
            return null;
        }

        private static GameEvent Find(List<GameEvent> events, EventKind kind)
        {
            return events.Find(e => e.kind == kind);
        }

        /// <summary>The rival's hero, stood on a free cell with a free cell next to it for the first hero, who then attacks him.</summary>
        private static HeroState Duel(HeroesGame game)
        {
            HeroState hero = game.State.heroes[0];
            HeroState rival = game.State.Hero(game.State.Player(1).heroes[0]);
            HexGrid grid = game.Grid;
            for (int cell = 0; cell < grid.Count; cell++)
            {
                if (!FreeCell(game, cell))
                {
                    continue;
                }
                foreach (int next in grid.Neighbors(cell))
                {
                    if (FreeCell(game, next))
                    {
                        rival.cell = cell;
                        hero.cell = next;
                        hero.movement = 5000;
                        Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, rival.cell)), Is.True);
                        Assert.That(game.InBattle, Is.True, "walking into the rival started no battle");
                        return rival;
                    }
                }
            }
            Assert.Fail("no room for a duel");
            return null;
        }

        /// <summary>The rival's town, walled and towered as asked, with the first hero next to its gate, who then attacks it.</summary>
        private static TownState Besiege(HeroesGame game, int towers)
        {
            HeroState hero = game.State.heroes[0];
            TownState town = game.State.Town(game.State.Player(1).towns[0]);
            town.built.Remove((int)BuildingId.Citadel);
            town.built.Remove((int)BuildingId.Castle);
            if (!town.Has(BuildingId.Fort))
            {
                town.built.Add((int)BuildingId.Fort);
            }
            if (towers >= 1)
            {
                town.built.Add((int)BuildingId.Citadel);
            }
            if (towers >= 3)
            {
                town.built.Add((int)BuildingId.Castle);
            }
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Crusader, 60);
            hero.army.Add((int)CreatureId.Archer, 60);
            hero.army.Add((int)CreatureId.GoldDragon, 6);
            hero.movement = 5000;
            foreach (int next in game.Grid.Neighbors(town.cell))
            {
                if (FreeCell(game, next))
                {
                    hero.cell = next;
                    Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, town.cell)), Is.True);
                    Assert.That(game.Battle.town, Is.EqualTo(town.id), "walking into the town started no siege");
                    return town;
                }
            }
            Assert.Fail("no room before the gate");
            return null;
        }

        /// <summary>Whether every open cell can be walked to from the attacker's first column, found apart from the rules.</summary>
        private static bool HangsTogether(BattleState battle)
        {
            HexGrid grid = battle.field;
            var seen = new bool[grid.Count];
            var queue = new Queue<int>();
            for (int row = 0; row < grid.rows; row++)
            {
                int cell = grid.Index(0, row);
                if (!battle.blocked.Contains(cell))
                {
                    seen[cell] = true;
                    queue.Enqueue(cell);
                }
            }
            while (queue.Count > 0)
            {
                foreach (int next in grid.Neighbors(queue.Dequeue()))
                {
                    if (!seen[next] && !battle.blocked.Contains(next) && next != battle.gate)
                    {
                        seen[next] = true;
                        queue.Enqueue(next);
                    }
                }
            }
            for (int cell = 0; cell < grid.Count; cell++)
            {
                if (!seen[cell] && !battle.blocked.Contains(cell) && cell != battle.gate)
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------ the field

        [Test]
        public void ABattlefieldHasItsOwnGrid()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            MapObject monster = FightAMonster(game);
            BattleState battle = game.Battle;
            HexGrid field = battle.field;
            Assert.That(battle.IsField, Is.True);
            Assert.That(field.columns, Is.EqualTo(BattleState.FieldColumns));
            Assert.That(field.rows, Is.EqualTo(BattleState.FieldRows));
            Assert.That(battle.cells.Count, Is.EqualTo(165));
            Assert.That(game.BattleGrid, Is.SameAs(field));
            Assert.That(battle.mapCell, Is.EqualTo(monster.cell), "the fight is not where the monster stood");
            Assert.That(battle.center, Is.EqualTo(field.Index(7, 5)));
            Assert.That(battle.attackerLeft, Is.True);
            Assert.That(Land.Walkable((TerrainType)battle.terrain), Is.True);
            Assert.That(battle.obstacleCells.Count, Is.EqualTo(battle.obstacleKinds.Count));
            Assert.That(battle.obstacleCells.Count, Is.GreaterThan(0), "the field is bare");
            Assert.That(battle.blocked.Count, Is.EqualTo(battle.obstacleCells.Count), "something but the obstacles is in the way");
            foreach (int cell in battle.obstacleCells)
            {
                int column = field.Column(cell);
                Assert.That(column, Is.InRange(Battlefields.FirstObstacleColumn, Battlefields.LastObstacleColumn), "an obstacle where the armies line up");
                Assert.That(battle.blocked, Contains.Item(cell));
            }
            foreach (BattleStack stack in battle.stacks)
            {
                Assert.That(battle.IsBlocked(stack.cell), Is.False, "a stack stands in an obstacle");
                int column = field.Column(stack.cell);
                if (stack.side == 0)
                {
                    Assert.That(column, Is.EqualTo(0), "the attacker is not on the left edge");
                }
                else
                {
                    Assert.That(column, Is.EqualTo(BattleState.FieldColumns - 1), "the defender is not on the right edge");
                }
            }
            Assert.That(HangsTogether(battle), Is.True);

            GameEvent started = Find(game.TakeEvents(), EventKind.BattleStarted);
            Assert.That(started, Is.Not.Null);
            Assert.That(started.a, Is.EqualTo(monster.cell), "BattleStarted does not name the map cell");
            Assert.That(started.b, Is.EqualTo(-1));
            Assert.That(started.c, Is.EqualTo(monster.id));
            Assert.That(started.d, Is.EqualTo(-1), "no town, but the event names one");
            Assert.That(started.e, Is.EqualTo((int)BattleStyle.Battlefield));
            Assert.That(started.battle, Is.Not.Null.And.Not.SameAs(battle), "no copy of the battle came with it");
            Assert.That(started.battle.obstacleCells, Is.EqualTo(battle.obstacleCells));
            Assert.That(started.battle.stacks.Count, Is.EqualTo(battle.stacks.Count));
            Assert.That(started.battle.stacks[0], Is.Not.SameAs(battle.stacks[0]), "the copy shares its stacks");
            Assert.That(started.battle.field, Is.Not.SameAs(battle.field));
        }

        [Test]
        public void TheBattlefieldAlwaysHangsTogether()
        {
            int withObstacles = 0;
            int fields = 0;
            foreach (TerrainType terrain in (TerrainType[])Enum.GetValues(typeof(TerrainType)))
            {
                for (uint seed = 1; seed <= 150; seed++)
                {
                    for (int towers = -1; towers <= 3; towers += 2)
                    {
                        var battle = new BattleState
                        {
                            style = BattleStyle.Battlefield,
                            field = new HexGrid(BattleState.FieldColumns, BattleState.FieldRows),
                            terrain = (int)terrain,
                            fieldSeed = SeededRandom.Mix(seed, (uint)terrain, (uint)towers)
                        };
                        bool walled = towers >= 0;
                        HeroesGame.LayOutField(battle, walled, Math.Max(0, towers));
                        fields++;
                        Assert.That(HangsTogether(battle), Is.True, $"{terrain} field {seed} has a pocket");
                        Assert.That(HeroesGame.FieldHangsTogether(battle), Is.True);
                        int last = walled ? Battlefields.WallColumn - 2 : Battlefields.LastObstacleColumn;
                        for (int i = 0; i < battle.obstacleCells.Count; i++)
                        {
                            if (battle.obstacleKinds[i] == (int)BattleObstacle.Wall)
                            {
                                continue;
                            }
                            Assert.That(battle.field.Column(battle.obstacleCells[i]), Is.InRange(Battlefields.FirstObstacleColumn, last));
                        }
                        if (battle.obstacleCells.Count > battle.walls.Count)
                        {
                            withObstacles++;
                        }
                    }
                }
            }
            Assert.That(withObstacles, Is.GreaterThan(fields * 9 / 10), "most fields came out bare");
        }

        [Test]
        public void ACopyOfTheBattleSharesNothing()
        {
            var battle = new BattleState
            {
                style = BattleStyle.Battlefield,
                field = new HexGrid(BattleState.FieldColumns, BattleState.FieldRows),
                terrain = (int)TerrainType.Grass,
                fieldSeed = 7
            };
            HeroesGame.LayOutField(battle, true, 3);
            battle.stacks.Add(new BattleStack { id = 0, cell = 3, effects = new List<EffectState> { new EffectState { spell = 1, rounds = 2 } } });
            battle.attackerLosses.Add(new ArmySlot { creature = 1, count = 2 });
            battle.order.Add(0);
            battle.waiting.Add(0);
            BattleState copy = battle.Clone();
            Assert.That(copy.gate, Is.EqualTo(battle.gate));
            Assert.That(copy.obstacleKinds, Is.EqualTo(battle.obstacleKinds));
            copy.cells.Add(-1);
            copy.blocked.Add(-1);
            copy.obstacleCells.Add(-1);
            copy.obstacleKinds.Add(-1);
            copy.walls.Add(-1);
            copy.order.Add(-1);
            copy.waiting.Add(-1);
            copy.field.columns = 1;
            copy.stacks[0].cell = 9;
            copy.stacks[0].effects[0].rounds = 9;
            copy.attackerLosses[0].count = 9;
            foreach (List<int> list in new[] { battle.cells, battle.blocked, battle.obstacleCells, battle.obstacleKinds, battle.walls, battle.order, battle.waiting })
            {
                Assert.That(list.Contains(-1), Is.False, "the copy shares a list");
            }
            Assert.That(battle.field.columns, Is.EqualTo(BattleState.FieldColumns));
            Assert.That(battle.stacks[0].cell, Is.EqualTo(3));
            Assert.That(battle.stacks[0].effects[0].rounds, Is.EqualTo(2));
            Assert.That(battle.attackerLosses[0].count, Is.EqualTo(2));
        }

        [Test]
        public void TheSameFightGetsTheSameField()
        {
            HeroesGame first = Start(4242, BattleStyle.Battlefield);
            HeroesGame second = Start(4242, BattleStyle.Battlefield);
            FightAMonster(first);
            FightAMonster(second);
            Assert.That(second.Battle.fieldSeed, Is.EqualTo(first.Battle.fieldSeed));
            Assert.That(second.Battle.terrain, Is.EqualTo(first.Battle.terrain));
            Assert.That(second.Battle.obstacleCells, Is.EqualTo(first.Battle.obstacleCells));
            Assert.That(second.Battle.obstacleKinds, Is.EqualTo(first.Battle.obstacleKinds));
            Assert.That(LockstepGame.Checksum(second.State), Is.EqualTo(LockstepGame.Checksum(first.State)));
        }

        [Test]
        public void AnotherPlaceGetsAnotherField()
        {
            HeroesGame first = Start(4242, BattleStyle.Battlefield);
            HeroesGame second = Start(4242, BattleStyle.Battlefield);
            FightAMonster(first, 0);
            FightAMonster(second, 1);
            Assert.That(second.Battle.mapCell, Is.Not.EqualTo(first.Battle.mapCell));
            Assert.That(second.Battle.fieldSeed, Is.Not.EqualTo(first.Battle.fieldSeed));
            Assert.That(second.Battle.obstacleCells, Is.Not.EqualTo(first.Battle.obstacleCells));
        }

        // ------------------------------------------------------------------ battles on the field

        [Test]
        public void ABattlefieldBattleEndsWithOneSideLeft()
        {
            foreach (uint seed in new uint[] { 24680, 1357, 97531 })
            {
                HeroesGame game = Start(seed, BattleStyle.Battlefield);
                FightAMonster(game);
                FightItOut(game);
                BattleState battle = game.Battle;
                Assert.That(battle.AliveCount(0) == 0 || battle.AliveCount(1) == 0, Is.True,
                    $"both sides came out of battle {seed} alive ({battle.result})");
            }
        }

        /// <summary>Something a monster guards (a treasure, else a building), with the first hero on a cell next to it; null when there is none.</summary>
        private static MapObject GuardedPrize(HeroesGame game)
        {
            HeroState hero = game.State.heroes[0];
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (MapObject what in game.State.objects)
                {
                    if (what.removed || MapObjects.IsPickup(what.kind) != (pass == 0) || game.GuardianOf(what) == null || what.owner == hero.owner)
                    {
                        continue;
                    }
                    foreach (int next in game.Grid.Neighbors(what.cell))
                    {
                        if (game.Map.Open(next) && game.Map.occupant[next] < 0 && game.State.HeroAt(next) == null && game.TownAtGate(next) == null)
                        {
                            hero.cell = next;
                            return what;
                        }
                    }
                }
            }
            return null;
        }

        [Test]
        public void ABattlefieldBattleReturnsToTheMap()
        {
            HeroesGame game = null;
            MapObject prize = null;
            foreach (uint seed in new uint[] { 24680, 1357, 4242, 777, 97531, 20260923 })
            {
                game = Start(seed, BattleStyle.Battlefield);
                prize = GuardedPrize(game);
                if (prize != null)
                {
                    break;
                }
            }
            Assert.That(prize, Is.Not.Null, "nothing on the maps is guarded");
            // The hero is one step from what the monster guards, a point short of a level up.
            HeroState hero = game.State.heroes[0];
            MapObject guard = game.GuardianOf(prize);
            hero.army.Clear();
            hero.army.Add((int)CreatureId.GoldDragon, 12);
            hero.army.Add((int)CreatureId.Crusader, 40);
            hero.movement = 5000;
            hero.experience = HeroData.ExperienceFor(hero.level + 1) - 1;
            int level = hero.level;
            int cell = hero.cell;
            Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, prize.cell)), Is.True);
            Assert.That(game.InBattle, Is.True, "the guard did not attack");
            Assert.That(game.Battle.IsField, Is.True);
            Assert.That(game.Battle.prize, Is.EqualTo(prize.id));
            FightItOut(game);

            Assert.That(game.Battle.result, Is.EqualTo(BattleResult.AttackerWon));
            Assert.That(hero.alive, Is.True);
            Assert.That(game.Battle.attackerCell, Is.EqualTo(cell));
            Assert.That(hero.cell, Is.EqualTo(cell), "the hero is not back where he fought from");
            Assert.That(game.Battle.IsField, Is.True);
            Assert.That(game.Battle.mapCell, Is.EqualTo(guard.cell), "the battle does not remember where on the map it was");
            Assert.That(guard.removed, Is.True, "the beaten guard is still on the map");
            Assert.That(hero.level, Is.GreaterThan(level), "no experience came of the battle");
            Assert.That(game.State.pending.Find(p => p.kind == ChoiceKind.LevelUp), Is.Not.Null, "the level up asks nothing");
            int left = 0;
            foreach (BattleStack stack in game.Battle.stacks)
            {
                if (stack.side == 0 && stack.alive)
                {
                    left += stack.count;
                }
            }
            Assert.That(hero.army.TotalCreatures, Is.EqualTo(left), "the survivors did not go back to the army");
            if (MapObjects.IsPickup(prize.kind))
            {
                Assert.That(prize.removed, Is.True, "the treasure was left behind because of the level up");
            }
            else
            {
                Assert.That(prize.owner == hero.owner || prize.visitedBy.Contains(hero.id), Is.True, "the building was left alone because of the level up");
            }
        }

        [Test]
        public void ASiegeOnTheBattlefieldHasTowersAndBreaches()
        {
            HeroesGame game = Start(20260923, BattleStyle.Battlefield);
            TownState town = game.State.Town(game.State.Player(1).towns[0]);
            town.garrison.Clear();
            town.garrison.Add((int)CreatureId.Wolf, 30);
            town.garrison.Add((int)CreatureId.Hunter, 20);
            Besiege(game, 3);
            BattleState battle = game.Battle;
            HexGrid field = battle.field;
            Assert.That(battle.active, Is.True);
            Assert.That(battle.gate, Is.EqualTo(field.Index(Battlefields.WallColumn, Battlefields.GateRow)));
            Assert.That(battle.IsBlocked(battle.gate), Is.False);
            Assert.That(battle.Passable(battle.gate, 1), Is.True, "the defenders cannot use their gate");
            Assert.That(battle.Passable(battle.gate, 0), Is.False, "the besiegers walk through the gate");
            foreach (int row in Battlefields.BreachRows)
            {
                Assert.That(battle.IsBlocked(field.Index(Battlefields.WallColumn, row)), Is.False, "a breach is walled up");
            }
            var towers = new List<int>();
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.IsTower)
                {
                    towers.Add(stack.cell);
                }
                else if (stack.side == 1)
                {
                    Assert.That(field.Column(stack.cell), Is.GreaterThan(Battlefields.WallColumn), "a defender stands outside the walls");
                }
                else
                {
                    Assert.That(field.Column(stack.cell), Is.LessThan(Battlefields.WallColumn));
                }
            }
            Assert.That(towers, Is.EquivalentTo(new[]
            {
                field.Index(Battlefields.WallColumn, 4), field.Index(Battlefields.WallColumn, 0), field.Index(Battlefields.WallColumn, 10)
            }));
            // The rest of the wall's column is wall, and blocked.
            for (int row = 0; row < field.rows; row++)
            {
                int cell = field.Index(Battlefields.WallColumn, row);
                bool open = cell == battle.gate || towers.Contains(cell) || Array.IndexOf(Battlefields.BreachRows, row) >= 0;
                Assert.That(battle.walls.Contains(cell), Is.EqualTo(!open));
                Assert.That(battle.IsBlocked(cell), Is.EqualTo(!open));
                if (!open)
                {
                    Assert.That(battle.ObstacleAt(cell), Is.EqualTo(BattleObstacle.Wall));
                }
            }
            Assert.That(HangsTogether(battle), Is.True);

            // The besiegers never step into the gate, on foot or on wings; a defender next to it does.
            BattleStack besieger = battle.stacks.Find(s => s.side == 0 && !s.Def.IsFlying);
            besieger.cell = field.Index(Battlefields.WallColumn - 1, Battlefields.GateRow);
            Assert.That(game.BattleReach(besieger).ContainsKey(battle.gate), Is.False);
            BattleStack flyer = battle.stacks.Find(s => s.side == 0 && s.Def.IsFlying);
            Assert.That(game.BattleReach(flyer).ContainsKey(battle.gate), Is.False, "a besieger lands in the gate");
            BattleStack defender = battle.stacks.Find(s => s.side == 1 && !s.IsTower && !s.Def.IsFlying);
            int inside = field.Index(Battlefields.WallColumn + 1, Battlefields.GateRow);
            BattleStack there = battle.StackAt(inside);
            if (there != null && there != defender)
            {
                there.cell = field.Index(12, 3);
            }
            defender.cell = inside;
            Assert.That(game.BattleReach(defender).ContainsKey(battle.gate), Is.True, "the defenders cannot step into their gate");
        }

        [Test]
        public void ASiegeOnTheBattlefieldEnds()
        {
            foreach (int towers in new[] { 0, 1, 3 })
            {
                HeroesGame game = Start(20260923, BattleStyle.Battlefield);
                TownState town = game.State.Town(game.State.Player(1).towns[0]);
                town.garrison.Clear();
                town.garrison.Add((int)CreatureId.Brawler, 25);
                town.garrison.Add((int)CreatureId.Hunter, 15);
                Besiege(game, towers);
                Assert.That(game.Battle.gate, Is.GreaterThanOrEqualTo(0), "a town with a fort has no wall");
                FightItOut(game);
                BattleState battle = game.Battle;
                Assert.That(battle.stalled, Is.False, $"the siege with {towers} towers stalled");
                Assert.That(battle.result, Is.EqualTo(BattleResult.AttackerWon), $"the siege with {towers} towers was lost");
                Assert.That(town.owner, Is.EqualTo(0), "the town did not change hands");
            }
        }

        [Test]
        public void AFallenTowerLeavesTheWallClosed()
        {
            HeroesGame game = Start(20260923, BattleStyle.Battlefield);
            TownState town = game.State.Town(game.State.Player(1).towns[0]);
            town.garrison.Clear();
            town.garrison.Add((int)CreatureId.Brawler, 25);
            Besiege(game, 3);
            BattleState battle = game.Battle;
            BattleStack tower = battle.stacks.Find(s => s.IsTower);
            Assert.That(tower, Is.Not.Null);
            Assert.That(battle.IsBlocked(tower.cell), Is.False, "a tower stands on a wall cell");
            for (int step = 0; step < 400 && game.InBattle && tower.alive; step++)
            {
                BattleStack stack = battle.Current;
                GameCommand move = stack != null && stack.side == 0 && game.CanShoot(stack)
                    ? GameCommand.BattleShoot(battle.PlayerOf(0), stack.id, tower.id)
                    : BattleAI.Next(game);
                Assert.That(game.Apply(move), Is.True, $"the rules refused {move}");
            }
            Assert.That(tower.alive, Is.False, "the besiegers never brought the tower down");
            // The stones of the tower close the wall where it stood: the way in is still the breaches.
            Assert.That(battle.IsBlocked(tower.cell), Is.True);
            Assert.That(battle.walls, Contains.Item(tower.cell));
            Assert.That(battle.ObstacleAt(tower.cell), Is.EqualTo(BattleObstacle.Wall));
            Assert.That(battle.Passable(tower.cell, 0), Is.False);
            Assert.That(battle.Passable(tower.cell, 1), Is.False);
            Assert.That(HangsTogether(battle), Is.True);
            if (game.InBattle)
            {
                FightItOut(game);
            }
            Assert.That(battle.stalled, Is.False);
        }

        [Test]
        public void FlyersCrossObstaclesWalkersGoAround()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            FightAMonster(game);
            BattleState battle = game.Battle;
            HexGrid field = battle.field;
            battle.blocked.Clear();
            battle.obstacleCells.Clear();
            battle.obstacleKinds.Clear();
            // A line of rocks down column 4, open only at the top row.
            for (int row = 0; row < field.rows - 1; row++)
            {
                int cell = field.Index(4, row);
                battle.blocked.Add(cell);
                battle.obstacleCells.Add(cell);
                battle.obstacleKinds.Add((int)BattleObstacle.Rock);
            }
            BattleStack walker = battle.stacks.Find(s => s.side == 0);
            BattleStack flyer = battle.stacks.Find(s => s.side == 0 && s != walker);
            flyer.creature = (int)CreatureId.GoldDragon;
            for (int i = 0; i < battle.stacks.Count; i++)
            {
                battle.stacks[i].cell = field.Index(13 - i % 2, i % field.rows);
            }
            walker.cell = field.Index(3, 1);
            flyer.cell = field.Index(3, 3);
            int beyond = field.Index(5, 1);

            List<int> path = game.BattlePath(walker, beyond);
            Assert.That(path.Count, Is.GreaterThan(field.Distance(walker.cell, beyond)), "the walker went through the rocks");
            Assert.That(path[path.Count - 1], Is.EqualTo(beyond));
            int previous = walker.cell;
            foreach (int cell in path)
            {
                Assert.That(battle.IsBlocked(cell), Is.False, "the path goes through a rock");
                Assert.That(field.Distance(previous, cell), Is.EqualTo(1), "the path jumps");
                previous = cell;
            }
            Assert.That(game.BattleReach(walker).ContainsKey(beyond), Is.False, "the walker gets past the rocks in one turn");

            Dictionary<int, int> flight = game.BattleReach(flyer);
            int over = field.Index(5, 3);
            Assert.That(flight.ContainsKey(over), Is.True, "the flyer cannot cross the rocks");
            Assert.That(flight[over], Is.EqualTo(2));
            foreach (int cell in flight.Keys)
            {
                Assert.That(battle.IsBlocked(cell), Is.False, "the flyer lands on a rock");
            }
            Assert.That(game.BattlePath(flyer, over), Is.EqualTo(new List<int> { over }));
            battle.current = flyer.id;
            Assert.That(game.Apply(GameCommand.BattleMove(0, flyer.id, over)), Is.True);
            Assert.That(flyer.cell, Is.EqualTo(over));
        }

        [Test]
        public void TheChecksumSeesTheField()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            FightAMonster(game);
            BattleState battle = game.Battle;
            uint before = LockstepGame.Checksum(game.State);
            int free = -1;
            for (int cell = 0; cell < battle.field.Count && free < 0; cell++)
            {
                if (!battle.IsBlocked(cell) && battle.StackAt(cell) == null)
                {
                    free = cell;
                }
            }
            battle.blocked.Add(free);
            Assert.That(LockstepGame.Checksum(game.State), Is.Not.EqualTo(before), "a blocked cell more went unseen");
            battle.blocked.Remove(free);
            Assert.That(LockstepGame.Checksum(game.State), Is.EqualTo(before));
            battle.obstacleKinds.Add(1);
            Assert.That(LockstepGame.Checksum(game.State), Is.Not.EqualTo(before), "the kinds of obstacles went unseen");
            battle.obstacleKinds.RemoveAt(battle.obstacleKinds.Count - 1);
            battle.stacks[0].effects.Add(new EffectState { spell = (int)SpellId.Haste, rounds = 2 });
            Assert.That(LockstepGame.Checksum(game.State), Is.Not.EqualTo(before), "a spell on a stack went unseen");
            battle.stacks[0].effects.Clear();
            battle.style = BattleStyle.OnTheMap;
            Assert.That(LockstepGame.Checksum(game.State), Is.Not.EqualTo(before), "the style of the battle went unseen");
            battle.style = BattleStyle.Battlefield;
            game.State.rules.battleStyle = BattleStyle.OnTheMap;
            Assert.That(LockstepGame.Checksum(game.State), Is.Not.EqualTo(before), "the style of the game went unseen");
        }

        [TestCase(BattleStyle.OnTheMap)]
        [TestCase(BattleStyle.Battlefield)]
        public void TheSameCommandsGiveTheSameGame(BattleStyle style)
        {
            var commands = new List<(GameCommand command, uint stamp)>();
            var first = new LockstepGame(MapGenerator.Generate(Spec(777, style)));
            first.Game.Begin();
            SetUpAFight(first.Game);
            var brain = new AdventureAI();
            uint stamp = 12345;
            HeroState hero = first.Game.State.heroes[0];
            GameCommand opening = GameCommand.Move(hero.owner, hero.id, FirstMonster(first.Game).cell);
            bool fought = false;
            for (int i = 0; i < 400 && !first.Game.IsOver; i++)
            {
                int who = first.Game.WaitingPlayer;
                if (who < 0 && !first.Game.InBattle)
                {
                    break;
                }
                GameCommand command = i == 0 ? opening
                    : first.Game.State.pending.Count > 0 ? GameCommand.Choose(who, 0)
                    : first.Game.InBattle ? BattleAI.Next(first.Game)
                    : brain.Next(first.Game, who);
                command ??= GameCommand.EndTurn(who);
                stamp = stamp * 1664525u + 1013904223u;
                if (first.Apply(command, stamp))
                {
                    commands.Add((command, stamp));
                }
                if (first.Game.InBattle)
                {
                    fought = true;
                    Assert.That(first.Game.Battle.style, Is.EqualTo(style));
                }
            }
            Assert.That(fought, Is.True, "no battle was fought");
            Assert.That(commands.Count, Is.GreaterThan(20), "the game hardly moved");

            var second = new LockstepGame(MapGenerator.Generate(Spec(777, style)));
            second.Game.Begin();
            SetUpAFight(second.Game);
            foreach ((GameCommand command, uint each) in commands)
            {
                Assert.That(second.Apply(command, each), Is.True, $"the replay refused {command}");
            }
            Assert.That(second.Checksum(), Is.EqualTo(first.Checksum()), "the replay came out differently");
        }

        [Test]
        public void ComputerPlayersFinishASeasonOnTheBattlefield()
        {
            int allSieges = 0;
            // Map 2 is one of many sieges.
            foreach (uint seed in new uint[] { 20260923, 777, 2 })
            {
                HeroesGame game = Start(seed, BattleStyle.Battlefield, 3);
                foreach (PlayerState player in game.State.players)
                {
                    player.human = false;
                }
                var brain = new AdventureAI();
                int refused = 0;
                int battles = 0;
                int sieges = 0;
                int stalls = 0;
                for (int step = 0; step < 60000 && !game.IsOver && game.State.day <= 28; step++)
                {
                    int who = game.WaitingPlayer;
                    if (who < -1)
                    {
                        break;
                    }
                    GameCommand command = game.InBattle ? BattleAI.Next(game) : brain.Next(game, who);
                    if (command == null)
                    {
                        command = game.InBattle
                            ? GameCommand.BattleDefend(who, game.Battle.current)
                            : GameCommand.EndTurn(who);
                    }
                    bool accepted = game.Apply(command);
                    foreach (GameEvent what in game.TakeEvents())
                    {
                        if (what.kind == EventKind.BattleStarted)
                        {
                            battles++;
                            sieges += what.d >= 0 ? 1 : 0;
                            Assert.That(what.e, Is.EqualTo((int)BattleStyle.Battlefield));
                            Assert.That(HangsTogether(what.battle), Is.True, "a battlefield came out in pieces");
                        }
                        if (what.kind == EventKind.BattleEnded && what.battle.stalled)
                        {
                            stalls++;
                        }
                    }
                    if (accepted)
                    {
                        refused = 0;
                        continue;
                    }
                    Assert.That(game.InBattle, Is.False, $"the rules refused the computer's {command} in a battle");
                    brain.Refused(command);
                    refused++;
                    Assert.That(refused, Is.LessThan(20), $"the rules keep refusing {command.kind} on day {game.State.day}");
                }
                Assert.That(game.State.day, Is.GreaterThan(3), "the game barely started");
                Assert.That(battles, Is.GreaterThan(0), "a season without a battle");
                Assert.That(stalls, Is.EqualTo(0), $"{stalls} of {battles} battles ({sieges} sieges) stalled on map {seed}");
                allSieges += sieges;
            }
            Assert.That(allSieges, Is.GreaterThan(0), "no town was besieged");
        }

        private static void SetUpAFight(HeroesGame game)
        {
            HeroState hero = game.State.heroes[0];
            MapObject monster = FirstMonster(game);
            monster.amount = Math.Max(monster.amount, 40);
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Swordsman, 30);
            hero.army.Add((int)CreatureId.Archer, 25);
            hero.spells.Add((int)SpellId.MagicArrow);
            hero.mana = 30;
            hero.movement = 5000;
            hero.cell = game.Grid.Neighbors(monster.cell)[0];
        }

        [Test]
        public void CommandsSurviveTheLog()
        {
            foreach (CommandKind kind in (CommandKind[])Enum.GetValues(typeof(CommandKind)))
            {
                GameCommand command = GameCommand.Of(kind, 3, -1, 164, 2147483647, -2147483648, 7);
                GameCommand back = GameCommand.Parse(command.Seat, (uint)command.kind, command.Payload());
                if (kind == CommandKind.SeatToComputer)
                {
                    Assert.That(back, Is.Null, "a player sent the server's own command");
                    continue;
                }
                Assert.That(back, Is.Not.Null, $"{kind} did not come back");
                Assert.That(back.kind, Is.EqualTo(command.kind));
                Assert.That(new[] { back.player, back.a, back.b, back.c, back.d, back.e },
                    Is.EqualTo(new[] { command.player, command.a, command.b, command.c, command.d, command.e }), $"{kind} came back changed");
            }
            // The wandering armies fight through the world's seat, as player -1, with field cells like any other.
            GameCommand wild = GameCommand.BattleAttack(-1, 4, 2, 150);
            GameCommand fromTheWild = GameCommand.Parse(wild.Seat, (uint)wild.kind, wild.Payload());
            Assert.That(fromTheWild.player, Is.EqualTo(-1));
            Assert.That(new[] { fromTheWild.a, fromTheWild.b, fromTheWild.c }, Is.EqualTo(new[] { 4, 2, 150 }));
            Assert.That(GameCommand.Parse(0, 99, "0,0,0,0,0"), Is.Null, "a kind nobody knows was taken");
            Assert.That(GameCommand.Parse(0, (uint)CommandKind.BattleAttack, "0,1"), Is.Null);
        }

        [TestCase(BattleStyle.OnTheMap)]
        [TestCase(BattleStyle.Battlefield)]
        public void AStrikeNamesTheTargetStack(BattleStyle style)
        {
            HeroesGame game = Start(24680, style);
            FightAMonster(game);
            BattleStack stack = UntilTheAttackerCanStrike(game, out BattleStack target, out int from);
            game.TakeEvents();
            // As the battle bar sends it: the stack struck by its id, the cell to strike from.
            Assert.That(game.Apply(GameCommand.BattleAttack(0, stack.id, target.id, from)), Is.True, "the rules refused a blow at a stack");
            GameEvent blow = Find(game.TakeEvents(), EventKind.StackAttacked);
            Assert.That(blow, Is.Not.Null);
            Assert.That(blow.a, Is.EqualTo(stack.id));
            Assert.That(blow.b, Is.EqualTo(target.id), "somebody else was struck");
            Assert.That(stack.cell, Is.EqualTo(from));
        }

        [Test]
        public void AShotNamesTheTargetStack()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            FightAMonster(game);
            BattleStack archer = null;
            for (int step = 0; step < 400 && game.InBattle; step++)
            {
                BattleStack stack = game.Battle.Current;
                if (stack != null && stack.side == 0 && game.CanShoot(stack))
                {
                    archer = stack;
                    break;
                }
                Assert.That(game.Apply(BattleAI.Next(game)), Is.True);
            }
            Assert.That(archer, Is.Not.Null, "the archers never got to shoot");
            BattleStack target = game.Battle.stacks.Find(s => s.alive && s.side == 1);
            int shots = archer.shots;
            game.TakeEvents();
            Assert.That(game.Apply(GameCommand.BattleShoot(0, archer.id, target.id)), Is.True, "the rules refused a shot at a stack");
            GameEvent shot = Find(game.TakeEvents(), EventKind.StackShot);
            Assert.That(shot.b, Is.EqualTo(target.id));
            Assert.That(archer.shots, Is.LessThan(shots));
        }

        [Test]
        public void MapStyleIsUnchanged()
        {
            // Taken from the rules before the battlefield was added.
            Assert.That(MapBattleDigest(24680), Is.EqualTo(2040559973u));
            Assert.That(MapBattleDigest(1357), Is.EqualTo(3844629u));
        }

        /// <summary>
        /// Everything a battle on the map did before the battlefield came, folded into one number: the commands the
        /// computer gave, where every stack stood and how many it had after each, and what came back to the map.
        /// </summary>
        private static uint MapBattleDigest(uint seed)
        {
            GameState state = MapGenerator.Generate(Spec(seed, BattleStyle.OnTheMap));
            var game = new HeroesGame(state, new SeededRandom(seed ^ 0x5bd1e995u));
            game.Begin();
            HeroState hero = game.State.heroes[0];
            MapObject monster = FirstMonster(game);
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Swordsman, 30);
            hero.army.Add((int)CreatureId.Archer, 25);
            hero.army.Add((int)CreatureId.Priest, 6);
            hero.spells.Add((int)SpellId.MagicArrow);
            hero.spells.Add((int)SpellId.Bless);
            hero.mana = 40;
            hero.movement = 5000;
            hero.cell = game.Grid.Neighbors(monster.cell)[0];
            var sum = new StateChecksum();
            sum.Add(game.Apply(GameCommand.Move(hero.owner, hero.id, monster.cell)));
            BattleState battle = game.Battle;
            sum.Add(battle.cells).Add(battle.blocked).Add(battle.center).Add(battle.attackerLeft);
            for (int step = 0; step < 4000 && game.InBattle; step++)
            {
                GameCommand move = BattleAI.Next(game);
                sum.Add((int)move.kind).Add(move.player).Add(move.a).Add(move.b).Add(move.c);
                sum.Add(game.Apply(move));
                foreach (BattleStack stack in battle.stacks)
                {
                    sum.Add(stack.cell).Add(stack.count).Add(stack.health).Add(stack.alive).Add(stack.shots);
                }
            }
            sum.Add(game.InBattle).Add((int)battle.result).Add(battle.round).Add(battle.actions);
            sum.Add(hero.alive).Add(hero.experience).Add(hero.mana).Add(monster.amount).Add(monster.removed);
            foreach (ArmySlot slot in hero.army.slots)
            {
                sum.Add(slot.creature).Add(slot.count);
            }
            return sum.Value;
        }

        // ------------------------------------------------------------------ fixes of the battle rules

        [Test]
        public void AKillingBlowIsToldBeforeTheDeath()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            FightAMonster(game);
            BattleStack stack = UntilTheAttackerCanStrike(game, out BattleStack target, out int from);
            target.count = 1;
            target.health = 1;
            game.TakeEvents();
            Assert.That(game.Apply(GameCommand.BattleAttack(0, stack.id, target.id, from)), Is.True);
            List<GameEvent> events = game.TakeEvents();
            int blow = events.FindIndex(e => e.kind == EventKind.StackAttacked && e.b == target.id);
            int death = events.FindIndex(e => e.kind == EventKind.StackDied && e.a == target.id);
            Assert.That(blow, Is.GreaterThanOrEqualTo(0), "the killing blow was not told");
            Assert.That(death, Is.GreaterThan(blow), "the stack fell before the blow that killed it");
            Assert.That(events.FindAll(e => e.kind == EventKind.StackDied && e.a == target.id).Count, Is.EqualTo(1));
            Assert.That(events[blow].d, Is.EqualTo(1), "the blow did not count its kill");
        }

        [Test]
        public void RegenerationHealsWithoutRaising()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            FightAMonster(game);
            BattleStack slime = game.Battle.stacks.Find(s => s.side == 1);
            slime.creature = (int)CreatureId.Slime;
            slime.count = slime.startCount = 400;
            slime.health = 1;
            int round = game.Battle.round;
            var events = new List<GameEvent>();
            game.TakeEvents();
            for (int step = 0; step < 400 && game.InBattle && game.Battle.round == round; step++)
            {
                Assert.That(game.Apply(BattleAI.Next(game)), Is.True);
                events.AddRange(game.TakeEvents());
            }
            Assert.That(game.Battle.round, Is.EqualTo(round + 1), "the round never ended");
            GameEvent healed = events.Find(e => e.kind == EventKind.StackHealed && e.a == slime.id);
            Assert.That(healed, Is.Not.Null, "the slime did not regenerate");
            Assert.That(healed.b, Is.InRange(1, Creatures.Get(CreatureId.Slime).Health), "the health healed is off");
            Assert.That(healed.c, Is.EqualTo(0), "regeneration raised the dead");
            Assert.That(healed.e, Is.EqualTo(0));
        }

        [Test]
        public void ResurrectionIsOneEvent()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            hero.spells.Add((int)SpellId.Resurrection);
            hero.knowledge = 20;
            hero.mana = 200;
            FightAMonster(game);
            BattleStack current = UntilTheAttackersTurn(game);
            BattleStack fallen = game.Battle.stacks.Find(s => s.side == 0 && s != current && s.alive);
            fallen.alive = false;
            fallen.count = 0;
            fallen.health = 0;
            Assert.That(game.ValidSpellTarget(0, SpellId.Resurrection, fallen.cell), Is.True);
            game.TakeEvents();
            Assert.That(game.Apply(GameCommand.BattleCast(0, SpellId.Resurrection, fallen.cell)), Is.True);
            List<GameEvent> events = game.TakeEvents();
            List<GameEvent> healed = events.FindAll(e => e.kind == EventKind.StackHealed);
            Assert.That(healed.Count, Is.EqualTo(1), "the raised came back through more than one event");
            Assert.That(healed[0].a, Is.EqualTo(fallen.id));
            Assert.That(healed[0].c, Is.EqualTo(fallen.count).And.GreaterThan(0));
            Assert.That(healed[0].d, Is.EqualTo(fallen.cell));
            Assert.That(healed[0].e, Is.EqualTo(1), "the stack's return was not told");
            Assert.That(Find(events, EventKind.CreaturesRaised), Is.Null, "the raised were counted twice");
            Assert.That(fallen.alive, Is.True);
        }

        [Test]
        public void ACureTellsEverySpellItLifts()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            hero.spells.Add((int)SpellId.Cure);
            hero.knowledge = 20;
            hero.mana = 200;
            FightAMonster(game);
            BattleStack current = UntilTheAttackersTurn(game);
            current.effects.Add(new EffectState { spell = (int)SpellId.Slow, rounds = 3 });
            current.effects.Add(new EffectState { spell = (int)SpellId.Bless, rounds = 3 });
            current.effects.Add(new EffectState { spell = (int)SpellId.Curse, rounds = 3 });
            current.effects.Add(new EffectState { spell = (int)SpellId.Weakness, rounds = 3, amount = 3 });
            current.health = 1;
            Assert.That(game.ValidSpellTarget(0, SpellId.Cure, current.cell), Is.True);
            game.TakeEvents();
            Assert.That(game.Apply(GameCommand.BattleCast(0, SpellId.Cure, current.cell)), Is.True);
            List<GameEvent> events = game.TakeEvents();
            List<GameEvent> lifted = events.FindAll(e => e.kind == EventKind.EffectRemoved);
            Assert.That(lifted.ConvertAll(e => (SpellId)e.b),
                Is.EqualTo(new List<SpellId> { SpellId.Slow, SpellId.Curse, SpellId.Weakness }), "the spells lifted were not all told");
            Assert.That(lifted.TrueForAll(e => e.a == current.id), Is.True);
            Assert.That(current.effects.ConvertAll(e => (SpellId)e.spell), Is.EqualTo(new List<SpellId> { SpellId.Bless }));
            int cast = events.FindIndex(e => e.kind == EventKind.SpellCast);
            int healed = events.FindIndex(e => e.kind == EventKind.StackHealed);
            Assert.That(events.IndexOf(lifted[0]), Is.GreaterThan(cast), "a spell was lifted before the cure was cast");
            Assert.That(healed, Is.GreaterThan(events.IndexOf(lifted[lifted.Count - 1])), "the health came back before the spells were lifted");
        }

        [Test]
        public void ACureOnAnUnhurtStackStillLiftsItsCurses()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            hero.spells.Add((int)SpellId.Cure);
            hero.knowledge = 20;
            hero.mana = 200;
            FightAMonster(game);
            BattleStack current = UntilTheAttackersTurn(game);
            BattleStack other = game.Battle.stacks.Find(s => s.side == 0 && s != current && s.alive && !s.IsTower);
            Assert.That(other, Is.Not.Null, "the attacker has one stack");
            other.effects.Add(new EffectState { spell = (int)SpellId.Slow, rounds = 2 });
            game.TakeEvents();
            Assert.That(game.Apply(GameCommand.BattleCast(0, SpellId.Cure, other.cell)), Is.True);
            List<GameEvent> events = game.TakeEvents();
            GameEvent lifted = Find(events, EventKind.EffectRemoved);
            Assert.That(lifted, Is.Not.Null, "the slow lifted from a stack at full health was not told");
            Assert.That(lifted.a, Is.EqualTo(other.id));
            Assert.That((SpellId)lifted.b, Is.EqualTo(SpellId.Slow));
            Assert.That(Find(events, EventKind.StackHealed), Is.Null, "a stack at full health was told it healed");
            Assert.That(other.effects.Count, Is.EqualTo(0));
        }

        [Test]
        public void ARetreatingHeroKeepsHisArtifacts()
        {
            HeroesGame game = Start(31337, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Swordsman, 10);
            game.GiveArtifact(hero, ArtifactId.IronBlade);
            HeroState rival = game.State.Hero(game.State.Player(1).heroes[0]);
            rival.army.Clear();
            rival.army.Add((int)CreatureId.Berserker, 30);
            int rivalArtifacts = rival.backpack.Count;
            Duel(game);
            UntilTheAttackersTurn(game);
            game.TakeEvents();
            Assert.That(game.Apply(GameCommand.BattleRetreat(0)), Is.True);
            List<GameEvent> events = game.TakeEvents();
            Assert.That(game.Battle.result, Is.EqualTo(BattleResult.AttackerFled));
            Assert.That(game.Battle.stalled, Is.False);
            Assert.That(hero.alive, Is.False, "a hero who retreats stays on the map");
            Assert.That(Array.IndexOf(hero.equipped, (int)ArtifactId.IronBlade), Is.GreaterThanOrEqualTo(0), "the hero left his blade behind");
            Assert.That(Array.IndexOf(rival.equipped, (int)ArtifactId.IronBlade), Is.LessThan(0), "the rival took the blade of a hero who retreated");
            Assert.That(rival.backpack.Count, Is.EqualTo(rivalArtifacts));
            GameEvent gone = Find(events, EventKind.HeroDefeated);
            Assert.That(gone.text, Does.Contain("retreats"));
            int ended = events.FindIndex(e => e.kind == EventKind.BattleEnded);
            Assert.That(ended, Is.LessThan(events.IndexOf(gone)), "the map heard of the battle's end before the battle did");
            Assert.That(events[ended].battle, Is.Not.Null, "no copy of the battle as it ended");
        }

        [Test]
        public void ABeatenHeroLosesHisArtifacts()
        {
            HeroesGame game = Start(31337, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Militia, 3);
            game.GiveArtifact(hero, ArtifactId.IronBlade);
            HeroState rival = game.State.Hero(game.State.Player(1).heroes[0]);
            rival.army.Clear();
            rival.army.Add((int)CreatureId.RedDragon, 10);
            Duel(game);
            // The hero's troops stand their ground (the computer would retreat).
            for (int step = 0; step < 400 && game.InBattle; step++)
            {
                BattleStack stack = game.Battle.Current;
                Assert.That(game.Apply(stack.side == 0 ? GameCommand.BattleDefend(0, stack.id) : BattleAI.Next(game)), Is.True);
            }
            Assert.That(game.Battle.result, Is.EqualTo(BattleResult.DefenderWon));
            Assert.That(hero.alive, Is.False);
            Assert.That(Array.IndexOf(rival.equipped, (int)ArtifactId.IronBlade) >= 0 || rival.backpack.Contains((int)ArtifactId.IronBlade), Is.True,
                "the winner did not take the blade");
        }

        [Test]
        public void AStallIsARetreatThatKeepsTheHero()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            game.GiveArtifact(hero, ArtifactId.IronBlade);
            MapObject monster = FightAMonster(game);
            int cell = hero.cell;
            BattleStack stack = game.Battle.Current;
            game.Battle.actions = 2000;
            game.TakeEvents();
            Assert.That(game.Apply(GameCommand.BattleDefend(game.Battle.PlayerOf(stack.side), stack.id)), Is.True);
            List<GameEvent> events = game.TakeEvents();
            Assert.That(game.InBattle, Is.False, "the stalled battle went on");
            Assert.That(game.Battle.result, Is.EqualTo(BattleResult.AttackerFled));
            Assert.That(game.Battle.stalled, Is.True);
            Assert.That(hero.alive, Is.True, "a stalled battle cost the attacker his hero");
            Assert.That(hero.cell, Is.EqualTo(cell));
            Assert.That(hero.army.IsEmpty, Is.False, "the attacker lost his army to a stall");
            Assert.That(Array.IndexOf(hero.equipped, (int)ArtifactId.IronBlade), Is.GreaterThanOrEqualTo(0));
            Assert.That(Find(events, EventKind.HeroDefeated), Is.Null);
            Assert.That(Find(events, EventKind.BattleEnded).text, Is.Not.Null.And.Not.Empty);
            Assert.That(monster.removed, Is.False);
            Assert.That(monster.amount, Is.GreaterThan(0));
        }

        [Test]
        public void TheSixtyFirstRoundIsAStall()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            FightAMonster(game);
            game.Battle.round = 60;
            for (int step = 0; step < 400 && game.InBattle; step++)
            {
                Assert.That(game.Apply(BattleAI.Next(game)), Is.True);
            }
            Assert.That(game.InBattle, Is.False);
            if (game.Battle.AliveCount(0) > 0 && game.Battle.AliveCount(1) > 0)
            {
                Assert.That(game.Battle.stalled, Is.True);
                Assert.That(game.Battle.result, Is.EqualTo(BattleResult.AttackerFled));
                Assert.That(hero.alive, Is.True);
            }
        }

        [Test]
        public void NecromancyRaisesOnlyTheFallen()
        {
            HeroesGame game = Start(20260923, BattleStyle.Battlefield);
            HeroState hero = game.State.heroes[0];
            hero.skills.Add(new SkillEntry { skill = (int)SkillId.Necromancy, level = 3 });
            TownState town = game.State.Town(game.State.Player(1).towns[0]);
            HeroState rival = game.State.HeroAt(town.cell);
            if (rival != null)
            {
                for (int cell = 0; cell < game.Grid.Count; cell++)
                {
                    if (FreeCell(game, cell) && game.Grid.Distance(cell, town.cell) > 6)
                    {
                        rival.cell = cell;
                        break;
                    }
                }
            }
            town.garrison.Clear();
            int experience = hero.experience;
            Besiege(game, 0);
            List<GameEvent> events = game.TakeEvents();
            GameEvent started = Find(events, EventKind.BattleStarted);
            Assert.That(started.c, Is.EqualTo(-1), "no monster, but the event names one");
            Assert.That(started.d, Is.EqualTo(town.id));
            Assert.That(game.InBattle, Is.False, "an empty town held out");
            Assert.That(game.Battle.result, Is.EqualTo(BattleResult.AttackerWon));
            Assert.That(town.owner, Is.EqualTo(0));
            Assert.That(Find(events, EventKind.CreaturesRaised), Is.Null, "skeletons rose from a town nobody died in");
            foreach (ArmySlot slot in hero.army.slots)
            {
                Assert.That(slot.creature, Is.Not.EqualTo((int)CreatureId.Skeleton));
            }
            Assert.That(hero.experience - experience, Is.EqualTo(500), "the town's experience went missing");
        }

        /// <summary>
        /// Walks the first hero into a wandering army with the whole field around it on the map, after
        /// <paramref name="paint"/> has made a band of cells across the field, between the hero and the monster's side of
        /// it, into something in the way. Returns the cells of the band.
        /// </summary>
        private static List<int> CutTheField(HeroesGame game, Action<int> paint)
        {
            HeroState hero = game.State.heroes[0];
            HexGrid grid = game.Grid;
            MapObject monster = null;
            foreach (MapObject what in game.State.objects)
            {
                int x = grid.X2(what.cell);
                int y = grid.Row(what.cell);
                if (monster == null && what.kind == ObjectKind.Monster && !what.removed && x >= 18 && x <= 2 * grid.columns - 18 &&
                    y > BattleState.HalfHeight && y < grid.rows - 1 - BattleState.HalfHeight)
                {
                    monster = what;
                }
            }
            Assert.That(monster, Is.Not.Null, "no wandering army stands clear of the edges");
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Swordsman, 40);
            hero.movement = 5000;
            hero.cell = grid.Neighbors(monster.cell)[0];
            bool attackerLeft = grid.X2(hero.cell) <= grid.X2(monster.cell);
            int cx = grid.X2(monster.cell);
            int lo = attackerLeft ? cx + 4 : cx - 7;
            var band = new List<int>();
            for (int cell = 0; cell < grid.Count; cell++)
            {
                int x = grid.X2(cell);
                if (x >= lo && x <= lo + 3 && Math.Abs(grid.Row(cell) - grid.Row(monster.cell)) <= BattleState.HalfHeight)
                {
                    paint(cell);
                    band.Add(cell);
                }
            }
            Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, monster.cell)), Is.True);
            Assert.That(game.InBattle, Is.True);
            return band;
        }

        /// <summary>Whether every defender stands on or next to a cell the attackers can walk to.</summary>
        private static bool EveryDefenderCanBeReached(HeroesGame game)
        {
            BattleState battle = game.Battle;
            HexGrid grid = game.BattleGrid;
            var reached = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side == 0 && reached.Add(stack.cell))
                {
                    queue.Enqueue(stack.cell);
                }
            }
            while (queue.Count > 0)
            {
                foreach (int next in grid.Neighbors(queue.Dequeue()))
                {
                    if (battle.Passable(next, 0) && reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side != 1 || stack.IsTower)
                {
                    continue;
                }
                bool touches = reached.Contains(stack.cell);
                foreach (int next in grid.Neighbors(stack.cell))
                {
                    touches |= reached.Contains(next);
                }
                if (!touches)
                {
                    return false;
                }
            }
            return true;
        }

        [Test]
        public void TramplingNeverDrainsALake()
        {
            HeroesGame game = Start(24680, BattleStyle.OnTheMap);
            List<int> lake = CutTheField(game, cell => game.Map.terrain[cell] = (byte)TerrainType.Water);
            BattleState battle = game.Battle;
            foreach (int cell in lake)
            {
                if (battle.Contains(cell))
                {
                    Assert.That(battle.IsBlocked(cell), Is.True, "the troops walk on the lake");
                }
            }
            Assert.That(EveryDefenderCanBeReached(game), Is.True, "a defender was left beyond the lake");
            FightItOut(game);
        }

        [Test]
        public void TroopsTrampleAWayThroughTheWoods()
        {
            HeroesGame game = Start(24680, BattleStyle.OnTheMap);
            List<int> woods = CutTheField(game, cell =>
            {
                game.Map.terrain[cell] = (byte)TerrainType.Grass;
                game.Map.obstacle[cell] = (byte)Obstacle.Forest;
            });
            BattleState battle = game.Battle;
            int trampled = 0;
            foreach (int cell in woods)
            {
                if (battle.Contains(cell) && !battle.IsBlocked(cell))
                {
                    trampled++;
                    Assert.That(game.Map.occupant[cell], Is.LessThan(0), "the troops trampled a building");
                }
            }
            Assert.That(trampled, Is.GreaterThan(0), "no way was trampled through the woods");
            Assert.That(EveryDefenderCanBeReached(game), Is.True, "a defender was left beyond the woods");
            FightItOut(game);
        }

        [Test]
        public void ATroopInAPocketOfTheWoodsJoinsTheFight()
        {
            HeroesGame game = Start(24680, BattleStyle.OnTheMap);
            HeroState hero = game.State.heroes[0];
            HexGrid grid = game.Grid;
            MapObject monster = null;
            foreach (MapObject what in game.State.objects)
            {
                int x = grid.X2(what.cell);
                int y = grid.Row(what.cell);
                if (monster == null && what.kind == ObjectKind.Monster && !what.removed && x >= 18 && x <= 2 * grid.columns - 18 &&
                    y > BattleState.HalfHeight && y < grid.rows - 1 - BattleState.HalfHeight)
                {
                    monster = what;
                }
            }
            Assert.That(monster, Is.Not.Null, "no wandering army stands clear of the edges");
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Swordsman, 20);
            hero.army.Add((int)CreatureId.Crusader, 20);
            hero.movement = 5000;
            hero.cell = grid.Neighbors(monster.cell)[0];
            bool attackerLeft = grid.X2(hero.cell) <= grid.X2(monster.cell);
            int cx = grid.X2(monster.cell);
            int cy = grid.Row(monster.cell);
            // The attacker's half of the field is woods, but for a lane along the row the second troop lines up on (two
            // rows above the middle) and a clearing at the edge where the first lines up (two rows below).
            int pocket = grid.Index((cx + (attackerLeft ? -14 : 14) - 2 + (cy & 1)) / 2, cy - 2);
            for (int cell = 0; cell < grid.Count; cell++)
            {
                int x = grid.X2(cell);
                int row = grid.Row(cell);
                bool own = attackerLeft ? x < cx : x > cx;
                if (!own || Math.Abs(x - cx) > 14 || Math.Abs(row - cy) > BattleState.HalfHeight || cell == hero.cell || row == cy + 2)
                {
                    continue;
                }
                game.Map.terrain[cell] = (byte)TerrainType.Grass;
                game.Map.obstacle[cell] = (byte)(cell == pocket ? Obstacle.None : Obstacle.Forest);
                game.Map.occupant[cell] = cell == pocket ? -1 : game.Map.occupant[cell];
            }
            Assert.That(grid.X2(pocket), Is.EqualTo(cx + (attackerLeft ? -14 : 14)));
            Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, monster.cell)), Is.True);
            Assert.That(game.InBattle, Is.True);
            BattleState battle = game.Battle;
            BattleStack pocketed = battle.StackAt(pocket);
            Assert.That(pocketed, Is.Not.Null, "nobody lined up in the clearing");
            Assert.That(pocketed.side, Is.EqualTo(0));
            // Every attacker can walk up to the defenders, the one in the clearing through the woods it trampled.
            var reached = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (BattleStack stack in battle.stacks)
            {
                if (stack.side == 1 && reached.Add(stack.cell))
                {
                    queue.Enqueue(stack.cell);
                }
            }
            while (queue.Count > 0)
            {
                foreach (int next in grid.Neighbors(queue.Dequeue()))
                {
                    if (battle.Passable(next, 0) && reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }
            Assert.That(reached.Contains(pocketed.cell), Is.True, "the troop in the clearing cannot get out");
            FightItOut(game);
            Assert.That(battle.stalled, Is.False, "the battle stalled");
        }

        [Test]
        public void AMonsterKeepsTheGroupsThatFoundNoRoom()
        {
            HeroesGame game = Start(24680, BattleStyle.Battlefield);
            MapObject monster = FightAMonster(game);
            // One group off the field, as if it had found no room; the attacker retreats before anybody is hurt.
            BattleState battle = game.Battle;
            BattleStack group = battle.stacks.Find(s => s.side == 1);
            int amount = monster.amount;
            battle.stacks.Remove(group);
            battle.order.Remove(group.id);
            if (battle.Current == null || battle.Current.side != 0)
            {
                battle.current = battle.stacks.Find(s => s.side == 0).id;
            }
            Assert.That(game.Apply(GameCommand.BattleRetreat(0)), Is.True);
            Assert.That(monster.amount, Is.EqualTo(amount), "the monster lost a group that never fought");
        }

        [Test]
        public void ADamageEstimateBracketsTheBlowAndDrawsNothing()
        {
            GameState state = MapGenerator.Generate(Spec(24680, BattleStyle.Battlefield));
            var random = new SeededRandom(99);
            var game = new HeroesGame(state, random);
            game.Begin();
            FightAMonster(game);
            BattleStack stack = UntilTheAttackerCanStrike(game, out BattleStack target, out int _);
            uint before = random.State;
            game.DamageEstimate(stack, target, false, 2, out int least, out int most, out int fewest, out int mostKills);
            Assert.That(random.State, Is.EqualTo(before), "the estimate drew random numbers");
            int average = game.Damage(stack, target, false, 2, false, out _);
            Assert.That(least, Is.LessThanOrEqualTo(average));
            Assert.That(most, Is.GreaterThanOrEqualTo(average));
            Assert.That(fewest, Is.LessThanOrEqualTo(mostKills));
            Assert.That(fewest, Is.EqualTo(game.Kills(target, least)));
            Assert.That(mostKills, Is.LessThanOrEqualTo(target.count));
            for (int i = 0; i < 20; i++)
            {
                int rolled = game.Damage(stack, target, false, 2, true, out bool lucky);
                if (!lucky)
                {
                    Assert.That(rolled, Is.InRange(least, most), "a blow fell outside its estimate");
                }
            }
        }
    }
}
