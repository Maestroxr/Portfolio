using System;
using System.Collections.Generic;
using System.Text;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.DeathRace.RaceWindowParts;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The end of a race: every car in the order they finished (place, colour, driver, car, time, best lap, the cars they
    /// wrecked, a mark on the wrecked), and for a career race what it came to on the right: the prize, the bounties and
    /// the cash picked up adding up to a total that counts up, the points and the move on the ladder, the state of the
    /// car, and under the places the other races of the day and the news of the ladder (rivals in better cars, tracks
    /// opened). Continue goes on (the garage); a quick race offers Race Again or the menu instead.
    /// </summary>
    public sealed class ResultsWindow : DeathRaceWindow
    {
        /// <summary>What the race was: it decides the headline, the buttons and whose rows are marked.</summary>
        private enum Kind
        {
            Career,
            Quick,
            Local,
            Online
        }

        private const float RowHeight = 86f;
        private const float CountSeconds = 1.4f;

        private RaceResult result;
        private CareerReport report;
        private Action<CareerReport> continued;
        private Action again, menu;
        private Kind kind;

        private TMP_Text total;
        private float countStart = -1f;

        protected override string CanvasName => "DeathRaceResults";

        protected override Vector2 PanelSize => new Vector2(1720f, 980f);

        protected override float MessageWidth => 700f;

        private bool IsCareer => kind == Kind.Career && report != null;

        public static ResultsWindow For(BaseGameManager manager)
        {
            return For<ResultsWindow>(manager);
        }

        /// <summary>The results of a career race and what they came to; <paramref name="onContinue"/> follows Continue.</summary>
        public void ShowCareer(RaceResult raced, CareerReport booked, Action<CareerReport> onContinue)
        {
            result = raced;
            report = booked;
            continued = onContinue;
            again = null;
            menu = null;
            kind = Kind.Career;
            countStart = -1f;
            Present();
        }

        /// <summary>The results of a quick race: Race Again runs <paramref name="onAgain"/>, Menu <paramref name="onMenu"/>.</summary>
        public void ShowQuick(RaceResult raced, Action onAgain, Action onMenu)
        {
            Show(Kind.Quick, raced, onAgain, onMenu);
        }

        /// <summary>
        /// The results of a split screen race: the best placed person in the headline, the people's rows marked in their
        /// colours, Race Again or Menu.
        /// </summary>
        public void ShowLocal(RaceResult raced, Action onAgain, Action onMenu)
        {
            Show(Kind.Local, raced, onAgain, onMenu);
        }

        /// <summary>
        /// The results of an online race (the places as the server ranked them): the headline of the people at this device,
        /// their rows marked, one button back to the room.
        /// </summary>
        public void ShowOnline(RaceResult raced, Action onBack)
        {
            Show(Kind.Online, raced, null, onBack);
        }

        private void Show(Kind shown, RaceResult raced, Action onAgain, Action onMenu)
        {
            kind = shown;
            result = raced;
            report = null;
            continued = null;
            again = onAgain;
            menu = onMenu;
            Present();
        }

        /// <summary>Escape or back: what the main button does (Continue, or the menu after a quick race).</summary>
        protected override bool HandleBack()
        {
            if (!IsOpen)
            {
                return false;
            }
            if (IsCareer)
            {
                Continue();
            }
            else
            {
                Leave(menu);
            }
            return true;
        }

        protected override void Compose()
        {
            if (result == null)
            {
                return;
            }
            OverlayLook look = kit.Look;
            RacerResult player = PlayerOf(result);
            title.text = kind == Kind.Local ? LocalHeadline() : Headline(player);
            title.color = player != null && player.Wrecked ? Warn : player != null && player.Place == 1 ? Accent : look.text;
            hint.text = Subtitle();

            float tableRight = IsCareer ? 1120f : PanelSize.x - 40f;
            BuildPlaces(look, tableRight);
            if (IsCareer)
            {
                BuildPayout(look);
                BuildDay(look, tableRight);
                ui.ButtonAt(panel, "Continue", Accent, new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(300f, 76f), Continue);
            }
            else if (kind == Kind.Online)
            {
                ui.ButtonAt(panel, "Back to the Room", Accent, new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(340f, 76f), () => Leave(menu));
            }
            else
            {
                ui.ButtonAt(panel, "Race Again", Accent, new Vector2(1f, 0f), new Vector2(-40f, 36f), new Vector2(300f, 76f), () => Leave(again));
                ui.ButtonAt(panel, "Menu", look.row, new Vector2(1f, 0f), new Vector2(-360f, 36f), new Vector2(220f, 76f), () => Leave(menu));
            }
        }

        protected override void Refresh()
        {
            if (total != null && report != null)
            {
                countStart = Time.unscaledTime + 0.35f;
                total.text = RaceFormat.Money(0);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (!IsOpen || total == null || report == null || countStart < 0f)
            {
                return;
            }
            float t = Mathf.Clamp01((Time.unscaledTime - countStart) / CountSeconds);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            total.text = RaceFormat.Money(Mathf.RoundToInt(report.Earned * eased));
            if (t >= 1f)
            {
                countStart = -1f;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // The places
        // ------------------------------------------------------------------------------------------------

        private void BuildPlaces(OverlayLook look, float right)
        {
            var racers = new List<RacerResult>(result.Racers);
            racers.Sort((a, b) => a.Place != b.Place ? a.Place.CompareTo(b.Place) : a.Seat.CompareTo(b.Seat));
            float width = right - 40f;
            RectTransform table = Area(panel, "Places");
            Span(table, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -150f - 44f - racers.Count * (RowHeight + 8f)), new Vector2(right, -150f));

            // The column heads.
            float[] columns = Columns(width);
            string[] heads = { "", "Driver", "Car", "Time", "Best lap", "Wrecks" };
            for (int i = 1; i < heads.Length; i++)
            {
                ui.WordsAt(table, L(heads[i]), 20f, look.muted, i >= 3 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 1f), new Vector2(columns[i], 0f), new Vector2(ColumnWidth(columns, i, width), 36f));
            }
            for (int i = 0; i < racers.Count; i++)
            {
                BuildPlace(table, racers[i], -44f - i * (RowHeight + 8f), columns, width, look);
            }
        }

        /// <summary>Where the columns start: place, driver, car, time, best lap, wrecks.</summary>
        private static float[] Columns(float width)
        {
            return new[] { 18f, 120f, width * 0.43f, width * 0.6f, width * 0.74f, width * 0.86f };
        }

        private static float ColumnWidth(float[] columns, int index, float width)
        {
            float end = index + 1 < columns.Length ? columns[index + 1] : width - 18f;
            return end - columns[index] - 12f;
        }

        private void BuildPlace(RectTransform table, RacerResult racer, float y, float[] columns, float width, OverlayLook look)
        {
            bool human = racer.Driver == DriverKind.Human;
            RectTransform row = ui.Box(table, "Racer", look.row);
            Span(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - RowHeight), new Vector2(0f, y));
            if (human)
            {
                // Several people at one device are told apart by their colours; one person (or the people of this device online) by the accent.
                ui.Highlight(row, kind == Kind.Local ? racer.Color : Accent, kind == Kind.Local ? 0.3f : 0.24f);
            }
            Color placeColor = racer.Place == 1 && !racer.Wrecked ? Accent : look.text;
            ui.WordsAt(row, Standings.Ordinal(racer.Place), 38f, placeColor, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f),
                new Vector2(columns[0], 0f), new Vector2(columns[1] - columns[0] - 8f, 60f), FontStyles.Bold, true);
            ui.Chip(row, racer.Color, new Vector2(0f, 0.5f), new Vector2(columns[1], 0f), 34f);
            string name = human && kind != Kind.Local ? Standings.Who(racer.Name, true) : racer.Name;
            // A wreck is struck through.
            ui.WordsAt(row, name, 28f, racer.Wrecked ? Color.Lerp(look.text, Warn, 0.4f) : look.text, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.5f), new Vector2(columns[1] + 48f, 0f), new Vector2(columns[2] - columns[1] - 60f, 50f),
                racer.Wrecked ? FontStyles.Bold | FontStyles.Strikethrough : FontStyles.Bold);
            RaceEntrant entrant = result.Setup != null ? result.Setup.EntrantAt(racer.Seat) : null;
            string carName = entrant != null ? CarName(entrant.Loadout.model) : "";
            ui.WordsAt(row, carName, 24f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 0.5f), new Vector2(columns[2], 0f),
                new Vector2(ColumnWidth(columns, 2, width), 40f));
            string time = racer.Wrecked ? L("WRECKED") : racer.Finished ? RaceFormat.Time(racer.Time) : Portfolio.DeathRace.L.Laps(racer.LapsCompleted);
            ui.WordsAt(row, time, 26f, racer.Wrecked ? Warn : look.text, TextAlignmentOptions.MidlineRight, new Vector2(0f, 0.5f),
                new Vector2(columns[3], 0f), new Vector2(ColumnWidth(columns, 3, width), 40f), racer.Wrecked ? FontStyles.Bold : FontStyles.Normal);
            ui.WordsAt(row, RaceFormat.Time(racer.BestLap), 24f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(0f, 0.5f),
                new Vector2(columns[4], 0f), new Vector2(ColumnWidth(columns, 4, width), 40f));
            Sprite skull = Theme != null ? Theme.iconSkull : null;
            float killsRight = width - 18f;
            ui.WordsAt(row, RaceFormat.Number(racer.Kills), 28f, racer.Kills > 0 ? look.text : look.muted, TextAlignmentOptions.MidlineRight,
                new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(killsRight - columns[5] - 50f, 40f), FontStyles.Bold);
            if (skull != null)
            {
                Shape(row, skull, racer.Kills > 0 ? Warn : look.muted, new Vector2(1f, 0.5f), new Vector2(-70f, 0f), new Vector2(34f, 34f));
            }
        }

        // ------------------------------------------------------------------------------------------------
        // The money and the ladder
        // ------------------------------------------------------------------------------------------------

        private void BuildPayout(OverlayLook look)
        {
            RectTransform box = ui.Box(panel, "Payout", look.row);
            Span(box, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-560f, -800f), new Vector2(-40f, -150f));
            float y = -20f;
            Line(box, L("Prize"), report.Wrecked ? L("None: wrecked") : RaceFormat.Money(report.Prize), ref y, look,
                report.Wrecked ? Warn : look.text);
            string bounty = report.Kills > 0 ? LF("{0} x {1}", report.Kills, RaceFormat.Money(CareerRules.Bounty(report.Difficulty))) : "";
            Line(box, L("Bounties"), RaceFormat.Money(report.Bounty), ref y, look, look.text, bounty);
            Line(box, L("Cash picked up"), RaceFormat.Money(report.Cash), ref y, look, look.text);
            Image rule = OverlayKit.NewImage(box, "Rule", look.muted);
            rule.raycastTarget = false;
            OverlayKit.Anchor(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y - 4f), new Vector2(472f, 2f));
            y -= 16f;
            ui.WordsAt(box, L("Total"), 30f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, y),
                new Vector2(200f, 56f), FontStyles.Bold, true);
            total = ui.WordsAt(box, RaceFormat.Money(0), 46f, Accent, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, y),
                new Vector2(300f, 56f), FontStyles.Bold, true);
            y -= 80f;

            string points = report.Points > 0 ? "+" + RaceFormat.Number(report.Points) : RaceFormat.Number(0);
            Line(box, L("Ladder points"), points, ref y, look, report.Points > 0 ? look.good : look.muted);
            string rank = RaceFormat.Rank(report.RankBefore) + "  >  " + RaceFormat.Rank(report.RankAfter);
            Color rankColor = report.RankChange > 0 ? look.good : report.RankChange < 0 ? Warn : look.muted;
            if (report.RankChange != 0)
            {
                Shape(box, report.RankChange > 0 ? RaceShapes.Up : RaceShapes.Down, rankColor, new Vector2(1f, 1f), new Vector2(-306f, y - 9f),
                    new Vector2(26f, 26f));
                ui.WordsAt(box, RaceFormat.RankChange(report.RankBefore, report.RankAfter), 22f, rankColor, TextAlignmentOptions.MidlineLeft,
                    new Vector2(1f, 1f), new Vector2(-250f, y), new Vector2(50f, 44f), FontStyles.Bold);
            }
            Line(box, L("Rank"), rank, ref y, look, rankColor);
            y -= 12f;
            string health = LF("{0}, full repair {1}", RaceFormat.Percent(report.HealthNow), RaceFormat.Money(report.RepairCost));
            Line(box, L("Car"), health, ref y, look, report.HealthNow < 0.35f ? Warn : look.text);
            if (report.Patched)
            {
                Note(box, L("The crew patched the wreck up to drive again."), ref y, look);
            }
            string used = Used();
            if (used.Length > 0)
            {
                Note(box, used, ref y, look);
            }
        }

        /// <summary>A line of the payout: a label on the left, a value on the right, a small detail between them.</summary>
        private void Line(RectTransform box, string label, string value, ref float y, OverlayLook look, Color valueColor, string detail = "")
        {
            ui.WordsAt(box, label, 24f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(24f, y), new Vector2(240f, 44f));
            if (!string.IsNullOrEmpty(detail))
            {
                ui.WordsAt(box, detail, 20f, look.muted, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-230f, y),
                    new Vector2(150f, 44f));
            }
            ui.WordsAt(box, value, 28f, valueColor, TextAlignmentOptions.MidlineRight, new Vector2(1f, 1f), new Vector2(-24f, y),
                new Vector2(220f, 44f), FontStyles.Bold);
            y -= 50f;
        }

        private void Note(RectTransform box, string text, ref float y, OverlayLook look)
        {
            TMP_Text note = ui.WordsAt(box, text, 20f, look.muted, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(24f, y),
                new Vector2(472f, 52f), FontStyles.Italic);
            note.textWrappingMode = TextWrappingModes.Normal;
            y -= 56f;
        }

        /// <summary>"Used 1 mine, 2 rockets, 1 nitro", or nothing.</summary>
        private string Used()
        {
            var parts = new List<string>();
            if (report.MinesUsed > 0)
            {
                parts.Add(report.MinesUsed == 1 ? L("1 mine") : LF("{0} mines", report.MinesUsed));
            }
            if (report.RocketsUsed > 0)
            {
                parts.Add(report.RocketsUsed == 1 ? L("1 rocket") : LF("{0} rockets", report.RocketsUsed));
            }
            if (report.NitroUsed > 0)
            {
                parts.Add(report.NitroUsed == 1 ? L("1 nitro") : LF("{0} nitros", report.NitroUsed));
            }
            return parts.Count > 0 ? LF("Used {0}.", string.Join(", ", parts)) : "";
        }

        // ------------------------------------------------------------------------------------------------
        // The rest of the day
        // ------------------------------------------------------------------------------------------------

        private void BuildDay(OverlayLook look, float right)
        {
            CareerState state = Game != null ? Game.CurrentCareer : null;
            int racers = result.Racers.Count;
            float top = -150f - 44f - racers * (RowHeight + 8f) - 24f;
            RectTransform box = Area(panel, "Day");
            Span(box, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(40f, 130f), new Vector2(right, top));
            ui.WordsAt(box, L("The other races of the day"), 24f, look.muted, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), Vector2.zero,
                new Vector2(right - 40f, 36f), FontStyles.Bold);
            float y = -44f;
            foreach (DayRace race in report.OtherRaces)
            {
                Shape(box, RaceShapes.Dot, DifficultyColor(race.Difficulty), new Vector2(0f, 1f), new Vector2(0f, y - 6f), new Vector2(22f, 22f));
                TMP_Text line = ui.WordsAt(box, DayLine(state, race), 21f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f),
                    new Vector2(32f, y), new Vector2(right - 72f, 34f));
                line.richText = true;
                y -= 40f;
            }
            foreach (string news in News())
            {
                Shape(box, ui.Star, Accent, new Vector2(0f, 1f), new Vector2(0f, y - 6f), new Vector2(22f, 22f));
                ui.WordsAt(box, news, 21f, look.text, TextAlignmentOptions.MidlineLeft, new Vector2(0f, 1f), new Vector2(32f, y),
                    new Vector2(right - 72f, 34f));
                y -= 40f;
            }
        }

        /// <summary>"Hard, Canyon Run: 1st Mad Maxine, 2nd Viktor Vex, 3rd Hammerhead, Iron Ivan wrecked".</summary>
        private string DayLine(CareerState state, DayRace race)
        {
            var line = new StringBuilder();
            line.Append("<b>").Append(ui.DifficultyName(race.Difficulty)).Append(", ").Append(TrackName(race.Track)).Append(":</b> ");
            for (int i = 0; i < race.Order.Count; i++)
            {
                if (i > 0)
                {
                    line.Append(",  ");
                }
                string name = DriverName(state, race.Order[i]);
                line.Append(race.Wrecked[i] ? LF("{0} wrecked", name) : Standings.Ordinal(i + 1) + " " + name);
            }
            return line.ToString();
        }

        private IEnumerable<string> News()
        {
            if (report.Champion)
            {
                yield return L("You beat The Warden: you are the champion!");
            }
            else if (report.ShowdownLost)
            {
                yield return L("The Warden keeps the title. Climb back to the top and try again.");
            }
            foreach (int track in report.TracksOpened)
            {
                yield return LF("New track open: {0}.", TrackName(track));
            }
            foreach (Promotion promotion in report.Promotions)
            {
                yield return LF("{0} now drives a {1}.", Portfolio.DeathRace.L.Data(promotion.Name), CarName(promotion.Car));
            }
        }

        // ------------------------------------------------------------------------------------------------

        private string Headline(RacerResult player)
        {
            if (player == null)
            {
                return L("Race Over");
            }
            if (player.Wrecked)
            {
                return L("Wrecked!");
            }
            if (report != null && report.Champion)
            {
                return L("Champion!");
            }
            return player.Place == 1 ? L("You win!") : LF("{0} place", Standings.Ordinal(player.Place));
        }

        /// <summary>The best placed person of a split screen race: "Ada wins!", "Ada finished 2nd", or everybody wrecked.</summary>
        private string LocalHeadline()
        {
            RacerResult best = PlayerOf(result);
            if (best == null || best.Wrecked)
            {
                return L("Everybody wrecked!");
            }
            return best.Place == 1 ? LF("{0} wins!", best.Name) : LF("{0} finished {1}", best.Name, Standings.Ordinal(best.Place));
        }

        private string Subtitle()
        {
            RaceSetup setup = result.Setup;
            if (setup == null)
            {
                return "";
            }
            string track = TrackName(setup.Track);
            switch (kind)
            {
                case Kind.Local:
                    return LF("Split screen  ·  {0}  ·  {1}", track, Portfolio.DeathRace.L.Laps(setup.Laps));
                case Kind.Online:
                    return LF("Online race  ·  {0}  ·  {1}", track, Portfolio.DeathRace.L.Laps(setup.Laps));
                case Kind.Quick:
                    return string.Join("  ·  ", track, LF("Quick race, {0}", ui.DifficultyName(setup.Difficulty)), Portfolio.DeathRace.L.Laps(setup.Laps));
                default:
                    string line = string.Join("  ·  ", track, ui.DifficultyName(setup.Difficulty), Portfolio.DeathRace.L.Laps(setup.Laps));
                    return report != null ? LF("Day {0}", report.Day) + "  ·  " + line : line;
            }
        }

        /// <summary>The best placed person at this device (one still running before a wrecked one), or null.</summary>
        private static RacerResult PlayerOf(RaceResult raced)
        {
            RacerResult best = null;
            foreach (RacerResult racer in raced.Racers)
            {
                if (racer.Driver != DriverKind.Human)
                {
                    continue;
                }
                if (best == null || (best.Wrecked && !racer.Wrecked) || (best.Wrecked == racer.Wrecked && racer.Place < best.Place))
                {
                    best = racer;
                }
            }
            return best;
        }

        private void Continue()
        {
            Action<CareerReport> next = continued;
            CareerReport booked = report;
            continued = null;
            Hide();
            next?.Invoke(booked);
        }

        private void Leave(Action next)
        {
            again = null;
            menu = null;
            Hide();
            next?.Invoke();
        }
    }
}
