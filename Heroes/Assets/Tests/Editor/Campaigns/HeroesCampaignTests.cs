using System;
using System.Collections.Generic;
using Gamebox;
using Gamebox.Lockstep;
using NUnit.Framework;
using UnityEditor;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The campaigns as the game ships them (the campaign asset the builders make): three campaigns open from the start,
    /// chapters that open one after the other inside their campaign, the old chapters and skirmish maps at the places
    /// saved games know them by, every kind of victory and loss used, and every chapter laying out a map that can be
    /// played: the person's town and hero, every rival's, every town within reach, nothing standing on anything else,
    /// and what the chapter asks for on the map and within reach. A few days of every chapter of the new campaigns are
    /// played by the computer on every seat. They read the campaign asset, so they run in the editor only (dotnet test
    /// leaves this folder out).
    /// </summary>
    public class HeroesCampaignTests
    {
        private static HeroesCampaign Campaign()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:HeroesCampaign"))
            {
                var campaign = AssetDatabase.LoadAssetAtPath<HeroesCampaign>(AssetDatabase.GUIDToAssetPath(guid));
                if (campaign != null)
                {
                    return campaign;
                }
            }
            Assert.Fail("there is no campaign asset");
            return null;
        }

        private static HeroesProgress Progress()
        {
            return new HeroesProgress(null, GameType.Heroes);
        }

        // ------------------------------------------------------------------ the campaigns

        [Test]
        public void ThreeCampaignsOfSixToEightChaptersEachOpenFromTheStart()
        {
            HeroesCampaign campaign = Campaign();
            HeroesProgress progress = Progress();
            Assert.That(campaign.ArcCount, Is.EqualTo(3));
            for (int arc = 0; arc < campaign.ArcCount; arc++)
            {
                HeroesCampaign.Arc story = campaign.GetArc(arc);
                Assert.That(story.title, Is.Not.Empty, $"campaign {arc} has no title");
                Assert.That(story.tab, Is.Not.Empty, $"{story.title} has no name for its tab");
                Assert.That(story.description, Is.Not.Empty, $"{story.title} has no description");
                List<int> chapters = campaign.ChaptersOf(arc);
                Assert.That(chapters.Count, Is.InRange(6, 8), $"{story.title} has {chapters.Count} chapters");
                Assert.That(campaign.IsUnlocked(chapters[0], progress), Is.True, $"the first chapter of {story.title} is locked");
                Assert.That(campaign.IsUnlocked(chapters[1], progress), Is.False, $"the second chapter of {story.title} is open");
                Assert.That(campaign.ChapterToPlay(arc, progress), Is.EqualTo(chapters[0]));
                Assert.That(campaign.MaxStarsOf(arc), Is.EqualTo(chapters.Count * 3));
                foreach (int index in chapters)
                {
                    Assert.That(campaign.Scenario(index).Map.players.Find(seat => seat.human).faction, Is.EqualTo(story.faction),
                        $"{campaign.Scenario(index).Title} is not played as the realm of {story.title}");
                }
            }
            Assert.That(campaign.GetArc(1).faction, Is.EqualTo(Faction.Necropolis));
            Assert.That(campaign.GetArc(2).faction, Is.EqualTo(Faction.Stronghold));
        }

        [Test]
        public void TheOldChaptersAndMapsKeepTheirPlaces()
        {
            HeroesCampaign campaign = Campaign();
            string[] old =
            {
                "The Long Road Home", "The Salt Marches", "The Barrow Fields", "Gold and Ashes", "The Hundred Isles",
                "Winter at the Gate", "The Dragon's Debt", "The Shattered Crown", "River Crossing", "Three Crowns",
                "The High Passes", "Four Winds"
            };
            for (int i = 0; i < old.Length; i++)
            {
                HeroesLevel level = campaign.Scenario(i);
                Assert.That(level.Title, Is.EqualTo(old[i]), $"place {i} changed");
                Assert.That(level.Index, Is.EqualTo(i));
                Assert.That(level.IsSkirmish, Is.EqualTo(i >= 8), $"{level.Title} changed from a chapter to a map or back");
                Assert.That(campaign.ArcOf(i), Is.EqualTo(0));
            }
            Assert.That(campaign.Skirmishes, Is.EqualTo(new List<int> { 8, 9, 10, 11 }), "the maps of the skirmish and the rooms moved");
            for (int i = old.Length; i < campaign.Count; i++)
            {
                Assert.That(campaign.Scenario(i).IsSkirmish, Is.False);
                Assert.That(campaign.Scenario(i).Index, Is.EqualTo(i));
                Assert.That(campaign.ArcOf(i), Is.GreaterThan(0), $"{campaign.Scenario(i).Title} belongs to the first campaign");
            }
        }

        [Test]
        public void AChapterOpensWhenTheOneBeforeItInItsCampaignIsWon()
        {
            HeroesCampaign campaign = Campaign();
            HeroesProgress progress = Progress();
            List<int> first = campaign.ChaptersOf(0);
            List<int> second = campaign.ChaptersOf(1);
            List<int> third = campaign.ChaptersOf(2);
            progress.RecordWin(second[0], 2, 100, 0);
            Assert.That(campaign.IsUnlocked(second[1], progress), Is.True, "winning a chapter opened nothing");
            Assert.That(campaign.IsUnlocked(second[2], progress), Is.False);
            Assert.That(campaign.IsUnlocked(third[1], progress), Is.False, "a chapter of another campaign opened");
            Assert.That(campaign.IsUnlocked(first[1], progress), Is.False, "a chapter of another campaign opened");
            Assert.That(campaign.ChapterToPlay(1, progress), Is.EqualTo(second[1]));
            Assert.That(campaign.StarsOf(1, progress), Is.EqualTo(2));
            Assert.That(campaign.StarsOf(0, progress), Is.EqualTo(0));
            Assert.That(campaign.TotalStars(progress), Is.EqualTo(2));

            // The last chapter of the first campaign does not open the first of the next one, which is open anyway.
            progress.RecordWin(first[first.Count - 1], 3, 100, 0);
            Assert.That(campaign.IsUnlocked(third[1], progress), Is.False);
            // Every chapter won: the chapter to play is the first one again.
            foreach (int index in third)
            {
                progress.RecordWin(index, 1, 10, 0);
            }
            Assert.That(campaign.ChapterToPlay(2, progress), Is.EqualTo(third[0]));
        }

        [Test]
        public void TheNextChapterIsTheNextOfItsCampaign()
        {
            HeroesCampaign campaign = Campaign();
            for (int arc = 0; arc < campaign.ArcCount; arc++)
            {
                List<int> chapters = campaign.ChaptersOf(arc);
                for (int i = 0; i < chapters.Count; i++)
                {
                    int next = campaign.NextChapter(chapters[i]);
                    Assert.That(next, Is.EqualTo(i + 1 < chapters.Count ? chapters[i + 1] : -1), $"after {campaign.Scenario(chapters[i]).Title}");
                }
            }
            foreach (int map in campaign.Skirmishes)
            {
                Assert.That(campaign.NextChapter(map), Is.EqualTo(-1), "a skirmish map leads on to a chapter");
            }
        }

        [Test]
        public void EveryKindOfVictoryAndLossIsUsed()
        {
            HeroesCampaign campaign = Campaign();
            var victories = new HashSet<VictoryKind>();
            var losses = new HashSet<LossKind>();
            foreach (int index in campaign.Chapters)
            {
                ScenarioRules rules = campaign.Scenario(index).Map.rules;
                victories.Add(rules.victory);
                losses.Add(rules.loss);
            }
            foreach (VictoryKind kind in Enum.GetValues(typeof(VictoryKind)))
            {
                Assert.That(victories, Contains.Item(kind), $"no chapter is won by {kind}");
            }
            foreach (LossKind kind in Enum.GetValues(typeof(LossKind)))
            {
                Assert.That(losses, Contains.Item(kind), $"no chapter is lost by {kind}");
            }
        }

        [Test]
        public void EveryChapterTellsItsStory()
        {
            HeroesCampaign campaign = Campaign();
            foreach (int index in campaign.Chapters)
            {
                HeroesLevel level = campaign.Scenario(index);
                Assert.That(level.Intro, Is.Not.Empty, $"{level.Title} has no intro");
                Assert.That(level.Outro, Is.Not.Empty, $"{level.Title} has no outro");
                Assert.That(level.Goal, Is.Not.Empty, $"{level.Title} has no goal");
                Assert.That(level.ThreeStarDays, Is.LessThan(level.TwoStarDays), $"{level.Title}: three stars are not quicker than two");
                ScenarioRules rules = level.Map.rules;
                // A chapter lost in a way of its own says how; one lost only by losing everything says nothing.
                Assert.That(string.IsNullOrEmpty(level.Loss), Is.EqualTo(rules.loss == LossKind.LoseAll),
                    $"{level.Title}: the line on how it is lost does not match its rules");
                if (rules.loss == LossKind.TimeLimit)
                {
                    Assert.That(rules.dayLimit, Is.GreaterThan(level.TwoStarDays), $"{level.Title}: the stars come after the day limit");
                }
                if (rules.victory == VictoryKind.Survive)
                {
                    Assert.That(rules.victoryValue, Is.GreaterThan(1));
                    Assert.That(level.TwoStarDays, Is.GreaterThanOrEqualTo(rules.victoryValue), $"{level.Title}: holding out gives one star only");
                }
            }
        }

        [Test]
        public void TheSaltMarchesAskForTheCovensTown()
        {
            HeroesCampaign campaign = Campaign();
            HeroesLevel level = campaign.Scenario(1);
            Assert.That(level.Title, Is.EqualTo("The Salt Marches"));
            GameState state = MapGenerator.Generate(level.Map.Clone());
            TownState target = state.Town(state.rules.victoryValue);
            Assert.That(target, Is.Not.Null);
            Assert.That(target.owner, Is.EqualTo(1), "the town to take is not the coven's");
        }

        // ------------------------------------------------------------------ the maps

        [Test]
        public void EveryChapterLaysOutAPlayableMapWithItsGoalInReach()
        {
            HeroesCampaign campaign = Campaign();
            foreach (int index in campaign.Chapters)
            {
                HeroesLevel level = campaign.Scenario(index);
                MapSpec spec = level.Map.Clone();
                GameState state = MapGenerator.Generate(spec);
                string name = level.Title;
                Assert.That(state.map.grid.columns, Is.EqualTo(spec.columns), name);
                Assert.That(state.players.Count, Is.EqualTo(spec.players.Count), name);
                PlayerState me = state.players.Find(player => player.human);
                Assert.That(me, Is.Not.Null, $"{name} has nobody to play it");
                foreach (PlayerState player in state.players)
                {
                    Assert.That(player.towns.Count, Is.GreaterThanOrEqualTo(1), $"{player.name} has no town on {name}");
                    Assert.That(player.heroes.Count, Is.EqualTo(1), $"{player.name} has no hero on {name}");
                }
                Assert.That(state.players.Exists(player => player.team != me.team), Is.True, $"{name} has no enemy");

                bool[] reached = Walk(state, state.Hero(me.heroes[0]).cell);
                foreach (TownState town in state.towns)
                {
                    Assert.That(reached[town.cell], Is.True, $"no way to {town.name} on {name}");
                }
                var taken = new Dictionary<int, int>();
                foreach (MapObject obj in state.objects)
                {
                    foreach (int cell in obj.footprint)
                    {
                        Assert.That(taken.ContainsKey(cell), Is.False, $"two things stand on cell {cell} of {name}");
                        taken[cell] = obj.id;
                        Assert.That(state.map.occupant[cell], Is.EqualTo(obj.id), $"cell {cell} of {name} does not know what stands on it");
                    }
                }
                Goal(state, reached, me, level);
            }
        }

        /// <summary>What the chapter asks for is on the map, someone else's, and within reach of the person's hero.</summary>
        private static void Goal(GameState state, bool[] reached, PlayerState me, HeroesLevel level)
        {
            string name = level.Title;
            ScenarioRules rules = state.rules;
            switch (rules.victory)
            {
                case VictoryKind.CaptureTown:
                    TownState town = state.Town(rules.victoryValue);
                    Assert.That(town, Is.Not.Null, $"{name}: no town to take");
                    Assert.That(town.owner, Is.Not.EqualTo(me.index), $"{name}: the town to take is the person's own");
                    Assert.That(reached[town.cell], Is.True, $"{name}: the town to take is out of reach");
                    break;
                case VictoryKind.DefeatMonster:
                    MapObject beast = state.objects.Find(obj => obj.kind == ObjectKind.Monster && obj.tag == rules.victoryTag);
                    Assert.That(beast, Is.Not.Null, $"{name}: no monster to slay");
                    Assert.That(NextToReach(state, reached, beast.cell), Is.True, $"{name}: the monster to slay is out of reach");
                    break;
                case VictoryKind.FindArtifact:
                    MapObject relic = state.objects.Find(obj => (obj.kind == ObjectKind.Artifact || obj.kind == ObjectKind.Treasure) && obj.subtype == rules.victoryValue);
                    Assert.That(relic, Is.Not.Null, $"{name}: no artifact to find");
                    Assert.That(NextToReach(state, reached, relic.cell), Is.True, $"{name}: the artifact is out of reach");
                    break;
                case VictoryKind.GatherGold:
                    Assert.That(rules.victoryValue, Is.GreaterThan(me.resources.Gold), $"{name}: the gold is there from the start");
                    break;
                case VictoryKind.Survive:
                    Assert.That(rules.victoryValue, Is.GreaterThan(state.day), name);
                    break;
                case VictoryKind.DefeatHero:
                    HeroState foe = state.Hero(rules.victoryValue);
                    Assert.That(foe, Is.Not.Null, $"{name}: no hero to beat");
                    Assert.That(foe.alive, Is.True);
                    Assert.That(state.Player(foe.owner).team, Is.Not.EqualTo(me.team), $"{name}: the hero to beat is a friend");
                    Assert.That(reached[foe.cell], Is.True, $"{name}: the hero to beat is out of reach");
                    break;
                case VictoryKind.HoldTowns:
                    Assert.That(state.towns.Count, Is.GreaterThanOrEqualTo(rules.victoryValue), $"{name}: there are too few towns to hold");
                    Assert.That(me.towns.Count, Is.LessThan(rules.victoryValue), $"{name}: the towns are held from the start");
                    break;
            }
            switch (rules.loss)
            {
                case LossKind.LoseHero:
                    Assert.That(rules.lossValue, Is.EqualTo(me.heroes[0]), $"{name}: the hero to keep is not the person's");
                    break;
                case LossKind.LoseTown:
                    Assert.That(rules.lossValue, Is.EqualTo(me.towns[0]), $"{name}: the town to keep is not the person's");
                    break;
            }
        }

        private static bool NextToReach(GameState state, bool[] reached, int cell)
        {
            foreach (int next in state.map.grid.Neighbors(cell))
            {
                if (reached[next])
                {
                    return true;
                }
            }
            return false;
        }

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

        [Test]
        public void TheComputerPlaysTheFirstDaysOfEveryNewChapter()
        {
            HeroesCampaign campaign = Campaign();
            foreach (int index in campaign.Chapters)
            {
                if (campaign.ArcOf(index) == 0)
                {
                    continue;
                }
                HeroesLevel level = campaign.Scenario(index);
                MapSpec spec = level.Map.Clone();
                var game = new HeroesGame(MapGenerator.Generate(spec), new SeededRandom(spec.seed ^ 0x5bd1e995u));
                game.Begin();
                var brain = new AdventureAI();
                int refused = 0;
                for (int step = 0; step < 60000 && !game.IsOver && game.State.day <= 10; step++)
                {
                    int who = game.WaitingPlayer;
                    GameCommand command = game.State.pending.Count > 0 && !game.InBattle ? GameCommand.Choose(who, 0)
                        : game.InBattle ? BattleAI.Next(game) : brain.Next(game, who);
                    command ??= game.InBattle ? GameCommand.BattleDefend(who, game.Battle.current) : GameCommand.EndTurn(who);
                    if (game.Apply(command))
                    {
                        refused = 0;
                    }
                    else
                    {
                        brain.Refused(command);
                        refused++;
                        Assert.That(refused, Is.LessThan(20), $"{level.Title}: the rules keep refusing {command.kind} on day {game.State.day}");
                    }
                    game.TakeEvents();
                }
                Assert.That(game.IsOver || game.State.day > 10, Is.True, $"{level.Title} got stuck on day {game.State.day}");
            }
        }
    }
}
