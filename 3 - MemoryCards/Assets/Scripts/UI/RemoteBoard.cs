using System.Collections.Generic;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.MemoryCards.MemoryCardsText;

namespace Portfolio.MemoryCards
{
    /// <summary>What a card of the remote panel shows: its back, its face, its face faintly once it was found, or next to nothing once it was spent.</summary>
    public enum RemoteCardLook
    {
        Back,
        Face,
        Found,
        Gone
    }

    /// <summary>
    /// The sums of the remote panel's board (<see cref="RemoteBoard"/>), apart from the objects: where the cards go in the
    /// panel, what each of them shows, and what the line under the score says. Here so the tests can check them.
    /// </summary>
    public static class RemoteBoardLayout
    {
        /// <summary>The width of a card for its height, as the cards of the board (150 by 200).</summary>
        public const float Aspect = 0.75f;

        /// <summary>How many rows <paramref name="cards"/> cards make in <paramref name="columns"/> columns, as the board lays them out.</summary>
        public static int Rows(int cards, int columns)
        {
            return columns > 0 && cards > 0 ? (cards + columns - 1) / columns : 0;
        }

        /// <summary>
        /// The size of a card so that <paramref name="columns"/> by <paramref name="rows"/> of them, <paramref name="gap"/>
        /// apart, fit <paramref name="area"/> and keep the shape of a card.
        /// </summary>
        public static Vector2 CellSize(int columns, int rows, Vector2 area, float gap)
        {
            if (columns <= 0 || rows <= 0)
            {
                return Vector2.zero;
            }
            float width = (area.x - gap * (columns - 1)) / columns;
            float height = (area.y - gap * (rows - 1)) / rows;
            float cellWidth = Mathf.Max(0f, Mathf.Min(width, height * Aspect));
            return new Vector2(cellWidth, cellWidth / Aspect);
        }

        /// <summary>
        /// The middle of card <paramref name="index"/> in a grid of <paramref name="columns"/> by <paramref name="rows"/>
        /// cells of <paramref name="cell"/>, centred on the origin: index 0 at the top left, then along the row, as on the board.
        /// </summary>
        public static Vector2 CellPosition(int index, int columns, int rows, Vector2 cell, float gap)
        {
            int column = columns > 0 ? index % columns : 0;
            int row = columns > 0 ? index / columns : 0;
            float totalWidth = columns * cell.x + (columns - 1) * gap;
            float totalHeight = rows * cell.y + (rows - 1) * gap;
            return new Vector2(-totalWidth * 0.5f + cell.x * 0.5f + column * (cell.x + gap), totalHeight * 0.5f - cell.y * 0.5f - row * (cell.y + gap));
        }

        /// <summary>What a card of the mirror shows on the panel.</summary>
        public static RemoteCardLook LookOf(MemoryCard card)
        {
            switch (card != null ? card.State : CardState.Hidden)
            {
                case CardState.Revealed:
                    return RemoteCardLook.Face;
                case CardState.Matched:
                    return RemoteCardLook.Found;
                case CardState.Spent:
                    return RemoteCardLook.Gone;
                default:
                    return RemoteCardLook.Back;
            }
        }

        /// <summary>
        /// The colour a card is drawn in: a back is white (bluish under ice) and dimmed while the player may not tap; a face
        /// that is up is white; a set that was found stays faintly; a spent card is all but gone.
        /// </summary>
        public static Color TintOf(RemoteCardLook look, bool frozen, bool mayTap)
        {
            switch (look)
            {
                case RemoteCardLook.Face:
                    return Color.white;
                case RemoteCardLook.Found:
                    return new Color(1f, 1f, 1f, 0.38f);
                case RemoteCardLook.Gone:
                    return new Color(1f, 1f, 1f, 0.1f);
                default:
                    Color back = frozen ? new Color(0.72f, 0.88f, 1f, 1f) : Color.white;
                    return mayTap ? back : back * new Color(0.62f, 0.62f, 0.62f, 1f);
            }
        }

        /// <summary>The line under the score: what the player can do now (a key of the game's words).</summary>
        public static string HintKey(bool memorizing, bool mayTap)
        {
            return memorizing ? K("Memorize the board") : mayTap ? K("Tap a card") : K("Wait for your turn");
        }
    }

    /// <summary>
    /// The board of a phone that plays as a remote control of a shared screen: the content of the remote panel
    /// (<see cref="MemoryCardsOnlineController.BuildRemotePanel"/>). The device runs the game underneath as every client
    /// does, and this grid shows its mirror sized to the panel: the backs of the hidden cards, the faces of the cards that
    /// are up, the sets found faintly, with the player's score, sets and mistakes above and a line that says whether they
    /// may tap. A tap on a hidden card on the player's turn flips it the way a click on the board does
    /// (<see cref="MemoryCardsGameManager.ClickCard"/>: the same gates, the same reducer), so the panel is another face of
    /// the same game; the tour's pilot taps here too. It follows the mirror every frame, and lays itself out again for a new
    /// board or another size of the panel.
    /// </summary>
    public sealed class RemoteBoard : MonoBehaviour
    {
        /// <summary>The space between two cards, in the units of the panel.</summary>
        public const float Gap = 12f;
        private const float StatusHeight = 140f;

        private readonly List<Cell> cells = new List<Cell>();
        private MemoryCardsOnlineController online;
        private RectTransform area;
        private TMP_Text score, tally, hint;
        private MemoryRound shownRound;
        private Vector2 shownArea;
        private int shownColumns;

        /// <summary>Builds the panel's content with <paramref name="kit"/>: the status strip at the top, then the board in what is left.</summary>
        public static RemoteBoard Build(RemotePanelKit kit, MemoryCardsOnlineController online)
        {
            OverlayLook look = kit.Kit.Look;
            RectTransform status = kit.Section("Status", StatusHeight);
            RectTransform area = kit.Rest("Board");
            var board = area.gameObject.AddComponent<RemoteBoard>();
            board.online = online;
            board.area = area;
            board.score = kit.Kit.Text(status, "Score", "", 54f, look.text, TextAlignmentOptions.MidlineLeft, FontStyles.Bold, true);
            OverlayKit.Anchor(board.score.rectTransform, new Vector2(0f, 1f), Vector2.zero, new Vector2(900f, 72f));
            OverlayKit.Fit(board.score, 54f);
            board.tally = kit.Kit.Text(status, "Tally", "", 34f, look.muted, TextAlignmentOptions.MidlineLeft);
            OverlayKit.Anchor(board.tally.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -80f), new Vector2(900f, 50f));
            OverlayKit.Fit(board.tally, 34f);
            board.hint = kit.Kit.Text(status, "Hint", "", 42f, look.accent, TextAlignmentOptions.MidlineRight, FontStyles.Bold, true);
            OverlayKit.Anchor(board.hint.rectTransform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(820f, 64f));
            OverlayKit.Fit(board.hint, 42f);
            board.Refresh();
            return board;
        }

        /// <summary>The manager of the game this board mirrors.</summary>
        private MemoryCardsGameManager Manager => online != null ? online.MemoryCards : null;

        /// <summary>How many cards the grid shows: those of the board the device runs, none between games.</summary>
        public int CardCount => shownRound != null ? shownRound.Cards.Count : 0;

        /// <summary>Whether a tap flips a card now: the player's turn began and they took the board, and no mistake shows.</summary>
        public bool MayTap => Manager != null && Manager.MayFlipOnline;

        /// <summary>
        /// A tap on card <paramref name="index"/> (0 at the top left, then along the rows): flips it as a click on the board
        /// would, when the player may. The tour's pilot plays a remote through here.
        /// </summary>
        public void Tap(int index)
        {
            MemoryCardsGameManager manager = Manager;
            MemoryRound round = manager != null ? manager.Round : null;
            if (round == null || index < 0 || index >= round.Cards.Count)
            {
                return;
            }
            manager.ClickCard(manager.ViewOf(round.Cards[index]));
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>The grid follows the mirror: laid out again for another board or another size of the panel, then every card's look.</summary>
        private void Refresh()
        {
            MemoryCardsGameManager manager = Manager;
            MemoryRound round = manager != null && manager.IsOnlineVersus ? manager.Round : null;
            Vector2 size = area != null ? area.rect.size : Vector2.zero;
            int columns = round != null ? Mathf.Max(1, round.Rules.Columns) : 0;
            if (!ReferenceEquals(round, shownRound) || size != shownArea || columns != shownColumns)
            {
                shownRound = round;
                shownArea = size;
                shownColumns = columns;
                Layout(round, columns, size);
            }
            RefreshStatus(manager, round);
            RefreshCells(manager, round);
        }

        private void Layout(MemoryRound round, int columns, Vector2 size)
        {
            int count = round != null ? round.Cards.Count : 0;
            int rows = RemoteBoardLayout.Rows(count, columns);
            Vector2 cell = RemoteBoardLayout.CellSize(columns, rows, size, Gap);
            while (cells.Count < count)
            {
                cells.Add(MakeCell(cells.Count));
            }
            for (int i = 0; i < cells.Count; i++)
            {
                bool used = i < count;
                cells[i].Rect.gameObject.SetActive(used);
                if (used)
                {
                    cells[i].Rect.anchoredPosition = RemoteBoardLayout.CellPosition(i, columns, rows, cell, Gap);
                    cells[i].Rect.sizeDelta = cell;
                }
            }
        }

        private Cell MakeCell(int index)
        {
            Image image = OverlayKit.NewImage(area, $"Card {index + 1}", Color.white);
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Tap(index));
            return new Cell { Rect = rect, Image = image, Button = button };
        }

        private void RefreshStatus(MemoryCardsGameManager manager, MemoryRound round)
        {
            VersusMatch versus = manager != null ? manager.Versus : null;
            int seat = manager != null ? manager.DeviceSeat : -1;
            VersusSeat mine = versus != null && seat >= 0 && seat < versus.Seats.Count ? versus.Seats[seat] : null;
            if (score != null)
            {
                score.text = mine != null ? F("Your score: {0}", N(mine.Score)) : "";
            }
            if (tally != null)
            {
                tally.text = mine != null ? (mine.Sets == 1 ? T("1 set") : F("{0} sets", mine.Sets)) + "     " + F("Mistakes   <b>{0}</b>", mine.Mistakes) : "";
            }
            if (hint != null)
            {
                hint.text = round != null && manager != null ? T(RemoteBoardLayout.HintKey(manager.IsMemorizing, manager.MayFlipOnline)) : "";
            }
        }

        private void RefreshCells(MemoryCardsGameManager manager, MemoryRound round)
        {
            if (round == null || manager == null)
            {
                return;
            }
            bool mayTap = manager.MayFlipOnline;
            Sprite back = manager.BackSprite;
            for (int i = 0; i < round.Cards.Count && i < cells.Count; i++)
            {
                MemoryCard card = round.Cards[i];
                RemoteCardLook look = RemoteBoardLayout.LookOf(card);
                Sprite face = look == RemoteCardLook.Back ? null : manager.FaceOf(card);
                Cell cell = cells[i];
                cell.Image.sprite = face != null ? face : back;
                cell.Image.color = RemoteBoardLayout.TintOf(look, card.Frozen, mayTap);
                cell.Button.interactable = mayTap && look == RemoteCardLook.Back;
            }
        }

        /// <summary>A card of the grid: its image and the button over it.</summary>
        private sealed class Cell
        {
            public RectTransform Rect;
            public Image Image;
            public Button Button;
        }
    }
}
