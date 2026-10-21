using System.Collections.Generic;
using NUnit.Framework;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The shared screen and the remote controls, as far as the rules see them (<see cref="RemoteView"/>, the hot seat's
    /// hand-over with no seat at the device): what the panel of a remote offers its seat at every moment of the game, what
    /// it shows of the seat's realm, that auto combat on a remote never retreats on its own, and that a device without a
    /// seat (the shared screen) never asks anybody to take it.
    /// </summary>
    public class RemoteViewTests
    {
        private static MapSpec Spec(uint seed)
        {
            var spec = new MapSpec { seed = seed, columns = 30, rows = 30, treasure = 2, monsters = 2 };
            spec.players.Add(new PlayerSpec { human = true, name = "Alice", faction = Faction.Castle, team = 0, aiLevel = 1 });
            spec.players.Add(new PlayerSpec { human = true, name = "Bob", faction = Faction.Necropolis, team = 1, aiLevel = 1 });
            return spec;
        }

        private static LockstepGame Table(uint seed = 777)
        {
            var table = new LockstepGame(MapGenerator.Generate(Spec(seed)));
            table.Game.Begin();
            table.Game.TakeEvents();
            return table;
        }

        /// <summary>Walks the first hero of <paramref name="seat"/> into the nearest wandering army, as the log would: the battle is on.</summary>
        private static void AttackTheWilds(LockstepGame table, int seat, uint stamp)
        {
            HeroesGame game = table.Game;
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
            HeroState hero = game.State.Hero(game.State.Player(seat).heroes[0]);
            hero.army.Clear();
            hero.army.Add((int)CreatureId.Cavalier, 5);
            hero.army.Add((int)CreatureId.Militia, 20);
            hero.movement = 5000;
            hero.cell = game.Grid.Neighbors(monster.cell)[0];
            game.TakeEvents();
            Assert.That(table.Apply(GameCommand.Move(seat, hero.id, monster.cell), stamp), Is.True);
            Assert.That(game.InBattle, Is.True, "walking into the army started no battle");
            game.TakeEvents();
        }

        // ------------------------------------------------------------------ what the panel offers

        [Test]
        public void TheSeatWhoseTurnItIsMovesAndTheOthersWatch()
        {
            HeroesGame game = Table().Game;

            Assert.That(game.WaitingPlayer, Is.EqualTo(0));
            Assert.That(RemoteView.ModeOf(game, 0), Is.EqualTo(RemoteMode.Moving));
            Assert.That(RemoteView.ModeOf(game, 1), Is.EqualTo(RemoteMode.Watching));
            Assert.That(RemoteView.ModeOf(game, -1), Is.EqualTo(RemoteMode.Watching), "the shared screen, which has no seat, watches");
            Assert.That(RemoteView.ModeOf(null, 0), Is.EqualTo(RemoteMode.Watching));
        }

        [Test]
        public void AQuestionIsForItsPlayerToAnswer()
        {
            HeroesGame game = Table().Game;
            int hero = game.State.Player(1).heroes[0];
            game.State.pending.Add(new PendingChoice { kind = ChoiceKind.Treasure, player = 1, hero = hero, options = new[] { 500, 1000 } });

            Assert.That(RemoteView.ModeOf(game, 1), Is.EqualTo(RemoteMode.Choosing));
            Assert.That(RemoteView.ModeOf(game, 0), Is.EqualTo(RemoteMode.Watching), "the turn's owner waits while the question stands");
            Assert.That(RemoteView.AutoMove(game, 1), Is.Null, "no battle move while a question stands");
        }

        [Test]
        public void TheGameOverLeavesEverybodyWatching()
        {
            HeroesGame game = Table().Game;
            game.State.over = true;

            Assert.That(RemoteView.ModeOf(game, 0), Is.EqualTo(RemoteMode.Watching));
            Assert.That(RemoteView.ModeOf(game, 1), Is.EqualTo(RemoteMode.Watching));
        }

        [Test]
        public void ABattleIsFoughtByTheOwnerOfTheStackThatActs()
        {
            LockstepGame table = Table();
            HeroesGame game = table.Game;
            AttackTheWilds(table, 0, 11);
            BattleStack current = game.Battle.Current;
            Assert.That(current, Is.Not.Null);
            int acting = game.Battle.PlayerOf(current.side);
            Assert.That(acting, Is.EqualTo(0).Or.EqualTo(-1), "the attacker or the wilds act first");

            Assert.That(RemoteView.ModeOf(game, 0), Is.EqualTo(acting == 0 ? RemoteMode.Fighting : RemoteMode.Watching));
            Assert.That(RemoteView.ModeOf(game, 1), Is.EqualTo(RemoteMode.Watching), "the other seat only watches the battle");
            Assert.That(RemoteView.AutoMove(game, 1), Is.Null);
            if (acting == 0)
            {
                GameCommand move = RemoteView.AutoMove(game, 0);
                Assert.That(move, Is.Not.Null);
                Assert.That(move.IsBattle, Is.True);
                Assert.That(move.player, Is.EqualTo(0), "the move is the seat's own");
                Assert.That(move.kind, Is.Not.EqualTo(CommandKind.BattleRetreat), "auto combat never retreats by itself");
                Assert.That(RemoteView.MayRetreat(game, 0), Is.True, "a hero leads the attack, so he may flee");
                Assert.That(RemoteView.MayWait(game, 0), Is.True, "nothing has waited yet this round");
                Assert.That(table.Apply(move, 12), Is.True, "the move auto combat makes is one the rules take");
            }
        }

        [Test]
        public void AutoCombatDefendsRatherThanRetreats()
        {
            LockstepGame table = Table();
            HeroesGame game = table.Game;
            AttackTheWilds(table, 0, 21);
            // Fight the whole battle for the seat through auto combat: however it goes, the seat never flees.
            for (int guard = 0; guard < 200 && game.InBattle; guard++)
            {
                int acting = game.Battle.Current != null ? game.Battle.PlayerOf(game.Battle.Current.side) : -1;
                GameCommand move = acting == 0 ? RemoteView.AutoMove(game, 0) : BattleAI.Next(game) ?? LockstepGame.DefaultMove(game);
                Assert.That(move, Is.Not.Null);
                if (acting == 0)
                {
                    Assert.That(move.kind, Is.Not.EqualTo(CommandKind.BattleRetreat));
                }
                if (!table.Apply(move, (uint)(30 + guard)))
                {
                    table.Apply(LockstepGame.DefaultMove(game), (uint)(300 + guard));
                }
                game.TakeEvents();
            }
            Assert.That(game.InBattle, Is.False, "the battle was fought to its end");
        }

        // ------------------------------------------------------------------ what the panel shows

        [Test]
        public void TheHeroInHandIsTheSeatsOwnElseOneWithMovesLeft()
        {
            GameState state = Table().Game.State;
            PlayerState alice = state.Player(0);
            PlayerState bob = state.Player(1);
            HeroState own = state.Hero(alice.heroes[0]);
            HeroState other = state.Hero(bob.heroes[0]);

            Assert.That(RemoteView.HeroInHand(state, 0, own.id), Is.SameAs(own));
            Assert.That(RemoteView.HeroInHand(state, 0, other.id), Is.Not.SameAs(other), "another realm's hero is never the one in hand");
            Assert.That(RemoteView.HeroInHand(state, 0, -1), Is.Not.Null);
            Assert.That(RemoteView.HeroInHand(state, 0, -1).owner, Is.EqualTo(0));
            own.movement = 0;
            Assert.That(RemoteView.HeroInHand(state, 0, -1), Is.Not.Null, "a hero without moves is still shown when he is the only one");
            Assert.That(RemoteView.HeroInHand(state, -1, own.id), Is.Null, "the shared screen has no hero in hand");
            Assert.That(RemoteView.HeroesWithMoves(state, 0), Is.Empty);
            own.movement = 100;
            Assert.That(RemoteView.HeroesWithMoves(state, 0), Has.Count.EqualTo(1));
        }

        [Test]
        public void TheArmyAndTheTreasuryAreWrittenAsLines()
        {
            var army = new Army();
            army.Add((int)CreatureId.Cavalier, 1);
            army.Add((int)CreatureId.Militia, 20);
            string line = RemoteView.ArmyLine(army);
            Assert.That(line, Is.EqualTo($"1 {Creatures.Get((int)CreatureId.Cavalier).Name}, 20 {Creatures.Get((int)CreatureId.Militia).Plural}"));
            Assert.That(RemoteView.ArmyLine(army, name => name.ToUpperInvariant()), Does.Contain(Creatures.Get((int)CreatureId.Militia).Plural.ToUpperInvariant()),
                "the names go through the translation given");
            Assert.That(RemoteView.ArmyLine(new Army()), Is.Empty);
            Assert.That(RemoteView.ArmyLine(null), Is.Empty);

            var player = new PlayerState { index = 0, resources = new ResourceSet(12500, 20, 10) };
            string treasury = RemoteView.ResourcesLine(player);
            Assert.That(treasury, Does.StartWith("12,500 Gold"));
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                Assert.That(treasury, Does.Contain(Land.ResourceName((ResourceKind)i)), "every kind is listed, held or not");
            }
            Assert.That(RemoteView.ResourcesLine(null), Is.Empty, "the shared screen has no treasury");
        }

        // ------------------------------------------------------------------ a device without a seat

        [Test]
        public void TheSharedScreenNeverTakesTheDevice()
        {
            LockstepGame table = Table();
            GameState state = table.Game.State;
            var nobody = new List<int>();

            Assert.That(HotSeat.HandOverTo(state, -1, state.currentPlayer, nobody), Is.EqualTo(-1), "no seat here: nobody to hand the device to");
            Assert.That(HotSeat.NextPerson(state, 0, nobody), Is.EqualTo(-1));
            state.pending.Add(new PendingChoice { kind = ChoiceKind.Treasure, player = 1, hero = state.Player(1).heroes[0], options = new[] { 500, 1000 } });
            Assert.That(HotSeat.HandOverTo(state, -1, 1, nobody), Is.EqualTo(-1), "a question of a seat played elsewhere hands nothing over");
            state.pending.Clear();
            AttackTheWilds(table, 0, 41);
            Assert.That(HotSeat.ToBattle(state, -1, 0, 0, -1, nobody), Is.EqualTo(-1), "a battle of a seat played elsewhere hands nothing over");
        }
    }
}
