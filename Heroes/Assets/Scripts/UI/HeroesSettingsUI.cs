using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The settings panel of the game: where battles are fought, how big a skirmish map is, how much it holds, how hard
    /// the computer players are, how fast heroes walk and battles play, and the sound. The base class handles the
    /// default and custom switch and the save, load and back buttons; while the defaults are in use the rows show them
    /// and cannot be changed.
    /// </summary>
    public class HeroesSettingsUI : SettingsUI
    {
        [SerializeField] private ChoiceSelector battles;
        [SerializeField] private Slider mapSize;
        [SerializeField] private Slider treasure;
        [SerializeField] private Slider monsters;
        [SerializeField] private Slider difficulty;
        [SerializeField] private Slider heroSpeed;
        [SerializeField] private Slider battleSpeed;
        [SerializeField] private Slider music;
        [SerializeField] private Slider effects;
        [SerializeField] private Toggle enemyMoves;
        [SerializeField] private Toggle edgeScroll;
        [SerializeField] private Toggle autoSave;

        public override void UpdateFromSettings(IGameSettings settings)
        {
            if (settings is not HeroesSettings heroes)
            {
                return;
            }
            SetBattleStyle(heroes.battleStyle);
            SetSlider(mapSize, heroes.mapSize);
            SetSlider(treasure, heroes.treasure);
            SetSlider(monsters, heroes.monsters);
            SetSlider(difficulty, heroes.difficulty);
            SetSlider(heroSpeed, heroes.heroSpeed);
            SetSlider(battleSpeed, heroes.battleSpeed);
            SetSlider(music, heroes.musicVolume);
            SetSlider(effects, heroes.effectsVolume);
            SetToggle(enemyMoves, heroes.showEnemyMoves);
            SetToggle(edgeScroll, heroes.edgeScroll);
            SetToggle(autoSave, heroes.autoSave);
        }

        public override void CopyToSettings(IGameSettings settings)
        {
            if (settings is not HeroesSettings heroes)
            {
                return;
            }
            heroes.battleStyle = battles != null ? Mathf.Clamp(battles.Value, 0, 1) : heroes.battleStyle;
            heroes.mapSize = Mathf.RoundToInt(Read(mapSize, heroes.mapSize));
            heroes.treasure = Mathf.RoundToInt(Read(treasure, heroes.treasure));
            heroes.monsters = Mathf.RoundToInt(Read(monsters, heroes.monsters));
            heroes.difficulty = Mathf.RoundToInt(Read(difficulty, heroes.difficulty));
            heroes.heroSpeed = Read(heroSpeed, heroes.heroSpeed);
            heroes.battleSpeed = Read(battleSpeed, heroes.battleSpeed);
            heroes.musicVolume = Read(music, heroes.musicVolume);
            heroes.effectsVolume = Read(effects, heroes.effectsVolume);
            heroes.showEnemyMoves = Read(enemyMoves, heroes.showEnemyMoves);
            heroes.edgeScroll = Read(edgeScroll, heroes.edgeScroll);
            heroes.autoSave = Read(autoSave, heroes.autoSave);
        }

        /// <summary>Shows where battles are fought (a <see cref="BattleStyle"/>) in its row, without saving it.</summary>
        public void SetBattleStyle(int style)
        {
            if (battles != null)
            {
                battles.SetValueWithoutNotify(style);
            }
        }

        /// <summary>While the defaults are in use the rows show them, faded, and do not answer.</summary>
        protected override void SetInputsInteractable(bool interactable)
        {
            if (battles != null)
            {
                battles.Interactable = interactable;
                Fade(battles.transform, interactable);
            }
            foreach (Selectable input in new Selectable[] { mapSize, treasure, monsters, difficulty, heroSpeed, battleSpeed, music, effects,
                         enemyMoves, edgeScroll, autoSave })
            {
                if (input != null)
                {
                    input.interactable = interactable;
                    Fade(input.transform, interactable);
                }
            }
        }

        /// <summary>Fades the row a control sits in (its parent) while it cannot be changed.</summary>
        private static void Fade(Transform control, bool on)
        {
            Transform row = control.parent;
            if (row == null)
            {
                return;
            }
            if (!row.TryGetComponent(out CanvasGroup group))
            {
                group = row.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = on ? 1f : 0.6f;
        }

        /// <summary>The switch between the defaults and your own settings, in the gold and ink of the game.</summary>
        protected override void UpdateCustomSettingsLabel(bool usingDefault)
        {
            if (CustomSettingsButtonText == null)
            {
                return;
            }
            CustomSettingsButtonText.text = usingDefault ? "Use My Settings" : "Use the Defaults";
            CustomSettingsButtonText.color = usingDefault ? UIKit.Gold : UIKit.Ink;
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        private static float Read(Slider slider, float fallback)
        {
            return slider != null ? slider.value : fallback;
        }

        private static bool Read(Toggle toggle, bool fallback)
        {
            return toggle != null ? toggle.isOn : fallback;
        }
    }
}
