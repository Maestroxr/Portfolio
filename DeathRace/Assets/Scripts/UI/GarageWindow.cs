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
    /// The hub of a career, on BaseGame's <see cref="ShopWindow"/>: the driver, the day, the rank and the money across
    /// the top, and five pages. RACE: the offers of the day as big cards (difficulty, track and its map, laps, the three
    /// rivals with their cars and ranks, the prizes) to enter. GARAGE: the car (its picture, stat bars with what the next
    /// upgrade adds, its health with Repair All and Repair Half), its engine, tyre and armour upgrades and its
    /// equipment. MARKET (the shop's own grid): mines, rockets, nitro and sabotage of a rival of the day (a picker of the
    /// drivers racing today). DEALER (the shop's grid): the six cars, what the current one trades in for and what the
    /// player pays. STANDINGS: the ladder of twenty, with The Warden above it. Everything is bought through
    /// <see cref="CareerShop"/>; leaving the garage after buying saves the career.
    /// </summary>
    public sealed class GarageWindow : ShopWindow
    {
        public const string RacePage = "race";
        public const string GaragePage = CareerShop.GaragePage;
        public const string MarketPage = CareerShop.MarketPage;
        public const string DealerPage = CareerShop.DealerPage;
        public const string StandingsPage = "standings";

        private static readonly string[] PageIds = { RacePage, GaragePage, MarketPage, DealerPage, StandingsPage };

        private static readonly string[] Upgrades = { CareerShop.EngineId, CareerShop.TiresId, CareerShop.ArmorId };
        private static readonly string[] Equipment = { CareerShop.SpikesId, CareerShop.PiercingId };

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
        private RaceWindowParts.StatBar speedBar, accelerationBar, handlingBar, armourBar, healthBar;
        private TMP_Text healthText;
        private Button repairAll, repairHalf;

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
                int rank = id == Roster.WardenId ? 0 : Ladder.RankOf(state, id);
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
                case MarketPage: return L("Black Market");
                case DealerPage: return L("Dealer");
                default: return L("Standings");
            }
        }

        /// <summary>The market and the dealer are the shop's own grids; the races, the garage and the standings are drawn here.</summary>
        protected override bool IsCatalogPage(string page)
        {
            return page == MarketPage || page == DealerPage;
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
                string line = LF("Day {0}  ·  Rank {1} of {2}  ·  {3} points  ·  {4}", state.day, RaceFormat.Rank(Ladder.Rank(state)),
                    CareerRules.LadderSize, state.points, CarName(state.car));
                return state.champion ? line + "  ·  " + L("Champion") : line;
            }
        }

        protected override string FormatMoney(int amount)
        {
            return RaceFormat.Money(amount);
        }

        protected override Sprite IconOf(ShopItem item)
        {
            DeathRaceTheme theme = Theme;
            if (theme == null || item == null)
            {
                return null;
            }
            if (item.Tag is CarModel model)
            {
                return theme.CarPicture(model);
            }
            switch (item.Id)
            {
                case CareerShop.MineId: return theme.iconMine;
                case CareerShop.RocketId: return theme.iconRocket;
                case CareerShop.NitroId: return theme.iconNitro;
                case CareerShop.SabotageId: return ui.Icon("Sabotage", theme.iconSkull);
                case CareerShop.RepairId: return ui.Icon("Repair", theme.iconRepair);
                case CareerShop.SpikesId: return ui.Icon("Spikes");
                case CareerShop.PiercingId: return ui.Icon("Piercing");
                default: return item.Tag is UpgradeKind kind ? UpgradePicture(kind) : null;
            }
        }

        /// <summary>The picture of an upgrade: the look's own per kind, else its named icon.</summary>
        private Sprite UpgradePicture(UpgradeKind kind)
        {
            Sprite[] pictures = Theme != null ? Theme.upgradePictures : null;
            int index = (int)kind;
            Sprite picture = pictures != null && index < pictures.Length ? pictures[index] : null;
            string[] names = { "Engine", "Tyres", "Armour" };
            return picture != null ? picture : ui.Icon(names[index]);
        }

        /// <summary>The line under the description: a car's numbers and trade, what the player carries, the sabotage's target.</summary>
        protected override string DetailOf(ShopItem item)
        {
            CareerState state = State;
            CareerShop rules = CareerShopOf();
            if (state == null || rules == null)
            {
                return null;
            }
            if (item.Tag is CarModel model)
            {
                if (model == state.car)
                {
                    return LF("Your car. The dealer gives {0} for it.", RaceFormat.Money(rules.TradeInValue));
                }
                CarSpec spec = CarSpecs.Of(model);
                return LF("{0} less {1} for your {2}", RaceFormat.Money(spec.Price), RaceFormat.Money(rules.TradeInValue), CarName(state.car));
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
            if (item.Tag is CarModel model)
            {
                CarSpec spec = CarSpecs.Of(model);
                if (owned > 0)
                {
                    return L("YOUR CAR");
                }
                return result == BuyResult.Locked ? L(CareerShop.BetterCarReason) : LF("Tier {0}", spec.Tier);
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
            if (item.Id == CareerShop.SabotageId && result == BuyResult.Bought)
            {
                return LF("Sabotage {0}", DriverName(State, CareerShopOf().SabotageTarget));
            }
            return base.CaptionOf(item, owned, result);
        }

        protected override string BuyLabel(ShopItem item)
        {
            return item.Id == CareerShop.SabotageId ? L("Pick a Driver") : base.BuyLabel(item);
        }

        /// <summary>Sabotage asks whom first; a car asks nothing but tells what happened to the old one.</summary>
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
        }

        protected override void Teardown()
        {
            parts.Clear();
            picker = null;
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
            RaceOffer offer = state.offers[index];
            Color color = DifficultyColor(offer.difficulty);
            bool narrow = width < 440f;

            // The band across the top: the difficulty and the winner's prize.
            RectTransform band = ui.Fill(card, "Band", color);
            Span(band, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -58f), Vector2.zero);
            ui.WordsAt(band, ui.DifficultyName(offer.difficulty).ToUpperInvariant(), 30f, Color.Lerp(color, Color.black, 0.75f),
                TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(width * 0.55f, 46f), FontStyles.Bold, true);
            ui.WordsAt(band, RaceFormat.Money(offer.Prize(1)), 30f, Color.Lerp(color, Color.black, 0.75f), TextAlignmentOptions.MidlineRight,
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
                int prize = offer.Prize(place);
                ui.WordsAt(card, Gamebox.Online.Standings.Ordinal(place), 22f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                    new Vector2(prizesX, y), new Vector2(prizesWidth * 0.4f, 32f));
                ui.WordsAt(card, prize > 0 ? RaceFormat.Money(prize) : "-", 24f, place == 1 ? ui.Accent : look.text, TextAlignmentOptions.MidlineRight,
                    new Vector2(0f, 1f), new Vector2(prizesX + prizesWidth * 0.4f, y), new Vector2(prizesWidth * 0.6f, 32f), FontStyles.Bold);
            }
            ui.WordsAt(card, LF("{0} a wreck", RaceFormat.Money(CareerRules.Bounty(offer.difficulty))), 20f, look.muted, TextAlignmentOptions.MidlineRight,
                new Vector2(0f, 1f), new Vector2(prizesX, -116f - 4 * 34f - 4f), new Vector2(prizesWidth, 30f), FontStyles.Italic);
            string points = offer.IsShowdown ? L("For the title") : LF("{0} points to the winner", CareerRules.Points(offer.difficulty, 1));
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

            Button enter = ui.ButtonAt(card, offer.IsShowdown ? "Face The Warden" : "Enter Race", color, new Vector2(0.5f, 0f), new Vector2(0f, 16f),
                new Vector2(width - 32f, 68f), () => Enter(index));
            TMP_Text label = OverlayKit.LabelOf(enter);
            if (label != null)
            {
                label.color = Color.Lerp(color, Color.black, 0.8f);
            }
        }

        private void BuildRival(RectTransform card, CareerState state, string id, float y, float width)
        {
            OverlayLook look = kit.Look;
            bool warden = id == Roster.WardenId;
            RivalState rival = state.Rival(id);
            ui.Chip(card, ui.DriverColor(state, id), new Vector2(0f, 1f), new Vector2(20f, y - 6f), 30f);
            bool sabotaged = state.HasSabotage && state.sabotage == id;
            ui.WordsAt(card, DriverName(state, id), 24f, warden ? ui.Accent : look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(62f, y), new Vector2(width * 0.52f - 66f, 42f), FontStyles.Bold);
            CarModel car = warden ? CarModel.Tyrant : rival != null ? rival.Car : CarModel.Rustbucket;
            string detail = sabotaged ? L("SABOTAGED") : CarName(car);
            ui.WordsAt(card, detail, 20f, sabotaged ? ui.Warn : look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                new Vector2(width * 0.54f, y), new Vector2(width * 0.28f, 42f), sabotaged ? FontStyles.Bold : FontStyles.Normal);
            string rank = warden ? L("Champion") : RaceFormat.Rank(Ladder.RankOf(state, id));
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
        // GARAGE: the car
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
            Picture(car, ui.CarPicture(state.car), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(520f, 220f));
            ui.WordsAt(car, CarName(state.car), 36f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, -232f),
                new Vector2(420f, 48f), FontStyles.Bold, true);
            ui.WordsAt(car, LF("Tier {0}", spec.Tier), 22f, ui.Accent, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, -232f),
                new Vector2(160f, 48f), FontStyles.Bold);
            speedBar = StatLine(car, "Speed", -294f);
            accelerationBar = StatLine(car, "Acceleration", -330f);
            handlingBar = StatLine(car, "Handling", -366f);
            armourBar = StatLine(car, "Armor", -402f);

            // The health and the repairs.
            ui.WordsAt(car, L("Health"), 22f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, -448f),
                new Vector2(170f, 32f));
            healthBar = ui.Bar(car, look.good, new Vector2(0f, 1f), new Vector2(200f, -455f), new Vector2(300f, 20f));
            healthText = ui.WordsAt(car, "", 22f, look.text, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, -448f),
                new Vector2(100f, 32f), FontStyles.Bold);
            repairAll = ui.ButtonAt(car, "Repair All", look.good, new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(290f, 68f), () => Repair(true));
            repairHalf = ui.ButtonAt(car, "Repair Half", look.accent, new Vector2(1f, 0f), new Vector2(-24f, 20f), new Vector2(290f, 68f), () => Repair(false));

            // The parts on the right: upgrades, then equipment.
            RectTransform list = Area(area, "Parts");
            Span(list, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(664f, 0f), Vector2.zero);
            const float rowHeight = 112f;
            float y = 0f;
            foreach (string id in Upgrades)
            {
                parts.Add(BuildPart(list, rules.Catalog[id], y, rowHeight, true));
                y -= rowHeight + 10f;
            }
            foreach (string id in Equipment)
            {
                parts.Add(BuildPart(list, rules.Catalog[id], y, rowHeight, false));
                y -= rowHeight + 10f;
            }
        }

        private RaceWindowParts.StatBar StatLine(RectTransform parent, string label, float y)
        {
            ui.WordsAt(parent, L(label), 22f, kit.Look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, y),
                new Vector2(170f, 32f));
            return ui.Bar(parent, ui.Accent, new Vector2(0f, 1f), new Vector2(200f, y - 8f), new Vector2(412f, 18f));
        }

        private PartRow BuildPart(RectTransform list, ShopItem item, float y, float height, bool leveled)
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
                new Vector2(420f, 40f), FontStyles.Bold, true);
            TMP_Text words = ui.WordsAt(box, L(item.Description), 19f, look.muted, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f),
                new Vector2(left, -50f), new Vector2(470f, height - 56f));
            words.fontSizeMin = 16f;
            words.textWrappingMode = TextWrappingModes.Normal;
            if (leveled)
            {
                row.Pips = new Image[item.Max];
                for (int i = 0; i < item.Max; i++)
                {
                    row.Pips[i] = Shape(box, RaceShapes.Dot, look.muted, new Vector2(1f, 1f), new Vector2(-260f - (item.Max - 1 - i) * 30f, -18f),
                        new Vector2(26f, 26f));
                }
            }
            row.Status = ui.WordsAt(box, "", 20f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, -12f),
                new Vector2(220f, 30f), FontStyles.Bold);
            row.Price = ui.WordsAt(box, "", 28f, look.good, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f), new Vector2(-250f, -14f),
                new Vector2(200f, 40f), FontStyles.Bold, true);
            row.Buy = ui.ButtonAt(box, leveled ? "Upgrade" : "Fit", look.good, new Vector2(1f, 0.5f), new Vector2(-24f, -14f), new Vector2(210f, 60f),
                () => Buy(item));
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
            speedBar.Set(CarBars.Speed(stats), CarBars.Speed(engine));
            accelerationBar.Set(CarBars.Acceleration(stats), CarBars.Acceleration(engine));
            handlingBar.Set(CarBars.Handling(stats), CarBars.Handling(tires));
            armourBar.Set(CarBars.Armour(stats), CarBars.Armour(armor));
            healthBar.Set(car.health);
            healthBar.Fill.color = car.health < 0.35f ? ui.Warn : car.health < 0.7f ? ui.Accent : look.good;
            healthText.text = RaceFormat.Percent(car.health);

            int missing = Career.MissingPoints(car);
            int unit = CareerRules.RepairPrice(car.Spec.Tier);
            Relabel(repairAll, missing > 0 ? LF("Repair All {0}", RaceFormat.Money(missing * unit)) : L("In One Piece"), missing > 0);
            int half = (missing + 1) / 2;
            Relabel(repairHalf, missing > 1 ? LF("Repair Half {0}", RaceFormat.Money(half * unit)) : L("Repair Half"), missing > 1);

            foreach (PartRow row in parts)
            {
                ShopItem item = rules.Catalog[row.Item.Id];
                int owned = rules.Owned(state.consumables, item);
                BuyResult result = rules.CanBuy(state.wallet, state.consumables, item, out string reason);
                int price = rules.NextPrice(state.consumables, item);
                if (row.Pips != null)
                {
                    for (int i = 0; i < row.Pips.Length; i++)
                    {
                        row.Pips[i].sprite = i < owned ? RaceShapes.Dot : RaceShapes.Ring;
                        row.Pips[i].color = i < owned ? ui.Accent : look.muted;
                    }
                    row.Status.text = LF("Level {0} of {1}", owned, item.Max);
                }
                else
                {
                    row.Status.text = owned > 0 ? L("FITTED") : "";
                    row.Status.color = owned > 0 ? look.good : look.muted;
                }
                row.Price.text = price < 0 ? (row.Pips != null ? L("MAX") : "") : RaceFormat.Money(price);
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
        // MARKET: whom to sabotage
        // ------------------------------------------------------------------------------------------------

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
            // The Warden above everyone.
            RectTransform warden = ui.Box(area, "Warden", look.row);
            Span(warden, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), Vector2.zero);
            ui.Highlight(warden, Career.WardenPaint, 0.55f);
            Introduces(warden, Roster.WardenId);
            Shape(warden, ui.Icon("Trophy", RaceShapes.Star), ui.Accent, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(38f, 38f));
            ui.WordsAt(warden, L("The Warden"), 30f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(68f, 0f),
                new Vector2(400f, 48f), FontStyles.Bold, true);
            string crown = state.champion ? LF("Beaten by {0} on day {1}", state.driverName, state.championDay) : L("The champion. Reach first place to face him.");
            ui.WordsAt(warden, crown, 22f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(380f, 0f),
                new Vector2(760f, 40f), FontStyles.Italic);
            ui.WordsAt(warden, CarName(CarModel.Tyrant), 22f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 0.5f), new Vector2(-20f, 0f),
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
                RosterDriver driver = Roster.Find(id);
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
