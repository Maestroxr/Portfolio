using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The property manager of the player who is deciding: every property they own, sorted by set, with buttons to
    /// build and sell houses and hotels and to mortgage or lift mortgages. Rules the buttons break are greyed out; the
    /// hint line says what the bank has left and, while a debt is open, how much is still missing.
    /// </summary>
    public class ManageUI : Popup
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private RectTransform list;
        [SerializeField] private PropertyRow template;
        [SerializeField] private TMP_Text emptyText;
        [SerializeField] private Button closeButton;

        private readonly List<PropertyRow> rows = new List<PropertyRow>();
        private MonopolyMatch match;
        private MonopolyController controller;

        public int Seat { get; private set; } = -1;

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
            if (template != null)
            {
                template.gameObject.SetActive(false);
            }
        }

        public void Show(MonopolyMatch value, int seat, MonopolyController owner)
        {
            match = value;
            Seat = seat;
            controller = owner;
            Open();
            Refresh();
        }

        public void Refresh()
        {
            if (match == null || Seat < 0 || !gameObject.activeSelf)
            {
                return;
            }
            PlayerState player = match.players[Seat];
            if (title != null)
            {
                title.text = $"{MonopolyStyle.NamedPossessive(player)} properties";
            }
            if (cashText != null)
            {
                cashText.text = MonopolyStyle.Money(player.cash);
            }
            if (hint != null)
            {
                Debt debt = match.CurrentDebt;
                string bank = match.rules.limitedBuildings ? $"The bank has {match.housesLeft} houses and {match.hotelsLeft} hotels left." : "";
                hint.text = debt != null && debt.debtor == Seat
                    ? $"<color=#E4002B>Raise {MonopolyStyle.Money(debt.amount - player.cash)} more to pay {MonopolyStyle.Money(debt.amount)}.</color> Sell buildings or mortgage."
                    : bank;
            }
            List<int> owned = match.PropertiesOf(Seat)
                .OrderBy(space => System.Array.IndexOf(MonopolyStyle.Groups, match.Space(space).group))
                .ThenBy(space => space).ToList();
            while (rows.Count < owned.Count)
            {
                PropertyRow row = Instantiate(template, list);
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
            for (int i = 0; i < rows.Count; i++)
            {
                bool used = i < owned.Count;
                rows[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }
                int space = owned[i];
                rows[i].Bind(match, Seat, space,
                    () => controller.Build(Seat, space),
                    () => controller.Sell(Seat, space),
                    () =>
                    {
                        if (match.Deed(space).mortgaged)
                        {
                            controller.Unmortgage(Seat, space);
                        }
                        else
                        {
                            controller.Mortgage(Seat, space);
                        }
                    });
            }
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(owned.Count == 0);
            }
        }
    }
}
