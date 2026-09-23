using Gamebox;
using Gamebox.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.Heroes.UI
{
    /// <summary>
    /// The settings panel of the game: how big a skirmish map is, how much it holds, how hard the computer players
    /// are, and how fast heroes walk and battles play. The base class handles the default and custom switch and the
    /// save, load and back buttons.
    /// </summary>
    public class HeroesSettingsUI : SettingsUI
    {
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
