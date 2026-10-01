using System.Collections.Generic;
using Gamebox.Lockstep;
using NUnit.Framework;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The hot seat, as the rules see it: the map made from the setup (who plays which seat, by what name, the computer's
    /// levels as chosen), who has to hold the device before the game goes on, so nobody ever sees the map as another
    /// person sees it, and the standings at the end.
    /// </summary>
    public class HotSeatTests
    {
        private static MapSpec ThreeRealms()
        {
            var map = new MapSpec { name = "Test", seed = 4242, columns = 30, rows = 34, treasure = 2, monsters = 2 };
            map.players.Add(new PlayerSpec { faction = Faction.Castle, human = true, team = 0, name = "You" });
            map.players.Add(new PlayerSpec { faction = Faction.Necropolis, human = false, team = 1, aiLevel = 1, name = "The Pale Coven" });
            map.players.Add(new PlayerSpec { faction = Faction.Stronghold, human = false, team = 2, aiLevel = 1, name = "Stronghold" });
            return map;
        }

        private static List<HotSeatPlayer> Seats(params HotSeatPlayer[] seats)
        {
            return new List<HotSeatPlayer>(seats);
        }

        private static HotSeatPlayer Person(string name) => new HotSeatPlayer { name = name, human = true };

        private static HotSeatPlayer Computer(int level, string name = "") => new HotSeatPlayer { name = name, human = false, aiLevel = level };

        /// <summary>A state of seats only: people (true) and computer players (false), the first one's turn.</summary>
        private static GameState Table(params bool[] people)
        {
            var state = new GameState();
            for (int i = 0; i < people.Length; i++)
            {
                state.players.Add(new PlayerState { index = i, human = people[i], name = $"Seat {i}", color = (PlayerColor)i });
            }
            return state;
        }

        // ------------------------------------------------------------------ the map of a hot seat

        [Test]
        public void TheSetupDecidesWhoPlaysEverySeatAndByWhatName()
        {
            MapSpec map = ThreeRealms();
            MapSpec spec = HotSeat.Spec(map, Seats(Person("Alice"), Computer(2, "Grim"), Person("  Bob ")), 0, 3, 4);

            Assert.That(spec.players.ConvertAll(p => p.human), Is.EqualTo(new[] { true, false, true }));
            Assert.That(spec.players.ConvertAll(p => p.name), Is.EqualTo(new[] { "Alice", "Grim", "Bob" }));
            Assert.That(spec.players[1].aiLevel, Is.EqualTo(2), "the computer's level is the one chosen, not shifted by a difficulty");
            Assert.That(spec.players.ConvertAll(p => p.faction), Is.EqualTo(new[] { Faction.Castle, Faction.Necropolis, Faction.Stronghold }),
                "every seat keeps the map's faction");
            Assert.That(spec.players.ConvertAll(p => p.team), Is.EqualTo(new[] { 0, 1, 2 }));
            MapSpec.SkirmishSize(0, out int columns, out int rows);
            Assert.That((spec.columns, spec.rows, spec.treasure, spec.monsters), Is.EqualTo((columns, rows, 3, 4)));
            Assert.That(map.players[0].name, Is.EqualTo("You"), "the scenario's own map is left as it was");
            Assert.That(map.players[2].human, Is.False);
        }

        [Test]
        public void ASeatWithoutANameIsCalledAfterItsRealm()
        {
            MapSpec spec = HotSeat.Spec(ThreeRealms(), Seats(Computer(0), Computer(1), Person("Carol")), 1, 2, 2);

            Assert.That(spec.players[0].name, Is.EqualTo("Castle"), "the map's \"You\" is no name for a computer player");
            Assert.That(spec.players[1].name, Is.EqualTo("The Pale Coven"), "a computer seat keeps the map's name");
            Assert.That(spec.players[0].aiLevel, Is.EqualTo(0));
        }

        [Test]
        public void SeatsTheSetupDoesNotReachAreTheComputers()
        {
            string longName = new string('x', HotSeat.NameLength + 10);
            MapSpec spec = HotSeat.Spec(ThreeRealms(), Seats(Person(longName), Person("Dan")), 1, 2, 2);

            Assert.That(spec.players[2].human, Is.False);
            Assert.That(spec.players[0].name.Length, Is.EqualTo(HotSeat.NameLength), "a name is cut to what the panels show");
        }

        [Test]
        public void TheGeneratedGameKnowsEveryPersonByName()
        {
            MapSpec spec = HotSeat.Spec(ThreeRealms(), Seats(Person("Alice"), Computer(1), Person("Bob")), 0, 2, 2);
            GameState state = MapGenerator.Generate(spec);

            Assert.That(state.players.ConvertAll(p => p.name), Is.EqualTo(new[] { "Alice", "The Pale Coven", "Bob" }));
            Assert.That(state.players.ConvertAll(p => p.human), Is.EqualTo(new[] { true, false, true }));
            Assert.That(HotSeat.People(state), Is.EqualTo(2));
        }

        // ------------------------------------------------------------------ who holds the device

        [Test]
        public void TheNextPersonSkipsTheComputerAndThoseKnockedOut()
        {
            GameState state = Table(true, false, true, true);
            Assert.That(HotSeat.NextPerson(state, 0), Is.EqualTo(0), "a person's own turn");
            Assert.That(HotSeat.NextPerson(state, 1), Is.EqualTo(2), "after the computer");
            state.players[2].alive = false;
            Assert.That(HotSeat.NextPerson(state, 1), Is.EqualTo(3), "past a person knocked out");
            Assert.That(HotSeat.NextPerson(Table(true, true, false), 2), Is.EqualTo(0), "round to the first seat");
            Assert.That(HotSeat.NextPerson(Table(false, false), 0), Is.EqualTo(-1));
        }

        [Test]
        public void TheFirstPersonIsAskedForBeforeAnythingShows()
        {
            GameState state = Table(false, true, true);
            Assert.That(HotSeat.HandOverTo(state, -1, 0), Is.EqualTo(1), "the computer's first turn is watched by the person after it");
            Assert.That(HotSeat.HandOverTo(state, 1, 0), Is.EqualTo(-1), "who already holds it keeps it");
        }

        [Test]
        public void TheDeviceGoesToTheNextPersonAsSoonAsATurnEnds()
        {
            // Alice, the computer, Bob.
            GameState state = Table(true, false, true);
            state.currentPlayer = 0;
            Assert.That(HotSeat.HandOverTo(state, 0, 0), Is.EqualTo(-1), "Alice plays her own turn");
            state.currentPlayer = 1;
            Assert.That(HotSeat.HandOverTo(state, 0, 1), Is.EqualTo(2),
                "Bob takes the device before the computer moves: Alice's view of the map is gone");
            Assert.That(HotSeat.HandOverTo(state, 2, 1), Is.EqualTo(-1), "Bob watches the computer");
            state.currentPlayer = 2;
            Assert.That(HotSeat.HandOverTo(state, 2, 2), Is.EqualTo(-1), "and plays on without another hand-over");
            state.currentPlayer = 0;
            Assert.That(HotSeat.HandOverTo(state, 2, 0), Is.EqualTo(0), "then back to Alice");
        }

        [Test]
        public void AQuestionForAnotherPersonComesToThemAndGoesBack()
        {
            GameState state = Table(true, true);
            state.currentPlayer = 0;
            state.pending.Add(new PendingChoice { kind = ChoiceKind.LevelUp, player = 1, hero = 0 });
            Assert.That(HotSeat.HandOverTo(state, 0, 1), Is.EqualTo(1), "Bob's hero went up a level on Alice's turn");
            state.pending.Clear();
            Assert.That(HotSeat.HandOverTo(state, 1, 0), Is.EqualTo(0), "Alice gets her turn back");

            state.pending.Add(new PendingChoice { kind = ChoiceKind.Treasure, player = 0 });
            Assert.That(HotSeat.HandOverTo(state, 0, 0), Is.EqualTo(-1), "a question of her own needs no hand-over");
        }

        [Test]
        public void TwoPeopleFightTheirBattleAtOneScreen()
        {
            GameState state = Table(true, true);
            state.currentPlayer = 0;
            state.battle = new BattleState { active = true, attackerPlayer = 0, defenderPlayer = 1 };
            Assert.That(HotSeat.HandOverTo(state, 0, 1), Is.EqualTo(-1), "Bob's troops act on Alice's screen");
            Assert.That(HotSeat.HandOverTo(state, 0, 0), Is.EqualTo(-1));
        }

        [Test]
        public void APersonAttackedOnTheComputersTurnTakesTheDeviceForTheBattle()
        {
            // Alice, Bob, the computer: the computer's turn, watched by Alice (the next person), attacks Bob.
            GameState state = Table(true, true, false);
            state.currentPlayer = 2;
            state.battle = new BattleState { active = true, attackerPlayer = 2, defenderPlayer = 1 };
            Assert.That(HotSeat.HandOverTo(state, 0, 2), Is.EqualTo(1), "Bob fights his own battle, whoever's troops act first");
            Assert.That(HotSeat.HandOverTo(state, 1, 2), Is.EqualTo(-1));
            state.battle.active = false;
            Assert.That(HotSeat.HandOverTo(state, 1, 2), Is.EqualTo(0), "after it the device goes back to Alice, who plays next");
        }

        [Test]
        public void NobodyWatchesTheComputersBattlesAmongThemselvesOrTheWilds()
        {
            GameState state = Table(true, false, false);
            state.currentPlayer = 1;
            state.battle = new BattleState { active = true, attackerPlayer = 1, defenderPlayer = 2 };
            Assert.That(HotSeat.HandOverTo(state, 0, 1), Is.EqualTo(-1));
            state.battle = new BattleState { active = true, attackerPlayer = 1, defenderPlayer = -1 };
            Assert.That(HotSeat.HandOverTo(state, 0, -1), Is.EqualTo(-1));
        }

        [Test]
        public void AFinishedGameHandsNothingOver()
        {
            GameState state = Table(true, true);
            state.currentPlayer = 1;
            state.over = true;
            Assert.That(HotSeat.HandOverTo(state, 0, 1), Is.EqualTo(-1));
        }

        /// <summary>
        /// A hot seat played out by the computer's brains on every seat (two people and the computer), with the device
        /// handed over as the director does: a person never decides anything without holding the device (but in a battle
        /// they fight together), and the computer's turns are always watched by the person who plays next.
        /// </summary>
        [Test]
        public void NobodyEverPlaysOnAnotherPersonsScreen()
        {
            MapSpec spec = HotSeat.Spec(ThreeRealms(), Seats(Person("Alice"), Computer(1), Person("Bob")), 0, 2, 2);
            GameState state = MapGenerator.Generate(spec);
            var game = new HeroesGame(state, new SeededRandom(spec.seed ^ 0x5bd1e995u));
            game.Begin();
            var brain = new AdventureAI();
            int holder = -1;
            var handedTo = new List<int>();
            int refused = 0;
            for (int step = 0; step < 40000 && !game.IsOver && game.State.day <= 8; step++)
            {
                game.TakeEvents();
                int to = HotSeat.HandOverTo(game.State, holder, game.WaitingPlayer);
                if (to >= 0)
                {
                    Assert.That(to, Is.Not.EqualTo(holder));
                    holder = to;
                    handedTo.Add(to);
                }
                int who = game.WaitingPlayer;
                if (who < -1)
                {
                    break;
                }
                bool person = !game.IsComputer(who);
                if (game.InBattle)
                {
                    BattleState battle = game.Battle;
                    bool fights = holder == battle.attackerPlayer || holder == battle.defenderPlayer;
                    bool peopleFight = !game.IsComputer(battle.attackerPlayer) || !game.IsComputer(battle.defenderPlayer);
                    Assert.That(!peopleFight || fights, Is.True, $"day {game.State.day}: a battle of a person shown to {holder}");
                }
                else if (person)
                {
                    Assert.That(holder, Is.EqualTo(who), $"day {game.State.day}: seat {who} decides on the screen of {holder}");
                }
                else
                {
                    Assert.That(holder, Is.EqualTo(HotSeat.NextPerson(game.State, game.State.currentPlayer)),
                        $"day {game.State.day}: the computer's turn is watched by the wrong person");
                }
                // The people answer the questions the rules put to them (the computer's own are answered by the rules).
                GameCommand command = game.State.pending.Count > 0 ? GameCommand.Of(CommandKind.Choose, who, 0)
                    : game.InBattle ? BattleAI.Next(game) : brain.Next(game, who);
                command ??= game.InBattle ? GameCommand.BattleDefend(who, game.Battle.current) : GameCommand.EndTurn(who);
                if (game.Apply(command))
                {
                    refused = 0;
                    continue;
                }
                brain.Refused(command);
                refused++;
                Assert.That(refused, Is.LessThan(20), $"the rules keep refusing {command.kind}");
            }
            Assert.That(game.State.day, Is.GreaterThan(4), "the game barely started");
            Assert.That(handedTo[0], Is.EqualTo(0), "Alice is asked for first");
            Assert.That(handedTo.Count, Is.GreaterThanOrEqualTo(2 * (game.State.day - 1)),
                "the device changes hands at least twice a day between two people");
        }

        // ------------------------------------------------------------------ the end

        [Test]
        public void TheStandingsPutTheWinnerFirstAndTheFallenLast()
        {
            GameState state = Table(true, false, true, true);
            state.players[0].alive = false;
            state.players[0].towns.AddRange(new[] { 1, 2, 3 });
            state.players[1].towns.Add(4);
            state.players[2].towns.Add(5);
            state.players[2].battlesWon = 1;
            state.players[3].towns.Add(6);
            state.players[3].battlesWon = 4;
            state.winner = 2;
            state.over = true;

            List<PlayerState> standings = HotSeat.Standings(state);
            Assert.That(standings.ConvertAll(p => p.index), Is.EqualTo(new[] { 2, 3, 1, 0 }),
                "the winner, then those standing by their towns and battles, then the fallen");
        }

        [Test]
        public void TheComputerCanWinAHotSeat()
        {
            GameState state = Table(true, false, true);
            state.players[0].alive = false;
            state.players[2].alive = false;
            state.winner = 1;
            Assert.That(HotSeat.Standings(state)[0].index, Is.EqualTo(1));
            Assert.That(HotSeat.Standings(state)[0].human, Is.False);
        }
    }
}
