using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerId PlayerId { get; private set; }
    public int Location { get; private set; } = 0;
    public int LastLocation { get; private set; } = 0;
    public int Money
    {
        get { return money; }
        private set { money = value; MoneyChangedEvent(money); }
    }
    public HashSet<Asset> Assets => new HashSet<Asset>(assets);

    public delegate void MoneyChanged(int money);
    public event MoneyChanged MoneyChangedEvent;


    private Monopoly monopoly;
    private MonopolyController controller;
    private HashSet<Asset> assets = new HashSet<Asset>();
    private int tilesAmount = -1;
    private int money;


    public void InitPlayer(Monopoly monopoly, PlayerId playerId)
    {
        this.monopoly = monopoly;
        controller = monopoly.Controller;
        var settings = monopoly.Settings;
        PlayerId = playerId;
        Location = settings.InitialPlayerLocation;
        Money = settings.InitialPlayerMoney;
        tilesAmount = settings.Board.TileLayout.Count;
    }


    public bool CanBuyAsset(Asset asset)
    {
        return Money >= asset.Price;
    }


    public void BuyAsset(Asset asset)
    {
        if (asset.OwningPlayer != null)
        {
            throw new ArgumentException($"Asset is already owned by a player.");
        }
        Money -= asset.Price;
        assets.Add(asset);
        asset.OwningPlayer = this;
        controller.PlayerDialog($"You({PlayerId}) have bought {asset.name}", $"Pay {asset.Price}$");
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
            IDeclareBankruptcy();//!!!!
        }
        else
        {
            Money -= asset.Fine;
            asset.OwningPlayer.ReceiveFine(asset);
            controller.PlayerDialog($"You({PlayerId}) landed on {asset.OwningPlayer.PlayerId}'s property", $"Pay {asset.Fine}$");
        }
    }


    public void ReceiveFine(Asset asset)
    {
        Money += asset.Fine;
    }


    public void Move(int move)
    {
        LastLocation = Location;
        Location = (Location + move) % tilesAmount;
        monopoly.MovePlayer(this);
    }


    public void IDeclareBankruptcy()//!!!!! 
    {
        foreach (var asset in assets)
        {
            asset.OwningPlayer = null;
        }
        assets.Clear();
        controller.PlayerDialog($"You({PlayerId}) went bankrupt", $"Pay Everything");
        monopoly.PlayerBankrupt(this);
    }


    public void Award(Reward reward)
    {
        int showMeTheMoney = reward.CalculateReward();
        Money += showMeTheMoney;
        controller.PlayerDialog($"{PlayerId} received {showMeTheMoney}$ reward", $"Gain {showMeTheMoney}$");
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
