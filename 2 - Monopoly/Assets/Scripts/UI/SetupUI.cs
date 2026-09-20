using System;
using System.Collections.Generic;
using System.Linq;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The new game screen: the game mode (with its rules and the stars won on it) and the four seats. The last choices
    /// are remembered, so "Play" is one click away the next time.
    /// </summary>
    public class SetupUI : Popup
    {
        [SerializeField] private SeatCard[] seats = new SeatCard[4];
        [SerializeField] private ModeCard[] modes = new ModeCard[6];
        [SerializeField] private TMP_Text modeTitle;
        [SerializeField] private TMP_Text modeDescription;
        [SerializeField] private TMP_Text starRules;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        private const string SetupKey = "Monopoly.Setup";
        private const string ModeKey = "Monopoly.Mode";

        private MonopolyCampaign campaign;
        private CampaignProgress progress;
        private Func<int, Sprite> tokens;
        private Action<MatchSetup, int> start;
        private Action back;
        private MatchSetup setup;
        private int mode;

        private void Awake()
        {
            startButton.onClick.AddListener(Begin);
            backButton.onClick.AddListener(GoBack);
        }

        /// <summary>Leaves the screen without starting (Back, or the back button of a phone).</summary>
        public void GoBack()
        {
            Close();
            back?.Invoke();
        }

        public void Show(MonopolyCampaign modeList, CampaignProgress stars, Func<int, Sprite> tokenSprites, Action<MatchSetup, int> onStart, Action onBack)
        {
            campaign = modeList;
            progress = stars;
            tokens = tokenSprites;
            start = onStart;
            back = onBack;
            setup ??= LoadSetup();
            mode = Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, Mathf.Max(0, (campaign != null ? campaign.Count : 1) - 1));
            for (int i = 0; i < seats.Length; i++)
            {
                seats[i].Bind(i, setup.seats[i], tokens, ChangeToken, Validate);
            }
            for (int i = 0; i < modes.Length; i++)
            {
                MonopolyLevel level = campaign != null ? campaign.Mode(i) : null;
                modes[i].Bind(i, level, progress != null ? progress.Stars(i) : 0, SelectMode);
            }
            if (starRules != null)
            {
                starRules.text = MonopolyCampaign.StarRules;
            }
            Open();
            SelectMode(mode);
        }

        private void SelectMode(int index)
        {
            mode = index;
            MonopolyLevel level = campaign != null ? campaign.Mode(index) : null;
            for (int i = 0; i < modes.Length; i++)
            {
                modes[i].SetSelected(i == index, level != null ? level.Accent : MonopolyStyle.Red);
            }
            if (modeTitle != null)
            {
                modeTitle.text = level != null ? level.Title : "";
            }
            if (modeDescription != null)
            {
                modeDescription.text = level != null ? level.Description : "";
            }
            Validate();
        }

        /// <summary>Steps a seat to the next token no other seat uses.</summary>
        private void ChangeToken(SeatCard card, int direction)
        {
            SeatSetup seat = card.Seat;
            var taken = new HashSet<int>(setup.seats.Where(s => s != seat && s.kind != SeatKind.Off).Select(s => s.token));
            int count = MonopolyStyle.TokenCount;
            int token = seat.token;
            for (int i = 0; i < count; i++)
            {
                token = (token + direction + count) % count;
                if (!taken.Contains(token))
                {
                    break;
                }
            }
            seat.token = token;
            card.Paint();
            Validate();
        }

        private void Validate()
        {
            if (setup == null)
            {
                return;
            }
            // Seats switched back on may clash with another seat's token: move them along.
            var used = new HashSet<int>();
            for (int i = 0; i < setup.seats.Count; i++)
            {
                SeatSetup seat = setup.seats[i];
                if (seat.kind == SeatKind.Off)
                {
                    continue;
                }
                while (used.Contains(seat.token))
                {
                    seat.token = (seat.token + 1) % MonopolyStyle.TokenCount;
                }
                used.Add(seat.token);
                seats[i].Paint();
            }
            bool ok = setup.IsValid(out string message);
            startButton.interactable = ok;
            if (status != null)
            {
                int humans = setup.seats.Count(s => s.kind == SeatKind.Human);
                int bots = setup.seats.Count(s => s.kind == SeatKind.Computer);
                status.text = ok ? $"{humans} {(humans == 1 ? "player" : "players")} and {bots} computer {(bots == 1 ? "player" : "players")}" + (humans > 1 ? ", taking turns on this device" : "") : message;
                status.color = ok ? MonopolyStyle.Muted : MonopolyStyle.Red;
            }
        }

        private void Begin()
        {
            if (setup == null || !setup.IsValid(out _))
            {
                return;
            }
            SaveSetup();
            Close();
            start?.Invoke(setup.Clone(), mode);
        }

        private static MatchSetup LoadSetup()
        {
            string json = PlayerPrefs.GetString(SetupKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var saved = JsonUtility.FromJson<MatchSetup>(json);
                    if (saved != null && saved.seats != null && saved.seats.Count == 4)
                    {
                        return saved;
                    }
                }
                catch (ArgumentException)
                {
                    // An unreadable setup falls back to the default one.
                }
            }
            return MatchSetup.Default();
        }

        private void SaveSetup()
        {
            PlayerPrefs.SetString(SetupKey, JsonUtility.ToJson(setup));
            PlayerPrefs.SetInt(ModeKey, mode);
            PlayerPrefs.Save();
        }
    }
}
