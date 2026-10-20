using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Portfolio.DeathRace.RaceWindowParts;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The end of a league's climb: its champion is beaten and the player is champion of the league. The career's record
    /// (days, races, wins, cars wrecked, money earned, the car that did it) under a big trophy, with the beaten champion's
    /// portrait (when the look has one) beside it. Before the last league the next one is open now: Move Up keeps the
    /// finished career as a save and starts the same driver, in the same colour, at the bottom of the next league's ladder
    /// (<see cref="DeathRaceManager.MoveUpLeague"/>). The career also goes on from here if the player wants (Keep Racing
    /// opens the garage), or back to the menu.
    /// </summary>
    public sealed class ChampionWindow : DeathRaceWindow
    {
        private TMP_Text who;
        private TMP_Text[] values;
        private TMP_Text nextLine;
        private Button moveUp;

        private static readonly string[] Labels = { "Days", "Races", "Wins", "Cars wrecked", "Money earned", "Car" };

        protected override string CanvasName => "DeathRaceChampion";

        protected override Vector2 PanelSize => new Vector2(1180f, 900f);

        protected override float MessageWidth => 400f;

        public static ChampionWindow For(BaseGameManager manager)
        {
            return For<ChampionWindow>(manager);
        }

        public void Show()
        {
            Present();
        }

        /// <summary>Whether Move Up is offered (the career check looks).</summary>
        internal bool OffersMoveUp => moveUp != null && moveUp.gameObject.activeSelf;

        protected override void Compose()
        {
            OverlayLook look = kit.Look;
            CareerState state = Game != null ? Game.CurrentCareer : null;
            LeagueSpec league = state != null ? state.League : Leagues.Wasteland;
            title.text = L("Champion!");
            title.color = Accent;
            hint.text = LF("{0} is beaten. You are the champion of the {1}.", Portfolio.DeathRace.L.Data(league.Champion.Name), L(league.Name));

            Shape(panel, ui.Icon("Trophy", RaceShapes.Star), Accent, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(170f, 170f));
            Sprite portrait = Portrait(league);
            if (portrait != null)
            {
                // The beaten champion looks on from beside the trophy, darkened.
                Image face = Picture(panel, portrait, new Vector2(0.5f, 1f), new Vector2(300f, -150f), new Vector2(170f, 170f));
                face.color = new Color(0.55f, 0.55f, 0.55f, 1f);
            }
            who = ui.WordsAt(panel, "", 52f, look.text, TextAlignmentOptions.Midline, new Vector2(0.5f, 1f), new Vector2(0f, -330f),
                new Vector2(1000f, 70f), FontStyles.Bold, true);

            values = new TMP_Text[Labels.Length];
            for (int i = 0; i < Labels.Length; i++)
            {
                float x = -360f + (i % 3) * 360f;
                float y = -420f - (i / 3) * 110f;
                RectTransform cell = ui.Box(panel, Labels[i], look.row);
                OverlayKit.Anchor(cell, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(330f, 96f));
                ui.WordsAt(cell, L(Labels[i]), 22f, look.muted, TextAlignmentOptions.Midline, new Vector2(0.5f, 1f), new Vector2(0f, -8f),
                    new Vector2(310f, 30f));
                values[i] = ui.WordsAt(cell, "", 34f, look.text, TextAlignmentOptions.Midline, new Vector2(0.5f, 0f), new Vector2(0f, 10f),
                    new Vector2(310f, 46f), FontStyles.Bold, true);
            }

            nextLine = ui.WordsAt(panel, "", 24f, look.text, TextAlignmentOptions.Midline, new Vector2(0.5f, 1f), new Vector2(0f, -656f),
                new Vector2(1060f, 70f), FontStyles.Italic);
            nextLine.textWrappingMode = TextWrappingModes.Normal;
            LeagueSpec next = league.Next;
            moveUp = ui.ButtonAt(panel, next != null ? LF("Move Up to the {0}", L(next.Name)) : L("Move Up"), Accent, new Vector2(1f, 0f),
                new Vector2(-48f, 40f), new Vector2(440f, 76f), MoveUp, true);
            ui.ButtonAt(panel, "Keep Racing", next != null ? look.row : Accent, new Vector2(1f, 0f), new Vector2(next != null ? -508f : -48f, 40f),
                new Vector2(280f, 76f), KeepRacing);
            ui.ButtonAt(panel, "Main Menu", look.row, new Vector2(1f, 0f), new Vector2(next != null ? -808f : -348f, 40f), new Vector2(240f, 76f), Hide);
        }

        /// <summary>The champion's portrait in the look shown (after the twenty drivers' colours), or null.</summary>
        private Sprite Portrait(LeagueSpec league)
        {
            Sprite[] portraits = Theme != null ? Theme.driverPortraits : null;
            int index = league.ChampionPortrait;
            return portraits != null && index >= 0 && index < portraits.Length ? portraits[index] : null;
        }

        protected override void Refresh()
        {
            CareerState state = Game != null ? Game.CurrentCareer : null;
            if (state == null)
            {
                return;
            }
            who.text = state.driverName;
            who.color = Color.Lerp(ui.DriverColor(state.colorIndex), kit.Look.text, 0.2f);
            values[0].text = RaceFormat.Number(state.championDay > 0 ? state.championDay : state.day);
            values[1].text = RaceFormat.Number(state.races);
            values[2].text = RaceFormat.Number(state.wins);
            values[3].text = RaceFormat.Number(state.kills);
            values[4].text = RaceFormat.Money(state.earned);
            values[5].text = CarName(state.car);
            LeagueSpec next = Game.NextLeague;
            moveUp.gameObject.SetActive(next != null);
            nextLine.text = next != null
                ? LF("The {0} is open: {1} and the bottom of a new ladder, with {2} waiting at the top.", L(next.Name), CarName(next.StartCar),
                    Portfolio.DeathRace.L.Data(next.Champion.Name))
                : L("There is no league left to win. The wasteland is yours.");
        }

        private void MoveUp()
        {
            if (Game == null)
            {
                return;
            }
            Hide();
            if (!Game.MoveUpLeague())
            {
                Present();
            }
        }

        private void KeepRacing()
        {
            Hide();
            Game?.OpenGarage(GarageWindow.RacePage);
        }
    }
}
