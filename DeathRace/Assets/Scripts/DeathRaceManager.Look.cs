using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// ART-WORLD owns this file.
    ///
    /// The theme's light on the scene: the sun (the moon at night), the ambient light, the fog and the colour behind the
    /// world (both the haze of the land being raced on, when one is shown), the sky that shiny things reflect (the
    /// theme's "reflection" texture slot: the scene has no sky box, so without it paint, chrome and wet asphalt would
    /// reflect black) and the post processing. The tracks, cars and interface read the theme themselves.
    /// </summary>
    public partial class DeathRaceManager
    {
        /// <summary>The key of the reflected sky among the theme's texture slots.</summary>
        public const string ReflectionSlot = "reflection";

        private bool showsBiome;
        private Biome shownBiome;

        /// <summary>
        /// The land the cameras look at now (the track just built): its haze becomes the fog and the colour behind the
        /// world. The race director calls it when it builds a track; the theme's own background applies until then.
        /// </summary>
        public void ShowBiome(Biome biome)
        {
            showsBiome = true;
            shownBiome = biome;
            ApplyLook();
        }

        partial void ApplyLook()
        {
            DeathRaceTheme look = Look;
            if (look == null)
            {
                return;
            }
            if (!showsBiome && CurrentRace != null && CurrentRace.Track != null && CurrentRace.Track.Layout != null)
            {
                showsBiome = true;
                shownBiome = CurrentRace.Track.Layout.Biome;
            }
            BiomeLook land = showsBiome ? look.Biome(shownBiome) : null;
            Color haze = land != null && land.fogColor.a > 0f ? land.fogColor : look.background;
            if (sun != null)
            {
                sun.color = look.sunColor;
                sun.intensity = look.sunIntensity;
                sun.transform.rotation = Quaternion.Euler(look.sunEuler);
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.ambientSky;
            RenderSettings.ambientEquatorColor = look.ambientEquator;
            RenderSettings.ambientGroundColor = look.ambientGround;
            RenderSettings.fog = look.fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = look.fogStart;
            RenderSettings.fogEndDistance = look.fogEnd;
            RenderSettings.fogColor = haze;
            if (look.TextureOf(ReflectionSlot) is Cubemap sky)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = sky;
                RenderSettings.reflectionIntensity = 1f;
            }
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = haze;
            }
            if (volume != null && look.volume != null)
            {
                volume.sharedProfile = look.volume;
            }
            DynamicGI.UpdateEnvironment();
        }
    }
}
