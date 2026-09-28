using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.Monopoly
{
    /// <summary>
    /// The parts of the scene around the board that a theme changes but no themed component covers: the sky (the
    /// camera background, the fog, the ambient light), the sun and the fill light, what the metal reflects and the
    /// colours of the confetti. The manager applies the active theme when the scene starts and on every theme change.
    /// </summary>
    public class MonopolySkin : MonoBehaviour
    {
        [SerializeField] private Camera view;
        [SerializeField] private Light sun;
        [SerializeField] private Light fill;
        [SerializeField] private ParticleSystem confetti;

        public void Apply(MonopolyTheme theme)
        {
            if (theme == null)
            {
                return;
            }
            SceneLook look = theme.scene;
            if (view != null)
            {
                view.backgroundColor = look.background;
            }
            RenderSettings.fogColor = look.background;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.ambientSky;
            RenderSettings.ambientEquatorColor = look.ambientEquator;
            RenderSettings.ambientGroundColor = look.ambientGround;
            if (look.reflection != null)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = look.reflection;
                RenderSettings.reflectionIntensity = look.reflectionIntensity;
            }
            if (sun != null)
            {
                sun.color = look.sunColor;
                sun.intensity = look.sunIntensity;
            }
            if (fill != null)
            {
                fill.color = look.fillColor;
                fill.intensity = look.fillIntensity;
            }
            if (confetti != null && look.confetti != null && look.confetti.Count > 0)
            {
                ParticleSystem.MainModule main = confetti.main;
                main.startColor = new ParticleSystem.MinMaxGradient(Gradient(look.confetti)) { mode = ParticleSystemGradientMode.RandomColor };
            }
        }

        /// <summary>A gradient with a key per colour, evenly spread, for a random pick of one of them.</summary>
        public static Gradient Gradient(IList<Color> colors)
        {
            var gradient = new Gradient();
            int count = Mathf.Clamp(colors.Count, 1, 8);
            var keys = new GradientColorKey[count];
            for (int i = 0; i < count; i++)
            {
                keys[i] = new GradientColorKey(colors[i], count == 1 ? 0f : i / (float)(count - 1));
            }
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}
