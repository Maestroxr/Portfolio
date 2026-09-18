using System;
using System.Collections.Generic;
using Gamebox;

namespace Portfolio.Monopoly
{
    /// <summary>A Monopoly player: money, board position and owned properties.</summary>
    public class MonopolyPlayer : PlayerBase
    {
        public PlayerId PlayerId { get; private set; }
        public int Location { get; private set; } = 0;
        public int LastLocation { get; private set; } = 0;
        public int Money
        {
            get { return money; }
            private set { money = value; MoneyChangedEvent?.Invoke(money); }
        }
        public HashSet<Asset> Assets => new HashSet<Asset>(assets);

        public delegate void MoneyChanged(int money);
        public event MoneyChanged MoneyChangedEvent;


        private MonopolyGameManager monopoly;
        private readonly HashSet<Asset> assets = new HashSet<Asset>();
        private int tilesAmount = -1;
        private int money;

        private MonopolyController Controller => monopoly != null ? monopoly.MonopolyController : null;


        public void InitPlayer(MonopolyGameManager monopoly, PlayerId playerId)
        {
            this.monopoly = monopoly;
            GameManager = monopoly;
            PlayerId = playerId;
            ResetPlayer(monopoly.MonopolySettings);
        }


        /// <summary>Puts the player back at the start with the starting money and no properties.</summary>
        public void ResetPlayer(MonopolySettings settings)
        {
            foreach (Asset asset in assets)
            {
                if (asset.OwningPlayer == this)
                {
                    asset.OwningPlayer = null;
                }
            }
            assets.Clear();
            Location = settings.InitialPlayerLocation;
            LastLocation = Location;
            tilesAmount = settings.Board != null ? settings.Board.TileLayout.Count : -1;
            Money = settings.InitialPlayerMoney;
        }


        /// <summary>Restores a saved state without triggering dialogs.</summary>
        public void Restore(int money, int location, int lastLocation)
        {
            Location = location;
            LastLocation = lastLocation;
            Money = money;
        }


        /// <summary>Takes ownership of <paramref name="asset"/> without paying for it (used when loading a game).</summary>
        public void ClaimAsset(Asset asset)
        {
            assets.Add(asset);
            asset.OwningPlayer = this;
        }


        public bool CanBuyAsset(Asset asset)
        {
            return Money >= asset.Price;
        }


        public void BuyAsset(Asset asset)
        {
            if (asset.OwningPlayer != null)
            {
                throw new ArgumentException("Asset is already owned by a player.");
            }
            Money -= asset.Price;
            assets.Add(asset);
            asset.OwningPlayer = this;
            Controller?.PlayerDialog($"You({PlayerId}) have bought {asset.name}", $"Pay {asset.Price}$");
        }


        public bool DoesOwnAsset(Asset asset)
        {
            return assets.Contains(asset);
        }


        public bool CanPayFine(Asset asset)
        {
            return Money >= asset.Fine;
        }


        public void PayFine(Asset asset)
        {
            if (!CanPayFine(asset))
            {
                IDeclareBankruptcy();
            }
            else
            {
                Money -= asset.Fine;
                asset.OwningPlayer.ReceiveFine(asset);
                Controller?.PlayerDialog($"You({PlayerId}) landed on {asset.OwningPlayer.PlayerId}'s property", $"Pay {asset.Fine}$");
            }
        }


        public void ReceiveFine(Asset asset)
        {
            Money += asset.Fine;
        }


        public void Move(int move)
        {
            if (tilesAmount <= 0)
            {
                return;
            }
            LastLocation = Location;
            Location = (Location + move) % tilesAmount;
            monopoly?.MovePlayer(this);
        }


        public void IDeclareBankruptcy()
        {
            foreach (var asset in assets)
            {
                asset.OwningPlayer = null;
            }
            assets.Clear();
            Controller?.PlayerDialog($"You({PlayerId}) went bankrupt", "Pay Everything");
            monopoly?.PlayerBankrupt(this);
        }


        public void Award(Reward reward)
        {
            int showMeTheMoney = reward.CalculateReward();
            Money += showMeTheMoney;
            Controller?.PlayerDialog($"{PlayerId} received {showMeTheMoney}$ reward", $"Gain {showMeTheMoney}$");
        }


        public int DistanceFromLastLocation()
        {
            if (LastLocation > Location)
            {
                return tilesAmount - LastLocation + Location + 1;
            }
            else
            {
                return Location - LastLocation;
            }
        }
    }
}
