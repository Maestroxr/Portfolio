using Gamebox;
using UnityEngine;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The board, the rules and the pace of a game. Every game mode (<see cref="MonopolyLevel"/>) has its own settings
    /// asset; the default settings asset is also the starting point of the custom rules that the settings panel edits
    /// (the house rules, starting cash, round limit, dealt properties and animation speed).
    /// </summary>
    [CreateAssetMenu(fileName = "MonopolySettings", menuName = "Monopoly/Settings", order = 1)]
    public class MonopolySettings : GameSettings
    {
        [SerializeField] private Board board;
        [SerializeField] private RuleSet rules = new RuleSet();
        [Tooltip("1 plays the animations at normal speed, 2 twice as fast.")]
        [SerializeField] private float animationSpeed = 1f;
        [Tooltip("Seconds a computer player waits before each move.")]
        [SerializeField] private float botThinkTime = 0.45f;

        public Board Board
        {
            get => board;
            set => board = value;
        }

        public RuleSet Rules => rules ??= new RuleSet();

        public float AnimationSpeed
        {
            get => animationSpeed;
            set => animationSpeed = value;
        }

        public float BotThinkTime
        {
            get => botThinkTime;
            set => botThinkTime = value;
        }

        public void SetRules(RuleSet value)
        {
            rules = value != null ? value.Clone() : new RuleSet();
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
            if (!Rules.IsValid(out message))
            {
                return false;
            }
            if (animationSpeed < 0.25f || animationSpeed > 4f)
            {
                message = $"Animation speed {animationSpeed} has to be between 0.25 and 4";
                return false;
            }
            if (botThinkTime < 0f)
            {
                message = "The computer players' thinking time cannot be negative";
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
            RuleSet r = Rules;
            storage.SetInt($"{prefix}.StartingCash", r.startingCash);
            storage.SetInt($"{prefix}.RoundLimit", r.roundLimit);
            storage.SetInt($"{prefix}.DealtProperties", r.dealtProperties);
            storage.SetInt($"{prefix}.HousesForHotel", r.housesForHotel);
            storage.SetBool($"{prefix}.Auctions", r.auctions);
            storage.SetBool($"{prefix}.SpeedDie", r.speedDie);
            storage.SetBool($"{prefix}.Jackpot", r.freeParkingJackpot);
            storage.SetBool($"{prefix}.DoubleGo", r.doubleSalaryOnGo);
            storage.SetBool($"{prefix}.NoRentInJail", r.noRentInJail);
            storage.SetBool($"{prefix}.PartyCards", r.partyCards);
            storage.SetFloat($"{prefix}.AnimationSpeed", animationSpeed);
        }

        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            RuleSet r = Rules;
            r.startingCash = ReadInt(storage, $"{prefix}.StartingCash", r.startingCash);
            r.roundLimit = ReadInt(storage, $"{prefix}.RoundLimit", r.roundLimit);
            r.dealtProperties = ReadInt(storage, $"{prefix}.DealtProperties", r.dealtProperties);
            r.housesForHotel = ReadInt(storage, $"{prefix}.HousesForHotel", r.housesForHotel);
            r.auctions = ReadBool(storage, $"{prefix}.Auctions", r.auctions);
            r.speedDie = ReadBool(storage, $"{prefix}.SpeedDie", r.speedDie);
            r.freeParkingJackpot = ReadBool(storage, $"{prefix}.Jackpot", r.freeParkingJackpot);
            r.doubleSalaryOnGo = ReadBool(storage, $"{prefix}.DoubleGo", r.doubleSalaryOnGo);
            r.noRentInJail = ReadBool(storage, $"{prefix}.NoRentInJail", r.noRentInJail);
            r.partyCards = ReadBool(storage, $"{prefix}.PartyCards", r.partyCards);
            if (storage.DoesKeyExist($"{prefix}.AnimationSpeed"))
            {
                animationSpeed = storage.GetFloat($"{prefix}.AnimationSpeed");
            }
            // A dealt short game ends at the second bankruptcy, as the official short game does.
            r.bankruptciesToEnd = r.dealtProperties > 0 ? 2 : 0;
        }

        public override void CopySettings(IGameSettings other)
        {
            if (!(other is MonopolySettings source))
            {
                return;
            }
            board = source.board;
            rules = source.Rules.Clone();
            animationSpeed = source.animationSpeed;
            botThinkTime = source.botThinkTime;
        }

        private static int ReadInt(IStorageStrategy storage, string key, int fallback)
        {
            return storage.DoesKeyExist(key) ? storage.GetInt(key) : fallback;
        }

        private static bool ReadBool(IStorageStrategy storage, string key, bool fallback)
        {
            return storage.DoesKeyExist(key) ? storage.GetBool(key) : fallback;
        }
    }
}
