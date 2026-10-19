using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using static Portfolio.DeathRace.RaceWindowParts;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The end of the climb: The Warden is beaten and the player is champion. The career's record (days, races, wins,
    /// cars wrecked, money earned, the car that did it) under a big star; the career goes on from here if the player
    /// wants (Keep Racing opens the garage), or back to the menu.
    /// </summary>
    public sealed class ChampionWindow : DeathRaceWindow
    {
        private TMP_Text who;
        private TMP_Text[] values;

        private static readonly string[] Labels = { "Days", "Races", "Wins", "Cars wrecked", "Money earned", "Car" };

        protected override string CanvasName => "DeathRaceChampion";

        protected override Vector2 PanelSize => new Vector2(1180f, 820f);

        protected override float MessageWidth => 400f;

        public static ChampionWindow For(BaseGameManager manager)
        {
            return For<ChampionWindow>(manager);
        }

        public void Show()
        {
            Present();
        }

        protected override void Compose()
        {
            OverlayLook look = kit.Look;
            title.text = L("Champion!");
            title.color = Accent;
            hint.text = L("The Warden is beaten. The wasteland has a new name to fear.");

            Shape(panel, ui.Icon("Trophy", RaceShapes.Star), Accent, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(170f, 170f));
            who = ui.WordsAt(panel, "", 52f, look.text, TextAlignmentOptions.Midline, new Vector2(0.5f, 1f), new Vector2(0f, -330f),
                new Vector2(1000f, 70f), FontStyles.Bold, true);

            values = new TMP_Text[Labels.Length];
            for (int i = 0; i < Labels.Length; i++)
            {
                float x = -360f + (i % 3) * 360f;
                float y = -440f - (i / 3) * 110f;
                RectTransform cell = ui.Box(panel, Labels[i], look.row);
                OverlayKit.Anchor(cell, new Vector2(0.5f, 1f), new Vector2(x, y), new Vector2(330f, 96f));
                ui.WordsAt(cell, L(Labels[i]), 22f, look.muted, TextAlignmentOptions.Midline, new Vector2(0.5f, 1f), new Vector2(0f, -8f),
                    new Vector2(310f, 30f));
                values[i] = ui.WordsAt(cell, "", 34f, look.text, TextAlignmentOptions.Midline, new Vector2(0.5f, 0f), new Vector2(0f, 10f),
                    new Vector2(310f, 46f), FontStyles.Bold, true);
            }

            ui.ButtonAt(panel, "Keep Racing", Accent, new Vector2(1f, 0f), new Vector2(-48f, 40f), new Vector2(300f, 76f), KeepRacing);
            ui.ButtonAt(panel, "Main Menu", look.row, new Vector2(1f, 0f), new Vector2(-368f, 40f), new Vector2(260f, 76f), Hide);
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
        }

        private void KeepRacing()
        {
            Hide();
            Game?.OpenGarage(GarageWindow.RacePage);
        }
    }
}
