using System.Collections.Generic;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static Portfolio.Asteroids.AsteroidsText;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The strike mode's interface on the Asteroids canvas (referenced by <see cref="AsteroidsUI"/>): the PLANET STRIKE tab
    /// of the mission select (the 3 x 3 sector map, the details with the boss, the star rules and the difficulty, the
    /// money, LAUNCH and SUPPLY ROOM), the Supply Room overlay, the strike HUD, the strike results and the touch buttons
    /// WEAPON and MEGA. <see cref="AsteroidsInterfaceBuilder"/> builds it (BuildStrike).
    /// </summary>
    public class StrikeUI : MonoBehaviour
    {
        private static readonly Color Good = new Color(0.55f, 1f, 0.65f);
        private static readonly Color Bad = new Color(1f, 0.42f, 0.35f);
        private static readonly Color Money = new Color(1f, 0.84f, 0.35f);
        private static readonly Color Faint = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color TabOn = new Color(1f, 0.6f, 0.25f);
        private static readonly Color TabOff = new Color(0.16f, 0.26f, 0.4f, 0.95f);

        [Tooltip("The Asteroids interface this belongs to (the manager, the sounds, the field's screens).")]
        [SerializeField] internal AsteroidsUI ui;

        [Header("Screens")]
        [Tooltip("The strike tab of the mission select.")]
        [SerializeField] internal CanvasGroup missionSelect;
        [Tooltip("The Supply Room overlay.")]
        [SerializeField] internal CanvasGroup supply;
        [Tooltip("The strike HUD.")]
        [SerializeField] internal CanvasGroup hud;
        [Tooltip("The strike results.")]
        [SerializeField] internal CanvasGroup results;
        [SerializeField] internal ShopCard[] shopCards = new ShopCard[0];

        [Header("Mission select")]
        [SerializeField] internal MissionNode[] missionNodes = new MissionNode[0];
        [SerializeField] internal TMP_Text[] sectorTitles = new TMP_Text[0];
        [SerializeField] internal TMP_Text[] sectorGates = new TMP_Text[0];
        [Tooltip("The header and the path of each sector column, in pairs (hidden for the columns the campaign does not use).")]
        [SerializeField] internal GameObject[] sectorColumns = new GameObject[0];
        [SerializeField] internal Image detailAccent;
        [SerializeField] internal TMP_Text detailSector;
        [SerializeField] internal TMP_Text detailTitle;
        [SerializeField] internal Image[] detailStars = new Image[0];
        [SerializeField] internal TMP_Text detailDescription;
        [SerializeField] internal TMP_Text detailBoss;
        [SerializeField] internal TMP_Text detailGoals;
        [SerializeField] internal TMP_Text detailDifficulty;
        [SerializeField] internal TMP_Text detailBest;
        [Tooltip("ROOKIE, VETERAN, ELITE (StrikeDifficulty order).")]
        [SerializeField] internal Button[] difficultyButtons = new Button[0];
        [SerializeField] internal Button launchButton;
        [SerializeField] internal TMP_Text launchLabel;
        [SerializeField] internal Button supplyButton;
        [SerializeField] internal TMP_Text starsTotal;
        [SerializeField] internal TMP_Text walletText;
        [Tooltip("Shown when the campaign has no strike missions.")]
        [SerializeField] internal GameObject emptyNote;

        [Header("Supply Room")]
        [SerializeField] internal Button buyTab;
        [SerializeField] internal Button sellTab;
        [SerializeField] internal Button actionButton;
        [SerializeField] internal TMP_Text actionLabel;
        [SerializeField] internal Button supplyBackButton;
        [SerializeField] internal Image itemIcon;
        [SerializeField] internal TMP_Text itemTitle;
        [SerializeField] internal TMP_Text itemGroup;
        [SerializeField] internal TMP_Text itemDescription;
        [SerializeField] internal TMP_Text itemPrice;
        [SerializeField] internal TMP_Text itemOwned;
        [SerializeField] internal TMP_Text supplyMoney;
        [SerializeField] internal Image supplyEnergyFill;
        [SerializeField] internal TMP_Text supplyEnergyText;
        [SerializeField] internal TMP_Text supplyShields;
        [SerializeField] internal TMP_Text supplyMegabombs;
        [SerializeField] internal TMP_Text quartermaster;
        [SerializeField] internal TMP_Text supplyEmpty;

        [Header("HUD")]
        [SerializeField] internal TMP_Text hudWallet;
        [SerializeField] internal TMP_Text hudEarned;
        [SerializeField] internal Image progressFill;
        [SerializeField] internal Image specialIcon;
        [SerializeField] internal TMP_Text specialText;
        [SerializeField] internal Image[] phaseIcons = new Image[0];
        [SerializeField] internal Image phaseFill;
        [SerializeField] internal Image energyFill;
        [SerializeField] internal TMP_Text energyText;
        [SerializeField] internal Image[] megabombIcons = new Image[0];
        [SerializeField] internal Image megabombRing;
        [SerializeField] internal GameObject bossBar;
        [SerializeField] internal TMP_Text bossName;
        [SerializeField] internal Image bossFill;
        [SerializeField] internal TMP_Text bossNoScanner;
        [SerializeField] internal TMP_Text warningText;
        [Tooltip("Parts of the strike HUD about the one ship at this device (special, phase shields, energy, megabombs): hidden in " +
                 "local co-op, where the pilots list shows every pilot.")]
        [SerializeField] internal GameObject[] shipHudParts = new GameObject[0];

        [Header("Results")]
        [SerializeField] internal TMP_Text resultTitle;
        [SerializeField] internal TMP_Text resultSubtitle;
        [SerializeField] internal Image[] resultStars = new Image[0];
        [SerializeField] internal TMP_Text resultMoney;
        [SerializeField] internal TMP_Text resultWallet;
        [SerializeField] internal TMP_Text resultStats;
        [SerializeField] internal TMP_Text resultGoals;
        [SerializeField] internal Button resultMissions;
        [SerializeField] internal TMP_Text resultMissionsLabel;
        [SerializeField] internal Button resultSupply;
        [SerializeField] internal Button resultRetry;
        [SerializeField] internal Button resultNext;
        [SerializeField] internal TMP_Text resultNextLabel;

        [Header("Touch")]
        [Tooltip("The ship's touch controls: in strike DASH cycles the special (WEAPON) and NOVA drops a megabomb (MEGA).")]
        [SerializeField] internal ShipTouchControls touch;
        [Tooltip("The icon of the DASH button, which shows the special weapon in strike.")]
        [SerializeField] internal Image touchCycleIcon;
        [Tooltip("The DASH button's own icon, for the asteroid field.")]
        [SerializeField] internal Sprite touchDashSprite;

        [Header("Sprites")]
        [Tooltip("An icon per StrikeItem (by value).")]
        [SerializeField] internal Sprite[] itemSprites = new Sprite[0];
        [SerializeField] internal Sprite starFull;
        [SerializeField] internal Sprite starEmpty;

        /// <summary>The full star: the active theme's, else the scene's.</summary>
        internal Sprite StarFull => AsteroidsUI.Themed(theme => theme.Interface.starFull, starFull);

        /// <summary>The empty star: the active theme's, else the scene's.</summary>
        internal Sprite StarEmpty => AsteroidsUI.Themed(theme => theme.Interface.starEmpty, starEmpty);

        private readonly List<StrikeMissionSummary> summaries = new List<StrikeMissionSummary>();
        private readonly List<StrikeItemInfo> offers = new List<StrikeItemInfo>();
        private StrikeLoadout pilot;
        private StrikeItem selectedItem = StrikeItem.AirMissiles;
        private bool selectUnderSupply;
        private bool selling;
        private bool strikeMode;
        private int shownWallet = -1;
        private int shownEarned = -1;
        private int shownShields = -1;
        private int shownBombs = -1;
        private int shownEnergy = -1;
        private string shownBossName;
        private bool bossNameShown;
        private StrikeItem shownSpecial = (StrikeItem)255;
        private float warningTime = -1f;
        private float resultsTime = -1f;
        private int resultStarCount;
        private int starsPopped;
        private int resultMoneyTarget;
        private StrikeResult? shownResult;

        /// <summary>Whether the Supply Room is open.</summary>
        public bool IsSupplyOpen => supply != null && supply.gameObject.activeSelf;

        /// <summary>Whether the Supply Room lists what the pilot can sell (else what can be bought).</summary>
        public bool IsSelling => selling;

        /// <summary>The item shown in the Supply Room's details.</summary>
        public StrikeItem SelectedItem => selectedItem;

        private AsteroidsGameManager Game => ui != null ? ui.Game : null;

        private AsteroidsAudio Sounds => ui != null ? ui.Audio : null;


        private void Awake()
        {
            Listen(launchButton, () => Game?.LaunchSelectedMission());
            Listen(supplyButton, () => Game?.OpenSupply());
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                var difficulty = (StrikeDifficulty)i;
                Listen(difficultyButtons[i], () => Game?.SetDifficulty(difficulty));
            }
            foreach (MissionNode node in missionNodes)
            {
                if (node != null)
                {
                    node.Clicked += index => Game?.SelectMission(index);
                }
            }
            foreach (ShopCard card in shopCards)
            {
                if (card != null)
                {
                    card.Clicked += item => SelectItem((StrikeItem)item);
                }
            }
            Listen(buyTab, () => SetSelling(false));
            Listen(sellTab, () => SetSelling(true));
            Listen(actionButton, OnAction);
            Listen(supplyBackButton, () => Game?.CloseSupply());
            Listen(resultMissions, () => Game?.ReturnToMissionSelect());
            Listen(resultSupply, () => Game?.OpenSupplyAfterResults());
            Listen(resultRetry, () => Game?.RetryMission());
            Listen(resultNext, () => Game?.PlayNextMission());
            SetActive(supply, false);
            SetActive(results, false);
            SetActive(hud, false);
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }


        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (warningText != null && warningTime >= 0f)
            {
                warningTime += deltaTime;
                warningText.alpha = Mathf.Repeat(warningTime * 3f, 1f) < 0.65f ? 1f : 0.2f;
                if (warningTime > 2.4f)
                {
                    warningTime = -1f;
                    warningText.gameObject.SetActive(false);
                }
            }
            AnimateResults(deltaTime);
        }


        private static void Listen(Button button, UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }


        #region Screens

        /// <summary>The base game's state changed (the manager's screens follow it).</summary>
        public void UpdateGameState(GameState state)
        {
            if (!state.Is(BaseGameState.Initialization))
            {
                SetActive(supply, false);
            }
            // The results come back with ShowResults right after the state changed.
            shownResult = null;
            SetActive(results, false);
            resultsTime = -1f;
            if (state.Is(BaseGameState.Initialization))
            {
                SetActive(hud, false);
                warningTime = -1f;
            }
        }


        /// <summary>
        /// The field screens or the strike ones: the strike tab and HUD show in strike, and the touch buttons turn into WEAPON
        /// and MEGA (the field look comes back in the asteroid field).
        /// </summary>
        public void ShowMode(MissionMode mode)
        {
            strikeMode = mode == MissionMode.Strike;
            if (!strikeMode)
            {
                SetActive(missionSelect, false);
                SetActive(supply, false);
                SetActive(hud, false);
            }
            if (touchCycleIcon != null)
            {
                touchCycleIcon.sprite = strikeMode ? ItemSprite(shownSpecial == (StrikeItem)255 ? StrikeItem.MachineGun : shownSpecial) : touchDashSprite;
            }
        }


        /// <summary>Back closes the Supply Room first. True when it did something.</summary>
        public bool HandleBack()
        {
            if (IsSupplyOpen)
            {
                Game?.CloseSupply();
                if (IsSupplyOpen)
                {
                    HideSupply();
                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// The language changed: the Supply Room and the HUD write their words again (the manager redraws the mission
        /// select, the fixed texts follow the canvas's LocalizedTexts).
        /// </summary>
        public void RefreshTexts()
        {
            if (IsSupplyOpen)
            {
                RefreshSupply();
            }
            shownSpecial = (StrikeItem)255;
            bossNameShown = false;
        }


        /// <summary>Hides every strike screen.</summary>
        public void HideAll()
        {
            SetActive(missionSelect, false);
            SetActive(supply, false);
            SetActive(hud, false);
            SetActive(results, false);
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
            warningTime = -1f;
            resultsTime = -1f;
        }

        #endregion


        #region Mission select

        /// <summary>Shows the strike tab with its missions, the selected one, the strike stars and the pilot.</summary>
        public void ShowMissionSelect(List<StrikeMissionSummary> missions, int selected, int stars, int maxStars, StrikeLoadout pilot)
        {
            this.pilot = pilot;
            summaries.Clear();
            if (missions != null)
            {
                summaries.AddRange(missions);
            }
            if (IsSupplyOpen)
            {
                // Refreshed under the open Supply Room: shown again when the room closes.
                selectUnderSupply = true;
            }
            else
            {
                SetActive(missionSelect, true);
            }
            for (int i = 0; i < missionNodes.Length; i++)
            {
                if (missionNodes[i] == null)
                {
                    continue;
                }
                bool used = i < summaries.Count;
                missionNodes[i].gameObject.SetActive(used);
                if (used)
                {
                    missionNodes[i].Show(NodeSummary(summaries[i]), summaries[i].Index == selected, StarFull, StarEmpty);
                }
            }
            ShowSectorHeaders(stars);
            SetLabel(starsTotal, $"{stars} / {maxStars}");
            SetLabel(walletText, pilot != null ? $"${pilot.Money:N0}" : "$0");
            if (emptyNote != null)
            {
                emptyNote.SetActive(summaries.Count == 0);
            }
            int current = summaries.FindIndex(summary => summary.Index == selected);
            ShowDetails(current >= 0 ? summaries[current] : (StrikeMissionSummary?)null);
            ShowDifficulty(pilot != null ? pilot.Difficulty : StrikeRules.NewPilotDifficulty);
        }


        /// <summary>Hides the strike tab (the asteroid field tab is shown).</summary>
        public void HideMissionSelect()
        {
            SetActive(missionSelect, false);
            SetActive(supply, false);
        }


        private static MissionSummary NodeSummary(StrikeMissionSummary summary)
        {
            return new MissionSummary
            {
                Index = summary.Index,
                Number = summary.Number,
                Title = summary.Title,
                Accent = summary.Accent,
                Unlocked = summary.Unlocked,
                Stars = summary.Stars,
                Sector = summary.Sector,
                SectorTitle = summary.SectorTitle
            };
        }


        private void ShowSectorHeaders(int stars)
        {
            var sectors = new List<StrikeMissionSummary>();
            foreach (StrikeMissionSummary summary in summaries)
            {
                if (!sectors.Exists(known => known.Sector == summary.Sector))
                {
                    sectors.Add(summary);
                }
            }
            for (int i = 0; i < sectorColumns.Length; i++)
            {
                if (sectorColumns[i] != null)
                {
                    sectorColumns[i].SetActive(i / 2 < sectors.Count);
                }
            }
            for (int i = 0; i < sectorTitles.Length; i++)
            {
                bool used = i < sectors.Count;
                if (sectorTitles[i] != null)
                {
                    sectorTitles[i].text = used ? sectors[i].SectorTitle.ToUpperInvariant() : string.Empty;
                    if (used)
                    {
                        sectorTitles[i].color = sectors[i].Accent;
                    }
                }
                if (i < sectorGates.Length && sectorGates[i] != null)
                {
                    int required = used ? sectors[i].SectorStars : 0;
                    sectorGates[i].text = !used ? string.Empty : required <= 0 ? T("OPEN") : stars >= required ? F("{0} STARS - OPEN", required) : F("NEEDS {0} STARS", required);
                    sectorGates[i].color = required <= 0 || stars >= required ? new Color(0.6f, 1f, 0.7f, 0.8f) : new Color(1f, 0.75f, 0.35f);
                }
            }
        }


        private void ShowDetails(StrikeMissionSummary? shown)
        {
            if (shown == null)
            {
                SetLabel(detailSector, T("PLANET STRIKE"));
                SetLabel(detailTitle, T("No missions"));
                SetLabel(detailDescription, T("The strike campaign has no missions yet."));
                SetLabel(detailBoss, string.Empty);
                SetLabel(detailGoals, string.Empty);
                SetLabel(detailBest, string.Empty);
                SetStars(detailStars, 0);
                if (launchButton != null)
                {
                    launchButton.interactable = false;
                }
                SetLabel(launchLabel, T("LAUNCH"));
                return;
            }
            StrikeMissionSummary summary = shown.Value;
            Color accent = summary.Accent.a > 0f ? summary.Accent : TabOn;
            if (detailAccent != null)
            {
                detailAccent.color = accent;
            }
            SetLabel(detailSector, F("{0}  -  MISSION {1}", summary.SectorTitle.ToUpperInvariant(), summary.Number));
            if (detailSector != null)
            {
                detailSector.color = accent;
            }
            SetLabel(detailTitle, summary.Title);
            SetLabel(detailDescription, summary.Description);
            SetLabel(detailBoss, string.IsNullOrEmpty(summary.BossName) ? string.Empty : $"<color=#FF8A7A>{T("BOSS")}</color>  {summary.BossName}");
            SetLabel(detailGoals,
                $"<color=#FFD45E>{T("STARS")}</color>  {T("Complete the mission")}\n" +
                $"<color=#FFD45E>+</color>  {F("Destroy {0}% of the hostiles", Mathf.RoundToInt(StrikeRules.StarKillShare * 100f))}\n" +
                $"<color=#FFD45E>+</color>  {F("Take at most {0:0} damage", StrikeRules.StarDamage)}");
            SetLabel(detailBest, summary.BestMoney > 0 ? F("BEST  ${0:N0}", summary.BestMoney) : T("Not flown yet"));
            SetStars(detailStars, summary.Stars);
            if (launchButton != null)
            {
                launchButton.interactable = summary.Unlocked;
            }
            SetLabel(launchLabel, summary.Unlocked ? T("LAUNCH") : T("LOCKED"));
            if (!summary.Unlocked)
            {
                SetLabel(detailBest, $"<color=#FFB24D>{summary.LockReason}</color>");
            }
        }


        private void ShowDifficulty(StrikeDifficulty difficulty)
        {
            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                if (difficultyButtons[i] != null && difficultyButtons[i].targetGraphic != null)
                {
                    difficultyButtons[i].targetGraphic.color = i == (int)difficulty ? TabOn : TabOff;
                }
            }
            SetLabel(detailDifficulty, DifficultyText(difficulty));
        }


        /// <summary>What a difficulty changes, for the details.</summary>
        internal static string DifficultyText(StrikeDifficulty difficulty)
        {
            switch (difficulty)
            {
                case StrikeDifficulty.Rookie: return T("Half the damage, weaker bosses.");
                case StrikeDifficulty.Elite: return T("More hostiles and no energy regeneration.");
                default: return T("The campaign as it was meant to be flown.");
            }
        }

        #endregion


        #region Supply Room

        /// <summary>Shows (or refreshes) the Supply Room for <paramref name="pilot"/> with <paramref name="selected"/> in the details.</summary>
        public void ShowSupply(StrikeLoadout pilot, StrikeItem selected)
        {
            this.pilot = pilot;
            selectedItem = selected;
            if (!IsSupplyOpen)
            {
                // The Supply Room covers the whole screen: the mission select under it is hidden (its wallet and title
                // would otherwise show through) and comes back when the room closes.
                selectUnderSupply = missionSelect != null && missionSelect.gameObject.activeSelf;
                SetActive(missionSelect, false);
            }
            SetActive(supply, true);
            RefreshSupply();
        }


        public void HideSupply()
        {
            bool wasOpen = IsSupplyOpen;
            SetActive(supply, false);
            if (wasOpen && selectUnderSupply)
            {
                SetActive(missionSelect, true);
            }
            selectUnderSupply = false;
        }


        /// <summary>The quartermaster's line under the title (a greeting, a thank you, a refusal).</summary>
        public void Quartermaster(string line, bool pleased = true)
        {
            SetLabel(quartermaster, line);
            if (quartermaster != null)
            {
                quartermaster.color = pleased ? new Color(0.78f, 0.86f, 0.96f) : new Color(1f, 0.7f, 0.4f);
            }
        }


        /// <summary>Lists what can be bought, or what the pilot can sell.</summary>
        public void SetSelling(bool sell)
        {
            if (selling != sell)
            {
                Sounds?.Click();
            }
            selling = sell;
            RefreshSupply();
        }


        private void SelectItem(StrikeItem item)
        {
            if (selectedItem != item)
            {
                Sounds?.Click();
            }
            selectedItem = item;
            RefreshSupply();
        }


        private void OnAction()
        {
            if (selling)
            {
                Game?.SellItem(selectedItem);
            }
            else
            {
                Game?.BuyItem(selectedItem);
            }
        }


        /// <summary>The items of the list: what is for sale, or what the pilot has to sell, by price.</summary>
        internal static void Offers(StrikeLoadout pilot, bool sell, List<StrikeItemInfo> into)
        {
            into.Clear();
            foreach (StrikeItemInfo info in StrikeArmory.Sorted())
            {
                if (info.NotForSale || info.Max == 0 && info.Item != StrikeItem.EnergyModule)
                {
                    continue;
                }
                if (sell)
                {
                    // Energy sells only while the launch minimum stays (StrikeArmory.CanSellEnergy).
                    bool has = info.Item == StrikeItem.EnergyModule
                        ? pilot != null && StrikeArmory.CanSellEnergy(pilot)
                        : pilot != null && pilot.Count(info.Item) > 0;
                    if (!has)
                    {
                        continue;
                    }
                }
                into.Add(info);
            }
        }


        private void RefreshSupply()
        {
            Offers(pilot, selling, offers);
            if (offers.Count > 0 && !offers.Exists(info => info.Item == selectedItem))
            {
                selectedItem = offers[0].Item;
            }
            for (int i = 0; i < shopCards.Length; i++)
            {
                if (shopCards[i] == null)
                {
                    continue;
                }
                bool used = i < offers.Count;
                shopCards[i].gameObject.SetActive(used);
                if (used)
                {
                    StrikeItemInfo info = offers[i];
                    int price = selling ? StrikeArmory.SellPrice(pilot, info.Item) : StrikeArmory.BuyPrice(pilot, info.Item);
                    shopCards[i].Show(info, pilot != null ? pilot.Count(info.Item) : 0, price, info.Item == selectedItem, ItemSprite(info.Item));
                }
            }
            SetTab(buyTab, !selling);
            SetTab(sellTab, selling);
            if (supplyEmpty != null)
            {
                supplyEmpty.gameObject.SetActive(offers.Count == 0);
                supplyEmpty.text = selling ? T("Nothing to sell.") : T("Nothing for sale.");
            }
            ShowItem(offers.Count > 0 ? selectedItem : (StrikeItem?)null);
            ShowPilot();
        }


        private void ShowItem(StrikeItem? shown)
        {
            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(shown != null);
            }
            if (shown == null)
            {
                SetLabel(itemTitle, string.Empty);
                SetLabel(itemGroup, string.Empty);
                SetLabel(itemDescription, string.Empty);
                SetLabel(itemPrice, string.Empty);
                SetLabel(itemOwned, string.Empty);
                if (itemIcon != null)
                {
                    itemIcon.enabled = false;
                }
                return;
            }
            StrikeItem item = shown.Value;
            StrikeItemInfo info = StrikeArmory.Info(item);
            if (itemIcon != null)
            {
                itemIcon.sprite = ItemSprite(item);
                itemIcon.enabled = itemIcon.sprite != null;
            }
            SetLabel(itemTitle, T(info.Title));
            SetLabel(itemGroup, GroupText(info));
            SetLabel(itemDescription, T(info.Description));
            int count = pilot != null ? pilot.Count(item) : 0;
            if (selling)
            {
                int pay = StrikeArmory.SellPrice(pilot, item);
                SetLabel(itemPrice, item == StrikeItem.EnergyModule ? F("PAYS  ${0:N0}  for {1} energy", pay, StrikeArmory.EnergySellAmount) : F("PAYS  ${0:N0}", pay));
                SetLabel(actionLabel, T("SELL"));
            }
            else
            {
                int cost = StrikeArmory.BuyPrice(pilot, item);
                bool afford = pilot != null && pilot.Money >= cost;
                string price = afford ? $"${cost:N0}" : $"<color=#FF7A66>${cost:N0}</color>";
                if (item == StrikeItem.EnergyModule)
                {
                    SetLabel(itemPrice, cost > 0 ? F("PRICE  {0}  ({1} a point)", price, StrikeArmory.Price(item)) : T("ENERGY FULL"));
                }
                else
                {
                    SetLabel(itemPrice, F("PRICE  {0}", price));
                }
                SetLabel(actionLabel, T("BUY"));
            }
            string owned = ShopCard.OwnedText(info, count);
            SetLabel(itemOwned, item == StrikeItem.EnergyModule
                ? F("ENERGY  {0} / {1}", Mathf.RoundToInt(pilot != null ? pilot.Energy : 0f), StrikeRules.MaxEnergy)
                : string.IsNullOrEmpty(owned) ? T("Not owned") : F("OWNED  {0}", owned));
        }


        private void ShowPilot()
        {
            int money = pilot != null ? pilot.Money : 0;
            // Seven digits, as the old counter showed them.
            SetLabel(supplyMoney, Mathf.Clamp(money, 0, StrikeRules.WalletCap).ToString("D7"));
            float energy = pilot != null ? pilot.Energy : 0f;
            if (supplyEnergyFill != null)
            {
                supplyEnergyFill.fillAmount = Mathf.Clamp01(energy / StrikeRules.MaxEnergy);
                supplyEnergyFill.color = EnergyColor(energy / StrikeRules.MaxEnergy);
            }
            SetLabel(supplyEnergyText, $"{Mathf.RoundToInt(energy)}");
            SetLabel(supplyShields, $"x{(pilot != null ? pilot.PhaseShields : 0)}");
            SetLabel(supplyMegabombs, $"x{(pilot != null ? pilot.Megabombs : 0)}");
        }


        private static string GroupText(StrikeItemInfo info)
        {
            switch (info.Group)
            {
                case ItemGroup.AlwaysOn: return T("PRIMARY WEAPON  -  ALWAYS FIRES");
                case ItemGroup.Special:
                    Altitude mask = StrikeWeaponRules.Mask(info.Item);
                    string layers = mask == Altitude.Air ? T("AIR") : mask == Altitude.Ground ? T("GROUND") : T("AIR + GROUND");
                    return F("SPECIAL WEAPON  -  {0}", layers);
                case ItemGroup.Consumable: return T("SUPPLIES");
                default: return T("EQUIPMENT");
            }
        }


        private static void SetTab(Button tab, bool on)
        {
            if (tab != null && tab.targetGraphic != null)
            {
                tab.targetGraphic.color = on ? TabOn : TabOff;
            }
        }

        #endregion


        #region HUD

        /// <summary>A strike mission starts: the strike HUD replaces the field's.</summary>
        public void BeginMission(StrikeLevel mission, string sectorTitle)
        {
            ShowMode(MissionMode.Strike);
            SetActive(hud, true);
            SetActive(results, false);
            SetActive(missionSelect, false);
            SetActive(supply, false);
            shownWallet = -1;
            shownEarned = -1;
            shownShields = -1;
            shownBombs = -1;
            shownEnergy = -1;
            shownBossName = null;
            bossNameShown = false;
            shownSpecial = (StrikeItem)255;
            warningTime = -1f;
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
            if (bossBar != null)
            {
                bossBar.SetActive(false);
            }
            if (progressFill != null)
            {
                progressFill.fillAmount = 0f;
                if (mission != null && mission.Terrain != null)
                {
                    progressFill.color = AsteroidsThemes.Accent(mission.Terrain, progressFill.color);
                }
            }
        }


        /// <summary>Refreshes the strike HUD.</summary>
        public void UpdateHud(StrikeHudState state)
        {
            if (state.Wallet != shownWallet)
            {
                shownWallet = state.Wallet;
                SetLabel(hudWallet, $"${state.Wallet:N0}");
            }
            if (state.MissionMoney != shownEarned)
            {
                shownEarned = state.MissionMoney;
                SetLabel(hudEarned, state.MissionMoney > 0 ? $"+${state.MissionMoney:N0}" : string.Empty);
            }
            float blend = Time.unscaledDeltaTime * 3f;
            StepFill(progressFill, state.Progress, blend);
            if (energyFill != null)
            {
                StepFill(energyFill, state.Energy, blend);
                energyFill.color = EnergyColor(state.Energy);
            }
            int energy = Mathf.CeilToInt(state.Energy * StrikeRules.MaxEnergy);
            if (energy != shownEnergy)
            {
                shownEnergy = energy;
                SetLabel(energyText, energy.ToString());
            }
            StepFill(phaseFill, state.PhaseShields > 0 ? state.Shield : 0f, blend);
            if (state.PhaseShields != shownShields)
            {
                shownShields = state.PhaseShields;
                for (int i = 0; i < phaseIcons.Length; i++)
                {
                    if (phaseIcons[i] != null)
                    {
                        phaseIcons[i].color = i < state.PhaseShields ? new Color(0.45f, 0.85f, 1f) : Faint;
                    }
                }
            }
            if (state.Megabombs != shownBombs)
            {
                shownBombs = state.Megabombs;
                for (int i = 0; i < megabombIcons.Length; i++)
                {
                    if (megabombIcons[i] != null)
                    {
                        megabombIcons[i].color = i < state.Megabombs ? Color.white : Faint;
                    }
                }
            }
            StepFill(megabombRing, 1f - Mathf.Clamp01(state.MegabombCooldown), 1f);
            if (state.Special != shownSpecial)
            {
                shownSpecial = state.Special;
                bool none = state.Special == StrikeItem.MachineGun;
                SetLabel(specialText, none ? T("NO SPECIAL") : T(ShortName(state.Special)));
                if (specialText != null)
                {
                    specialText.color = none ? new Color(1f, 1f, 1f, 0.45f) : new Color(1f, 0.72f, 0.35f);
                }
                if (specialIcon != null)
                {
                    specialIcon.sprite = ItemSprite(state.Special);
                    specialIcon.color = none ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                }
                if (touchCycleIcon != null && strikeMode)
                {
                    touchCycleIcon.sprite = ItemSprite(state.Special);
                }
            }
            if (bossBar != null)
            {
                if (bossBar.activeSelf != state.BossActive)
                {
                    bossBar.SetActive(state.BossActive);
                    bossNameShown = false;
                    if (state.BossActive && bossFill != null)
                    {
                        bossFill.fillAmount = 0f;
                    }
                }
                if (state.BossActive)
                {
                    // Upper-cased once per boss (or name change), not once a frame.
                    if (!bossNameShown || !ReferenceEquals(state.BossName, shownBossName))
                    {
                        bossNameShown = true;
                        shownBossName = state.BossName;
                        SetLabel(bossName, T(state.BossName ?? string.Empty).ToUpperInvariant());
                    }
                    if (bossFill != null)
                    {
                        bossFill.enabled = state.HasScanner;
                        StepFill(bossFill, state.BossHealth, Time.unscaledDeltaTime * 1.5f);
                    }
                    if (bossNoScanner != null)
                    {
                        bossNoScanner.gameObject.SetActive(!state.HasScanner);
                    }
                }
            }
        }


        /// <summary>The strike HUD of a local co-op mission (<paramref name="local"/>) leaves out the parts about one ship.</summary>
        public void ShowLocalHud(bool local)
        {
            foreach (GameObject part in shipHudParts)
            {
                if (part != null && part.activeSelf == local)
                {
                    part.SetActive(!local);
                }
            }
        }


        /// <summary>A blinking warning on the HUD ("SHIELD LOW", "WEAPON DESTROYED").</summary>
        public void Warn(string text)
        {
            if (warningText == null)
            {
                return;
            }
            warningText.text = text;
            warningText.gameObject.SetActive(true);
            warningTime = 0f;
        }


        /// <summary>The short name of a weapon for the HUD.</summary>
        internal static string ShortName(StrikeItem item)
        {
            switch (item)
            {
                case StrikeItem.MachineGun: return "MACHINE GUN";
                case StrikeItem.PlasmaCannon: return "PLASMA CANNON";
                case StrikeItem.MicroMissiles: return "MICRO MISSILES";
                case StrikeItem.Dumbfire: return "DUMBFIRE";
                case StrikeItem.MiniGun: return "MINI-GUN";
                case StrikeItem.LaserTurret: return "LASER TURRET";
                case StrikeItem.MissilePods: return "MISSILE PODS";
                case StrikeItem.AirMissiles: return "AIR/AIR MISSILES";
                case StrikeItem.GroundMissiles: return "AIR/GROUND MISSILES";
                case StrikeItem.Bombs: return "BOMBS";
                case StrikeItem.PulseCannon: return "PULSE CANNON";
                case StrikeItem.Deathray: return "DEATHRAY";
                case StrikeItem.TwinLaser: return "TWIN LASER";
                case StrikeItem.MegaBomb: return "MEGABOMB";
                case StrikeItem.EnergyModule: return "ENERGY";
                case StrikeItem.PhaseShield: return "PHASE SHIELD";
                case StrikeItem.IonScanner: return "ION SCANNER";
                default: return item.ToString().ToUpperInvariant();
            }
        }

        #endregion


        #region Results

        /// <summary>Shows the results of a strike mission (solo, or co-op through the manager's co-op results).</summary>
        public void ShowResults(StrikeResult result)
        {
            if (result.Coop && shownResult.HasValue && shownResult.Value.Coop && shownResult.Value.Title == result.Title && results != null &&
                results.gameObject.activeInHierarchy)
            {
                // The places of a shared mission changed: only the standings show again.
                shownResult = result;
                SetLabel(resultGoals, result.Standings);
                return;
            }
            shownResult = result;
            ui?.ShowFieldResults(false);
            SetActive(hud, false);
            SetActive(results, true);
            SetLabel(resultTitle, result.Victory ? T("MISSION COMPLETE") : T("MISSION FAILED"));
            if (resultTitle != null)
            {
                resultTitle.color = result.Victory ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.4f, 0.35f);
            }
            SetLabel(resultSubtitle, T(result.Title ?? string.Empty).ToUpperInvariant());
            resultMoneyTarget = result.Victory ? result.Money : 0;
            SetLabel(resultMoney, result.Victory ? "+$0" : $"<color=#FF7A66>{F("${0:N0} LOST", result.Money)}</color>");
            string best = result.NewBest ? $"<color=#FFD24A>{T("NEW BEST!")}</color>   " : string.Empty;
            SetLabel(resultWallet, best + F("WALLET  ${0:N0}", result.Wallet));
            int share = result.HostilesEntered > 0 ? Mathf.RoundToInt(100f * result.HostilesDestroyed / result.HostilesEntered) : 0;
            SetLabel(resultStats,
                $"{T("Hostiles destroyed")}  <b>{result.HostilesDestroyed} / {result.HostilesEntered}</b> ({share}%)\n" +
                $"{T("Damage taken")}  <b>{Mathf.RoundToInt(result.DamageTaken)}</b>     {T("Time")}  <b>{MissionObjective.FormatTime(result.Time)}</b>");
            bool together = result.Coop || result.Local;
            if (together)
            {
                SetLabel(resultGoals, result.Standings);
            }
            else
            {
                SetLabel(resultGoals,
                    Goal(result.Victory, T("Complete the mission")) + "\n" +
                    Goal(result.Victory && result.KillStar, F("Destroy {0}% of the hostiles", Mathf.RoundToInt(StrikeRules.StarKillShare * 100f))) + "\n" +
                    Goal(result.Victory && result.DamageStar, F("Take at most {0:0} damage", StrikeRules.StarDamage)));
            }
            SetLabel(resultMissionsLabel, result.Coop ? T("Room") : T("Missions"));
            if (resultSupply != null)
            {
                resultSupply.gameObject.SetActive(!together);
            }
            if (resultRetry != null)
            {
                resultRetry.gameObject.SetActive(!result.Coop);
            }
            if (resultNext != null)
            {
                resultNext.gameObject.SetActive(result.HasNext && !together);
                resultNext.interactable = !result.NextLocked;
            }
            SetLabel(resultNextLabel, result.NextLocked ? T("LOCKED") : T("NEXT"));
            if (result.NextLocked && result.HasNext && !together)
            {
                SetLabel(resultGoals, (resultGoals != null ? resultGoals.text : string.Empty) + $"\n<color=#FFB24D>{result.NextLockReason}</color>");
            }
            resultStarCount = together ? 0 : result.Stars;
            starsPopped = 0;
            resultsTime = 0f;
            foreach (Image star in resultStars)
            {
                if (star != null)
                {
                    star.sprite = StarEmpty;
                    star.transform.localScale = Vector3.one;
                    star.gameObject.SetActive(!together);
                }
            }
        }


        /// <summary>The strike results go (a field result or the menu takes over).</summary>
        public void HideResults()
        {
            shownResult = null;
            SetActive(results, false);
            resultsTime = -1f;
        }


        private void AnimateResults(float deltaTime)
        {
            if (resultsTime < 0f)
            {
                return;
            }
            resultsTime += deltaTime;
            float count = Mathf.Clamp01((resultsTime - 0.3f) / 1.4f);
            if (resultMoneyTarget > 0 && resultMoney != null)
            {
                resultMoney.text = $"+${Mathf.RoundToInt(resultMoneyTarget * Tween.OutCubic(count)):N0}";
            }
            while (starsPopped < resultStarCount && starsPopped < resultStars.Length && resultsTime > 0.6f + starsPopped * 0.45f)
            {
                if (resultStars[starsPopped] != null)
                {
                    resultStars[starsPopped].sprite = StarFull;
                }
                Sounds?.Star(starsPopped);
                starsPopped++;
            }
            for (int i = 0; i < resultStars.Length && i < starsPopped; i++)
            {
                if (resultStars[i] == null)
                {
                    continue;
                }
                float age = resultsTime - (0.6f + i * 0.45f);
                resultStars[i].transform.localScale = Vector3.one * (age < 0.25f ? Mathf.Lerp(1.7f, 1f, Tween.OutCubic(age / 0.25f)) : 1f);
            }
            if (resultsTime > 3f && count >= 1f)
            {
                resultsTime = -1f;
            }
        }


        private static string Goal(bool reached, string text)
        {
            return reached ? $"<color=#8CFF6A>+ {text}</color>" : $"<color=#FFFFFF66>- {text}</color>";
        }

        #endregion


        /// <summary>The icon of <paramref name="item"/>: the active theme's, else the scene's (null when neither has one).</summary>
        internal Sprite ItemSprite(StrikeItem item)
        {
            int index = (int)item;
            return AsteroidsUI.Themed(theme => theme.StrikeIcon(item.ToString()), index >= 0 && index < itemSprites.Length ? itemSprites[index] : null);
        }


        private static Color EnergyColor(float fraction)
        {
            return fraction > 0.5f ? new Color(0.35f, 1f, 0.55f) : fraction > 0.25f ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 0.3f, 0.25f);
        }


        private static void SetStars(Image[] stars, int count, Sprite full, Sprite empty)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    stars[i].sprite = i < count ? full : empty;
                }
            }
        }


        private void SetStars(Image[] stars, int count)
        {
            SetStars(stars, count, StarFull, StarEmpty);
        }


        private static void SetActive(Component component, bool active)
        {
            if (component != null && component.gameObject.activeSelf != active)
            {
                component.gameObject.SetActive(active);
            }
        }


        private static void SetLabel(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }


        /// <summary>The finest step a HUD bar shows: finer than a pixel on any bar the HUD has.</summary>
        internal const float FillStep = 1f / 512f;


        /// <summary>
        /// Moves a HUD bar toward its target, the target snapped to <see cref="FillStep"/>. A value that creeps a little
        /// every frame (the mission progress) then only changes the bar when it crosses a step, and an unchanged bar is
        /// not written: every write dirties the canvas the whole HUD shares.
        /// </summary>
        internal static void StepFill(Image fill, float target, float maxDelta)
        {
            if (fill == null)
            {
                return;
            }
            float next = StepFill(fill.fillAmount, target, maxDelta);
            if (next != fill.fillAmount)
            {
                fill.fillAmount = next;
            }
        }


        /// <summary>The next fill of a HUD bar at <paramref name="current"/> (see <see cref="StepFill(Image, float, float)"/>).</summary>
        internal static float StepFill(float current, float target, float maxDelta)
        {
            float goal = Mathf.Round(Mathf.Clamp01(target) / FillStep) * FillStep;
            return Mathf.MoveTowards(current, goal, maxDelta);
        }
    }
}
