using Gamebox.Online;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The panel of one seat in the HUD: a badge in the side's colour, the player's name ("(you)" after the player at
    /// this device online), the side's name (Light or Dark in draughts, White or Black in chess and backgammon), the
    /// game's line about the seat (pieces taken, material, pips and checkers borne off) and a mark while it is the
    /// seat's turn. The words come from the manager (<see cref="BoardGamesManager.SideName"/>,
    /// <see cref="BoardGamesManager.SeatLine"/>), which writes them in the language shown.
    /// </summary>
    public sealed class SeatPanel : MonoBehaviour
    {
        [SerializeField] private Image badge;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text sideText;
        [SerializeField] private TMP_Text lineText;
        [SerializeField] private GameObject turnMark;
        [SerializeField] private GameObject kingIcon;

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

        /// <summary>The game's line about the seat (empty hides nothing, it just shows no words).</summary>
        public void SetLine(string words)
        {
            if (lineText != null)
            {
                lineText.text = words ?? "";
            }
        }

        public void SetTurn(bool turn)
        {
            if (turnMark != null)
            {
                turnMark.SetActive(turn);
            }
        }
    }
}
