using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The scoreboard of a race, under the coins and the score of the HUD: the place of the local runner, and a row per
    /// runner in the order of the places, with the name in the colour of the runner, the score, the distance and the coins.
    /// </summary>
    public class RaceHud : MonoBehaviour
    {
        [Serializable]
        internal class Row
        {
            public GameObject root;
            [Tooltip("Shown behind the row of the local runner.")]
            public Image highlight;
            public Image chip;
            public TMP_Text place;
            public TMP_Text runnerName;
            public TMP_Text score;
            public TMP_Text detail;

            [NonSerialized] public int Seat = -1;
            [NonSerialized] public int Place = -1;
            [NonSerialized] public long Score = -1;
            [NonSerialized] public int Meters = -1;
            [NonSerialized] public int Coins = -1;
            [NonSerialized] public bool Done;
        }

        [SerializeField] internal Row[] rows = new Row[0];
        [Tooltip("The place of the local runner, large.")]
        [SerializeField] internal TMP_Text placeText;
        [SerializeField] internal RectTransform panel;
        [SerializeField] internal float headerHeight = 62f;
        [SerializeField] internal float rowHeight = 58f;

        private int shownPlace = -1;
        private int shownRows = -1;

        /// <summary>Shows the scoreboard for a race of <paramref name="runners"/>.</summary>
        public void Show(int runners)
        {
            gameObject.SetActive(true);
            shownPlace = -1;
            ShowRows(Mathf.Min(runners, rows.Length));
            foreach (Row row in rows)
            {
                row.Seat = row.Place = row.Meters = row.Coins = -1;
                row.Score = -1;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>The runners in the order of their places; <paramref name="colors"/> are theirs by start slot.</summary>
        public void Refresh(IReadOnlyList<Racer> ranking, Color[] colors)
        {
            int used = Mathf.Min(ranking.Count, rows.Length);
            if (used != shownRows)
            {
                // A runner left the race.
                ShowRows(used);
            }
            for (int i = 0; i < used; i++)
            {
                Refresh(rows[i], ranking[i], colors);
            }
        }

        private void Refresh(Row row, Racer racer, Color[] colors)
        {
            if (row.Seat != racer.Seat)
            {
                row.Seat = racer.Seat;
                Color color = colors != null && colors.Length > 0 ? colors[Mathf.Abs(racer.Slot) % colors.Length] : Color.white;
                if (row.chip != null)
                {
                    row.chip.color = color;
                }
                if (row.runnerName != null)
                {
                    row.runnerName.text = racer.Local ? $"{racer.Name} <size=75%>(you)</size>" : racer.Name;
                    row.runnerName.color = Color.Lerp(color, Color.white, 0.45f);
                }
                if (row.highlight != null)
                {
                    row.highlight.enabled = racer.Local;
                }
            }
            if (row.Place != racer.Place)
            {
                row.Place = racer.Place;
                SetText(row.place, racer.Place.ToString());
            }
            if (row.Score != racer.Score)
            {
                row.Score = racer.Score;
                SetText(row.score, racer.Score.ToString());
            }
            int meters = Mathf.FloorToInt(racer.Distance);
            if (row.Meters != meters || row.Coins != racer.Coins || row.Done != racer.Done)
            {
                row.Meters = meters;
                row.Coins = racer.Coins;
                row.Done = racer.Done;
                SetText(row.detail, racer.Done ? $"{meters} m   {racer.Coins} coins   done" : $"{meters} m   {racer.Coins} coins");
            }
            if (racer.Local && shownPlace != racer.Place)
            {
                shownPlace = racer.Place;
                SetText(placeText, RaceStandings.Ordinal(Mathf.Max(1, racer.Place)));
            }
        }

        private void ShowRows(int count)
        {
            shownRows = count;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].root != null)
                {
                    rows[i].root.SetActive(i < count);
                }
            }
            if (panel != null)
            {
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, headerHeight + Mathf.Max(1, count) * rowHeight + 10f);
            }
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
