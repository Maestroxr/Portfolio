using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.Monopoly
{
    public enum PlayerId { None, Player1, Player2, Player3, Player4 }

    /// <summary>
    /// Monopoly game module. Holds the board, the players and whose turn it is; the dice rolling and dialogs go
    /// through <see cref="MonopolyController"/>, the menu and state flow through <see cref="BaseGameManager"/>.
    /// </summary>
    public class MonopolyGameManager : BaseGameManager
    {
        public MonopolySettings MonopolySettings => Settings as MonopolySettings ?? settings;
        public MonopolyController MonopolyController => controller;
        public MonopolyUI MonopolyUI => ui;
        public Dictionary<PlayerId, MonopolyPlayer> PlayersById => new Dictionary<PlayerId, MonopolyPlayer>(players);
        public List<Tile> Tiles => new List<Tile>(tiles);
        public PlayerId CurrentPlayer { get; private set; }

        public delegate void PlayerDelegate(MonopolyPlayer player);
        public event PlayerDelegate PlayerMovedEvent;
        public event PlayerDelegate NowPlayingEvent;
        /// <summary>Raised for every player when a new game starts or a saved one is loaded.</summary>
        public event PlayerDelegate PlayerResetEvent;

        [SerializeField] private MonopolySettings settings;
        [SerializeField] private MonopolyController controller;
        [SerializeField] private MonopolyUI ui;
        [SerializeField] private Campaign campaign;
        [SerializeField] private List<Tile> tiles = new List<Tile>();

        private readonly Dictionary<PlayerId, MonopolyPlayer> players = new Dictionary<PlayerId, MonopolyPlayer>();
        private readonly List<PlayerId> playerIds = new List<PlayerId>();
        private readonly List<MonopolyPlayer> allPlayers = new List<MonopolyPlayer>();
        private MonopolySettings customSettings;
        private MonopolySettings activeSettings;
        private bool boardReady;

        private const string SavePrefix = "Monopoly.";

        public override IGameController Controller => controller;
        public override IGameUI UI => ui;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => settings;
        public override IGameSettings CustomSettings => customSettings != null ? customSettings : (customSettings = CreateCustomSettings());

        public override IGameSettings Settings
        {
            get => activeSettings != null ? activeSettings : settings;
            set => activeSettings = value as MonopolySettings;
        }

        protected override bool UsesTimer => false;

        public IReadOnlyList<MonopolyPlayer> AllPlayers => allPlayers;


        private MonopolySettings CreateCustomSettings()
        {
            MonopolySettings copy = settings != null ? Instantiate(settings) : ScriptableObject.CreateInstance<MonopolySettings>();
            copy.name = $"{(settings != null ? settings.name : "MonopolySettings")} (custom)";
            return copy;
        }


        protected override void Awake()
        {
            base.Awake();
            if (settings == null)
            {
                Debug.LogError("Monopoly has no settings assigned.", this);
                return;
            }
            SetupPlayers();
            SetupBoard();
        }


        private void SetupPlayers()
        {
            // Players can be initialized in run time or be prepared and linked in advance
            List<MonopolyPlayer> existing = PlayerList.OfType<MonopolyPlayer>().ToList();
            bool initializeNewPlayers = existing.Count < settings.MaxPlayers;

            for (int i = 0; i < settings.MaxPlayers; i++)
            {
                MonopolyPlayer player;
                if (initializeNewPlayers)
                {
                    player = Instantiate(settings.PlayerPrefab);
                    RegisterPlayer(player);
                }
                else
                {
                    player = existing[i];
                }
                PlayerId playerId = (PlayerId)i + 1;
                player.InitPlayer(this, playerId);
                allPlayers.Add(player);
                players[playerId] = player;
                playerIds.Add(playerId);
            }
            CurrentPlayer = playerIds.Count > 0 ? playerIds[0] : PlayerId.None;
        }


        private void SetupBoard()
        {
            var board = settings.Board;
            if (board == null)
            {
                Debug.LogError("Monopoly board error: no board assigned in the settings.", this);
                return;
            }
            if (!board.Check(out string boardError))
            {
                Debug.LogError($"Monopoly board error: {boardError}.", this);
                return;
            }

            var tileLayout = board.TileLayout;
            if (tileLayout.Count != tiles.Count)
            {
                Debug.LogError($"Tile layout amount of tiles {tileLayout.Count} is different from actual tiles available {tiles.Count}", this);
                return;
            }

            List<Asset> assets = board.Assets;
            List<Reward> rewards = board.Rewards;
            foreach (var reward in rewards)
            {
                if (!reward.Assert(out string error))
                {
                    Debug.LogError($"Monopoly reward error: {error}.", this);
                    return;
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
            boardReady = true;
        }


        public override void StartGame()
        {
            if (!boardReady)
            {
                UI?.UpdateError("The Monopoly board is not set up; check the settings and the tiles.");
                return;
            }
            ResetBoard();
            controller?.ResetDialog();
            TransitionState(BaseGameState.Running);
            NowPlayingEvent?.Invoke(GetPlayer(CurrentPlayer));
        }


        /// <summary>Frees every property, resets every player and puts them back on the start tile.</summary>
        private void ResetBoard()
        {
            foreach (Asset asset in settings.Board.Assets)
            {
                asset.OwningPlayer = null;
            }
            players.Clear();
            playerIds.Clear();
            foreach (MonopolyPlayer player in allPlayers)
            {
                player.gameObject.SetActive(true);
                player.ResetPlayer(MonopolySettings);
                players[player.PlayerId] = player;
                playerIds.Add(player.PlayerId);
            }
            foreach (Tile tile in tiles)
            {
                tile.VisitingPlayers.Clear();
            }
            CurrentPlayer = playerIds.Count > 0 ? playerIds[0] : PlayerId.None;
            foreach (MonopolyPlayer player in allPlayers)
            {
                PlayerResetEvent?.Invoke(player);
            }
            ResetScore();
        }


        public override void LoadLevel(int level)
        {
            LevelIndex = level;
            CurrentLevel = LevelData.Create(level);
            UpdateLevel();
        }


        public void NextTurn()
        {
            if (playerIds.Count == 0)
            {
                return;
            }
            int currentPlayerIndex = playerIds.IndexOf(CurrentPlayer) + 1;
            if (currentPlayerIndex >= playerIds.Count)
            {
                currentPlayerIndex = 0;
            }
            CurrentPlayer = playerIds[currentPlayerIndex];
            NowPlayingEvent?.Invoke(GetPlayer(CurrentPlayer));
        }


        public void MovePlayer(MonopolyPlayer player)
        {
            PlayerMovedEvent?.Invoke(player);
        }


        public void PlayerBankrupt(MonopolyPlayer player)
        {
            players.Remove(player.PlayerId);
            playerIds.Remove(player.PlayerId);
            player.gameObject.SetActive(false);
            if (players.Count <= 1)
            {
                GameOver();
            }
            else if (CurrentPlayer == player.PlayerId)
            {
                NextTurn();
            }
        }


        public MonopolyPlayer GetPlayer(PlayerId playerId)
        {
            return players.TryGetValue(playerId, out MonopolyPlayer player) ? player : null;
        }


        public Tile GetTile(int tileId)
        {
            return tileId >= 0 && tileId < tiles.Count ? tiles[tileId] : null;
        }


        private void GameOver()
        {
            MonopolyPlayer winner = players.Values.FirstOrDefault();
            if (winner != null)
            {
                PlayerScore = winner.Money;
                UpdateScore();
                controller?.PlayerDialog($"{winner.name} is the last player remaining", $"{winner.name} Won");
            }
            TransitionState(BaseGameState.GameOver);
        }


        public override bool DoesSaveGameExist()
        {
            IStorageStrategy disk = Disk;
            return disk != null && disk.DoesKeyExist(SavePrefix + "saved") && disk.GetBool(SavePrefix + "saved");
        }


        public override void SaveGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null || !(IsGameRunning || State.Is(BaseGameState.Paused)))
            {
                return;
            }

            disk.SetInt(SavePrefix + "players", allPlayers.Count);
            for (int i = 0; i < allPlayers.Count; i++)
            {
                MonopolyPlayer player = allPlayers[i];
                disk.SetInt($"{SavePrefix}player{i}.money", player.Money);
                disk.SetInt($"{SavePrefix}player{i}.location", player.Location);
                disk.SetInt($"{SavePrefix}player{i}.lastLocation", player.LastLocation);
                disk.SetBool($"{SavePrefix}player{i}.active", players.ContainsKey(player.PlayerId));
            }
            List<Asset> assets = settings.Board.Assets;
            disk.SetInt(SavePrefix + "assets", assets.Count);
            for (int i = 0; i < assets.Count; i++)
            {
                PlayerId owner = assets[i].OwningPlayer != null ? assets[i].OwningPlayer.PlayerId : PlayerId.None;
                disk.SetInt($"{SavePrefix}asset{i}.owner", (int)owner);
            }
            disk.SetInt(SavePrefix + "currentPlayer", (int)CurrentPlayer);
            disk.SetBool(SavePrefix + "saved", true);

            try
            {
                disk.Persist();
            }
            catch (NotImplementedException notImplemented)
            {
                UI?.UpdateError($"Cannot save game - storage does not support it. {notImplemented.Message}");
                return;
            }
            UI?.EnableLoad();
        }


        public override void LoadGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null || !DoesSaveGameExist())
            {
                UI?.UpdateError("There is no saved game to load.");
                return;
            }
            if (!boardReady)
            {
                UI?.UpdateError("The Monopoly board is not set up.");
                return;
            }

            ResetBoard();
            int playerCount = Math.Min(disk.GetInt(SavePrefix + "players"), allPlayers.Count);
            for (int i = 0; i < playerCount; i++)
            {
                MonopolyPlayer player = allPlayers[i];
                player.Restore(disk.GetInt($"{SavePrefix}player{i}.money"), disk.GetInt($"{SavePrefix}player{i}.location"),
                    disk.GetInt($"{SavePrefix}player{i}.lastLocation"));
                if (!disk.GetBool($"{SavePrefix}player{i}.active"))
                {
                    players.Remove(player.PlayerId);
                    playerIds.Remove(player.PlayerId);
                    player.gameObject.SetActive(false);
                }
            }
            List<Asset> assets = settings.Board.Assets;
            int assetCount = Math.Min(disk.GetInt(SavePrefix + "assets"), assets.Count);
            for (int i = 0; i < assetCount; i++)
            {
                var owner = (PlayerId)disk.GetInt($"{SavePrefix}asset{i}.owner");
                MonopolyPlayer player = GetPlayer(owner);
                if (player != null)
                {
                    player.ClaimAsset(assets[i]);
                }
            }
            var current = (PlayerId)disk.GetInt(SavePrefix + "currentPlayer");
            CurrentPlayer = players.ContainsKey(current) ? current : (playerIds.Count > 0 ? playerIds[0] : PlayerId.None);

            foreach (MonopolyPlayer player in allPlayers)
            {
                if (player.gameObject.activeSelf)
                {
                    ui?.PlaceToken(player, GetTile(player.Location));
                }
            }
            controller?.ResetDialog();
            TransitionState(BaseGameState.Running);
            NowPlayingEvent?.Invoke(GetPlayer(CurrentPlayer));
        }
    }
}
