using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// Memory Cards game module. Deals the cards, checks matches and ends the game in victory or when the base
    /// timer runs out; menu, settings, pause and save/load buttons come from <see cref="BaseGameManager"/>.
    /// </summary>
    public class MemoryCardsGameManager : BaseGameManager
    {
        /// The reason for using the concrete classes which implement the IStorage and ICache interfaces is
        /// ease of use for assignability in the Unity editor.
        [SerializeField] private FlippableCache CardsCacheBehaviour;
        [SerializeField] private ItemLineCache CardLineCacheBehaviour;
        private ICache<Flippable> cardsCache;
        private ICache<HorizontalLayoutGroup> cardLinesCache;


        [SerializeField] private MemoryCardsSettings defaultSettings, customSettings, currentGameSettings;
        [SerializeField] private VerticalLayoutGroup CardsPanel;
        [SerializeField] private MemoryCardsUI gameUI;
        [SerializeField] private MemoryCardsSettingsUI settingsUI;
        [SerializeField] private MemoryCardsController controller;
        [SerializeField] private Campaign campaign;


        [SerializeField] private MemoryCardsSettings settings;
        [SerializeField] private Vector2 CardRectSize;

        private readonly List<Flippable> deployedCards = new List<Flippable>();
        private readonly IList<Flippable> flippedCards = new List<Flippable>();
        private readonly IList<HorizontalLayoutGroup> cardLines = new List<HorizontalLayoutGroup>();
        private bool gameStartedWithCustomSettings;


        private const string SavePrefix = "MemoryCards.";
        private const string gameSaveWithCustomSettings = SavePrefix + "GameSavedWithCustomSettings";
        private const string savedGameSettingsPrefix = SavePrefix + "GameSave";
        private const string saveCardPrefix = SavePrefix + "card";
        private const string saveCardIndex = "-index";
        private const string saveCardState = "-state";
        private const string saveTimer = SavePrefix + "timer";
        private const string saveScore = SavePrefix + "score";
        private const string cardsAmountKey = SavePrefix + "CardsInSaveGame";
        private const int cardsDisplayLength = 16;

        public override IGameController Controller => controller;
        public override IGameUI UI => gameUI;
        public override ICampaign Campaign => campaign;
        public override IGameSettings DefaultSettings => defaultSettings;
        public override IGameSettings CustomSettings => customSettings;

        public override IGameSettings Settings
        {
            get => settings != null ? settings : defaultSettings;
            set => settings = value as MemoryCardsSettings;
        }

        /// <summary>The settings the current (or next) game plays with.</summary>
        public MemoryCardsSettings ActiveSettings => Settings as MemoryCardsSettings;

        public MemoryCardsPlayer Player => PlayerList.OfType<MemoryCardsPlayer>().FirstOrDefault();


        protected override void Awake()
        {
            base.Awake();
            gameUI?.ClearError();
            if (null == StorageBehaviour || null == CardsCacheBehaviour || null == CardLineCacheBehaviour)
            {
                string error = "Storage or cache reference is missing.";
                gameUI?.UpdateError(error);
                throw new ArgumentNullException(error);
            }
            cardsCache = CardsCacheBehaviour;
            cardLinesCache = CardLineCacheBehaviour;
        }


        #region Game Logic

        public override void StartGame()
        {
            Undeploy();

            if (!UsingDefaultSettings)
            {
                try
                {
                    currentGameSettings.CopySettings(customSettings);
                    UI?.SettingsUI?.CopyToSettings(currentGameSettings);
                }
                catch (FormatException formatException)
                {
                    UI?.UpdateError($"Cannot start game because of bad game settings. Reason: {formatException.Message}");
                    return;
                }
                if (!currentGameSettings.AreSettingsValid(out string error))
                {
                    UI?.UpdateError($"Cannot start game because of bad game settings. Reason: {error}");
                    return;
                }
                settings = currentGameSettings;
                gameStartedWithCustomSettings = true;
            }
            else
            {
                gameStartedWithCustomSettings = false;
                settings = defaultSettings;
            }

            Player?.ResetMatches();
            PrepareGame();
            TransitionState(BaseGameState.Running);
        }


        public override void TransitionState(GameState state)
        {
            bool wasRunning = IsGameRunning;
            base.TransitionState(state);
            switch (state.BaseState)
            {
                case BaseGameState.Running:
                    if (!wasRunning && (Audio == null || Audio.clip != GameRunningAudio || !Audio.isPlaying))
                    {
                        PlayClip(GameRunningAudio);
                    }
                    break;
                case BaseGameState.Victory:
                    PlayClip(VictoryAudio);
                    foreach (Flippable card in deployedCards)
                    {
                        card.PlayWin();
                    }
                    Invoke(nameof(CleanGameState), 2f);
                    break;
                case BaseGameState.GameOver:
                    PlayClip(GameOverAudio);
                    foreach (Flippable card in deployedCards)
                    {
                        card.PlayLose();
                    }
                    Invoke(nameof(CleanGameState), 2f);
                    break;
            }
        }


        public override void LoadLevel(int level)
        {
            LevelIndex = level;
            CurrentLevel = LevelData.Create(level);
            if (Level is MemoryCardsLevel cardsLevel && cardsLevel.Settings != null)
            {
                defaultSettings = cardsLevel.Settings;
                if (UsingDefaultSettings)
                {
                    settings = defaultSettings;
                }
            }
            UpdateLevel();
        }


        /// <summary>
        /// Does all the preparations for starting a new game.
        /// It uses the game settings to decide how many card types participate in the game, then assigns
        /// to Card Amount of cards random types from the deck - shuffling it.
        /// The cards are then reset for game and put in lines of cards which handle alignment.
        /// </summary>
        private void PrepareGame()
        {
            MemoryCardsSettings active = ActiveSettings;
            List<Sprite> cardTypes = active.CardTypes != null && active.CardTypes.Count > 0 ? active.CardTypes : defaultSettings.CardTypes;
            float cardScalePerAxis = Mathf.Min(1f, 1f / ((float)active.CardsPerRow / (float)cardsDisplayLength));
            deployedCards.AddRange(cardsCache.Deploy(active.CardsAmount));
            int cardTypesInGame = Math.Min(cardTypes.Count, active.CardsAmount / active.FlippedCardsPerMatch);
            // Making a list of card types by index
            var cardTypeIndexNumbers = Enumerable.Range(0, cardTypesInGame).ToList();
            var cardTypeIndexes = new List<int>(cardTypeIndexNumbers);
            for (int i = 0; i < active.FlippedCardsPerMatch - 1; i++)
            {
                cardTypeIndexes.AddRange(cardTypeIndexNumbers);
            }

            // Shuffling the deck
            cardTypeIndexes.Shuffle();

            // Cards are placed under HorizontalLayoutGroup that align them into lines
            int cardsLeftInRow = active.CardsPerRow - 1;
            HorizontalLayoutGroup cardLine = DeployCardLine();

            foreach (Flippable card in deployedCards)
            {
                // UI objects are parented in local space so the canvas scale does not shrink them.
                card.transform.SetParent(cardLine.transform, false);
                card.transform.localScale = Vector3.one;
                if (cardsLeftInRow > 0)
                {
                    cardsLeftInRow--;
                }
                else
                {
                    cardsLeftInRow = active.CardsPerRow - 1;
                    cardLine = DeployCardLine();
                }

                int typeIndex = cardTypeIndexes.Count > 0 ? cardTypeIndexes[0] : 0;
                card.SetType(typeIndex, cardTypes[typeIndex % cardTypes.Count]);
                if (cardTypeIndexes.Count > 0)
                {
                    cardTypeIndexes.RemoveAt(0);
                }
                var rect = card.GetComponent<RectTransform>();
                rect.sizeDelta = CardRectSize * cardScalePerAxis;

                Button cardButton = card.GetComponent<Button>();
                cardButton.onClick.RemoveAllListeners();
                Flippable captured = card;
                cardButton.onClick.AddListener(() => CardFlipped(captured));
            }
            cardLinesCache.Undeploy(cardLine);

            Timer = active.TimePerGame;
            UpdateTimer();
            ResetScore();
        }


        private HorizontalLayoutGroup DeployCardLine()
        {
            HorizontalLayoutGroup cardLine = cardLinesCache.Deploy();
            cardLines.Add(cardLine);
            cardLine.transform.SetParent(CardsPanel.transform, false);
            cardLine.transform.localScale = Vector3.one;
            return cardLine;
        }


        /// <summary>
        /// Handles the pressing of a card in the game. Checks whether it matches the type of other
        /// flipped cards, and ends the game in victory if all cards have been matched.
        /// </summary>
        /// <param name="card"></param>
        private void CardFlipped(Flippable card)
        {
            MemoryCardsSettings active = ActiveSettings;
            if (flippedCards.Count >= active.FlippedCardsPerMatch || card.State != FlippableState.Hidden || !IsGameRunning)
            {
                return;
            }

            card.SetState(FlippableState.Flipped);
            flippedCards.Add(card);
            if (flippedCards.Count > 0)
            {
                if (flippedCards.All(alreadyFlipped => alreadyFlipped.FlippableTypeIndex == card.FlippableTypeIndex))
                {
                    if (flippedCards.Count == active.FlippedCardsPerMatch)
                    {
                        foreach (Flippable matchedCard in flippedCards)
                        {
                            matchedCard.SetState(FlippableState.Matched);
                        }
                        flippedCards.Clear();
                        IncreaseScore(1);
                        Player?.RegisterMatch();

                        if (!deployedCards.Any(deployedCard => deployedCard.State != FlippableState.Matched))
                        {
                            TransitionState(BaseGameState.Victory);
                        }
                    }
                }
                else
                {
                    foreach (var flippedCard in flippedCards)
                    {
                        flippedCard.FlippedImage.color = Color.red;
                    }
                    Invoke(nameof(UnflipCards), active.TimeUntilUnflip);
                }
            }
        }


        private void UnflipCards()
        {
            foreach (Flippable card in flippedCards)
            {
                card.SetState(FlippableState.Hidden);
            }
            flippedCards.Clear();
        }


        private void CleanGameState()
        {
            UI?.Show();
            Undeploy();
        }


        private void Undeploy()
        {
            CancelInvoke(nameof(UnflipCards));
            flippedCards.Clear();
            foreach (Flippable card in deployedCards)
            {
                card.SetState(FlippableState.Hidden);
                cardsCache.Undeploy(card);
            }
            deployedCards.Clear();

            foreach (var cardLine in cardLines)
            {
                cardLinesCache.Undeploy(cardLine);
            }
            cardLines.Clear();
        }

        #endregion


        #region Saving/Loading

        public override bool DoesSaveGameExist()
        {
            IStorageStrategy disk = Disk;
            return disk != null && disk.DoesKeyExist(cardsAmountKey);
        }


        /// <summary>
        /// Saves the game by first deleting previous save if it exists,
        /// and then saving cards by index both type and state.
        /// When a game is saved, current game settings are saved with it unless
        /// default settings are used. Also saves game score and timer.
        /// </summary>
        public override void SaveGame()
        {
            IStorageStrategy disk = Disk;
            if (disk == null)
            {
                return;
            }
            if (!State.Is(BaseGameState.Paused))
            {
                UI?.UpdateError("Pause the game (Escape) to save it.");
                return;
            }

            if (DoesSaveGameExist())
            {
                int cardsSaved = disk.GetInt(cardsAmountKey);
                for (int i = 0; i < cardsSaved; i++)
                {
                    disk.DeleteByKey($"{saveCardPrefix}{i}{saveCardIndex}");
                    disk.DeleteByKey($"{saveCardPrefix}{i}{saveCardState}");
                }
            }

            disk.SetInt(cardsAmountKey, deployedCards.Count);
            for (int i = 0; i < deployedCards.Count; i++)
            {
                Flippable card = deployedCards[i];
                disk.SetInt($"{saveCardPrefix}{i}{saveCardIndex}", card.FlippableTypeIndex);
                disk.SetInt($"{saveCardPrefix}{i}{saveCardState}", (int)card.State);
            }
            disk.SetFloat(saveScore, PlayerScore);
            disk.SetFloat(saveTimer, Timer);

            if (gameStartedWithCustomSettings)
            {
                disk.SetBool(gameSaveWithCustomSettings, true);
                currentGameSettings.SaveSettings(disk, savedGameSettingsPrefix);
            }
            else
            {
                disk.SetBool(gameSaveWithCustomSettings, false);
            }

            try
            {
                disk.Persist();
            }
            catch (NotImplementedException excp)
            {
                UI?.UpdateError($"Cannot save game - storage does not support it. {excp.Message}");
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

            if (deployedCards.Count > 0)
            {
                Undeploy();
            }

            if (disk.DoesKeyExist(gameSaveWithCustomSettings) && disk.GetBool(gameSaveWithCustomSettings))
            {
                currentGameSettings.LoadSettings(disk, savedGameSettingsPrefix);
                settings = currentGameSettings;
                gameStartedWithCustomSettings = true;
            }
            else
            {
                settings = defaultSettings;
                gameStartedWithCustomSettings = false;
            }

            PrepareGame();
            MemoryCardsSettings active = ActiveSettings;
            List<Sprite> cardTypes = active.CardTypes != null && active.CardTypes.Count > 0 ? active.CardTypes : defaultSettings.CardTypes;
            for (int i = 0; i < deployedCards.Count; i++)
            {
                Flippable card = deployedCards[i];
                int cardTypeIndex = disk.GetInt($"{saveCardPrefix}{i}{saveCardIndex}");
                card.SetType(cardTypeIndex, cardTypes[cardTypeIndex % cardTypes.Count]);
                int cardState = disk.GetInt($"{saveCardPrefix}{i}{saveCardState}");
                card.SetState((FlippableState)cardState);
            }

            PlayerScore = disk.GetFloat(saveScore);
            UpdateScore();
            Timer = disk.GetFloat(saveTimer);
            UpdateTimer();
            TransitionState(BaseGameState.Running);
        }

        #endregion
    }


    public static class Extension
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
