using System;
using System.Collections.Generic;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The game's content of the remote panel (<see cref="RemoteControlUI"/>): what a player sees on their own device
    /// while the match plays on the room's shared screen. From the top: the seat's cash, net worth and square, a line
    /// about what the table waits for; then the seat's properties with their buildings and the build, sell and
    /// mortgage buttons of the property manager; and at the bottom the decision the match waits for from the seat as big
    /// buttons (roll, pay the fine, buy or auction, bid or pass, end the turn, accept or decline an offer...), with Trade
    /// whenever the seat may manage: it turns the list into the trade builder (the other player, what is given and asked
    /// for, cash each way) with Propose and Cancel below (<see cref="TradeDraft"/>). Everything is read from
    /// <see cref="SeatPanelModel"/> and redrawn when it changes; a press goes through the one seam the interface uses
    /// (<see cref="MonopolyGameManager.Commands"/>), so the server, the log and the other devices see nothing but an
    /// ordinary move or offer. Once the match is over the list shows the standings. Built by
    /// <see cref="MonopolyOnlineController.BuildRemotePanel"/>, which keeps it in <see cref="MonopolyOnlineController.Panel"/>
    /// for the online tour, which plays through it.
    /// </summary>
    public sealed class MonopolyRemotePanel : MonoBehaviour
    {
        private const float InfoHeight = 236f;
        private const float ButtonHeight = 100f;
        private const float ButtonGap = 12f;
        private const float RowHeight = 84f;
        private const float RowButtonWidth = 230f;
        private const float StepButtonWidth = 130f;

        private readonly List<PanelChoice> choices = new List<PanelChoice>();
        private readonly List<PanelProperty> properties = new List<PanelProperty>();
        private MonopolyOnlineController online;
        private MonopolyGameManager manager;
        private OverlayKit kit;
        private TMP_Text cash, worth, position, headline, detail;
        private RectTransform listArea, listContent, buttons;
        private TradeDraft trade;
        private string drawn;

        /// <summary>Builds the panel into <paramref name="content"/> with the frame's kit.</summary>
        public static MonopolyRemotePanel Build(RectTransform content, RemotePanelKit kit, MonopolyOnlineController online)
        {
            RectTransform root = kit.Rest("Monopoly");
            var panel = root.gameObject.AddComponent<MonopolyRemotePanel>();
            panel.online = online;
            panel.manager = online != null ? online.Monopoly : null;
            panel.kit = kit.Kit;
            panel.Lay();
            panel.Redraw();
            return panel;
        }

        /// <summary>The seat this panel speaks for: the one played at this device, -1 outside a match.</summary>
        public int Seat => manager != null ? manager.LocalSeat : -1;

        /// <summary>The big buttons shown now, in order (<see cref="SeatPanelModel.Choices"/>); none while the trade builder is open.</summary>
        public IReadOnlyList<PanelChoice> Choices => choices;

        /// <summary>The properties listed now, with the buttons each shows (<see cref="SeatPanelModel.Properties"/>); none while the trade builder is open.</summary>
        public IReadOnlyList<PanelProperty> Properties => properties;

        /// <summary>The trade being put together while the builder is open, else null.</summary>
        public TradeDraft Trade => trade;

        /// <summary>Whether the panel is on screen (the frame shows and this content is in it).</summary>
        public bool IsShown => this != null && gameObject.activeInHierarchy;

        /// <summary>The first choice of <paramref name="kind"/> shown now (with <paramref name="argument"/> when given), or null.</summary>
        public PanelChoice Find(PanelChoiceKind kind, int? argument = null)
        {
            foreach (PanelChoice choice in choices)
            {
                if (choice.kind == kind && (argument == null || choice.argument == argument.Value))
                {
                    return choice;
                }
            }
            foreach (PanelProperty property in properties)
            {
                foreach (PanelChoice choice in property.actions)
                {
                    if (choice.kind == kind && (argument == null || choice.argument == argument.Value))
                    {
                        return choice;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Presses <paramref name="choice"/> for the seat (a button of the panel, or the tour): the command goes through
        /// <see cref="MonopolyGameManager.Commands"/>, an answer to an offer through the online controller, Trade opens the
        /// builder. Returns whether anything was sent or opened; a disabled choice does nothing.
        /// </summary>
        public bool Press(PanelChoice choice)
        {
            if (choice == null || !choice.enabled || manager == null || online == null || Seat < 0)
            {
                return false;
            }
            manager.MonopolyUI?.Sound?.Play(Sfx.Click);
            if (choice.kind == PanelChoiceKind.Trade)
            {
                return OpenTrade();
            }
            return SeatPanelModel.Perform(choice, Seat, manager.Commands, online.AnswerTrade);
        }

        // ------------------------------------------------------------------ the trade builder

        /// <summary>Opens the builder with a fresh draft (the first partner picked), when the seat may trade now. Returns whether it opened.</summary>
        public bool OpenTrade()
        {
            MonopolyMatch match = manager != null ? manager.Match : null;
            int seat = Seat;
            if (!SeatPanelModel.CanTrade(match, seat, online != null && online.MayManage(seat)))
            {
                return false;
            }
            trade = SeatPanelModel.NewTrade(match, seat);
            Redraw();
            return trade != null;
        }

        /// <summary>Closes the builder; nothing is sent.</summary>
        public void CancelTrade()
        {
            trade = null;
            Redraw();
        }

        /// <summary>Picks the other side of the trade (<see cref="SeatPanelModel.SelectPartner"/>).</summary>
        public void PickPartner(int partner)
        {
            SeatPanelModel.SelectPartner(trade, partner);
            Redraw();
        }

        /// <summary>Ticks a property into or out of what the player gives (<paramref name="give"/>) or asks for.</summary>
        public void ToggleSpace(int space, bool give)
        {
            SeatPanelModel.ToggleSpace(trade, space, give);
            Redraw();
        }

        /// <summary>Moves the cash the player gives (<paramref name="give"/>) or asks for by <paramref name="step"/>.</summary>
        public void AddCash(int step, bool give)
        {
            SeatPanelModel.ChangeCash(manager != null ? manager.Match : null, trade, step, give);
            Redraw();
        }

        /// <summary>Ticks a Get Out of Jail Free card into or out of what the player gives (<paramref name="give"/>) or asks for.</summary>
        public void ToggleCard(bool give)
        {
            SeatPanelModel.ToggleCard(trade, give);
            Redraw();
        }

        /// <summary>
        /// Proposes the draft: the offer the device's trade window would send goes through
        /// <see cref="IMonopolyCommands.ProposeTrade"/>, and the builder closes. Returns whether it was sent (the rules
        /// have to take the draft, and the seat has to be allowed to trade now).
        /// </summary>
        public bool ProposeTrade()
        {
            MonopolyMatch match = manager != null ? manager.Match : null;
            if (trade == null || !SeatPanelModel.ValidateTrade(match, trade, out _) || online == null || !online.MayManage(Seat))
            {
                return false;
            }
            TradeOffer offer = trade.ToOffer();
            trade = null;
            manager.MonopolyUI?.Sound?.Play(Sfx.Click);
            manager.Commands.ProposeTrade(offer);
            Redraw();
            return true;
        }

        // ------------------------------------------------------------------ drawing

        private void Update()
        {
            if (manager == null)
            {
                return;
            }
            if (Signature() != drawn)
            {
                Redraw();
            }
        }

        /// <summary>What the panel shows for the seat now, as a text: the model's picture plus the draft of the builder.</summary>
        private string Signature()
        {
            int seat = Seat;
            string signature = manager.Match == null ? "" : SeatPanelModel.Signature(manager.Match, manager.Lockstep, seat,
                online.MayDecide(seat), online.MayManage(seat), online.MayAnswer(seat));
            return trade != null ? signature + "\n#trade " + trade.Key : signature;
        }

        /// <summary>The parts that stay: the lines of the seat at the top, the list in the middle, the buttons at the bottom.</summary>
        private void Lay()
        {
            var root = (RectTransform)transform;
            OverlayLook look = kit.Look;
            cash = Line(root, "Cash", 64f, look.text, 0f, 78f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, true);
            worth = Line(root, "Worth", 34f, look.muted, -8f, 50f, TextAlignmentOptions.MidlineRight);
            position = Line(root, "Position", 32f, look.text, -84f, 44f, TextAlignmentOptions.MidlineLeft);
            headline = Line(root, "Headline", 42f, look.accent, -134f, 56f, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, true);
            detail = Line(root, "Detail", 28f, look.muted, -190f, 40f, TextAlignmentOptions.MidlineLeft);

            buttons = OverlayKit.Rect(root, "Buttons");
            buttons.anchorMin = new Vector2(0f, 0f);
            buttons.anchorMax = new Vector2(1f, 0f);
            buttons.pivot = new Vector2(0.5f, 0f);
            buttons.anchoredPosition = Vector2.zero;
            buttons.sizeDelta = new Vector2(0f, 0f);

            listContent = kit.ScrollList(root, "List", new Vector2(0f, 0f), new Vector2(0f, -InfoHeight), 8f);
            listArea = (RectTransform)listContent.parent;
        }

        private TMP_Text Line(RectTransform parent, string lineName, float size, Color color, float y, float height, TextAlignmentOptions alignment,
            FontStyles style = FontStyles.Normal, bool heading = false)
        {
            TMP_Text text = kit.Text(parent, lineName, "", size, color, alignment, style, heading);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(0f, height);
            OverlayKit.Fit(text, size);
            return text;
        }

        /// <summary>Draws what the model says for the seat now: the builder while a trade is being put together, else the decisions and the properties.</summary>
        private void Redraw()
        {
            MonopolyMatch match = manager != null ? manager.Match : null;
            LockstepMatch table = manager != null ? manager.Lockstep : null;
            int seat = Seat;
            choices.Clear();
            properties.Clear();
            OverlayKit.Clear(buttons);
            OverlayKit.Clear(listContent);
            if (match == null || seat < 0)
            {
                trade = null;
                drawn = "";
                cash.text = "";
                worth.text = "";
                position.text = "";
                headline.text = L.T("The game is starting...");
                detail.text = "";
                PlaceList(0f);
                return;
            }
            bool deciding = online.MayDecide(seat);
            bool managing = online.MayManage(seat);
            bool answering = online.MayAnswer(seat);
            if (trade != null && !SeatPanelModel.CanTrade(match, seat, managing))
            {
                // The turn moved on (the clock, an offer on the table): the builder goes with it.
                trade = null;
            }
            cash.text = SeatPanelModel.Cash(match, seat);
            worth.text = SeatPanelModel.Worth(match, seat);
            position.text = SeatPanelModel.Position(match, seat);
            headline.text = SeatPanelModel.Headline(match, table, seat);
            detail.text = SeatPanelModel.Detail(match, table, seat);
            drawn = Signature();

            if (trade != null)
            {
                DrawBuilder(match, seat);
                return;
            }

            choices.AddRange(SeatPanelModel.Choices(match, table, seat, deciding, answering, managing));
            var grid = new List<(string label, Color color, bool enabled, Action click)>();
            foreach (PanelChoice choice in choices)
            {
                PanelChoice pressed = choice;
                grid.Add((choice.label, choice.primary ? kit.Look.accent : kit.Look.row, choice.enabled, () => Press(pressed)));
            }
            DrawGrid(grid);

            if (match.IsOver)
            {
                foreach (PanelStanding standing in SeatPanelModel.Standings(match))
                {
                    RectTransform row = kit.Row(listContent, RowHeight);
                    Color color = MonopolyStyle.PlayerColor(match.players[standing.seat].color);
                    Square(row, color);
                    kit.Label(row, $"{standing.place}. {standing.name}", 34f, standing.seat == seat ? kit.Look.accent : kit.Look.text, new Vector2(54f, 0f), new Vector2(900f, 0f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                    string right = standing.bankrupt ? L.F("Bankrupt in round {0}", standing.bankruptRound) : MonopolyStyle.Money(standing.worth);
                    TMP_Text worthText = kit.Text(row, "Worth", right, 32f, kit.Look.muted, TextAlignmentOptions.MidlineRight);
                    OverlayKit.Anchor(worthText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(700f, RowHeight));
                    OverlayKit.Fit(worthText, 32f);
                }
                return;
            }

            properties.AddRange(SeatPanelModel.Properties(match, seat, managing));
            foreach (PanelProperty property in properties)
            {
                RectTransform row = kit.Row(listContent, RowHeight);
                Square(row, MonopolyStyle.GroupColor(property.group));
                TMP_Text nameText = kit.Text(row, "Name", property.name, 32f, kit.Look.text, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                OverlayKit.Anchor(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(54f, -4f), new Vector2(760f, 40f));
                OverlayKit.Fit(nameText, 32f);
                TMP_Text stateText = kit.Text(row, "State", property.state, 26f, property.mortgaged ? kit.Look.warn : kit.Look.muted, TextAlignmentOptions.MidlineLeft);
                OverlayKit.Anchor(stateText.rectTransform, new Vector2(0f, 0f), new Vector2(54f, 4f), new Vector2(760f, 36f));
                OverlayKit.Fit(stateText, 26f);
                for (int i = 0; i < property.actions.Count; i++)
                {
                    PanelChoice action = property.actions[property.actions.Count - 1 - i];
                    Button button = kit.MakeButton(row, action.label, action.kind == PanelChoiceKind.Build ? kit.Look.good : kit.Look.accent,
                        new Vector2(RowButtonWidth, 60f), () => Press(action), literal: true);
                    button.interactable = action.enabled;
                    OverlayKit.Anchor((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-12f - i * (RowButtonWidth + 10f), 0f), new Vector2(RowButtonWidth, 60f));
                }
            }
            if (properties.Count == 0)
            {
                RectTransform row = kit.Row(listContent, RowHeight);
                kit.Label(row, L.T("No properties yet"), 30f, kit.Look.muted, new Vector2(20f, 0f), new Vector2(900f, 0f), TextAlignmentOptions.MidlineLeft);
            }
        }

        /// <summary>
        /// The trade builder in the list: the other players to pick from, what the player gives (their offerable properties
        /// to tick, cash with step buttons, a Get Out of Jail Free card when they hold one), what they ask for from the
        /// other side the same way, and the rules' verdict; Propose and Cancel at the bottom.
        /// </summary>
        private void DrawBuilder(MonopolyMatch match, int seat)
        {
            PlayerState me = match.players[seat];
            PlayerState them = trade.to >= 0 ? match.players[trade.to] : null;
            bool valid = SeatPanelModel.ValidateTrade(match, trade, out _);
            DrawGrid(new List<(string, Color, bool, Action)>
            {
                (L.T("Propose"), kit.Look.accent, valid, () => ProposeTrade()),
                (L.T("Cancel"), kit.Look.row, true, CancelTrade)
            });

            // Who with.
            List<int> partners = SeatPanelModel.TradePartners(match, seat);
            RectTransform picks = kit.Row(listContent, RowHeight);
            int columns = Math.Max(1, partners.Count);
            for (int i = 0; i < partners.Count; i++)
            {
                int partner = partners[i];
                PlayerState player = match.players[partner];
                Button button = kit.MakeButton(picks, MonopolyStyle.NameOf(player), partner == trade.to ? MonopolyStyle.PlayerColor(player.color) : kit.Look.window,
                    new Vector2(0f, RowHeight - 16f), () => PickPartner(partner), literal: true);
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(i / (float)columns, 0.5f);
                rect.anchorMax = new Vector2((i + 1) / (float)columns, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(-ButtonGap, RowHeight - 16f);
            }

            // What the player gives.
            HeaderRow(L.F("You give ({0})", MonopolyStyle.Money(me.cash)));
            foreach (int space in SeatPanelModel.Offerable(match, seat))
            {
                SpaceRow(match, space, trade.giveSpaces.Contains(space), () => ToggleSpace(space, true));
            }
            CashRow(trade.giveCash, true);
            if (me.jailCards.Count > 0)
            {
                ToggleRow(L.T("Get Out of Jail Free card"), trade.giveCard, () => ToggleCard(true));
            }

            // What the player asks for.
            if (them != null)
            {
                HeaderRow(MonopolyStyle.Say(them, "{0} gives ({1})", $"{MonopolyStyle.Named(them)} {MonopolyStyle.Verb(them, "gives")} ({MonopolyStyle.Money(them.cash)})",
                    MonopolyStyle.Named(them), MonopolyStyle.Money(them.cash)));
                foreach (int space in SeatPanelModel.Offerable(match, trade.to))
                {
                    SpaceRow(match, space, trade.getSpaces.Contains(space), () => ToggleSpace(space, false));
                }
                CashRow(trade.getCash, false);
                if (them.jailCards.Count > 0)
                {
                    ToggleRow(L.T("Get Out of Jail Free card"), trade.getCard, () => ToggleCard(false));
                }
            }

            // The verdict.
            RectTransform status = kit.Row(listContent, RowHeight);
            TMP_Text verdict = kit.Label(status, SeatPanelModel.TradeStatus(match, trade), 28f, valid ? kit.Look.muted : kit.Look.warn, new Vector2(20f, 0f), new Vector2(1700f, 0f), TextAlignmentOptions.MidlineLeft);
            verdict.rectTransform.anchorMax = new Vector2(1f, 1f);
            verdict.rectTransform.sizeDelta = new Vector2(-40f, 0f);
        }

        /// <summary>Big buttons at the bottom of the panel in a grid of two columns (one when there is one button), the rows from the bottom up.</summary>
        private void DrawGrid(List<(string label, Color color, bool enabled, Action click)> entries)
        {
            int columns = entries.Count > 1 ? 2 : 1;
            int rows = (entries.Count + columns - 1) / columns;
            for (int i = 0; i < entries.Count; i++)
            {
                (string label, Color color, bool enabled, Action click) entry = entries[i];
                int column = i % columns;
                int row = rows - 1 - i / columns;
                Button button = kit.MakeButton(buttons, entry.label, entry.color, new Vector2(0f, ButtonHeight), entry.click, literal: true);
                button.interactable = entry.enabled;
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(column / (float)columns, 0f);
                rect.anchorMax = new Vector2((column + 1) / (float)columns, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, row * (ButtonHeight + ButtonGap));
                rect.sizeDelta = new Vector2(columns > 1 ? -ButtonGap : 0f, ButtonHeight);
            }
            PlaceList(rows > 0 ? rows * (ButtonHeight + ButtonGap) : 0f);
        }

        private void HeaderRow(string text)
        {
            RectTransform row = kit.Row(listContent, RowHeight - 20f);
            kit.Label(row, text, 30f, kit.Look.text, new Vector2(20f, 0f), new Vector2(1200f, 0f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        }

        /// <summary>A property to tick into the trade: its name and price (and that it is mortgaged), lit when ticked.</summary>
        private void SpaceRow(MonopolyMatch match, int space, bool ticked, Action toggle)
        {
            SpaceData data = match.Space(space);
            string label = $"{MonopolyStyle.SpaceName(data)}   {MonopolyStyle.Money(data.price)}{(match.Deed(space).mortgaged ? $"  ({L.T("mortgaged")})" : "")}";
            ToggleRow(label, ticked, toggle, MonopolyStyle.GroupColor(data.group));
        }

        /// <summary>A row that is ticked or not by pressing it: lit in the colour of good news while ticked.</summary>
        private void ToggleRow(string label, bool ticked, Action toggle, Color? mark = null)
        {
            Button button = kit.MakeButton(listContent, label, ticked ? kit.Look.good : kit.Look.row, new Vector2(0f, RowHeight), toggle, literal: true);
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = RowHeight;
            element.preferredHeight = RowHeight;
            TMP_Text text = OverlayKit.LabelOf(button);
            if (text != null)
            {
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.margin = new Vector4(mark != null ? 60f : 20f, 0f, 20f, 0f);
            }
            if (mark != null)
            {
                Square((RectTransform)button.transform, mark.Value);
            }
        }

        /// <summary>The cash a side gives: the amount with a minus and a plus button per step (<see cref="SeatPanelModel.CashSteps"/>).</summary>
        private void CashRow(int amount, bool give)
        {
            RectTransform row = kit.Row(listContent, RowHeight);
            kit.Label(row, MonopolyStyle.Money(amount), 36f, kit.Look.text, new Vector2(20f, 0f), new Vector2(400f, 0f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            int[] steps = SeatPanelModel.CashSteps;
            var labels = new List<(string label, int step)>();
            for (int i = steps.Length - 1; i >= 0; i--)
            {
                labels.Add(($"-{steps[i]}", -steps[i]));
            }
            foreach (int step in steps)
            {
                labels.Add(($"+{step}", step));
            }
            for (int i = 0; i < labels.Count; i++)
            {
                int step = labels[i].step;
                Button button = kit.MakeButton(row, labels[i].label, step > 0 ? kit.Look.accent : kit.Look.window, new Vector2(StepButtonWidth, 60f), () => AddCash(step, give), literal: true);
                OverlayKit.Anchor((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-12f - (labels.Count - 1 - i) * (StepButtonWidth + 10f), 0f), new Vector2(StepButtonWidth, 60f));
            }
        }

        /// <summary>The list fills what the lines at the top and the buttons at the bottom leave.</summary>
        private void PlaceList(float buttonsHeight)
        {
            buttons.sizeDelta = new Vector2(0f, buttonsHeight);
            listArea.offsetMin = new Vector2(0f, buttonsHeight > 0f ? buttonsHeight + ButtonGap : 0f);
            listArea.offsetMax = new Vector2(0f, -InfoHeight);
        }

        private static void Square(RectTransform row, Color color)
        {
            Image square = OverlayKit.NewImage(row, "Colour", color);
            square.raycastTarget = false;
            OverlayKit.Anchor(square.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(28f, 28f));
        }
    }
}
