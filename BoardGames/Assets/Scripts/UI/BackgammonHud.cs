using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.BoardGames
{
    /// <summary>
    /// The backgammon buttons at the bottom of the HUD, shown only while a person here may act: Roll (with "Roll to decide
    /// who starts" over it for the opening roll), Double while the cube may be offered, Undo while a play is in progress,
    /// and for an offered cube the line "{0} doubles to {1}." with Take and Drop. It redraws on the manager's
    /// <see cref="BoardGamesManager.Changed"/> and acts through the manager's backgammon methods; Space or R rolls,
    /// Backspace undoes.
    /// </summary>
    public sealed class BackgammonHud : MonoBehaviour
    {
        [SerializeField] private BoardGamesManager manager;
        [Tooltip("Everything shown: the offer line and the row of buttons.")]
        [SerializeField] private GameObject panel;
        [Tooltip("The backing of the offer line, shown with it.")]
        [SerializeField] private GameObject offerBox;
        [SerializeField] private TMP_Text offerText;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button doubleButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button takeButton;
        [SerializeField] private Button dropButton;

        private BoardGamesManager subscribed;

        private void Awake()
        {
            Listen(rollButton, () => manager?.BackgammonRoll());
            Listen(doubleButton, () => manager?.BackgammonDouble());
            Listen(undoButton, () => manager?.BackgammonUndo());
            Listen(takeButton, () => manager?.BackgammonTake());
            Listen(dropButton, () => manager?.BackgammonDrop());
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            if (subscribed != null)
            {
                subscribed.Changed -= Refresh;
                subscribed = null;
            }
        }

        private void Update()
        {
            if (manager == null || panel == null || !panel.activeSelf)
            {
                return;
            }
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R)) && manager.BackgammonCanRoll)
            {
                manager.BackgammonRoll();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace) && manager.BackgammonCanUndo)
            {
                manager.BackgammonUndo();
            }
        }

        private void Subscribe()
        {
            if (manager == null || subscribed == manager)
            {
                return;
            }
            if (subscribed != null)
            {
                subscribed.Changed -= Refresh;
            }
            subscribed = manager;
            manager.Changed += Refresh;
        }

        /// <summary>Shows the buttons the person here may use now, in the language shown, or hides everything.</summary>
        public void Refresh()
        {
            if (panel == null)
            {
                return;
            }
            bool roll = manager != null && manager.BackgammonCanRoll;
            bool @double = manager != null && manager.BackgammonCanDouble;
            bool undo = manager != null && manager.BackgammonCanUndo;
            bool answer = manager != null && manager.BackgammonAnswering;
            string offer = manager != null ? manager.BackgammonOfferLine : null;
            bool shown = roll || @double || undo || answer;
            if (panel.activeSelf != shown)
            {
                panel.SetActive(shown);
            }
            if (!shown)
            {
                return;
            }
            Show(rollButton, roll, L.T("Roll"));
            Show(doubleButton, @double, L.T("Double"));
            Show(undoButton, undo, L.T("Undo"));
            Show(takeButton, answer, L.T("Take"));
            Show(dropButton, answer, L.T("Drop"));
            if (offerText != null)
            {
                offerText.text = offer ?? "";
                GameObject box = offerBox != null ? offerBox : offerText.gameObject;
                bool offered = !string.IsNullOrEmpty(offer);
                if (box.activeSelf != offered)
                {
                    box.SetActive(offered);
                }
            }
        }

        private static void Listen(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void Show(Button button, bool shown, string words)
        {
            if (button == null)
            {
                return;
            }
            if (button.gameObject.activeSelf != shown)
            {
                button.gameObject.SetActive(shown);
            }
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (shown && label != null && label.text != words)
            {
                label.text = words;
            }
        }
    }
}
