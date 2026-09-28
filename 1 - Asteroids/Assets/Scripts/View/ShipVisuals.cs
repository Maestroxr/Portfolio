using UnityEngine;

namespace Portfolio.Asteroids
{
    /// <summary>
    /// How the ship looks in flight: the hull model of the chosen ship, a roll into turns, engine flames and exhaust
    /// that follow the throttle, the shield bubble (brighter where it was hit), a muzzle flash, blinking while it is
    /// invulnerable after a respawn and smoke when the hull is failing. In a strike mission the nose stays up and the ship
    /// banks with its sideways speed instead of its turn (<see cref="AnimateStrike"/>).
    /// </summary>
    public class ShipVisuals : MonoBehaviour
    {
        /// <summary>What the visuals show of a ship in one frame: read from the ship itself, or from what a pilot on another device reports.</summary>
        public struct Look
        {
            public float Thrust;
            public float Turn;
            public bool Dashing;
            /// <summary>Invulnerable after a respawn: the hull blinks and the shield glows.</summary>
            public bool Blinking;
            /// <summary>Shield charge, 0 to 1.</summary>
            public float Shield;
            /// <summary>Hull points left, 0 to 1.</summary>
            public float Hull;
        }

        [SerializeField] internal Transform bank;
        [SerializeField] internal Transform modelRoot;
        [SerializeField] internal MeshFilter modelFilter;
        [SerializeField] internal MeshRenderer modelRenderer;
        [SerializeField] internal Transform[] flames = new Transform[0];
        [SerializeField] internal ParticleSystem[] exhausts = new ParticleSystem[0];
        [SerializeField] internal Renderer shieldBubble;
        [SerializeField] internal Renderer muzzle;
        [SerializeField] internal ParticleSystem damageSmoke;
        [SerializeField] internal float maxBank = 30f;
        [SerializeField] internal float flameLength = 1.4f;
        [SerializeField] internal float flameWidth = 0.4f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int HitId = Shader.PropertyToID("_Hit");
        private MaterialPropertyBlock block;
        private float bankAngle;
        private float throttle;
        private float muzzleTime;
        private float shieldFlash;
        private Vector2 shieldHitDirection = Vector2.up;
        private Color muzzleColor = Color.white;
        private Color engineColor = new Color(0.4f, 0.8f, 1f);
        private Color shieldColor = new Color(0.35f, 0.8f, 1.6f);


        /// <summary>The hull shown last (a theme change shows it again in the new look).</summary>
        public PlayerSettings Hull { get; private set; }


        /// <summary>
        /// Shows the model of <paramref name="hull"/> and moves the flames and gun to its engines and nose. The model, its
        /// paint, its size and the engine colour come from the active theme's look of the hull when it has one
        /// (<see cref="AsteroidsTheme.ShipLook"/>), else from the hull itself.
        /// </summary>
        public void SetModel(PlayerSettings hull)
        {
            if (hull == null)
            {
                return;
            }
            Hull = hull;
            AsteroidsTheme.ShipLook look = AsteroidsThemes.Ship(hull.DisplayName);
            Mesh mesh = look != null && look.mesh != null ? look.mesh : hull.ModelMesh;
            Material paint = look != null && look.material != null ? look.material : hull.ModelMaterial;
            float scale = look != null && look.scale > 0f ? look.scale : hull.ModelScale;
            if (modelFilter != null && mesh != null)
            {
                modelFilter.sharedMesh = mesh;
            }
            if (modelRenderer != null && paint != null)
            {
                modelRenderer.sharedMaterial = paint;
            }
            if (modelRoot != null)
            {
                modelRoot.localScale = Vector3.one * scale;
            }
            // The strike drop shadow is a flattened copy of the model ("Shadow/Flat/Model"): it follows the hull too.
            Transform shadowModel = transform.Find("Shadow/Flat/Model");
            if (shadowModel != null)
            {
                MeshFilter shadowFilter = shadowModel.GetComponent<MeshFilter>();
                if (shadowFilter != null && mesh != null)
                {
                    shadowFilter.sharedMesh = mesh;
                }
                if (modelFilter != null)
                {
                    shadowModel.localScale = modelFilter.transform.localScale;
                }
            }
            engineColor = look != null && look.engineColor.a > 0f ? look.engineColor : hull.EngineColor;
            Vector2[] engines = hull.EnginePoints ?? new Vector2[0];
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] == null)
                {
                    continue;
                }
                bool used = i < engines.Length;
                flames[i].gameObject.SetActive(used);
                if (used)
                {
                    flames[i].localPosition = new Vector3(engines[i].x, engines[i].y, 0.05f);
                }
            }
            for (int i = 0; i < exhausts.Length; i++)
            {
                if (exhausts[i] == null)
                {
                    continue;
                }
                bool used = i < engines.Length;
                exhausts[i].gameObject.SetActive(used);
                if (used)
                {
                    exhausts[i].transform.localPosition = new Vector3(engines[i].x, engines[i].y - 0.1f, 0.05f);
                    ParticleSystem.MainModule main = exhausts[i].main;
                    main.startColor = new ParticleSystem.MinMaxGradient(engineColor, Color.white);
                }
            }
            if (muzzle != null)
            {
                muzzle.transform.localPosition = new Vector3(hull.GunPoint.x, hull.GunPoint.y, -0.05f);
            }
        }


        public void ResetVisuals()
        {
            bankAngle = 0f;
            throttle = 0f;
            muzzleTime = 0f;
            shieldFlash = 0f;
            if (bank != null)
            {
                bank.localRotation = Quaternion.identity;
            }
            if (modelRenderer != null)
            {
                modelRenderer.enabled = true;
            }
            if (muzzle != null)
            {
                muzzle.enabled = false;
            }
            if (damageSmoke != null)
            {
                damageSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }


        public void Animate(AsteroidsPlayer ship, float thrust, float turn, float deltaTime)
        {
            Animate(new Look
            {
                Thrust = thrust,
                Turn = turn,
                Dashing = ship.IsDashing,
                Blinking = ship.InvulnerableTime > 0f && !ship.IsDashing,
                Shield = ship.MaxShield > 0f ? ship.Shield / ship.MaxShield : 0f,
                Hull = ship.MaxHealth > 0f ? ship.Health / ship.MaxHealth : 1f
            }, deltaTime);
        }


        /// <summary>
        /// The turn input that makes the ship bank like a strike ship flying sideways at <paramref name="horizontalSpeed"/>
        /// with a top speed of <paramref name="maxSpeed"/> (left is positive, like a left turn); for the stand-ins too.
        /// </summary>
        public static float StrikeBank(float horizontalSpeed, float maxSpeed)
        {
            return maxSpeed > 0.01f ? Mathf.Clamp(-horizontalSpeed / maxSpeed, -1f, 1f) : 0f;
        }


        /// <summary>
        /// A frame of a strike ship flying along <paramref name="move"/> (-1 to 1 per axis): it banks with its sideways
        /// speed, the engines burn harder going up, and the phase shield shows only faintly (and flashes when hit).
        /// </summary>
        public void AnimateStrike(AsteroidsPlayer ship, Vector2 move, float maxSpeed, float deltaTime)
        {
            Animate(new Look
            {
                Thrust = Mathf.Clamp01(0.55f + move.y * 0.45f),
                Turn = StrikeBank(ship.Velocity.x, maxSpeed),
                Dashing = false,
                Blinking = ship.InvulnerableTime > 0f,
                Shield = ship.MaxShield > 0f ? ship.Shield / ship.MaxShield * 0.25f : 0f,
                Hull = ship.MaxHealth > 0f ? ship.Health / ship.MaxHealth : 1f
            }, deltaTime);
        }


        public void Animate(Look ship, float deltaTime)
        {
            float thrust = ship.Thrust;
            bankAngle = Mathf.Lerp(bankAngle, -ship.Turn * maxBank, 1f - Mathf.Exp(-8f * deltaTime));
            if (bank != null)
            {
                bank.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.up);
            }
            float target = ship.Dashing ? 1.6f : Mathf.Max(0.22f, thrust);
            throttle = Mathf.Lerp(throttle, target, 1f - Mathf.Exp(-12f * deltaTime));
            float flicker = 1f + (Mathf.PerlinNoise(Time.time * 30f, 0.3f) - 0.5f) * 0.35f;
            foreach (Transform flame in flames)
            {
                if (flame != null && flame.gameObject.activeSelf)
                {
                    flame.localScale = new Vector3(flameWidth * (0.7f + 0.3f * throttle), flameLength * throttle * flicker, 1f);
                }
            }
            foreach (ParticleSystem exhaust in exhausts)
            {
                if (exhaust == null || !exhaust.gameObject.activeSelf)
                {
                    continue;
                }
                ParticleSystem.EmissionModule emission = exhaust.emission;
                emission.rateOverTime = thrust > 0.05f || ship.Dashing ? 45f * Mathf.Max(0.4f, throttle) : 4f;
            }

            if (modelRenderer != null)
            {
                modelRenderer.enabled = !ship.Blinking || Mathf.Repeat(Time.time * 12f, 1f) < 0.6f;
            }

            UpdateShield(ship, deltaTime);

            if (muzzle != null)
            {
                muzzleTime -= deltaTime;
                muzzle.enabled = muzzleTime > 0f;
                if (muzzle.enabled)
                {
                    block ??= new MaterialPropertyBlock();
                    block.Clear();
                    block.SetColor(BaseColorId, muzzleColor * (3f * muzzleTime / 0.06f));
                    muzzle.SetPropertyBlock(block);
                }
            }

            if (damageSmoke != null)
            {
                bool smoking = ship.Hull < 0.35f;
                if (smoking && !damageSmoke.isEmitting)
                {
                    damageSmoke.Play(true);
                }
                else if (!smoking && damageSmoke.isEmitting)
                {
                    damageSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }


        private void UpdateShield(Look ship, float deltaTime)
        {
            if (shieldBubble == null)
            {
                return;
            }
            shieldFlash = Mathf.Max(0f, shieldFlash - deltaTime * 3f);
            float charge = ship.Shield;
            float strength = charge > 0.001f ? 0.12f + 0.3f * charge : 0f;
            strength += shieldFlash;
            if (ship.Blinking)
            {
                strength = Mathf.Max(strength, 0.6f + 0.3f * Mathf.Sin(Time.time * 10f));
            }
            shieldBubble.enabled = strength > 0.01f;
            if (!shieldBubble.enabled)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            block.Clear();
            Color color = shieldColor * strength;
            color.a = Mathf.Clamp01(strength);
            block.SetColor(BaseColorId, color);
            Vector3 local = transform.InverseTransformDirection(new Vector3(-shieldHitDirection.x, -shieldHitDirection.y, 0f));
            block.SetVector(HitId, new Vector4(local.x, local.y, local.z, shieldFlash));
            shieldBubble.SetPropertyBlock(block);
        }


        public void MuzzleFlash(Color color)
        {
            muzzleColor = color;
            muzzleTime = 0.06f;
        }


        /// <summary>The shield took a hit coming along <paramref name="direction"/>.</summary>
        public void ShieldHit(Vector2 direction)
        {
            shieldFlash = 1f;
            if (direction.sqrMagnitude > 0.001f)
            {
                shieldHitDirection = direction.normalized;
            }
        }
    }
}
