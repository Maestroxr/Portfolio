using System;
using System.Collections.Generic;
using System.Globalization;
using Gamebox.Online;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.MemoryCards
{
    /// <summary>
    /// The scoreboard of a versus game, at the top of the HUD: a chip per player with the name, the score and the sets
    /// found. The chip of the player whose turn it is stands out and carries the clock of the turn. Animations run on
    /// unscaled time, like the rest of the interface.
    /// </summary>
    public class VersusHud : MonoBehaviour
    {
        [Serializable]
        public class Chip
        {
            public RectTransform root;
            public Image panel;
            public Image frame;
            public TMP_Text nameText;
            public TMP_Text scoreText;
            public TMP_Text setsText;
            public Image clockTrack;
            public Image clockFill;
        }

        [SerializeField] internal Chip[] chips = new Chip[0];
        [Tooltip("The colour of each seat, for the name and the frame of its chip.")]
        [SerializeField] internal Color[] seatColors = { new Color(0.26f, 0.6f, 1f), new Color(1f, 0.42f, 0.42f), new Color(0.35f, 0.8f, 0.45f), new Color(1f, 0.7f, 0.25f) };
        [SerializeField] internal float chipWidth = 210f;
        [SerializeField] internal float chipGap = 14f;
        [SerializeField] internal Color hurryColor = new Color(1f, 0.35f, 0.3f);

        private readonly List<int> shownScores = new List<int>();
        private readonly List<float> punches = new List<float>();
        private int count;
        private int current = -1;

        public Color SeatColor(int seat)
        {
            return seatColors != null && seatColors.Length > 0 ? seatColors[Mathf.Abs(seat) % seatColors.Length] : Color.white;
        }

        /// <summary>The middle of the chip of <paramref name="seat"/> in world space, for points flying to it.</summary>
        public Vector3 ChipPosition(int seat)
        {
            return seat >= 0 && seat < chips.Length && chips[seat].root != null ? chips[seat].root.position : transform.position;
        }

        /// <summary>Lays the chips out for the players of a game. <paramref name="localSeat"/> marks "you" online; -1 on one device.</summary>
        public void Show(IReadOnlyList<VersusSeat> seats, int localSeat)
        {
            gameObject.SetActive(true);
            count = Mathf.Min(seats.Count, chips.Length);
            current = -1;
            shownScores.Clear();
            punches.Clear();
            float total = count * chipWidth + (count - 1) * chipGap;
            for (int i = 0; i < chips.Length; i++)
            {
                Chip chip = chips[i];
                if (chip.root == null)
                {
                    continue;
                }
                bool used = i < count;
                chip.root.gameObject.SetActive(used);
                shownScores.Add(-1);
                punches.Add(0f);
                if (!used)
                {
                    continue;
                }
                chip.root.anchoredPosition = new Vector2(-total * 0.5f + chipWidth * 0.5f + i * (chipWidth + chipGap), chip.root.anchoredPosition.y);
                chip.root.localScale = Vector3.one;
                if (chip.nameText != null)
                {
                    chip.nameText.text = Standings.Who(seats[i].Name, i == localSeat);
                    chip.nameText.color = SeatColor(i);
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Scores, sets, whose turn it is and the clock of the turn (<paramref name="secondsLeft"/> below 0 for no clock).</summary>
        public void Refresh(IReadOnlyList<VersusSeat> seats, int currentSeat, float turnFraction, float secondsLeft)
        {
            current = currentSeat;
            for (int i = 0; i < count && i < seats.Count; i++)
            {
                Chip chip = chips[i];
                VersusSeat seat = seats[i];
                if (shownScores[i] != seat.Score)
                {
                    if (shownScores[i] >= 0)
                    {
                        punches[i] = 1f;
                    }
                    shownScores[i] = seat.Score;
                    if (chip.scoreText != null)
                    {
                        chip.scoreText.text = seat.Score.ToString("N0", CultureInfo.InvariantCulture);
                    }
                }
                if (chip.setsText != null)
                {
                    chip.setsText.text = !seat.Playing ? "left the game" : seat.Sets == 1 ? "1 set" : $"{seat.Sets} sets";
                }
                bool turn = i == currentSeat;
                bool clock = turn && secondsLeft >= 0f;
                if (chip.frame != null)
                {
                    chip.frame.color = turn ? SeatColor(i) : new Color(1f, 1f, 1f, 0f);
                }
                if (chip.panel != null)
                {
                    chip.panel.color = new Color(1f, 1f, 1f, !seat.Playing ? 0.45f : turn ? 1f : 0.78f);
                }
                if (chip.clockTrack != null)
                {
                    chip.clockTrack.gameObject.SetActive(clock);
                }
                if (clock && chip.clockFill != null)
                {
                    chip.clockFill.fillAmount = Mathf.Clamp01(turnFraction);
                    chip.clockFill.color = secondsLeft <= 5f ? hurryColor : SeatColor(i);
                }
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < count; i++)
            {
                Chip chip = chips[i];
                if (chip.root == null)
                {
                    continue;
                }
                punches[i] = Mathf.MoveTowards(punches[i], 0f, dt * 3.2f);
                float target = (i == current ? 1.08f : 0.94f) + Mathf.Sin(punches[i] * Mathf.PI) * 0.16f;
                float scale = Mathf.Lerp(chip.root.localScale.x, target, 1f - Mathf.Exp(-14f * dt));
                chip.root.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
