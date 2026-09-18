using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// Board layout, player prefab and the timings of a Monopoly game. The starting money, dice roll time and
    /// player move time are the user configurable part edited by the settings menu.
    /// </summary>
    [CreateAssetMenu(fileName = "MonopolySettings", menuName = "Monopoly/Settings", order = 1)]
    public class MonopolySettings : GameSettings
    {
        // Board class contains the tile layout of the monopoly board
        [SerializeField]
        private Board board;
        public Board Board => board;

        [SerializeField]
        private uint maxPlayers = 2;
        public uint MaxPlayers => maxPlayers;

        public MonopolyPlayer PlayerPrefab;

        [SerializeField]
        private int initialPlayerLocation = 0;
        public int InitialPlayerLocation => initialPlayerLocation;

        [field: SerializeField]
        public int InitialPlayerMoney { get; set; } = 5;

        [SerializeField]
        private float diceRollTime;
        public float DiceRollTime
        {
            get => diceRollTime;
            set => diceRollTime = value;
        }

        [SerializeField]
        private float diceRollChangeValueTime;
        public float DiceRollChangeValueTime => diceRollChangeValueTime;

        [SerializeField]
        private float playerMoveTime;
        public float PlayerMoveTime
        {
            get => playerMoveTime;
            set => playerMoveTime = value;
        }


        public override bool AreSettingsValid(out string message)
        {
            if (board == null)
            {
                message = "No board assigned";
                return false;
            }
            if (!board.Check(out message))
            {
                return false;
            }
            if (maxPlayers < 2)
            {
                message = $"Max players {maxPlayers} has to be 2 or more";
                return false;
            }
            if (InitialPlayerMoney <= 0)
            {
                message = $"Initial player money {InitialPlayerMoney} has to be positive";
                return false;
            }
            if (diceRollTime <= 0f)
            {
                message = $"Dice roll time {diceRollTime} has to be positive";
                return false;
            }
            if (playerMoveTime <= 0f)
            {
                message = $"Player move time {playerMoveTime} has to be positive";
                return false;
            }
            message = "OK";
            return true;
        }


        public override void SaveSettings(IStorageStrategy storage, string prefix)
        {
            if (!AreSettingsValid(out string error))
            {
                throw new GameSettingsException($"Cannot save invalid settings. Reason: {error}");
            }
            storage.SetInt($"{prefix}InitialPlayerMoney", InitialPlayerMoney);
            storage.SetFloat($"{prefix}DiceRollTime", diceRollTime);
            storage.SetFloat($"{prefix}PlayerMoveTime", playerMoveTime);
        }


        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            if (storage.DoesKeyExist($"{prefix}InitialPlayerMoney"))
            {
                InitialPlayerMoney = storage.GetInt($"{prefix}InitialPlayerMoney");
            }
            if (storage.DoesKeyExist($"{prefix}DiceRollTime"))
            {
                diceRollTime = storage.GetFloat($"{prefix}DiceRollTime");
            }
            if (storage.DoesKeyExist($"{prefix}PlayerMoveTime"))
            {
                playerMoveTime = storage.GetFloat($"{prefix}PlayerMoveTime");
            }
        }


        public override void CopySettings(IGameSettings other)
        {
            if (!(other is MonopolySettings source))
            {
                return;
            }
            board = source.board;
            maxPlayers = source.maxPlayers;
            PlayerPrefab = source.PlayerPrefab;
            initialPlayerLocation = source.initialPlayerLocation;
            InitialPlayerMoney = source.InitialPlayerMoney;
            diceRollTime = source.diceRollTime;
            diceRollChangeValueTime = source.diceRollChangeValueTime;
            playerMoveTime = source.playerMoveTime;
        }
    }
}
