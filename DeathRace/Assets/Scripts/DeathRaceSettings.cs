using Gamebox;
using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// What the player can tune: the volumes of the sounds and the music, how close the camera follows, whether hits
    /// shake the screen, whether the minimap shows, and the track, car, difficulty and laps a quick race starts with.
    /// The settings panel edits a runtime copy of the asset.
    /// </summary>
    [CreateAssetMenu(fileName = "DeathRaceSettings", menuName = "Death Race/Settings", order = 1)]
    public sealed class DeathRaceSettings : GameSettings
    {
        public const int MinLaps = 1;
        public const int MaxLaps = 9;

        [Tooltip("The volume of the sound effects, 0 to 1.")]
        public float volume = 0.8f;
        [Tooltip("The volume of the music, 0 to 1.")]
        public float musicVolume = 0.5f;
        [Tooltip("How far the camera stands off the car: 0 near, 1 normal, 2 far.")]
        public int cameraDistance = 1;
        [Tooltip("Hits and explosions shake the screen.")]
        public bool screenShake = true;
        [Tooltip("Show the map of the track in a corner during a race.")]
        public bool showMinimap = true;
        [Tooltip("The laps of a quick race (0 = the track's own number).")]
        public int quickLaps;
        [Tooltip("The car of a quick race (a CarModel).")]
        public int quickCar = (int)CarModel.Viper;
        [Tooltip("The difficulty of a quick race (0 Easy, 1 Medium, 2 Hard).")]
        public int quickDifficulty = (int)Difficulty.Medium;

        public CarModel QuickCar => (CarModel)Mathf.Clamp(quickCar, 0, CarSpecs.Count - 1);

        public Difficulty QuickDifficulty => (Difficulty)Mathf.Clamp(quickDifficulty, 0, (int)Difficulty.Hard);

        public override bool AreSettingsValid(out string message)
        {
            if (volume < 0f || volume > 1f)
            {
                message = $"Volume {volume} has to be between 0 and 1";
                return false;
            }
            if (musicVolume < 0f || musicVolume > 1f)
            {
                message = $"Music volume {musicVolume} has to be between 0 and 1";
                return false;
            }
            if (cameraDistance < 0 || cameraDistance > 2)
            {
                message = $"Camera distance {cameraDistance} has to be 0, 1 or 2";
                return false;
            }
            if (quickLaps != 0 && (quickLaps < MinLaps || quickLaps > MaxLaps))
            {
                message = $"Laps {quickLaps} has to be between {MinLaps} and {MaxLaps} (or 0 for the track's own)";
                return false;
            }
            if (quickCar < 0 || quickCar >= CarSpecs.Count)
            {
                message = $"Car {quickCar} is not one of the {CarSpecs.Count} cars";
                return false;
            }
            if (quickDifficulty < 0 || quickDifficulty > (int)Difficulty.Hard)
            {
                message = $"Difficulty {quickDifficulty} has to be 0, 1 or 2";
                return false;
            }
            message = "OK";
            return true;
        }

        public override void SaveSettings(IStorageStrategy storage, string prefix)
        {
            if (!AreSettingsValid(out string error))
            {
                throw new GameSettingsException($"Cannot save invalid settings. Reason: {error}");
            }
            storage.SetFloat($"{prefix}.Volume", volume);
            storage.SetFloat($"{prefix}.MusicVolume", musicVolume);
            storage.SetInt($"{prefix}.CameraDistance", cameraDistance);
            storage.SetBool($"{prefix}.ScreenShake", screenShake);
            storage.SetBool($"{prefix}.ShowMinimap", showMinimap);
            storage.SetInt($"{prefix}.QuickLaps", quickLaps);
            storage.SetInt($"{prefix}.QuickCar", quickCar);
            storage.SetInt($"{prefix}.QuickDifficulty", quickDifficulty);
        }

        public override void LoadSettings(IStorageStrategy storage, string prefix)
        {
            volume = storage.GetFloat($"{prefix}.Volume", volume);
            musicVolume = storage.GetFloat($"{prefix}.MusicVolume", musicVolume);
            cameraDistance = storage.GetInt($"{prefix}.CameraDistance", cameraDistance);
            screenShake = storage.GetBool($"{prefix}.ScreenShake", screenShake);
            showMinimap = storage.GetBool($"{prefix}.ShowMinimap", showMinimap);
            quickLaps = storage.GetInt($"{prefix}.QuickLaps", quickLaps);
            quickCar = storage.GetInt($"{prefix}.QuickCar", quickCar);
            quickDifficulty = storage.GetInt($"{prefix}.QuickDifficulty", quickDifficulty);
        }

        public override void CopySettings(IGameSettings other)
        {
            if (!(other is DeathRaceSettings source))
            {
                return;
            }
            volume = source.volume;
            musicVolume = source.musicVolume;
            cameraDistance = source.cameraDistance;
            screenShake = source.screenShake;
            showMinimap = source.showMinimap;
            quickLaps = source.quickLaps;
            quickCar = source.quickCar;
            quickDifficulty = source.quickDifficulty;
        }
    }
}
