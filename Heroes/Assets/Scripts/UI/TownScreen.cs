using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The screen of a town: its picture and what it yields along the top; the buildings as cards, built, to be built
    /// today, or out of reach and why; the creatures waiting in its dwellings; the garrison and the hero visiting along
    /// the bottom, with the doors of the tavern and the market beside them. Everything on it goes through the commands,
    /// so an online town behaves exactly like one at this device.
    /// </summary>
    public sealed class TownScreen : Dialog
    {
        private const float HeaderHeight = 84f;
        private const float ArmiesHeight = 204f;
        private const float RowSize = 80f;
        /// <summary>The space between the header, the buildings and dwellings, and the armies.</summary>
        private const float Gap = 10f;

        /// <summary>The buildings in the order the town shows them: the halls, the walls, the trades, the guild, the dwellings.</summary>
        private static readonly BuildingId[] Order =
        {
            BuildingId.TownHall, BuildingId.CityHall, BuildingId.Fort, BuildingId.Citadel, BuildingId.Castle, BuildingId.Tavern,
            BuildingId.Marketplace, BuildingId.Silo, BuildingId.MageGuild1, BuildingId.MageGuild2, BuildingId.MageGuild3,
            BuildingId.Dwelling1, BuildingId.Dwelling2, BuildingId.Dwelling3, BuildingId.Dwelling4, BuildingId.Dwelling5,
            BuildingId.Dwelling6, BuildingId.Dwelling7
        };

        private TownState town;
        private Image picture;
        private TextMeshProUGUI townName;
        private TextMeshProUGUI townLine;
        private TextMeshProUGUI income;
        private TextMeshProUGUI growth;
        private TextMeshProUGUI guild;
        private TextMeshProUGUI today;
        private RectTransform buildings;
        private RectTransform dwellings;
        private ArmyRow garrison;
        private ArmyRow visitor;
        private Image garrisonPicture;
        private Image visitorPicture;
        private TextMeshProUGUI visitorName;
        private TextMeshProUGUI visitorLine;
        private Button tavern;
        private Button market;
        private TextMeshProUGUI tavernLine;
        private TextMeshProUGUI marketLine;
        private MarketBox marketBox;
        private TavernBox tavernBox;
        private string shownBuildings;
        private string shownDwellings;

        public static TownScreen Make(Transform parent, HeroesGameManager manager)
        {
            TownScreen screen = Build<TownScreen>(parent, manager, "Town", new Vector2(1720f, UnderBarHeight), "Town");
            screen.KeepUnderTopBar();
            screen.Header(screen.Body);
            screen.Middle(screen.Body);
            screen.Armies(screen.Body);
            Button leave = screen.Answer("Leave", screen.Close);
            UIKit.Fit((RectTransform)leave.transform, 240f, ButtonRow - 4f);
            screen.marketBox = MarketBox.Make(parent, manager);
            screen.tavernBox = TavernBox.Make(parent, manager);
            return screen;
        }

        // ------------------------------------------------------------------ building

        /// <summary>The picture of the town, its name and people, and what it yields, in sunken boxes on the right.</summary>
        private void Header(RectTransform body)
        {
            RectTransform header = Band(body, "Header", 0f, HeaderHeight);
            picture = UIKit.PortraitFrame(header, "Picture", null);
            UIKit.Pin((RectTransform)picture.transform.parent.parent, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(HeaderHeight, HeaderHeight));

            townName = UIKit.Heading(header, "Name", "", 36f, TextAlignmentOptions.BottomLeft);
            RectTransform nameRect = (RectTransform)townName.transform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.offsetMin = new Vector2(HeaderHeight + 18f, 0f);
            nameRect.offsetMax = new Vector2(HeaderHeight + 18f + 640f, 0f);
            UIKit.FitLine(townName, 36f, 22f);
            townLine = UIKit.Label(header, "Line", "", 21f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            townLine.fontStyle = FontStyles.Italic;
            RectTransform lineRect = (RectTransform)townLine.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(0f, 0.5f);
            lineRect.offsetMin = new Vector2(HeaderHeight + 20f, 0f);
            lineRect.offsetMax = new Vector2(HeaderHeight + 20f + 640f, -4f);
            UIKit.FitLine(townLine, 21f, 14f);

            RectTransform boxes = UIKit.Rect(header, "Figures");
            boxes.anchorMin = new Vector2(1f, 0.5f);
            boxes.anchorMax = new Vector2(1f, 0.5f);
            boxes.pivot = new Vector2(1f, 0.5f);
            boxes.sizeDelta = new Vector2(4 * 200f + 3 * 10f, 64f);
            boxes.anchoredPosition = Vector2.zero;
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(boxes, 10f);
            row.childAlignment = TextAnchor.MiddleRight;
            income = Figure(boxes, "Income", Manager.Art.Resource(ResourceKind.Gold), false,
                "Income\nGold the town's hall brings in every day.");
            growth = Figure(boxes, "Growth", Manager.Art.Icon("monster"), true,
                "Growth\nThe walls of a Citadel or a Castle bring more creatures to the dwellings every week.");
            guild = Figure(boxes, "Guild", Manager.Art.Icon("spellbook"), true,
                "Mage Guild\nA hero who visits the town learns the spells of its guild.");
            today = Figure(boxes, "Today", Manager.Art.Icon("build"), true, "Building\nA town can put up one building a day.");
        }

        private static TextMeshProUGUI Figure(RectTransform parent, string name, Sprite icon, bool tinted, string tip)
        {
            return UIKit.Figure(parent, name, icon, tinted, tip, 200f);
        }

        /// <summary>The buildings on the left and the dwellings on the right, between the header and the armies.</summary>
        private void Middle(RectTransform body)
        {
            RectTransform middle = UIKit.Rect(body, "Middle");
            middle.anchorMin = Vector2.zero;
            middle.anchorMax = Vector2.one;
            middle.offsetMin = new Vector2(0f, ArmiesHeight + Gap);
            middle.offsetMax = new Vector2(0f, -HeaderHeight - Gap);

            Image left = UIKit.Panel(middle, "Buildings");
            RectTransform leftRect = (RectTransform)left.transform;
            leftRect.anchorMin = new Vector2(0f, 0f);
            leftRect.anchorMax = new Vector2(0.57f, 1f);
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = new Vector2(-6f, 0f);
            RectTransform leftContent = UIKit.Content(left, 6f);
            UIKit.SectionTitle(leftContent, "Caption", "Buildings", 24f);
            buildings = UIKit.Scroll(leftContent, "List", out ScrollRect buildScroll);
            UIKit.Stretch((RectTransform)buildScroll.transform, 0f, 0f, 0f, 54f);
            // Four rows of cards fit exactly; the rest scroll into sight.
            GridLayoutGroup grid = UIKit.Grid(buildings, new Vector2(214f, 94f), new Vector2(8f, 8f), 4);
            grid.padding = new RectOffset(2, 2, 2, 2);
            grid.childAlignment = TextAnchor.UpperCenter;

            Image right = UIKit.Panel(middle, "Dwellings");
            RectTransform rightRect = (RectTransform)right.transform;
            rightRect.anchorMin = new Vector2(0.57f, 0f);
            rightRect.anchorMax = new Vector2(1f, 1f);
            rightRect.offsetMin = new Vector2(6f, 0f);
            rightRect.offsetMax = Vector2.zero;
            RectTransform rightContent = UIKit.Content(right, 6f);
            UIKit.SectionTitle(rightContent, "Caption", "Recruit", 24f);
            dwellings = UIKit.Scroll(rightContent, "List", out ScrollRect dwellingScroll);
            UIKit.Stretch((RectTransform)dwellingScroll.transform, 0f, 0f, 0f, 54f);
            VerticalLayoutGroup list = UIKit.Layout<VerticalLayoutGroup>(dwellings, 6f, new RectOffset(2, 2, 2, 2));
            list.childForceExpandWidth = true;
        }

        /// <summary>The garrison and the visiting hero along the bottom, and the doors of the tavern and the market.</summary>
        private void Armies(RectTransform body)
        {
            Image panel = UIKit.Panel(body, "Armies");
            RectTransform rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, ArmiesHeight);
            RectTransform content = UIKit.Content(panel, 6f);

            garrison = ArmyLine(content, "Garrison", 1f, out garrisonPicture, out TextMeshProUGUI garrisonName, out TextMeshProUGUI garrisonLine);
            garrisonName.text = "Garrison";
            garrisonLine.text = "Holds the walls";
            visitor = ArmyLine(content, "Visitor", 0f, out visitorPicture, out visitorName, out visitorLine);

            RectTransform doors = UIKit.Rect(content, "Doors");
            doors.anchorMin = new Vector2(1f, 0f);
            doors.anchorMax = new Vector2(1f, 1f);
            doors.pivot = new Vector2(1f, 0.5f);
            doors.offsetMin = new Vector2(-2f * 330f - 16f, 4f);
            doors.offsetMax = new Vector2(0f, -4f);
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(doors, 16f);
            row.childForceExpandHeight = true;
            row.childForceExpandWidth = true;
            tavern = Door(doors, "Tavern", Manager.Art.Icon("hire"), OpenTavern, out tavernLine);
            market = Door(doors, "Marketplace", Manager.Art.Icon("market"), OpenMarket, out marketLine);
        }

        /// <summary>An army along the bottom: whose it is in a portrait and two lines, then its seven places.</summary>
        private ArmyRow ArmyLine(RectTransform parent, string name, float top, out Image portrait, out TextMeshProUGUI title,
            out TextMeshProUGUI line)
        {
            RectTransform holder = UIKit.Rect(parent, name);
            holder.anchorMin = new Vector2(0f, top);
            holder.anchorMax = new Vector2(0f, top);
            holder.pivot = new Vector2(0f, top);
            holder.sizeDelta = new Vector2(980f, RowSize);
            holder.anchoredPosition = new Vector2(0f, top > 0.5f ? -4f : 4f);
            portrait = UIKit.PortraitFrame(holder, "Portrait", null);
            UIKit.Pin((RectTransform)portrait.transform.parent.parent, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(RowSize, RowSize));
            title = UIKit.Title(holder, "Name", "", 21f, UIKit.Gold, TextAlignmentOptions.BottomLeft);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(RowSize + 12f, 2f);
            titleRect.offsetMax = new Vector2(RowSize + 12f + 190f, -6f);
            UIKit.FitLine(title, 21f, 13f);
            title.characterSpacing = 1f;
            line = UIKit.Label(holder, "Line", "", 18f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            line.fontStyle = FontStyles.Italic;
            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(0f, 0.5f);
            lineRect.offsetMin = new Vector2(RowSize + 12f, 6f);
            lineRect.offsetMax = new Vector2(RowSize + 12f + 190f, -2f);
            UIKit.FitLine(line, 18f, 12f);
            ArmyRow army = ArmyRow.Make(holder, Manager, RowSize);
            UIKit.Pin((RectTransform)army.transform, new Vector2(0f, 0.5f), new Vector2(RowSize + 212f, 0f),
                new Vector2(7f * RowSize + 6f * Mathf.Round(RowSize * 0.1f), RowSize));
            return army;
        }

        /// <summary>A door of the town: a card with an icon, the name of the place and a line on it.</summary>
        private static Button Door(RectTransform parent, string name, Sprite icon, System.Action onClick, out TextMeshProUGUI line)
        {
            Button button = UIKit.CardButton(parent, name, onClick);
            var card = (Image)button.targetGraphic;
            RectTransform content = UIKit.Content(card, 8f);
            Image image = UIKit.PortraitFrame(content, "Icon", icon, true);
            image.color = UIKit.Gold;
            UIKit.Stretch((RectTransform)image.transform, 16f, 16f, 16f, 16f);
            UIKit.Pin((RectTransform)image.transform.parent.parent, new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(92f, 92f));
            TextMeshProUGUI title = UIKit.Heading(content, "Name", name, 26f, TextAlignmentOptions.BottomLeft);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(110f, 2f);
            titleRect.offsetMax = new Vector2(-4f, -10f);
            UIKit.FitLine(title, 26f, 16f);
            line = UIKit.Label(content, "Line", "", 18f, UIKit.Dim, TextAlignmentOptions.TopLeft);
            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, 0.5f);
            lineRect.offsetMin = new Vector2(110f, 10f);
            lineRect.offsetMax = new Vector2(-4f, -4f);
            line.textWrappingMode = TextWrappingModes.Normal;
            line.enableAutoSizing = true;
            line.fontSizeMax = 18f;
            line.fontSizeMin = 13f;
            card.gameObject.AddComponent<CanvasGroup>();
            return button;
        }

        /// <summary>A strip across the top of <paramref name="parent"/>, <paramref name="height"/> tall, <paramref name="top"/> down.</summary>
        private static RectTransform Band(RectTransform parent, string name, float top, float height)
        {
            RectTransform band = UIKit.Rect(parent, name);
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.offsetMin = new Vector2(0f, -top - height);
            band.offsetMax = new Vector2(0f, -top);
            return band;
        }

        // ------------------------------------------------------------------ showing a town

        public void Open(TownState which)
        {
            town = which;
            shownBuildings = null;
            shownDwellings = null;
            ArmyRow.Drop();
            Open();
            Refresh();
            Manager.Sound?.PlayTownMusic(which.faction);
        }

        public override void Close()
        {
            bool was = IsOpen;
            base.Close();
            ArmyRow.Drop();
            marketBox.Close();
            tavernBox.Close();
            if (was && town != null && Manager.Game != null && !Manager.Game.InBattle)
            {
                Manager.Sound?.PlayAdventureMusic();
            }
        }

        /// <summary>Escape closes the market or the tavern first, then the town. False when none is open.</summary>
        public bool HandleBack()
        {
            if (marketBox.IsOpen)
            {
                marketBox.Close();
                return true;
            }
            if (tavernBox.IsOpen)
            {
                tavernBox.Close();
                return true;
            }
            if (IsOpen)
            {
                Close();
                return true;
            }
            return false;
        }

        public void Refresh()
        {
            if (!IsOpen || town == null || Manager.Game == null)
            {
                return;
            }
            town = Manager.Game.State.Town(town.id);
            if (town == null)
            {
                Close();
                return;
            }
            GameState state = Manager.Game.State;
            PlayerState owner = state.Player(town.owner);
            Heading.text = town.name;
            picture.sprite = Manager.Art.TownPortrait(town.faction, owner != null ? (int)owner.color : 4);
            townName.text = town.name;
            townLine.text = $"A {Land.FactionName(town.faction)} town, " +
                            (owner == null ? "held by no one" : owner.index == Manager.Viewer ? "yours" : $"held by {owner.name}");
            income.text = $"+{Buildings.Income(town):N0} a day";
            int bonus = Buildings.GrowthBonus(town);
            growth.text = bonus > 0 ? $"Growth +{bonus}%" : "Growth as usual";
            int guildLevel = Buildings.MageGuildLevel(town);
            guild.text = guildLevel > 0 ? $"Mage Guild {guildLevel}" : "No Mage Guild";
            today.text = town.builtToday ? "Built today" : "Can build today";
            today.color = town.builtToday ? UIKit.Dim : UIKit.Good;

            BuildingList(owner);
            DwellingList(owner);
            Armies(state, owner);
            if (marketBox.IsOpen)
            {
                marketBox.Refresh();
            }
            if (tavernBox.IsOpen)
            {
                tavernBox.Refresh();
            }
        }

        private bool Mine => town != null && town.owner == Manager.Viewer;

        private void Armies(GameState state, PlayerState owner)
        {
            garrisonPicture.sprite = Manager.Art.TownPortrait(town.faction, owner != null ? (int)owner.color : 4);
            garrison.Bind(Holder.Garrison(town.id));
            HeroState hero = state.HeroAt(town.cell);
            bool visiting = hero != null && hero.owner == town.owner;
            visitor.gameObject.SetActive(visiting);
            visitorPicture.color = visiting ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            if (visiting)
            {
                visitor.Bind(Holder.Hero(hero.id));
                HeroClass heroClass = hero.Def != null ? hero.Def.Class : HeroClass.Knight;
                visitorPicture.sprite = Manager.Art.HeroPortrait(heroClass);
                visitorName.text = hero.Name;
                visitorLine.text = $"Level {hero.level} {HeroData.Class(heroClass).Name}";
            }
            else
            {
                visitorPicture.sprite = Manager.Art.Icon("hero");
                visitorName.text = "No hero visiting";
                visitorLine.text = "A hero in town can trade troops";
            }

            bool free = Mine && !Manager.HeroesUI.Busy;
            bool hasTavern = town.Has(BuildingId.Tavern);
            bool hasMarket = town.Has(BuildingId.Marketplace);
            Door(tavern, tavernLine, free && hasTavern,
                hasTavern ? $"Hire a hero for {UIKit.Glyph(ResourceKind.Gold)} {HeroData.HireCost:N0}" : "Not built yet");
            Door(market, marketLine, free && hasMarket, hasMarket ? "Trade one resource for another" : "Not built yet");
        }

        private static void Door(Button door, TextMeshProUGUI line, bool open, string text)
        {
            door.interactable = open;
            line.text = text;
            door.GetComponent<CanvasGroup>().alpha = open ? 1f : 0.55f;
        }

        // ------------------------------------------------------------------ the buildings

        /// <summary>Every building of the town as a card: built, buildable today, or out of reach and why.</summary>
        private void BuildingList(PlayerState owner)
        {
            bool free = Mine && !Manager.HeroesUI.Busy;
            string signature = $"{town.id}|{string.Join(",", town.built)}|{town.builtToday}|{free}|{Held(owner)}";
            if (signature == shownBuildings)
            {
                return;
            }
            shownBuildings = signature;
            UIKit.Clear(buildings);
            foreach (BuildingId id in Order)
            {
                BuildingDef def = Buildings.Get(town.faction, id);
                if (def != null)
                {
                    BuildingCard(def, owner, free);
                }
            }
        }

        private static string Held(PlayerState owner)
        {
            return owner != null ? string.Join(",", owner.resources.values) : "";
        }

        private void BuildingCard(BuildingDef def, PlayerState owner, bool free)
        {
            bool built = town.Has(def.Id);
            bool ready = Ready(def);
            bool affordable = owner != null && owner.resources.CanAfford(def.Cost);
            bool can = !built && ready && affordable && !town.builtToday && free;

            BuildingId building = def.Id;
            Button button = UIKit.CardButton(buildings, def.Name, () => BuildClicked(building));
            var card = (Image)button.targetGraphic;
            button.interactable = can;
            if (built)
            {
                UIKit.Select(card, true);
            }
            RectTransform content = UIKit.Content(card, 3f);

            Sprite icon = BuildingIcon(def, out bool portrait);
            Image image = UIKit.PortraitFrame(content, "Icon", icon, !portrait);
            if (!portrait)
            {
                image.color = def.Id == BuildingId.TownHall || def.Id == BuildingId.CityHall || def.Id == BuildingId.Silo ? Color.white : UIKit.Gold;
                UIKit.Stretch((RectTransform)image.transform, 11f, 11f, 11f, 11f);
            }
            UIKit.Pin((RectTransform)image.transform.parent.parent, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(62f, 62f));

            string state;
            Color color;
            if (built)
            {
                state = "✓ Built";
                color = UIKit.Good;
            }
            else if (!ready)
            {
                // The card has room for two short lines; the tooltip lists every building still needed.
                List<string> missing = MissingNames(def);
                state = $"Needs {missing[0]}" +
                        (missing.Count == 2 ? $"\nand {missing[1]}" : missing.Count > 2 ? $"\nand {missing.Count - 1} more" : "");
                color = UIKit.Bad;
            }
            else
            {
                state = CardCost(def.Cost, owner != null ? owner.resources : null);
                color = affordable ? UIKit.Ink : UIKit.Dim;
            }
            // A state of two lines takes more of the card from the name above it, and its lines close up.
            bool twoLines = state.IndexOf('\n') >= 0;
            float split = twoLines ? 0.62f : 0.5f;

            TextMeshProUGUI title = UIKit.Label(content, "Name", def.Name, 20f, built ? UIKit.Gold : can ? UIKit.Ink : UIKit.Dim,
                TextAlignmentOptions.BottomLeft, true);
            RectTransform titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, split);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(70f, 0f);
            titleRect.offsetMax = new Vector2(-2f, -4f);
            UIKit.FitLine(title, 19f, 12f);

            TextMeshProUGUI line = UIKit.Label(content, "State", state, 18f, color, TextAlignmentOptions.TopLeft);
            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, split);
            lineRect.offsetMin = new Vector2(70f, 2f);
            lineRect.offsetMax = new Vector2(-2f, -2f);
            UIKit.FitLine(line, 18f, 14f);
            if (twoLines)
            {
                line.lineSpacing = -30f;
            }

            if (!built && !can)
            {
                // Out of reach today: the card fades a little, and its tooltip says why.
                card.gameObject.AddComponent<CanvasGroup>().alpha = ready ? 0.85f : 0.62f;
            }
            Tooltip.Attach(card.gameObject, def.Name, Explain(def, built, ready, affordable), icon);
        }

        /// <summary>The picture of a building: the creature a dwelling houses, else the icon of what the building does.</summary>
        private Sprite BuildingIcon(BuildingDef def, out bool portrait)
        {
            portrait = false;
            int tier = def.DwellingTier;
            if (tier > 0)
            {
                CreatureDef creature = Creatures.Get(Creatures.OfTier(town.faction, tier));
                Sprite face = creature != null ? Manager.Art.Portrait(creature.Id) : null;
                if (face != null)
                {
                    portrait = true;
                    return face;
                }
                return Manager.Art.Icon("monster");
            }
            HeroesArt art = Manager.Art;
            switch (def.Id)
            {
                case BuildingId.TownHall:
                case BuildingId.CityHall:
                    return art.Resource(ResourceKind.Gold);
                case BuildingId.Fort:
                case BuildingId.Citadel:
                case BuildingId.Castle:
                    return art.Icon("defend");
                case BuildingId.Tavern:
                    return art.Icon("hire");
                case BuildingId.Marketplace:
                    return art.Icon("market");
                case BuildingId.Silo:
                    return art.Resource(Land.RareOf(town.faction));
                default:
                    return art.Icon("spellbook");
            }
        }

        private void BuildClicked(BuildingId building)
        {
            if (Mine && MayCommand)
            {
                Manager.Commands.Build(town.id, building);
            }
        }

        private string Explain(BuildingDef def, bool built, bool ready, bool affordable)
        {
            var text = new StringBuilder(def.Description);
            if (built)
            {
                return text.Append("\n<color=#8CE07E>Built.</color>").ToString();
            }
            text.Append("\nCosts ").Append(UIKit.Cost(def.Cost));
            if (!ready)
            {
                text.Append($"\n<color=#EE7A66>Needs {Missing(def)} first.</color>");
            }
            else if (!affordable)
            {
                text.Append("\n<color=#EE7A66>You cannot afford it yet.</color>");
            }
            else if (town.builtToday)
            {
                text.Append("\nThe town has built today: one building a day.");
            }
            else if (Mine)
            {
                text.Append("\nClick to build it.");
            }
            return text.ToString();
        }

        /// <summary>The buildings a building still needs, by name: "Fort", "Fort and Tavern", "Fort, Tavern and Chapel".</summary>
        private string Missing(BuildingDef def)
        {
            List<string> names = MissingNames(def);
            if (names.Count < 2)
            {
                return string.Join("", names);
            }
            return $"{string.Join(", ", names.GetRange(0, names.Count - 1))} and {names[names.Count - 1]}";
        }

        /// <summary>
        /// The price on a building's card, large enough to read: on one line while it asks for one or two resources, else
        /// the gold on the first line and the rest on a second.
        /// </summary>
        private static string CardCost(ResourceSet cost, ResourceSet held)
        {
            int kinds = 0;
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                if (cost.values[i] != 0)
                {
                    kinds++;
                }
            }
            if (kinds <= 2 || cost.Gold == 0)
            {
                return UIKit.Cost(cost, held);
            }
            ResourceSet rest = cost.Clone();
            rest.values[0] = 0;
            return UIKit.Cost(new ResourceSet(cost.Gold), held) + "\n" + UIKit.Cost(rest, held);
        }

        private List<string> MissingNames(BuildingDef def)
        {
            var names = new List<string>();
            foreach (BuildingId need in def.Requires)
            {
                if (!town.Has(need))
                {
                    names.Add(Buildings.Get(town.faction, need).Name);
                }
            }
            return names;
        }

        private bool Ready(BuildingDef def)
        {
            foreach (BuildingId need in def.Requires)
            {
                if (!town.Has(need))
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------ the dwellings

        /// <summary>The creatures of each dwelling built: who they are, what they cost, how many wait, and the buttons.</summary>
        private void DwellingList(PlayerState owner)
        {
            HeroState hero = Manager.Game.State.HeroAt(town.cell);
            bool visiting = hero != null && hero.owner == town.owner;
            bool free = Mine && !Manager.HeroesUI.Busy;
            string signature = $"{town.id}|{string.Join(",", town.built)}|{string.Join(",", town.available)}|{free}|{visiting}|{Held(owner)}";
            if (signature == shownDwellings)
            {
                return;
            }
            shownDwellings = signature;
            UIKit.Clear(dwellings);
            for (int tier = 1; tier <= 7; tier++)
            {
                if (!town.Has(Buildings.DwellingOf(tier)))
                {
                    continue;
                }
                CreatureDef def = Creatures.Get(Creatures.OfTier(town.faction, tier));
                if (def != null)
                {
                    DwellingRow(def, tier, owner, visiting, free);
                }
            }
            if (dwellings.childCount == 0)
            {
                TextMeshProUGUI empty = UIKit.Label(dwellings, "Empty", "No dwelling yet. Build one to recruit creatures.", 21f, UIKit.Dim,
                    TextAlignmentOptions.Center);
                empty.fontStyle = FontStyles.Italic;
                UIKit.Fit((RectTransform)empty.transform, 0f, 80f, true);
            }
        }

        private void DwellingRow(CreatureDef def, int tier, PlayerState owner, bool visiting, bool free)
        {
            int waiting = town.available[tier - 1];
            int affordable = owner != null ? owner.resources.Times(def.Cost) : 0;
            int most = Mathf.Min(waiting, affordable);

            Image row = UIKit.Card(dwellings, def.Name);
            UIKit.Fit((RectTransform)row.transform, 0f, 78f, true);
            RectTransform content = UIKit.Content(row, 3f);
            Sprite face = Manager.Art.Portrait(def.Id);
            Image portrait = UIKit.PortraitFrame(content, "Portrait", face);
            UIKit.Pin((RectTransform)portrait.transform.parent.parent, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
            Tooltip.Attach(portrait.transform.parent.gameObject, def.Name, ArmyRow.Describe(def), face);

            TextMeshProUGUI name = UIKit.Label(content, "Name", def.Plural, 21f, UIKit.Ink, TextAlignmentOptions.BottomLeft, true);
            RectTransform nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(0f, 1f);
            nameRect.offsetMin = new Vector2(74f, 0f);
            nameRect.offsetMax = new Vector2(74f + 240f, -4f);
            UIKit.FitLine(name, 21f, 13f);
            name.characterSpacing = 1f;
            TextMeshProUGUI cost = UIKit.Label(content, "Cost", $"{UIKit.Cost(def.Cost, owner != null ? owner.resources : null)} <color=#B8AB8F>each</color>",
                18f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            RectTransform costRect = (RectTransform)cost.transform;
            costRect.anchorMin = new Vector2(0f, 0f);
            costRect.anchorMax = new Vector2(0f, 0.5f);
            costRect.offsetMin = new Vector2(74f, 2f);
            costRect.offsetMax = new Vector2(74f + 240f, -2f);
            UIKit.FitLine(cost, 18f, 12f);

            Image box = UIKit.Recess(content, "Waiting");
            box.raycastTarget = true;
            UIKit.Pin((RectTransform)box.transform, new Vector2(0f, 0.5f), new Vector2(326f, 0f), new Vector2(96f, 62f));
            TextMeshProUGUI count = UIKit.Label(box.transform, "Count", waiting.ToString("N0"), 26f, waiting > 0 ? UIKit.Gold : UIKit.Dim,
                TextAlignmentOptions.Center, true);
            UIKit.Stretch((RectTransform)count.transform, 4f, 22f, 4f, 2f);
            UIKit.FitLine(count, 26f, 14f);
            TextMeshProUGUI week = UIKit.Label(box.transform, "Growth", $"+{def.Growth} a week", 14f, UIKit.Dim, TextAlignmentOptions.Center);
            UIKit.Stretch((RectTransform)week.transform, 4f, 4f, 4f, 36f);
            UIKit.FitLine(week, 14f, 10f);
            Tooltip.Attach(box.gameObject, "Waiting", $"{waiting} {(waiting == 1 ? def.Name : def.Plural)} can be recruited.\n" +
                                                      $"The dwelling brings {def.Growth} more every week.");

            int level = tier;
            int all = most;
            bool can = free && most >= 1;
            RectTransform buttons = UIKit.Rect(content, "Buttons");
            buttons.anchorMin = new Vector2(1f, 0.5f);
            buttons.anchorMax = new Vector2(1f, 0.5f);
            buttons.pivot = new Vector2(1f, 0.5f);
            buttons.sizeDelta = new Vector2(2f * 104f + 8f, 50f);
            buttons.anchoredPosition = new Vector2(-2f, 0f);
            UIKit.Layout<HorizontalLayoutGroup>(buttons, 8f).childForceExpandHeight = true;
            Button one = UIKit.Push(buttons, "One", "Recruit", () => Recruit(level, 1, visiting), 19f);
            UIKit.Fit((RectTransform)one.transform, 104f, 50f);
            UIKit.Enable(one, can);
            Button every = UIKit.Push(buttons, "All", most > 0 ? $"All {most}" : "All", () => Recruit(level, all, visiting), 19f);
            UIKit.Fit((RectTransform)every.transform, 104f, 50f);
            UIKit.Enable(every, can);
            string whereTo = visiting ? "They join the visiting hero's army." : "They join the garrison.";
            Tooltip.Attach(one.gameObject, "Recruit one", $"{UIKit.Cost(def.Cost)}\n{whereTo}");
            Tooltip.Attach(every.gameObject, "Recruit all you can",
                most > 0 ? $"{most} for {UIKit.Cost(def.Cost, null, most)}\n{whereTo}" : "None waiting, or not enough to pay for one.");
        }

        private void Recruit(int tier, int count, bool toHero)
        {
            if (Mine && MayCommand && count > 0)
            {
                Manager.Commands.Recruit(town.id, tier, count, toHero);
            }
        }

        /// <summary>Opens the marketplace of the town over it.</summary>
        public void OpenMarket()
        {
            marketBox.Show(town);
        }

        /// <summary>Opens the tavern of the town over it.</summary>
        public void OpenTavern()
        {
            tavernBox.Show(town);
        }
    }

    /// <summary>
    /// The tavern of a town: the two heroes waiting there, each with his portrait, his calling, his story and what he
    /// knows, and what he asks to join.
    /// </summary>
    public sealed class TavernBox : Dialog
    {
        private TownState town;
        private RectTransform cards;
        private TextMeshProUGUI note;
        private string shown;

        public static TavernBox Make(Transform parent, HeroesGameManager manager)
        {
            TavernBox box = Build<TavernBox>(parent, manager, "Tavern", new Vector2(1000f, 660f), "Tavern");
            box.note = UIKit.Label(box.Body, "Note", "", 21f, UIKit.Dim, TextAlignmentOptions.Center);
            box.note.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)box.note.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(880f, 34f));
            UIKit.FitLine(box.note, 21f, 14f);
            box.cards = UIKit.Rect(box.Body, "Heroes");
            UIKit.Stretch(box.cards, 0f, 0f, 0f, 44f);
            HorizontalLayoutGroup row = UIKit.Layout<HorizontalLayoutGroup>(box.cards, 24f);
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childForceExpandHeight = true;
            Button leave = box.Answer("Leave", box.Close);
            UIKit.Fit((RectTransform)leave.transform, 220f, ButtonRow - 4f);
            return box;
        }

        public void Show(TownState which)
        {
            town = which;
            shown = null;
            Open();
            Refresh();
        }

        public void Refresh()
        {
            if (!IsOpen || town == null || Manager.Game == null)
            {
                return;
            }
            PlayerState owner = Manager.Game.State.Player(town.owner);
            if (owner == null)
            {
                Close();
                return;
            }
            bool free = town.owner == Manager.Viewer && !Manager.HeroesUI.Busy;
            string signature = $"{string.Join(",", owner.tavern)}|{owner.resources.Gold}|{owner.heroes.Count}|{free}|{Manager.Game.State.HeroAt(town.cell) != null}";
            if (signature == shown)
            {
                return;
            }
            shown = signature;
            Heading.text = $"Tavern of {town.name}";
            bool full = owner.heroes.Count >= 8;
            bool blocked = Manager.Game.State.HeroAt(town.cell) != null;
            note.text = full ? "You lead as many heroes as a realm may."
                : blocked ? "A hero stands in the town: a new one has nowhere to go out from."
                : $"Two heroes wait for a lord who can pay them {UIKit.Glyph(ResourceKind.Gold)} {HeroData.HireCost:N0}.";
            UIKit.Clear(cards);
            for (int slot = 0; slot < owner.tavern.Length; slot++)
            {
                HeroDef def = HeroData.Hero(owner.tavern[slot]);
                if (def != null)
                {
                    Card(def, slot, free && !full && !blocked && owner.resources.Gold >= HeroData.HireCost);
                }
            }
        }

        private void Card(HeroDef def, int slot, bool can)
        {
            Image card = UIKit.Card(cards, def.Name);
            UIKit.Fit((RectTransform)card.transform, 430f, 0f);
            RectTransform content = UIKit.Content(card, 12f);
            HeroClassDef heroClass = HeroData.Class(def.Class);

            Image portrait = UIKit.PortraitFrame(content, "Portrait", Manager.Art.HeroPortrait(def.Class));
            UIKit.Pin((RectTransform)portrait.transform.parent.parent, new Vector2(0f, 1f), Vector2.zero, new Vector2(150f, 150f));
            TextMeshProUGUI name = UIKit.Heading(content, "Name", def.Name, 28f, TextAlignmentOptions.TopLeft);
            RectTransform nameRect = (RectTransform)name.transform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0f, 1f);
            nameRect.offsetMin = new Vector2(166f, -40f);
            nameRect.offsetMax = new Vector2(0f, -4f);
            UIKit.FitLine(name, 28f, 16f);
            TextMeshProUGUI calling = UIKit.Label(content, "Class", $"{heroClass.Name} of the {Land.FactionName(heroClass.Faction)}", 19f,
                UIKit.Dim, TextAlignmentOptions.TopLeft);
            calling.fontStyle = FontStyles.Italic;
            UIKit.Pin((RectTransform)calling.transform, new Vector2(0f, 1f), new Vector2(166f, -44f), new Vector2(240f, 26f));
            UIKit.FitLine(calling, 19f, 12f);

            // What he knows: his two skills and his spell, as icons with their names.
            RectTransform knows = UIKit.Rect(content, "Knows");
            UIKit.Pin(knows, new Vector2(0f, 1f), new Vector2(166f, -78f), new Vector2(240f, 72f));
            VerticalLayoutGroup list = UIKit.Layout<VerticalLayoutGroup>(knows, 2f);
            list.childForceExpandWidth = true;
            Known(knows, Manager.Art.Skill(def.FirstSkill), $"Basic {HeroData.Skill(def.FirstSkill).Name}", HeroData.Skill(def.FirstSkill).Description);
            if (def.SecondSkill != def.FirstSkill)
            {
                Known(knows, Manager.Art.Skill(def.SecondSkill), $"Basic {HeroData.Skill(def.SecondSkill).Name}", HeroData.Skill(def.SecondSkill).Description);
            }

            // What he starts with in the four primary skills.
            RectTransform stats = UIKit.Rect(content, "Stats");
            stats.anchorMin = new Vector2(0f, 1f);
            stats.anchorMax = new Vector2(1f, 1f);
            stats.pivot = new Vector2(0.5f, 1f);
            stats.offsetMin = new Vector2(0f, -222f);
            stats.offsetMax = new Vector2(0f, -164f);
            HorizontalLayoutGroup statRow = UIKit.Layout<HorizontalLayoutGroup>(stats, 8f);
            statRow.childForceExpandWidth = true;
            statRow.childForceExpandHeight = true;
            string[] statNames = { "Attack", "Defense", "Power", "Knowledge" };
            for (int i = 0; i < 4; i++)
            {
                Sprite icon = Manager.Art.statIcons.Length > i ? Manager.Art.statIcons[i] : null;
                int value = heroClass.Start != null && heroClass.Start.Length > i ? heroClass.Start[i] : 0;
                TextMeshProUGUI figure = UIKit.Figure(stats, statNames[i], icon, false, $"{statNames[i]}\nWhat the hero starts with.", 90f, 56f);
                figure.text = value.ToString();
                figure.color = UIKit.Gold;
                figure.fontSizeMax = 24f;
            }

            TextMeshProUGUI story = UIKit.Label(content, "Story", def.Biography, 19f, UIKit.Ink, TextAlignmentOptions.TopLeft);
            story.fontStyle = FontStyles.Italic;
            RectTransform storyRect = (RectTransform)story.transform;
            storyRect.anchorMin = new Vector2(0f, 0f);
            storyRect.anchorMax = new Vector2(1f, 1f);
            storyRect.offsetMin = new Vector2(4f, 70f);
            storyRect.offsetMax = new Vector2(-4f, -234f);
            story.enableAutoSizing = true;
            story.fontSizeMax = 19f;
            story.fontSizeMin = 13f;
            story.overflowMode = TextOverflowModes.Ellipsis;

            int which = slot;
            Button hire = UIKit.Push(content, "Hire", $"Hire for {UIKit.Glyph(ResourceKind.Gold)} {HeroData.HireCost:N0}", () => Hire(which), 22f);
            UIKit.Pin((RectTransform)hire.transform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300f, 56f));
            UIKit.Enable(hire, can);
        }

        private static void Known(RectTransform parent, Sprite icon, string text, string tip)
        {
            RectTransform row = UIKit.Rect(parent, text);
            UIKit.Fit(row, 0f, 32f, true);
            Image image = UIKit.Sprite(row, "Icon", icon, Color.white);
            image.preserveAspect = true;
            UIKit.Pin((RectTransform)image.transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            TextMeshProUGUI label = UIKit.Label(row, "Name", text, 18f, UIKit.Ink, TextAlignmentOptions.MidlineLeft);
            UIKit.Stretch((RectTransform)label.transform, 38f, 0f, 0f, 0f);
            UIKit.FitLine(label, 18f, 12f);
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Tooltip.Attach(row.gameObject, text, tip, icon);
        }

        private void Hire(int slot)
        {
            if (town != null && town.owner == Manager.Viewer && MayCommand)
            {
                Manager.Commands.Hire(town.id, slot);
                Close();
            }
        }
    }

    /// <summary>
    /// The marketplace: one resource given for another, at a rate the number of the player's marketplaces improves.
    /// The goods are tiles with what the player holds of each, and under the goods to receive what each would fetch.
    /// </summary>
    public sealed class MarketBox : Dialog
    {
        private const float TileWidth = 118f;
        private const float TileHeight = 112f;

        private TownState town;
        private ResourceKind give = ResourceKind.Wood;
        private ResourceKind take = ResourceKind.Gold;
        private TextMeshProUGUI summary;
        private Button once;
        private Button ten;
        private readonly List<Tile> giveTiles = new List<Tile>();
        private readonly List<Tile> takeTiles = new List<Tile>();

        private sealed class Tile
        {
            public Image Card;
            public TextMeshProUGUI Line;
        }

        public static MarketBox Make(Transform parent, HeroesGameManager manager)
        {
            MarketBox box = Build<MarketBox>(parent, manager, "Market", new Vector2(1000f, 610f), "Marketplace");
            RectTransform body = box.Body;
            Caption(body, "GiveLabel", "You give", 0f);
            RectTransform giveRow = Row(body, "Give", -36f);
            Caption(body, "TakeLabel", "You receive", -168f);
            RectTransform takeRow = Row(body, "Take", -204f);
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                var kind = (ResourceKind)i;
                box.giveTiles.Add(box.MakeTile(giveRow, kind, () => box.SetGive(kind)));
                box.takeTiles.Add(box.MakeTile(takeRow, kind, () => box.SetTake(kind)));
            }

            Image plate = UIKit.Recess(body, "Summary");
            UIKit.Pin((RectTransform)plate.transform, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(760f, 66f));
            box.summary = UIKit.Label(plate.transform, "Text", "", 24f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Stretch((RectTransform)box.summary.transform, 16f, 4f, 16f, 4f);
            UIKit.FitLine(box.summary, 24f, 14f);

            box.once = box.Answer("Trade Once", () => box.Trade(1));
            box.ten = box.Answer("Trade Ten", () => box.Trade(10));
            box.Answer("Done", box.Close);
            return box;
        }

        private static void Caption(RectTransform body, string name, string text, float y)
        {
            TextMeshProUGUI label = UIKit.Title(body, name, text, 22f, UIKit.Gold);
            UIKit.Pin((RectTransform)label.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(400f, 30f));
            UIKit.Look(label, TextLook.Gold);
        }

        private static RectTransform Row(RectTransform body, string name, float y)
        {
            RectTransform row = UIKit.Rect(body, name);
            UIKit.Pin(row, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(ResourceSet.Kinds * (TileWidth + 10f) - 10f, TileHeight));
            UIKit.Layout<HorizontalLayoutGroup>(row, 10f).childAlignment = TextAnchor.MiddleCenter;
            return row;
        }

        private Tile MakeTile(RectTransform row, ResourceKind kind, System.Action onClick)
        {
            Button button = UIKit.CardButton(row, kind.ToString(), onClick);
            var card = (Image)button.targetGraphic;
            UIKit.Fit((RectTransform)card.transform, TileWidth, TileHeight);
            RectTransform content = UIKit.Content(card, 3f);
            Image icon = UIKit.Sprite(content, "Icon", Manager.Art.Resource(kind), Color.white);
            icon.preserveAspect = true;
            UIKit.Pin((RectTransform)icon.transform, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(58f, 58f));
            TextMeshProUGUI line = UIKit.Label(content, "Line", "", 19f, UIKit.Ink, TextAlignmentOptions.Center);
            RectTransform lineRect = (RectTransform)line.transform;
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, 0f);
            lineRect.pivot = new Vector2(0.5f, 0f);
            lineRect.offsetMin = new Vector2(2f, 2f);
            lineRect.offsetMax = new Vector2(-2f, 30f);
            UIKit.FitLine(line, 19f, 12f);
            Tooltip.Attach(card.gameObject, Land.ResourceName(kind));
            return new Tile { Card = card, Line = line };
        }

        public void Show(TownState which)
        {
            town = which;
            Open();
            Refresh();
        }

        private void SetGive(ResourceKind kind)
        {
            give = kind;
            if (take == give)
            {
                take = give == ResourceKind.Gold ? ResourceKind.Wood : ResourceKind.Gold;
            }
            Refresh();
        }

        private void SetTake(ResourceKind kind)
        {
            take = kind;
            Refresh();
        }

        private void Trade(int amount)
        {
            if (MayCommand && give != take)
            {
                Manager.Commands.Trade(give, take, amount);
            }
        }

        public void Refresh()
        {
            if (!IsOpen || Manager.Game == null)
            {
                return;
            }
            PlayerState owner = Manager.ViewerState;
            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                var kind = (ResourceKind)i;
                UIKit.Select(giveTiles[i].Card, kind == give);
                UIKit.Select(takeTiles[i].Card, kind == take);
                giveTiles[i].Line.text = owner != null ? owner.resources[kind].ToString("N0") : "0";
                giveTiles[i].Line.color = kind == give ? UIKit.Gold : UIKit.Ink;
                int rate = Manager.Game.TradeRate(Manager.Viewer, give, kind);
                takeTiles[i].Line.text = kind == give || rate == 0 ? "-" : rate < 0 ? $"{-rate:N0} each" : $"1 for {rate}";
                takeTiles[i].Line.color = kind == take ? UIKit.Gold : UIKit.Dim;
            }
            int received = Manager.Game.TradeRate(Manager.Viewer, give, take);
            int held = owner != null ? owner.resources[give] : 0;
            // A positive rate is how much has to be given for one unit; a negative one is the gold a unit sells for.
            int price = received < 0 ? 1 : received;
            bool can = give != take && received != 0 && held >= price && !Manager.HeroesUI.Busy;
            summary.text = give == take ? "Choose two different goods."
                : received == 0 ? "You have no marketplace."
                : received < 0
                    ? $"{UIKit.Glyph(give)} 1  →  {UIKit.Glyph(take)} {-received:N0}      <color=#B8AB8F>you hold {held:N0}</color>"
                    : $"{UIKit.Glyph(give)} {received}  →  {UIKit.Glyph(take)} 1      <color=#B8AB8F>you hold {held:N0}</color>";
            UIKit.Enable(once, can);
            UIKit.Enable(ten, can && held >= price * 10);
        }
    }
}
