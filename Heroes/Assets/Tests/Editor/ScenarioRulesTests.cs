using System.Collections.Generic;
using NUnit.Framework;
using Gamebox.Lockstep;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// How a scenario is won and lost (<see cref="ScenarioRules"/>): holding out until a day, beating a named hero,
    /// holding a number of towns, keeping the first town, keeping the named hero and the day limit; and what a
    /// scenario's recipe can say of its map beyond the usual: where a home lies, a town a seat holds from the start, and
    /// how wooded the land is. Plain rules, run by the editor and by dotnet test alike.
    /// </summary>
    public class ScenarioRulesTests
    {
        private static MapSpec Spec(uint seed, int players = 2, int columns = 30, int rows = 34)
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

        private static HeroesGame Start(MapSpec spec)
        {
            var game = new HeroesGame(MapGenerator.Generate(spec), new SeededRandom(spec.seed ^ 0x5bd1e995u));
            game.Begin();
            game.TakeEvents();
            return game;
        }

        /// <summary>Every realm ends its turn until the day <paramref name="day"/> begins (or the game is over).</summary>
        private static void PassDays(HeroesGame game, int day)
        {
            for (int guard = 0; guard < 400 && !game.IsOver && game.State.day < day; guard++)
            {
                int who = game.WaitingPlayer;
                Assert.That(who, Is.GreaterThanOrEqualTo(0), "nobody's turn on the map");
                Assert.That(game.Apply(GameCommand.EndTurn(who)), Is.True, "the turn would not end");
            }
        }

        private static bool FreeCell(HeroesGame game, int cell)
        {
            return game.Map.Open(cell) && game.Map.occupant[cell] < 0 && game.State.HeroAt(cell) == null &&
                   game.TownAtGate(cell) == null && game.GuardOf(cell) == null;
        }

        /// <summary>
        /// Stands <paramref name="hero"/> beside <paramref name="target"/> (the monster there may well guard the cell he
        /// stands on), gives him <paramref name="army"/> and walks him in. A town nobody defends falls without a battle.
        /// </summary>
        private static void Attack(HeroesGame game, HeroState hero, int target, params (CreatureId creature, int count)[] army)
        {
            hero.army.Clear();
            foreach ((CreatureId creature, int count) in army)
            {
                hero.army.Add((int)creature, count);
            }
            hero.movement = 5000;
            foreach (int next in game.Grid.Neighbors(target))
            {
                if (game.Map.Open(next) && game.Map.occupant[next] < 0 && game.State.HeroAt(next) == null && game.TownAtGate(next) == null)
                {
                    hero.cell = next;
                    Assert.That(game.Apply(GameCommand.Move(hero.owner, hero.id, target)), Is.True, "the hero would not attack");
                    return;
                }
            }
            Assert.Fail("no room beside the target");
        }

        /// <summary>The computer fights both sides; a retreat it would choose for either is left out, so the battle is fought to its end.</summary>
        private static void FightItOut(HeroesGame game)
        {
            for (int step = 0; step < 4000 && game.InBattle; step++)
            {
                GameCommand move = BattleAI.Next(game);
                if (move.kind == CommandKind.BattleRetreat)
                {
                    move = GameCommand.BattleDefend(move.player, game.Battle.current);
                }
                Assert.That(game.Apply(move), Is.True, $"the rules refused {move.kind}");
            }
            Assert.That(game.InBattle, Is.False, "the battle never ended");
        }

        // ------------------------------------------------------------------ winning

        [Test]
        public void HoldingOutUntilTheDayWins()
        {
            MapSpec spec = Spec(5150);
            spec.rules.victory = VictoryKind.Survive;
            spec.rules.victoryValue = 4;
            HeroesGame game = Start(spec);
            PassDays(game, 3);
            Assert.That(game.IsOver, Is.False, "the game ended before the day");
            PassDays(game, 4);
            Assert.That(game.IsOver, Is.True, "the day came and nothing happened");
            Assert.That(game.State.winner, Is.EqualTo(0), "the person held out but did not win");
        }

        [Test]
        public void TheNamedHeroIsTheStartingHeroOfHisSeat()
        {
            MapSpec spec = Spec(6160, 3);
            spec.players[2].hero = 18;
            spec.rules.victory = VictoryKind.DefeatHero;
            spec.rules.victoryTag = "player:2";
            GameState state = MapGenerator.Generate(spec);
            HeroState foe = state.Hero(state.rules.victoryValue);
            Assert.That(foe, Is.Not.Null, "no hero named");
            Assert.That(foe.owner, Is.EqualTo(2));
            Assert.That(foe.def, Is.EqualTo(18), "not the hero the seat starts with");
        }

        [Test]
        public void BeatingTheNamedHeroWins()
        {
            MapSpec spec = Spec(6161, 3);
            spec.rules.victory = VictoryKind.DefeatHero;
            spec.rules.victoryTag = "player:1";
            HeroesGame game = Start(spec);
            HeroState foe = game.State.Hero(game.State.rules.victoryValue);
            HeroState hero = game.State.heroes[0];
            foe.army.Clear();
            foe.army.Add((int)CreatureId.Militia, 3);
            foe.cell = -1;
            // The foe stands on free ground away from his town, and the person's hero walks into him.
            for (int cell = 0; cell < game.Grid.Count && foe.cell < 0; cell++)
            {
                if (FreeCell(game, cell) && game.Grid.Distance(cell, game.State.Town(game.State.Player(1).towns[0]).cell) > 3)
                {
                    foreach (int next in game.Grid.Neighbors(cell))
                    {
                        if (FreeCell(game, next) && next != cell)
                        {
                            foe.cell = cell;
                            break;
                        }
                    }
                }
            }
            Attack(game, hero, foe.cell, (CreatureId.Crusader, 40), (CreatureId.Archer, 40));
            FightItOut(game);
            Assert.That(foe.alive, Is.False, "the named hero is still on the map");
            Assert.That(game.IsOver, Is.True, "beating him did not end the game");
            Assert.That(game.State.winner, Is.EqualTo(0));
            Assert.That(game.State.Player(2).alive, Is.True, "the third realm is still in it: the named hero alone won the game");
        }

        [Test]
        public void HoldingEnoughTownsWins()
        {
            MapSpec spec = Spec(7170);
            spec.neutralTowns = 1;
            spec.rules.victory = VictoryKind.HoldTowns;
            spec.rules.victoryValue = 2;
            HeroesGame game = Start(spec);
            TownState neutral = game.State.towns.Find(town => town.owner < 0);
            Assert.That(neutral, Is.Not.Null, "the map has no town of the wilds");
            neutral.garrison.Clear();
            neutral.garrison.Add((int)CreatureId.Militia, 2);
            Attack(game, game.State.heroes[0], neutral.cell, (CreatureId.Crusader, 30));
            FightItOut(game);
            Assert.That(neutral.owner, Is.EqualTo(0), "the town did not change hands");
            Assert.That(game.IsOver, Is.True, "two towns held and the game goes on");
            Assert.That(game.State.winner, Is.EqualTo(0));
        }

        [Test]
        public void OneTownTooFewIsNoVictory()
        {
            MapSpec spec = Spec(7171);
            spec.rules.victory = VictoryKind.HoldTowns;
            spec.rules.victoryValue = 2;
            HeroesGame game = Start(spec);
            PassDays(game, 3);
            Assert.That(game.IsOver, Is.False, "one town was enough");
        }

        // ------------------------------------------------------------------ losing

        [Test]
        public void LosingTheFirstTownLoses()
        {
            MapSpec spec = Spec(8180);
            spec.rules.loss = LossKind.LoseTown;
            HeroesGame game = Start(spec);
            TownState home = game.State.Town(game.State.Player(0).towns[0]);
            Assert.That(game.State.rules.lossValue, Is.EqualTo(home.id), "the town to keep is not the person's first");
            // The person's hero is out of the way; the rival walks into the undefended town on his turn.
            HeroState hero = game.State.heroes[0];
            hero.cell = -1;
            foreach (int cell in game.Grid.Disk(home.center, 8))
            {
                if (FreeCell(game, cell) && game.Grid.Distance(cell, home.cell) > 4)
                {
                    hero.cell = cell;
                    break;
                }
            }
            home.garrison.Clear();
            Assert.That(game.Apply(GameCommand.EndTurn(0)), Is.True);
            game.TakeEvents();
            HeroState rival = game.State.Hero(game.State.Player(1).heroes[0]);
            Attack(game, rival, home.cell, (CreatureId.Berserker, 30));
            FightItOut(game);
            Assert.That(home.owner, Is.EqualTo(1), "the town did not fall");
            Assert.That(game.State.Player(0).alive, Is.False, "the town fell and the person plays on");
            Assert.That(game.IsOver, Is.True);
            Assert.That(game.State.winner, Is.EqualTo(1));
        }

        [Test]
        public void LosingTheNamedHeroLoses()
        {
            MapSpec spec = Spec(9190);
            spec.rules.loss = LossKind.LoseHero;
            HeroesGame game = Start(spec);
            HeroState hero = game.State.heroes[0];
            Assert.That(game.State.rules.lossValue, Is.EqualTo(hero.id), "the hero to keep is not the person's first");
            MapObject monster = game.State.objects.Find(what => what.kind == ObjectKind.Monster && !what.removed);
            Assert.That(monster, Is.Not.Null, "the map holds no wandering army");
            monster.subtype = (int)CreatureId.RedDragon;
            monster.amount = 20;
            Attack(game, hero, monster.cell, (CreatureId.Militia, 2));
            FightItOut(game);
            Assert.That(hero.alive, Is.False, "the hero survived a red dragon");
            Assert.That(game.State.Player(0).alive, Is.False, "the hero fell and the person plays on");
            Assert.That(game.IsOver, Is.True);
            Assert.That(game.State.winner, Is.EqualTo(1), "the computer did not take the game");
        }

        [Test]
        public void AnotherHeroOfTheRealmFallingIsNoLoss()
        {
            MapSpec spec = Spec(9191);
            spec.rules.loss = LossKind.LoseHero;
            HeroesGame game = Start(spec);
            HeroState second = null;
            foreach (int cell in game.Grid.Neighbors(game.State.heroes[0].cell))
            {
                if (FreeCell(game, cell))
                {
                    second = game.SpawnHero(0, game.State.freeHeroes[0], cell, false);
                    break;
                }
            }
            Assert.That(second, Is.Not.Null, "no room for a second hero");
            MapObject monster = game.State.objects.Find(what => what.kind == ObjectKind.Monster && !what.removed);
            monster.subtype = (int)CreatureId.RedDragon;
            monster.amount = 20;
            Attack(game, second, monster.cell, (CreatureId.Militia, 2));
            FightItOut(game);
            Assert.That(second.alive, Is.False);
            Assert.That(game.IsOver, Is.False, "a hero the scenario does not name was lost, and with him the game");
        }

        [Test]
        public void TheDayLimitHandsTheGameToTheComputer()
        {
            MapSpec spec = Spec(1010);
            spec.rules.loss = LossKind.TimeLimit;
            spec.rules.dayLimit = 3;
            HeroesGame game = Start(spec);
            PassDays(game, 3);
            Assert.That(game.IsOver, Is.False, "the last day is still the person's to win on");
            PassDays(game, 4);
            Assert.That(game.IsOver, Is.True, "the days ran out and the game went on");
            Assert.That(game.State.winner, Is.EqualTo(1));
        }

        [Test]
        public void TheComputerMarchesOnTheTownThePersonMustKeep()
        {
            MapSpec spec = Spec(2020, 2, 44, 48);
            spec.rules.loss = LossKind.LoseTown;
            HeroesGame game = Start(spec);
            // The second week, the person's town empty and the person's hero gone; the rival strong, at home, far away.
            game.State.day = 8;
            TownState home = game.State.Town(game.State.rules.lossValue);
            home.garrison.Clear();
            game.State.heroes[0].army.Clear();
            game.State.heroes[0].army.Add((int)CreatureId.Skeleton, 1);
            HeroState rival = game.State.Hero(game.State.Player(1).heroes[0]);
            rival.army.Clear();
            rival.army.Add((int)CreatureId.Crusader, 60);
            rival.army.Add((int)CreatureId.Archer, 60);
            Assert.That(game.Grid.Distance(rival.cell, home.cell), Is.GreaterThan(20), "the rival starts too close to tell");
            var brain = new AdventureAI();
            for (int step = 0; step < 3000 && !game.IsOver && game.State.day < 16; step++)
            {
                int who = game.WaitingPlayer;
                GameCommand command = game.State.pending.Count > 0 ? GameCommand.Choose(who, 0)
                    : game.InBattle ? BattleAI.Next(game)
                    : who == 0 ? GameCommand.EndTurn(0) : brain.Next(game, who);
                if (!game.Apply(command ?? GameCommand.EndTurn(who)))
                {
                    brain.Refused(command);
                    game.Apply(game.InBattle ? GameCommand.BattleDefend(who, game.Battle.current) : GameCommand.EndTurn(who));
                }
            }
            Assert.That(home.owner, Is.EqualTo(1), "the rival never came for the town");
            Assert.That(game.IsOver, Is.True);
            Assert.That(game.State.winner, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ the recipe

        [Test]
        public void ASeatsHomeLiesWhereTheRecipeSays()
        {
            MapSpec spec = Spec(1111, 2, 40, 44);
            spec.players[0].homeX = 820;
            spec.players[0].homeY = 180;
            spec.players[1].homeX = 180;
            spec.players[1].homeY = 820;
            GameState state = MapGenerator.Generate(spec);
            HexGrid grid = state.map.grid;
            TownState mine = state.towns[state.players[0].towns[0]];
            TownState theirs = state.towns[state.players[1].towns[0]];
            Assert.That(grid.Column(mine.center), Is.GreaterThan(grid.columns / 2), "the person's home is not on the right");
            Assert.That(grid.Row(mine.center), Is.LessThan(grid.rows / 2), "the person's home is not at the bottom");
            Assert.That(grid.Column(theirs.center), Is.LessThan(grid.columns / 2), "the rival's home is not on the left");
            Assert.That(grid.Row(theirs.center), Is.GreaterThan(grid.rows / 2), "the rival's home is not at the top");
        }

        [Test]
        public void ASeatCanHoldASecondTownFromTheStart()
        {
            MapSpec spec = Spec(1212, 2, 40, 44);
            spec.specials.Add(new SpecialSpec { kind = SpecialKind.NeutralTown, faction = Faction.Castle, owner = 1, x = 500, y = 820, tag = "keep" });
            spec.specials.Add(new SpecialSpec { kind = SpecialKind.NeutralTown, faction = Faction.Stronghold, x = 820, y = 300, tag = "hall" });
            GameState state = MapGenerator.Generate(spec);
            Assert.That(state.players[1].towns.Count, Is.EqualTo(2), "the seat holds no second town");
            TownState keep = state.towns.Find(town => state.Object(town.objectId).tag == "keep");
            TownState hall = state.towns.Find(town => state.Object(town.objectId).tag == "hall");
            Assert.That(keep.owner, Is.EqualTo(1));
            Assert.That(keep.faction, Is.EqualTo(Faction.Castle));
            Assert.That(hall.owner, Is.EqualTo(-1));
            Assert.That(hall.garrison.IsEmpty, Is.False, "a town of the wilds stands undefended");
            // Both can be walked to, and nothing else stands where they do.
            var reached = Walk(state, state.heroes[0].cell);
            foreach (TownState town in new[] { keep, hall })
            {
                Assert.That(reached[town.cell], Is.True, $"no way to {town.name}");
                foreach (int cell in state.Object(town.objectId).footprint)
                {
                    Assert.That(state.map.occupant[cell], Is.EqualTo(town.objectId), $"{town.name} stands on something else");
                }
            }
        }

        [Test]
        public void WoodsThickenAndThinTheLand()
        {
            int Forests(int woods)
            {
                MapSpec spec = Spec(1313, 2, 40, 44);
                spec.woods = woods;
                GameState state = MapGenerator.Generate(spec);
                int count = 0;
                foreach (byte obstacle in state.map.obstacle)
                {
                    count += obstacle == (byte)Obstacle.Forest || obstacle == (byte)Obstacle.DeadTrees ? 1 : 0;
                }
                return count;
            }
            int plain = Forests(0);
            Assert.That(Forests(80), Is.GreaterThan(plain), "more woods made no more forest");
            Assert.That(Forests(-80), Is.LessThan(plain), "fewer woods made no less forest");
        }

        [Test]
        public void ARecipeWithoutTheNewPartsLaysOutTheMapItAlwaysDid()
        {
            // The checksums of these maps as the generator laid them out before homes, woods and towns held from the
            // start were added: skirmish and online maps must come out exactly the same.
            Assert.That(LockstepGame.Checksum(MapGenerator.Generate(Spec(4242))), Is.EqualTo(OldChecksum4242));
            Assert.That(LockstepGame.Checksum(MapGenerator.Generate(Spec(777, 3, 40, 46))), Is.EqualTo(OldChecksum777));
        }

        private const uint OldChecksum4242 = 1158481270u;
        private const uint OldChecksum777 = 3620284879u;

        /// <summary>The cells that can be walked to from <paramref name="start"/> over open land (objects aside).</summary>
        private static bool[] Walk(GameState state, int start)
        {
            HexGrid grid = state.map.grid;
            var seen = new bool[grid.Count];
            var queue = new Queue<int>();
            queue.Enqueue(start);
            seen[start] = true;
            while (queue.Count > 0)
            {
                foreach (int next in grid.Neighbors(queue.Dequeue()))
                {
                    if (!seen[next] && state.map.Open(next))
                    {
                        seen[next] = true;
                        queue.Enqueue(next);
                    }
                }
            }
            return seen;
        }
    }
}
