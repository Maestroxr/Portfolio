using System.Collections.Generic;
using Gamebox.Lockstep;
using NUnit.Framework;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The online table, checked without a server: a command is read with the seat and the kind of its action and never
    /// with what its payload claims, the wandering armies only ever fight, the computer takes a seat over at the same place
    /// in the log on every device, two tables fed the same entries stay the same, the clock of a turn on the map does not
    /// take the first decision of a battle, and a table nobody plays at any more ends.
    /// </summary>
    public class LockstepTests
    {
        private static MapSpec Spec(uint seed, int columns = 30, int rows = 30, int computers = 0)
        {
            var spec = new MapSpec { seed = seed, columns = columns, rows = rows, treasure = 2, monsters = 2 };
            spec.players.Add(new PlayerSpec { human = true, name = "A", faction = Faction.Castle, team = 0, aiLevel = 1 });
            spec.players.Add(new PlayerSpec { human = true, name = "B", faction = Faction.Necropolis, team = 1, aiLevel = 1 });
            for (int i = 0; i < computers; i++)
            {
                spec.players.Add(new PlayerSpec { human = false, name = $"C{i}", faction = (Faction)((2 + i) % 3), team = 2 + i, aiLevel = 1 });
            }
            return spec;
        }

        private static LockstepGame Table(uint seed, int columns = 30, int rows = 30, int computers = 0)
        {
            var table = new LockstepGame(MapGenerator.Generate(Spec(seed, columns, rows, computers)));
            table.Game.Begin();
            return table;
        }

        /// <summary>
        /// Walks the first hero of <paramref name="seat"/> from the cell next to a wandering army into it, as the log
        /// would: the battle is on.
        /// </summary>
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
        }

        /// <summary>Everything of <paramref name="seat"/>'s realm but its heroes is gone, and they have one day left to take a town.</summary>
        private static void LoseTheTowns(HeroesGame game, int seat)
        {
            PlayerState player = game.State.Player(seat);
            foreach (int id in player.towns)
            {
                game.State.Town(id).owner = -1;
            }
            player.towns.Clear();
            player.daysWithoutTown = 6;
        }

        /// <summary>Every seat ends its turn as the clock would have it, until the next day begins or the game is over.</summary>
        private static void EndTheDay(LockstepGame table, ref uint stamp)
        {
            int day = table.Game.State.day;
            for (int guard = 0; guard < 40 && table.Game.State.day == day && !table.IsOver; guard++)
            {
                table.Apply(LockstepGame.DefaultMove(table.Game), stamp++);
                table.Game.TakeEvents();
            }
        }

        [Test]
        public void TheActionSaysWhoActsAndHow()
        {
            GameCommand move = GameCommand.Parse(1, (uint)CommandKind.MoveHero, "3,17,0,0,0");
            Assert.That(move, Is.Not.Null);
            Assert.That(move.kind, Is.EqualTo(CommandKind.MoveHero));
            Assert.That(move.player, Is.EqualTo(1), "the seat of the action is the player");
            Assert.That(move.a, Is.EqualTo(3));
            Assert.That(move.b, Is.EqualTo(17));

            GameCommand wild = GameCommand.Parse(LockstepSeats.World, (uint)CommandKind.BattleMove, "1,2,0,0,0");
            Assert.That(wild, Is.Not.Null);
            Assert.That(wild.player, Is.EqualTo(-1), "the world's seat is the wilds");
            Assert.That(wild.Seat, Is.EqualTo(LockstepSeats.World));
        }

        [Test]
        public void WhatNoPlayerCanSendIsNoCommand()
        {
            Assert.That(GameCommand.Parse(LockstepSeats.World, (uint)CommandKind.MoveHero, "1,2,0,0,0"), Is.Null, "the wilds only fight");
            Assert.That(GameCommand.Parse(LockstepSeats.World, (uint)CommandKind.EndTurn, "0,0,0,0,0"), Is.Null, "the wilds have no turn");
            Assert.That(GameCommand.Parse(0, (uint)CommandKind.SeatToComputer, "1,0,0,0,0"), Is.Null, "the server says that itself");
            Assert.That(GameCommand.Parse(LockstepSeats.None, (uint)CommandKind.EndTurn, "0,0,0,0,0"), Is.Null, "nobody's seat");
            Assert.That(GameCommand.Parse(0, 99, "1,0,0,0,0"), Is.Null, "an unknown kind");
            Assert.That(GameCommand.Parse(0, 15, "1,0,0,0,0"), Is.Null, "a gap between the kinds");
            Assert.That(GameCommand.Parse(0, uint.MaxValue, ""), Is.Null, "the clock of the server");
            Assert.That(GameCommand.Parse(0, uint.MaxValue - 1, "1"), Is.Null, "a seat going to the computer");
            Assert.That(GameCommand.Parse(0, (uint)CommandKind.MoveHero, "1,2"), Is.Null, "too few numbers");
            Assert.That(GameCommand.Parse(0, (uint)CommandKind.MoveHero, "1,2,x,0,0"), Is.Null, "not a number");
            Assert.That(GameCommand.Parse(0, (uint)CommandKind.MoveHero, null), Is.Null);
            Assert.That(GameCommand.Parse(0, (uint)CommandKind.EndTurn, "40,1,0,0,0,0,0"), Is.Null,
                "a payload naming a kind and a player of its own is not read");
        }

        [Test]
        public void EveryCommandSurvivesTheLog()
        {
            foreach (CommandKind kind in System.Enum.GetValues(typeof(CommandKind)))
            {
                if (kind == CommandKind.SeatToComputer)
                {
                    continue;
                }
                GameCommand sent = GameCommand.Of(kind, 2, 5, -6, 7, 8, 9);
                GameCommand back = GameCommand.Parse(sent.Seat, (uint)sent.kind, sent.Payload());
                Assert.That(back, Is.Not.Null, kind.ToString());
                Assert.That(back.ToString(), Is.EqualTo(sent.ToString()), kind.ToString());
            }
        }

        [Test]
        public void TheComputerTakesASeatOverAtItsPlaceInTheLog()
        {
            LockstepGame table = Table(99);
            Assert.That(table.HandToComputer(1, 2, 12345), Is.True);
            Assert.That(table.Game.State.Player(1).human, Is.False);
            Assert.That(table.Game.State.Player(1).aiLevel, Is.EqualTo(2));
            Assert.That(table.Game.State.Player(0).human, Is.True, "only that seat");
            Assert.That(table.Applied, Is.EqualTo(1), "a seat going to the computer is an entry of the log");
            Assert.That(table.HandToComputer(7, 1, 5), Is.False, "there is no such seat");
            Assert.That(table.Apply(null, 6), Is.False, "an action nobody could read");
            Assert.That(table.Applied, Is.EqualTo(3), "entries count whether the rules took them or not");

            LockstepTable<GameCommand> shared = table;
            List<GameCommand> made = shared.Timeout(77);
            Assert.That(made.Count, Is.EqualTo(1), "the clock made the default move");
            Assert.That(table.Applied, Is.EqualTo(4));
        }

        [Test]
        public void TwoTablesFedTheSameEntriesStayTheSame()
        {
            LockstepGame one = Table(5);
            LockstepGame two = Table(5);
            for (uint i = 0; i < 30; i++)
            {
                GameCommand a = one.Timeout(i * 31 + 7);
                GameCommand b = two.Timeout(i * 31 + 7);
                Assert.That(a?.ToString(), Is.EqualTo(b?.ToString()), $"timeout {i}");
            }
            one.HandToComputer(0, 1, 3);
            two.HandToComputer(0, 1, 3);
            Assert.That(one.Checksum(), Is.EqualTo(two.Checksum()));
            Assert.That(one.Applied, Is.EqualTo(two.Applied));
        }

        [Test]
        public void TheDefaultMoveAnswersAChoiceFirst()
        {
            // The move the clock makes is also what the host falls back on when the computer's own moves are refused, so it
            // has to be one the rules take whatever the game waits for.
            LockstepGame table = Table(4321);
            for (int i = 0; i < 60 && !table.Game.IsOver; i++)
            {
                GameCommand move = LockstepGame.DefaultMove(table.Game);
                Assert.That(move, Is.Not.Null);
                if (table.Game.State.pending.Count > 0)
                {
                    Assert.That(move.kind, Is.EqualTo(CommandKind.Choose));
                }
                Assert.That(table.Apply(move, (uint)(i * 2654435761u)), Is.True, $"{move.kind} for {move.player} was refused");
            }
        }

        [Test]
        public void TheClockOfTheMapTurnDoesNotTakeTheFirstDecisionOfABattle()
        {
            // A move on the map does not restart the server's clock, so a battle it opens late in the turn has what was
            // left of the turn. That timeout makes no move (it restarted the clock); the next one does, as ever.
            LockstepGame table = Table(24680, 30, 34);
            AttackTheWilds(table, 0, 11);
            int waiting = table.Game.WaitingPlayer;
            BattleStack first = table.Game.Battle.Current;
            uint before = table.Checksum();
            Assert.That(table.Timeout(12), Is.Null, "the clock of the map turn made a move in the battle");
            Assert.That(table.Checksum(), Is.EqualTo(before), "a timeout that makes no move changes nothing");
            Assert.That(table.Game.WaitingPlayer, Is.EqualTo(waiting));
            Assert.That(table.Game.Battle.Current, Is.SameAs(first));
            GameCommand made = table.Timeout(13);
            Assert.That(made, Is.Not.Null, "a whole turn of the clock later the default move is made");
            Assert.That(made.kind, Is.EqualTo(CommandKind.BattleDefend));
            Assert.That(made.a, Is.EqualTo(first.id));

            // Once a decision of the battle is in the log, the clock is the battle's own.
            LockstepGame decided = Table(24680, 30, 34);
            AttackTheWilds(decided, 0, 11);
            Assert.That(decided.Apply(LockstepGame.DefaultMove(decided.Game), 12), Is.True);
            Assert.That(decided.Game.InBattle, Is.True);
            Assert.That(decided.Timeout(13), Is.Not.Null, "the timeout of a decision in the battle made no move");
        }

        [Test]
        public void ABattleTheComputerOpensHasItsClockAlready()
        {
            // The server restarts the clock on every move the host sends for a computer player, so the timeout after a
            // battle the computer opened is a whole turn of the clock: it makes the default move.
            LockstepGame table = Table(24680, 30, 34);
            Assert.That(table.HandToComputer(0, 1, 5), Is.True);
            AttackTheWilds(table, 0, 11);
            GameCommand made = table.Timeout(12);
            Assert.That(made, Is.Not.Null);
            Assert.That(made.kind, Is.EqualTo(CommandKind.BattleDefend));

            // So does a timeout after the computer took the seat over in the middle of a battle a person opened.
            LockstepGame left = Table(24680, 30, 34);
            AttackTheWilds(left, 0, 11);
            Assert.That(left.HandToComputer(1, 1, 12), Is.True);
            Assert.That(left.Timeout(13), Is.Not.Null);
        }

        [Test]
        public void TheGameEndsOnceNobodyAtTheTablePlaysOn()
        {
            // Two members and a computer player: the guest leaves, the computer plays on for them, and the host loses
            // the realm. Only computer players are left, who need not ever settle it among themselves: the computer won.
            uint stamp = 1;
            LockstepGame left = Table(24680, 30, 34, 1);
            Assert.That(left.HandToComputer(1, 1, stamp++), Is.True);
            LoseTheTowns(left.Game, 0);
            EndTheDay(left, ref stamp);
            Assert.That(left.Game.State.Player(0).alive, Is.False, "the host still stands");
            Assert.That(left.IsOver, Is.True, "a game only the computer plays went on");
            Assert.That(left.Game.State.winner, Is.InRange(1, 2), "a computer player won");
            Assert.That(LockstepGame.DefaultMove(left.Game), Is.Null, "the clock has nothing left to do");

            // Nobody left: both members lost, two computer players stand.
            stamp = 1;
            LockstepGame lost = Table(1357, 36, 40, 2);
            LoseTheTowns(lost.Game, 0);
            LoseTheTowns(lost.Game, 1);
            EndTheDay(lost, ref stamp);
            Assert.That(lost.Game.State.Player(0).alive || lost.Game.State.Player(1).alive, Is.False);
            Assert.That(lost.IsOver, Is.True);
            Assert.That(lost.Game.State.winner, Is.InRange(2, 3), "a computer player won");

            // While one member still plays, the game goes on.
            stamp = 1;
            LockstepGame playing = Table(1357, 36, 40, 2);
            LoseTheTowns(playing.Game, 0);
            EndTheDay(playing, ref stamp);
            Assert.That(playing.Game.State.Player(0).alive, Is.False);
            Assert.That(playing.IsOver, Is.False, "one member plays on");
        }
    }
}
