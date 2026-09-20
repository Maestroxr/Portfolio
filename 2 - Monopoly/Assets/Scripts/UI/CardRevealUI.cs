using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// A drawn Chance or Community Chest card: it flips over in the deck's colour and waits for OK (a human player) or
    /// for a moment (a computer player) before the card's effect plays out on the board.
    /// </summary>
    public class CardRevealUI : Popup
    {
        [SerializeField] private Image face;
        [SerializeField] private Image band;
        [SerializeField] private TMP_Text deckTitle;
        [SerializeField] private TMP_Text icon;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text drawnBy;
        [SerializeField] private Button okButton;
        [SerializeField] private Image okBackground;

        private bool confirmed;

        protected override bool PopsCard => false;

        private void Awake()
        {
            if (okButton != null)
            {
                okButton.onClick.AddListener(() => confirmed = true);
            }
        }

        /// <summary>Takes the card as read (the OK button, the keyboard, the screenshot tour).</summary>
        public void Confirm()
        {
            confirmed = true;
        }

        /// <summary>Shows the card and waits: for OK when <paramref name="confirm"/>, otherwise for <paramref name="seconds"/>.</summary>
        public IEnumerator Reveal(CardDeckKind deck, string text, PlayerState player, bool confirm, float seconds)
        {
            bool chance = deck == CardDeckKind.Chance;
            Color color = chance ? MonopolyStyle.ChanceOrange : MonopolyStyle.ChestBlue;
            if (band != null)
            {
                band.color = color;
            }
            if (okBackground != null)
            {
                okBackground.color = color;
            }
            if (deckTitle != null)
            {
                deckTitle.text = chance ? "CHANCE" : "COMMUNITY CHEST";
            }
            if (icon != null)
            {
                icon.text = chance ? Icons.Question : Icons.Chest;
                icon.color = color;
            }
            if (body != null)
            {
                body.text = text;
            }
            if (drawnBy != null)
            {
                drawnBy.text = player != null ? $"Drawn by {MonopolyStyle.NamedObject(player)}" : "";
            }
            if (okButton != null)
            {
                okButton.gameObject.SetActive(confirm);
            }
            confirmed = false;
            Open();
            if (card != null)
            {
                // Flip: the card turns from its edge.
                yield return Tween.Run(0.3f, t => card.localScale = new Vector3(Mathf.Max(0.02f, Tween.OutBack(t, 1.4f)), 1f, 1f), true);
            }
            if (confirm)
            {
                float waited = 0f;
                while (!confirmed && waited < 30f)
                {
                    if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        confirmed = true;
                    }
                    yield return null;
                    waited += Time.deltaTime;
                }
            }
            else
            {
                yield return new WaitForSeconds(seconds);
            }
            Close();
        }
    }
}
