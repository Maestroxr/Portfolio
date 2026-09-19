using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// The look of one sector of space: the colours of the nebula behind the playfield, the planet hanging in it, the
    /// light of the local star and the interface accent. <see cref="SpaceBackdrop"/> applies it to the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "SectorTheme", menuName = "Asteroids/Sector Theme", order = 4)]
    public class SectorTheme : ScriptableObject
    {
        [SerializeField] internal string title = "Sector";
        [SerializeField] internal Color accent = new Color(0.3f, 0.85f, 1f);

        [Header("Nebula")]
        [SerializeField] internal Color spaceColor = new Color(0.01f, 0.012f, 0.03f);
        [ColorUsage(false, true)] [SerializeField] internal Color nebulaColor = new Color(0.1f, 0.3f, 0.6f);
        [ColorUsage(false, true)] [SerializeField] internal Color highlightColor = new Color(0.4f, 0.9f, 1f);
        [ColorUsage(false, true)] [SerializeField] internal Color dustColor = new Color(0.05f, 0.02f, 0.08f);
        [SerializeField, Range(0f, 2f)] internal float starDensity = 1f;

        [Header("Planet")]
        [SerializeField] internal Material planetMaterial;
        [SerializeField] internal Material cloudMaterial;
        [ColorUsage(false, true)] [SerializeField] internal Color atmosphereColor = new Color(0.3f, 0.6f, 1f);
        [Tooltip("Where the planet hangs, in view-space fractions: x and y across the screen, z its depth in meters.")]
        [SerializeField] internal Vector3 planetPlacement = new Vector3(0.78f, 0.22f, 160f);
        [SerializeField] internal float planetSize = 70f;
        [SerializeField] internal Vector3 planetTilt = new Vector3(20f, 0f, -15f);
        [SerializeField] internal bool rings;
        [ColorUsage(true, true)] [SerializeField] internal Color ringColor = new Color(0.8f, 0.9f, 1f, 0.8f);

        [Header("Light")]
        [SerializeField] internal Color sunColor = new Color(1f, 0.95f, 0.88f);
        [SerializeField] internal float sunIntensity = 1.4f;
        [SerializeField] internal Vector3 sunAngles = new Vector3(35f, -35f, 0f);
        [SerializeField] internal Color ambientColor = new Color(0.12f, 0.14f, 0.2f);
        [ColorUsage(false, true)] [SerializeField] internal Color motesColor = new Color(0.5f, 0.7f, 1f);

        public string Title => title;
        public Color Accent => accent;
        public Color SpaceColor => spaceColor;
        public Color NebulaColor => nebulaColor;
        public Color HighlightColor => highlightColor;
        public Color DustColor => dustColor;
        public float StarDensity => starDensity;
        public Material PlanetMaterial => planetMaterial;
        public Material CloudMaterial => cloudMaterial;
        public Color AtmosphereColor => atmosphereColor;
        public Vector3 PlanetPlacement => planetPlacement;
        public float PlanetSize => planetSize;
        public Vector3 PlanetTilt => planetTilt;
        public bool Rings => rings;
        public Color RingColor => ringColor;
        public Color SunColor => sunColor;
        public float SunIntensity => sunIntensity;
        public Vector3 SunAngles => sunAngles;
        public Color AmbientColor => ambientColor;
        public Color MotesColor => motesColor;
    }
}
