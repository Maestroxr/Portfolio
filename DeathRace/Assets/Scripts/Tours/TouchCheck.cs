using System.Collections;
using System.Collections.Generic;
using System.IO;
using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// Development players only: the check of racing by touch. Started with <c>-deathrace-touchcheck &lt;folder&gt;</c>, it
    /// plays as a phone does (<see cref="MobilePlatform.ForceTouch"/>) and drives a quick race through the on-screen pad
    /// (<see cref="RaceTouchControls"/>) the way fingers do: through the event system, at the spots of the screen where
    /// the stick and the buttons are drawn, after checking that nothing else catches a finger there. It checks the menu
    /// (no split screen, the auto accelerate row in the settings), that the car accelerates by itself, steers, fires,
    /// drops a mine and brakes from the pad, and that the pad goes under the pause menu and comes back. It writes
    /// <c>&lt;folder&gt;/touch.log</c> (a PASS or FAIL line per check) with screenshots, and quits with 1 when a check failed.
    /// Add <c>-deathrace-touchcheck-manual</c> to turn auto accelerate off first (the stick is the pedals then).
    /// </summary>
    public sealed class TouchCheck : MonoBehaviour
    {
        public const string Argument = "-deathrace-touchcheck";
        public const string ManualArgument = "-deathrace-touchcheck-manual";

        private string folder;
        private StreamWriter log;
        private DeathRaceManager manager;
        private int failures;
        private int shots;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void PlayByTouch()
        {
            if (!Application.isEditor && Debug.isDebugBuild && !string.IsNullOrEmpty(MobilePlatform.ArgumentValue(Argument)))
            {
                // Before the menus wake: they lay themselves out for touch.
                MobilePlatform.ForceTouch = true;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
            {
                return;
            }
            string target = MobilePlatform.ArgumentValue(Argument);
            if (string.IsNullOrEmpty(target) || FindAnyObjectByType<TouchCheck>() != null)
            {
                return;
            }
            var holder = new GameObject(nameof(TouchCheck));
            DontDestroyOnLoad(holder);
            holder.AddComponent<TouchCheck>().folder = target;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            log = new StreamWriter(Path.Combine(folder, "touch.log"), false) { AutoFlush = true };
            Application.runInBackground = true;
            Note($"touchcheck: screen {Screen.width}x{Screen.height}, safe area {Screen.safeArea}, touch {MobilePlatform.UsesTouch}");
            float waited = 0f;
            while (manager == null && waited < 30f)
            {
                manager = FindAnyObjectByType<DeathRaceManager>();
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (manager == null || manager.Director == null)
            {
                Expect(false, "a Death Race manager with a director");
                Finish();
                yield break;
            }
            yield return new WaitForSecondsRealtime(2f);
            yield return CheckMenu();
            yield return CheckRace();
            Finish();
        }

        /// <summary>The title: no split screen with one pair of thumbs; the settings offer auto accelerate.</summary>
        private IEnumerator CheckMenu()
        {
            yield return Shot("00_menu");
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button.name == "LocalPlay")
                {
                    Expect(!button.gameObject.activeSelf, "the Split Screen button is hidden on a touch screen");
                }
            }
            manager.UI?.ShowSettings();
            yield return new WaitForSecondsRealtime(1f);
            GameObject row = GameObject.Find("AutoAccelerate");
            Expect(row != null && row.activeInHierarchy, "the settings have the auto accelerate row");
            TMP_Text label = row != null ? row.GetComponentInChildren<TMP_Text>(true) : null;
            Note($"  the row reads \"{(label != null ? label.text : "")}\"");
            Toggle toggle = row != null ? row.GetComponentInChildren<Toggle>(true) : null;
            Expect(toggle != null && toggle.isOn == manager.Options.autoAccelerate, "its tick box shows the setting");
            yield return Shot("01_settings");
            if (MobilePlatform.HasArgument(ManualArgument))
            {
                manager.Options.autoAccelerate = false;
                Note("  auto accelerate turned off for this run");
            }
            manager.Back();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private IEnumerator CheckRace()
        {
            RaceSetup setup = manager.QuickRaceSetup(0);
            setup.Entrants[0].Loadout = new CarLoadout { model = CarModel.Viper, mines = 3, rockets = 2, nitro = 2 };
            manager.BeginRace(setup);
            yield return null;
            yield return null;
            RaceTouchControls touch = manager.Director.TouchControls;
            Expect(touch != null, "the race has touch controls");
            if (touch == null)
            {
                yield break;
            }
            Car car = touch.Car;
            Race race = manager.CurrentRace;
            Expect(manager.Director.Huds.Count == 1 && manager.Director.Huds[0].Touch, "the HUD is laid out for touch");
            Expect(touch.Pad.ButtonCount == 5, $"the pad has the five weapon buttons ({touch.Pad.ButtonCount})");
            bool auto = manager.Options.autoAccelerate;
            Expect(touch.Pad.AutoForward == auto, $"the pad accelerates by itself as the setting says ({auto})");
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (string action in new[] { DeathRaceControls.Fire, DeathRaceControls.Nitro, DeathRaceControls.Mine, DeathRaceControls.Rocket, DeathRaceControls.Special })
            {
                TouchButton button = touch.Pad.ButtonOf(action);
                Expect(button != null && Reaches(Centre((RectTransform)button.transform), button.gameObject), $"a finger on the {action} button reaches it");
            }
            RectTransform stickBase = (RectTransform)touch.Pad.Joystick.transform.Find("Base");
            Expect(Reaches(Centre(stickBase), touch.Pad.Joystick.gameObject), "a finger on the stick reaches it");
            yield return Shot("02_grid");

            while (race.Phase != RacePhase.Racing)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(3f);
            GameObject stick = touch.Pad.Joystick.gameObject;
            if (auto)
            {
                Expect(car.Speed > 5f, $"untouched, the car accelerates by itself ({car.Speed:0.0} m/s)");
            }
            else
            {
                Expect(car.Speed < 1f, $"untouched, the car waits for the stick ({car.Speed:0.0} m/s)");
                Vector2 rest = Centre(stickBase);
                PointerEventData push = Press(stick, 1, rest);
                Drag(stick, push, rest + new Vector2(0f, Radius(touch)));
                yield return new WaitForSecondsRealtime(3f);
                Expect(car.Speed > 5f, $"the stick pushed up accelerates ({car.Speed:0.0} m/s)");
                Lift(stick, push);
            }

            // Steer: a thumb on the stick, slid to the right.
            Vector2 start = Centre(stickBase);
            PointerEventData thumb = Press(stick, 1, start);
            Drag(stick, thumb, start + new Vector2(Radius(touch), 0f));
            yield return new WaitForSecondsRealtime(0.4f);
            Expect(car.Input.Steer > 0.5f, $"the stick slid right steers right ({car.Input.Steer:0.00})");
            if (auto)
            {
                Expect(car.Input.Throttle > 0.9f, $"and the car keeps accelerating while it steers ({car.Input.Throttle:0.00})");
            }
            yield return Shot("03_steer");
            Lift(stick, thumb);

            // Fire: a finger held on the gun.
            int rounds = car.Weapons.RoundsFired;
            GameObject fire = touch.Pad.ButtonOf(DeathRaceControls.Fire).gameObject;
            PointerEventData trigger = Press(fire, 2, Centre((RectTransform)fire.transform));
            yield return new WaitForSecondsRealtime(1f);
            Expect(car.Weapons.RoundsFired > rounds, $"holding the gun fires ({car.Weapons.RoundsFired - rounds} rounds)");
            yield return Shot("04_fire");
            Lift(fire, trigger);

            // A mine: one tap, one mine.
            int mines = car.Weapons.Mines;
            GameObject mine = touch.Pad.ButtonOf(DeathRaceControls.Mine).gameObject;
            Lift(mine, Press(mine, 3, Centre((RectTransform)mine.transform)));
            yield return new WaitForSecondsRealtime(0.5f);
            Expect(car.Weapons.Mines == mines - 1, $"a tap on the mine drops one ({mines} -> {car.Weapons.Mines})");

            // Brake: the stick pulled right back.
            start = Centre(stickBase);
            thumb = Press(stick, 1, start);
            Drag(stick, thumb, start - new Vector2(0f, Radius(touch)));
            yield return new WaitForSecondsRealtime(0.4f);
            Expect(car.Input.Throttle < -0.5f, $"the stick pulled back brakes ({car.Input.Throttle:0.00})");
            Lift(stick, thumb);

            // The pause menu covers the race: the pad goes under it and comes back.
            manager.Back();
            yield return new WaitForSecondsRealtime(0.5f);
            Expect(!touch.Pad.gameObject.activeSelf, "the pad goes while the game is paused");
            yield return Shot("05_pause");
            manager.Back();
            yield return new WaitForSecondsRealtime(0.5f);
            Expect(touch.Pad.gameObject.activeSelf, "and comes back when it resumes");
            yield return new WaitForSecondsRealtime(2f);
            yield return Shot("06_racing");
        }

        /// <summary>How far a thumb slides to push the stick fully, in screen pixels.</summary>
        private static float Radius(RaceTouchControls touch)
        {
            Canvas canvas = touch.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            return touch.Pad.Layout.StickRadius * 1.5f * scale;
        }

        private static Vector2 Centre(RectTransform rect)
        {
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        /// <summary>Whether a finger at <paramref name="screen"/> lands on <paramref name="target"/> (or a child of it) first.</summary>
        private bool Reaches(Vector2 screen, GameObject target)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, results);
            if (results.Count == 0)
            {
                Note($"  nothing at {screen}");
                return false;
            }
            GameObject hit = results[0].gameObject;
            bool reached = hit == target || hit.transform.IsChildOf(target.transform);
            if (!reached)
            {
                Note($"  at {screen} a finger lands on {hit.name} (under {(hit.transform.parent != null ? hit.transform.parent.name : "-")}), not {target.name}");
            }
            return reached;
        }

        private static PointerEventData Press(GameObject target, int pointer, Vector2 screen)
        {
            var data = new PointerEventData(EventSystem.current) { pointerId = pointer, position = screen, pressPosition = screen };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            return data;
        }

        private static void Drag(GameObject target, PointerEventData data, Vector2 screen)
        {
            data.position = screen;
            ExecuteEvents.Execute(target, data, ExecuteEvents.dragHandler);
        }

        private static void Lift(GameObject target, PointerEventData data)
        {
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
        }

        private void Expect(bool condition, string what)
        {
            if (!condition)
            {
                failures++;
            }
            Note((condition ? "PASS " : "FAIL ") + what);
        }

        private void Note(string line)
        {
            log?.WriteLine(line);
            Debug.Log("TouchCheck: " + line);
        }

        private IEnumerator Shot(string shotName)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, shotName + ".png"));
            shots++;
            yield return null;
        }

        private void Finish()
        {
            Note($"touchcheck done: {failures} failed, {shots} screenshots");
            log?.Dispose();
            log = null;
            Application.Quit(failures > 0 ? 1 : 0);
        }
    }
}
