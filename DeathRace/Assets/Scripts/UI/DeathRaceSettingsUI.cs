using Gamebox;
using Gamebox.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The settings panel: the volumes of the sounds and the music, how far the camera stands off the car, whether hits
    /// shake the screen, whether the minimap shows, whether the car accelerates by itself (played by touch only), and the
    /// laps of a quick race. The base class handles the default/custom switch and the save, load and back buttons; the
    /// look (theme) row is BaseGame's own. A scene built before the auto accelerate row existed gets it here, made from
    /// the minimap's row.
    /// </summary>
    public sealed class DeathRaceSettingsUI : SettingsUI
    {
        [SerializeField] private Slider volume;
        [SerializeField] private Slider musicVolume;
        [Tooltip("Near, normal, far.")]
        [SerializeField] private ChoiceSelector cameraDistance;
        [SerializeField] private Toggle screenShake;
        [SerializeField] private Toggle showMinimap;
        [Tooltip("Shown only when the game is played by touch; made from the minimap's row when the scene has none.")]
        [SerializeField] private Toggle autoAccelerate;
        [Tooltip("0 = the track's own.")]
        [SerializeField] private Slider quickLaps;

        private TMP_Text autoAccelerateLabel;

        protected override void Awake()
        {
            base.Awake();
            if (autoAccelerate == null)
            {
                autoAccelerate = MakeAutoAccelerateRow();
                // A copy shows the minimap's tick until the settings are shown again: it starts from its own.
                if (Manager?.Settings is DeathRaceSettings current)
                {
                    SetToggle(autoAccelerate, current.autoAccelerate);
                }
            }
            Transform row = autoAccelerate != null ? RowOf(autoAccelerate) : null;
            if (row != null)
            {
                // Keys and pads have a pedal of their own: the row is for touch screens.
                row.gameObject.SetActive(MobilePlatform.UsesTouch);
            }
        }

        private void OnEnable()
        {
            GameLanguages.Changed += WriteLabels;
            WriteLabels();
        }

        private void OnDisable()
        {
            GameLanguages.Changed -= WriteLabels;
        }

        /// <summary>A copy of the minimap's row (label and tick box) right under it, for the auto accelerate setting.</summary>
        private Toggle MakeAutoAccelerateRow()
        {
            if (showMinimap == null)
            {
                return null;
            }
            Transform template = RowOf(showMinimap);
            if (template == null)
            {
                return null;
            }
            GameObject row = Instantiate(template.gameObject, template.parent, false);
            row.name = "AutoAccelerate";
            row.transform.SetSiblingIndex(template.GetSiblingIndex() + 1);
            Toggle toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
            }
            Transform label = row.transform.Find("Label");
            autoAccelerateLabel = label != null ? label.GetComponent<TMP_Text>() : null;
            return toggle;
        }

        /// <summary>The row of the settings panel a control sits in (the child of the rows' layout group), or null.</summary>
        private static Transform RowOf(Component control)
        {
            Transform row = control.transform;
            for (int depth = 0; depth < 3 && row.parent != null; depth++)
            {
                if (row.parent.GetComponent<LayoutGroup>() != null)
                {
                    return row;
                }
                row = row.parent;
            }
            return null;
        }

        private void WriteLabels()
        {
            if (autoAccelerateLabel != null)
            {
                autoAccelerateLabel.text = L.T("Auto accelerate");
            }
        }

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
            SetToggle(autoAccelerate, options.autoAccelerate);
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
            options.autoAccelerate = Read(autoAccelerate, options.autoAccelerate);
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
            SetInteractable(autoAccelerate, interactable);
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
