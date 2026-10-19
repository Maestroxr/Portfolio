using Gamebox;
using Gamebox.Economy;
using Portfolio.Heroes.UI;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// What Heroes sells in the shared shop (<see cref="GameShopDefinition"/>): a purse of gold for money and veteran
    /// heroes for Tokens, both perks the account keeps. A game at this device honours them where it lays out its map
    /// (<see cref="HeroesGameManager.StartGame"/>, through <see cref="Outfit"/>): the seats people play start with
    /// <see cref="PurseGold"/> more gold and their heroes <see cref="VeteranLevels"/> level higher
    /// (<see cref="PlayerSpec.extraGold"/> and <see cref="PlayerSpec.extraLevels"/>, which the generator applies). An
    /// online game lays out the same map on every client, so the perks stay out of it.
    /// </summary>
    public sealed class HeroesShop : GameShopDefinition
    {
        public const string PurseId = "HEROES_PURSE";
        public const int PurseCents = 100;
        public const string PurseProductId = "com.skinnerboxes.heroes.purse";
        public const int PurseGold = 2000;

        public const string VeteransId = "HEROES_VETERANS";
        public const int VeteransTokens = 200;
        public const int VeteranLevels = 1;

        public override GameType Game => GameType.Heroes;

        public override void DefineCloud(CloudCatalog catalog)
        {
            catalog.AddForMoney(Game, PurseId, "Royal Purse", "+2,000 gold at the start of every map", PurseCents, PurseProductId);
            catalog.AddForTokens(Game, VeteransId, "Veterans", "Your heroes start a level higher", VeteransTokens);
        }

        /// <summary>The gold's icon for the purse, a tome's for the veterans (the game's icon before the art is loaded).</summary>
        public override Sprite IconOf(CloudItem item)
        {
            HeroesArt art = UIKit.Art;
            if (art == null || item == null)
            {
                return null;
            }
            switch (item.Id)
            {
                case PurseId: return art.Resource(ResourceKind.Gold);
                case VeteransId: return art.Artifact(ArtifactId.TomeOfWar);
                default: return null;
            }
        }

        /// <summary>The gold a seat starts with beyond the map's when the purse is <paramref name="owned"/>.</summary>
        public static int GoldBonus(bool owned)
        {
            return owned ? PurseGold : 0;
        }

        /// <summary>The levels a starting hero has beyond the first when the veterans are <paramref name="owned"/>.</summary>
        public static int LevelBonus(bool owned)
        {
            return owned ? VeteranLevels : 0;
        }

        /// <summary>
        /// Gives every seat a person plays in <paramref name="spec"/> what the perks owned bring, and nothing when they are
        /// not: the people of a game at this device all play on the account's device.
        /// </summary>
        public static void Outfit(MapSpec spec, bool purse, bool veterans)
        {
            if (spec == null)
            {
                return;
            }
            foreach (PlayerSpec seat in spec.players)
            {
                if (seat.human)
                {
                    seat.extraGold = GoldBonus(purse);
                    seat.extraLevels = LevelBonus(veterans);
                }
            }
        }
    }
}
