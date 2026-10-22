using System.Globalization;
using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The remote panel of Death Race, what a phone shows while it plays as a remote control of a room's shared screen
    /// (<see cref="DeathRaceOnlineController.BuildRemotePanel"/>), and the status the screen publishes for every seat it
    /// drives from a remote (<see cref="Compose"/>): the place, the lap, the hull, the stock, the special armed, the
    /// kills and the cash, in the key=value form of the room's options. The panel is the status in three lines
    /// (<see cref="RemoteRaceStatus"/>) over the shared pad (the stick for the pedals and the wheel; the gun, the nitro,
    /// the mine and the rocket as its four buttons, by their roles in <see cref="DeathRaceControls"/>) with a fifth button
    /// for the special weapon beside them (<see cref="RemoteSpecialButton"/>), named after the special the status says
    /// is armed. A desktop that plays as a remote presses the keys of the first seat, the special's included.
    /// </summary>
    public static class RemoteRacePanel
    {
        public const string PlaceKey = "place";
        public const string CarsKey = "cars";
        public const string LapKey = "lap";
        public const string LapsKey = "laps";
        public const string HullKey = "hull";
        public const string AmmoKey = "ammo";
        public const string MinesKey = "mines";
        public const string RocketsKey = "rockets";
        public const string NitroKey = "nitro";
        public const string OilKey = "oil";
        public const string EmpKey = "emp";
        public const string MortarKey = "mortar";
        public const string SpecialKey = "special";
        public const string KillsKey = "kills";
        public const string CashKey = "cash";
        public const string StateKey = "state";

        /// <summary>The values of <see cref="StateKey"/>.</summary>
        public const string Racing = "racing";
        public const string Finished = "finished";
        public const string Wrecked = "wrecked";

        private static ControlScheme scheme;

        /// <summary>The game's control scheme, for the bits of the pad's packets.</summary>
        private static ControlScheme Scheme => scheme ??= DeathRaceControls.Scheme();

        /// <summary>The bit of <paramref name="action"/> (an id of <see cref="DeathRaceControls"/>) in a packet of the remote.</summary>
        public static uint Bit(string action)
        {
            return RemoteActionSource.Bit(Scheme.IndexOf(action));
        }

        /// <summary>The status of <paramref name="car"/> in <paramref name="race"/> as the screen publishes it for its driver.</summary>
        public static string Compose(Car car, Race race)
        {
            Racer racer = car != null ? car.Racer : null;
            CarWeapons weapons = car != null ? car.Weapons : null;
            int cars = race != null ? race.Cars.Count : 1;
            int laps = race != null ? race.Laps : 1;
            string special = "";
            if (weapons != null && weapons.NextSpecial is SpecialKind armed)
            {
                special = Specials.Of(armed).Name;
            }
            string state = car != null && car.IsWrecked ? Wrecked : car != null && car.HasFinished ? Finished : Racing;
            return Compose(
                racer != null ? Mathf.Clamp(racer.Place, 1, 99) : 1, cars,
                racer != null ? Mathf.Clamp(racer.Lap, 1, laps) : 1, laps,
                car != null && car.Health != null && !car.IsWrecked ? Mathf.RoundToInt(car.Health.Health01 * 100f) : 0,
                weapons != null ? weapons.Ammo : 0, weapons != null ? weapons.Mines : 0, weapons != null ? weapons.Rockets : 0,
                weapons != null ? weapons.NitroCharges : 0,
                weapons != null ? weapons.SpecialsLeft(SpecialKind.Oil) : 0, weapons != null ? weapons.SpecialsLeft(SpecialKind.Emp) : 0,
                weapons != null ? weapons.SpecialsLeft(SpecialKind.Mortar) : 0, special,
                racer != null ? racer.Kills : 0, racer != null ? racer.CashPicked : 0, state);
        }

        /// <summary>
        /// A status from its numbers: the whole stock (the rounds, mines, rockets, nitro charges and each special weapon,
        /// so a device that takes the car over mid-race starts with it, <see cref="HandedOverStock.FromStatus"/>) and
        /// <paramref name="special"/>, the English name of the special armed ("" for none).
        /// </summary>
        public static string Compose(int place, int cars, int lap, int laps, int hull, int ammo, int mines, int rockets, int nitro, int oil, int emp,
            int mortar, string special, int kills, int cash, string state)
        {
            return RoomOptions.Write(PlaceKey, place, CarsKey, cars, LapKey, lap, LapsKey, laps, HullKey, hull, AmmoKey, ammo, MinesKey, mines,
                RocketsKey, rockets, NitroKey, nitro, OilKey, oil, EmpKey, emp, MortarKey, mortar, SpecialKey, special ?? "", KillsKey, kills,
                CashKey, cash, StateKey, state ?? Racing);
        }

        /// <summary>
        /// The state of the pad that stands for <paramref name="input"/> (the tour's pilot drives a remote by it): the stick
        /// from the wheel and the pedals, the gun and the nitro held, the mine, the rocket and the special pressed.
        /// </summary>
        public static void PadStateOf(CarInput input, out Vector2 stick, out uint held, out uint pressed)
        {
            stick = new Vector2(Mathf.Clamp(input.Steer, -1f, 1f), Mathf.Clamp(input.Throttle, -1f, 1f));
            held = (input.Fire ? Bit(DeathRaceControls.Fire) : 0u) | (input.Nitro ? Bit(DeathRaceControls.Nitro) : 0u);
            pressed = (input.DropMine ? Bit(DeathRaceControls.Mine) : 0u) | (input.FireRocket ? Bit(DeathRaceControls.Rocket) : 0u)
                | (input.Special ? Bit(DeathRaceControls.Special) : 0u);
        }

        /// <summary>Builds the panel with <paramref name="kit"/>: the status lines, then the pad with the special's button.</summary>
        public static void Build(RemotePanelKit kit, DeathRaceOnlineController controller)
        {
            RemoteRaceStatus.Build(kit, controller);
            LocalPlayRules rules = controller != null && controller.Manager != null ? controller.Manager.LocalPlay : null;
            RemotePad pad = kit.Pad(rules != null && rules.Controls != null ? rules.Controls : Scheme, controller != null ? controller.LocalBindings : null);
            if (pad != null)
            {
                RemoteSpecialButton.Build(kit, controller, (RectTransform)pad.transform);
            }
        }
    }


    /// <summary>
    /// The three lines of the race on the remote panel, from the status the screen publishes
    /// (<see cref="RemoteRacePanel.Compose"/>): the place and the lap (and the finish or the wreck), the hull and the
    /// stock, the kills, the cash and the special armed. Waits for the first status.
    /// </summary>
    public sealed class RemoteRaceStatus : MonoBehaviour
    {
        /// <summary>The height of the section.</summary>
        public const float Height = 150f;

        private const float HeadlineSize = 40f;
        private const float LineSize = 30f;

        private DeathRaceOnlineController controller;
        private TMP_Text headline, stock, score;
        private string shown;

        /// <summary>A section of the panel with the lines, following the status as it changes.</summary>
        public static RemoteRaceStatus Build(RemotePanelKit kit, DeathRaceOnlineController controller)
        {
            RectTransform section = kit.Section("Race", Height);
            var status = section.gameObject.AddComponent<RemoteRaceStatus>();
            status.controller = controller;
            OverlayKit overlay = kit.Kit;
            status.headline = Line(overlay, section, "Headline", HeadlineSize, overlay.Look.text, FontStyles.Bold, true, 0f, 56f);
            status.stock = Line(overlay, section, "Stock", LineSize, overlay.Look.text, FontStyles.Normal, false, 60f, 42f);
            status.score = Line(overlay, section, "Score", LineSize, overlay.Look.muted, FontStyles.Normal, false, 104f, 42f);
            status.Refresh();
            return status;
        }

        /// <summary>A line across the section, <paramref name="top"/> under its top edge.</summary>
        private static TMP_Text Line(OverlayKit kit, RectTransform section, string lineName, float size, Color color, FontStyles style, bool heading,
            float top, float height)
        {
            TMP_Text text = kit.Text(section, lineName, "", size, color, TextAlignmentOptions.MidlineLeft, style, heading);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(0f, height);
            OverlayKit.Fit(text, size);
            return text;
        }

        private void Update()
        {
            if (controller != null && controller.RemoteStatus != shown)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (controller == null || headline == null)
            {
                return;
            }
            shown = controller.RemoteStatus;
            if (string.IsNullOrEmpty(shown))
            {
                headline.text = L.T("Waiting for the screen...");
                stock.text = "";
                score.text = "";
                return;
            }
            headline.text = Headline(shown);
            stock.text = L.F("Hull {0}%  ·  Ammo {1}  ·  Mines {2}  ·  Rockets {3}  ·  Nitro {4}",
                Number(RemoteRacePanel.HullKey), Number(RemoteRacePanel.AmmoKey), Number(RemoteRacePanel.MinesKey),
                Number(RemoteRacePanel.RocketsKey), Number(RemoteRacePanel.NitroKey));
            string special = RoomOptions.Value(shown, RemoteRacePanel.SpecialKey);
            score.text = L.F("Kills {0}  ·  ${1}  ·  Special: {2}", Number(RemoteRacePanel.KillsKey), Number(RemoteRacePanel.CashKey),
                string.IsNullOrEmpty(special) ? L.T("None") : L.T(special));
        }

        /// <summary>"Rank 2 of 4  ·  LAP 1/3", with the finish or the wreck after it.</summary>
        public static string Headline(string status)
        {
            int laps = Mathf.Max(1, RoomOptions.Number(status, RemoteRacePanel.LapsKey, 1));
            int lap = Mathf.Clamp(RoomOptions.Number(status, RemoteRacePanel.LapKey, 1), 1, laps);
            string words = L.F("Rank {0} of {1}", RoomOptions.Number(status, RemoteRacePanel.PlaceKey, 1), RoomOptions.Number(status, RemoteRacePanel.CarsKey, 1))
                + "  ·  " + (lap == laps && laps > 1 ? L.T("FINAL LAP") : L.F("LAP {0}/{1}", lap, laps));
            switch (RoomOptions.Value(status, RemoteRacePanel.StateKey))
            {
                case RemoteRacePanel.Finished:
                    return words + "  ·  " + L.T("Finished!");
                case RemoteRacePanel.Wrecked:
                    return words + "  ·  " + L.T("Wrecked!");
                default:
                    return words;
            }
        }

        private string Number(string key)
        {
            return RoomOptions.Number(shown, key, 0).ToString(CultureInfo.InvariantCulture);
        }
    }


    /// <summary>
    /// The fifth button of the pad, the special weapon (<see cref="DeathRaceControls.Special"/> has no role the shared pad
    /// lays out): beside the four buttons, named after the special the status says is armed. While held or tapped it
    /// feeds its bit to the controller (<see cref="DeathRaceOnlineController.FeedPad"/>), which the frame merges with
    /// its own pad.
    /// </summary>
    public sealed class RemoteSpecialButton : MonoBehaviour
    {
        private const float Size = 200f;
        private static readonly Vector2 Spot = new Vector2(-610f, 120f);

        private DeathRaceOnlineController controller;
        private TouchButton touch;
        private TMP_Text label;
        private string shownSpecial;
        private uint bit;

        /// <summary>The button in <paramref name="pad"/> (the pad's root), in the look of <paramref name="kit"/>.</summary>
        public static RemoteSpecialButton Build(RemotePanelKit kit, DeathRaceOnlineController controller, RectTransform pad)
        {
            OverlayKit overlay = kit.Kit;
            RectTransform button = overlay.Panel(pad, "Button " + DeathRaceControls.Special, overlay.Look.row, new Vector2(Size, Size), overlay.Look.buttonSprite);
            OverlayKit.Anchor(button, new Vector2(1f, 0.5f), Spot, new Vector2(Size, Size));
            var special = button.gameObject.AddComponent<RemoteSpecialButton>();
            special.controller = controller;
            special.bit = RemoteRacePanel.Bit(DeathRaceControls.Special);
            special.touch = button.gameObject.AddComponent<TouchButton>();
            special.touch.Configure(button.GetComponent<Image>(), button);
            special.label = overlay.Text(button, "Label", L.T("Special weapon"), 30f, overlay.Look.buttonText, TextAlignmentOptions.Midline, FontStyles.Bold, true);
            OverlayKit.Stretch(special.label.rectTransform);
            special.label.margin = new Vector4(10f, 0f, 10f, 0f);
            OverlayKit.Fit(special.label, 30f);
            return special;
        }

        private void Update()
        {
            if (controller == null || touch == null)
            {
                return;
            }
            bool held = touch.IsHeld;
            bool pressed = touch.ConsumePress();
            if (held || pressed)
            {
                controller.FeedPad(Vector2.zero, held ? bit : 0u, pressed ? bit : 0u);
            }
            string special = controller.RemoteStatusValue(RemoteRacePanel.SpecialKey);
            if (special != shownSpecial && label != null)
            {
                shownSpecial = special;
                label.text = string.IsNullOrEmpty(special) ? L.T("Special weapon") : L.T(special);
            }
        }

        private void OnDisable()
        {
            if (touch != null)
            {
                touch.ReleaseAll();
            }
        }
    }
}
