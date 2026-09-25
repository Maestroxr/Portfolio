using System;
using System.Globalization;
using Gamebox;
using UnityEngine;

namespace Portfolio.Heroes
{
    /// <summary>
    /// What a player sets up: how fast heroes walk and battles play, how long the computer's moves are shown, the
    /// sound, the rules of a skirmish (map size, riches, monsters) and how well the computer plays a new game.
    /// </summary>
    [CreateAssetMenu(fileName = "HeroesSettings", menuName = "Heroes/Settings", order = 1)]
    public class HeroesSettings : GameSettings
    {
        [Range(0.5f, 4f)] public float heroSpeed = 1.5f;
        [Range(0.5f, 4f)] public float battleSpeed = 1.25f;
        /// <summary>Show the moves of the computer's heroes the player can see (off: they happen at once).</summary>
        public bool showEnemyMoves = true;
        [Range(0f, 1f)] public float musicVolume = 0.55f;
        [Range(0f, 1f)] public float effectsVolume = 0.8f;
        /// <summary>Scroll the map when the pointer touches the screen's edge.</summary>
        public bool edgeScroll = true;
        public bool autoSave = true;
        /// <summary>0 small, 1 medium, 2 large, for a skirmish.</summary>
        [Range(0, 2)] public int mapSize = 1;
        [Range(1, 3)] public int treasure = 2;
        [Range(1, 4)] public int monsters = 2;
        /// <summary>0 easy, 1 normal, 2 hard: how well the computer players of a new game at this device play (<see cref="MapSpec.SetDifficulty"/>).</summary>
        [Range(0, 2)] public int difficulty = 1;
        /// <summary>Where the battles of a new game are fought: a <see cref="BattleStyle"/> (a saved game keeps its own).</summary>
        [Range(0, 1)] public int battleStyle = (int)BattleStyle.Battlefield;

        private static readonly string[] Keys = { "heroSpeed", "battleSpeed", "showEnemyMoves", "musicVolume", "effectsVolume", "edgeScroll", "autoSave", "mapSize", "treasure", "monsters", "difficulty", "battleStyle" };

        public override void SaveSettings(IStorageStrategy storage, string prefix)
        {
            storage.SetFloat($"{prefix}.{Keys[0]}", heroSpeed);
            storage.SetFloat($"{prefix}.{Keys[1]}", battleSpeed);
            storage.SetBool($"{prefix}.{Keys[2]}", showEnemyMoves);
            storage.SetFloat($"{prefix}.{Keys[3]}", musicVolume);
            storage.SetFloat($"{prefix}.{Keys[4]}", effectsVolume);
            storage.SetBool($"{prefix}.{Keys[5]}", edgeScroll);
            storage.SetBool($"{prefix}.{Keys[6]}", autoSave);
            storage.SetInt($"{prefix}.{Keys[7]}", mapSize);
            storage.SetInt($"{prefix}.{Keys[8]}", treasure);
            storage.SetInt($"{prefix}.{Keys[9]}", monsters);
            storage.SetInt($"{prefix}.{Keys[10]}", difficulty);
            storage.SetInt($"{prefix}.{Keys[11]}", battleStyle);
            storage.TryPersist();
        }

        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            float F(int i, float fallback) => storage.GetFloat($"{prefix}.{Keys[i]}", fallback);
            bool B(int i, bool fallback) => storage.GetBool($"{prefix}.{Keys[i]}", fallback);
            int I(int i, int fallback) => storage.GetInt($"{prefix}.{Keys[i]}", fallback);
            heroSpeed = F(0, heroSpeed);
            battleSpeed = F(1, battleSpeed);
            showEnemyMoves = B(2, showEnemyMoves);
            musicVolume = F(3, musicVolume);
            effectsVolume = F(4, effectsVolume);
            edgeScroll = B(5, edgeScroll);
            autoSave = B(6, autoSave);
            mapSize = I(7, mapSize);
            treasure = I(8, treasure);
            monsters = I(9, monsters);
            difficulty = I(10, difficulty);
            battleStyle = I(11, battleStyle);
        }

        public override void CopySettings(IGameSettings other)
        {
            if (other is HeroesSettings source)
            {
                heroSpeed = source.heroSpeed;
                battleSpeed = source.battleSpeed;
                showEnemyMoves = source.showEnemyMoves;
                musicVolume = source.musicVolume;
                effectsVolume = source.effectsVolume;
                edgeScroll = source.edgeScroll;
                autoSave = source.autoSave;
                mapSize = source.mapSize;
                treasure = source.treasure;
                monsters = source.monsters;
                difficulty = source.difficulty;
                battleStyle = source.battleStyle;
            }
        }

        public override bool AreSettingsValid(out string message)
        {
            if (heroSpeed <= 0f || battleSpeed <= 0f)
            {
                message = "Speeds must be above zero.";
                return false;
            }
            if (mapSize < 0 || mapSize > 2 || treasure < 1 || treasure > 3 || monsters < 1 || monsters > 4 || difficulty < 0 || difficulty > 2 || battleStyle < 0 || battleStyle > 1)
            {
                message = "A setting is out of range.";
                return false;
            }
            message = "OK";
            return true;
        }

        /// <summary>Columns and rows of a skirmish map of the chosen size (<see cref="MapSpec.SkirmishSize"/>).</summary>
        public static Vector2Int MapDimensions(int size)
        {
            MapSpec.SkirmishSize(size, out int columns, out int rows);
            return new Vector2Int(columns, rows);
        }

        public static string MapSizeName(int size)
        {
            return size == 0 ? "Small" : size == 2 ? "Large" : "Medium";
        }

        public static string Percent(float value)
        {
            return Mathf.RoundToInt(value * 100f).ToString(CultureInfo.InvariantCulture) + "%";
        }
    }
}
