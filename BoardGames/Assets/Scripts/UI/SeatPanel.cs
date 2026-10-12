using Gamebox.Online;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Checkers
{
    /// <summary>
    /// The panel of one seat in the HUD: a badge in the side's colour, the player's name ("(you)" after the player at
    /// this device online), the side's name, how many pieces the seat has taken, and a mark while it is the seat's move.
    /// </summary>
    public sealed class SeatPanel : MonoBehaviour
    {
        [SerializeField] private Image badge;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text sideText;
        [SerializeField] private TMP_Text capturedText;
        [SerializeField] private GameObject turnMark;
        [SerializeField] private GameObject kingIcon;

        private int captured = -1;

        /// <summary>Whose panel it is: the name, the side's colour and words, and whether it is the player at this device.</summary>
        public void Bind(string name, Color color, string sideWords, bool you)
        {
            if (nameText != null)
            {
                nameText.text = Standings.Who(name ?? "", you);
            }
            if (badge != null)
            {
                badge.color = color;
            }
            if (sideText != null)
            {
                sideText.text = sideWords ?? "";
            }
            if (kingIcon != null)
            {
                kingIcon.SetActive(you);
            }
        }

        public void SetCaptured(int count)
        {
            captured = count;
            if (capturedText != null)
            {
                capturedText.text = L.F("{0} taken", count);
            }
        }

        public void SetTurn(bool turn)
        {
            if (turnMark != null)
            {
                turnMark.SetActive(turn);
            }
        }

        /// <summary>Writes the words again in the language shown.</summary>
        public void RefreshTexts()
        {
            if (captured >= 0)
            {
                SetCaptured(captured);
            }
        }
    }
}
