using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The visual side of a car model, on the root of its prefab: the wheels that turn and steer, the painted body, the
    /// mounts of the weapons, the exhausts and the lights. It has no physics; the race adds that when it spawns a car.
    ///
    /// The prefab is drawn in two materials: the palette (every unpainted part, the wheels and the light lenses) and the
    /// paint (the body panels, tinted per car with a property block, so every car of a race wears its driver's colour
    /// while all cars share two materials). A look swaps both for its own (<see cref="ApplyTheme"/>), lights the lamps at
    /// night and shows the headlight beams on the road. Some cars carry lamps that glow all the time (the Spectre's light
    /// strips, the Apocalypse's jet nozzles), brighter at night and flaring while the nitro burns, and a car may wear a
    /// duller finish than the look's glossy paint (<see cref="paintGloss"/>). Braking, damage, nitro and a wreck change
    /// only property blocks, enabled flags and, for a wreck, the materials: nothing allocates while a race runs.
    /// </summary>
    [DisallowMultipleComponent]
    public class CarModelView : MonoBehaviour
    {
        public CarModel model;
        public Transform wheelFrontLeft, wheelFrontRight, wheelRearLeft, wheelRearRight;
        [Tooltip("Renderers drawn in the paint material (tinted per car).")]
        public Renderer[] paint = new Renderer[0];
        [Tooltip("Renderers drawn in the palette material.")]
        public Renderer[] body = new Renderer[0];
        public Transform muzzle;
        public Transform mineDrop;
        public Transform rocketMount;
        [Tooltip("Exhaust tips; each one's forward is the direction it blows.")]
        public Transform[] exhausts = new Transform[0];
        public Renderer[] headlights = new Renderer[0];
        public Renderer[] taillights = new Renderer[0];

        [Header("Lights and flames")]
        [Tooltip("The light of the headlights on the road ahead (shown at night).")]
        public Renderer[] beams = new Renderer[0];
        [Tooltip("The red glow behind the tail lights (shown at night, brighter while braking).")]
        public Renderer[] tailGlows = new Renderer[0];
        [Tooltip("Flames out of the exhausts while the nitro burns.")]
        public Renderer[] nitroFlames = new Renderer[0];
        [Tooltip("The soft shadow under the car.")]
        public Renderer shadow;
        [Tooltip("Lamps of the body that glow by day and by night and flare up while the nitro burns (light strips, jet nozzles).")]
        public Renderer[] glows = new Renderer[0];

        [Header("Shape")]
        public float frontWheelRadius = 0.35f;
        public float rearWheelRadius = 0.35f;
        [Tooltip("How glossy the paint is against the look's paint (1); a matte finish is less.")]
        [Range(0f, 1.5f)] public float paintGloss = 1f;
        [SerializeField] private Bounds bounds = new Bounds(new Vector3(0f, 0.7f, 0f), new Vector3(2f, 1.4f, 4.2f));

        // Lamp brightness (multiplies the palette's emission map) by day and by night.
        private const float HeadDay = 0.35f;
        private const float HeadNight = 2.8f;
        private const float TailDay = 0.35f;
        private const float TailNight = 1.4f;
        private const float BrakeDay = 2.4f;
        private const float BrakeNight = 4.2f;
        private const float GlowDay = 1.1f;
        private const float GlowNight = 2.2f;
        private const float GlowNitro = 2.4f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly Color Grime = new Color(0.36f, 0.31f, 0.26f, 1f);
        private static readonly Color Dust = new Color(0.68f, 0.63f, 0.58f, 1f);
        private static readonly Color Charred = new Color(0.12f, 0.11f, 0.1f, 1f);
        private static bool warnedNoTheme;

        private MaterialPropertyBlock block;
        private Material paletteMaterial;
        private Material paintMaterial;
        private Material wreckMaterial;
        private float paintSmoothness = 0.75f;
        private Color paintColor = Color.white;
        private bool night;
        private bool braking;
        private bool nitro;
        private bool wrecked;
        private float damage;
        private Transform[] rearWheels;
        private Transform[] wheels;
        private Vector3[] flameScales;

        /// <summary>The box the model fills, in its own space.</summary>
        public Bounds Bounds => bounds;

        public bool IsWrecked => wrecked;

        public bool IsNight => night;

        public Color PaintColor => paintColor;

        public Transform[] RearWheels => rearWheels ?? (rearWheels = new[] { wheelRearLeft, wheelRearRight });

        /// <summary>Front left, front right, rear left, rear right.</summary>
        public Transform[] Wheels => wheels ?? (wheels = new[] { wheelFrontLeft, wheelFrontRight, wheelRearLeft, wheelRearRight });

        private void Awake()
        {
            Prepare();
        }

        /// <summary>Remembers the prefab's own materials as the fallback look (once).</summary>
        private void Prepare()
        {
            if (block != null)
            {
                return;
            }
            block = new MaterialPropertyBlock();
            paletteMaterial = FirstMaterial(body);
            paintMaterial = FirstMaterial(paint);
            if (paintMaterial != null && paintMaterial.HasProperty(SmoothnessId))
            {
                paintSmoothness = paintMaterial.GetFloat(SmoothnessId);
            }
            flameScales = new Vector3[nitroFlames.Length];
            for (int i = 0; i < nitroFlames.Length; i++)
            {
                flameScales[i] = nitroFlames[i] != null ? nitroFlames[i].transform.localScale : Vector3.one;
            }
        }

        private static Material FirstMaterial(Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    return renderer.sharedMaterial;
                }
            }
            return null;
        }

        /// <summary>Dresses the car in a look's materials and lights, painted <paramref name="paintColor"/>.</summary>
        public virtual void ApplyTheme(DeathRaceTheme theme, Color paintColor)
        {
            Prepare();
            this.paintColor = paintColor;
            if (theme != null)
            {
                night = theme.night;
                if (theme.carPalette != null && theme.carPaint != null)
                {
                    paletteMaterial = theme.carPalette;
                    paintMaterial = theme.carPaint;
                    if (paintMaterial.HasProperty(SmoothnessId))
                    {
                        paintSmoothness = paintMaterial.GetFloat(SmoothnessId);
                    }
                }
                else if (!warnedNoTheme)
                {
                    warnedNoTheme = true;
                    Debug.LogWarning($"Death Race: the look {theme.name} has no car materials yet; the cars keep the materials of their prefabs.");
                }
                if (theme.carWreck != null)
                {
                    wreckMaterial = theme.carWreck;
                }
            }
            ApplyMaterials();
            ShowLooks();
            Refresh();
        }

        /// <summary>Turns the front wheels by <paramref name="steerAngle"/> degrees and rolls all four to <paramref name="spinDegrees"/>.</summary>
        /// <remarks>
        /// <paramref name="spinDegrees"/> grows as the car rolls forward (distance over a wheel radius, in degrees; it
        /// may wrap at 360). All four wheels take the same roll, so a wrap never makes one of them jump.
        /// </remarks>
        public virtual void SetWheels(float steerAngle, float spinDegrees)
        {
            if (wrecked)
            {
                return;
            }
            Quaternion front = Quaternion.Euler(spinDegrees, steerAngle, 0f);
            if (wheelFrontLeft != null)
            {
                wheelFrontLeft.localRotation = front;
            }
            if (wheelFrontRight != null)
            {
                wheelFrontRight.localRotation = front;
            }
            Quaternion back = Quaternion.Euler(spinDegrees, 0f, 0f);
            if (wheelRearLeft != null)
            {
                wheelRearLeft.localRotation = back;
            }
            if (wheelRearRight != null)
            {
                wheelRearRight.localRotation = back;
            }
        }

        public virtual void SetBraking(bool braking)
        {
            if (this.braking == braking)
            {
                return;
            }
            this.braking = braking;
            Prepare();
            RefreshLights();
        }

        public virtual void SetNitro(bool burning)
        {
            if (nitro == burning)
            {
                return;
            }
            nitro = burning;
            Prepare();
            SetEnabled(nitroFlames, nitro && !wrecked);
            if (glows.Length > 0)
            {
                RefreshLights();
            }
        }

        public virtual void SetWrecked(bool wrecked)
        {
            if (this.wrecked == wrecked)
            {
                return;
            }
            this.wrecked = wrecked;
            Prepare();
            ApplyMaterials();
            ShowLooks();
            Refresh();
        }

        /// <summary>How damaged the car looks, 1 = like new.</summary>
        public virtual void SetDamage(float health01)
        {
            float next = 1f - Mathf.Clamp01(health01);
            if (Mathf.Abs(next - damage) < 0.02f && !(next == 0f && damage != 0f))
            {
                return;
            }
            damage = next;
            Prepare();
            RefreshSurfaces();
            RefreshLights();
        }

        private void LateUpdate()
        {
            if (!nitro || wrecked || flameScales == null)
            {
                return;
            }
            // A cheap flicker: the flames stretch and shrink a little every frame.
            int frame = Time.frameCount;
            for (int i = 0; i < nitroFlames.Length && i < flameScales.Length; i++)
            {
                if (nitroFlames[i] == null)
                {
                    continue;
                }
                float noise = Mathf.Abs(Mathf.Sin((frame + i * 7) * 12.9898f) * 43758.5453f) % 1f;
                Vector3 scale = flameScales[i];
                nitroFlames[i].transform.localScale = new Vector3(scale.x * (0.9f + 0.2f * noise), scale.y * (0.9f + 0.2f * noise), scale.z * (0.75f + 0.5f * noise));
            }
        }

        // ------------------------------------------------------------------ drawing

        private void ApplyMaterials()
        {
            Material palette = wrecked && wreckMaterial != null ? wreckMaterial : paletteMaterial;
            Material painted = wrecked && wreckMaterial != null ? wreckMaterial : paintMaterial;
            SetMaterial(body, palette);
            SetMaterial(headlights, palette);
            SetMaterial(taillights, palette);
            SetMaterial(glows, palette);
            SetMaterial(paint, painted);
        }

        private static void SetMaterial(Renderer[] renderers, Material material)
        {
            if (material == null)
            {
                return;
            }
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.sharedMaterial != material)
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        private void ShowLooks()
        {
            SetEnabled(beams, night && !wrecked);
            SetEnabled(tailGlows, night && !wrecked);
            SetEnabled(nitroFlames, nitro && !wrecked);
        }

        private static void SetEnabled(Renderer[] renderers, bool on)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = on;
                }
            }
        }

        private void Refresh()
        {
            RefreshSurfaces();
            RefreshLights();
        }

        private void RefreshSurfaces()
        {
            if (wrecked)
            {
                // The wreck material is charred already (its own colour must show); without one the palette is darkened.
                if (wreckMaterial != null)
                {
                    ClearBlocks(body);
                    ClearBlocks(paint);
                }
                else
                {
                    ApplyBlock(body, Charred, null, false, 0f);
                    ApplyBlock(paint, Charred, null, false, 0f);
                }
                return;
            }
            Color dirt = Color.Lerp(Color.white, Dust, damage * 0.7f);
            ApplyBlock(body, dirt, null, false, 0f);
            // Damage greys the paint out and dirties it.
            float grey = paintColor.r * 0.3f + paintColor.g * 0.59f + paintColor.b * 0.11f;
            Color faded = Color.Lerp(paintColor, new Color(grey, grey, grey, 1f), damage * 0.55f);
            Color painted = faded * Color.Lerp(Color.white, Grime, damage * 0.85f);
            painted.a = 1f;
            ApplyBlock(paint, painted, Mathf.Lerp(paintSmoothness * paintGloss, 0.22f, damage * 0.85f), false, 0f);
        }

        private void RefreshLights()
        {
            if (block == null)
            {
                return;
            }
            if (wrecked && wreckMaterial != null)
            {
                ClearBlocks(headlights);
                ClearBlocks(taillights);
                ClearBlocks(glows);
                return;
            }
            Color dirt = wrecked ? Charred : Color.Lerp(Color.white, Dust, damage * 0.7f);
            float head = wrecked ? 0f : night ? HeadNight : HeadDay;
            float tail = wrecked ? 0f : braking ? (night ? BrakeNight : BrakeDay) : night ? TailNight : TailDay;
            // A badly damaged car's lamps dim.
            float failing = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((damage - 0.6f) / 0.4f));
            ApplyBlock(headlights, dirt, null, true, head * failing);
            ApplyBlock(taillights, dirt, null, true, tail);
            if (glows.Length > 0)
            {
                float glow = wrecked ? 0f : (night ? GlowNight : GlowDay) * (nitro ? GlowNitro : 1f) * failing;
                ApplyBlock(glows, dirt, null, true, glow);
            }
            foreach (Renderer glow in tailGlows)
            {
                if (glow == null)
                {
                    continue;
                }
                block.Clear();
                block.SetColor(BaseColorId, new Color(1f, 0.12f, 0.08f, braking ? 0.85f : 0.35f));
                glow.SetPropertyBlock(block);
            }
        }

        private static void ClearBlocks(Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }
        }

        private void ApplyBlock(Renderer[] renderers, Color baseColor, float? smoothness, bool emission, float emissionLevel)
        {
            if (block == null)
            {
                return;
            }
            block.Clear();
            block.SetColor(BaseColorId, baseColor);
            if (smoothness.HasValue)
            {
                block.SetFloat(SmoothnessId, smoothness.Value);
            }
            if (emission)
            {
                block.SetColor(EmissionColorId, new Color(emissionLevel, emissionLevel, emissionLevel, 1f));
            }
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(block);
                }
            }
        }
    }
}
