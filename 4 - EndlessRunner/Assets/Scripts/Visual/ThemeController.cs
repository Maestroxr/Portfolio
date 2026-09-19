using UnityEngine;
using UnityEngine.Rendering;

namespace Portfolio.EndlessRunner
{
    /// <summary>
    /// Applies a <see cref="RunnerTheme"/> to the environment: sky, sun, ambient light, fog and the drifting ambient
    /// particles. Changes of theme blend over a few seconds, which endless runs use when they enter the next world.
    /// </summary>
    public class ThemeController : MonoBehaviour
    {
        private struct Environment
        {
            public Color SkyTop, SkyHorizon, SkyBottom, SunDisc, CloudColor, MountainFar, MountainNear;
            public Color SunColor, AmbientSky, AmbientEquator, AmbientGround, FogColor;
            public float SunSize, StarDensity, CloudCoverage, MountainHeight, SunIntensity, ShadowStrength, FogDensity;
            public Quaternion SunRotation;

            public static Environment From(RunnerTheme theme)
            {
                return new Environment
                {
                    SkyTop = theme.skyTop,
                    SkyHorizon = theme.skyHorizon,
                    SkyBottom = theme.skyBottom,
                    SunDisc = theme.sunDisc,
                    CloudColor = theme.cloudColor,
                    MountainFar = theme.mountainFar,
                    MountainNear = theme.mountainNear,
                    SunColor = theme.sunColor,
                    AmbientSky = theme.ambientSky,
                    AmbientEquator = theme.ambientEquator,
                    AmbientGround = theme.ambientGround,
                    FogColor = theme.fogColor,
                    SunSize = theme.sunSize,
                    StarDensity = theme.starDensity,
                    CloudCoverage = theme.cloudCoverage,
                    MountainHeight = theme.mountainHeight,
                    SunIntensity = theme.sunIntensity,
                    ShadowStrength = theme.shadowStrength,
                    FogDensity = theme.fogDensity,
                    SunRotation = Quaternion.Euler(theme.sunRotation)
                };
            }

            public static Environment Lerp(Environment a, Environment b, float t)
            {
                return new Environment
                {
                    SkyTop = Color.Lerp(a.SkyTop, b.SkyTop, t),
                    SkyHorizon = Color.Lerp(a.SkyHorizon, b.SkyHorizon, t),
                    SkyBottom = Color.Lerp(a.SkyBottom, b.SkyBottom, t),
                    SunDisc = Color.Lerp(a.SunDisc, b.SunDisc, t),
                    CloudColor = Color.Lerp(a.CloudColor, b.CloudColor, t),
                    MountainFar = Color.Lerp(a.MountainFar, b.MountainFar, t),
                    MountainNear = Color.Lerp(a.MountainNear, b.MountainNear, t),
                    SunColor = Color.Lerp(a.SunColor, b.SunColor, t),
                    AmbientSky = Color.Lerp(a.AmbientSky, b.AmbientSky, t),
                    AmbientEquator = Color.Lerp(a.AmbientEquator, b.AmbientEquator, t),
                    AmbientGround = Color.Lerp(a.AmbientGround, b.AmbientGround, t),
                    FogColor = Color.Lerp(a.FogColor, b.FogColor, t),
                    SunSize = Mathf.Lerp(a.SunSize, b.SunSize, t),
                    StarDensity = Mathf.Lerp(a.StarDensity, b.StarDensity, t),
                    CloudCoverage = Mathf.Lerp(a.CloudCoverage, b.CloudCoverage, t),
                    MountainHeight = Mathf.Lerp(a.MountainHeight, b.MountainHeight, t),
                    SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
                    ShadowStrength = Mathf.Lerp(a.ShadowStrength, b.ShadowStrength, t),
                    FogDensity = Mathf.Lerp(a.FogDensity, b.FogDensity, t),
                    SunRotation = Quaternion.Slerp(a.SunRotation, b.SunRotation, t)
                };
            }
        }

        private static readonly int SkyTopId = Shader.PropertyToID("_TopColor");
        private static readonly int SkyHorizonId = Shader.PropertyToID("_HorizonColor");
        private static readonly int SkyBottomId = Shader.PropertyToID("_BottomColor");
        private static readonly int SunDiscId = Shader.PropertyToID("_SunColor");
        private static readonly int SunSizeId = Shader.PropertyToID("_SunSize");
        private static readonly int SunDirectionId = Shader.PropertyToID("_SunDirection");
        private static readonly int StarsId = Shader.PropertyToID("_StarDensity");
        private static readonly int CloudColorId = Shader.PropertyToID("_CloudColor");
        private static readonly int CloudCoverageId = Shader.PropertyToID("_CloudCoverage");
        private static readonly int MountainFarId = Shader.PropertyToID("_MountainFar");
        private static readonly int MountainNearId = Shader.PropertyToID("_MountainNear");
        private static readonly int MountainHeightId = Shader.PropertyToID("_MountainHeight");

        [SerializeField] internal Light sun;
        [Tooltip("Sky material asset; the controller works on a copy so the asset is never changed at runtime.")]
        [SerializeField] internal Material skyMaterial;
        [Tooltip("Ambient particles are kept around this transform (the camera).")]
        [SerializeField] internal Transform follow;
        [SerializeField] internal ParticleSystem pollen;
        [SerializeField] internal ParticleSystem dust;
        [SerializeField] internal ParticleSystem snow;
        [SerializeField] internal ParticleSystem fireflies;
        [SerializeField] internal ParticleSystem embers;
        [SerializeField] internal float transitionTime = 2.5f;

        private Environment from;
        private Environment to;
        private Environment current;
        private float progress = 1f;
        private Material sky;

        public RunnerTheme Theme { get; private set; }

        private void Awake()
        {
            if (skyMaterial != null)
            {
                sky = new Material(skyMaterial) { name = skyMaterial.name + " (runtime)" };
                RenderSettings.skybox = sky;
            }
        }

        private void OnDestroy()
        {
            if (sky != null)
            {
                Destroy(sky);
            }
        }

        /// <summary>Switches to <paramref name="theme"/>, at once or blending over <see cref="transitionTime"/>.</summary>
        public void Apply(RunnerTheme theme, bool instant)
        {
            if (theme == null)
            {
                return;
            }
            bool first = Theme == null;
            Theme = theme;
            to = Environment.From(theme);
            from = first ? to : current;
            progress = instant || first ? 1f : 0f;
            current = progress >= 1f ? to : from;
            Push(current);
            SetAmbient(theme.ambientEffect);
        }

        private void Update()
        {
            if (progress < 1f)
            {
                progress = Mathf.Min(1f, progress + Time.deltaTime / Mathf.Max(0.01f, transitionTime));
                float eased = progress * progress * (3f - 2f * progress);
                current = Environment.Lerp(from, to, eased);
                Push(current);
            }
            if (follow != null)
            {
                Vector3 anchor = follow.position + follow.forward * 8f;
                Place(pollen, anchor);
                Place(dust, anchor - Vector3.up * 2f);
                Place(snow, anchor + Vector3.up * 8f);
                Place(fireflies, anchor);
                Place(embers, anchor - Vector3.up * 2f);
            }
        }

        private static void Place(ParticleSystem system, Vector3 position)
        {
            if (system != null)
            {
                system.transform.position = position;
            }
        }

        private void Push(Environment environment)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = environment.FogColor;
            RenderSettings.fogDensity = environment.FogDensity;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = environment.AmbientSky;
            RenderSettings.ambientEquatorColor = environment.AmbientEquator;
            RenderSettings.ambientGroundColor = environment.AmbientGround;
            if (sun != null)
            {
                sun.color = environment.SunColor;
                sun.intensity = environment.SunIntensity;
                sun.shadowStrength = environment.ShadowStrength;
                sun.transform.rotation = environment.SunRotation;
            }
            if (sky != null)
            {
                sky.SetColor(SkyTopId, environment.SkyTop);
                sky.SetColor(SkyHorizonId, environment.SkyHorizon);
                sky.SetColor(SkyBottomId, environment.SkyBottom);
                sky.SetColor(SunDiscId, environment.SunDisc);
                sky.SetFloat(SunSizeId, environment.SunSize);
                sky.SetFloat(StarsId, environment.StarDensity);
                sky.SetColor(CloudColorId, environment.CloudColor);
                sky.SetFloat(CloudCoverageId, environment.CloudCoverage);
                sky.SetColor(MountainFarId, environment.MountainFar);
                sky.SetColor(MountainNearId, environment.MountainNear);
                sky.SetFloat(MountainHeightId, environment.MountainHeight);
                Vector3 toSun = -(environment.SunRotation * Vector3.forward);
                sky.SetVector(SunDirectionId, toSun);
            }
        }

        private void SetAmbient(AmbientEffect effect)
        {
            Toggle(pollen, effect == AmbientEffect.Pollen);
            Toggle(dust, effect == AmbientEffect.Dust);
            Toggle(snow, effect == AmbientEffect.Snow);
            Toggle(fireflies, effect == AmbientEffect.Fireflies);
            Toggle(embers, effect == AmbientEffect.Embers);
        }

        private static void Toggle(ParticleSystem system, bool on)
        {
            if (system == null)
            {
                return;
            }
            if (on && !system.isEmitting)
            {
                system.Play(true);
            }
            else if (!on && system.isPlaying)
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>Editor helper: applies a theme straight to the scene settings and a sky material asset.</summary>
        public static void ApplyToScene(RunnerTheme theme, Light sunLight, Material skyAsset)
        {
            var controller = new GameObject("Theme Preview").AddComponent<ThemeController>();
            try
            {
                controller.sun = sunLight;
                controller.sky = skyAsset;
                controller.Theme = theme;
                controller.Push(Environment.From(theme));
            }
            finally
            {
                DestroyImmediate(controller.gameObject);
            }
        }
    }
}
