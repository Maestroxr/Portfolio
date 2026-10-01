using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The end of a match: the winner, the standings with net worth and properties, and the stars earned. After an online
    /// match, which earns no stars, the buttons lead back to the room and out of it.
    /// </summary>
    public class ResultsUI : Popup
    {
        [Serializable]
        public class StandingRow
        {
            public GameObject root;
            public TMP_Text place;
            public Image badge;
            public Image token;
            public TMP_Text name;
            public TMP_Text detail;
            public TMP_Text worth;
        }

        [SerializeField] private Image winnerBadge;
        [SerializeField] private Image winnerToken;
        [SerializeField] private TMP_Text headline;
        [SerializeField] private TMP_Text subline;
        [SerializeField] private StandingRow[] rows = new StandingRow[4];
        [SerializeField] private TMP_Text[] stars = new TMP_Text[3];
        [SerializeField] private TMP_Text starsCaption;
        [SerializeField] private Button againButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private TMP_Text againLabel;
        [SerializeField] private TMP_Text menuLabel;

        private Action again;
        private Action menu;

        private void Awake()
        {
            againButton.onClick.AddListener(() => again?.Invoke());
            menuButton.onClick.AddListener(() => menu?.Invoke());
        }

        public void Show(MonopolyMatch match, string modeName, int earned, bool newBest, Func<int, Sprite> tokens, Action onAgain, Action onMenu,
            bool online = false)
        {
            again = onAgain;
            menu = onMenu;
            if (againLabel != null)
            {
                againLabel.text = online ? L.T("BACK TO ROOM") : L.T("PLAY AGAIN");
            }
            if (menuLabel != null)
            {
                menuLabel.text = online ? L.T("LEAVE ROOM") : L.T("MAIN MENU");
            }
            List<PlayerState> standings = match.Standings();
            PlayerState winner = match.winner >= 0 ? match.players[match.winner] : standings.FirstOrDefault();
            if (winner != null)
            {
                winnerBadge.color = MonopolyStyle.PlayerColor(winner.color);
                winnerToken.sprite = tokens?.Invoke(winner.token);
                winnerToken.color = MonopolyStyle.TextOn(winnerBadge.color);
                headline.text = MonopolyStyle.Say(winner, "{0} wins!", $"{winner.name} {MonopolyStyle.Verb(winner, "wins")}!", MonopolyStyle.NameOf(winner));
                bool byWorth = match.rules.DecidedByNetWorth && match.ActiveCount > 1;
                string worth = MonopolyStyle.Money(match.NetWorth(winner.index));
                subline.text = byWorth
                    ? match.rules.roundLimit > 0 ? L.F("{0}: richest after {1}, worth {2}", modeName, Rounds(match.round), worth)
                        : L.F("{0}: richest after the second bankruptcy, worth {1}", modeName, worth)
                    : L.F("{0}: the last tycoon standing, after {1}", modeName, Rounds(match.round));
            }
            for (int i = 0; i < rows.Length; i++)
            {
                StandingRow row = rows[i];
                bool used = i < standings.Count;
                row.root.SetActive(used);
                if (!used)
                {
                    continue;
                }
                PlayerState player = standings[i];
                row.place.text = (i + 1).ToString();
                row.badge.color = MonopolyStyle.PlayerColor(player.color);
                row.token.sprite = tokens?.Invoke(player.token);
                row.token.color = MonopolyStyle.TextOn(row.badge.color);
                row.name.text = MonopolyStyle.NameOf(player) + (player.bot ? $" <size=70%><color={MonopolyStyle.ColorTag(MonopolyStyle.Muted)}>{MonopolyStyle.LevelName(player.level)}</color></size>" : "");
                int properties = match.PropertiesOf(player.index).Count();
                row.detail.text = player.bankrupt
                    ? L.F("Bankrupt in round {0}", player.bankruptRound)
                    : properties == 1 ? L.F("1 property  •  rent collected {0}", MonopolyStyle.Money(player.rentCollected))
                    : L.F("{0} properties  •  rent collected {1}", properties, MonopolyStyle.Money(player.rentCollected));
                row.worth.text = player.bankrupt ? "" : MonopolyStyle.Money(match.NetWorth(player.index));
            }
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].gameObject.SetActive(!online);
                stars[i].color = i < earned ? MonopolyStyle.Gold : MonopolyStyle.WithAlpha(Color.white, 0.25f);
            }
            if (starsCaption != null)
            {
                starsCaption.text = online ? L.T("Online match  •  back in the room the host starts the next game")
                    : earned > 0 ? (earned == 1 ? L.T("1 star earned") : L.F("{0} stars earned", earned)) + (newBest ? "  •  " + L.T("new best!") : "")
                    : winner != null && winner.bot ? L.T("The computer won this time. Try again!") : L.T("Beat computer players to earn stars.");
            }
            Open();
        }

        private static string Rounds(int count)
        {
            return count == 1 ? L.T("1 round") : L.F("{0} rounds", count);
        }
    }
}
