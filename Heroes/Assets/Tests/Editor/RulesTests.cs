using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The rules of the game, checked without any of Unity in the way: the grid, the maps the generator lays out, the
    /// way heroes find their road, what a battle does to an army, and that a game played twice from the same seed
    /// comes out exactly the same, which is what an online game rests on.
    /// </summary>
    public class RulesTests
    {
        private static MapSpec Spec(uint seed, int columns = 30, int rows = 34, int players = 2)
        {
            var spec = new MapSpec { seed = seed, columns = columns, rows = rows, treasure = 2, monsters = 2 };
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

        private static HeroesGame Start(uint seed, int players = 2)
        {
            GameState state = MapGenerator.Generate(Spec(seed, players: players));
            var game = new HeroesGame(state, new SeededRandom(seed ^ 0x5bd1e995u));
            game.Begin();
            return game;
        }

        // ------------------------------------------------------------------ the grid

        [Test]
        public void EveryCellHasNeighboursThatKnowItBack()
        {
            var grid = new HexGrid(9, 11);
            for (int cell = 0; cell < grid.Count; cell++)
            {
                foreach (int neighbour in grid.Neighbors(cell))
                {
                    Assert.That(grid.Neighbors(neighbour), Contains.Item(cell),
                        $"cell {cell} and {neighbour} do not agree that they touch");
                    Assert.That(grid.Distance(cell, neighbour), Is.EqualTo(1));
                }
            }
        }

        [Test]
        public void DistanceIsSymmetricAndZeroToItself()
        {
            var grid = new HexGrid(12, 12);
            var random = new SeededRandom(7);
            for (int i = 0; i < 200; i++)
            {
                int a = random.Range(0, grid.Count);
                int b = random.Range(0, grid.Count);
                Assert.That(grid.Distance(a, b), Is.EqualTo(grid.Distance(b, a)));
            }
            Assert.That(grid.Distance(17, 17), Is.EqualTo(0));
        }

        [Test]
        public void AxialCoordinatesComeBackToTheSameCell()
        {
            var grid = new HexGrid(14, 16);
            for (int cell = 0; cell < grid.Count; cell++)
            {
                int q = HexGrid.AxialQ(grid.Column(cell), grid.Row(cell));
                int r = HexGrid.AxialR(grid.Row(cell));
                Assert.That(grid.FromAxial(q, r), Is.EqualTo(cell));
            }
        }

        // ------------------------------------------------------------------ the map

        [Test]
        public void TheSameSeedLaysOutTheSameMap()
        {
            GameState first = MapGenerator.Generate(Spec(4242));
            GameState second = MapGenerator.Generate(Spec(4242));
            Assert.That(LockstepGame.Checksum(second), Is.EqualTo(LockstepGame.Checksum(first)));
        }

        [Test]
        public void DifferentSeedsLayOutDifferentMaps()
        {
            GameState first = MapGenerator.Generate(Spec(1));
            GameState second = MapGenerator.Generate(Spec(2));
            Assert.That(LockstepGame.Checksum(second), Is.Not.EqualTo(LockstepGame.Checksum(first)));
        }

        [Test]
        public void EveryPlayerStartsWithATownAndAHero()
        {
            HeroesGame game = Start(99, 3);
            Assert.That(game.State.players.Count, Is.EqualTo(3));
            foreach (PlayerState player in game.State.players)
            {
                Assert.That(player.towns.Count, Is.GreaterThanOrEqualTo(1), $"{player.name} has no town");
                Assert.That(player.heroes.Count, Is.GreaterThanOrEqualTo(1), $"{player.name} has no hero");
                HeroState hero = game.State.Hero(player.heroes[0]);
                Assert.That(hero.army.IsEmpty, Is.False, $"{player.name}'s hero leads nobody");
                Assert.That(hero.movement, Is.GreaterThan(0));
            }
        }

        [Test]
        public void TheWholeMapHangsTogether()
        {
            // Wandering armies and towns bar the way until they are dealt with, so the map is walked here without
            // them: what matters is that the generator left no island behind.
            foreach (uint seed in new uint[] { 1234, 55, 909090 })
            {
                HeroesGame game = Start(seed, 3);
                MapData map = game.State.map;
                HexGrid grid = map.grid;
                var seen = new bool[grid.Count];
                var queue = new Queue<int>();
                queue.Enqueue(game.State.heroes[0].cell);
                seen[game.State.heroes[0].cell] = true;
                int reached = 0;
                while (queue.Count > 0)
                {
                    int cell = queue.Dequeue();
                    reached++;
                    foreach (int next in grid.Neighbors(cell))
                    {
                        if (!seen[next] && map.Open(next))
                        {
                            seen[next] = true;
                            queue.Enqueue(next);
                        }
                    }
                }
                int open = 0;
                for (int cell = 0; cell < grid.Count; cell++)
                {
                    if (map.Open(cell))
                    {
                        open++;
                    }
                }
                Assert.That(reached, Is.EqualTo(open), $"map {seed} has {open - reached} cells cut off");
                foreach (TownState town in game.State.towns)
                {
                    Assert.That(seen[town.cell], Is.True, $"map {seed} has no way to {town.name}");
                }
            }
        }

        [Test]
        public void AHeroCanWalkToSomethingWorthHaving()
        {
            HeroesGame game = Start(1234, 3);
            HeroState hero = game.State.heroes[0];
            int found = 0;
            foreach (MapObject what in game.State.objects)
            {
                if (what.removed || what.cell == hero.cell)
                {
                    continue;
                }
                MovePlan plan = game.PlanPath(hero, what.cell);
                if (plan != null && plan.Cells.Count > 0)
                {
                    found++;
                }
            }
            Assert.That(found, Is.GreaterThan(3), "the hero has nowhere to go on his first day");
        }

        // ------------------------------------------------------------------ finding the way

        [Test]
        public void APathCostsMoreTheFurtherItGoes()
        {
            HeroesGame game = Start(555);
            HeroState hero = game.State.heroes[0];
            int target = -1;
            for (int cell = 0; cell < game.Map.grid.Count && target < 0; cell++)
            {
                if (game.Map.Open(cell) && game.Grid.Distance(hero.cell, cell) > 6)
                {
                    MovePlan check = game.PlanPath(hero, cell);
                    if (check != null && check.Cells.Count > 4)
                    {
                        target = cell;
                    }
                }
            }
            Assert.That(target, Is.GreaterThanOrEqualTo(0), "no cell far enough to walk to");
            MovePlan plan = game.PlanPath(hero, target);
            Assert.That(plan.Cells.Count, Is.EqualTo(plan.Cost.Count));
            for (int i = 1; i < plan.Cost.Count; i++)
            {
                Assert.That(plan.Cost[i], Is.GreaterThan(plan.Cost[i - 1]), "a step cost nothing");
            }
            Assert.That(plan.Destination, Is.EqualTo(target));
            for (int i = 1; i < plan.Cells.Count; i++)
            {
                Assert.That(game.Grid.Distance(plan.Cells[i - 1], plan.Cells[i]), Is.EqualTo(1), "the path jumps");
            }
        }

        [Test]
        public void AHeroSpendsMovementWalking()
        {
            HeroesGame game = Start(808);
            HeroState hero = game.State.heroes[0];
            int target = -1;
            for (int cell = 0; cell < game.Map.grid.Count && target < 0; cell++)
            {
                MovePlan plan = game.Map.Open(cell) && game.Grid.Distance(hero.cell, cell) == 3
                    ? game.PlanPath(hero, cell)
                    : null;
                if (plan != null && plan.ReachesToday && game.State.ObjectAt(cell) == null)
                {
                    target = cell;
                }
            }
            Assert.That(target, Is.GreaterThanOrEqualTo(0), "nowhere to walk");
            int before = hero.movement;
            Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, target)), Is.True);
            Assert.That(hero.cell, Is.EqualTo(target));
            Assert.That(hero.movement, Is.LessThan(before));
        }

        // ------------------------------------------------------------------ turns

        [Test]
        public void ANewDayGivesIncomeAndMovement()
        {
            HeroesGame game = Start(31337);
            PlayerState player = game.State.Player(0);
            HeroState hero = game.State.Hero(player.heroes[0]);
            int gold = player.resources.Gold;
            hero.movement = 0;
            for (int i = 0; i < game.State.players.Count; i++)
            {
                Assert.That(game.Apply(GameCommand.EndTurn(game.State.currentPlayer)), Is.True);
            }
            Assert.That(game.State.day, Is.EqualTo(2));
            Assert.That(player.resources.Gold, Is.GreaterThan(gold), "no income arrived");
            Assert.That(hero.movement, Is.GreaterThan(0), "the hero did not rest");
        }

        [Test]
        public void ATownGrowsItsCreaturesEveryWeek()
        {
            HeroesGame game = Start(6161);
            TownState town = game.State.towns[0];
            town.built.Add((int)BuildingId.Fort);
            town.built.Add((int)BuildingId.Dwelling1);
            town.available[0] = 0;
            // Seven days of turns for everybody.
            for (int day = 0; day < 7; day++)
            {
                for (int i = 0; i < game.State.players.Count; i++)
                {
                    game.Apply(GameCommand.EndTurn(game.State.currentPlayer));
                }
            }
            Assert.That(game.State.Week, Is.EqualTo(2));
            Assert.That(town.available[0], Is.GreaterThan(0), "the dwelling stayed empty");
        }

        // ------------------------------------------------------------------ battle

        [Test]
        public void ABattleEndsWithOneSideLeft()
        {
            HeroesGame game = Start(24680);
            HeroState hero = game.State.heroes[0];
            MapObject monster = null;
            foreach (MapObject what in game.State.objects)
            {
                if (what.kind == ObjectKind.Monster && !what.removed)
                {
                    monster = what;
                    break;
                }
            }
            Assert.That(monster, Is.Not.Null, "the map holds no wandering army");

            // Put the hero next to it with an army strong enough to be interesting, and walk in.
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Swordsman, 40);
            hero.army.Add((int)CreatureId.Archer, 30);
            hero.movement = 5000;
            hero.cell = game.Grid.Neighbors(monster.cell)[0];
            Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, monster.cell)), Is.True);
            Assert.That(game.InBattle, Is.True, "walking into the army started no battle");

            for (int step = 0; step < 4000 && game.InBattle; step++)
            {
                GameCommand move = BattleAI.Next(game);
                Assert.That(move, Is.Not.Null, "the computer had no move in a battle");
                Assert.That(game.Apply(move), Is.True, $"the rules refused {move.kind}");
            }
            Assert.That(game.InBattle, Is.False, "the battle never ended");
            Assert.That(game.Battle.AliveCount(0) == 0 || game.Battle.AliveCount(1) == 0, Is.True,
                "both sides came out of the battle alive");
        }

        [Test]
        public void ArmiesKeepTheirCreaturesWhenTheyChangeHands()
        {
            var army = new Army();
            Assert.That(army.Add((int)CreatureId.Militia, 12), Is.True);
            Assert.That(army.Add((int)CreatureId.Militia, 5), Is.True);
            Assert.That(army.StackCount, Is.EqualTo(1), "the same creature took two places");
            Assert.That(army.TotalCreatures, Is.EqualTo(17));
            Army copy = army.Clone();
            copy.slots[0].count = 1;
            Assert.That(army.TotalCreatures, Is.EqualTo(17), "the copy is the same army");
        }

        // ------------------------------------------------------------------ playing in step

        [Test]
        public void TheSameCommandsGiveTheSameGame()
        {
            var commands = new List<(GameCommand command, uint stamp)>();
            var first = new LockstepGame(MapGenerator.Generate(Spec(777)));
            first.Game.Begin();
            var brain = new AdventureAI();
            uint stamp = 12345;
            for (int i = 0; i < 300 && !first.Game.IsOver; i++)
            {
                int who = first.Game.WaitingPlayer;
                if (who < 0 && !first.Game.InBattle)
                {
                    break;
                }
                GameCommand command = first.Game.InBattle ? BattleAI.Next(first.Game) : brain.Next(first.Game, who);
                command ??= GameCommand.EndTurn(who);
                stamp = stamp * 1664525u + 1013904223u;
                if (first.Apply(command, stamp))
                {
                    commands.Add((command, stamp));
                }
            }
            Assert.That(commands.Count, Is.GreaterThan(20), "the game hardly moved");

            var second = new LockstepGame(MapGenerator.Generate(Spec(777)));
            second.Game.Begin();
            foreach ((GameCommand command, uint each) in commands)
            {
                Assert.That(second.Apply(command, each), Is.True, $"the replay refused {command.kind}");
            }
            Assert.That(second.Checksum(), Is.EqualTo(first.Checksum()), "the replay came out differently");
            // Applied counts every action the log handed over, refused ones included; the replay only has the
            // accepted ones, so the number to compare is the count of those.
            Assert.That(second.Applied, Is.EqualTo(commands.Count));
        }

        [Test]
        public void ATimeoutAlwaysMakesAMove()
        {
            var game = new LockstepGame(MapGenerator.Generate(Spec(4321)));
            game.Game.Begin();
            for (int i = 0; i < 40 && !game.Game.IsOver; i++)
            {
                GameCommand made = game.Timeout((uint)(i * 7919 + 13));
                Assert.That(made, Is.Not.Null, "the clock ran out and nothing happened");
            }
        }

        [Test]
        public void ComputerPlayersFinishASeasonWithoutGettingStuck()
        {
            HeroesGame game = Start(20260923, 3);
            var brain = new AdventureAI();
            int refused = 0;
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
                if (game.Apply(command))
                {
                    refused = 0;
                    continue;
                }
                brain.Refused(command);
                refused++;
                Assert.That(refused, Is.LessThan(20), $"the rules keep refusing {command.kind} on day {game.State.day}");
            }
            Assert.That(game.State.day, Is.GreaterThan(3), "the game barely started");
        }
    }
}
