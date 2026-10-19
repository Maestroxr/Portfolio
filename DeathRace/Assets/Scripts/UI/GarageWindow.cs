using System.Collections.Generic;
using Gamebox;
using Gamebox.Economy;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.DeathRace.RaceWindowParts;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The hub of a career, on BaseGame's <see cref="ShopWindow"/>: the league, the driver, the day, the rank and the
    /// money across the top, and six pages. RACE: the offers of the day as big cards (difficulty, track and its map,
    /// laps, the three rivals with their cars and ranks, the prizes) to enter. GARAGE: the car (its picture, stat bars
    /// with what the next upgrade adds, its gun and the special weapon it fires first, its health with Repair All and
    /// Repair Half) and its five upgrades: engine, tyres, armour, nitro system and gun mount. WORKSHOP (the shop's own
    /// grid): the seven fittings and the guns (a gun bought is mounted; one the car has is mounted again for nothing).
    /// MARKET (the shop's grid): mines, rockets, nitro, the three special weapons (with Arm, which picks the one the
    /// Special button fires first) and sabotage of a rival of the day (a picker of the drivers racing today). DEALER
    /// (the shop's grid): the cars of the league's tiers, what the current one trades in for and what the player pays.
    /// STANDINGS: the ladder of twenty, with the league's champion above it. Everything is bought through
    /// <see cref="CareerShop"/>; leaving the garage after buying saves the career.
    /// </summary>
    public sealed class GarageWindow : ShopWindow
    {
        public const string RacePage = "race";
        public const string GaragePage = CareerShop.GaragePage;
        public const string WorkshopPage = CareerShop.WorkshopPage;
        public const string MarketPage = CareerShop.MarketPage;
        public const string DealerPage = CareerShop.DealerPage;
        public const string StandingsPage = "standings";

        private static readonly string[] PageIds = { RacePage, GaragePage, WorkshopPage, MarketPage, DealerPage, StandingsPage };

        /// <summary>The names of the upgrades' icons in the theme, in <see cref="UpgradeKind"/> order (when it has no picture of its own).</summary>
        private static readonly string[] UpgradeIcons = { "Engine", "Tyres", "Armour", "Boost", "GunMount" };

        private RaceWindowParts ui;
        private CareerShop shop;
        private CareerState shopFor;
        private int targetDay;
        private bool spent;

        // The garage page.
        private sealed class PartRow
        {
            public ShopItem Item;
            public Image[] Pips;
            public TMP_Text Status;
            public TMP_Text Price;
            public Button Buy;
        }

        private readonly List<PartRow> parts = new List<PartRow>();
        private RaceWindowParts.StatBar speedBar, accelerationBar, handlingBar, armourBar, firepowerBar, nitroBar, healthBar;
        private TMP_Text healthText, armsText;
        private Button repairAll, repairHalf;

        // The market: the button over the details panel that arms the special weapon shown.
        private Button armButton;
        private ShopItem armShownFor;
        private SpecialKind armShownArmed;

        // The sabotage picker.
        private RectTransform picker;

        protected override string CanvasName => "DeathRaceGarage";

        protected override Vector2 PanelSize => new Vector2(1720f, 1000f);

        public static GarageWindow For(BaseGameManager manager)
        {
            return For<GarageWindow>(manager);
        }

        /// <summary>Opens the garage on <paramref name="page"/> (null: the page shown last) over the menu.</summary>
        public new void Show(string page = null)
        {
            if (State == null)
            {
                return;
            }
            spent = false;
            base.Show(page);
            Game?.Interface?.Cover(this);
        }

        public override void Hide()
        {
            bool shown = IsOpen;
            ClosePicker();
            base.Hide();
            Game?.Interface?.Uncover(this);
            if (shown && spent && Game != null && !Game.IsRacing)
            {
                // What was bought this visit is kept even if the game is closed now.
                Game.Autosave();
            }
            spent = false;
        }

        protected override void OnDestroy()
        {
            Game?.Interface?.Uncover(this);
            base.OnDestroy();
        }

        /// <summary>Escape: the sabotage picker closes first, then any page goes back to the races, then the garage closes.</summary>
        protected override bool HandleBack()
        {
            if (!IsOpen)
            {
                return false;
            }
            if (picker != null)
            {
                ClosePicker();
                return true;
            }
            if (CurrentPage != RacePage)
            {
                ShowPage(RacePage);
                return true;
            }
            return base.HandleBack();
        }

        private DeathRaceManager Game => manager as DeathRaceManager;

        private CareerState State => Game != null ? Game.CurrentCareer : null;

        private DeathRaceTheme Theme => Game != null ? Game.Look : null;

        // ------------------------------------------------------------------------------------------------
        // The shop
        // ------------------------------------------------------------------------------------------------

        protected override Wallet Wallet => State?.wallet;

        protected override Inventory Inventory => State?.consumables;

        /// <summary>The career's shop, made again for a new car, new armour or another career.</summary>
        protected override Shop Shop => CareerShopOf();

        private CareerShop CareerShopOf()
        {
            CareerState state = State;
            if (state == null)
            {
                return null;
            }
            if (shop == null || shopFor != state || shop.IsStale)
            {
                string target = shop != null && shopFor == state ? shop.SabotageTarget : null;
                shop = new CareerShop(state);
                shopFor = state;
                shop.SabotageTarget = target;
            }
            if (targetDay != state.day || shop.SabotageTarget == null)
            {
                // A new day races other drivers: aim at one of them.
                targetDay = state.day;
                List<string> targets = shop.SabotageTargets();
                shop.SabotageTarget = shop.SabotageTarget != null && targets.Contains(shop.SabotageTarget)
                    ? shop.SabotageTarget
                    : DefaultTarget(state, targets);
            }
            return shop;
        }

        /// <summary>The sabotage on the shelf is aimed at the best driver of the hardest race of the day until the player picks another.</summary>
        private static string DefaultTarget(CareerState state, List<string> targets)
        {
            string best = null;
            int bestRank = int.MaxValue;
            foreach (string id in targets)
            {
                int rank = state.League.IsChampion(id) ? 0 : Ladder.RankOf(state, id);
                if (rank < bestRank)
                {
                    bestRank = rank;
                    best = id;
                }
            }
            return best;
        }

        protected override IReadOnlyList<string> Pages => PageIds;

        protected override string PageTitle(string page)
        {
            switch (page)
            {
                case RacePage: return L("Races");
                case GaragePage: return L("Garage");
                case WorkshopPage: return L("Workshop");
                case MarketPage: return L("Black Market");
                case DealerPage: return L("Dealer");
                default: return L("Standings");
            }
        }

        /// <summary>The workshop, the market and the dealer are the shop's own grids; the races, the garage and the standings are drawn here.</summary>
        protected override bool IsCatalogPage(string page)
        {
            return page == WorkshopPage || page == MarketPage || page == DealerPage;
        }

        protected override string WindowTitle
        {
            get
            {
                CareerState state = State;
                return state != null ? LF("{0}'s Garage", state.driverName) : L("Garage");
            }
        }

        protected override string WindowHint
        {
            get
            {
                CareerState state = State;
                if (state == null)
                {
                    return "";
                }
                string line = LF("{0}  ·  Day {1}  ·  Rank {2} of {3}  ·  {4} points  ·  {5}", Portfolio.DeathRace.L.Data(state.League.Name), state.day,
                    RaceFormat.Rank(Ladder.Rank(state)), CareerRules.LadderSize, state.points, CarName(state.car));
                return state.champion ? line + "  ·  " + L("Champion") : line;
            }
        }

        protected override string FormatMoney(int amount)
        {
            return RaceFormat.Money(amount);
        }

        protected override Sprite IconOf(ShopItem item)
        {
            return IconFor(Theme, item);
        }

        /// <summary>
        /// The picture of a career shop item in <paramref name="theme"/>: a car's picture, an upgrade's own picture, the
        /// HUD's icons of the mines, rockets and nitro, else the named icon of the item (a fitting, a gun, a special
        /// weapon). Null while the theme has none, and the windows show the item's initial instead. The cross-game shop's
        /// pages of the career use it too.
        /// </summary>
        public static Sprite IconFor(DeathRaceTheme theme, ShopItem item)
        {
            if (theme == null || item == null)
            {
                return null;
            }
            switch (item.Tag)
            {
                case CarModel model:
                    return theme.CarPicture(model);
                case UpgradeKind kind:
                    return UpgradePicture(theme, kind);
            }
            switch (item.Id)
            {
                case CareerShop.MineId: return theme.iconMine;
                case CareerShop.RocketId: return theme.iconRocket;
                case CareerShop.NitroId: return theme.iconNitro;
                case CareerShop.SabotageId: return Named(theme, "Sabotage", theme.iconSkull);
                case CareerShop.RepairId: return Named(theme, "Repair", theme.iconRepair);
                default: return Named(theme, item.Icon);
            }
        }

        /// <summary>The picture of an upgrade: the look's own per kind, else its named icon.</summary>
        private static Sprite UpgradePicture(DeathRaceTheme theme, UpgradeKind kind)
        {
            Sprite[] pictures = theme.upgradePictures;
            int index = (int)kind;
            Sprite picture = pictures != null && index < pictures.Length ? pictures[index] : null;
            return picture != null ? picture : Named(theme, UpgradeIcons[Mathf.Clamp(index, 0, UpgradeIcons.Length - 1)]);
        }

        private static Sprite Named(DeathRaceTheme theme, string key, Sprite fallback = null)
        {
            Sprite sprite = string.IsNullOrEmpty(key) ? null : theme.SpriteOf(key);
            return sprite != null ? sprite : fallback;
        }

        /// <summary>The line under the description: a car's trade, a gun's numbers on this car, what the player carries, the sabotage's target.</summary>
        protected override string DetailOf(ShopItem item)
        {
            CareerState state = State;
            CareerShop rules = CareerShopOf();
            if (state == null || rules == null)
            {
                return null;
            }
            switch (item.Tag)
            {
                case CarModel model:
                    if (model == state.car)
                    {
                        return LF("Your car. The dealer gives {0} for it.", RaceFormat.Money(rules.TradeInValue));
                    }
                    return LF("{0} less {1} for your {2}", RaceFormat.Money(CarSpecs.Of(model).Price), RaceFormat.Money(rules.TradeInValue),
                        CarName(state.car));
                case GunKind gun:
                    CarLoadout armed = state.Current.loadout.Clone();
                    armed.GiveGun(gun);
                    CarStats stats = armed.Stats();
                    return LF("{0} damage a second, {1} rounds", RaceFormat.Number(Mathf.RoundToInt(Firepower(stats))), RaceFormat.Number(stats.AmmoCapacity));
                case SpecialKind special:
                    string carried = LF("You carry {0}", RaceFormat.OutOf(state.consumables.Get(item.Id), item.Max));
                    return special == state.armed ? carried + "  ·  " + L("fires first") : carried;
            }
            switch (item.Id)
            {
                case CareerShop.MineId:
                case CareerShop.RocketId:
                case CareerShop.NitroId:
                    return LF("You carry {0}", RaceFormat.OutOf(state.consumables.Get(item.Id), item.Max));
                case CareerShop.SabotageId:
                    if (state.HasSabotage)
                    {
                        return LF("{0} starts the next race hurt.", DriverName(state, state.sabotage));
                    }
                    return rules.SabotageTarget != null ? LF("Target: {0}. Buy to pick another.", DriverName(state, rules.SabotageTarget)) : null;
            }
            return null;
        }

        protected override string StatusText(ShopItem item, int owned, BuyResult result)
        {
            switch (item.Tag)
            {
                case CarModel model:
                    if (owned > 0)
                    {
                        return L("YOUR CAR");
                    }
                    return result == BuyResult.Locked ? L(CareerShop.BetterCarReason) : LF("Tier {0}", CarSpecs.Of(model).Tier);
                case GunKind gun:
                    GunSpec spec = Guns.Of(gun);
                    return LF("{0} a second  ·  {1} m", RaceFormat.Number(Mathf.RoundToInt(spec.RoundsPerSecond)), RaceFormat.Number(Mathf.RoundToInt(spec.Range)));
                case EquipmentKind _:
                    // FITTED shows on the price label of a fitting on the car.
                    return "";
                case SpecialKind special:
                    string count = base.StatusText(item, owned, result);
                    return State != null && special == State.armed ? count + "  ·  " + L("ARMED") : count;
            }
            if (item.Id == CareerShop.SabotageId)
            {
                return owned > 0 ? L("Done for today") : LF("{0} racing today", Shop != null ? CareerShopOf().SabotageTargets().Count : 0);
            }
            return base.StatusText(item, owned, result);
        }

        protected override string CaptionOf(ShopItem item, int owned, BuyResult result)
        {
            if (item.Tag is CarModel && result != BuyResult.Full && result != BuyResult.Locked)
            {
                return L("You pay");
            }
            if (item.Tag is GunKind gun && result != BuyResult.Full && CareerShopOf() != null && CareerShopOf().HasGun(gun))
            {
                return L("Yours: mount it again");
            }
            if (item.Id == CareerShop.SabotageId && result == BuyResult.Bought)
            {
                return LF("Sabotage {0}", DriverName(State, CareerShopOf().SabotageTarget));
            }
            return base.CaptionOf(item, owned, result);
        }

        /// <summary>A mounted gun says MOUNTED, one the car has FREE; a fitting on the car FITTED; the rest as the shop window says.</summary>
        protected override void PriceLabel(ShopItem item, int owned, BuyResult result, out string text, out Color color, out Color back)
        {
            OverlayLook look = kit.Look;
            if (item.Tag is GunKind gun)
            {
                if (owned > 0)
                {
                    text = L("MOUNTED");
                    color = look.good;
                    back = Faded(look.good, 0.22f);
                    return;
                }
                if (CareerShopOf() != null && CareerShopOf().HasGun(gun))
                {
                    text = L("FREE");
                    color = look.good;
                    back = Darker(look.row, 0.3f);
                    return;
                }
            }
            if (item.Tag is EquipmentKind && owned > 0)
            {
                text = L("FITTED");
                color = look.good;
                back = Faded(look.good, 0.22f);
                return;
            }
            base.PriceLabel(item, owned, result, out text, out color, out back);
        }

        protected override string BuyLabel(ShopItem item)
        {
            switch (item.Tag)
            {
                case GunKind gun:
                    CareerShop rules = CareerShopOf();
                    if (rules != null && rules.Owned(Inventory, item) > 0)
                    {
                        return L("MOUNTED");
                    }
                    return rules != null && rules.HasGun(gun) ? L("Mount") : L("Buy and Mount");
                case EquipmentKind _:
                    return L("Fit");
            }
            return item.Id == CareerShop.SabotageId ? L("Pick a Driver") : base.BuyLabel(item);
        }

        /// <summary>Sabotage asks whom first; a car asks nothing but tells what happened to the old one; a gun says it is mounted.</summary>
        protected override void Buy(ShopItem item)
        {
            // The rows of the garage page keep the item of the shop they were built with; buy the current shop's.
            CareerShop rules = CareerShopOf();
            item = item != null && rules != null ? rules.Catalog[item.Id] ?? item : item;
            if (item != null && item.Id == CareerShop.SabotageId && State != null && !State.HasSabotage)
            {
                OpenPicker();
                return;
            }
            CarModel before = State != null ? State.car : CarModel.Rustbucket;
            int tradeIn = rules != null ? rules.TradeInValue : 0;
            base.Buy(item);
            if (item != null && item.Tag is CarModel model && State != null && State.car == model && model != before)
            {
                ShowMessage(LF("The {0} is yours. The dealer took your {1} for {2}.", CarName(model), CarName(before), RaceFormat.Money(tradeIn)), false);
            }
            else if (item != null && item.Tag is GunKind gun && State != null && State.Current.loadout.gun == gun)
            {
                ShowMessage(LF("The {0} is on the bonnet.", L(Guns.Of(gun).Name)), false);
            }
        }

        protected override void OnBought(ShopItem item, BuyResult result)
        {
            if (result == BuyResult.Bought)
            {
                spent = true;
                Game?.Sounds?.Buy();
            }
            else
            {
                Game?.Sounds?.Deny();
            }
        }

        protected override void OnSold(ShopItem item, int money)
        {
            spent = true;
            Game?.Sounds?.Sell();
        }

        // ------------------------------------------------------------------------------------------------
        // Building
        // ------------------------------------------------------------------------------------------------

        protected override void BuildContent()
        {
            ui = new RaceWindowParts(kit, Theme, L);
            picker = null;
            base.BuildContent();
            OverlayLook look = kit.Look;
            ui.ButtonAt(panel, "Leave Garage", look.row, new Vector2(1f, 0f), new Vector2(-48f, 28f), new Vector2(260f, 64f), Hide);
            ui.ButtonAt(panel, "Save Career", look.row, new Vector2(1f, 0f), new Vector2(-324f, 28f), new Vector2(260f, 64f), SaveCareer);
            // Arm sits in the top corner of the details panel of the market (beside the picture), for a special weapon.
            armButton = ui.ButtonAt(panel, "Arm", look.accent, new Vector2(1f, 1f), new Vector2(-64f, -250f), new Vector2(170f, 56f), ArmShown);
            armButton.gameObject.SetActive(false);
            armShownFor = null;
        }

        protected override void Teardown()
        {
            parts.Clear();
            picker = null;
            armButton = null;
            armShownFor = null;
            base.Teardown();
        }

        protected override void BuildPage(string page, RectTransform area)
        {
            parts.Clear();
            switch (page)
            {
                case RacePage:
                    BuildRaces(area);
                    break;
                case GaragePage:
                    BuildGarage(area);
                    break;
                case StandingsPage:
                    BuildStandings(area);
                    break;
            }
        }

        protected override void RefreshPage(string page)
        {
            if (page == GaragePage)
            {
                RefreshGarage();
            }
        }

        protected override void Refresh()
        {
            base.Refresh();
            RefreshArm();
        }

        protected override void Update()
        {
            base.Update();
            if (IsOpen && (Selected != armShownFor || (State != null && State.armed != armShownArmed)))
            {
                // The details panel changes with the card picked, without a refresh of the window.
                RefreshArm();
            }
        }

        /// <summary>For the career check: turns to <paramref name="page"/> as its tab would.</summary>
        internal void TurnTo(string page)
        {
            ShowPage(page);
        }

        /// <summary>For the career check: presses Buy on <paramref name="id"/> as the details panel would (sabotage opens the picker).</summary>
        internal void Press(string id)
        {
            ShopItem item = CareerShopOf()?.Catalog[id];
            if (item != null)
            {
                Buy(item);
            }
        }

        /// <summary>For the career check: picks the card of <paramref name="id"/> on its page, as a click on it would.</summary>
        internal void Pick(string id)
        {
            ShopItem item = CareerShopOf()?.Catalog[id];
            if (item != null)
            {
                Select(item);
                RefreshArm();
            }
        }

        /// <summary>For the career check: presses Arm on the special weapon shown in the market.</summary>
        internal void PressArm()
        {
            ArmShown();
        }

        /// <summary>Whether the sabotage picker is open (the career check looks).</summary>
        internal bool IsPicking => picker != null;

        private void SaveCareer()
        {
            if (Game == null)
            {
                return;
            }
            Game.ShowSavedGames(true);
        }

        // ------------------------------------------------------------------------------------------------
        // RACE: the offers of the day
        // ------------------------------------------------------------------------------------------------

        private void BuildRaces(RectTransform area)
        {
            CareerState state = State;
            if (state == null)
            {
                return;
            }
            int count = state.offers.Count;
            const float gap = 20f;
            float width = area.rect.width > 0f ? area.rect.width : PanelSize.x - 96f;
            float cardWidth = (width - gap * (count - 1)) / Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                RectTransform card = ui.Box(area, "Offer", kit.Look.row);
                Span(card, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(i * (cardWidth + gap), 0f), new Vector2(i * (cardWidth + gap) + cardWidth, 0f));
                BuildOffer(card, state, i, cardWidth);
            }
        }

        private void BuildOffer(RectTransform card, CareerState state, int index, float width)
        {
            OverlayLook look = kit.Look;
            LeagueSpec league = state.League;
            RaceOffer offer = state.offers[index];
            Color color = DifficultyColor(offer.difficulty);
            bool narrow = width < 440f;

            // The band across the top: the difficulty and the winner's prize.
            RectTransform band = ui.Fill(card, "Band", color);
            Span(band, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -58f), Vector2.zero);
            ui.WordsAt(band, ui.DifficultyName(offer.difficulty).ToUpperInvariant(), 30f, Color.Lerp(color, Color.black, 0.75f),
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(width * 0.55f, 46f), FontStyles.Bold, true);
            ui.WordsAt(band, RaceFormat.Money(offer.Prize(league, 1)), 30f, Color.Lerp(color, Color.black, 0.75f), TextAlignmentOptions.MidlineRight,
                new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(width * 0.42f, 46f), FontStyles.Bold, true);

            // The track: its name, laps and map, and the prizes beside the map.
            ui.WordsAt(card, TrackName(offer.track), 28f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(20f, -66f),
                new Vector2(width - 140f, 40f), FontStyles.Bold, true);
            ui.WordsAt(card, Portfolio.DeathRace.L.Laps(offer.laps), 22f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-20f, -66f),
                new Vector2(110f, 40f));
            float map = narrow ? 150f : 188f;
            ui.TrackMap(card, offer.track, new Vector2(0f, 1f), new Vector2(16f, -112f), new Vector2(map, map));
            float prizesX = map + 32f;
            float prizesWidth = width - prizesX - 20f;
            for (int place = 1; place <= CareerRules.CarsPerRace; place++)
            {
                float y = -116f - (place - 1) * 34f;
                int prize = offer.Prize(league, place);
                ui.WordsAt(card, Gamebox.Online.Standings.Ordinal(place), 22f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                    new Vector2(prizesX, y), new Vector2(prizesWidth * 0.4f, 32f));
                ui.WordsAt(card, prize > 0 ? RaceFormat.Money(prize) : "-", 24f, place == 1 ? ui.Accent : look.text, TextAlignmentOptions.MidlineRight,
                    new Vector2(0f, 1f), new Vector2(prizesX + prizesWidth * 0.4f, y), new Vector2(prizesWidth * 0.6f, 32f), FontStyles.Bold);
            }
            ui.WordsAt(card, LF("{0} a wreck", RaceFormat.Money(CareerRules.Bounty(league, offer.difficulty))), 20f, look.muted, TextAlignmentOptions.MidlineRight,
                new Vector2(0f, 1f), new Vector2(prizesX, -116f - 4 * 34f - 4f), new Vector2(prizesWidth, 30f), FontStyles.Italic);
            string points = offer.IsShowdown ? L("For the title") : LF("{0} points to the winner", CareerRules.Points(league, offer.difficulty, 1));
            ui.WordsAt(card, points, 20f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(0f, 1f),
                new Vector2(prizesX, -116f - 5 * 34f + 2f), new Vector2(prizesWidth, 30f), FontStyles.Italic);

            // The rivals.
            float top = -112f - map - 18f;
            ui.WordsAt(card, L("Against"), 20f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(20f, top),
                new Vector2(width - 40f, 28f));
            for (int i = 0; i < offer.rivals.Count; i++)
            {
                BuildRival(card, state, offer.rivals[i], top - 34f - i * 50f, width);
            }

            string enter = offer.IsShowdown ? LF("Face {0}", DriverName(state, league.Champion.Id)) : L("Enter Race");
            Button button = ui.ButtonAt(card, enter, color, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(width - 32f, 68f), () => Enter(index), true);
            TMP_Text label = OverlayKit.LabelOf(button);
            if (label != null)
            {
                label.color = Color.Lerp(color, Color.black, 0.8f);
            }
        }

        private void BuildRival(RectTransform card, CareerState state, string id, float y, float width)
        {
            OverlayLook look = kit.Look;
            LeagueSpec league = state.League;
            bool champion = league.IsChampion(id);
            RivalState rival = state.Rival(id);
            ui.Chip(card, ui.DriverColor(state, id), new Vector2(0f, 1f), new Vector2(20f, y - 6f), 30f);
            bool sabotaged = state.HasSabotage && state.sabotage == id;
            ui.WordsAt(card, DriverName(state, id), 24f, champion ? ui.Accent : look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(62f, y), new Vector2(width * 0.52f - 66f, 42f), FontStyles.Bold);
            CarModel car = champion ? league.ChampionCar : rival != null ? rival.Car : CarModel.Rustbucket;
            string detail = sabotaged ? L("SABOTAGED") : CarName(car);
            ui.WordsAt(card, detail, 20f, sabotaged ? ui.Warn : look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(width * 0.54f, y), new Vector2(width * 0.28f, 42f), sabotaged ? FontStyles.Bold : FontStyles.Normal);
            string rank = champion ? L("Champion") : RaceFormat.Rank(Ladder.RankOf(state, id));
            ui.WordsAt(card, rank, 22f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-20f, y),
                new Vector2(width * 0.16f, 42f), FontStyles.Bold);
        }

        private void Enter(int index)
        {
            CareerState state = State;
            if (state == null || Game == null)
            {
                return;
            }
            Game.Sounds?.Click();
            Game.EnterOffer(index);
        }

        // ------------------------------------------------------------------------------------------------
        // GARAGE: the car and its upgrades
        // ------------------------------------------------------------------------------------------------

        private void BuildGarage(RectTransform area)
        {
            CareerState state = State;
            CareerShop rules = CareerShopOf();
            if (state == null || rules == null)
            {
                return;
            }
            OverlayLook look = kit.Look;
            CarSpec spec = CarSpecs.Of(state.car);

            // The car on the left.
            RectTransform car = ui.Box(area, "Car", look.row);
            Span(car, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(640f, 0f));
            Picture(car, ui.CarPicture(state.car), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(480f, 180f));
            ui.WordsAt(car, CarName(state.car), 36f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, -192f),
                new Vector2(420f, 48f), FontStyles.Bold, true);
            ui.WordsAt(car, LF("Tier {0}", spec.Tier), 22f, ui.Accent, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, -192f),
                new Vector2(160f, 48f), FontStyles.Bold);
            speedBar = StatLine(car, "Speed", -248f);
            accelerationBar = StatLine(car, "Acceleration", -280f);
            handlingBar = StatLine(car, "Handling", -312f);
            armourBar = StatLine(car, "Armor", -344f);
            firepowerBar = StatLine(car, "Firepower", -376f);
            nitroBar = StatLine(car, "Nitro", -408f);

            // What the car fights with: the gun on the bonnet and the special weapon fired first.
            armsText = ui.WordsAt(car, "", 21f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, -446f),
                new Vector2(592f, 32f));

            // The health and the repairs.
            ui.WordsAt(car, L("Health"), 22f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, -490f),
                new Vector2(170f, 32f));
            healthBar = ui.Bar(car, look.good, new Vector2(0f, 1f), new Vector2(200f, -497f), new Vector2(300f, 20f));
            healthText = ui.WordsAt(car, "", 22f, look.text, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, -490f),
                new Vector2(100f, 32f), FontStyles.Bold);
            repairAll = ui.ButtonAt(car, "Repair All", look.good, new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(290f, 68f), () => Repair(true));
            repairHalf = ui.ButtonAt(car, "Repair Half", look.accent, new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(290f, 68f), () => Repair(false));

            // The five upgrades on the right (the fittings and the guns are the workshop's).
            RectTransform list = Area(area, "Parts");
            Span(list, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(664f, 0f), Vector2.zero);
            const float gap = 10f;
            float height = area.rect.height > 0f ? area.rect.height : PanelSize.y - 336f;
            float rowHeight = Mathf.Min(112f, (height - gap * (CareerRules.Upgrades.Length - 1)) / CareerRules.Upgrades.Length);
            float y = 0f;
            foreach (UpgradeKind kind in CareerRules.Upgrades)
            {
                parts.Add(BuildPart(list, rules.Catalog[CareerShop.UpgradeId(kind)], y, rowHeight));
                y -= rowHeight + gap;
            }
        }

        private RaceWindowParts.StatBar StatLine(RectTransform parent, string label, float y)
        {
            ui.WordsAt(parent, L(label), 22f, kit.Look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, y),
                new Vector2(170f, 30f));
            return ui.Bar(parent, ui.Accent, new Vector2(0f, 1f), new Vector2(200f, y - 7f), new Vector2(412f, 16f));
        }

        private PartRow BuildPart(RectTransform list, ShopItem item, float y, float height)
        {
            OverlayLook look = kit.Look;
            var row = new PartRow { Item = item };
            RectTransform box = ui.Box(list, item.Id, look.row);
            Span(box, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - height), new Vector2(0f, y));
            float icon = height - 28f;
            Sprite picture = IconOf(item);
            if (picture != null)
            {
                Picture(box, picture, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(icon, icon));
            }
            else
            {
                Image well = ui.Chip(box, Color.Lerp(look.row, Color.black, 0.25f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), icon);
                ui.WordsAt(well.rectTransform, L(item.Title).Substring(0, 1), icon * 0.5f, look.muted, TextAlignmentOptions.Midline,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(icon, icon), FontStyles.Bold, true);
            }
            float left = icon + 34f;
            ui.WordsAt(box, L(item.Title), 28f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(left, -10f),
                new Vector2(390f, 40f), FontStyles.Bold, true);
            // The description stops short of the price (MAX is a long word in some languages).
            TMP_Text words = ui.WordsAt(box, L(item.Description), 19f, look.muted, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f),
                new Vector2(left, -50f), new Vector2(380f, height - 56f));
            words.fontSizeMin = 16f;
            words.textWrappingMode = TextWrappingModes.Normal;
            row.Pips = new Image[item.Max];
            for (int i = 0; i < item.Max; i++)
            {
                row.Pips[i] = Shape(box, RaceShapes.Dot, look.muted, new Vector2(1f, 1f), new Vector2(-250f - (item.Max - 1 - i) * 28f, -18f),
                    new Vector2(24f, 24f));
            }
            row.Status = ui.WordsAt(box, "", 20f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, -12f),
                new Vector2(210f, 30f), FontStyles.Bold);
            row.Price = ui.WordsAt(box, "", 28f, look.good, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f), new Vector2(-250f, -14f),
                new Vector2(200f, 40f), FontStyles.Bold, true);
            row.Buy = ui.ButtonAt(box, "Upgrade", look.good, new Vector2(1f, 0.5f), new Vector2(-24f, -14f), new Vector2(210f, 60f), () => Buy(item));
            return row;
        }

        private void RefreshGarage()
        {
            CareerState state = State;
            CareerShop rules = CareerShopOf();
            if (state == null || rules == null || speedBar == null)
            {
                return;
            }
            OverlayLook look = kit.Look;
            CarLoadout car = state.Current.loadout;
            CarStats stats = car.Stats();
            CarStats engine = Next(car, UpgradeKind.Engine).Stats();
            CarStats tires = Next(car, UpgradeKind.Tires).Stats();
            CarStats armor = Next(car, UpgradeKind.Armor).Stats();
            CarStats boost = Next(car, UpgradeKind.Boost).Stats();
            CarStats mount = Next(car, UpgradeKind.GunMount).Stats();
            speedBar.Set(CarBars.Speed(stats), CarBars.Speed(engine));
            accelerationBar.Set(CarBars.Acceleration(stats), CarBars.Acceleration(engine));
            handlingBar.Set(CarBars.Handling(stats), CarBars.Handling(tires));
            armourBar.Set(CarBars.Armour(stats), CarBars.Armour(armor));
            firepowerBar.Set(Firepower(stats) / BestFirepower, Firepower(mount) / BestFirepower);
            nitroBar.Set(Nitro(stats) / BestNitro, Nitro(boost) / BestNitro);
            healthBar.Set(car.health);
            healthBar.Fill.color = car.health < 0.35f ? ui.Warn : car.health < 0.7f ? ui.Accent : look.good;
            healthText.text = RaceFormat.Percent(car.health);
            SpecialKind armed = state.armed;
            int specials = 0;
            foreach (SpecialSpec special in Specials.All)
            {
                specials += state.consumables.Get(special.Id);
            }
            string gun = L(Guns.Of(stats.Gun).Name);
            armsText.text = specials > 0
                ? LF("{0}  ·  {1} first ({2})", gun, L(Specials.Of(armed).Name), RaceFormat.Number(state.consumables.Get(CareerShop.SpecialId(armed))))
                : LF("{0}  ·  no special weapons", gun);

            int missing = Career.MissingPoints(car);
            int unit = rules.Catalog[CareerShop.RepairId].Cost(1);
            Relabel(repairAll, missing > 0 ? LF("Repair All {0}", RaceFormat.Money(missing * unit)) : L("In One Piece"), missing > 0);
            int half = (missing + 1) / 2;
            Relabel(repairHalf, missing > 1 ? LF("Repair Half {0}", RaceFormat.Money(half * unit)) : L("Repair Half"), missing > 1);

            foreach (PartRow row in parts)
            {
                ShopItem item = rules.Catalog[row.Item.Id];
                int owned = rules.Owned(state.consumables, item);
                BuyResult result = rules.CanBuy(state.wallet, state.consumables, item, out string reason);
                int price = rules.NextPrice(state.consumables, item);
                for (int i = 0; i < row.Pips.Length; i++)
                {
                    row.Pips[i].sprite = i < owned ? RaceShapes.Dot : RaceShapes.Ring;
                    row.Pips[i].color = i < owned ? ui.Accent : look.muted;
                }
                row.Status.text = LF("Level {0} of {1}", owned, item.Max);
                row.Price.text = price < 0 ? L("MAX") : RaceFormat.Money(price);
                row.Price.color = price < 0 ? ui.Accent : state.wallet.CanAfford(price) ? look.good : ui.Warn;
                row.Buy.gameObject.SetActive(price >= 0);
                row.Buy.interactable = result == BuyResult.Bought;
            }
        }

        /// <summary>The car with one more level of <paramref name="kind"/> (as it is when the part is at its maximum).</summary>
        private static CarLoadout Next(CarLoadout car, UpgradeKind kind)
        {
            CarLoadout next = car.Clone();
            next.SetLevel(kind, car.Level(kind) + 1);
            return next;
        }

        /// <summary>What the gun of a car deals in a second with the trigger held (before armour): its rounds, the mount and the rounds' piercing.</summary>
        private static float Firepower(CarStats stats)
        {
            return Guns.Of(stats.Gun).DamagePerSecond * stats.GunDamage * stats.GunRate;
        }

        /// <summary>How much a nitro charge gives: how long it burns times how hard it pushes.</summary>
        private static float Nitro(CarStats stats)
        {
            return stats.NitroSeconds * stats.NitroPower;
        }

        /// <summary>The firepower and the nitro of the strongest champion's car: the ends of those two bars.</summary>
        private static readonly float BestFirepower = Best(Firepower);

        private static readonly float BestNitro = Best(Nitro);

        private static float Best(System.Func<CarStats, float> measure)
        {
            float best = 0.001f;
            foreach (LeagueSpec league in Leagues.All)
            {
                best = Mathf.Max(best, measure(Career.ChampionLoadout(league).Stats()));
            }
            return best;
        }

        private void Repair(bool all)
        {
            CareerShop rules = CareerShopOf();
            if (rules == null)
            {
                return;
            }
            int bought = all ? rules.RepairAll() : rules.RepairHalf();
            if (bought > 0)
            {
                spent = true;
                Game?.Sounds?.Buy();
                ShowMessage(LF("Repaired {0} points.", bought), false);
            }
            else
            {
                Game?.Sounds?.Deny();
                ShowMessage(L(Shop.NoMoneyReason));
            }
            RefreshShown();
        }

        // ------------------------------------------------------------------------------------------------
        // MARKET: the special weapon fired first, and whom to sabotage
        // ------------------------------------------------------------------------------------------------

        /// <summary>Shows Arm over the details panel while a special weapon is picked in the market: ARMED when it is the one fired first.</summary>
        private void RefreshArm()
        {
            if (armButton == null)
            {
                return;
            }
            ShopItem item = CurrentPage == MarketPage ? Selected : null;
            armShownFor = Selected;
            armShownArmed = State != null ? State.armed : SpecialKind.Oil;
            bool shown = item != null && item.Tag is SpecialKind && State != null && picker == null;
            armButton.gameObject.SetActive(shown);
            if (!shown)
            {
                return;
            }
            bool armed = (SpecialKind)item.Tag == State.armed;
            Relabel(armButton, armed ? L("ARMED") : L("Arm"), !armed);
            armButton.transform.SetAsLastSibling();
        }

        /// <summary>Makes the special weapon shown in the market the one the Special button fires first.</summary>
        private void ArmShown()
        {
            CareerShop rules = CareerShopOf();
            if (rules == null || !(Selected?.Tag is SpecialKind kind))
            {
                return;
            }
            rules.Arm(kind);
            spent = true;
            Game?.Sounds?.Click();
            ShowMessage(LF("The {0} fires first.", L(Specials.Of(kind).Name)), false);
            Refresh();
        }

        /// <summary>A panel over the market with every driver racing today, the race they are in and what sabotaging them costs.</summary>
        private void OpenPicker()
        {
            CareerState state = State;
            CareerShop rules = CareerShopOf();
            if (state == null || rules == null)
            {
                return;
            }
            ClosePicker();
            OverlayLook look = kit.Look;
            List<string> targets = rules.SabotageTargets();
            float rowHeight = 56f;
            float height = 150f + targets.Count * (rowHeight + 6f);
            picker = kit.Panel(panel, "Sabotage", Color.Lerp(look.window, Color.black, 0.2f), new Vector2(760f, height), look.windowSprite);
            OverlayKit.Place(picker, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f));
            ui.WordsAt(picker, L("Whose car gets a visit tonight?"), 30f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(32f, -24f), new Vector2(620f, 44f), FontStyles.Bold, true);
            Button close = kit.MakeButton(picker, "Close", look.row, new Vector2(52f, 52f), ClosePicker, kit.Cross);
            OverlayKit.Anchor(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-24f, -20f), new Vector2(52f, 52f));
            ui.WordsAt(picker, LF("They start the race at {0} health.", RaceFormat.Percent(CareerRules.SabotageHealth)), 20f, look.muted,
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(32f, -70f), new Vector2(680f, 30f), FontStyles.Italic);
            for (int i = 0; i < targets.Count; i++)
            {
                string id = targets[i];
                int price = rules.SabotagePrice(id);
                Difficulty race = Difficulty.Easy;
                foreach (RaceOffer offer in state.offers)
                {
                    if (offer.rivals.Contains(id))
                    {
                        race = offer.difficulty;
                    }
                }
                float y = -112f - i * (rowHeight + 6f);
                RectTransform row = ui.Box(picker, id, look.row);
                Span(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, y - rowHeight), new Vector2(-24f, y));
                ui.Chip(row, ui.DriverColor(state, id), new Vector2(0f, 0.5f), new Vector2(14f, 0f), 28f);
                ui.WordsAt(row, DriverName(state, id), 24f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(54f, 0f),
                    new Vector2(300f, 40f), FontStyles.Bold);
                ui.WordsAt(row, ui.DifficultyName(race), 20f, DifficultyColor(race), TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f),
                    new Vector2(360f, 0f), new Vector2(130f, 40f), FontStyles.Bold);
                bool affordable = state.wallet.CanAfford(price);
                Button pick = ui.ButtonAt(row, RaceFormat.Money(price), affordable ? look.good : look.row, new Vector2(1f, 0.5f), new Vector2(-8f, 0f),
                    new Vector2(180f, 44f), () => Sabotage(id), true);
                pick.interactable = affordable;
            }
            RefreshArm();
        }

        private void Sabotage(string id)
        {
            CareerShop rules = CareerShopOf();
            if (rules == null)
            {
                return;
            }
            BuyResult result = rules.Sabotage(id);
            OnBought(rules.Catalog[CareerShop.SabotageId], result);
            ClosePicker();
            ShowMessage(result == BuyResult.Bought ? LF("{0} will start the next race hurt.", DriverName(State, id)) : L(Shop.NoMoneyReason),
                result != BuyResult.Bought);
            RefreshShown();
        }

        private void ClosePicker()
        {
            if (picker != null)
            {
                Destroy(picker.gameObject);
                picker = null;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // STANDINGS: the ladder
        // ------------------------------------------------------------------------------------------------

        private void BuildStandings(RectTransform area)
        {
            CareerState state = State;
            if (state == null)
            {
                return;
            }
            OverlayLook look = kit.Look;
            LeagueSpec league = state.League;
            // The champion above everyone.
            RectTransform champion = ui.Box(area, "Champion", look.row);
            Span(champion, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), Vector2.zero);
            ui.Highlight(champion, league.ChampionPaint, 0.55f);
            Introduces(champion, league.Champion.Id);
            Shape(champion, ui.Icon("Trophy", RaceShapes.Star), ui.Accent, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(38f, 38f));
            ui.WordsAt(champion, DriverName(state, league.Champion.Id), 30f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f),
                new Vector2(68f, 0f), new Vector2(400f, 48f), FontStyles.Bold, true);
            string crown = state.champion ? LF("Beaten by {0} on day {1}", state.driverName, state.championDay) : L("The champion. Reach first place for the Showdown.");
            ui.WordsAt(champion, crown, 22f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(480f, 0f),
                new Vector2(760f, 40f), FontStyles.Italic);
            ui.WordsAt(champion, CarName(league.ChampionCar), 22f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f), new Vector2(-20f, 0f),
                new Vector2(260f, 40f));

            ShowMessage(L("Pick a driver to see who they are."), false, true);
            List<LadderEntry> ladder = Ladder.Entries(state);
            int perColumn = (ladder.Count + 1) / 2;
            const float gap = 24f;
            float width = ((area.rect.width > 0f ? area.rect.width : PanelSize.x - 96f) - gap) / 2f;
            float rowHeight = 50f;
            for (int column = 0; column < 2; column++)
            {
                float x = column * (width + gap);
                float y = -82f;
                Head(area, x, y, width);
                for (int i = column * perColumn; i < Mathf.Min(ladder.Count, (column + 1) * perColumn); i++)
                {
                    y -= rowHeight + 4f;
                    Standing(area, ladder[i], x, y, width, rowHeight);
                }
            }
        }

        /// <summary>A click on a driver's line says who they are (their line of the roster) on the message line.</summary>
        private void Introduces(RectTransform row, string id)
        {
            Image back = row.GetComponent<Image>();
            back.raycastTarget = true;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.onClick.AddListener(() =>
            {
                RosterDriver driver = State != null ? State.League.Find(id) : Roster.Find(id);
                if (driver != null)
                {
                    ShowMessage(DriverName(State, id) + ": " + L(driver.Blurb), false, true);
                }
            });
        }

        private void Head(RectTransform area, float x, float y, float width)
        {
            OverlayLook look = kit.Look;
            string[] heads = { "Driver", "Car", "Wins", "Points" };
            float[] at = { 120f, width * 0.52f, width * 0.76f, width - 16f };
            for (int i = 0; i < heads.Length; i++)
            {
                bool right = i >= 2;
                ui.WordsAt(area, L(heads[i]), 18f, look.muted, right ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 1f), new Vector2(right ? x + at[i] - 120f : x + at[i], y), new Vector2(120f, 26f));
            }
        }

        private void Standing(RectTransform area, LadderEntry entry, float x, float y, float width, float height)
        {
            OverlayLook look = kit.Look;
            RectTransform row = ui.Box(area, entry.Id ?? "Player", look.row);
            Span(row, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y - height), new Vector2(x + width, y));
            if (entry.IsPlayer)
            {
                ui.Highlight(row, ui.Accent, 0.3f);
            }
            else
            {
                Introduces(row, entry.Id);
            }
            ui.WordsAt(row, RaceFormat.Rank(entry.Rank), 24f, entry.Rank <= 3 ? ui.Accent : look.text, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(64f, 40f), FontStyles.Bold, true);
            ui.Chip(row, ui.DriverColor(entry.ColorIndex), new Vector2(0f, 0.5f), new Vector2(82f, 0f), 26f);
            string name = entry.IsPlayer ? Gamebox.Online.Standings.Who(entry.Name, true) : Portfolio.DeathRace.L.Data(entry.Name);
            ui.WordsAt(row, name, 23f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(120f, 0f),
                new Vector2(width * 0.52f - 130f, 40f), entry.IsPlayer ? FontStyles.Bold : FontStyles.Normal);
            ui.WordsAt(row, CarName(entry.Car), 20f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(width * 0.52f, 0f),
                new Vector2(width * 0.2f, 40f));
            ui.WordsAt(row, RaceFormat.Number(entry.Wins), 22f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(0f, 0.5f),
                new Vector2(width * 0.76f - 120f, 0f), new Vector2(120f, 40f));
            ui.WordsAt(row, RaceFormat.Number(entry.Points), 26f, look.text, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f),
                new Vector2(-16f, 0f), new Vector2(120f, 40f), FontStyles.Bold, true);
        }
    }
}
