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
    /// buttons (roll, pay the fine, buy or auction, bid or pass, end the turn, accept or decline an offer...). Everything
    /// is read from <see cref="SeatPanelModel"/> and redrawn when it changes; a press goes through the one seam the
    /// interface uses (<see cref="MonopolyGameManager.Commands"/>), so the server, the log and the other devices see
    /// nothing but an ordinary move. Once the match is over the list shows the standings. Built by
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

        private readonly List<PanelChoice> choices = new List<PanelChoice>();
        private readonly List<PanelProperty> properties = new List<PanelProperty>();
        private MonopolyOnlineController online;
        private MonopolyGameManager manager;
        private OverlayKit kit;
        private TMP_Text cash, worth, position, headline, detail;
        private RectTransform listArea, listContent, buttons;
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

        /// <summary>The big buttons shown now, in order (<see cref="SeatPanelModel.Choices"/>).</summary>
        public IReadOnlyList<PanelChoice> Choices => choices;

        /// <summary>The properties listed now, with the buttons each shows (<see cref="SeatPanelModel.Properties"/>).</summary>
        public IReadOnlyList<PanelProperty> Properties => properties;

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
        /// <see cref="MonopolyGameManager.Commands"/>, an answer to an offer through the online controller. Returns whether
        /// anything was sent; a disabled choice sends nothing.
        /// </summary>
        public bool Press(PanelChoice choice)
        {
            if (choice == null || !choice.enabled || manager == null || online == null || Seat < 0)
            {
                return false;
            }
            manager.MonopolyUI?.Sound?.Play(Sfx.Click);
            return SeatPanelModel.Perform(choice, Seat, manager.Commands, online.AnswerTrade);
        }

        private void Update()
        {
            if (manager == null)
            {
                return;
            }
            int seat = Seat;
            string signature = manager.Match == null ? "" : SeatPanelModel.Signature(manager.Match, manager.Lockstep, seat,
                online.MayDecide(seat), online.MayManage(seat), online.MayAnswer(seat));
            if (signature != drawn)
            {
                Redraw();
            }
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

        /// <summary>Draws what the model says for the seat now.</summary>
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
            drawn = SeatPanelModel.Signature(match, table, seat, deciding, managing, answering);
            cash.text = SeatPanelModel.Cash(match, seat);
            worth.text = SeatPanelModel.Worth(match, seat);
            position.text = SeatPanelModel.Position(match, seat);
            headline.text = SeatPanelModel.Headline(match, table, seat);
            detail.text = SeatPanelModel.Detail(match, table, seat);

            choices.AddRange(SeatPanelModel.Choices(match, table, seat, deciding, answering));
            int columns = choices.Count > 1 ? 2 : 1;
            int rows = (choices.Count + columns - 1) / columns;
            for (int i = 0; i < choices.Count; i++)
            {
                PanelChoice choice = choices[i];
                int column = i % columns;
                int row = rows - 1 - i / columns;
                Button button = kit.MakeButton(buttons, choice.label, choice.primary ? kit.Look.accent : kit.Look.row, new Vector2(0f, ButtonHeight), () => Press(choice), literal: true);
                button.interactable = choice.enabled;
                var rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(column / (float)columns, 0f);
                rect.anchorMax = new Vector2((column + 1) / (float)columns, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, row * (ButtonHeight + ButtonGap));
                rect.sizeDelta = new Vector2(columns > 1 ? -ButtonGap : 0f, ButtonHeight);
            }
            PlaceList(rows > 0 ? rows * (ButtonHeight + ButtonGap) : 0f);

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
