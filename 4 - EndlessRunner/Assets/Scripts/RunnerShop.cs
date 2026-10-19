using Gamebox;
using Gamebox.Economy;
using UnityEngine;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// What the Endless Runner sells through the cross-game shop: two perks the account keeps. A Head Start (for money)
    /// puts a shield on the runner at the start of every run, and the Coin Magnet (for Tokens) draws the coins from
    /// farther away, with a magnet picked up or without one. Both apply to the device's own runner only (the runner of
    /// the scene; the companions of a local race and the ghosts of an online race run as they are) and are read where
    /// they apply, in <see cref="RunnerGameManager"/>, so nothing is delivered here.
    /// </summary>
    public sealed class RunnerShop : GameShopDefinition
    {
        public const string HeadStartId = "RUNNER_HEAD_START";
        public const int HeadStartCents = 100;
        public const string HeadStartProductId = "com.skinnerboxes.runner.headstart";
        public const string CoinMagnetId = "RUNNER_COIN_MAGNET";
        public const int CoinMagnetTokens = 150;

        /// <summary>How much farther a magnet picked up reaches with the Coin Magnet.</summary>
        private const float MagnetReach = 1.5f;
        /// <summary>How far the runner draws coins without a magnet with the Coin Magnet, as a fraction of a magnet's reach.</summary>
        private const float PassiveReach = 0.4f;

        public override GameType Game => GameType.EndlessRunner;

        public override void DefineCloud(CloudCatalog catalog)
        {
            catalog.AddForMoney(Game, HeadStartId, RunnerText.Key("Head Start"), RunnerText.Key("Every run starts with a shield"), HeadStartCents, HeadStartProductId);
            catalog.AddForTokens(Game, CoinMagnetId, RunnerText.Key("Coin Magnet"), RunnerText.Key("Coins come to you from farther away"), CoinMagnetTokens);
        }

        /// <summary>The power-up icons of the active theme: the shield for the Head Start, the magnet for the Coin Magnet.</summary>
        public override Sprite IconOf(CloudItem item)
        {
            RunnerGameTheme look = RunnerGameTheme.Active;
            if (item == null || look == null)
            {
                return null;
            }
            switch (item.Id)
            {
                case HeadStartId: return look.Sprites.shield;
                case CoinMagnetId: return look.Sprites.magnet;
                default: return null;
            }
        }

        /// <summary>What a magnet's reach is multiplied by while the player <paramref name="owned"/> the Coin Magnet: 1 otherwise.</summary>
        public static float MagnetFactor(bool owned)
        {
            return owned ? MagnetReach : 1f;
        }

        /// <summary>
        /// The reach of the runner without a magnet, as a fraction of a magnet's, while the player <paramref name="owned"/>
        /// the Coin Magnet: 0 otherwise (the coins stay where they are until a magnet is picked up).
        /// </summary>
        public static float PassiveMagnetFactor(bool owned)
        {
            return owned ? PassiveReach : 0f;
        }
    }
}
