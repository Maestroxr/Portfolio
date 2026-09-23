using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The screen of a town: what has been built and what may still be built today, the creatures waiting in its
    /// dwellings, the garrison and the hero visiting, the market and the tavern. Everything on it goes through the
    /// commands, so an online town behaves exactly like one at this device.
    /// </summary>
    public sealed class TownScreen : Dialog
    {
        private TownState town;
        private RectTransform buildings;
        private RectTransform dwellings;
        private ArmyRow garrison;
        private ArmyRow visitor;
        private TextMeshProUGUI visitorName;
        private TextMeshProUGUI income;
        private Button tavern;
        private Button market;
        private MarketBox marketBox;

        public static TownScreen Make(Transform parent, HeroesGameManager manager)
        {
            TownScreen screen = Build<TownScreen>(parent, manager, "Town", new Vector2(1480f, 880f), "Town");

            Image left = UIKit.Panel(screen.Body, "Buildings", 0.85f);
            RectTransform leftRect = UIKit.Pin((RectTransform)left.transform, new Vector2(0f, 1f), new Vector2(0f, 0f),
                new Vector2(680f, 470f));
            UIKit.Title(leftRect, "Caption", "Buildings", 24f, UIKit.Gold);
            UIKit.Pin((RectTransform)leftRect.GetChild(0), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(300f, 30f));
            screen.buildings = UIKit.Scroll(leftRect, "Scroll", out ScrollRect buildScroll);
            UIKit.Stretch((RectTransform)buildScroll.transform, 16f, 16f, 16f, 50f);
            UIKit.Grid(screen.buildings, new Vector2(206f, 92f), new Vector2(8f, 8f), 3);

            Image right = UIKit.Panel(screen.Body, "Dwellings", 0.85f);
            RectTransform rightRect = UIKit.Pin((RectTransform)right.transform, new Vector2(1f, 1f), new Vector2(0f, 0f),
                new Vector2(700f, 470f));
            UIKit.Title(rightRect, "Caption", "Recruit", 24f, UIKit.Gold);
            UIKit.Pin((RectTransform)rightRect.GetChild(0), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(300f, 30f));
            screen.dwellings = UIKit.Rect(rightRect, "List");
            UIKit.Stretch(screen.dwellings, 16f, 16f, 16f, 50f);
            VerticalLayoutGroup dwellingLayout = UIKit.Layout<VerticalLayoutGroup>(screen.dwellings, 6f);
            dwellingLayout.childForceExpandWidth = true;

            // The armies along the bottom: the garrison that holds the walls and the hero who came to call.
            Image bottom = UIKit.Panel(screen.Body, "Armies", 0.85f);
            RectTransform bottomRect = UIKit.Pin((RectTransform)bottom.transform, new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(1390f, 210f));
            TextMeshProUGUI garrisonName = UIKit.Label(bottomRect, "GarrisonName", "Garrison", 22f, UIKit.Dim);
            UIKit.Pin((RectTransform)garrisonName.transform, new Vector2(0f, 1f), new Vector2(24f, -12f), new Vector2(400f, 26f));
            screen.garrison = ArmyRow.Make(bottomRect, manager, 78f);
            UIKit.Pin((RectTransform)screen.garrison.transform, new Vector2(0f, 1f), new Vector2(24f, -42f), new Vector2(640f, 82f));

            screen.visitorName = UIKit.Label(bottomRect, "VisitorName", "No hero visiting", 22f, UIKit.Dim);
            UIKit.Pin((RectTransform)screen.visitorName.transform, new Vector2(0f, 1f), new Vector2(24f, -132f), new Vector2(400f, 26f));
            screen.visitor = ArmyRow.Make(bottomRect, manager, 78f);
            UIKit.Pin((RectTransform)screen.visitor.transform, new Vector2(0f, 1f), new Vector2(24f, -162f), new Vector2(640f, 82f));

            screen.income = UIKit.Label(bottomRect, "Income", "", 22f, UIKit.Ink, TextAlignmentOptions.TopRight);
            UIKit.Pin((RectTransform)screen.income.transform, new Vector2(1f, 1f), new Vector2(-24f, -12f), new Vector2(430f, 80f));

            screen.tavern = UIKit.Push(bottomRect, "Tavern", "Tavern", screen.OpenTavern, 24f);
            UIKit.Pin((RectTransform)screen.tavern.transform, new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(200f, 56f));
            screen.market = UIKit.Push(bottomRect, "Market", "Marketplace", screen.OpenMarket, 24f);
            UIKit.Pin((RectTransform)screen.market.transform, new Vector2(1f, 0f), new Vector2(-236f, 24f), new Vector2(200f, 56f));

            screen.Answer("Leave", screen.Close);
            screen.marketBox = MarketBox.Make(parent, manager);
            return screen;
        }

        public void Open(TownState which)
        {
            town = which;
            ArmyRow.Drop();
            Open();
            Refresh();
            Manager.Sound?.PlayTownMusic(which.faction);
        }

        public override void Close()
        {
            base.Close();
            ArmyRow.Drop();
            if (town != null && Manager.Game != null && !Manager.Game.InBattle)
            {
                Manager.Sound?.PlayAdventureMusic();
            }
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
            Heading.text = $"{town.name} — {Land.FactionName(town.faction)}";
            BuildingList();
            DwellingList();

            garrison.Bind(Holder.Garrison(town.id));
            HeroState hero = Manager.Game.State.HeroAt(town.cell);
            if (hero != null && hero.owner == town.owner)
            {
                visitor.gameObject.SetActive(true);
                visitor.Bind(Holder.Hero(hero.id));
                visitorName.text = $"{hero.Name}, level {hero.level}";
            }
            else
            {
                visitor.gameObject.SetActive(false);
                visitorName.text = "No hero is visiting";
            }

            income.text = $"Income: {Buildings.Income(town)} gold a day\n" +
                          $"Growth: +{Buildings.GrowthBonus(town)}%\n" +
                          $"Mage Guild: level {Buildings.MageGuildLevel(town)}";
            bool mine = town.owner == Manager.Viewer && !Manager.HeroesUI.Busy;
            UIKit.Enable(tavern, mine && town.Has(BuildingId.Tavern));
            UIKit.Enable(market, mine && town.Has(BuildingId.Marketplace));
        }

        /// <summary>Every building of the town: built, buildable today, or out of reach and why.</summary>
        private void BuildingList()
        {
            Clear(buildings);
            PlayerState owner = Manager.Game.State.Player(town.owner);
            for (int i = 0; i < Buildings.Count; i++)
            {
                var id = (BuildingId)i;
                BuildingDef def = Buildings.Get(town.faction, id);
                if (def == null || id == BuildingId.VillageHall)
                {
                    continue;
                }
                bool built = town.Has(id);
                bool affordable = owner != null && owner.resources.CanAfford(def.Cost);
                bool ready = Ready(def);
                bool can = !built && ready && affordable && !town.builtToday && town.owner == Manager.Viewer;

                Image card = UIKit.Panel(buildings, def.Name, built ? 1f : 0.6f);
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.colors = UIKit.Tint();
                button.interactable = can;
                card.color = built ? new Color(0.75f, 0.95f, 0.75f) : Color.white;
                BuildingId building = id;
                button.onClick.AddListener(() => Manager.Commands.Build(town.id, building));
                card.gameObject.AddComponent<Clicker>();

                TextMeshProUGUI title = UIKit.Label(card.transform, "Name", def.Name, 19f, built ? UIKit.Good : UIKit.Ink);
                UIKit.Pin((RectTransform)title.transform, new Vector2(0f, 1f), new Vector2(10f, -8f), new Vector2(192f, 44f));
                TextMeshProUGUI cost = UIKit.Label(card.transform, "Cost", built ? "Built" : def.Cost.ToString(), 16f,
                    built ? UIKit.Dim : affordable ? UIKit.Gold : UIKit.Bad);
                UIKit.Pin((RectTransform)cost.transform, new Vector2(0f, 0f), new Vector2(10f, 8f), new Vector2(192f, 34f));
                Tooltip.Attach(card.gameObject, Explain(def, built, ready, affordable));
            }
        }

        private string Explain(BuildingDef def, bool built, bool ready, bool affordable)
        {
            if (built)
            {
                return $"{def.Name}\n{def.Description}";
            }
            var text = new System.Text.StringBuilder();
            text.Append(def.Name).Append('\n').Append(def.Description).Append('\n').Append("Costs ").Append(def.Cost);
            if (!ready)
            {
                text.Append("\nNeeds: ");
                foreach (BuildingId need in def.Requires)
                {
                    if (!town.Has(need))
                    {
                        text.Append(Buildings.Get(town.faction, need).Name).Append(' ');
                    }
                }
            }
            else if (!affordable)
            {
                text.Append("\nYou cannot afford it yet.");
            }
            else if (town.builtToday)
            {
                text.Append("\nOnly one building a day.");
            }
            return text.ToString();
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

        /// <summary>The seven tiers, with what is waiting and what it costs.</summary>
        private void DwellingList()
        {
            Clear(dwellings);
            PlayerState owner = Manager.Game.State.Player(town.owner);
            HeroState hero = Manager.Game.State.HeroAt(town.cell);
            bool visiting = hero != null && hero.owner == town.owner;
            for (int tier = 1; tier <= 7; tier++)
            {
                BuildingId dwelling = Buildings.DwellingOf(tier);
                if (!town.Has(dwelling))
                {
                    continue;
                }
                CreatureDef def = Creatures.Get(Creatures.OfTier(town.faction, tier));
                if (def == null)
                {
                    continue;
                }
                int waiting = town.available[tier - 1];
                int affordable = owner != null ? owner.resources.Times(def.Cost) : 0;
                int most = Mathf.Min(waiting, affordable);

                Image row = UIKit.Panel(dwellings, def.Name, 0.7f);
                UIKit.Fit((RectTransform)row.transform, 0f, 62f, true);
                TextMeshProUGUI title = UIKit.Label(row.transform, "Name", $"{def.Plural}", 20f, UIKit.Ink);
                UIKit.Pin((RectTransform)title.transform, new Vector2(0f, 0.5f), new Vector2(14f, 8f), new Vector2(300f, 26f));
                TextMeshProUGUI note = UIKit.Label(row.transform, "Note", $"{waiting} available — {def.Cost}", 16f, UIKit.Dim);
                UIKit.Pin((RectTransform)note.transform, new Vector2(0f, 0.5f), new Vector2(14f, -14f), new Vector2(340f, 22f));

                int count = most;
                int level = tier;
                Button one = UIKit.Push(row.transform, "One", "Recruit", () => Manager.Commands.Recruit(town.id, level, 1, visiting), 20f);
                UIKit.Pin((RectTransform)one.transform, new Vector2(1f, 0.5f), new Vector2(-136f, 0f), new Vector2(126f, 44f));
                UIKit.Enable(one, most >= 1 && town.owner == Manager.Viewer);
                Button all = UIKit.Push(row.transform, "All", $"All ({most})", () => Manager.Commands.Recruit(town.id, level, count, visiting), 20f);
                UIKit.Pin((RectTransform)all.transform, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(126f, 44f));
                UIKit.Enable(all, most >= 1 && town.owner == Manager.Viewer);
                Tooltip.Attach(row.gameObject, $"{def.Name}\nGrowth {def.Growth} a week\nCosts {def.Cost}");
            }
            if (dwellings.childCount == 0)
            {
                TextMeshProUGUI empty = UIKit.Label(dwellings, "Empty", "Build a dwelling to recruit creatures.", 20f, UIKit.Dim,
                    TextAlignmentOptions.Center);
                UIKit.Fit((RectTransform)empty.transform, 0f, 40f, true);
            }
        }

        private static void Clear(RectTransform rect)
        {
            for (int i = rect.childCount - 1; i >= 0; i--)
            {
                Destroy(rect.GetChild(i).gameObject);
            }
        }

        private void OpenMarket()
        {
            marketBox.Show(town);
        }

        /// <summary>The tavern: the two heroes waiting there, and what they cost.</summary>
        private void OpenTavern()
        {
            PlayerState owner = Manager.Game.State.Player(town.owner);
            if (owner == null)
            {
                return;
            }
            ClearButtons();
            for (int slot = 0; slot < owner.tavern.Length; slot++)
            {
                int which = slot;
                HeroDef def = HeroData.Hero(owner.tavern[slot]);
                if (def == null)
                {
                    continue;
                }
                Button button = Answer($"{def.Name} ({HeroData.Class(def.Class).Name}) — {HeroData.HireCost}g", () =>
                {
                    Manager.Commands.Hire(town.id, which);
                    ClearButtons();
                    Answer("Leave", Close);
                });
                UIKit.Enable(button, owner.resources.Gold >= HeroData.HireCost && owner.heroes.Count < 8);
                Tooltip.Attach(button.gameObject, def.Biography);
            }
            Answer("Leave", Close);
        }
    }

    /// <summary>The marketplace: resources for resources, at a rate the number of your markets improves.</summary>
    public sealed class MarketBox : Dialog
    {
        private TownState town;
        private ResourceKind give = ResourceKind.Wood;
        private ResourceKind take = ResourceKind.Gold;
        private TextMeshProUGUI rate;
        private readonly List<Button> giveButtons = new List<Button>();
        private readonly List<Button> takeButtons = new List<Button>();

        public static MarketBox Make(Transform parent, HeroesGameManager manager)
        {
            MarketBox box = Build<MarketBox>(parent, manager, "Market", new Vector2(820f, 520f), "Marketplace");
            RectTransform giveRow = UIKit.Rect(box.Body, "Give");
            UIKit.Pin(giveRow, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 70f));
            UIKit.Layout<HorizontalLayoutGroup>(giveRow, 8f).childAlignment = TextAnchor.MiddleCenter;
            RectTransform takeRow = UIKit.Rect(box.Body, "Take");
            UIKit.Pin(takeRow, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(700f, 70f));
            UIKit.Layout<HorizontalLayoutGroup>(takeRow, 8f).childAlignment = TextAnchor.MiddleCenter;

            TextMeshProUGUI giveLabel = UIKit.Label(box.Body, "GiveLabel", "You give", 22f, UIKit.Dim, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)giveLabel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(400f, 26f));
            TextMeshProUGUI takeLabel = UIKit.Label(box.Body, "TakeLabel", "You receive", 22f, UIKit.Dim, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)takeLabel.transform, new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(400f, 26f));

            for (int i = 0; i < ResourceSet.Kinds; i++)
            {
                var kind = (ResourceKind)i;
                Button giveButton = UIKit.Icon(giveRow, $"Give{kind}", manager.Art.Resource(kind), () => box.SetGive(kind),
                    Land.ResourceName(kind));
                UIKit.Fit((RectTransform)giveButton.transform, 64f, 64f);
                box.giveButtons.Add(giveButton);
                Button takeButton = UIKit.Icon(takeRow, $"Take{kind}", manager.Art.Resource(kind), () => box.SetTake(kind),
                    Land.ResourceName(kind));
                UIKit.Fit((RectTransform)takeButton.transform, 64f, 64f);
                box.takeButtons.Add(takeButton);
            }

            box.rate = UIKit.Label(box.Body, "Rate", "", 24f, UIKit.Ink, TextAlignmentOptions.Center);
            UIKit.Pin((RectTransform)box.rate.transform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(700f, 60f));
            return box;
        }

        public void Show(TownState which)
        {
            town = which;
            ClearButtons();
            Answer("Trade Once", () => Trade(1));
            Answer("Trade Ten", () => Trade(10));
            Answer("Done", Close);
            Refresh();
            Open();
        }

        private void SetGive(ResourceKind kind)
        {
            give = kind;
            Refresh();
        }

        private void SetTake(ResourceKind kind)
        {
            take = kind;
            Refresh();
        }

        private void Trade(int amount)
        {
            Manager.Commands.Trade(give, take, amount);
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < giveButtons.Count; i++)
            {
                giveButtons[i].GetComponent<Image>().color = (ResourceKind)i == give ? UIKit.Gold : Color.white;
                takeButtons[i].GetComponent<Image>().color = (ResourceKind)i == take ? UIKit.Gold : Color.white;
            }
            PlayerState owner = Manager.ViewerState;
            int received = Manager.Game.TradeRate(Manager.Viewer, give, take);
            int held = owner != null ? owner.resources[give] : 0;
            // A positive rate is how much has to be given for one unit; a negative one is the gold a unit sells for.
            rate.text = give == take ? "Choose two different goods."
                : received == 0 ? "You have no marketplace."
                : received < 0
                    ? $"1 {Land.ResourceName(give)} → {-received} Gold   (you hold {held})"
                    : $"{received} {Land.ResourceName(give)} → 1 {Land.ResourceName(take)}   (you hold {held})";
        }
    }
}
