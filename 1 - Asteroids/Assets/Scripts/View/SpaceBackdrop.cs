using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The space behind the playfield: a nebula and star field on a far plane, a planet (with clouds, an atmosphere
    /// glow and optional rings), the local star's light and drifting dust. <see cref="Apply"/> switches to a sector's
    /// <see cref="SectorTheme"/>, blending the colours over a couple of seconds when not instant.
    /// </summary>
    public class SpaceBackdrop : MonoBehaviour
    {
        [SerializeField] internal Camera view;
        [SerializeField] internal Renderer background;
        [SerializeField] internal Transform planet;
        [SerializeField] internal Renderer planetRenderer;
        [SerializeField] internal Renderer cloudRenderer;
        [SerializeField] internal Renderer atmosphere;
        [SerializeField] internal Renderer rings;
        [SerializeField] internal Light sun;
        [SerializeField] internal Light rim;
        [SerializeField] internal float rimIntensity = 0.8f;
        [SerializeField] internal ParticleSystem motes;
        [SerializeField] internal float blendTime = 2.2f;
        [SerializeField] internal float planetSpin = 1.2f;
        [SerializeField] internal float cloudSpin = 2f;

        private static readonly int SpaceColorId = Shader.PropertyToID("_SpaceColor");
        private static readonly int NebulaColorId = Shader.PropertyToID("_NebulaColor");
        private static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");
        private static readonly int DustColorId = Shader.PropertyToID("_DustColor");
        private static readonly int StarDensityId = Shader.PropertyToID("_StarDensity");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private struct Look
        {
            public Color Space;
            public Color Nebula;
            public Color Highlight;
            public Color Dust;
            public float Stars;
            public Color Sun;
            public float SunIntensity;
            public Quaternion SunRotation;
            public Color Ambient;
            public Color Atmosphere;
            public Color Ring;
            public Color Motes;

            public static Look Of(SectorTheme theme)
            {
                return new Look
                {
                    Space = theme.SpaceColor,
                    Nebula = theme.NebulaColor,
                    Highlight = theme.HighlightColor,
                    Dust = theme.DustColor,
                    Stars = theme.StarDensity,
                    Sun = theme.SunColor,
                    SunIntensity = theme.SunIntensity,
                    SunRotation = Quaternion.Euler(theme.SunAngles),
                    Ambient = theme.AmbientColor,
                    Atmosphere = theme.AtmosphereColor,
                    Ring = theme.RingColor,
                    Motes = theme.MotesColor
                };
            }

            public static Look Lerp(Look a, Look b, float t)
            {
                return new Look
                {
                    Space = Color.Lerp(a.Space, b.Space, t),
                    Nebula = Color.Lerp(a.Nebula, b.Nebula, t),
                    Highlight = Color.Lerp(a.Highlight, b.Highlight, t),
                    Dust = Color.Lerp(a.Dust, b.Dust, t),
                    Stars = Mathf.Lerp(a.Stars, b.Stars, t),
                    Sun = Color.Lerp(a.Sun, b.Sun, t),
                    SunIntensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
                    SunRotation = Quaternion.Slerp(a.SunRotation, b.SunRotation, t),
                    Ambient = Color.Lerp(a.Ambient, b.Ambient, t),
                    Atmosphere = Color.Lerp(a.Atmosphere, b.Atmosphere, t),
                    Ring = Color.Lerp(a.Ring, b.Ring, t),
                    Motes = Color.Lerp(a.Motes, b.Motes, t)
                };
            }
        }

        private MaterialPropertyBlock block;
        private Look from;
        private Look to;
        private Look current;
        private float blend = 1f;
        private SectorTheme theme;
        private SectorTheme pendingPlanet;
        private float planetFade = 1f;
        private bool planetOut;

        public SectorTheme Theme => theme;


        /// <summary>Switches to <paramref name="next"/>; blends over <see cref="blendTime"/> unless <paramref name="instant"/>.</summary>
        public void Apply(SectorTheme next, bool instant)
        {
            if (next == null)
            {
                return;
            }
            bool first = theme == null;
            if (next == theme && !instant)
            {
                return;
            }
            theme = next;
            to = Look.Of(next);
            if (instant || first)
            {
                current = to;
                from = to;
                blend = 1f;
                PlacePlanet(next);
                planetFade = 1f;
                planetOut = false;
                pendingPlanet = null;
                Push(current);
                return;
            }
            from = current;
            blend = 0f;
            pendingPlanet = next;
            planetOut = true;
        }


        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (blend < 1f)
            {
                blend = Mathf.Min(1f, blend + deltaTime / Mathf.Max(0.01f, blendTime));
                current = Look.Lerp(from, to, Mathf.SmoothStep(0f, 1f, blend));
                Push(current);
            }
            if (planet != null)
            {
                if (planetOut)
                {
                    planetFade = Mathf.Max(0f, planetFade - deltaTime / (blendTime * 0.5f));
                    if (planetFade <= 0f)
                    {
                        planetOut = false;
                        if (pendingPlanet != null)
                        {
                            PlacePlanet(pendingPlanet);
                            pendingPlanet = null;
                        }
                    }
                }
                else if (planetFade < 1f)
                {
                    planetFade = Mathf.Min(1f, planetFade + deltaTime / (blendTime * 0.5f));
                }
                if (planetRenderer != null)
                {
                    planetRenderer.transform.localRotation = Quaternion.Euler(0f, -planetSpin * deltaTime, 0f) * planetRenderer.transform.localRotation;
                }
                if (cloudRenderer != null)
                {
                    cloudRenderer.transform.localRotation = Quaternion.Euler(0f, -cloudSpin * deltaTime, 0f) * cloudRenderer.transform.localRotation;
                }
                float size = theme != null ? theme.PlanetSize : 60f;
                planet.localScale = Vector3.one * size * Mathf.SmoothStep(0.001f, 1f, planetFade);
            }
        }


        private void PlacePlanet(SectorTheme next)
        {
            if (planet == null)
            {
                return;
            }
            bool hasPlanet = next.PlanetMaterial != null;
            planet.gameObject.SetActive(hasPlanet);
            if (!hasPlanet)
            {
                return;
            }
            if (planetRenderer != null)
            {
                planetRenderer.sharedMaterial = next.PlanetMaterial;
            }
            if (cloudRenderer != null)
            {
                cloudRenderer.gameObject.SetActive(next.CloudMaterial != null);
                if (next.CloudMaterial != null)
                {
                    cloudRenderer.sharedMaterial = next.CloudMaterial;
                }
            }
            if (rings != null)
            {
                rings.gameObject.SetActive(next.Rings);
            }
            Vector3 placement = next.PlanetPlacement;
            float depth = placement.z;
            float fov = view != null ? view.fieldOfView : 40f;
            float aspect = view != null ? view.aspect : 16f / 9f;
            float cameraDistance = view != null ? -view.transform.position.z : 27f;
            float halfHeight = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * (depth + cameraDistance);
            float halfWidth = halfHeight * aspect;
            planet.position = new Vector3((placement.x - 0.5f) * 2f * halfWidth, (placement.y - 0.5f) * 2f * halfHeight, depth);
            planet.localRotation = Quaternion.Euler(next.PlanetTilt);
        }


        private void Push(Look look)
        {
            block ??= new MaterialPropertyBlock();
            if (background != null)
            {
                block.Clear();
                block.SetColor(SpaceColorId, look.Space);
                block.SetColor(NebulaColorId, look.Nebula);
                block.SetColor(HighlightColorId, look.Highlight);
                block.SetColor(DustColorId, look.Dust);
                block.SetFloat(StarDensityId, look.Stars);
                background.SetPropertyBlock(block);
            }
            if (atmosphere != null)
            {
                block.Clear();
                block.SetColor(BaseColorId, look.Atmosphere);
                atmosphere.SetPropertyBlock(block);
            }
            if (rings != null)
            {
                block.Clear();
                block.SetColor(BaseColorId, look.Ring);
                rings.SetPropertyBlock(block);
            }
            if (sun != null)
            {
                sun.color = look.Sun;
                sun.intensity = look.SunIntensity;
                sun.transform.rotation = look.SunRotation;
            }
            if (rim != null)
            {
                // A back light in the sector's highlight colour outlines the hulls and rocks against the dark space.
                float peak = Mathf.Max(0.01f, Mathf.Max(look.Highlight.r, Mathf.Max(look.Highlight.g, look.Highlight.b)));
                rim.color = Color.Lerp(Color.white, new Color(look.Highlight.r / peak, look.Highlight.g / peak, look.Highlight.b / peak), 0.7f);
                rim.intensity = rimIntensity;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = look.Ambient;
            if (motes != null)
            {
                ParticleSystem.MainModule main = motes.main;
                main.startColor = look.Motes;
            }
        }
    }
}
