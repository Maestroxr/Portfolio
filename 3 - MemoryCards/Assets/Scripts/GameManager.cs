using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Portfolio
{
    public enum GameState { Initialization, Running, Paused, GameOver, Victory }

    /// <summary>
    /// Main class for managing the game.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] public bool UsingDefaultSettings;
        public GameState CurrentGameState => GameState;
        [SerializeField] private GameState GameState;


        /// The reason for using the concrete classes which implement the IStorage and ICache interfaces is
        /// ease of use for assignability in the Unity editor. There are solutions for
        /// interface assignability(for example on the asset store) so the code is not coupled to concrete implementations.
        [SerializeField] private Storage StorageBehaviour;
        [SerializeField] private FlippableCache CardsCacheBehaviour;
        [SerializeField] private ItemLineCache CardLineCacheBehaviour;
        private IStorage storage;
        private ICache<Flippable> cardsCache;
        private ICache<HorizontalLayoutGroup> cardLinesCache;


        [SerializeField] private GameSettings DefaultSettings, CustomSettings, CurrentGameSettings;
        [SerializeField] private VerticalLayoutGroup CardsPanel;
        [SerializeField] private GameUI GameUI;
        [SerializeField] private SettingsUI SettingsUI;
        [SerializeField] private AudioSource Audio;
        [SerializeField] private AudioClip VictoryAudio, GameOverAudio, GameRunningAudio;


        [SerializeField] private GameSettings Settings;
        [SerializeField] private int PlayerScore;
        [SerializeField] private float Timer;
        [SerializeField] private Vector2 CardRectSize;

        private List<Flippable> deployedCards = new List<Flippable>();
        private IList<Flippable> flippedCards = new List<Flippable>();
        private IList<HorizontalLayoutGroup> cardLines = new List<HorizontalLayoutGroup>();
        private bool gameStartedWithCustomSettings;


        private const string customSettingKey = "CustomSettings";
        private const string gameSaveWithCustomSettings = "GameSavedWithCustomSettings";
        private const string savedGameSettingsPrefix = "GameSave";
        private const string saveCardPrefix = "card";
        private const string saveCardIndex = "-index";
        private const string saveCardState = "-state";
        private const string saveTimer = "timer";
        private const string saveScore = "score";
        private const string cardsAmountKey = "CardsInSaveGame";
        private const int cardsDisplayLenngth = 16;

        void Awake()
        {
            GameUI.ClearError();
            if (null == StorageBehaviour || null == CardsCacheBehaviour)
            {
                string error = "Storage or cache reference is missing.";
                GameUI.UpdateError(error);
                throw new ArgumentNullException(error);
            }
            storage = StorageBehaviour;
            cardsCache = CardsCacheBehaviour;
            cardLinesCache = CardLineCacheBehaviour;

            TransitionState(GameState.Initialization);
        }


        private void Start()
        {
            if (storage.SelectedStorage.DoesKeyExist(customSettingKey))
            {
                UsingDefaultSettings = !storage.SelectedStorage.GetBool(customSettingKey);
            }
            if (UsingDefaultSettings)
            {
                Settings = DefaultSettings;
            }
            else
            {
                LoadSettings();
                Settings = CustomSettings;
            }
            SettingsUI.UsingDefault(UsingDefaultSettings);

            if (DoesSaveGameExit())
            {
                GameUI.EnableLoad();
            }

            ShowMenu();
        }

        void Update()
        {
            if (GameState == GameState.Running)
            {
                GameUI.UpdateTimer(Timer);
                Timer -= Time.deltaTime;
                if (Timer < 0)
                {
                    GameOver();
                }
                else
                {
                    if (Input.GetKey(KeyCode.Escape))
                    {
                        TransitionState(GameState.Paused);
                        ShowMenu();
                    }
                }
            }
        }


        #region Game Logic

        public void StartNewGame()
        {
            Undeploy();

            if (!UsingDefaultSettings)
            {
                //CurrentGameSettings.CopySettings(CustomSettings);
                SettingsUI.CopyToSettings(CurrentGameSettings);
                if (!CurrentGameSettings.AreSettingsValid(out string error))
                {
                    string message = $"Cannot start game because of bad game settings. Reason: {error}";
                    GameUI.UpdateError(message);
                    return;
                    //throw new GameSettingsException(message);
                }
                Settings = CurrentGameSettings;
                gameStartedWithCustomSettings = true;
            }
            else
            {
                gameStartedWithCustomSettings = false;
                Settings = DefaultSettings;
            }

            TransitionState(GameState.Running);
            Audio.clip = GameRunningAudio;
            Audio.Play();

            PrepareGame();
            HideMenu();
        }


        private void Victory()
        {
            TransitionState(GameState.Victory);

            Audio.clip = VictoryAudio;
            Audio.Play();

            foreach (Flippable card in deployedCards)
            {
                card.PlayWin();
            }

            Invoke("CleanGameState", 2);
        }


        private void GameOver()
        {
            TransitionState(GameState.GameOver);

            Audio.clip = GameOverAudio;
            Audio.Play();

            foreach (Flippable card in deployedCards)
            {
                card.PlayLose();
            }

            Invoke("CleanGameState", 2);
        }


        private void TransitionState(GameState state)
        {
            GameUI.UpdateGameState(state);
            GameState = state;
        }


        /// <summary>
        /// Does all the preparations for starting a new game.
        /// It uses the game settings to decide how many card types participate in the game, then assigns 
        /// to Card Amount of cards random types from the deck - shuffling it.
        /// The cards are then reset for game and put in lines of cards which handle alignment.
        /// </summary>
        private void PrepareGame()
        {
            float cardScalePerAxis = Mathf.Min(1f, 1f / ((float)Settings.CardsPerRow / (float)cardsDisplayLenngth));
            deployedCards.AddRange(cardsCache.Deploy(Settings.CardsAmount));
            int cardTypesInGame = Math.Min(Settings.CardTypes.Count, Settings.CardsAmount / Settings.FlippedCardsPerMatch);
            // Making a list of card types by index
            var cardTypeIndexNumbers = Enumerable.Range(0, cardTypesInGame).ToList();
            var cardTypeIndexes = new List<int>(cardTypeIndexNumbers);
            for (int i = 0; i < Settings.FlippedCardsPerMatch - 1; i++)
            {
                cardTypeIndexes.AddRange(cardTypeIndexNumbers);
            }

            // Shuffling the deck
            cardTypeIndexes.Shuffle();

            // Cards are placed under HorizontalLayoutGroup that align them into lines
            int cardsLeftInRow = Settings.CardsPerRow - 1;
            HorizontalLayoutGroup cardLine = cardLinesCache.Deploy();
            cardLines.Add(cardLine);
            cardLine.transform.parent = CardsPanel.transform;

            foreach (Flippable card in deployedCards)
            {
                card.transform.parent = cardLine.transform;
                if (cardsLeftInRow > 0)
                {
                    cardsLeftInRow--;
                }
                else
                {
                    cardsLeftInRow = Settings.CardsPerRow - 1;
                    cardLine = cardLinesCache.Deploy();
                    cardLines.Add(cardLine);
                    cardLine.transform.parent = CardsPanel.transform;
                }

                card.SetType(cardTypeIndexes[0], Settings.CardTypes[cardTypeIndexes[0]]);
                cardTypeIndexes.RemoveAt(0);
                var rect = card.GetComponent<RectTransform>();
                rect.sizeDelta = CardRectSize * cardScalePerAxis;

                Button cardButton = card.GetComponent<Button>();
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => CardFlipped(card));
            }
            cardLinesCache.Undeploy(cardLine);

            Timer = Settings.TimePerGame;
            PlayerScore = 0;
            GameUI.UpdateScore(PlayerScore);
        }


        /// <summary>
        /// Handles the pressing of a card in the game. Checks whether it matches the type of other
        /// flipped cards, and ends the game in victory if all cards have been matched.
        /// </summary>
        /// <param name="card"></param>
        private void CardFlipped(Flippable card)
        {
            if (flippedCards.Count >= Settings.FlippedCardsPerMatch || card.State != FlippableState.Hidden || GameState != GameState.Running) return;

            card.SetState(FlippableState.Flipped);
            flippedCards.Add(card);
            if (flippedCards.Count > 0)
            {
                if (flippedCards.All(alreadyFlipped => alreadyFlipped.FlippableTypeIndex == card.FlippableTypeIndex))
                {

                    if (flippedCards.Count == Settings.FlippedCardsPerMatch)
                    {
                        foreach (Flippable matchedCard in flippedCards)
                        {
                            matchedCard.SetState(FlippableState.Matched);
                        }
                        flippedCards.Clear();
                        PlayerScore++;
                        GameUI.UpdateScore(PlayerScore);

                        if (!deployedCards.Any(deployedCard => deployedCard.State != FlippableState.Matched))
                        {
                            Victory();
                        }
                    }
                }
                else
                {
                    foreach (var flippedCard in flippedCards)
                    {
                        flippedCard.FlippedImage.color = Color.red;
                    }
                    Invoke("UnflipCards", Settings.TimeUntilUnflip);
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
            ShowMenu();
            Undeploy();
        }


        private void Undeploy()
        {
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


        #region Settings And Saving/Loading

        /// <summary>
        /// Saves custom settings, which are stored seprately from saved game settings.
        /// </summary>
        public void SaveSettings()
        {
            SaveSettings(CustomSettings, customSettingKey);
        }


        public void SaveSettings(GameSettings settings, string key)
        {
            try
            {
                SettingsUI.SaveSettings(storage.SelectedStorage, settings);
                storage.SelectedStorage.SetBool(key, true);
            }
            catch (GameSettingsException settingsException)
            {
                GameUI.UpdateError($"Cannot save settings. Reason: {settingsException.Message}");
            }
            catch (NotImplementedException notImpl)
            {
                GameUI.UpdateError($"Cannot save settings. Reason: {notImpl.Message}");
            }
        }


        public bool DoesSaveGameExit()
        {
            return storage.SelectedStorage.DoesKeyExist(cardsAmountKey);
        }

        /// <summary>
        /// Saves the game by first deleting previous save if it exists,
        /// and then saving cards by index both type and state.
        /// When a game is saved, current game settings are saved with it unless
        /// default settings are used. Also saves game score and timer.
        /// </summary>
        public void SaveGame()
        {
            if (GameState != GameState.Paused)
            {
                return;
            }

            if (DoesSaveGameExit())
            {
                int cardsSaved = storage.SelectedStorage.GetInt(cardsAmountKey);
                for (int i = 0; i < cardsSaved; i++)
                {
                    storage.SelectedStorage.DeleteByKey($"{saveCardPrefix}{i}{saveCardIndex}");
                    storage.SelectedStorage.DeleteByKey($"{saveCardPrefix}{i}{saveCardState}");
                }
            }

            storage.SelectedStorage.SetInt(cardsAmountKey, deployedCards.Count);
            for (int i = 0; i < deployedCards.Count; i++)
            {
                Flippable card = deployedCards[i];
                storage.SelectedStorage.SetInt($"{saveCardPrefix}{i}{saveCardIndex}", card.FlippableTypeIndex);
                storage.SelectedStorage.SetInt($"{saveCardPrefix}{i}{saveCardState}", (int)card.State);
            }
            storage.SelectedStorage.SetInt($"{saveScore}", PlayerScore);
            storage.SelectedStorage.SetFloat($"{saveTimer}", Timer);
            

            if (gameStartedWithCustomSettings)
            {
                storage.SelectedStorage.SetBool(gameSaveWithCustomSettings, true);
                CurrentGameSettings.SaveSettings(storage.SelectedStorage, savedGameSettingsPrefix);
            }
            else
            {
                storage.SelectedStorage.SetBool(gameSaveWithCustomSettings, false);
            }

            try
            {
                storage.SelectedStorage.Persist();
            }
            catch (NotImplementedException excp)
            {
                GameUI.UpdateError($"Cannot save game - storage does not support it. {excp.Message}");
                return;
            }

            GameUI.EnableLoad();
        }


        public void LoadGame()
        {
            if (deployedCards.Count > 0)
            {
                Undeploy();
            }

            if (storage.SelectedStorage.GetBool(gameSaveWithCustomSettings))
            {
                CurrentGameSettings.LoadSettings(storage.SelectedStorage, savedGameSettingsPrefix);
                Settings = CurrentGameSettings;
                gameStartedWithCustomSettings = true;
            }
            else
            {
                Settings = DefaultSettings;
                gameStartedWithCustomSettings = false;
            }

            PrepareGame();
            for (int i = 0; i < deployedCards.Count; i++)
            {
                Flippable card = deployedCards[i];
                int cardTypeIndex = storage.SelectedStorage.GetInt($"{saveCardPrefix}{i}{saveCardIndex}");
                card.SetType(cardTypeIndex, Settings.CardTypes[cardTypeIndex]);
                int cardState = storage.SelectedStorage.GetInt($"{saveCardPrefix}{i}{saveCardState}");
                card.SetState((FlippableState)cardState);
            }

            PlayerScore = storage.SelectedStorage.GetInt($"{saveScore}");
            GameUI.UpdateScore(PlayerScore);
            Timer = storage.SelectedStorage.GetFloat($"{saveTimer}");
            TransitionState(GameState.Running);
            HideMenu();

            Audio.clip = GameRunningAudio;
            Audio.Play();
        }

        // Loads custom settings into the UI
        public void LoadSettings()
        {
            SettingsUI.LoadSettings(storage.SelectedStorage, CustomSettings, customSettingKey);
            SettingsUI.UsingDefault(false);
            Settings = CustomSettings;
        }

        /// <summary>
        /// Switches between a new game starting according to default or custom settings.
        /// </summary>
        public void SwitchCustomDefaultSettings()
        {
            UsingDefaultSettings = !UsingDefaultSettings;
            if (storage.SelectedStorage.DoesKeyExist(customSettingKey))
            {
                if (!UsingDefaultSettings)
                {
                    SettingsUI.LoadSettings(storage.SelectedStorage, CustomSettings, customSettingKey);
                    storage.SelectedStorage.SetBool(customSettingKey, true);
                }
                else
                {
                    storage.SelectedStorage.SetBool(customSettingKey, false);
                }
            }
            SettingsUI.UsingDefault(UsingDefaultSettings);
        }

        #endregion


        #region UI Handling

        public void ShowMenu()
        {
            GameUI.gameObject.SetActive(true);
        }


        public void ReturnToGame()
        {
            if (GameState == GameState.Paused)
            {
                TransitionState(GameState.Running);
                HideMenu();
            }
        }


        public void HideMenu()
        {
            GameUI.gameObject.SetActive(false);
        }


        public void Exit()
        {
            Application.Quit();
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