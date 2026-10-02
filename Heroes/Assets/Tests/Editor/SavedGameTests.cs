using Gamebox;
using Gamebox.Lockstep;
using NUnit.Framework;
using UnityEngine;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// A game of Heroes in a named save: the whole state of a game well under way (heroes out on the map, towns built,
    /// fights fought) goes into the YAML of a save file and comes back exactly as it was.
    /// </summary>
    public class SavedGameTests
    {
        /// <summary>A game of three realms after the computer has played <paramref name="commands"/> moves of it (or fewer, when refused).</summary>
        private static GameState PlayedGame(uint seed, int commands, out int accepted)
        {
            var spec = new MapSpec { seed = seed, columns = 30, rows = 34, treasure = 2, monsters = 2 };
            for (int i = 0; i < 3; i++)
            {
                spec.players.Add(new PlayerSpec { faction = (Faction)(i % 3), human = i == 0, team = i, aiLevel = 1, name = i == 0 ? "You: \"the\" #1" : $"Rival {i}" });
            }
            var game = new HeroesGame(MapGenerator.Generate(spec), new SeededRandom(seed ^ 0x5bd1e995u));
            game.Begin();
            var brain = new AdventureAI();
            accepted = 0;
            for (int i = 0; i < commands && !game.IsOver; i++)
            {
                int who = game.WaitingPlayer;
                if (who < 0 && !game.InBattle)
                {
                    break;
                }
                GameCommand command = game.InBattle ? BattleAI.Next(game) : brain.Next(game, who);
                if (game.Apply(command ?? GameCommand.EndTurn(who)))
                {
                    accepted++;
                }
            }
            return game.State;
        }

        [Test]
        public void AGameUnderWayComesBackFromItsSaveFileExactly()
        {
            GameState state = PlayedGame(4242, 300, out int accepted);
            Assert.That(accepted, Is.GreaterThan(20), "the game hardly moved");
            string json = JsonUtility.ToJson(state);

            SavedGame save = SavedGame.Of(GameType.Heroes, "Before the dragon", state, 3, "The Shattered Crown, day " + state.day);
            string yaml = save.ToYaml();
            SavedGame back = SavedGame.Parse(yaml);

            Assert.That(back.Game, Is.EqualTo(GameType.Heroes));
            Assert.That(back.Name, Is.EqualTo("Before the dragon"));
            Assert.That(back.Level, Is.EqualTo(3));
            Assert.That(JsonUtility.ToJson(back.Read<GameState>()), Is.EqualTo(json));
        }
    }
}
