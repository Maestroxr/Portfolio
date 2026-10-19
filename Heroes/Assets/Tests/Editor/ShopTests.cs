using Gamebox;
using Gamebox.Economy;
using NUnit.Framework;

namespace Portfolio.Heroes.Tests
{
    /// <summary>
    /// What Heroes sells in the shared shop, and what the perks do to a map laid out at this device: the purse's gold
    /// in the coffers of the seats people play, the veterans' level on their starting heroes, and nothing for the
    /// computer's seats.
    /// </summary>
    public class ShopTests
    {
        private static MapSpec Spec(uint seed)
        {
            var spec = new MapSpec { seed = seed, columns = 30, rows = 34, treasure = 2, monsters = 2 };
            spec.players.Add(new PlayerSpec { faction = Faction.Castle, human = true, team = 0, name = "You" });
            spec.players.Add(new PlayerSpec { faction = Faction.Necropolis, human = false, team = 1, aiLevel = 1, name = "Rival" });
            return spec;
        }

        private static int Stats(HeroState hero)
        {
            return hero.attack + hero.defense + hero.power + hero.knowledge;
        }

        [Test]
        public void SellsThePurseForMoneyAndTheVeteransForTokens()
        {
            var catalog = new CloudCatalog();
            new HeroesShop().DefineCloud(catalog);
            Assert.That(catalog.Count, Is.EqualTo(2));

            CloudItem purse = catalog[HeroesShop.PurseId];
            Assert.That(purse, Is.Not.Null);
            Assert.That(purse.Id, Is.EqualTo("HEROES_PURSE"));
            Assert.That(purse.Game, Is.EqualTo(GameType.Heroes));
            Assert.That(purse.Kind, Is.EqualTo(ItemKind.Owned));
            Assert.That(purse.Cents, Is.EqualTo(100));
            Assert.That(purse.Tokens, Is.EqualTo(0));
            Assert.That(purse.AppleProductId, Is.EqualTo("com.skinnerboxes.heroes.purse"));
            Assert.That(purse.GooglePlayProductId, Is.EqualTo("com.skinnerboxes.heroes.purse"));

            CloudItem veterans = catalog[HeroesShop.VeteransId];
            Assert.That(veterans, Is.Not.Null);
            Assert.That(veterans.Id, Is.EqualTo("HEROES_VETERANS"));
            Assert.That(veterans.Game, Is.EqualTo(GameType.Heroes));
            Assert.That(veterans.Kind, Is.EqualTo(ItemKind.Owned));
            Assert.That(veterans.Tokens, Is.EqualTo(200));
            Assert.That(veterans.Cents, Is.EqualTo(0));

            Assert.That(catalog.ForMoney(), Is.EqualTo(new[] { purse }));
            Assert.That(catalog.ForTokens(), Is.EqualTo(new[] { veterans }));
            Assert.That(new HeroesShop().Game, Is.EqualTo(GameType.Heroes));
        }

        [Test]
        public void TheBonusesAreWorthNothingUnlessOwned()
        {
            Assert.That(HeroesShop.GoldBonus(false), Is.EqualTo(0));
            Assert.That(HeroesShop.GoldBonus(true), Is.EqualTo(2000));
            Assert.That(HeroesShop.LevelBonus(false), Is.EqualTo(0));
            Assert.That(HeroesShop.LevelBonus(true), Is.EqualTo(1));
        }

        [Test]
        public void OutfitGivesThePerksToTheSeatsPeoplePlayOnly()
        {
            MapSpec spec = Spec(1);
            HeroesShop.Outfit(spec, true, true);
            Assert.That(spec.players[0].extraGold, Is.EqualTo(HeroesShop.PurseGold));
            Assert.That(spec.players[0].extraLevels, Is.EqualTo(HeroesShop.VeteranLevels));
            Assert.That(spec.players[1].extraGold, Is.EqualTo(0));
            Assert.That(spec.players[1].extraLevels, Is.EqualTo(0));

            // Not owned (any more): the seats go back to the map's own.
            HeroesShop.Outfit(spec, false, false);
            Assert.That(spec.players[0].extraGold, Is.EqualTo(0));
            Assert.That(spec.players[0].extraLevels, Is.EqualTo(0));

            // A clone keeps what the seats bring along.
            HeroesShop.Outfit(spec, true, true);
            MapSpec copy = spec.Clone();
            Assert.That(copy.players[0].extraGold, Is.EqualTo(HeroesShop.PurseGold));
            Assert.That(copy.players[0].extraLevels, Is.EqualTo(HeroesShop.VeteranLevels));
        }

        [Test]
        public void ThePurseFillsTheCoffersOfItsSeatAlone()
        {
            GameState plain = MapGenerator.Generate(Spec(31));
            MapSpec richer = Spec(31);
            HeroesShop.Outfit(richer, true, false);
            GameState state = MapGenerator.Generate(richer);

            Assert.That(state.players[0].resources.values[(int)ResourceKind.Gold],
                Is.EqualTo(plain.players[0].resources.values[(int)ResourceKind.Gold] + HeroesShop.PurseGold));
            Assert.That(state.players[1].resources.values[(int)ResourceKind.Gold],
                Is.EqualTo(plain.players[1].resources.values[(int)ResourceKind.Gold]));
            // The gold alone: the heroes are as green as ever.
            Assert.That(state.Hero(state.players[0].heroes[0]).level, Is.EqualTo(1));
            Assert.That(state.pending, Is.Empty);
        }

        [Test]
        public void TheVeteransStartALevelHigherWithWhatALevelBrings()
        {
            GameState plain = MapGenerator.Generate(Spec(32));
            MapSpec seasoned = Spec(32);
            HeroesShop.Outfit(seasoned, false, true);
            GameState state = MapGenerator.Generate(seasoned);

            HeroState green = plain.Hero(plain.players[0].heroes[0]);
            HeroState veteran = state.Hero(state.players[0].heroes[0]);
            // The same hero (chosen before the level is given), a level up through the rules: a stat and the experience of the level.
            Assert.That(veteran.def, Is.EqualTo(green.def));
            Assert.That(green.level, Is.EqualTo(1));
            Assert.That(veteran.level, Is.EqualTo(2));
            Assert.That(veteran.experience, Is.EqualTo(HeroData.ExperienceFor(2)));
            Assert.That(Stats(veteran), Is.EqualTo(Stats(green) + 1));
            // The skill a level brings is the person's to pick, as soon as the game begins.
            Assert.That(state.pending.Count, Is.EqualTo(1));
            Assert.That(state.pending[0].kind, Is.EqualTo(ChoiceKind.LevelUp));
            Assert.That(state.pending[0].player, Is.EqualTo(0));
            Assert.That(state.pending[0].hero, Is.EqualTo(veteran.id));
            // The computer's hero is untouched, and so are the coffers.
            Assert.That(state.Hero(state.players[1].heroes[0]).level, Is.EqualTo(1));
            Assert.That(state.players[0].resources.values[(int)ResourceKind.Gold],
                Is.EqualTo(plain.players[0].resources.values[(int)ResourceKind.Gold]));
        }
    }
}
