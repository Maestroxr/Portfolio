using System.Collections.Generic;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// The game's content of the remote panel (<see cref="RemoteControlUI"/>, built by
    /// <see cref="RunnerOnlineController.BuildRemotePanel"/>): rows that say how the runner the shared screen runs for this
    /// player is doing (<see cref="RunnerRemoteStatus"/>: hearts, coins, score, distance, place and power-ups; how the run
    /// goes is the line of the frame), and the pad that moves it. The pad is the game's own touch control, swipes
    /// anywhere on the panel (<see cref="RunnerInput"/>), four buttons to tap, and the keys of a runner played alone; what
    /// was pressed this frame goes to the screen as the pressed bits of the game's control scheme
    /// (<see cref="OnlineGameController.SetPadState"/>, <see cref="SeatInput.Scheme"/>), where the screen reads it as the
    /// controls of the seat. The panel is built again when the language changes.
    /// </summary>
    internal sealed class RunnerRemotePanel : MonoBehaviour
    {
        private const float RowHeight = 52f;
        private const int Rows = 6;

        private readonly List<TouchButton> buttons = new List<TouchButton>();
        private readonly List<uint> buttonBits = new List<uint>();
        private readonly TMP_Text[] values = new TMP_Text[Rows];
        private readonly RunnerInput swipes = new RunnerInput(false);
        private RunnerOnlineController online;
        private RunnerRemoteStatus status;
        private string shown;
        private uint leftBit, rightBit, jumpBit, slideBit;

        /// <summary>Builds the rows and the pad into the panel of <paramref name="online"/> with <paramref name="kit"/>.</summary>
        public static RunnerRemotePanel Build(RemotePanelKit kit, RunnerOnlineController online)
        {
            RectTransform rows = kit.Section("Status", Rows * RowHeight);
            var panel = rows.gameObject.AddComponent<RunnerRemotePanel>();
            panel.online = online;
            panel.BuildRows(kit.Kit, rows);
            kit.Line(RunnerText.T("Swipe anywhere, or tap the buttons"), 26f);
            panel.BuildPad(kit.Kit, kit.Rest("PadArea"));
            panel.Refresh();
            return panel;
        }

        private void BuildRows(OverlayKit kit, RectTransform rows)
        {
            string[] labels =
            {
                RunnerText.T("Hearts"), RunnerText.Say("Coins"), RunnerText.T("Score"), RunnerText.T("Distance"), RunnerText.T("Place"), RunnerText.T("Power-ups")
            };
            for (int i = 0; i < Rows; i++)
            {
                TMP_Text label = kit.Text(rows, "Label", labels[i], 30f, kit.Look.muted, TextAlignmentOptions.MidlineLeft);
                OverlayKit.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -i * RowHeight), new Vector2(600f, RowHeight));
                OverlayKit.Fit(label, 30f);
                TMP_Text value = kit.Text(rows, "Value", "-", 34f, kit.Look.text, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
                OverlayKit.Anchor(value.rectTransform, new Vector2(1f, 1f), new Vector2(0f, -i * RowHeight), new Vector2(900f, RowHeight));
                OverlayKit.Fit(value, 34f);
                values[i] = value;
            }
        }

        /// <summary>Four buttons in a diamond, jump up and slide down where the thumbs rest; the whole area takes swipes.</summary>
        private void BuildPad(OverlayKit kit, RectTransform area)
        {
            ControlScheme scheme = SeatInput.Scheme();
            leftBit = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.LeftAction));
            rightBit = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.RightAction));
            jumpBit = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.JumpAction));
            slideBit = RemoteActionSource.Bit(scheme.IndexOf(SeatInput.SlideAction));
            var spots = new (string id, string label, Vector2 at, Color tint, uint bit)[]
            {
                (SeatInput.JumpAction, "Jump", new Vector2(0f, 125f), kit.Look.good, jumpBit),
                (SeatInput.SlideAction, "Slide", new Vector2(0f, -125f), kit.Look.accent, slideBit),
                (SeatInput.LeftAction, "Left", new Vector2(-250f, 0f), kit.Look.row, leftBit),
                (SeatInput.RightAction, "Right", new Vector2(250f, 0f), kit.Look.row, rightBit)
            };
            foreach ((string id, string label, Vector2 at, Color tint, uint bit) in spots)
            {
                RectTransform button = kit.Panel(area, "Button " + id, tint, new Vector2(210f, 210f), kit.Look.buttonSprite);
                OverlayKit.Anchor(button, new Vector2(0.5f, 0.5f), at, new Vector2(210f, 210f));
                var touch = button.gameObject.AddComponent<TouchButton>();
                touch.Configure(button.GetComponent<Image>(), button);
                TMP_Text words = kit.Text(button, "Label", RunnerText.T(label), 32f, kit.Look.buttonText, TextAlignmentOptions.Midline, FontStyles.Bold, true);
                OverlayKit.Stretch(words.rectTransform);
                words.margin = new Vector4(10f, 0f, 10f, 0f);
                OverlayKit.Fit(words, 32f);
                buttons.Add(touch);
                buttonBits.Add(bit);
            }
        }

        private void Update()
        {
            if (online == null)
            {
                return;
            }
            string now = online.RemoteStatus;
            if (now != shown)
            {
                shown = now;
                status = RunnerRemoteStatus.Parse(now);
                Refresh();
            }
            ReadPad();
        }

        /// <summary>The rows and the line of the frame say what the screen last said.</summary>
        private void Refresh()
        {
            if (status == null)
            {
                foreach (TMP_Text value in values)
                {
                    value.text = "-";
                }
                online.RemoteUI?.SetStatusLine(RunnerText.T("Waiting for the screen..."));
                return;
            }
            values[0].text = $"{status.Hearts} / {status.MaxHearts}";
            values[1].text = status.Coins.ToString();
            values[2].text = status.Score.ToString();
            values[3].text = status.Length > 0 ? RunnerText.F("{0} / {1:0} m", status.Distance, (float)status.Length) : RunnerText.F("{0} m", status.Distance);
            values[4].text = status.Place > 0 ? Standings.Ordinal(status.Place) : "-";
            values[5].text = PowerUpWords();
            online.RemoteUI?.SetStatusLine(StateWords());
        }

        private string PowerUpWords()
        {
            var words = new List<string>();
            for (int i = 0; i < PowerUps.Count; i++)
            {
                var type = (PowerUpType)i;
                float seconds = status.PowerUpSeconds(type);
                if (seconds > 0f)
                {
                    words.Add(RunnerText.F("{0}: {1} s", RunnerText.Say(PowerUps.Title(type)), Mathf.CeilToInt(seconds)));
                }
            }
            return words.Count > 0 ? string.Join(", ", words) : "-";
        }

        private string StateWords()
        {
            switch (status.State)
            {
                case RemoteRunState.Running: return RunnerText.T("Running");
                case RemoteRunState.Finished: return RunnerText.F("Finished - {0}", Standings.Ordinal(Mathf.Max(1, status.Place)));
                case RemoteRunState.Out: return RunnerText.T("Out of hearts");
                default: return RunnerText.T("Get ready!");
            }
        }

        /// <summary>What was pressed this frame, swipes, buttons and keys, goes to the screen; every frame, so a button let go reads as such.</summary>
        private void ReadPad()
        {
            if (!online.ShowsRemotePanel)
            {
                return;
            }
            swipes.Read();
            uint pressed = 0;
            uint held = 0;
            if (swipes.Left)
            {
                pressed |= leftBit;
            }
            if (swipes.Right)
            {
                pressed |= rightBit;
            }
            if (swipes.Jump)
            {
                pressed |= jumpBit;
            }
            if (swipes.Slide)
            {
                pressed |= slideBit;
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].ConsumePress())
                {
                    pressed |= buttonBits[i];
                }
                if (buttons[i].IsHeld)
                {
                    held |= buttonBits[i];
                }
            }
            online.SetPadState(Vector2.zero, Vector2.zero, held, pressed);
        }

        private void OnDisable()
        {
            foreach (TouchButton button in buttons)
            {
                button.ReleaseAll();
            }
        }
    }
}
