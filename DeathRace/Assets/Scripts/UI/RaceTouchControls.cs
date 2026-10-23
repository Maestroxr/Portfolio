using Gamebox;
using Gamebox.Online;
using Gamebox.UI;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The race's on-screen controls on a phone or a tablet (<see cref="MobilePlatform.UsesTouch"/>): BaseGame's
    /// <see cref="TouchPad"/> over the HUD of the one person racing at this device, built from the car's controls
    /// (<see cref="DeathRaceControls"/>). A thumb anywhere on the lower left takes the stick: it steers, and with
    /// <see cref="DeathRaceSettings.autoAccelerate"/> the car accelerates by itself until the stick is pulled back to
    /// brake and reverse (without it the stick is the pedals too). The gun sits under the right thumb with the nitro,
    /// the mine, the rocket and the special weapon around it, each with its icon and what is left of it, the special
    /// weapon's the one the button fires next; a weapon with nothing left is faded. The car's driver reads the pad beside
    /// the keys (<see cref="HumanDriver.Touch"/>). The pad shows while the race is played: not over the pause menu or the
    /// results, and not once the car has finished or is wrecked.
    /// </summary>
    public sealed class RaceTouchControls : MonoBehaviour
    {
        /// <summary>The whole numbers the buttons show, written once.</summary>
        private static readonly string[] Numbers = new string[1000];

        private Car car;
        private DeathRaceManager manager;
        private DeathRaceTheme theme;
        private OverlayKit kit;
        private TouchPad pad;
        private ControlScheme scheme;
        private PlayerControls controls;
        private HumanDriver driver;
        private SpecialKind? shownSpecial;
        private bool specialShown;
        private readonly int[] shownCounts = { -1, -1, -1, -1, -1 };

        /// <summary>The pad.</summary>
        public TouchPad Pad => pad;

        /// <summary>The car the pad drives.</summary>
        public Car Car => car;

        /// <summary>The controls the car's driver reads the pad through.</summary>
        public PlayerControls Controls => controls;

        /// <summary>
        /// Whether <paramref name="car"/>, the only person racing at this device, is driven by touch here: not a seat that a
        /// phone plays as a remote control of this (shared) screen, which its own pad drives.
        /// </summary>
        public static bool WantedFor(Car car)
        {
            return MobilePlatform.UsesTouch && car != null && car.Driver is HumanDriver
                && !(car.Entrant != null && car.Entrant.Controls is PlayerControls controls && controls.Source is RemoteActionSource);
        }

        /// <summary>The pad for <paramref name="car"/> over <paramref name="hudRoot"/> (the director's HUD canvas).</summary>
        public static RaceTouchControls Create(RectTransform hudRoot, Car car, DeathRaceManager manager, DeathRaceTheme theme, DeathRaceSettings settings)
        {
            RectTransform root = OverlayKit.Rect(hudRoot, "Touch Controls");
            OverlayKit.Stretch(root);
            var touch = root.gameObject.AddComponent<RaceTouchControls>();
            touch.car = car;
            touch.manager = manager;
            touch.scheme = DeathRaceControls.Scheme();
            touch.Build(theme);
            touch.ApplySettings(settings);
            touch.driver = car.Driver as HumanDriver;
            if (touch.driver != null)
            {
                touch.controls = new PlayerControls(touch.scheme, touch.pad, 0);
                touch.driver.Touch = touch.controls;
            }
            return touch;
        }

        /// <summary>The look or the language changed: the pad is built again in it.</summary>
        public void Restyle(DeathRaceTheme look)
        {
            Build(look);
            if (controls != null)
            {
                controls = new PlayerControls(scheme, pad, 0);
                if (driver != null)
                {
                    driver.Touch = controls;
                }
            }
        }

        /// <summary>The settings changed: the accelerator.</summary>
        public void ApplySettings(DeathRaceSettings settings)
        {
            if (pad != null)
            {
                pad.AutoForward = settings == null || settings.autoAccelerate;
            }
        }

        private void Build(DeathRaceTheme look)
        {
            theme = look;
            bool autoForward = pad != null && pad.AutoForward;
            if (pad != null)
            {
                // Out of the way at once: the new pad takes its place in this frame.
                pad.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(pad.gameObject);
                }
                else
                {
                    DestroyImmediate(pad.gameObject);
                }
            }
            kit?.Dispose();
            kit = new OverlayKit(OverlayLook.Of(manager), L.T);
            pad = TouchPad.Build(kit, (RectTransform)transform, scheme, TouchPadLayout.Overlay(), L.T);
            pad.AutoForward = autoForward;
            pad.SetIcon(DeathRaceControls.Nitro, theme != null ? theme.iconNitro : null);
            pad.SetIcon(DeathRaceControls.Mine, theme != null ? theme.iconMine : null);
            pad.SetIcon(DeathRaceControls.Rocket, theme != null ? theme.iconRocket : null);
            specialShown = false;
            for (int i = 0; i < shownCounts.Length; i++)
            {
                shownCounts[i] = -1;
            }
            ShowGun();
            Refresh();
        }

        /// <summary>The gun fitted: its icon (the rounds' when the look has none), or its name without any icon.</summary>
        private void ShowGun()
        {
            GunSpec gun = car != null && car.Weapons != null ? car.Weapons.GunSpec : Guns.Of(GunKind.MachineGun);
            Sprite icon = Named(gun.Icon);
            if (icon == null && theme != null)
            {
                icon = theme.iconAmmo;
            }
            pad.SetIcon(DeathRaceControls.Fire, icon);
            if (icon == null)
            {
                pad.SetLabel(DeathRaceControls.Fire, Upper(L.Data(gun.Name)));
            }
        }

        /// <summary>Capitals, as the HUD writes its words (Hebrew has none and stays as it is).</summary>
        private static string Upper(string words)
        {
            return string.IsNullOrEmpty(words) ? "" : words.ToUpperInvariant();
        }

        private Sprite Named(string key)
        {
            return theme != null && !string.IsNullOrEmpty(key) ? theme.SpriteOf(key) : null;
        }

        private void Update()
        {
            if (pad == null)
            {
                return;
            }
            bool shown = Playing;
            if (pad.gameObject.activeSelf != shown)
            {
                pad.gameObject.SetActive(shown);
            }
            if (shown)
            {
                Refresh();
            }
        }

        /// <summary>Whether the race is being played with the pad: the car races on it and no menu is over the race.</summary>
        private bool Playing
        {
            get
            {
                if (car == null || car.IsWrecked || car.HasFinished || driver == null || car.Driver != driver || driver.Touch != controls)
                {
                    return false;
                }
                Race race = car.Race;
                if (race == null || race.Phase == RacePhase.Over)
                {
                    return false;
                }
                return manager == null || manager.State == null || manager.State.BaseState == BaseGameState.Running;
            }
        }

        /// <summary>What is left of every weapon on its button, the special armed, the empty ones faded.</summary>
        private void Refresh()
        {
            CarWeapons weapons = car != null ? car.Weapons : null;
            if (weapons == null)
            {
                return;
            }
            Count(0, DeathRaceControls.Fire, weapons.Ammo);
            Count(1, DeathRaceControls.Nitro, weapons.NitroCharges);
            Count(2, DeathRaceControls.Mine, weapons.Mines);
            Count(3, DeathRaceControls.Rocket, weapons.Rockets);
            SpecialKind? next = weapons.NextSpecial;
            if (!specialShown || next != shownSpecial)
            {
                // The special the button fires next changed (one ran out): its icon, or its name without one.
                specialShown = true;
                shownSpecial = next;
                SpecialSpec spec = Specials.Of(next ?? weapons.ArmedSpecial);
                Sprite icon = Named(spec.Icon);
                pad.SetIcon(DeathRaceControls.Special, icon);
                if (icon == null)
                {
                    pad.SetLabel(DeathRaceControls.Special, Upper(L.Data(spec.Name)));
                }
                shownCounts[4] = -1;
            }
            Count(4, DeathRaceControls.Special, next.HasValue ? weapons.SpecialsLeft(next.Value) : 0);
        }

        /// <summary>Writes the count of a weapon under its icon (a button without an icon keeps its words) and fades it when empty.</summary>
        private void Count(int slot, string action, int left)
        {
            if (shownCounts[slot] == left)
            {
                return;
            }
            shownCounts[slot] = left;
            pad.SetAvailable(action, left > 0);
            if (pad.HasIcon(action))
            {
                pad.SetLabel(action, Number(left));
            }
        }

        private static string Number(int value)
        {
            if (value < 0 || value >= Numbers.Length)
            {
                return value.ToString();
            }
            return Numbers[value] ??= value.ToString();
        }

        private void OnDestroy()
        {
            if (driver != null && driver.Touch == controls)
            {
                driver.Touch = null;
            }
            kit?.Dispose();
        }
    }
}
