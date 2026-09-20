using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>The end of a match: the winner, the standings with net worth and properties, and the stars earned.</summary>
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

        private Action again;
        private Action menu;

        private void Awake()
        {
            againButton.onClick.AddListener(() => again?.Invoke());
            menuButton.onClick.AddListener(() => menu?.Invoke());
        }

        public void Show(MonopolyMatch match, string modeName, int earned, bool newBest, Func<int, Sprite> tokens, Action onAgain, Action onMenu)
        {
            again = onAgain;
            menu = onMenu;
            List<PlayerState> standings = match.Standings();
            PlayerState winner = match.winner >= 0 ? match.players[match.winner] : standings.FirstOrDefault();
            if (winner != null)
            {
                winnerBadge.color = MonopolyStyle.PlayerColor(winner.color);
                winnerToken.sprite = tokens?.Invoke(winner.token);
                headline.text = $"{winner.name} {MonopolyStyle.Verb(winner, "wins")}!";
                bool byWorth = match.rules.DecidedByNetWorth && match.ActiveCount > 1;
                subline.text = byWorth
                    ? $"{modeName}: richest after {(match.rules.roundLimit > 0 ? Rounds(match.round) : "the second bankruptcy")}, worth {MonopolyStyle.Money(match.NetWorth(winner.index))}"
                    : $"{modeName}: the last tycoon standing, after {Rounds(match.round)}";
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
                row.name.text = player.name + (player.bot ? $" <size=70%><color=#8C96A5>{player.level.ToString().ToUpperInvariant()}</color></size>" : "");
                int properties = match.PropertiesOf(player.index).Count();
                row.detail.text = player.bankrupt
                    ? $"Bankrupt in round {player.bankruptRound}"
                    : $"{properties} {(properties == 1 ? "property" : "properties")}  •  rent collected {MonopolyStyle.Money(player.rentCollected)}";
                row.worth.text = player.bankrupt ? "" : MonopolyStyle.Money(match.NetWorth(player.index));
            }
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < earned ? MonopolyStyle.Gold : MonopolyStyle.WithAlpha(Color.white, 0.25f);
            }
            if (starsCaption != null)
            {
                starsCaption.text = earned > 0
                    ? $"{earned} {(earned == 1 ? "star" : "stars")} earned" + (newBest ? "  •  new best!" : "")
                    : winner != null && winner.bot ? "The computer won this time. Try again!" : "Beat computer players to earn stars.";
            }
            Open();
        }

        private static string Rounds(int count)
        {
            return count == 1 ? "1 round" : $"{count} rounds";
        }
    }
}
