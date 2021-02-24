using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// The Monopoly class is the main Model class which holds the game state
/// </summary>
public class Monopoly : MonoBehaviour
{
    public MonopolySettings Settings => settings;
    public MonopolyController Controller => controller;
    public Dictionary<PlayerId, Player> Players => new Dictionary<PlayerId, Player>(players);
    public List<Tile> Tiles => new List<Tile>(tiles);
    public PlayerId CurrentPlayer { get; private set; }

    public delegate void PlayerDelegate(Player player);
    public event PlayerDelegate PlayerMovedEvent;
    public event PlayerDelegate NowPlayingEvent;

    [SerializeField] private MonopolySettings settings;
    [SerializeField] private MonopolyController controller;
    [SerializeField] private List<Tile> tiles;
    [SerializeField] private List<Player> preexistingPlayers = new List<Player>();

    private Dictionary<PlayerId, Player> players = new Dictionary<PlayerId, Player>();
    private List<PlayerId> playerIds = new List<PlayerId>();


    void Awake()
    {
        // Players can be initialized in run time or be prepared and linked in advance
        bool initializeNewPlayers = preexistingPlayers.Count < settings.MaxPlayers;

        // Setup players
        for (int i = 0; i < settings.MaxPlayers; i++)
        {
            Player player;
            if (initializeNewPlayers)
            {
                player = Instantiate(settings.PlayerPrefab);
            }
            else
            {
                player = preexistingPlayers[i];
            }
            PlayerId playerId = (PlayerId)i + 1;
            player.InitPlayer(this, playerId);
            players[playerId] = player;
            playerIds.Add(playerId);
        }
        CurrentPlayer = playerIds[0];
        
        // Assert loaded configuation
        var board = settings.Board;
        if (!board.Check(out string boardError))
        {
            throw new ArgumentException($"Monopoly board error: {boardError}.");
        }

        var tileLayout = board.TileLayout;
        if (tileLayout.Count != tiles.Count)
        {
            throw new ArgumentException($"Tile layout amount of tiles {tileLayout.Count} is different from actual tiles available {tiles.Count}");
        }

        List<Asset> assets = board.Assets;
        List<Reward> rewards = board.Rewards;
        foreach (var reward in rewards)
        {
            if (!reward.Assert(out string error))
            {
                throw new ArgumentException($"Monopoly reward error: {error}.");
            }
        }

        // Setup tiles
        for (int i = 0; i < tileLayout.Count; i++)
        {
            var tileType = tileLayout[i];
            Tile tile = tiles[i];
            tile.SetupTile(i);
            switch (tile)
            {
                case AssetTile assetTile:
                    assetTile.InitAsset(assets[0]);
                    assets.RemoveAt(0);
                    break;
                case RewardTile rewardTile:
                    if (tileType == TileBehaviour.Start)
                    {
                        rewardTile.InitReward(rewards.GetRange(0, 1));
                        rewards.RemoveAt(0);
                    }
                    else
                    {
                        rewardTile.InitReward(rewards);
                    }
                    break;
            }
        }
    }


    public void NextTurn()
    {
        int currentPlayerIndex = playerIds.IndexOf(CurrentPlayer) + 1;
        if (currentPlayerIndex >= playerIds.Count)
        {
            currentPlayerIndex = 0;
        }
        CurrentPlayer = playerIds[currentPlayerIndex];
        NowPlayingEvent(GetPlayer(CurrentPlayer));
    }


    public void MovePlayer(Player player)
    {
        PlayerMovedEvent(player);
    }


    public void PlayerBankrupt(Player player)
    {
        players.Remove(player.PlayerId);
        playerIds.Remove(player.PlayerId);
        if (players.Count == 1)
        {
            GameOver();
        }
        player.gameObject.SetActive(false);
    }


    public Player GetPlayer(PlayerId playerId)
    {
        return players[playerId];
    }


    public Tile GetTile(int tileId)
    {
        return tiles[tileId];
    }


    private void GameOver()
    {
        Player winner = players.Values.First();
        controller.PlayerDialog($"{winner.name} is the last player remaining", $"{winner.name} Won");
    }
}

public enum PlayerId { None, Player1, Player2, Player3, Player4 }

