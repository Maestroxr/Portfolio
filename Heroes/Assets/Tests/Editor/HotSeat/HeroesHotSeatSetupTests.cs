using System.Collections.Generic;
using Gamebox;
using NUnit.Framework;
using UnityEditor;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// The setup of a hot seat as the shared local play screen shows it (<see cref="HeroesHotSeatRules"/>): the skirmish
    /// maps of the campaign, each seating exactly as many realms as it has in their colours and factions, two people at
    /// least, the hand-over screen on, and the map of a match made from the setup as the setup says. They read the
    /// campaign asset, so they run in the editor only (dotnet test leaves this folder out).
    /// </summary>
    public class HeroesHotSeatSetupTests
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

        private static LocalMatch Match(HeroesHotSeatRules rules, int map)
        {
            var match = new LocalMatch(GameType.Heroes, rules);
            match.SetOption(HeroesHotSeatRules.MapOption, map);
            rules.OptionsChanged(match);
            return match;
        }

        [Test]
        public void TheSetupIsAHotSeatOnTheSkirmishMaps()
        {
            HeroesCampaign campaign = Campaign();
            var rules = new HeroesHotSeatRules(campaign);

            Assert.That(rules.Title, Is.EqualTo("Hot Seat"));
            Assert.That(rules.Simultaneous, Is.False);
            Assert.That(rules.AllowComputers, Is.True);
            Assert.That(rules.MinHumans, Is.EqualTo(2));
            Assert.That(rules.OffersHandOver && rules.HandOverByDefault, Is.True);
            Assert.That(rules.ComputerLevels, Is.EqualTo(new[] { "Easy", "Normal", "Hard" }), "the computer's levels are the AI's 0, 1 and 2");
            List<int> maps = campaign.Skirmishes;
            Assert.That(rules.Maps, Is.EqualTo(maps));
            LocalOption map = rules.FindOption(HeroesHotSeatRules.MapOption);
            Assert.That(map.Values.Length, Is.EqualTo(maps.Count));
            for (int i = 0; i < maps.Count; i++)
            {
                Assert.That(map.Values[i], Is.EqualTo(campaign.Scenario(maps[i]).Title));
            }
            Assert.That(rules.FindOption(HeroesHotSeatRules.SizeOption).Index, Is.EqualTo(1), "a medium map, as the settings start");
            Assert.That(rules.FindOption(HeroesHotSeatRules.BattlesOption).Index, Is.EqualTo((int)BattleStyle.Battlefield));
        }

        [Test]
        public void EveryMapSeatsExactlyItsRealmsInTheirColoursAndFactions()
        {
            HeroesCampaign campaign = Campaign();
            var rules = new HeroesHotSeatRules(campaign);
            for (int option = 0; option < rules.Maps.Count; option++)
            {
                HeroesLevel level = campaign.Scenario(rules.Maps[option]);
                LocalMatch match = Match(rules, option);
                int seats = level.Map.players.Count;
                Assert.That(match.Count, Is.EqualTo(seats), level.Title);
                Assert.That((rules.MinPlayers, rules.MaxPlayers), Is.EqualTo((seats, seats)), $"{level.Title}: no other number of players");
                Assert.That(rules.LevelOf(match), Is.EqualTo(rules.Maps[option]));
                for (int seat = 0; seat < seats; seat++)
                {
                    Assert.That(rules.SeatDetail(match, seat), Is.EqualTo(Land.FactionName(level.Map.players[seat].faction)));
                    Assert.That(match[seat].Color, Is.EqualTo(HeroesArt.PlayerColor(seat)), "the colour the realm flies on the map");
                }
            }
        }

        [Test]
        public void ARememberedSetupComesBackOnItsOwnMap()
        {
            var rules = new HeroesHotSeatRules(Campaign());
            int last = rules.Maps.Count - 1;
            LocalMatch match = Match(rules, last);
            match[0].Name = "Alice";
            match[1].Kind = SeatKind.Computer;
            match[1].Level = 2;
            match.HandOver = false;
            string saved = match.Save();

            rules.Refresh();
            var again = new LocalMatch(GameType.Heroes, rules);
            again.Load(saved);
            Assert.That(again.Option(HeroesHotSeatRules.MapOption), Is.EqualTo(last));
            Assert.That(again.Count, Is.EqualTo(match.Count));
            Assert.That(again[0].Name, Is.EqualTo("Alice"));
            Assert.That(again[1].IsHuman, Is.False);
            Assert.That(again[1].Level, Is.EqualTo(2));
            Assert.That(again.HandOver, Is.False);
        }

        [Test]
        public void TheMapOfAMatchIsPlayedAsTheSetupSays()
        {
            HeroesCampaign campaign = Campaign();
            var rules = new HeroesHotSeatRules(campaign);
            int option = -1;
            for (int i = 0; i < rules.Maps.Count && option < 0; i++)
            {
                option = campaign.Scenario(rules.Maps[i]).Map.players.Count >= 3 ? i : -1;
            }
            Assert.That(option, Is.GreaterThanOrEqualTo(0), "a map for three");
            LocalMatch match = Match(rules, option);
            match[0].Name = "Alice";
            match[1].Name = "Bob";
            match[2].Kind = SeatKind.Computer;
            match[2].Level = 0;
            Assert.That(match.Problem(), Is.Null);

            List<HotSeatPlayer> players = HeroesHotSeatRules.Players(match);
            MapSpec spec = HotSeat.Spec(campaign.Scenario(rules.LevelOf(match)).Map, players, 2, 2, 2);
            Assert.That(spec.players.ConvertAll(p => p.human).GetRange(0, 3), Is.EqualTo(new[] { true, true, false }));
            Assert.That(spec.players[0].name, Is.EqualTo("Alice"));
            Assert.That(spec.players[1].name, Is.EqualTo("Bob"));
            Assert.That(spec.players[2].aiLevel, Is.EqualTo(0));

            match[1].Kind = SeatKind.Computer;
            Assert.That(match.Problem(), Is.Not.Null, "a hot seat needs two people");
        }
    }
}
