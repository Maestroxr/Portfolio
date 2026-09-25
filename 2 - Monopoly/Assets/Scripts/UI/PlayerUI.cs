using System.Collections;
using Gamebox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A seat's panel: token badge in the seat colour, name, cash (counting up and down as money moves), net worth, a
    /// chip per set showing how much of it the player holds (full sets glow), and tags for computer players, jail and
    /// Get Out of Jail Free cards. The panel lights up on the seat's turn and greys out on bankruptcy.
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image frame;
        [SerializeField] private Image accent;
        [SerializeField] private Image glow;
        [SerializeField] private Image badge;
        [SerializeField] private Image tokenIcon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text cashText;
        [SerializeField] private TMP_Text worthText;
        [SerializeField] private GameObject cpuTag;
        [SerializeField] private TMP_Text cpuText;
        [SerializeField] private GameObject jailTag;
        [SerializeField] private GameObject cardTag;
        [SerializeField] private TMP_Text cardCount;
        [SerializeField] private Image[] chips = new Image[0];
        [SerializeField] private TMP_Text[] chipCounts = new TMP_Text[0];
        [SerializeField] private GameObject bankruptStamp;
        [SerializeField] private GameObject turnArrow;

        private int shownCash;
        private int targetCash;
        private Coroutine rolling;
        private bool isTurn;
        private bool isLocal;
        private int seat = -1;

        public int Seat => seat;

        public RectTransform Anchor => badge != null ? badge.rectTransform : (RectTransform)transform;

        public void Bind(PlayerState player, Sprite token)
        {
            seat = player.index;
            isLocal = false;
            gameObject.SetActive(true);
            Color color = MonopolyStyle.PlayerColor(player.color);
            if (accent != null)
            {
                accent.color = color;
            }
            if (badge != null)
            {
                badge.color = color;
            }
            if (glow != null)
            {
                glow.color = MonopolyStyle.WithAlpha(color, 0f);
            }
            if (tokenIcon != null)
            {
                tokenIcon.sprite = token;
                tokenIcon.enabled = token != null;
            }
            if (nameText != null)
            {
                nameText.text = player.name;
            }
            PaintComputerTag(player);
            shownCash = targetCash = player.cash;
            WriteCash(shownCash);
            SetTurn(false);
            if (bankruptStamp != null)
            {
                bankruptStamp.SetActive(false);
            }
            if (group != null)
            {
                group.alpha = 1f;
            }
            transform.localScale = Vector3.one;
        }

        /// <summary>Marks the panel of the player at this device, in an online match where the others sit elsewhere.</summary>
        public void MarkAsLocal()
        {
            isLocal = true;
        }

        private void PaintComputerTag(PlayerState player)
        {
            if (cpuTag != null)
            {
                cpuTag.SetActive(player.bot);
            }
            if (cpuText != null)
            {
                cpuText.text = player.bot ? player.level.ToString().ToUpperInvariant() : "";
            }
        }

        /// <summary>Updates everything but the cash (which follows the money events) from the match.</summary>
        public void Refresh(MonopolyMatch match)
        {
            if (seat < 0 || match == null || seat >= match.players.Count)
            {
                return;
            }
            PlayerState player = match.players[seat];
            // The computer takes over the seat of a player who leaves an online match.
            PaintComputerTag(player);
            if (worthText != null)
            {
                string worth = player.bankrupt ? "Out of the game" : $"Net worth {MonopolyStyle.Money(match.NetWorth(seat))}";
                worthText.text = isLocal ? $"<b>You</b>  •  {worth}" : worth;
            }
            if (jailTag != null)
            {
                jailTag.SetActive(player.inJail && !player.bankrupt);
            }
            if (cardTag != null)
            {
                cardTag.SetActive(player.jailCards.Count > 0);
            }
            if (cardCount != null)
            {
                cardCount.text = player.jailCards.Count > 1 ? $"x{player.jailCards.Count}" : "";
            }
            for (int i = 0; i < chips.Length && i < MonopolyStyle.Groups.Length; i++)
            {
                ColorGroup group = MonopolyStyle.Groups[i];
                int members = match.Board.Group(group).Length;
                int owned = match.CountOwned(seat, group);
                Color color = MonopolyStyle.GroupColor(group);
                chips[i].color = owned == 0 ? MonopolyStyle.WithAlpha(color, 0.16f) : owned == members ? color : MonopolyStyle.WithAlpha(color, 0.55f);
                if (i < chipCounts.Length && chipCounts[i] != null)
                {
                    chipCounts[i].text = owned == 0 ? "" : owned == members ? Icons.Check : owned.ToString();
                    chipCounts[i].color = owned == members ? MonopolyStyle.GroupTextColor(group) : MonopolyStyle.Ink;
                }
            }
            if (bankruptStamp != null)
            {
                bankruptStamp.SetActive(player.bankrupt);
            }
            if (this.group != null)
            {
                this.group.alpha = player.bankrupt ? 0.55f : 1f;
            }
        }

        /// <summary>Moves the cash shown towards <paramref name="cash"/>, counting when animated.</summary>
        public void SetCash(int cash, bool animate)
        {
            targetCash = cash;
            if (rolling != null)
            {
                // A count still running would end on its old target.
                StopCoroutine(rolling);
                rolling = null;
            }
            if (!animate || !isActiveAndEnabled)
            {
                shownCash = cash;
                WriteCash(cash);
                if (cashText != null)
                {
                    cashText.color = MonopolyStyle.Ink;
                    cashText.transform.localScale = Vector3.one;
                }
                return;
            }
            rolling = StartCoroutine(RollCash());
        }

        private IEnumerator RollCash()
        {
            int from = shownCash;
            int to = targetCash;
            bool gain = to > from;
            if (cashText != null)
            {
                cashText.color = gain ? MonopolyStyle.Green : MonopolyStyle.Red;
            }
            yield return Tween.Run(0.55f, t =>
            {
                shownCash = Mathf.RoundToInt(Mathf.Lerp(from, to, Tween.OutCubic(t)));
                WriteCash(shownCash);
                float punch = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
                if (cashText != null)
                {
                    cashText.transform.localScale = new Vector3(punch, punch, 1f);
                }
            }, true);
            if (cashText != null)
            {
                cashText.color = MonopolyStyle.Ink;
                cashText.transform.localScale = Vector3.one;
            }
            rolling = null;
        }

        private void OnDisable()
        {
            // A count cut short by hiding the panel would leave the old amount on it for good.
            rolling = null;
            shownCash = targetCash;
            WriteCash(targetCash);
            if (cashText != null)
            {
                cashText.color = MonopolyStyle.Ink;
                cashText.transform.localScale = Vector3.one;
            }
        }

        private void WriteCash(int cash)
        {
            if (cashText != null)
            {
                cashText.text = MonopolyStyle.Money(cash);
            }
        }

        public void SetTurn(bool value)
        {
            isTurn = value;
            if (turnArrow != null)
            {
                turnArrow.SetActive(value);
            }
            if (frame != null)
            {
                frame.color = value ? Color.white : MonopolyStyle.Panel;
            }
        }

        private void Update()
        {
            float target = isTurn ? 1.06f : 1f;
            float scale = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 0.6f);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (glow != null)
            {
                Color c = glow.color;
                float alpha = isTurn ? 0.55f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f) : 0f;
                c.a = Mathf.MoveTowards(c.a, alpha, Time.unscaledDeltaTime * 3f);
                glow.color = c;
            }
        }
    }
}
