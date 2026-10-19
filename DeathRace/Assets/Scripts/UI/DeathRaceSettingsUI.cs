using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The settings panel: the volumes of the sounds and the music, how far the camera stands off the car, whether hits
    /// shake the screen, whether the minimap shows, and the laps of a quick race. The base class handles the
    /// default/custom switch and the save, load and back buttons; the look (theme) row is BaseGame's own.
    /// </summary>
    public sealed class DeathRaceSettingsUI : SettingsUI
    {
        [SerializeField] private Slider volume;
        [SerializeField] private Slider musicVolume;
        [Tooltip("Near, normal, far.")]
        [SerializeField] private ChoiceSelector cameraDistance;
        [SerializeField] private Toggle screenShake;
        [SerializeField] private Toggle showMinimap;
        [Tooltip("0 = the track's own.")]
        [SerializeField] private Slider quickLaps;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (!(settings is DeathRaceSettings options))
            {
                return;
            }
            SetSlider(volume, options.volume);
            SetSlider(musicVolume, options.musicVolume);
            SetToggle(screenShake, options.screenShake);
            SetToggle(showMinimap, options.showMinimap);
            SetSlider(quickLaps, options.quickLaps);
            if (cameraDistance != null)
            {
                cameraDistance.SetValueWithoutNotify(options.cameraDistance);
            }
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (!(settings is DeathRaceSettings options))
            {
                return;
            }
            options.volume = Read(volume, options.volume);
            options.musicVolume = Read(musicVolume, options.musicVolume);
            options.screenShake = Read(screenShake, options.screenShake);
            options.showMinimap = Read(showMinimap, options.showMinimap);
            options.quickLaps = Mathf.RoundToInt(Read(quickLaps, options.quickLaps));
            if (cameraDistance != null)
            {
                options.cameraDistance = cameraDistance.Value;
            }
        }

        protected override void SetInputsInteractable(bool interactable)
        {
            SetInteractable(volume, interactable);
            SetInteractable(musicVolume, interactable);
            SetInteractable(screenShake, interactable);
            SetInteractable(showMinimap, interactable);
            SetInteractable(quickLaps, interactable);
            if (cameraDistance != null)
            {
                cameraDistance.Interactable = interactable;
            }
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        private static bool Read(Toggle toggle, bool fallback)
        {
            return toggle != null ? toggle.isOn : fallback;
        }

        private static float Read(Slider slider, float fallback)
        {
            return slider != null ? slider.value : fallback;
        }
    }
}
