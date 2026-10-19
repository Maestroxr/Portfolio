using UnityEngine;

namespace Portfolio.DeathRace
{
    /// <summary>
    /// The black marks a car's rear tyres leave while it slides, and the puffs of dust (or snow, or tyre smoke) when it
    /// slides hard. Two trails lying flat on the road under the rear wheels, laid while <see cref="CarBody.Slip"/> is high
    /// and fading after a few seconds; the gaps between slides stay clean. Added to every car by its health's setup.
    /// </summary>
    public class SkidMarks : MonoBehaviour
    {
        /// <summary>Slip above which the tyres leave marks.</summary>
        public const float MarkSlip = 0.32f;

        /// <summary>Slip above which the tyres throw dust as well.</summary>
        public const float DustSlip = 0.6f;

        private const float MarkSeconds = 7f;
        private const float MinSpeed = 4f;

        private readonly TrailRenderer[] trails = new TrailRenderer[2];
        private Car car;
        private RaceEffects effects;
        private float dustTimer;
        private bool marking;

        /// <summary>Whether the tyres are leaving marks now.</summary>
        public bool Marking => marking;

        public void Setup(Car owner)
        {
            car = owner;
            effects = owner != null && owner.Race != null ? owner.Race.Effects : null;
            Material material = effects != null ? effects.SkidMaterial : CombatArt.Skid;
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] == null)
                {
                    trails[i] = MakeTrail(i == 0 ? "Skid Left" : "Skid Right", material);
                }
                else
                {
                    trails[i].sharedMaterial = material;
                }
            }
        }

        private TrailRenderer MakeTrail(string name, Material material)
        {
            var go = new GameObject(name);
            // The trails live in the world, not on the car: the race's root keeps them when the car is gone.
            Transform parent = car != null && car.Race != null && car.Race.Root != null ? car.Race.Root : null;
            go.transform.SetParent(parent, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = MarkSeconds;
            trail.minVertexDistance = 0.3f;
            trail.widthMultiplier = 0.42f;
            trail.alignment = LineAlignment.TransformZ;
            trail.textureMode = LineTextureMode.Tile;
            trail.numCapVertices = 0;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = material;
            trail.emitting = false;
            trail.autodestruct = false;
            // Dark rubber whatever the material's texture is (a white mask is darkened by the colour, a dark one stays dark).
            var rubber = new Color(0.09f, 0.08f, 0.07f, 1f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(rubber, 0f), new GradientColorKey(rubber, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            return trail;
        }

        private void LateUpdate()
        {
            if (car == null || car.Body == null)
            {
                return;
            }
            float slip = car.Body.Slip;
            float speed = car.Body.Rigidbody != null ? car.Body.Rigidbody.linearVelocity.magnitude : Mathf.Abs(car.Speed);
            bool slide = !car.IsWrecked && speed > MinSpeed && slip > MarkSlip;
            Quaternion flat = Quaternion.LookRotation(Vector3.up, transform.forward);
            for (int i = 0; i < trails.Length; i++)
            {
                TrailRenderer trail = trails[i];
                if (trail == null)
                {
                    continue;
                }
                trail.transform.SetPositionAndRotation(WheelPoint(i), flat);
                trail.emitting = slide;
            }
            marking = slide;
            if (!slide || slip < DustSlip || effects == null)
            {
                return;
            }
            dustTimer -= Time.deltaTime;
            if (dustTimer <= 0f)
            {
                dustTimer = 0.07f;
                float amount = Mathf.InverseLerp(DustSlip, 1f, slip) + 0.4f;
                effects.Dust(WheelPoint(0), amount);
                effects.Dust(WheelPoint(1), amount);
            }
        }

        /// <summary>Where rear wheel <paramref name="index"/> (0 left, 1 right) touches the road.</summary>
        private Vector3 WheelPoint(int index)
        {
            CarModelView view = car.View;
            Transform wheel = view != null ? (index == 0 ? view.wheelRearLeft : view.wheelRearRight) : null;
            Vector3 point;
            if (wheel != null)
            {
                point = wheel.position;
            }
            else
            {
                Vector3 size = CarSpecs.Of(car.Loadout != null ? car.Loadout.model : CarModel.Rustbucket).Size;
                float side = (index == 0 ? -1f : 1f) * (size.x * 0.5f - 0.3f);
                point = transform.TransformPoint(new Vector3(side, 0f, -size.z * 0.5f + 0.85f));
            }
            point.y = RaceEffects.GroundMarkHeight + 0.01f;
            return point;
        }

        private void OnDestroy()
        {
            // The marks already laid fade where they are; the trails go with the race.
            foreach (TrailRenderer trail in trails)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                    trail.autodestruct = true;
                }
            }
        }
    }
}
